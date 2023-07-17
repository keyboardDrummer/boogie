using System.Diagnostics.Contracts;
using Microsoft.Dafny;

namespace Microsoft.Boogie;

public abstract class NamedDeclaration : Declaration
{
  public Name NameNode { get; }

  protected NamedDeclaration(IToken tok, Name nameNode)
    : base(tok)
  {
    NameNode = nameNode;
  }
  
  protected NamedDeclaration(IToken tok, string name)
    : base(tok)
  {
    NameNode = new Name(name);
  }
  
  public string Name => NameNode.Value;

  // A name for a declaration that may be more verbose than, and use
  // characters not allowed by, standard Boogie identifiers. This can
  // be useful for encoding the original source names of languages
  // translated to Boogie.
  public string VerboseName =>
    QKeyValue.FindStringAttribute(Attributes, "verboseName") ?? Name;

  public uint GetTimeLimit(CoreOptions options)
  {
    uint tl = options.TimeLimit;
    CheckUIntAttribute("timeLimit", ref tl);
    if (tl < 0) {
      tl = options.TimeLimit;
    }
    return tl;
  }

  public uint GetResourceLimit(CoreOptions options)
  {
    uint rl = options.ResourceLimit;
    CheckUIntAttribute("rlimit", ref rl);
    if (rl < 0) {
      rl = options.ResourceLimit;
    }
    return rl;
  }

  public int? RandomSeed
  {
    get
    {
      int rs = 0;
      if (CheckIntAttribute("random_seed", ref rs))
      {
        return rs;
      }
      return null;
    }
  }

  [Pure]
  public override string ToString()
  {
    return cce.NonNull(Name);
  }
    
  public virtual bool MayRename => true;
}