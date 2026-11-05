#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests;

// A binding dropped without being disposed must not leave subscriptions on a source that outlives it,
// whatever kind of source it binds to: a collection it indexes into, a DependencyObject, or an x:Bind source.
[TestClass]
[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
public class Given_BindingSourceStaleHandlers
{
	private const int Count = 5;

	// A DependencyObject source: the binding registers a property-changed callback on it.
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_DependencyObject_Source_Elements_Collected_Then_Callbacks_Removed(bool loaded)
	{
		var source = new Border { Tag = "tag" }; // outlives the elements
		var baseline = CallbackCount(source, FrameworkElement.TagProperty);

		var refs = await CreateBoundToDependencyObjectAsync(source, loaded);
		Assert.AreEqual(baseline + Count, CallbackCount(source, FrameworkElement.TagProperty), "Each binding should register one callback.");

		await CollectAsync(refs, () => CallbackCount(source, FrameworkElement.TagProperty) == baseline);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");
		Assert.AreEqual(baseline, CallbackCount(source, FrameworkElement.TagProperty), "Callbacks of collected bindings are still on the source.");

		source.Tag = "changed"; // must not reach a collected binding
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<List<WeakReference>> CreateBoundToDependencyObjectAsync(DependencyObject source, bool loaded)
	{
		var refs = new List<WeakReference>();
		var panel = new StackPanel();
		for (var i = 0; i < Count; i++)
		{
			var element = new TextBlock();
			element.SetBinding(TextBlock.TextProperty, new Binding { Source = source, Path = new PropertyPath(nameof(FrameworkElement.Tag)) });
			Assert.AreEqual("tag", element.Text);
			panel.Children.Add(element);
			refs.Add(new WeakReference(element));
		}

		await ShowAndRemoveAsync(panel, loaded);
		return refs;
	}

	private static async Task ShowAndRemoveAsync(Panel panel, bool loaded)
	{
		if (loaded)
		{
			try
			{
				WindowHelper.WindowContent = panel;
				for (var i = 0; i < 20 && !panel.IsLoaded; i++)
				{
					await WindowHelper.WaitForIdle();
				}
				Assert.IsTrue(panel.IsLoaded, "The elements were never loaded.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		await WindowHelper.WaitForIdle();
	}

	private static int CallbackCount(DependencyObject source, DependencyProperty property)
	{
		var details = ((IDependencyObjectStoreProvider)source).Store.GetPropertyDetails(property);
		var manager = typeof(DependencyPropertyDetails)
			.GetField("_callbackManager", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(details);
		if (manager is null)
		{
			return 0;
		}

		var callbacks = (ICollection)manager.GetType()
			.GetField("_callbacks", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(manager)!;
		return callbacks.Count;
	}

	// Collects at least 3 times, pumping the dispatcher in between so the removals posted by finalizers run.
	private static async Task CollectAsync(List<WeakReference> refs, Func<bool>? until = null)
	{
		until ??= () => !refs.Any(r => r.IsAlive);
		var sw = Stopwatch.StartNew();
		var minIterations = 3;

		while (minIterations-- > 0 || (!until() && sw.Elapsed < TimeSpan.FromSeconds(10)))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			// Some platforms need a yield before weak references are cleared, as in Given_BindingMemoryLeak.
#if __SKIA__
			if (OperatingSystem.IsBrowser() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
#endif
			{
				await Task.Yield();
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				GC.Collect(2);
			}

			await WindowHelper.WaitForIdle();
		}
	}
}
