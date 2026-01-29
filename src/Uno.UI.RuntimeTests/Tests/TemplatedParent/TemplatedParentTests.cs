#if __ANDROID__ || __APPLE_UIKIT__
// On droid and ios, ContentPresenter bypass can be potentially enabled (based on if a base control template is present, or not).
// As such, ContentPresenter may be omitted, and altering its descendants templated-parent too.
#define NEED_CUSTOM_ADJUSTMENTS_FOR_CP_BYPASS
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;
#if HAS_UNO
using Uno.UI.Xaml;
using Uno.UI.DataBinding;
#endif
using WindowHelper = Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent;

[TestClass]
[RunsOnUIThread]
public partial class TemplatedParentTests // tests
{
	[TestMethod]
	public async Task Uno8049_Test()
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
	public async Task Uno9059_Test()
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
	public async Task Uno12624_Test()
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
		""";
#if NEED_CUSTOM_ADJUSTMENTS_FOR_CP_BYPASS
		// skip ContentControls' ContentPresenter that were bypassed
		expectations = SkipLines(expectations, 4, 6, 9, 11);
#endif

		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task Uno17313_Test()
	{
		var setup = new Uno17313();
		await UITestHelper.Load(setup);

		var tree = setup.TreeGraph(DebugVT_TP);
		var expectations = """
		0	Uno17313_SettingsExpander // TP=<null>
		1		Uno17313_Expander // TP=Uno17313_SettingsExpander
		2			StackPanel // TP=Uno17313_Expander
		3				ContentControl // TP=Uno17313_Expander
		4					ContentPresenter // TP=ContentControl
		5						Uno17313_SettingsCard // TP=Uno17313_SettingsExpander
		6							Grid // TP=Uno17313_SettingsCard
		7								ContentPresenter#PART_HeaderPresenter // TP=Uno17313_SettingsCard
		8									ImplicitTextBlock // TP=ContentPresenter#PART_HeaderPresenter
		9								ContentPresenter#PART_ContentPresenter // TP=Uno17313_SettingsCard
		10									TextBlock // TP=<null>
		11				ContentPresenter // TP=Uno17313_Expander
		12					Border // TP=Uno17313_SettingsExpander
		""";
#if NEED_CUSTOM_ADJUSTMENTS_FOR_CP_BYPASS
		// skip ContentControls' ContentPresenter that were bypassed
		// ContentPresenter on line#11 is from the control template, so that doesn't count
		expectations = SkipLines(expectations, 4);
#endif

		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task ContentControl_Content_Test()
	{
		var setup = new ContentControl_Content();
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// direct content members should NOT have content-presenter as templated-parent.
		var tree = setup.SUT.TreeGraph(DebugVT_TP);
		var expectations = """
		0	ContentControl#SUT // TP=<null>
		1		ContentPresenter // TP=ContentControl#SUT
		2			StackPanel // TP=<null>
		3				TextBlock // TP=<null>
		""";
#if NEED_CUSTOM_ADJUSTMENTS_FOR_CP_BYPASS
		// skip ContentControls' ContentPresenter that were bypassed
		expectations = SkipLines(expectations, 1);
#endif
		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task ContentControl_ContentTemplate_Test()
	{
		var setup = new ContentControl_ContentTemplate();
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// data-template members should have content-presenter as templated-parent.
		var tree = setup.SUT.TreeGraph(DebugVT_TP);
		var expectations =
#if !NEED_CUSTOM_ADJUSTMENTS_FOR_CP_BYPASS
		"""
		0	ContentControl#SUT // TP=<null>
		1		ContentPresenter // TP=ContentControl#SUT
		2			StackPanel // TP=ContentPresenter
		3				TextBlock // TP=ContentPresenter
		""";
#else
		// because of content-presenter bypass,
		// we wont have ContentPresenter in the tree,
		// and the ContentTemplate descendants will not have a templated-parent.
		"""
		0	ContentControl#SUT // TP=<null>
		1		StackPanel // TP=<null>
		2			TextBlock // TP=<null>
		""";
#endif
		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task ItemsControl_HeaderFooter_NoTP_PreCompiledXaml()
	{
		var setup = new ItemsControl_HeaderFooter();
		var sut = setup.SUT;

		await ItemsControl_HeaderFooter_NoTP(setup, sut);
	}

	[TestMethod]
	public async Task ItemsControl_HeaderFooter_NoTP_DynamicXaml()
	{
		var sut = new ItemsControl()
		{
			ItemsPanel = XamlHelper.LoadXaml<ItemsPanelTemplate>("""
				<ItemsPanelTemplate>
					<StackPanel />
				</ItemsPanelTemplate>
			"""),
			Template = XamlHelper.LoadXaml<ControlTemplate>("""
				<ControlTemplate TargetType="ItemsControl">
					<ItemsPresenter Header="asd" />
				</ControlTemplate>
			"""),
		};

		await ItemsControl_HeaderFooter_NoTP(sut, sut);
	}

	private async Task ItemsControl_HeaderFooter_NoTP(FrameworkElement setup, ItemsControl sut)
	{
		await UITestHelper.Load(setup, x => x.IsLoaded);

		var tree = sut.TreeGraph(DebugVT_TP);
		var presenter = sut.FindFirstDescendantOrThrow<ItemsPresenter>();
		var children = presenter.GetChildren().ToArray();

		Assert.IsInstanceOfType<ContentControl>(children[0], "Header ContentControl not found.");
		Assert.IsInstanceOfType<StackPanel>(children[1], "StackPanel not found.");
		Assert.IsInstanceOfType<ContentControl>(children[2], "Footer ContentControl not found.");

		Assert.IsNull(GetTemplatedParentCompat(children[0] as FrameworkElement), "Injected Header ContentControl should not have a templated-parent.");
		Assert.IsNull(GetTemplatedParentCompat(children[2] as FrameworkElement), "Injected Header ContentControl should not have a templated-parent.");
	}

	[TestMethod]
	public async Task ItemsControl_ItemTemplate_Test()
	{
		var setup = new ItemsControl_ItemTemplate();
		setup.SUT.ItemsSource = new[] { 1, 2, 3 };
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// item-template members should have content-presenter as templated-parent.
		var tree = setup.SUT.TreeGraph(DebugVT_TP);
		var expectations = """
		0	ItemsControl#SUT // TP=<null>
		1		ItemsPresenter // TP=ItemsControl#SUT
		2			ContentControl // TP=<null>
		3				ContentPresenter // TP=ContentControl
		4			StackPanel // TP=ItemsPresenter
		5				ContentPresenter // TP=<null>
		6					TextBlock // TP=ContentPresenter
		7				ContentPresenter // TP=<null>
		8					TextBlock // TP=ContentPresenter
		9				ContentPresenter // TP=<null>
		10					TextBlock // TP=ContentPresenter
		11			ContentControl // TP=<null>
		12				ContentPresenter // TP=ContentControl
		""";
#if NEED_CUSTOM_ADJUSTMENTS_FOR_CP_BYPASS
		// skip ContentControls' ContentPresenter that were bypassed
		expectations = SkipLines(expectations, 3, 12);
#endif
		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task VisualStateGroup_TP_Inheritance()
	{
		var setup = new VisualStateGroup_Full();
		await UITestHelper.Load(setup, x => x.IsLoaded);

		var tree = setup.SUT.TreeGraph(DebugVT_TPTV, TraverseVSG);
		var expectations = """
		0	ContentControl#SUT // TP=<null>, TV=<null>
		1		VisualStateGroup // TV=ContentControl#SUT
		2			VisualState // TV=ContentControl#SUT
		3			VisualState // TV=String
		4				Setter // TV=ContentControl#SUT
		5				Storyboard // TV=ContentControl#SUT
		6					ObjectAnimationUsingKeyFrames // TV=ContentControl#SUT
		7					DoubleAnimation // TV=ContentControl#SUT
		8			VisualTransition // TV=ContentControl#SUT
		9				Storyboard // TV=ContentControl#SUT
		10					DoubleAnimation // TV=ContentControl#SUT
		11		Border#TemplateRoot // TP=ContentControl#SUT, TV=<null>
		12			Grid#ContentElement // TP=ContentControl#SUT, TV=<null>
		""";
		VerifyTree(expectations, setup, checkVSG: true);
	}

	[TestMethod]
	public Task LateTemplateSwapping_NonContentControl() => LateTemplateSwapping<TextBox>();

	[TestMethod]
	public Task LateTemplateSwapping_ContentControl() => LateTemplateSwapping<ContentControl>();

	public async Task LateTemplateSwapping<TControl>() where TControl : Control, new()
	{
		var templateA = XamlHelper.LoadXaml<ControlTemplate>("""
			<ControlTemplate>
				<Grid x:Name="RootA" Width="150" Height="50" Background="SkyBlue">
					<TextBlock>Template A</TextBlock>
				</Grid>
			</ControlTemplate>
		""");
		var templateB = XamlHelper.LoadXaml<ControlTemplate>("""
			<ControlTemplate>
				<Grid x:Name="RootB" Width="150" Height="50" Background="Pink">
					<TextBlock>Template B</TextBlock>
				</Grid>
			</ControlTemplate>
		""");

		var sut = new TControl();

		sut.Template = templateA;
		await UITestHelper.Load(sut, x => x.IsLoaded);
		sut.FindFirstDescendantOrThrow<Grid>("RootA");

		sut.Template = templateB;
		await UITestHelper.WaitForIdle();
		sut.FindFirstDescendantOrThrow<Grid>("RootB");
	}

	[TestMethod]
	public async Task Uno19264_Test()
	{
		var setup = new Uno19264();
		await UITestHelper.Load(setup);

		// force lazily load element to load, and wait until done
		var lazy = setup.HostButton.FindName("LazyContentControl") as ContentControl ?? throw new Exception("failed to ContentControl#LazyContentControl");
		await UITestHelper.WaitForLoaded(lazy, x => x.IsLoaded);

		/* var tree = setup.TreeGraph();
		Uno19264 // TP=null, DC=null, Content=Button
			Button // TP=null, DC=null, Content=String
				StackPanel // TP=Button, DC=null
					TextBlock // TP=Button, DC=null, Text=String
					ContentControl#LazyContentControl // TP=Button, DC=null, Content=ContentPresenter
						ContentPresenter // TP=ContentControl#LazyContentControl, DC=null, Content=ContentPresenter, Content.bind=[Path=Content, TemplatedParent]
							ContentPresenter#LazyDescendant // TP=Button, DC=String, Content=String, Content.bind=[Path=Content, TemplatedParent]
								ImplicitTextBlock // TP=ContentPresenter, DC=String, Text=String, Text.bind=[Path=Content, TemplatedParent]
					TextBlock // TP=Button, DC=null, Text=String
		 */

		Assert.AreEqual(setup.HostButton, GetTemplatedParentCompat(lazy), "The lazy element didnt receive the correct templated-parent.");

		var descendent = setup.FindFirstDescendantOrThrow<ContentPresenter>("LazyDescendant");
		Assert.AreEqual(setup.HostButton, GetTemplatedParentCompat(descendent), "The lazy descendant didnt receive the correct templated-parent.");
		Assert.AreEqual(setup.HostButton.Content, descendent.Content, "The lazy descendant didnt have its template-binding applied correctly");
	}
}
public partial class TemplatedParentTests // helper methods
{
	private static string SkipLines(string tree, params int[] lines)
	{
		return string.Join('\n', tree.Split('\n')
			.Where((x, i) => !lines.Contains(i))
		);
	}

	private static IEnumerable<T> FlattenHierarchy<T>(T node, Func<T, IEnumerable<T>> getChildren)
	{
		foreach (var child in getChildren(node))
		{
			yield return child;
			foreach (var nested in FlattenHierarchy(child, getChildren))
			{
				yield return nested;
			}
		}
	}

	private static void VerifyTree(string expectedTree, FrameworkElement root, bool checkVSG = false)
	{
		var expectations = expectedTree.Split('\n', StringSplitOptions.TrimEntries);
		var descendants = checkVSG
			? FlattenHierarchy<object>(root, TraverseChildrenAndVSG).ToArray()
			: root.EnumerateDescendants().ToArray();
		Func<object, IEnumerable<string>> debugVT = checkVSG ? DebugVT_TPTV : DebugVT_TP;


		Assert.AreEqual(expectations.Length, descendants.Length, "Mismatched descendant size");
		for (int i = 0; i < expectations.Length; i++)
		{
			var line = expectations[i].TrimStart("\t 0123456789.".ToArray());
			var parts = line.Split(" // ", count: 2);

			var node = descendants[i];
			var actual = string.Join(", ", debugVT(node));

			Assert.HasCount(2, parts, $"Failed to parse expectation on line {i}: {expectations[i]}");
			Assert.AreEqual(parts[0], DescribeObject(node), $"Invalid node on line {i}");
			Assert.AreEqual(parts[1], actual, $"Invalid property(ies) on line {i}");
		}
	}


	private static object GetTemplatedParentCompat(FrameworkElement fe)
	{
		if (fe is null)
		{
			return null;
		}

#if HAS_UNO
		return fe.GetTemplatedParent();
#else
		fe.SetBinding(FrameworkElement.TagProperty, new Binding
		{
			RelativeSource = new() { Mode = RelativeSourceMode.TemplatedParent }
		});
		var tp = fe.Tag;

		fe.ClearValue(FrameworkElement.TagProperty);
		return tp;
#endif
	}
}
public partial class TemplatedParentTests // TreeGraph helper methods
{
	private static IEnumerable<string> DebugVT_TP(object x)
	{
		if (x is FrameworkElement fe)
		{
			yield return $"TP={DescribeObject(GetTemplatedParentCompat(fe))}";
		}
	}
	private static IEnumerable<string> DebugVT_TPTV(object x)
	{
		if (x is FrameworkElement fe)
		{
			yield return $"TP={DescribeObject(GetTemplatedParentCompat(fe))}";
		}
		if (x is DependencyObject @do)
		{
			yield return $"TV={DescribeObject(TestDP.GetTestValue(@do))}";
		}
	}

	private static IEnumerable<object> TraverseChildrenAndVSG(object o)
	{
		return TraverseVSG(o, o.AsNativeView()?.EnumerateChildren());
	}
	private static IEnumerable<object> TraverseVSG(object o, IEnumerable<object> children)
	{
		if (o is Control c && c.EnumerateChildren().FirstOrDefault() is FrameworkElement root)
		{
			foreach (var item in VisualStateManager.GetVisualStateGroups(root) ?? Array.Empty<VisualStateGroup>())
			{
				yield return item;
			}
		}
		if (o is VisualStateGroup vsg)
		{
			foreach (var item in vsg.States ?? Array.Empty<VisualState>())
			{
				yield return item;
			}
			foreach (var item in vsg.Transitions ?? Array.Empty<VisualTransition>())
			{
				yield return item;
			}
		}
		if (o is VisualState vs)
		{
			foreach (var item in vs.Setters?.AsEnumerable() ?? Array.Empty<Setter>())
			{
				yield return item;
			}
			if (vs.Storyboard is { })
			{
				yield return vs.Storyboard;
			}
		}
		if (o is VisualTransition vt)
		{
			if (vt.Storyboard is { })
			{
				yield return vt.Storyboard;
			}
		}
		if (o is Storyboard sb)
		{
			foreach (var item in sb.Children.AsEnumerable() ?? Array.Empty<Timeline>())
			{
				yield return item;
			}
		}

		foreach (var child in children ?? Array.Empty<object>())
		{
			yield return child;
		}
	}

	private static string DescribeObject(object x)
	{
		if (x == null) return "<null>";
		if (x is FrameworkElement { Name: { Length: > 0 } name }) return $"{x.GetType().Name}#{name}";
		return x.GetType().Name;
	}
}
