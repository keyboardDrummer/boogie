using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Boogie
{
  public class SetOfSets<T>
  {
    private readonly HashSet<HashSet<T>> sets = new();

    public bool ContainsSubSetOf(HashSet<T> outgoing)
    {
      return sets.Any(s => s.IsSubsetOf(outgoing));
    }
    
    public void Add(T singleton)
    {
      sets.Add(new HashSet<T>() { singleton });
    }
    
    public void Add(IEnumerable<T> set)
    {
      sets.Add(set.ToHashSet());
    }
  }
}