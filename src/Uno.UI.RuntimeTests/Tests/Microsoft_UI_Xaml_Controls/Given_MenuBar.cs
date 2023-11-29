using System;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using MenuBar = Microsoft.UI.Xaml.Controls.MenuBar;
using MenuBarItem = Microsoft.UI.Xaml.Controls.MenuBarItem;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public partial class Given_MenuBar
{
#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("InputInjector is only supported on skia")]
#endif
	public async Task When_Hover_over_Different_Items()
	{
		XamlRoot xamlRoot = null;
		try
		{
			var SUT = new MenuBar
			{
				Items =
				{
					new MenuBarItem
					{
						Title = "File",
						Items =
						{
							new MenuFlyoutItem
							{
								Text = "New"
							},
							new MenuFlyoutItem
							{
								Text = "Open"
							},
							new MenuFlyoutItem
							{
								Text = "Save"
							},
						}
					},
					new MenuBarItem
					{
						Title = "Edit",
						Items =
						{
							new MenuFlyoutItem
							{
								Text = "Undo"
							},
							new MenuFlyoutItem
							{
								Text = "Cut"
							},
							new MenuFlyoutItem
							{
								Text = "Copy"
							},
							new MenuFlyoutItem
							{
								Text = "Paste"
							}
						}
					},
					new MenuBarItem
					{
						Title = "Help",
						Items =
						{
							new MenuFlyoutItem
							{
								Text = "About"
							},
						}
					}
				}
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			xamlRoot = SUT.XamlRoot;

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.MoveTo(SUT.GetAbsoluteBounds().GetCenter());
			mouse.Press();
			mouse.Release();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(4, ((Panel)VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)[0].Child.FindVisualChildByType<MenuFlyoutItem>().GetParent()).Children.Count);

			mouse.MoveBy(-50, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(3, ((Panel)VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)[0].Child.FindVisualChildByType<MenuFlyoutItem>().GetParent()).Children.Count);

			mouse.MoveBy(100, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, ((Panel)VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)[0].Child.FindVisualChildByType<MenuFlyoutItem>().GetParent()).Children.Count);

			mouse.MoveBy(0, -50);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, ((Panel)VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot)[0].Child.FindVisualChildByType<MenuFlyoutItem>().GetParent()).Children.Count);
		}
		finally
		{
			VisualTreeHelper.CloseAllFlyouts(xamlRoot);
		}
	}
#endif
}
