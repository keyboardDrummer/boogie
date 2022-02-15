using System;
using System.Collections.Generic;
using System.IO;
using VC;

namespace Microsoft.Boogie;

public sealed class VerificationResult
{
  
  public readonly string RequestId;
  public readonly string Checksum;
  public readonly string DependeciesChecksum;
  public readonly string ImplementationName;
  public readonly IToken ImplementationToken;
  public readonly string ProgramId;
  public readonly string MessageIfVerifies;
  public readonly StringWriter Output;
  public bool WasCached { get; set; }

  public DateTime Start { get; set; }
  public DateTime End { get; set; }

  public int ResourceCount { get; set; }

  public int ProofObligationCount
  {
    get { return ProofObligationCountAfter - ProofObligationCountBefore; }
  }

  public int ProofObligationCountBefore { get; set; }
  public int ProofObligationCountAfter { get; set; }

  public ConditionGeneration.Outcome Outcome { get; set; }
  public List<Counterexample> Errors = new();

  public ISet<byte[]> AssertionChecksums { get; }

  public VerificationResult(string requestId, Implementation implementation, StringWriter output, string programId = null)
  {
    Checksum = implementation.Checksum;
    DependeciesChecksum = implementation.DependencyChecksum;
    RequestId = requestId;
    Output = output;
    ImplementationName = implementation.Name;
    ImplementationToken = implementation.tok;
    ProgramId = programId;
    AssertionChecksums = implementation.AssertionChecksums;
    MessageIfVerifies = implementation.FindStringAttribute("msg_if_verifies");
  }

  public StringWriter Process(OutputPrinter printer,
    ExecutionEngineOptions options,
    PipelineStatistics stats, ErrorReporterDelegate er,
    Implementation implementation)
  {
    ExecutionEngine.ProcessOutcome(printer, options, Outcome, Errors, TimeIndication(options), stats,
      Output, implementation.TimeLimit, er, ImplementationName, ImplementationToken,
      RequestId, MessageIfVerifies, WasCached);

    ExecutionEngine.ProcessErrors(printer, options, Errors, Outcome, Output, er, implementation);

    options.XmlSink?.WriteEndMethod(Outcome.ToString().ToLowerInvariant(),
      End, End - Start,
      ResourceCount);

    if (Outcome == ConditionGeneration.Outcome.Errors || options.Trace)
    {
      Console.Out.Flush();
    }

    return Output;
  }

  private string TimeIndication(ExecutionEngineOptions options)
  {
    var result = "";
    if (options.Trace)
    {
      result = string.Format("  [{0:F3} s, solver resource count: {1}, {2} proof obligation{3}]  ",
        (End - Start).TotalSeconds,
        ResourceCount,
        ProofObligationCount,
        ProofObligationCount == 1 ? "" : "s");
    }
    else if (options.TraceProofObligations)
    {
      result = string.Format("  [{0} proof obligation{1}]  ", ProofObligationCount,
        ProofObligationCount == 1 ? "" : "s");
    }

    return result;
  }
}