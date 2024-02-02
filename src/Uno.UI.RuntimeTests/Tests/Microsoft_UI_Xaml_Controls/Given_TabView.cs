using System;
using System.Threading.Tasks;
using Windows.UI.Input.Preview.Injection;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TabView
{
#if HAS_UNO
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is only supported on skia")]
#endif
	public async Task When_First_Item_Unselected()
	{
		var SUT = new TabView
		{
			TabItems =
			{
				new TabViewItem
				{
					Header = "Tab1"
				},
				new TabViewItem
				{
					Header = "Tab2"
				}
			}
		};

		await UITestHelper.Load(SUT);

		Assert.IsTrue(((TabViewItem)SUT.TabItems[0]).IsSelected);
		Assert.IsFalse(((TabViewItem)SUT.TabItems[1]).IsSelected);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(SUT.GetAbsoluteBounds().GetCenter());
		finger.Release();

		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(((TabViewItem)SUT.TabItems[0]).IsSelected);
		Assert.IsTrue(((TabViewItem)SUT.TabItems[1]).IsSelected);
	}
#endif
}
