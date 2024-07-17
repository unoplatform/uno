using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HarfBuzzSharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;
using Uno.UI.Xaml;
using WindowHelper = Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent;

[TestClass]
[RunsOnUIThread]
public partial class TemplatedParentTests
{
	[TestMethod]
	public async Task Uno8049_Test_AsdAsd000() // passed
	{
		var setup = new Uno8049();
		await UITestHelper.Load(setup);

		var tree = setup.TreeGraph(DebugVT_TP);
		var sut = setup.Content as TextBox ?? throw new Exception("Invalid content root");
		var cc = sut.FindFirstDescendantOrThrow<ContentControl>("HeaderTemplate_ContentControl");
		var tb = cc.FindFirstDescendantOrThrow<TextBlock>("Header_TextBlock");

		Assert.IsNotNull(sut.Header);
		Assert.AreEqual(sut.Header, tb);
	}

	[TestMethod]
	public async Task Uno9059_Test_AsdAsd001() // passed
	{
		try
		{
			var setup = new Uno9059();
			await UITestHelper.Load(setup);

			var sut = setup.Content as Uno9059_CustomControl ?? throw new Exception("Invalid content root");
			var button = sut.FindFirstDescendantOrThrow<Button>("CustomControl_Button");

			button.Flyout.ShowAt(button);
			await UITestHelper.WaitForIdle();

			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
			var presenter = popups.FirstOrDefault(x => x.Child is MenuFlyoutPresenter).Child as MenuFlyoutPresenter ??
				throw new Exception("Failed to find opened MenuFlyoutPresenter");
			var tree = presenter?.TreeGraph(DebugVT_TP);
			var mfi = presenter.FindFirstDescendantOrThrow<MenuFlyoutItem>("Button_MenuFlyoutItem");

			Assert.IsNotNull(sut.Action1);
			Assert.AreEqual(sut.Action1, mfi.Command);
		}
		finally
		{
			UITestHelper.CloseAllPopups();
		}
	}

	[TestMethod]
	[SuppressMessage("Performance", "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.", Justification = "...")]
	public async Task Uno12624_Test_AsdAsd002()
	{
		var setup = new Uno12624();
		await UITestHelper.Load(setup);

		var tree = setup.TreeGraph(DebugVT_TP);
		var expectations = """
		 0.	Uno12624_LeftRightControl#RootLeftRight // TP=<null>
		 1.		Uno12624_WestEastControl#LeftRightControl_Template_Root // TP=Uno12624_LeftRightControl#RootLeftRight
		 2.			StackPanel#WestEastControl_Template_RootPanel // TP=Uno12624_WestEastControl#LeftRightControl_Template_Root
		 3.				ContentControl#WestEastControl_Template_WestContent // TP=Uno12624_WestEastControl#LeftRightControl_Template_Root
		 4.					ContentPresenter // TP=ContentControl#WestEastControl_Template_WestContent
		 5.						ContentControl#LeftRightControl_Template_LeftContent // TP=Uno12624_LeftRightControl#RootLeftRight
		 6.							ContentPresenter // TP=ContentControl#LeftRightControl_Template_LeftContent
		 7.								TextBlock#LeftRightControl_Left // TP=<null>
		 8.				ContentControl#WestEastControl_Template_EastContent // TP=Uno12624_WestEastControl#LeftRightControl_Template_Root
		 9.					ContentPresenter // TP=ContentControl#WestEastControl_Template_EastContent
		10.						ContentControl#LeftRightControl_Template_RightContent // TP=Uno12624_LeftRightControl#RootLeftRight
		11.							ContentPresenter // TP=ContentControl#LeftRightControl_Template_RightContent
		12.								TextBlock#LeftRightControl_Right // TP=<null>
		""".Split('\n');
		var descendants = setup.EnumerateDescendants().ToArray();

		Assert.AreEqual(expectations.Length, descendants.Length);
		for (int i = 0; i < expectations.Length; i++)
		{
			var line = expectations[i];
			var match = Regex.Match(line, @"\s*\d+\.\s+(?<node>[\w#]+) // TP=(?<tp>[\w#<>]+)");

			var node = descendants[i];
			var tp = (node as FrameworkElement)?.GetTemplatedParent();

			Assert.AreEqual(match.Groups["node"].Value, DescribeObject(node));
			Assert.AreEqual(match.Groups["tp"].Value, DescribeObject(tp));
		}
	}
}

public partial class TemplatedParentTests
{
	private static IEnumerable<string> DebugVT_TP(object x)
	{
		if (x is FrameworkElement fe)
		{
			yield return $"TP={DescribeObject(fe.GetTemplatedParent())}";
		}
	}

	private static string DescribeObject(object x)
	{
		if (x == null) return "<null>";
		if (x is FrameworkElement { Name: { Length: > 0 } name }) return $"{x.GetType().Name}#{name}";
		return x.GetType().Name;
	}
}
