using System;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using NavigationView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

public partial class NavigationViewTests : MUXApiTestBase
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is only supported on skia")]
#endif
	public async Task VerifyNavigationViewItemExpandsCollapsesWhenChevronTapped()
	{
		var SUT = new NavigationView
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
		await WindowHelper.WaitForLoaded(SUT);
		await WindowHelper.WaitForIdle();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		var mi1 = (NavigationViewItem)SUT.FindVisualChildByName("MI1");
		var chevron = (FrameworkElement)SUT.FindVisualChildByName("ExpandCollapseChevron");

		Assert.IsFalse(mi1.IsExpanded);

		finger.Press(chevron.GetAbsoluteBounds().GetCenter());
		finger.Release();

		await WindowHelper.WaitForIdle();

		Assert.IsTrue(mi1.IsExpanded);

		finger.Press(chevron.GetAbsoluteBounds().GetCenter());
		finger.Release();

		await WindowHelper.WaitForIdle();

		Assert.IsFalse(mi1.IsExpanded);

		var bounds = mi1.GetAbsoluteBounds().GetCenter();
		finger.Press(bounds);
		finger.Release();

		await WindowHelper.WaitForIdle();

		Assert.IsTrue(mi1.IsExpanded);

		finger.Press(bounds);
		finger.Release();

		await WindowHelper.WaitForIdle();

		Assert.IsFalse(mi1.IsExpanded);
	}
}
