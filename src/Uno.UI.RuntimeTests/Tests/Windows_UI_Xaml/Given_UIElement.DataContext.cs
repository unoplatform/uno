using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

partial class Given_UIElement
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Subtree_SeveredFromDataContextSource()
	{
		const int DC = 312;

		var nested2 = new Border() { Name = "nested2" };
		var nested1 = new Border() { Name = "nested1", Child = nested2 };
		var host = new Border() { Name = "host", Child = nested1 };
		await UITestHelper.Load(host, x => x.IsLoaded);

		host.DataContext = DC;
		Assert.AreEqual(DC, nested1.DataContext, "1. initially, DC (nested1) should be inherited");
		Assert.AreEqual(DC, nested2.DataContext, "1. initially, DC (nested2) should be inherited");

		host.Child = null;
		Assert.IsNull(nested1.DataContext, "2. when detached, inherited DC(nested1) should be cleared");
		Assert.IsNull(nested2.DataContext, "2. when detached, inherited DC(nested2) should be cleared");

		host.Child = nested1;
		Assert.AreEqual(DC, nested1.DataContext, "3. when reattached, DC (nested1) should be inherited again");
		Assert.AreEqual(DC, nested2.DataContext, "3. when reattached, DC (nested2) should be inherited again");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_SeveredSubtree_ContainsDataContextSource()
	{
		const int DC = 312;

		//   v detachment point
		// H > N1 > N2 > N3 > N4
		//          ^ DC owner, and propagation source
		//               ^  + ^ inherited DC
		var nested4 = new Border() { Name = "nested4", };
		var nested3 = new Border() { Name = "nested3", Child = nested4 };
		var nested2 = new Border() { Name = "nested2", Child = nested3 };
		var nested1 = new Border() { Name = "nested1", Child = nested2 };
		var host = new Border() { Name = "host", Child = nested1 };
		await UITestHelper.Load(host, x => x.IsLoaded);

		nested2.DataContext = DC;
		Assert.AreEqual(DC, nested3.DataContext, "1. initially, DC (nested3) should be inherited");
		Assert.AreEqual(DC, nested4.DataContext, "1. initially, DC (nested4) should be inherited");

		host.Child = null;
		Assert.AreEqual(DC, nested3.DataContext, "2. when detached, DC (nested3) should still be inherited&unaffected");
		Assert.AreEqual(DC, nested4.DataContext, "2. when detached, DC (nested4) should still be inherited&unaffected");

		host.Child = nested1;
		Assert.AreEqual(DC, nested3.DataContext, "3. when reattached, DC (nested3) should still be inherited&unaffected");
		Assert.AreEqual(DC, nested4.DataContext, "3. when reattached, DC (nested4) should still be inherited&unaffected");
	}

#if HAS_UNO && false
	// DC on any DependencyObject is only an uno concept, not in WinUI
	// disabled until #22610 being fixed
	
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_DataContext_DoesNotPropagate_To_NonFrameworkElement_Property()
	{
		var setups = new Func<object, Brush, FrameworkElement>[]
		{
			// the order of property matters here
			(dc, brush) => new Border { DataContext = dc, Background = brush, },
			(dc, brush) => new Border { Background = brush, DataContext = dc, },
			(dc, brush) => new Control { DataContext = dc, Foreground = brush, },
			(dc, brush) => new Control { Foreground = brush, DataContext = dc, },
		};
		foreach (var setup in setups)
		{
			var brush = new SolidColorBrush(Colors.SkyBlue);
			var dc = new { Data = "Context" };

#if DEBUG
			using var disp = brush.RegisterDisposablePropertyChangedCallback(
				SolidColorBrush.DataContextProperty,
				(s, e) => { /* breakpoint here to investigate */ });
#endif

			var sut = setup(dc, brush);
			await UITestHelper.Load(sut, x => x.IsLoaded);

			Assert.IsNull(brush.DataContext, "DataContext should not propagate into Brush (via Control.Background)");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_DC_Propagate_To_NonFE_Property_In_Subtree()
	{
		var brush = new SolidColorBrush(Colors.SkyBlue);
		var dc = new object();

#if DEBUG
		using var disp = brush.RegisterDisposablePropertyChangedCallback(
			SolidColorBrush.DataContextProperty,
			(s, e) => { /* breakpoint here to investigate */ });
#endif

		var sut = new Control { Foreground = brush, };
		var host = new Border { DataContext = dc, Child = sut, };

		await UITestHelper.Load(host, x => x.IsLoaded);

		Assert.IsNull(brush.DataContext, "DataContext should not propagate into Brush (via Control.Foreground)");
	}
#endif
}
