using System.Collections.Generic;
using System.IO;

namespace Microsoft.Boogie;

public class ConcurrentToSequentialWriteManager
{
  public TextWriter Writer { get; }
  private readonly Queue<SubWriter> writers = new();

  public ConcurrentToSequentialWriteManager(TextWriter writer) {
    Writer = writer;
  }

  private readonly object myLock = new();

  private void Disposed() {
    lock (myLock) {
      while (writers.Count > 0 && writers.Peek().Disposed) {
        var disposedWriter = writers.Dequeue();
        Writer.Write(disposedWriter.SetTargetAndGetBuffer(null));
      }
      if (writers.Count > 0) {
        Writer.Write(writers.Peek().SetTargetAndGetBuffer(Writer));
      }
    }
  }

  public TextWriter AppendWriter() {
    lock (myLock) {
      var target = writers.Count == 0 ? Writer : null;
      var result = new SubWriter(this, target);
      writers.Enqueue(result);
      return result;
    }
  }

  class SubWriter : WriterWrapper {
    private readonly ConcurrentToSequentialWriteManager collector;
    private bool buffering;
    public bool Disposed { get; private set; }

    public SubWriter(ConcurrentToSequentialWriteManager collector, TextWriter target) : base(target ?? new StringWriter()) {
      this.collector = collector;
      buffering = target == null;
    }

    public string SetTargetAndGetBuffer(TextWriter newTarget) {
      var result = buffering ? ((StringWriter)target).ToString() : "";
      buffering = false;
      target = newTarget;
      return result;
    }

    protected override void Dispose(bool disposing) {
      Disposed = true;
      collector.Disposed();
    }
  }
}