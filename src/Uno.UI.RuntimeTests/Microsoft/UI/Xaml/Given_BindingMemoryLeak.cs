#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Microsoft.UI;
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
			// last element it was applied to, nor the DataContext it inherited from it.
			var sharedBrush = new SolidColorBrush(Colors.Red);

			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateSingleOwnerSharedBrushView(root, sharedBrush);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
			GC.KeepAlive(sharedBrush);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_TransitionsShared_Removed_Then_ViewModel_Collected()
		{
			// A TransitionCollection shared through a Style setter is pushed the owner's DataContext; it must
			// not retain that DataContext once the owner is gone.
			var sharedTransitions = new TransitionCollection { new AddDeleteThemeTransition() };
			var style = new Style(typeof(Border));
			style.Setters.Add(new Setter(UIElement.TransitionsProperty, sharedTransitions));

			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateStyledView(root, style);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
			GC.KeepAlive(sharedTransitions);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25099")]
		public async Task When_InputScopeShared_Removed_Then_ViewModel_Collected()
		{
			// A plain DependencyObject value (no bindings of its own) shared across TextBoxes inherits the
			// owner's DataContext; it must not retain it once the owner is gone.
			var sharedScope = new InputScope { Names = { new InputScopeName(InputScopeNameValue.Number) } };

			var root = new ContentControl();
			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var (viewRef, vmRef) = CreateSharedInputScopeView(root, sharedScope);
			await TestServices.WindowHelper.WaitForLoaded((FrameworkElement)root.Content);

			root.Content = null;

			await AssertCollectedAsync(viewRef, vmRef);
			GC.KeepAlive(sharedScope);
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
		private static (WeakReference viewRef, WeakReference vmRef) CreateStyledView(ContentControl root, Style style)
		{
			var vm = new BindingLeak_ViewModel { Text = "Styled shared value test" };

			var owner = new Border { Style = style, Width = 50, Height = 50 };
			var panel = new StackPanel { Width = 100, Height = 100 };
			panel.Children.Add(owner);
			panel.DataContext = vm;

			root.Content = panel;

			return (new WeakReference(owner), new WeakReference(vm));
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static (WeakReference viewRef, WeakReference vmRef) CreateSharedInputScopeView(ContentControl root, InputScope sharedScope)
		{
			var vm = new BindingLeak_ViewModel { Text = "Shared input scope test" };

			var owner = new TextBox { InputScope = sharedScope, Width = 50, Height = 50 };
			var panel = new StackPanel { Width = 100, Height = 100 };
			panel.Children.Add(owner);
			panel.DataContext = vm;

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
			var sw = Stopwatch.StartNew();
			var timeout = TimeSpan.FromSeconds(10);

			while (sw.Elapsed < timeout && (viewRef.IsAlive || vmRef.IsAlive))
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

			Assert.IsFalse(viewRef.IsAlive, "View should have been garbage collected after removal from visual tree.");
			Assert.IsFalse(vmRef.IsAlive, "ViewModel should have been garbage collected after View removal from visual tree.");
		}
	}
}
