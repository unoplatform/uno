using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

using MenuBar = Microsoft.UI.Xaml.Controls.MenuBar;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_MenuBar_HoverSwitch_13826
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/13826
		// On the Uno Gallery MenuBar sample, after opening one MenuBarItem's
		// flyout, hovering over a sibling MenuBarItem should switch the open
		// flyout to that sibling without requiring a second click. Reported as
		// not working on WebAssembly, Android, iOS, and Skia GTK.
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Hover_Switches_MenuBarItem_Flyout_13826()
		{
			var fileItem = new MenuBarItem { Title = "File" };
			fileItem.Items.Add(new MenuFlyoutItem { Text = "New" });

			var editItem = new MenuBarItem { Title = "Edit" };
			editItem.Items.Add(new MenuFlyoutItem { Text = "Cut" });

			var menuBar = new MenuBar
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
			};
			menuBar.Items.Add(fileItem);
			menuBar.Items.Add(editItem);

			var container = new Grid
			{
				Width = 400,
				Height = 200,
				Children = { menuBar },
			};

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForLoaded(menuBar);
			await WindowHelper.WaitForIdle();

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			var fileBounds = fileItem.GetAbsoluteBounds();
			var editBounds = editItem.GetAbsoluteBounds();

			mouse.Press(new Windows.Foundation.Point(fileBounds.X + fileBounds.Width / 2, fileBounds.Y + fileBounds.Height / 2));
			mouse.Release();
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			mouse.MoveTo(new Windows.Foundation.Point(editBounds.X + editBounds.Width / 2, editBounds.Y + editBounds.Height / 2));
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			var openFlyouts = VisualTreeHelper.GetOpenPopupsForXamlRoot(editItem.XamlRoot);
			var presenter = openFlyouts
				.Select(p => p.Child as MenuFlyoutPresenter)
				.FirstOrDefault(p => p is not null && p.Items.Any(item => item is MenuFlyoutItem mfi && mfi.Text == "Cut"));

			Assert.IsNotNull(
				presenter,
				"Hovering over the second MenuBarItem after opening the first should switch the open flyout to it (Edit menu's 'Cut' MenuFlyoutPresenter expected to be open). " +
				"See https://github.com/unoplatform/uno/issues/13826");
		}
	}
}
