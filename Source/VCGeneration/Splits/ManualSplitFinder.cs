#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Boogie;
using VC;

namespace VCGeneration;


public static class ManualSplitFinder {
  
  public static IEnumerable<ManualSplit> GetParts(VCGenOptions options, ImplementationRun run,
    Func<IImplementationPartOrigin, IList<Block>, ManualSplit> createPart) {
    
    var focussedParts = FocusAttributeHandler.GetParts(options, run, createPart);
    var result = focussedParts.SelectMany(focussedPart =>
    {
      var (isolatedJumps, withoutIsolatedJumps) =
        IsolateAttributeOnJumpsHandler.GetParts(options, focussedPart, createPart);
      return new[] { withoutIsolatedJumps }.Concat(isolatedJumps).SelectMany(isolatedJumpSplit =>
      {
        var (isolatedAssertions, withoutIsolatedAssertions) =
          IsolateAttributeOnAssertsHandler.GetParts(options, isolatedJumpSplit, createPart);

        var splitParts = SplitAttributeHandler.GetParts(withoutIsolatedAssertions);
        return isolatedAssertions.Concat(splitParts);
      });
    }).Where(s => s.Asserts.Any()).ToList();

    if (result.Any())
    {
      return result;
    }

    return focussedParts.Take(1);
  }
}

public interface IImplementationPartOrigin : IToken {
  string ShortName { get; }
}