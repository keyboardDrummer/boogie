using System.ComponentModel.Design.Serialization;
using System.Diagnostics;

namespace Microsoft.Boogie; 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

[AsyncMethodBuilder(typeof(DynamicStackBuilder))]
public class DynamicStack {

  public static DynamicStack<TResult> FromResult<TResult>(TResult result) {
    return new DynamicStack<TResult> {
      Result = result
    };
  }
  public void Run() {
    DynamicStackBuilder.Builder.Value!.Run();
  }

  public DynamicStackBuilder GetAwaiter() {
    return DynamicStackBuilder.Builder.Value;
  }
}

public class DynamicStackBuilder : INotifyCompletion {
  public static readonly ThreadLocal<DynamicStackBuilder> Builder = new(() => new DynamicStackBuilder());
  private static readonly DynamicStack TheOne = new();

  public static DynamicStackBuilder Create() {
    return Builder.Value;
  }

  private readonly Stack<IAsyncStateMachine> todos = new();

  public void Run() {
    while (todos.Any()) {
      var machine = todos.Pop();
      machine.MoveNext();
    }
  }

  public void Start<TStateMachine>(ref TStateMachine stateMachine)
    where TStateMachine : IAsyncStateMachine {
    // Called on await, push recursive call
    todos.Push(stateMachine);
  }

  public void SetException(Exception exception) {
    throw exception;
  }

  public void SetResult() {
  }

  public void SetStateMachine(IAsyncStateMachine stateMachine) {
  }

  public void AwaitOnCompleted<TAwaiter, TStateMachine>(
    ref TAwaiter awaiter, ref TStateMachine stateMachine)
    where TAwaiter : INotifyCompletion
    where TStateMachine : IAsyncStateMachine {
    // Place recursive call on top of continuation
    var recursiveCall = todos.Pop();
    todos.Push(stateMachine);
    todos.Push(recursiveCall);
  }

  public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
    ref TAwaiter awaiter, ref TStateMachine stateMachine)
    where TAwaiter : ICriticalNotifyCompletion
    where TStateMachine : IAsyncStateMachine {
    // Place recursive call on top of continuation
    var recursiveCall = todos.Pop();
    todos.Push(stateMachine);
    todos.Push(recursiveCall);
  }

  public DynamicStack Task => TheOne;

  public bool IsCompleted => false;

  public void GetResult() {
  }

  public void OnCompleted(System.Action continuation) {
    throw new NotImplementedException();
  }
}

public static class DynamicStackExtensions {
  public static async DynamicStack<IList<T>> ToDynamicStackList<T>(this IEnumerable<DynamicStack<T>> items) {
    var result = new List<T>();
    foreach (var item in items) {
      result.Add(await item);
    }
    return result;
  }
}

/// <summary>
/// Equivalent to Task<T>
/// </summary>
[AsyncMethodBuilder(typeof(DynamicStackBuilder<>))]
public class DynamicStack<TResult> : INotifyCompletion {
  private TResult result;
  private bool completed = false;

  internal DynamicStack() {
  }

  public TResult Result {
    get {
      if (!IsCompleted) {
        Run();
      }
      return result;
    }
    internal set {
      result = value;
      completed = true;
    }
  }

  public void Run() {
    DynamicStackBuilder<TResult>.Builder.Value!.Run();
  }

  public DynamicStack<TResult> GetAwaiter() {
    return this;
  }
  
  public void OnCompleted(Action continuation) {
    // Never called because AwaitOnCompleted doesn't call it.
  }

  public bool IsCompleted => completed;

  public TResult GetResult() {
    return result;
  }
}

/// <summary>
/// Combines both the builder and the awaiter pattern, described here:
/// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-7.0/task-types
/// </summary>
public class DynamicStackBuilder<TResult> {
  public static readonly ThreadLocal<DynamicStackBuilder<TResult>> Builder = new(() => new DynamicStackBuilder<TResult>());
  private ConditionalWeakTable<IAsyncStateMachine, DynamicStack<TResult>> results = new ();
  private DynamicStack<TResult> dynamicStack;

  public static DynamicStackBuilder<TResult> Create() {
    return Builder.Value;
  }

  private readonly Stack<IAsyncStateMachine> todos = new();

  public void Run() {
    while (todos.Any()) {
      var machine = todos.Pop();
      results.TryGetValue(machine, out dynamicStack);
      machine.MoveNext();
    }
  }
  
  [DebuggerStepThrough]
  public void Start<TStateMachine>(ref TStateMachine stateMachine)
    where TStateMachine : IAsyncStateMachine {
    // Push recursive call
    dynamicStack = new DynamicStack<TResult>();
    results.Add(stateMachine, dynamicStack);
    todos.Push(stateMachine);
  }

  public void SetStateMachine(IAsyncStateMachine stateMachine) {
  }

  public void SetException(Exception exception) {
    throw exception;
  }

  public DynamicStack<TResult> Task => dynamicStack;

  public void SetResult(TResult result) {
    dynamicStack.Result = result;
    // dynamicStack = new DynamicStack<TResult>() {
    //   Result = result
    // };
    //dynamicStack.Result = result;
  }
  
  public void SetResult(DynamicStack<TResult> result) {
    dynamicStack = new DynamicStack<TResult>() {
      Result = result.Result
    };
    // dynamicStack = result;
    // TODO maybe not correct?
  }

  public void AwaitOnCompleted<TAwaiter, TStateMachine>(
    ref TAwaiter awaiter, ref TStateMachine stateMachine)
    where TAwaiter : INotifyCompletion
    where TStateMachine : IAsyncStateMachine {
    // Place recursive call on top of continuation
    if (todos.Any()) {
      var recursiveCall = todos.Pop();
      todos.Push(stateMachine);
      todos.Push(recursiveCall);
    } else {
      todos.Push(stateMachine);
    }
  }

  public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
    ref TAwaiter awaiter, ref TStateMachine stateMachine)
    where TAwaiter : ICriticalNotifyCompletion
    where TStateMachine : IAsyncStateMachine {
    // Place recursive call on top of continuation
    var recursiveCall = todos.Pop();
    todos.Push(stateMachine);
    todos.Push(recursiveCall);
  }
}