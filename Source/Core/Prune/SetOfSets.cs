using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Boogie
{
  public class SetOfSets<T>
  {
    class TreeNode
    {
      public Dictionary<T, TreeNode> Map { get; } = new();
    }

    private readonly TreeNode root = new();
    private readonly Dictionary<T, int> order = new();

    private int GetOrder(T declaration)
    {
      return order.GetOrCreate(declaration, () => order.Count);
    }
    
    public bool ContainsSubSetOf(IEnumerable<T> outgoing)
    {
      var node = root;
      foreach (var declaration in outgoing.OrderBy(GetOrder)) {
        if (!node.Map.TryGetValue(declaration, out node)) {
          return false;
        }
      }
      return true;
    }
    
    public void Add(T singleton)
    {
      root.Map.GetOrCreate(singleton, () => new TreeNode());
    }
    
    public void Add(IEnumerable<T> set)
    {
      var node = root;
      foreach (var declaration in set.OrderBy(GetOrder)) {
        node = node.Map.GetOrCreate(declaration, () => new TreeNode());
      }
    }
  }
}