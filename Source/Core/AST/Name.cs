using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Boogie;

namespace Microsoft.Dafny;

public class Name {
  public IToken Token { get; }
  public string Value { get; set; }

  public Name Prepend(string prefix) {
    return new Name(Token, prefix + Value);
  }

  public Name Append(string suffix) {
    return new Name(Token, Value + suffix);
  }

  public Name Update(Func<string, string> update) {
    return new Name(Token, update(Value));
  }

  public Name(IToken token) : this(token, token.val)
  {
  }

  public Name(string value) : this(Boogie.Token.NoToken, value)
  {
  }
  
  public Name(IToken token, string value)
  {
    Token = token;
    Value = value;
  }

  public override string ToString() => Value;
}