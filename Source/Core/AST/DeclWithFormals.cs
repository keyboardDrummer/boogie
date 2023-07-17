using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.Dafny;

namespace Microsoft.Boogie;

public abstract class DeclWithFormals : NamedDeclaration
{
  // If this declaration is a monomorphized instance, OriginalDeclWithFormals
  // points to the original from which this declaration was instantiated.
  public DeclWithFormals OriginalDeclWithFormals;

  public List<TypeVariable> /*!*/ TypeParameters;

  public List<Variable> /*!*/ InParams { get; set; }

  public List<Variable> /*!*/ OutParams { get; set; }

  [ContractInvariantMethod]
  void ObjectInvariant()
  {
    Contract.Invariant(TypeParameters != null);
    Contract.Invariant(this.InParams != null);
    Contract.Invariant(this.OutParams != null);
  }

  protected DeclWithFormals(IToken tok, Name name, List<TypeVariable> typeParams,
    List<Variable> inParams, List<Variable> outParams)
    : base(tok, name)
  {
    Contract.Requires(inParams != null);
    Contract.Requires(outParams != null);
    Contract.Requires(typeParams != null);
    Contract.Requires(name != null);
    Contract.Requires(tok != null);
    this.TypeParameters = typeParams;
    this.InParams = inParams;
    this.OutParams = outParams;
  }
  
  protected DeclWithFormals(IToken tok, string name, List<TypeVariable> typeParams,
    List<Variable> inParams, List<Variable> outParams)
    : this(tok, new Name(name), typeParams, inParams, outParams)
  {
  }

  protected DeclWithFormals(DeclWithFormals that)
    : base(that.tok, cce.NonNull(that.Name))
  {
    Contract.Requires(that != null);
    this.TypeParameters = that.TypeParameters;
    this.InParams = cce.NonNull(that.InParams);
    this.OutParams = cce.NonNull(that.OutParams);
  }

  public byte[] MD5Checksum_;

