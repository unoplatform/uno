using System;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using Private.Infrastructure;
using Uno.UI.Toolkit.DevTools.Input;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

public partial class NavigationViewTests : MUXApiTestBase
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR || !HAS_UNO_WINUI
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task VerifyNavigationViewItemExpandsCollapsesWhenChevronTapped()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive("Test is currently disabled on Uno Islands #18105");
		}

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
