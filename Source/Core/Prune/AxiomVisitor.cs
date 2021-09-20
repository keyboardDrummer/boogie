using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.Boogie
{
  internal class AxiomVisitor : DeclarationDependencies
  {
    public AxiomVisitor (Axiom a) : base(a) {}

    private void VisitTriggerCustom(Trigger t)
    {
      var axiomVisitor = new AxiomVisitor((Axiom)declaration);
      var triggerList = t.Tr.ToList(); // TODO remove ToList, rename Tr.
      triggerList.ForEach(e => e.pos = Expr.Position.Neither);
      triggerList.ForEach(e => axiomVisitor.VisitExpr(e));
      incomings.Add(axiomVisitor.Outgoings);
    }

    private void SaveIncoming(Action action)
    {
      var old = incomings;
      incomings = new SetOfSets<Declaration>();
      action();
      incomings = old;
    }
    
    // TODO: Why do we need these switches instead of using the visitor?
    public override Expr VisitExpr(Expr node) {
      if (node is IdentifierExpr { Decl: Constant c }) {
        AddIncoming(c);
        AddOutgoing(c);
      } else if (node is NAryExpr e && e.Fun is FunctionCall f) {
        AddIncoming(f.Func);
        AddOutgoing(f.Func);
      } else if (node is NAryExpr n) {
        var appliable = n.Fun;
        if (appliable is UnaryOperator op) {
          Contract.Assert(op.Op == UnaryOperator.Opcode.Neg || op.Op == UnaryOperator.Opcode.Not);
          Contract.Assert(n.Args.Count() == 1);
          n.Args[0].pos = Expr.NegatePosition(n.Args[0].pos);
        } else if (appliable is BinaryOperator bin) {
          Contract.Assert(n.Args.Count() == 2);
          if (bin.Op == BinaryOperator.Opcode.And
              || bin.Op == BinaryOperator.Opcode.Or) {
          } else if (bin.Op == BinaryOperator.Opcode.Imp) {
            n.Args[0].pos = Expr.NegatePosition(n.Args[0].pos);
          } else {
            n.Args.ToList().ForEach(a => a.pos = Expr.Position.Neither);
          }
        } else {
          n.Args.ToList().ForEach(a => a.pos = Expr.Position.Neither);
        }
      } else if (node is QuantifierExpr quantifierExpr) {
        return VisitQuantifierExprCustom(quantifierExpr);
      } else if (node is OldExpr o) {
        o.Expr.pos = Expr.Position.Neither;
      } else if (node is CodeExpr) {
        // no blocks in axioms
        Contract.Assert(false);
      } else if (node is BvExtractExpr bve) {
        bve.Bitvector.pos = Expr.Position.Neither;
      } else if (node is BvConcatExpr bvc) {
        bvc.E0.pos = Expr.Position.Neither;
        bvc.E1.pos = Expr.Position.Neither;
      } else if (node is BinderExpr bexp) {
        bexp.Body.pos = Expr.Position.Neither;
      } else if (node is LetExpr l){
        l.Body.pos = Expr.Position.Neither;
      } else {
        if(node is LiteralExpr || node is IdentifierExpr) {

        } else {
          Console.WriteLine(node);
          Contract.Assert(false);
        }
      }
      return base.VisitExpr(node);
    }

    private Expr VisitQuantifierExprCustom(QuantifierExpr quantifierExpr)
    {
      Trigger start = quantifierExpr.Triggers;
      while (start != null) {
        VisitTriggerCustom(start);
        start = start.Next;
      }

      var discardBodyIncoming = quantifierExpr is ForallExpr { pos: Expr.Position.Pos } && quantifierExpr.Triggers != null
                                || quantifierExpr is ExistsExpr { pos: Expr.Position.Neg };
      quantifierExpr.Body.pos = Expr.Position.Neither;

      if (discardBodyIncoming) {
        var old = incomings;
        incomings = new SetOfSets<Declaration>();
        VisitExpr(quantifierExpr.Body);
        incomings = old;
      } else {
        VisitExpr(quantifierExpr.Body);
      }

      return null;
    }

    public override Boogie.Type VisitType(Boogie.Type node)
    {
      types.Add(node);
      return base.VisitType(node);
    }
  }
}