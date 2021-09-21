using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Boogie
{
  public class DeclarationDependencies : ReadOnlyVisitor
  {
    // For each declaration, we compute incoming and outgoing dependents.
    // Incoming dependents are functions or constants that the declaration may help the solver with.
    // Most incoming dependents correspond to exactly one function or constant, but some of them are tuples.
    // For example, consider an axiom of the form:
    //                        axiom forall x, y :: {P(x, y), Q(y)} {R(x)} P(x, y) ==> R(x)
    // The axiom may (only) be triggerd by a declaration/implementation that either mentions
    // both P and Q or mentions function R.
    // Thus, it has two incoming dependents:
    // 1) the tuple (P, Q) and 2) the function R. I store tuples in the variable incomingTuples.
    // Outgoing dependents consist of functions and constants that a declaration mentions.
    // For the axiom above, there are 2 outgoing dependents: P and R.
    // (notice that Q is excluded because the axiom itself does not mention it.)
    // Now, a declaration A depends on B, if the outgoing dependents of A match
    // with some incoming dependent of B (see method depends).

    public readonly Declaration declaration; // a node could either be a function or an axiom.
    protected HashSet<Declaration> Outgoings { get; }
    
    protected SetOfSets<Declaration> incomings;
    protected HashSet<Type> types;

    private static bool ExcludeDep(Declaration d)
    {
      return d.Attributes != null && QKeyValue.FindBoolAttribute(d.Attributes, "exclude_dep");
    }

    protected void AddIncoming(Declaration incoming)
    {
      if (declaration == incoming || !ExcludeDep(incoming))
        incomings.Add(incoming);
    }

    protected void AddOutgoing(Declaration outgoing)
    {
      if (!ExcludeDep(outgoing)) {
        Outgoings.Add(outgoing);
      }
    }
    
    public DeclarationDependencies(Declaration declaration)
    {
      this.declaration = declaration;
      incomings = new ();
      Outgoings = new HashSet<Declaration>();
      types = new HashSet<Type>();
    }
    
    /*
     * returns true if there is an edge from a to b
     */
    public static bool Depends(DeclarationDependencies from, DeclarationDependencies to)
    {
      return to.incomings.ContainsSubSetOf(from.Outgoings);
    }
  }
}