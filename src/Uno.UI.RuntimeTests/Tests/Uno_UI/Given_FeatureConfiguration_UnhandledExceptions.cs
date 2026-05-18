using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
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
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher;
		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = false;

			var sut = new Border();
			sut.Tapped += ThrowingHandler;

			var raised = sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs());

			Assert.IsFalse(raised);
		}
		finally
		{
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_SafeRaiseEvent_Throws_FlagOn_NoSubscriber_Propagates()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher;
		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = true;

			var sut = new Border();
			sut.Tapped += ThrowingHandler;

			Assert.ThrowsExactly<TestException>(() =>
				sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs()));
		}
		finally
		{
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_SafeRaiseEvent_Throws_FlagOn_Handled_DoesNotPropagate()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher;
		Exception observed = null;

		void Handler(object s, MuxUnhandledExceptionEventArgs e)
		{
			observed = e.Exception;
			e.Handled = true;
		}

		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = true;
			Application.Current.UnhandledException += Handler;

			var sut = new Border();
			sut.Tapped += ThrowingHandler;

			sut.SafeRaiseEvent(UIElement.TappedEvent, new TappedRoutedEventArgs());

			Assert.IsInstanceOfType(observed, typeof(TestException));
		}
		finally
		{
			Application.Current.UnhandledException -= Handler;
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = originalFlag;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Dispatcher_Throws_FlagOff_Swallowed()
	{
		var originalFlag = FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher;
		try
		{
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = false;

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
			FeatureConfiguration.UnhandledExceptionHandling.ShouldPropagateFromInputAndDispatcher = originalFlag;
		}
	}

	// Note: flag-on dispatcher propagation is intentionally not covered by an automated test.
	// The exception propagates up to the platform message pump, which on most hosts terminates
	// the process - incompatible with the runtime-test harness. Manual SamplesApp smoke covers it.

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
	public void When_Resource_Missing_FlagOn_Throws_XamlParseException()
	{
		var originalFlag = FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource;
		try
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = true;

			// ContentControl applies its default Style during construction (XamlReader.Load),
			// which triggers UpdateResourceBindings on the new instance. With the flag on,
			// the missing resource lookup throws XamlParseException, which the XAML reader
			// then wraps in its own parse exception with our XamlParseException as inner.
			var xaml = "<ContentControl xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Tag=\"{StaticResource NonExistentResource_ShouldThrow}\" />";

			XamlParseException foundXamlParseException = null;
			try
			{
				_ = XamlReader.Load(xaml);
			}
			catch (Exception ex)
			{
				for (var e = ex; e != null; e = e.InnerException)
				{
					if (e is XamlParseException xpe)
					{
						foundXamlParseException = xpe;
						break;
					}
				}
			}

			Assert.IsNotNull(
				foundXamlParseException,
				"Expected a Microsoft.UI.Xaml.Markup.XamlParseException to be thrown (possibly wrapped).");
			Assert.IsTrue(
				foundXamlParseException.Message.Contains("NonExistentResource_ShouldThrow"),
				$"Expected message to reference the missing key, got: {foundXamlParseException.Message}");
		}
		finally
		{
			FeatureConfiguration.ResourceResolution.ThrowOnUnresolvedResource = originalFlag;
		}
	}
}
