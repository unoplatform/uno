#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public class Given_BindingMemoryLeak
	{
		[TestCleanup]
		public void Cleanup()
		{
			// A failed test must not leave its tree rooted for retries and later tests.
			TestServices.WindowHelper.WindowContent = null;
		}

		[TestMethod]
		public async Task When_xBind_View_Removed_Then_Collected()
		{
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateXBindView(root);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
		}

		[TestMethod]
		public async Task When_xBind_View_NullDC_Removed_Then_Collected()
		{
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateXBindViewWithNullDC(root);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
		}

		[TestMethod]
		public async Task When_Binding_View_Removed_Then_Collected()
		{
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateBindingView(root);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
		}

		[TestMethod]
		public async Task When_Binding_View_NullDC_Removed_Then_Collected()
		{
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateBindingViewWithNullDC(root);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
		}

		[TestMethod]
		public async Task When_BrushInResourceDictionary_MultiParent_Then_ViewModel_Collected()
		{
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateBrushResourceView(root);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
		}

		[TestMethod]
		public async Task When_BrushSharedProgrammatically_Then_ViewModel_Collected()
		{
			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateProgrammaticSharedBrushView(root);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_BrushShared_SingleOwner_Removed_Then_Owner_And_ViewModel_Collected()
		{
			// A long-lived brush (e.g. a static field) applied to one element at a time must not retain the
			// last element it was applied to.
			var sharedBrush = new SolidColorBrush(Colors.Red);

			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateSingleOwnerSharedBrushView(root, sharedBrush);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await WaitUntilCollectedAsync(viewRef);
			Assert.IsFalse(viewRef.IsAlive, "The element the shared brush was applied to should have been garbage collected after removal from visual tree.");

			// The DataContext pushed into the brush is held at Inheritance precedence until the brush is re-associated:
			// the collected owner still counts as a first parent, so the next one disables inheritance and drops it.
			var next = new Border { Background = sharedBrush };
			await AssertCollectedAsync(viewRef, vmRef);

			GC.KeepAlive(next);
			GC.KeepAlive(sharedBrush);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateSingleOwnerSharedBrushView(ContentControl root, Brush sharedBrush)
		{
			var vm = new BindingLeak_ViewModel { Text = "Single owner shared brush test" };

			var owner = new Border { Background = sharedBrush, Width = 50, Height = 50 };
			var panel = new StackPanel { Width = 100, Height = 100, DataContext = vm };
			panel.Children.Add(owner);

			root.Content = panel;

			return (new WeakReference(owner), new WeakReference(vm));
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateXBindView(ContentControl root)
		{
			var vm = new BindingLeak_ViewModel { Text = "xBind test" };
			var view = new BindingLeak_xBind_View() { Width = 100, Height = 100, ViewModel = vm };
			root.Content = view;

			var viewRef = new WeakReference(view);
			var vmRef = new WeakReference(vm);

			return (viewRef, vmRef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateXBindViewWithNullDC(ContentControl root)
		{
			var vm = new BindingLeak_ViewModel { Text = "xBind null DC test" };
			var view = new BindingLeak_xBind_View() { Width = 100, Height = 100, ViewModel = vm };
			root.Content = view;

			// Null DataContext before removal
			view.ViewModel = null;

			var viewRef = new WeakReference(view);
			var vmRef = new WeakReference(vm);

			return (viewRef, vmRef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateBindingView(ContentControl root)
		{
			var vm = new BindingLeak_ViewModel { Text = "Binding test" };
			var view = new BindingLeak_Binding_View { Width = 100, Height = 100, DataContext = vm };
			root.Content = view;

			var viewRef = new WeakReference(view);
			var vmRef = new WeakReference(vm);

			return (viewRef, vmRef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateBindingViewWithNullDC(ContentControl root)
		{
			var vm = new BindingLeak_ViewModel { Text = "Binding null DC test" };
			var view = new BindingLeak_Binding_View { Width = 100, Height = 100, DataContext = vm };
			root.Content = view;

			// Null DataContext before removal
			view.DataContext = null;

			var viewRef = new WeakReference(view);
			var vmRef = new WeakReference(vm);

			return (viewRef, vmRef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateBrushResourceView(ContentControl root)
		{
			var vm = new BindingLeak_ViewModel { Text = "Brush resource test" };
			var view = new BindingLeak_BrushResource_View { Width = 100, Height = 100, DataContext = vm };
			root.Content = view;

			var viewRef = new WeakReference(view);
			var vmRef = new WeakReference(vm);

			return (viewRef, vmRef);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateProgrammaticSharedBrushView(ContentControl root)
		{
			var vm = new BindingLeak_ViewModel { Text = "Shared brush test" };

			// Create a brush and assign it to multiple controls programmatically
			// to trigger multi-parent sharing scenario
			var sharedBrush = new SolidColorBrush(Colors.Blue);

			var border1 = new Border { Background = sharedBrush, Width = 50, Height = 50 };
			var border2 = new Border { Background = sharedBrush, Width = 50, Height = 50 };

			var panel = new StackPanel { Width = 100, Height = 100 };
			panel.Children.Add(border1);
			panel.Children.Add(border2);
			panel.DataContext = vm;

			root.Content = panel;

			var viewRef = new WeakReference(panel);
			var vmRef = new WeakReference(vm);

			return (viewRef, vmRef);
		}

		private static async Task AssertCollectedAsync(WeakReference viewRef, WeakReference vmRef)
		{
			await WaitUntilCollectedAsync(viewRef, vmRef);

			Assert.IsFalse(viewRef.IsAlive, "View should have been garbage collected after removal from visual tree.");
			Assert.IsFalse(vmRef.IsAlive, "ViewModel should have been garbage collected after View removal from visual tree.");
		}

		private static async Task WaitUntilCollectedAsync(params WeakReference[] references)
		{
			var sw = Stopwatch.StartNew();
			var timeout = TimeSpan.FromSeconds(10);

			while (sw.Elapsed < timeout && Array.Exists(references, r => r.IsAlive))
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				GC.Collect(2);

				// Platform-specific GC quirk: some platforms need a Task.Yield()
				// to allow async continuations and weak reference cleanup
#if __SKIA__
				if (OperatingSystem.IsBrowser() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
#endif
				{
					await Task.Yield();
					GC.Collect(2);
					GC.WaitForPendingFinalizers();
					GC.Collect(2);
				}

				// Waiting for idle is required for collection of
				// DispatcherConditionalDisposable to be executed
				await TestServices.WindowHelper.WaitForIdle();
			}
		}
	}
}
