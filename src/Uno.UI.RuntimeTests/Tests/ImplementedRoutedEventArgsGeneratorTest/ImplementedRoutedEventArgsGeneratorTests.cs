#if !WINDOWS_UWP

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.ImplementedRoutedEventArgsGeneratorTest
{
	[TestClass]
	[RunsOnUIThread]
	public partial class ImplementedRoutedEventArgsGeneratorTests
	{
		private static void AssertRoutedEvent(Type type, RoutedEventFlag expected)
		{
			Assert.IsTrue(UIElementGeneratedProxy.TryGetImplementedRoutedEvents(type, out var actual));
			var args = new object[] { type, expected };
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
			// This test is focused more of the source generator rather than Button functionality.
			AssertRoutedEvent(typeof(Button), RoutedEventFlag.PointerEntered | RoutedEventFlag.PointerPressed | RoutedEventFlag.PointerReleased);
		}
	}
}
#endif
