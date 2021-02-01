# Troubleshooting Memory Issues 

Uno provides a set of classes aimed at diagnosing memory issues related to leaking controls, whether it be from
an Uno.UI issue or from an invalid pattern in user code.

## Enable Memory instances counter
In your application, as early as possible in the initialization (generally in the App.xaml.cs
constructor), add and call the following method:

```
using Uno.UI.DataBinding;

// ....
private void EnableViewsMemoryStatistics()
{
	//
	// Call this method to enable Views memory tracking.
	// Make sure that you've added the following :
	//
	//  { "Uno.UI.DataBinding", LogLevel.Information }
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
```
	{ "Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Information },
```

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

```
var myItem = new Grid(){ Tag = grid };
grid.Children.Add(myItem);
```

For Xamarin.iOS specifically, see [this article about performance tuning](https://docs.microsoft.com/en-us/xamarin/ios/deploy-test/performance).
