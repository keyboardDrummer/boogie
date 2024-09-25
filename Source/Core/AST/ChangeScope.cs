#nullable enable
using System.Collections.Generic;

namespace Microsoft.Boogie;

/// <summary>
/// Allows pushing and popping scopes inside a Boogie implementation.
/// 
/// Right now these scopes only affect the state of what functions are hidden and revealed using the hide and reveal keywords.
/// However, in the future these scopes should also allow lexical scoping and variable shadowing.
/// </summary>
public class ChangeScope : Cmd {
  public enum Modes { Push, Pop }
  public Modes Mode { get; }

  public ChangeScope(IToken tok, Modes mode) : base(tok) {
    Mode = mode;
  }

  public override void Resolve(ResolutionContext rc) {
  }

  public override void Typecheck(TypecheckingContext tc) {
  }

  public override void Emit(TokenTextWriter stream, int level) {
    stream.Write(this, level, Mode == Modes.Push ? "push" : "pop");
    stream.WriteLine(";");
  }

  public override void AddAssignedIdentifiers(List<IdentifierExpr> vars) {
  }

  public override Absy StdDispatch(StandardVisitor visitor) {
    return visitor.VisitChangeScopeCmd(this);
  }
}