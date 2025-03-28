#if !WINAPPSDK

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.ImplementedRoutedEventArgsGeneratorTest
{
	[TestClass]
	public partial class ImplementedRoutedEventArgsGeneratorTests
	{
		private static void AssertRoutedEvent(Type type, RoutedEventFlag expected)
		{
			Assert.IsTrue(UIElementGeneratedProxy.TryGetImplementedRoutedEvents(type, out var actual));
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void Given_Control()
		{
			AssertRoutedEvent(typeof(Control), RoutedEventFlag.None);
		}

		[TestMethod]
		public void Given_Button()
		{
			// It's okay to adjust the following assert if more events are needed and implemented in the future.
			// This test is focused more on the source generator rather than the Button functionality.
			AssertRoutedEvent(typeof(Button), RoutedEventFlag.PointerEntered | RoutedEventFlag.PointerPressed | RoutedEventFlag.PointerReleased | RoutedEventFlag.PointerExited | RoutedEventFlag.PointerMoved | RoutedEventFlag.PointerCaptureLost | RoutedEventFlag.KeyDown | RoutedEventFlag.KeyUp | RoutedEventFlag.GotFocus | RoutedEventFlag.LostFocus);
		}
	}
}
#endif
