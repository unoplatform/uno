#if HAS_UNO // Exercises Uno-only APIs (FeatureConfiguration, SafeRaiseEvent, Uno.UI.Xaml.BindingHelper) absent from native WinUI.
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using BindingHelper = Uno.UI.Xaml.BindingHelper;
using MuxUnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI;

[TestClass]
public class Given_FeatureConfiguration_UnhandledExceptions
{
	private sealed class TestException : Exception
	{
		public TestException(string message) : base(message) { }
	}

	private static void ThrowingHandler(object s, TappedRoutedEventArgs e)
	{
		throw new TestException("from-tap");
	}

	private static void ThrowingDispatcherWorkItem()
	{
		throw new TestException("from-dispatch");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_SafeRaiseEvent_Throws_FlagOff_Swallowed()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions;
		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = false;

			var sut = new Border();
			sut.Tapped += ThrowingHandler;

			var raised = sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs());

			Assert.IsFalse(raised);
		}
		finally
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_SafeRaiseEvent_Throws_FlagOn_NoSubscriber_Propagates()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions;
		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = true;

			var sut = new Border();
			sut.Tapped += ThrowingHandler;

			Assert.ThrowsExactly<TestException>(() =>
				sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs()));
		}
		finally
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_SafeRaiseEvent_Throws_FlagOn_Handled_DoesNotPropagate()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions;
		Exception observed = null;

		void Handler(object s, MuxUnhandledExceptionEventArgs e)
		{
			observed = e.Exception;
			e.Handled = true;
		}

		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = true;
			Application.Current.UnhandledException += Handler;

			var sut = new Border();
			sut.Tapped += ThrowingHandler;

			sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs());

			Assert.IsInstanceOfType(observed, typeof(TestException));
		}
		finally
		{
			Application.Current.UnhandledException -= Handler;
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Handler_Throws_FlagOn_Handled_RouteContinues()
	{
		// WinUI reports a failing handler through Application.UnhandledException and, once the app
		// marks it handled, keeps raising the event on the remaining handlers and ancestors
		// (CEventManager ignores the per-listener failure, DXamlCore::FireEvent reports it).
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions;
		var reported = 0;
		var sameElementInvoked = false;
		var parentInvoked = false;

		void Handler(object s, MuxUnhandledExceptionEventArgs e)
		{
			reported++;
			e.Handled = true;
		}

		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = true;
			Application.Current.UnhandledException += Handler;

			var sut = new Border();
			var parent = new Border { Child = sut };
			sut.Tapped += ThrowingHandler;
			sut.Tapped += (s, e) => sameElementInvoked = true;
			parent.Tapped += (s, e) => parentInvoked = true;

			sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs());

			Assert.AreEqual(1, reported);
			Assert.IsTrue(sameElementInvoked, "The next handler on the same element should still run.");
			Assert.IsTrue(parentInvoked, "The event should keep bubbling to the parent.");
		}
		finally
		{
			Application.Current.UnhandledException -= Handler;
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Handler_Throws_FlagOn_Unhandled_ReportedOnce()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions;
		var reported = 0;

		void Handler(object s, MuxUnhandledExceptionEventArgs e) => reported++;

		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = true;
			Application.Current.UnhandledException += Handler;

			var sut = new Border();
			sut.Tapped += ThrowingHandler;

			Assert.ThrowsExactly<TestException>(() =>
				sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs()));

			Assert.AreEqual(1, reported);
		}
		finally
		{
			Application.Current.UnhandledException -= Handler;
			FeatureConfiguration.UnhandledExceptionHandling.PropagateInputExceptions = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Dispatcher_Throws_FlagOff_Swallowed()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.PropagateDispatcherExceptions;
		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateDispatcherExceptions = false;

			var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
			var ran = false;
			dispatcher.TryEnqueue(ThrowingDispatcherWorkItem);

			await WindowHelper.WaitForIdle();

			// A subsequent enqueue still runs, meaning the dispatcher caught and logged the
			// prior exception (legacy behavior).
			dispatcher.TryEnqueue(() => ran = true);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(ran);
		}
		finally
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateDispatcherExceptions = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Dispatcher_Throws_FlagOn_Handled_Continues()
	{
		// WinUI raises Application.UnhandledException for a failing dispatcher work item
		// (FinalUnhandledErrorDetected -> ProcessUnhandledError) and only fails fast when unhandled.
		// The unhandled case reaches the platform message pump, which the test harness cannot survive.
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.PropagateDispatcherExceptions;
		Exception observed = null;

		void Handler(object s, MuxUnhandledExceptionEventArgs e)
		{
			observed = e.Exception;
			e.Handled = true;
		}

		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.PropagateDispatcherExceptions = true;
			Application.Current.UnhandledException += Handler;

			var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
			var ran = false;
			dispatcher.TryEnqueue(ThrowingDispatcherWorkItem);
			dispatcher.TryEnqueue(() => ran = true);

			await WindowHelper.WaitFor(() => ran);

			Assert.IsInstanceOfType(observed, typeof(TestException));
		}
		finally
		{
			Application.Current.UnhandledException -= Handler;
			FeatureConfiguration.UnhandledExceptionHandling.PropagateDispatcherExceptions = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_Resource_Missing_FlagOff_Update_Logs_Only()
	{
		var originalFlag = FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource;
		try
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = false;

			var xaml = "<ContentControl xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Tag=\"{StaticResource NonExistentResource_ShouldOnlyLog}\" />";
			var sut = (ContentControl)XamlReader.Load(xaml);

			// Triggering binding resolution with the flag off should NOT throw.
			BindingHelper.UpdateResourceBindings(sut);
		}
		finally
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Resource_Missing_FlagOn_NotLoaded_DoesNotThrow()
	{
		var originalFlag = FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource;
		try
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = true;

			// A {StaticResource} binding exists precisely because the key could not be resolved at
			// parse time; it is retried once the tree scope is available. Before the owner is loaded
			// the retry has not had its chance, so a miss must not be reported as a failure.
			var xaml = "<ContentControl xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Tag=\"{StaticResource NonExistentResource_NotLoaded}\" />";
			var sut = (ContentControl)XamlReader.Load(xaml);

			BindingHelper.UpdateResourceBindings(sut);

			await WindowHelper.WaitForIdle();
		}
		finally
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Resource_Missing_FlagOn_Loaded_Throws_XamlParseException()
	{
		var originalFlag = FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource;
		try
		{
			var xaml = "<ContentControl xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Tag=\"{StaticResource NonExistentResource_ShouldThrow}\" />";
			var sut = (ContentControl)XamlReader.Load(xaml);

			// Load with the flag off, so only the explicit post-load update below is under test.
			await UITestHelper.Load(sut, x => x.IsLoaded);

			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = true;

			// Now that the owner is live in the tree, the key is definitively missing.
			var exception = Assert.ThrowsExactly<XamlParseException>(
				() => BindingHelper.UpdateResourceBindings(sut));

			Assert.IsTrue(
				exception.Message.Contains("NonExistentResource_ShouldThrow"),
				$"Expected message to reference the missing key, got: {exception.Message}");
		}
		finally
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = originalFlag;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ThemeResource_Missing_FlagOn_Loaded_Throws_XamlParseException()
	{
		// WinUI: ThemeResourceExtension::ProvideValue raises AG_E_PARSER_FAILED_RESOURCE_FIND, surfacing
		// as XamlParseException (0x802B000A). Uno defers every XAML {ThemeResource} to load time, so a
		// key that never resolved is judged once the owner is live.
		var originalFlag = FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource;
		try
		{
			var xaml = "<ContentControl xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Tag=\"{ThemeResource NonExistentThemeResource_ShouldThrow}\" />";
			var sut = (ContentControl)XamlReader.Load(xaml);

			await UITestHelper.Load(sut, x => x.IsLoaded);

			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = true;

			var exception = Assert.ThrowsExactly<XamlParseException>(
				() => BindingHelper.UpdateResourceBindings(sut));

			Assert.IsTrue(
				exception.Message.Contains("NonExistentThemeResource_ShouldThrow"),
				$"Expected message to reference the missing key, got: {exception.Message}");
		}
		finally
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = originalFlag;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false, DisplayName = "Key only in Light")]
	[DataRow(true, DisplayName = "App resources also define the key")]
	public async Task When_ThemeResource_Missing_For_New_Theme_FlagOn_Throws(bool appLevelKey)
	{
		// WinUI (verified on WinAppSDK 2.5.1): CThemeResource::RefreshValue raises
		// AG_E_PARSER_FAILED_RESOURCE_FIND when the pinned dictionary lacks the key for the new theme. The
		// RequestedTheme setter throws COMException 0x800F1000 and the old value stays; an app-level
		// resource with the same key does not rescue it. The key must exist in the app's theme, as the
		// initial lookup runs against it.
		var originalFlag = FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource;
		var key = appLevelKey ? "ThemeMissProbe_AppLevel" : "ThemeMissProbe_LightOnly";
		if (appLevelKey)
		{
			Application.Current.Resources[key] = new SolidColorBrush(Microsoft.UI.Colors.Orange);
		}

		try
		{
			var (root, target, otherTheme) = CreateSingleThemeResource(key);
			await UITestHelper.Load(root, x => x.IsLoaded);
			Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)target.Background).Color);

			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = true;

			var exception = Assert.ThrowsExactly<COMException>(() => root.RequestedTheme = otherTheme);

			Assert.AreEqual(unchecked((int)0x800F1000), exception.HResult);
			Assert.IsTrue(exception.Message.Contains(key), $"Expected message to reference the missing key, got: {exception.Message}");
			Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)target.Background).Color);
		}
		finally
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = originalFlag;
			WindowHelper.WindowContent = null;
			if (appLevelKey)
			{
				Application.Current.Resources.Remove(key);
			}
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ThemeResource_Missing_For_New_Theme_FlagOff_DoesNotThrow()
	{
		var originalFlag = FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource;
		try
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = false;

			var (root, _, otherTheme) = CreateSingleThemeResource("ThemeMissProbe_FlagOff");
			await UITestHelper.Load(root, x => x.IsLoaded);

			root.RequestedTheme = otherTheme;
		}
		finally
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = originalFlag;
			WindowHelper.WindowContent = null;
		}
	}

	/// <summary>
	/// Defines <paramref name="key"/> only in the theme dictionary of the app's current theme, so it resolves
	/// initially and is missing once the root switches to the returned other theme.
	/// </summary>
	private static (Grid Root, Border Target, ElementTheme OtherTheme) CreateSingleThemeResource(string key)
	{
		var isAppLight = Application.Current.RequestedTheme == ApplicationTheme.Light;
		var (current, other) = isAppLight ? ("Light", "Dark") : ("Dark", "Light");

		var root = (Grid)XamlReader.Load($$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				  RequestedTheme="{{current}}">
				<Grid.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="{{current}}"><SolidColorBrush x:Key="{{key}}" Color="Red" /></ResourceDictionary>
							<ResourceDictionary x:Key="{{other}}"><SolidColorBrush x:Key="Unrelated" Color="Blue" /></ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Grid.Resources>
				<Border x:Name="Target" Width="50" Height="50" Background="{ThemeResource {{key}}}" />
			</Grid>
			""");

		return (root, (Border)root.FindName("Target"), isAppLight ? ElementTheme.Dark : ElementTheme.Light);
	}
}
#endif
