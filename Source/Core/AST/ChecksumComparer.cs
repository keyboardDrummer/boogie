using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Boogie;

public sealed class ChecksumComparer : IEqualityComparer<byte[]>
{
  static IEqualityComparer<byte[]> defaultComparer;

  public static IEqualityComparer<byte[]> Default
  {
    get
    {
      if (defaultComparer == null)
      {
        defaultComparer = new ChecksumComparer();
      }

      return defaultComparer;
    }
  }

  public bool Equals(byte[] x, byte[] y)
  {
    if (x == null || y == null)
    {
      return x == y;
    }
    else
    {
      return x.SequenceEqual(y);
    }
  }

  public int GetHashCode(byte[] checksum)
  {
    if (checksum == null)
    {
      throw new ArgumentNullException("checksum");
    }
    else
    {
      var result = 17;
      for (int i = 0; i < checksum.Length; i++)
      {
        result = result * 23 + checksum[i];
      }

      return result;
    }
  }
}