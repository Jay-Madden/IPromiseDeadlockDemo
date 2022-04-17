// See https://aka.ms/new-console-template for more information

using RLC.Promises;

public static class Program
{
  public static async Task Main()
  {
    const int maxThreads = 2;
    
    // Set the threadpool max to 2 so that we can minimally show the deadlocks produced
    ThreadPool.SetMinThreads(1, 1000);
    ThreadPool.SetMaxThreads(maxThreads, 1000);

    Console.WriteLine($"Starting Thread count: {ThreadPool.ThreadCount}");
    ThreadPool.GetAvailableThreads(out var i, out var o);
    Console.WriteLine($"Starting available thread count: {i}");
    Console.WriteLine($"Maximum Thread count: {maxThreads}");
    Console.WriteLine($"Starting Thread ID: {Environment.CurrentManagedThreadId}");
    
    Console.WriteLine("---------------  Start normal async/await demonstration  ----------------- \n");

    // These will both start and resume from the continuation on the same thread due to that thread being 
    // available to pick up the continuation
    await FooAsync(1);
    await FooAsync(2);
    await FooAsync(3);
    await FooAsync(4);
    
    Console.WriteLine("---------------  End normal async demonstration  ----------------- \n");
    
    Console.WriteLine("---------------  Start elided async/await demonstration  ----------------- \n");

    // These will be fired off into the abyss, but all will be fired off onto the same thread due to
    // The threadpool only having one free thread
    // If the threadpool max is increased here some of the FooAsyncs will continue on other threads due
    // to any number of unknown conditions that the TPL has decided. its a blackbox on WHY it would do that
    FooAsync(1);
    FooAsync(2);
    FooAsync(3);
    FooAsync(4);
    
    await Task.Delay(1500);
    Console.WriteLine("---------------  End elided async demonstration  ----------------- \n");
    
    Console.WriteLine("---------------  Start IPromise demonstration  ----------------- \n");
    
    // This will deadlock, due to the usage of both promises calling .GetResult on the delay and
    // Therefore blocking the thread that could be used as a continuation thread
    Promise<int>.Resolve(1).Then(FooAsync);
    Promise<int>.Resolve(1).Then(FooAsync);
    
    await Task.Delay(5000);
    Console.WriteLine("---------------  End IPromise demonstration  ----------------- \n");
    
    // Uncomment this to see the minimal viable reproduction of what the IPromise library is doing under the 
    // Hood to cause these deadlocks
    /*
    Console.WriteLine("---------------  Start simple reproduction demonstration  ----------------- \n");
    
    // This will deadlock, due to the usage of both GetResults calling .GetResult on the delay and
    // Therefore blocking the thread that could be used as a continuation thread
    Task.Delay(500).GetAwaiter().GetResult();
    Task.Delay(500).GetAwaiter().GetResult();
    
    await Task.Delay(5000);
    Console.WriteLine("---------------  End simple reproduction demonstration  ----------------- \n");
    */

  }

  public static async Task<int> FooAsync(int i)
  {
    Console.WriteLine($"Entering ID: {i} with threadID: {Environment.CurrentManagedThreadId}");
    await Task.Delay(1000);
    Console.WriteLine($"ID: {i} done with threadID: {Environment.CurrentManagedThreadId}");
    return 3;
  }
}
