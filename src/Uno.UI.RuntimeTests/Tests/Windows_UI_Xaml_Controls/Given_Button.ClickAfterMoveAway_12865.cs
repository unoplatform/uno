using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Button_ClickAfterMoveAway_12865
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/12865
		// When a Button is pressed and then the Button is moved out from
		// under a stationary pointer before the pointer is released,
		// WinUI does NOT raise Click, but Skia does. The click should be
		// cancelled when the release happens outside the Button's bounds.
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Pointer_Released_Outside_Because_Button_Moved_Then_No_Click_12865()
		{
			var button = new Button
			{
				Content = "Click me",
				Width = 160,
				Height = 48,
				Margin = new Thickness(0),
			};

			var clicked = 0;
			button.Click += (_, _) => clicked++;

			var container = new Grid
			{
				Width = 400,
				Height = 300,
				Children = { button },
			};
			button.HorizontalAlignment = HorizontalAlignment.Left;
			button.VerticalAlignment = VerticalAlignment.Top;

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(button);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var bounds = button.GetAbsoluteBounds();
			var center = new Windows.Foundation.Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

			mouse.Press(center);
			await WindowHelper.WaitForIdle();

			// Move the button away without moving the pointer.
			button.Margin = new Thickness(250, 0, 0, 0);
			await WindowHelper.WaitForIdle();

			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(
				0,
				clicked,
				"Click should not fire when the pointer is released outside the Button's bounds " +
				"(even if the Button moved away from a stationary pointer). " +
				"See https://github.com/unoplatform/uno/issues/12865");
		}
	}
}
