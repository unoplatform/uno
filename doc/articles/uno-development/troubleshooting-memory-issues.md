---
uid: Uno.Contributing.MemoryIssues
---

# Troubleshooting Memory Issues

Uno Platform provides a set of classes aimed at diagnosing memory issues related to leaking controls, whether it be from
an Uno.UI issue or from an invalid pattern in user code.

## WebAssembly memory profiling

Starting from [Uno.Wasm.Bootstrap](https://github.com/unoplatform/Uno.Wasm.Bootstrap) 3.2, the Xamarin Profiler can be used to profile memory usage. See [this documentation](https://github.com/unoplatform/Uno.Wasm.Bootstrap#memory-profiling) for additional details.

## Enable Memory instances counter

1. Install the `Uno.Core` NuGet package;
2. In your application, as early as possible in the initialization (generally in the `App.cs` or `App.xaml.cs` constructor), add and call the following method:

    ```csharp
    using Uno.UI.DataBinding;
    using Uno.UI.DataBinding;
    using System.Threading.Tasks;
    using Uno.Extensions;
    using Uno.Logging;

    // ....
    private void EnableViewsMemoryStatistics()
    {
    //
    // Call this method to enable Views memory tracking.
    // Make sure that you've added the following :
    //
            //  builder.AddFilter("Uno.UI.DataBinding", LogLevel.Information );
    //
    // in the logger settings, so that the statistics are showing up.
    //

    var unused = Windows.UI.Xaml.Window.Current.Dispatcher.RunAsync(
      CoreDispatcherPriority.Normal,
      async () =>
      {
      BinderReferenceHolder.IsEnabled = true;

      while (true)
      {
        await Task.Delay(1500);

        try
        {
        BinderReferenceHolder.LogReport();

        var inactiveInstances = BinderReferenceHolder.GetInactiveViewBinders();

        // Force the variable to be kept by the linker so we can see it with the debugger.
        // Put a breakpoint on this line to dig into the inactive views.
        inactiveInstances.ToString();
        }
        catch (Exception ex)
        {
        this.Log().Error("Report generation failed", ex);
        }
      }
      }
    );
    }
    ```

    You'll also need to add the following logger filter:

    ```csharp
    builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Information );
    ```

    As well as this package NuGet (you will need to update to the latest Uno.UI nuget version):

    ```xaml
    <PackageReference Include="Uno.UI.Adapter.Microsoft.Extensions.Logging" Version="4.0.13" />
    ```

It is also possible to create diffs between memory snapshots by using the `BinderReferenceHolder.LogInactiveViewReferencesStatsDiff` method.

## Interpreting the statistics output

The output provides two sets of DependencyObject in memory:

- Active, for which the instances have a parent dependency object (e.g. an item in a Grid)
- Inactive, for which the instances do not have a parent dependency object

When doing back and forth navigation between pages, instances of the controls in the dismissed pages should
generally be collected after a few seconds, once those have been cleared from the `Frame` control's forward
stack (done after every new page navigation).

Searching for inactive objects first is generally best, as those instances are most likely kept alive by
cross references.

This can happen when a Grid item has a strong field reference to its parent:

```csharp
var myItem = new Grid(){ Tag = grid };
grid.Children.Add(myItem);
```

For Xamarin.iOS specifically, see [this article about performance tuning](https://learn.microsoft.com/xamarin/ios/deploy-test/performance).

## Profiling on Desktop (Skia)

On the Desktop target, the Visual Studio diagnostic session can be used to investigate application memory to find issues such as memory leaks. Follow [this guide from Microsoft](https://learn.microsoft.com/en-us/visualstudio/profiling/memory-usage-without-debugging2?view=vs-2022&pivots=programming-language-dotnet).

> [!TIP]
> If the "Memory Usage" is not listed under the Available Tools of the Performance Profile. Make sure "YourProjectName (Desktop)" is selected as the target. If that still doesn't work, open the `YourProjectName.csproj` and manually replace the following line temporarily:
>
> ```xml
> <!--<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-browserwasm;net10.0-desktop</TargetFrameworks>-->
> <TargetFrameworks>net10.0-desktop</TargetFrameworks>
> ```
>
> You may need to close and re-open the solution after making the change.

---

> [!TIP]
> During profiling, you may use `GC.Collect()` to forces an immediate GC(Garbage Collection) without having to wait. It should be noted that the GC does not take effect immediately, and sometimes it may take more than one GC call for all loose objects to be collected. For rigorousness, it is recommended to call GC twice and wait a few seconds before taking a memory snapshot when troubleshooting memory leaks.
