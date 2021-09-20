using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Boogie
{
  public class Prune {

    public static Dictionary<DeclarationDependencies, List<DeclarationDependencies>> InitializeEdges(Program program)
    {
      if (!CommandLineOptions.Clo.PruneFunctionsAndAxioms)
      {
        return null;
      }
      var nodes = program.Axioms.Select(ax => (DeclarationDependencies)new AxiomVisitor(ax)).ToList();
      nodes.ForEach(axv => ((AxiomVisitor)axv).Visit(axv.declaration));
      var functionNodes = program.Functions.Select(f => (DeclarationDependencies)new FunctionVisitor(f)).ToList();
      functionNodes.ForEach(fv => ((FunctionVisitor)fv).Visit(fv.declaration));
      nodes.AddRange(functionNodes);
      var edges = new Dictionary<DeclarationDependencies, List<DeclarationDependencies>>();
      
      // TODO remove square complexity.
      nodes.ForEach(u => edges[u] = nodes.Where(v => DeclarationDependencies.Depends(u, v)).ToList());
      return edges;
    }

    public static Variable GetWhereVariable(Cmd c) {
      if (c is AssumeCmd ac)
      {
        var attr = QKeyValue.FindAttribute(ac.Attributes, qkv => qkv.Key == "where" && qkv.Params.Count == 1);
        if (attr != null)
        {
          var ie = (IdentifierExpr) attr.Params[0];
          return ie.Decl;
        }
      }
      return null;
    }

    public static void TrimWhereAssumes(List<Block> blocks, HashSet<Variable> liveVars) {
      var whereAssumes = new Dictionary<Variable, AssumeVisitor> ();
      foreach (var blk in blocks)
      {
        foreach(var cmd in blk.Cmds)
        {
          var v = GetWhereVariable(cmd);
          if (v != null)
          {
            var ac = cmd as AssumeCmd;
            whereAssumes[v] = new AssumeVisitor(ac);
            whereAssumes[v].Visit(ac);
          }
        }
      }

      var todo = new Stack<Variable> (liveVars);
      while (todo.Any())
      {
        var t = todo.Pop();
        if (whereAssumes.Keys.Contains(t)) {
          whereAssumes[t].RelVars.Where(v => !liveVars.Contains(v)).ToList().ForEach(v => todo.Push(v));
        }
        liveVars.Add(t);
      }

      bool DeadWhereAssumption(Cmd c)
      {
        var v = GetWhereVariable(c);
        return v != null && !liveVars.Contains(v);
      }

      blocks.ForEach(blk => blk.Cmds = blk.Cmds.Where(c => !DeadWhereAssumption(c)).ToList());
    }

    public static IEnumerable<Declaration> PruneDecl(Program p, List<Block> blocks)
    {
      if (p.edges == null || blocks == null || !CommandLineOptions.Clo.PruneFunctionsAndAxioms)
      {
        return p.TopLevelDeclarations;
      }

      var edges = p.edges;
      // an implementation only has outgoing edges.
      BlocksVisitor bnode = new BlocksVisitor(blocks);
      bnode.Blocks.ForEach(blk => bnode.Visit(blk));
      TrimWhereAssumes(blocks, bnode.RelVars);
      var implHooks = edges.Keys.Where(m => DeclarationDependencies.Depends(bnode, m));

      var reachableDeclarations = ComputeReachability(p, implHooks).ToHashSet();
      var result = p.TopLevelDeclarations.Where(d => d is not Constant && d is not Axiom && d is not Function || reachableDeclarations.Contains(d));
      return result;
    }
    
    static IEnumerable<Declaration> ComputeReachability(Program program, IEnumerable<DeclarationDependencies> implHooks)
    {
      var edges = program.edges;
      var todo = new Stack<DeclarationDependencies>(implHooks);
      var visited = new HashSet<DeclarationDependencies>();
      while(todo.Any())
      {
        var d = todo.Pop();
        foreach (var x in edges[d].Where(t => !visited.Contains(t)))
        {
          todo.Push(x);
        }
        visited.Add(d);
      }
      return visited.Select(a => a.declaration);
    }
  }
}