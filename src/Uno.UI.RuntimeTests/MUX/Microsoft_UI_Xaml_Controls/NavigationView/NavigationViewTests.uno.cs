using System;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using NavigationView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests;

public partial class NavigationViewTests : MUXApiTestBase
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR || !HAS_UNO_WINUI
	[Ignore("InputInjector is only supported on skia")]
#endif
	public async Task VerifyNavigationViewItemExpandsCollapsesWhenChevronTapped()
	{
		NavigationView SUT = null;

		MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
		{
			SUT = new NavigationView
			{
				MenuItems =
				{
					new NavigationViewItem
					{
						Content = "Menu Item 1",
						Icon = new SymbolIcon(Symbol.Home),
						Name = "MI1",
						MenuItems =
						{
							new NavigationViewItem
							{
								Content = "Menu Item 2", Icon = new SymbolIcon(Symbol.Save), Name = "MI2"
							},
							new NavigationViewItem
							{
								Content = "Menu Item 3", Icon = new SymbolIcon(Symbol.Save), Name = "MI3"
							}
						}
					}
				}
			};

			WindowHelper.WindowContent = SUT;
		});


		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		NavigationViewItem mi1 = null;
		FrameworkElement chevron = null;
		InputInjector injector = null;

		MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
		{
			injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			mi1 = (NavigationViewItem)SUT.FindVisualChildByName("MI1");
			chevron = (FrameworkElement)SUT.FindVisualChildByName("ExpandCollapseChevron");

			Assert.IsFalse(mi1.IsExpanded);

			finger.Press(chevron.GetAbsoluteBounds().GetCenter());
			finger.Release();
		});

		await WindowHelper.WaitForIdle();

		Assert.IsTrue(mi1.IsExpanded);

		MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
		{
			using var finger = injector.GetFinger();
			finger.Press(chevron.GetAbsoluteBounds().GetCenter());
			finger.Release();
		});


		await WindowHelper.WaitForIdle();

		Assert.IsFalse(mi1.IsExpanded);

		var bounds = mi1.GetAbsoluteBounds().GetCenter();
		MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
		{
			using var finger = injector.GetFinger();
			finger.Press(bounds);
			finger.Release();
		});


		await WindowHelper.WaitForIdle();

		Assert.IsTrue(mi1.IsExpanded);

		MUXControlsTestApp.Utilities.RunOnUIThread.Execute(() =>
		{
			using var finger = injector.GetFinger();
			finger.Press(bounds);
			finger.Release();
		});


		await WindowHelper.WaitForIdle();

		Assert.IsFalse(mi1.IsExpanded);
	}
}
