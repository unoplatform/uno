using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;

using TwoPaneView = Microsoft.UI.Xaml.Controls.TwoPaneView;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_TwoPaneView
{
	private partial class MyTwoPaneView : TwoPaneView
	{
		internal bool TemplateApplied { get; private set; }
		internal Exception ExceptionThrown { get; private set; }

		protected override void OnApplyTemplate()
		{
			try
			{
				TemplateApplied = true;
				base.OnApplyTemplate();
			}
			catch (Exception e)
			{
				ExceptionThrown = e;
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_ApplyTemplate_Should_Not_Throw()
	{
		var SUT = new MyTwoPaneView() { Width = 100, Height = 100 };
		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(SUT.TemplateApplied);
		Assert.IsNull(SUT.ExceptionThrown);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14133")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_TwoPaneView_In_Finite_Container_Pane1_Has_Finite_Height()
	{
		const double containerHeight = 400;
		const double containerWidth = 600;

		var listView = new ListView
		{
			ItemsSource = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList(),
		};

		var button = new Button { Content = "Below ListView" };

		var pane1Content = new StackPanel
		{
			Children = { listView, button }
		};

		var SUT = new TwoPaneView
		{
			Width = containerWidth,
			Height = containerHeight,
			Pane1 = pane1Content,
			MinWideModeWidth = double.MaxValue,
			MinTallModeHeight = double.MaxValue,
		};

		await UITestHelper.Load(SUT);

		Assert.IsTrue(listView.ActualHeight <= containerHeight,
			$"ListView inside TwoPaneView Pane1 should have finite height (≤{containerHeight}), but got {listView.ActualHeight}. Issue #14133: TwoPaneView passes infinite height to its panes.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/14133")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_TwoPaneView_Pane1_Sibling_Button_Is_Visible()
	{
		const double containerHeight = 400;
		const double containerWidth = 600;

		var listView = new ListView
		{
			ItemsSource = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToList(),
		};

		var button = new Button
		{
			Content = "Below ListView",
			Name = "SiblingButton"
		};

		var pane1Content = new StackPanel
		{
			Children = { listView, button }
		};

		var SUT = new TwoPaneView
		{
			Width = containerWidth,
			Height = containerHeight,
			Pane1 = pane1Content,
			MinWideModeWidth = double.MaxValue,
			MinTallModeHeight = double.MaxValue,
		};

		await UITestHelper.Load(SUT);

		var buttonBounds = button.GetAbsoluteBounds();
		var sutBounds = SUT.GetAbsoluteBounds();

		Assert.IsTrue(buttonBounds.Bottom <= sutBounds.Bottom + 1,
			$"Button sibling of ListView in TwoPaneView Pane1 should be visible within TwoPaneView bounds. Button bottom: {buttonBounds.Bottom}, TwoPaneView bottom: {sutBounds.Bottom}. Issue #14133: infinite height constraint pushes Button off-screen.");
	}
}
