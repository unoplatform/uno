#if !WINDOWS_UWP

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.ImplementedRoutedEventArgsGeneratorTest
{
	[TestClass]
	[RunsOnUIThread]
	public partial class ImplementedRoutedEventArgsGeneratorTests
	{
		private MethodInfo GetMethod(Control control)
		{
			return control.GetType().GetMethod("GetImplementedRoutedEvents", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
		}

		[TestMethod]
		public void Given_Control()
		{
			var control = new Control();
			var method = GetMethod(control);
			Assert.AreEqual(RoutedEventFlag.None, (RoutedEventFlag)method.Invoke(control, null));
		}

		[TestMethod]
		public void Given_Button()
		{
			var btn = new Button();
			var method = GetMethod(btn);

			// Make sure the generator is actually used and is generating source. We don't want the test to be running the virtual method.
			Assert.AreEqual(typeof(Button), method.DeclaringType);

			// It's okay to adjust the following assert if more events are needed and implemented in the future.
			// This test is focused more of the source generator rather than Button functionality.
			Assert.AreEqual(RoutedEventFlag.PointerEntered | RoutedEventFlag.PointerPressed | RoutedEventFlag.PointerReleased, (RoutedEventFlag)method.Invoke(btn, null));
		}
	}
}
#endif