  public byte[] MD5Checksum
  {
    get
    {
      if (MD5Checksum_ == null)
      {
        var c = Checksum;
        if (c != null)
        {
          MD5Checksum_ = System.Security.Cryptography.MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(c));
        }
      }

      return MD5Checksum_;
    }
  }

  public byte[] MD5DependencyChecksum_;

  public byte[] MD5DependencyChecksum
  {
    get
    {
      Contract.Requires(DependenciesCollected);

      if (MD5DependencyChecksum_ == null && MD5Checksum != null)
      {
        var c = MD5Checksum;
        var transFuncDeps = new HashSet<Function>();
        if (procedureDependencies != null)
        {
          foreach (var p in procedureDependencies)
          {
            if (p.FunctionDependencies != null)
            {
              foreach (var f in p.FunctionDependencies)
              {
                transFuncDeps.Add(f);
              }
            }

            var pc = p.MD5Checksum;
            if (pc == null)
            {
              return null;
            }

            c = ChecksumHelper.CombineChecksums(c, pc, true);
          }
        }

        if (FunctionDependencies != null)
        {
          foreach (var f in FunctionDependencies)
          {
            transFuncDeps.Add(f);
          }
        }

        var q = new Queue<Function>(transFuncDeps);
        while (q.Any())
        {
          var f = q.Dequeue();
          var fc = f.MD5Checksum;
          if (fc == null)
          {
            return null;
          }

          c = ChecksumHelper.CombineChecksums(c, fc, true);
          if (f.FunctionDependencies != null)
          {
            foreach (var d in f.FunctionDependencies)
            {
              if (!transFuncDeps.Contains(d))
              {
                transFuncDeps.Add(d);
                q.Enqueue(d);
              }
            }
          }
        }

        MD5DependencyChecksum_ = c;
      }

      return MD5DependencyChecksum_;
    }
  }

  public string Checksum
  {
    get { return (this as ICarriesAttributes).FindStringAttribute("checksum"); }
  }

  string dependencyChecksum;

  public string DependencyChecksum
  {
    get
    {
      if (dependencyChecksum == null && DependenciesCollected && MD5DependencyChecksum != null)
      {
        dependencyChecksum = BitConverter.ToString(MD5DependencyChecksum);
      }

      return dependencyChecksum;
    }
  }

  public bool DependenciesCollected { get; set; }

  ISet<Procedure> procedureDependencies;

  public ISet<Procedure> ProcedureDependencies
  {
    get { return procedureDependencies; }
  }

  public void AddProcedureDependency(Procedure procedure)
  {
    Contract.Requires(procedure != null);

    if (procedureDependencies == null)
    {
      procedureDependencies = new HashSet<Procedure>();
    }

    procedureDependencies.Add(procedure);
  }

  ISet<Function> functionDependencies;

  public ISet<Function> FunctionDependencies
  {
    get { return functionDependencies; }
  }

  public void AddFunctionDependency(Function function)
  {
    Contract.Requires(function != null);

    if (functionDependencies == null)
    {
      functionDependencies = new HashSet<Function>();
    }

    functionDependencies.Add(function);
  }

  public bool SignatureEquals(CoreOptions options, DeclWithFormals other)
  {
    Contract.Requires(other != null);

    string sig = null;
    string otherSig = null;
    using (var strWr = new System.IO.StringWriter())
    using (var tokTxtWr = new TokenTextWriter("<no file>", strWr, false, false, options))
    {
      EmitSignature(tokTxtWr, this is Function);
      sig = strWr.ToString();
    }

    using (var otherStrWr = new System.IO.StringWriter())
    using (var otherTokTxtWr = new TokenTextWriter("<no file>", otherStrWr, false, false, options))
    {
      EmitSignature(otherTokTxtWr, other is Function);
      otherSig = otherStrWr.ToString();
    }

    return sig == otherSig;
  }

  protected void EmitSignature(TokenTextWriter stream, bool shortRet)
  {
    Contract.Requires(stream != null);
    Type.EmitOptionalTypeParams(stream, TypeParameters);
    stream.Write("(");
    stream.push();
    InParams.Emit(stream, true);
    stream.Write(")");
    stream.sep();

    if (shortRet)
    {
      Contract.Assert(OutParams.Count == 1);
      stream.Write(" : ");
      cce.NonNull(OutParams[0]).TypedIdent.Type.Emit(stream);
    }
    else if (OutParams.Count > 0)
    {
      stream.Write(" returns (");
      OutParams.Emit(stream, true);
      stream.Write(")");
    }

    stream.pop();
  }

  // Register all type parameters at the resolution context
  protected void RegisterTypeParameters(ResolutionContext rc)
  {
    Contract.Requires(rc != null);
    foreach (TypeVariable /*!*/ v in TypeParameters)
    {
      Contract.Assert(v != null);
      rc.AddTypeBinder(v);
    }
  }

  protected void SortTypeParams()
  {
    List<Type> /*!*/
      allTypes = InParams.Select(Item => Item.TypedIdent.Type).ToList();
    Contract.Assert(allTypes != null);
    allTypes.AddRange(OutParams.Select(Item => Item.TypedIdent.Type));
    TypeParameters = Type.SortTypeParams(TypeParameters, allTypes, null);
  }

  /// <summary>
  /// Adds the given formals to the current variable context, and then resolves
  /// the types of those formals.  Does NOT resolve the where clauses of the
  /// formals.
  /// Relies on the caller to first create, and later tear down, that variable
  /// context.
  /// </summary>
  /// <param name="rc"></param>
  protected void RegisterFormals(List<Variable> formals, ResolutionContext rc)
  {
    Contract.Requires(rc != null);
    Contract.Requires(formals != null);
    foreach (Formal /*!*/ f in formals)
    {
      Contract.Assert(f != null);
      if (f.Name != TypedIdent.NoName)
      {
        rc.AddVariable(f);
      }

      f.Resolve(rc);
    }
  }

  /// <summary>
  /// Resolves the where clauses (and attributes) of the formals.
  /// </summary>
  /// <param name="rc"></param>
  protected void ResolveFormals(List<Variable> formals, ResolutionContext rc)
  {
    Contract.Requires(rc != null);
    Contract.Requires(formals != null);
    foreach (Formal /*!*/ f in formals)
    {
      Contract.Assert(f != null);
      f.ResolveWhere(rc);
    }
  }

  public override void Typecheck(TypecheckingContext tc)
  {
    //Contract.Requires(tc != null);
    (this as ICarriesAttributes).TypecheckAttributes(tc);
    foreach (Formal /*!*/ p in InParams)
    {
      Contract.Assert(p != null);
      p.Typecheck(tc);
    }

    foreach (Formal /*!*/ p in OutParams)
    {
      Contract.Assert(p != null);
      p.Typecheck(tc);
    }
  }
}