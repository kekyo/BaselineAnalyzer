# BaselineAnalyzer

Analyzer that is kind to C# beginners.

![BaselineAnalyzer](Images/BaselineAnalyzer.100.png)

# Status

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

|Target|Pakcage|
|:----|:----|
|Any|[![NuGet BaselineAnalyzer](https://img.shields.io/nuget/v/BaselineAnalyzer.svg?style=flat)](https://www.nuget.org/packages/BaselineAnalyzer)|


----

[![Japanese language](Images/Japanese.256.png)](https://github.com/kekyo/BaselineAnalyzer/blob/main/README.ja.md)

## What is this?

This is an analyzer for .NET C# beginners that automatically detects trivial implementation errors.
It is easy to use, just install the [BaselineAnalyzer NuGet package](https://www.nuget.org/packages/BaselineAnalyzer) in your project.

Install the `BaselineAnalyzer` package by editing the VisualStudio, Rider, or csproj file directly.
This will allow the IDE or compiler to detect and report problems in your source code.

There are many different analyzers released, but this package will focus its detection on problems that C# beginners are particularly prone to.
There is no need to configure the package, and it is easy to use.


----

## Detectable source code problems

BaselineAnalyzer outputs an identification code and message like `BLA0001` when the IDE or compiler detects a problem.
The message is output in the message window in the case of the IDE, or as a compiler message if the problem was discovered during compilation.

Below is a list of issues corresponding to the identifiers.

|Identifier|Summary|
|:----|:----|
|`BLA0001`|The catch block does not contain a throw statement|
|`BLA0002`|Avoid using 'throw ex;' in catch blocks, use 'throw;' instead to preserve stack trace|
|`BLA0011`|Asynchronous method 'Foo' must include the 'Async' suffix|
|`BLA0012`|If you implement a synchronous method, you append `Async` to the end of the method name|
|`BLA0021`|Using 'Wait/Result' in asynchronous methods may cause deadlock|

### BLA0001: The catch block does not contain a throw statement

This occurs when you write code that exits the `catch` block without throwing an exception when an exception is `caught`:

```csharp
public void Foo()
{
    try
    {
        // Do something...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");

        // Wasn't threw the exception...
    }
}
```

This kind of code can ignore dangerous situations that the exception contains.
Here is an example of how to fix the problem:

```csharp
public void Foo()
{
    try
    {
        // Do something...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");
        
        // Re-throw the exception
        throw;
    }
}
```

As shown above, there is no problem if the exception is thrown within the `catch` block.
Alternatively, you could wrap the exception in another exception and throw it:

```csharp
public void Foo()
{
    try
    {
        // do something...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");
        
        // Wrap the exception and throw it
        throw new FormatException("Invalid form", ex);
    }
}
```

#### Why?

Exceptions are there to tell us that something exceptional has happened,
that is, an abnormal situation that would not normally occur has occurred.

Not throwing an exception in a `catch` block could lead to you "ignoring" an abnormal situation,
and you could fail to notice a bigger problem.
It is better not to suppress exceptions in order to detect such situations at an early stage.

### BLA0002: Avoid using 'throw ex;' in catch blocks, use 'throw;' instead to preserve stack trace

This occurs when you specify the original exception object,
like `throw ex`, instead of using the `throw` clause when rethrowing an exception:

```csharp
public void Foo()
{
    try
    {
        // Do something...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");

        // Throw with original exception object...
        throw ex;
    }
}
```

At first glance, this looks fine.
But when you throw like this,
the stack trace information that shows the source of the exception is lost,
and the exception is treated as if it occurred "here".

As a result, an incorrect stack trace is stored in the log when you log the exception,
and the debugger cannot track the correct location where the exception occurred.

If you just want to do something special when an exception occurs,
you can "rethrow" the exception.
This is done using the special `throw` clause:

```csharp
public void Foo()
{
    try
    {
        // Do something...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");

        // "rethrow" the original exception object
        throw;
    }
}
```

### BLA0011: Asynchronous method 'Foo' must include the 'Async' suffix

When implementing an asynchronous method,
it is customary to add `Async` to the end of the method name:

```csharp
// Despite being an asynchronous method, `Async` is not added to the end of the method name
public Task Foo()
{
    // ...
}
```

This can be fixed by adding `Async` to the end of the method name:

```csharp
// Async method, so the method name ends with `Async`
public Task FooAsync()
{
    // ...
}
```

Whether a method is asynchronous or not is detected by the following conditions:

* Methods that return a type of `Task` or `ValueTask` and its generic version.
  * Methods that return `Task` or `ValueTask` and its generic version type,
    except for the `static Main` method in the case of `Task`.
* Methods that return the type of `IAsyncEnumerable<T>`.
  * Excepts for extension methods (Assuming asynchronous operators such as `System.Linq.Async` package.)

#### Why is this?

There are a number of possible reasons,
but one is that it is a convention that has been in place since the introduction of `async-await` in C#.
When `async-await` was first introduced,
overloads could not be used to add asynchronous versions of existing synchronous methods,
so this was used as a naming convention to make them distinguishable.

For example, the `Stream` class has a `Read` method, but the asynchronous version of this method had the advantage of requiring a different method name and making it clear that it was an asynchronous method:

```csharp
// .NET Standard Stream class
public class Stream
{
    // ...
    
    // Read data from stream (synchronous version)
    public abstract int Read(byte[] buffer, int offset, int count);

    // Read data from the stream (added asynchronous version)
    public abstract Task<int> ReadAsync(byte[] buffer, int offset, int count);
}
```

As shown above, even if you try to add a method that returns `Task<int>`,
you cannot do so due to the rules of overloading
(you cannot define multiple overloads with the same argument signature).

### BLA0012: Synchronous method 'FooAsync' must not include the 'Async' suffix

When implementing a synchronous method, adding `Async` to the end of the method name can cause confusion:

```csharp
// Despite being a synchronous method, the `Async` suffix is added to the end of the method name
public int FooAsync()
{
    // ...
}
```

You can fix this by removing `Async` from the end of the method name:

```csharp
// As this is a synchronous method, `Async` is not appended to the end of the method name
public int Foo()
{
    // ...
}
```

#### Why is this?

This is the opposite pattern to `BLA0011`.
For more information, please refer to `BLA0011`.

### BLA0021: Using 'Wait/Result' in asynchronous methods may cause deadlock

Within the implementation of the asynchronous method,
there is a call to the `Task.Wait()` method and a reference to the `Task.Result` property:

```csharp
public string Foo(HttpClient httpClient)
{
    // Call the asynchronous method and wait synchronously for the `Result`
    var result = httpClient.GetStringAsync("https://example.com/").Result;
}
```

Be sure to use the `async-await` clause to wait "asynchronously" for the result of an asynchronous method:

```csharp
public async Task<string> FooAsync(HttpClient httpClient)
{
    // Wait asynchronously using the `await` clause, without using `Result`
    var result = await httpClient.GetStringAsync("https://example.com/");
}
```

### Why?

When you use `Task.Wait()` or `Task.Result`,
the thread is forcibly paused there. As a result:

* In the case of a GUI application,
  the entire application will be suspended until the asynchronous method finishes,
  as it is usually only using the main thread to operate.
* In the case of a server-side application,
  it will no longer be possible to reuse threads,
  leading to a deterioration in concurrent execution performance.
* If the code is complicated, it may deadlock and not move until the application is forcibly terminated.

You may think that none of the above problems apply to your code.
However, please look again at the correction method.
It is not that difficult to write correct code.

If you continue to implement it ignoring the recommended implementation pattern for asynchronous methods,
be prepared for the fact that you will have to go through a lot of trouble to fix a large amount of code later on.


----

## Disabling Messages

In the same way that our world is not “Absolutely perfect”,
if you are confident that these issues are not a problem,
you can disable the messages using [`#pragma warning`](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings#use-a-preprocessor-directive).

However, if you don't know what the problem is,
you should understand the problematic points well before disabling it,
and fix the problematic code.

If you disable it without understanding the problematic points,
it probably won't produce good results,
or the problem will come back to haunt you in the future.

I did warn you, right? :)


## Limitations

This analyzer does not analyze very detailed implementation details.
Therefore, it is possible that it will generate warnings for implementations that are not actually considered to be problematic,
and vice versa.


## TODO

* Will add a few rules.


----

## License

Apache-v2

## History

* 0.2.0:
  * Changed referring to build with newer Roslyn package.
* 0.1.0:
  * Initial release.
