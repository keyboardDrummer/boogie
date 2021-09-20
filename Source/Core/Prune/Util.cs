using System;
using System.Collections.Generic;

namespace Microsoft.Boogie
{
  static class Util
  {
    public static V GetOrCreate<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> create)
    {
      if (dictionary.TryGetValue(key, out V result)) {
        return result;
      }

      result = create();
      dictionary[key] = result;
      return result;
    }
  }
}