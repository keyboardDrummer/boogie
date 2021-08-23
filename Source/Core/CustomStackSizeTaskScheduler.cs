using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core
{
  public class CustomStackSizeTaskScheduler : TaskScheduler
  {
    private readonly int maxStackSize;
    private readonly int maximumConcurrencyLevel;
    private readonly ConcurrentQueue<Task> queuedTasks = new();
    private readonly ConcurrentStack<Thread> freeThreads = new();
    private int uncreatedThreadCount = 0;

    public CustomStackSizeTaskScheduler(int maxStackSize, int maximumConcurrencyLevel)
    {
      this.maxStackSize = maxStackSize;
      this.maximumConcurrencyLevel = maximumConcurrencyLevel;
      uncreatedThreadCount = maximumConcurrencyLevel;
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
      return queuedTasks;
    }

    protected override void QueueTask(Task task)
    {
      if (queuedTasks.IsEmpty) {
        Thread thread = null;
        if (freeThreads.IsEmpty) {
          var afterDecCount = Interlocked.Decrement(ref uncreatedThreadCount);
          if (afterDecCount >= 0) {
            thread = new Thread(TaskMain, maxStackSize);
          } 
        } else {
          freeThreads.TryPop(out thread);
        }

        if (thread != null) {
          thread.Start(task);
        } else {
          queuedTasks.Enqueue(task);
        }
      }
    }

    private void TaskMain(object data)
    {
      var task = (Task) data;
      while (true) {
        TryExecuteTask(task);
        if (!queuedTasks.TryDequeue(out task))
        {
          freeThreads.Push(Thread.CurrentThread);
          break;
        }
      }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      if (taskWasPreviouslyQueued) {
        throw new NotImplementedException();
      }

      return TryExecuteTask(task);
    }
  }
}