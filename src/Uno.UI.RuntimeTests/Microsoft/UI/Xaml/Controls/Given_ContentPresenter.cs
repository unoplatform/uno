using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ContentPresenterPages;
using Windows.Foundation;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Extensions;
using System.Text.RegularExpressions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[RequiresFullWindow]
public partial class Given_ContentPresenter
{
	[TestMethod]
	public async Task When_Padding_Set_In_SizeChanged()
	{
		var SUT = new ContentPresenter()
		{
			Width = 200,
			Height = 200,
			Content = new Border()
			{
				Child = new Ellipse()
				{
					Fill = new SolidColorBrush(Colors.DarkOrange)
				}
			}
		};

		SUT.SizeChanged += (sender, args) => SUT.Padding = new Thickness(0, 200, 0, 0);

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(200, ((UIElement)VisualTreeHelper.GetChild(SUT, 0)).ActualOffset.Y);
	}

	[TestMethod]
	public void When_Content_Alignment_Set_Default_Alignment_Not_Overriden()
	{
		var contentPresenter = new ContentPresenter()
		{
			HorizontalContentAlignment = HorizontalAlignment.Center
		};
		var border = new Border();
		contentPresenter.Content = border;

		Assert.AreEqual(HorizontalAlignment.Center, contentPresenter.HorizontalContentAlignment);
		Assert.AreEqual(HorizontalAlignment.Stretch, border.HorizontalAlignment);
	}

	[TestMethod]
	public void When_Binding_And_DataContext_Same_As_Content()
	{
		var SUT = new ContentPresenter();
		var dataContextChangedCount = 0;

		SUT.DataContextChanged += (s, e) => dataContextChangedCount++;

		SUT.DataContext = new object();
		SUT.Content = SUT.DataContext;

		TestServices.WindowHelper.WindowContent = SUT;

		// This test ensures that the ContentPresenter does not reset
		// the DataContext to null and then back to the content and have
		// two-way bindings propagating the null value back to the source.
		Assert.AreEqual(1, dataContextChangedCount);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Content_Presenter_Empty()
	{
		var sut = new ContentPresenter_Content_DataContext();

		TestServices.WindowHelper.WindowContent = sut;

		Assert.AreEqual("", GetTextBlockText(sut, "emptyTest"));

		sut.emptyTestRoot.DataContext = "43";

		Assert.AreEqual("43", GetTextBlockText(sut, "emptyTest"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Content_Presenter_Priority()
	{
		var sut = new ContentPresenter_Content_DataContext();

		TestServices.WindowHelper.WindowContent = sut;

		Assert.AreEqual("43", GetTextBlockText(sut, "priorityTest"));

		sut.priorityTestRoot.DataContext = "44";
		Assert.AreEqual("44", GetTextBlockText(sut, "priorityTest"));

		sut.priorityTestRoot.Content = "45";
		Assert.AreEqual("45", GetTextBlockText(sut, "priorityTest"));

		sut.priorityTestRoot.DataContext = "46";
		Assert.AreEqual("46", GetTextBlockText(sut, "priorityTest"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Content_Presenter_SameValue()
	{
		var sut = new ContentPresenter_Content_DataContext();

		TestServices.WindowHelper.WindowContent = sut;

		Assert.AreEqual("42", GetTextBlockText(sut, "sameValueTest"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Content_Presenter_Inheritance()
	{
		var sut = new ContentPresenter_Content_DataContext();

		TestServices.WindowHelper.WindowContent = sut;

		Assert.AreEqual("DataContext", GetTextBlockText(sut, "inheritanceTest"));

		sut.inheritanceTestRoot.DataContext = "46";
		Assert.AreEqual("46", GetTextBlockText(sut, "inheritanceTest"));

		sut.inheritanceTestRoot.DataContext = "47";
		Assert.AreEqual("47", GetTextBlockText(sut, "inheritanceTest"));

		sut.inheritanceTestInner.DataContext = "48";
		Assert.AreEqual("48", GetTextBlockText(sut, "inheritanceTest"));

		sut.inheritanceTestRoot.DataContext = "49";
		Assert.AreEqual("48", GetTextBlockText(sut, "inheritanceTest"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Inside_ContentControl_Template()
	{
		var control = new CP_InCCTemplate();

		await UITestHelper.Load(control);

		var cc1 = control.FindName("CCWithContentTemplateSelectorAndContent") as ContentControl;
		var presenter1 = cc1.FindVisualChildByType<ContentPresenter>();
		var border1 = presenter1.FindVisualChildByType<Border>();
		var tb1 = presenter1.FindVisualChildByType<TextBlock>();

		Assert.AreEqual(new SolidColorBrush(Microsoft.UI.Colors.LightGreen), border1.Background);
		Assert.AreEqual("Item 1", tb1.Text);

		var cc2 = control.FindName("CCWithContentTemplate") as ContentControl;
		var presenter2 = cc2.FindVisualChildByType<ContentPresenter>();
		var border2 = presenter2.FindVisualChildByType<Border>();
		var tb2 = presenter2.FindVisualChildByType<TextBlock>();

		Assert.AreEqual(new SolidColorBrush(Microsoft.UI.Colors.LightPink), border2.Background);
		Assert.AreEqual("Item 2", tb2.Text);

		var cc3 = control.FindName("CCWithContentTemplateAndContent") as ContentControl;
		var presenter3 = cc3.FindVisualChildByType<ContentPresenter>();
		var border3 = presenter3.FindVisualChildByType<Border>();
		var tb3 = presenter3.FindVisualChildByType<TextBlock>();

		Assert.AreEqual(new SolidColorBrush(Microsoft.UI.Colors.LightGreen), border3.Background);
		Assert.AreEqual("Item 3", tb3.Text);

		var cc4 = control.FindName("CCWithContent") as ContentControl;
		var presenter4 = cc4.FindVisualChildByType<ContentPresenter>();
		var tb4 = presenter4.FindVisualChildByType<TextBlock>();

		Assert.AreEqual("Item 4", tb4.Text);

	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Content_Presenter_SameValue_Changing()
	{
		var sut = new ContentPresenter_Content_DataContext();

		TestServices.WindowHelper.WindowContent = sut;

		Assert.AreEqual("DataContext", GetTextBlockText(sut, "sameValueChangingTest"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_Content_Presenter_Null_Content_Changed()
	{
		var sut = new ContentPresenter_Content_DataContext();

		TestServices.WindowHelper.WindowContent = sut;

		Assert.AreEqual("42", GetTextBlockText(sut, "nullContentChanged"));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_ContentPresenter_ContentBindingNonPath()
	{
		var setup = new ContentPresenter_ContentBindings();

		await UITestHelper.Load(setup, x => x.IsLoaded);

#if false // WinAppSdk result for reference:
		StackPanel#SutPanel // DC=TestData
			ContentPresenter#CP_Content_Binding // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
				TextBlock // DC=TestData, Text='redacted47.TestData'
			ContentPresenter#CP_Content_Binding_SomePath // DC=TestData, Content=, Content.Binding=[Path=SomePath, RelativeSource=]

			ContentControl#CC_ContentTemplate_RawCP // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
				ContentPresenter // DC=TestData, Content=TestData
					ContentPresenter // DC=TestData, Content=
			ContentControl#CP_ContentTemplate_CPContentBinding // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
				ContentPresenter // DC=TestData, Content=TestData
					ContentPresenter // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
						TextBlock // DC=TestData, Text='redacted47.TestData'
			Button#Button_ControlTemplate_CPContentBinding // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
				ContentPresenter // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
					TextBlock // DC=TestData, Text='redacted47.TestData'

			ContentControl#CP_ContentTemplate_CPContentBindingPath // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
				ContentPresenter // DC=TestData, Content=TestData
					ContentPresenter // DC=String, Content=String, Content.Binding=[Path=Text, RelativeSource=]
						TextBlock // DC=String, Text='lalala~'
			Button#Button_ControlTemplate_CPContentBindingPath // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
				ContentPresenter // DC=String, Content=String, Content.Binding=[Path=Text, RelativeSource=]
					TextBlock // DC=String, Text='lalala~'
		^ main distinction should be TextBlock vs ImplicitTextBlock, and the implicit ContentPresenter's content uses binding on uno vs direct assignment.
#endif

		static IEnumerable<string> Describe(object x)
		{
			//if ((x as DependencyObject)? is { } dos)
			//{
			//	yield return $"TP={PrettyPrint.FormatType(dos.GetTemplatedParent())}";
			//}
			if (x is FrameworkElement fe)
			{
				yield return $"DC={PrettyPrint.FormatType(fe.DataContext)}";
			}
			if (x is ContentControl cc)
			{
				yield return $"Content={PrettyPrint.FormatType(cc.Content)}";
				if (cc.GetBindingExpression(ContentControl.ContentProperty)?.ParentBinding is { } b)
				{
					yield return $"Content.Binding=[Path={b.Path?.Path}, RelativeSource={b.RelativeSource?.Mode}]";
				}
			}
			if (x is ContentPresenter cp)
			{
				yield return $"Content={PrettyPrint.FormatType(cp.Content)}";
				if (cp.GetBindingExpression(ContentPresenter.ContentProperty)?.ParentBinding is { } b)
				{
					yield return $"Content.Binding=[Path={b.Path?.Path}, RelativeSource={b.RelativeSource?.Mode}]";
				}
			}
			if (x is TextBlock tb)
			{
				yield return $"Text='{tb.Text}'";
			}
		}
		var tree = setup.Content.TreeGraph(Describe);
		var expectedTree = """
		0	StackPanel#SutPanel // DC=TestData
		1		ContentPresenter#CP_Content_Binding // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		2			ImplicitTextBlock // DC=TestData, Text='TestData'
		3		ContentPresenter#CP_Content_Binding_SomePath // DC=TestData, Content=null, Content.Binding=[Path=SomePath, RelativeSource=]
		4		ContentControl#CC_ContentTemplate_RawCP // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		5			ContentPresenter // SKIP_DESC_COMPARE, IGNORE_FOR_MOBILE_CP_BYPASS
		6				ContentPresenter // DC=TestData, Content=null
		7		ContentControl#CP_ContentTemplate_CPContentBinding // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		8			ContentPresenter // SKIP_DESC_COMPARE, IGNORE_FOR_MOBILE_CP_BYPASS
		9				ContentPresenter // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		10					ImplicitTextBlock // DC=TestData, Text='TestData'
		11		Button#Button_ControlTemplate_CPContentBinding // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		12			ContentPresenter // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		13				ImplicitTextBlock // DC=TestData, Text='TestData'
		14		Button#Button_ControlTemplate_CPContentBindingPath // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		15			ContentPresenter // DC=TestData, Content=String, Content.Binding=[Path=Text, RelativeSource=]
		16				ImplicitTextBlock // DC=TestData, Text='lalala~'
		""";
		// fixme:
		//N+0		ContentControl#CP_ContentTemplate_CPContentBindingPath // DC=TestData, Content=TestData, Content.Binding=[Path=, RelativeSource=]
		//N+1			ContentPresenter // DC=TestData, Content=TestData
		//expected:
		//N+2				ContentPresenter // DC=String, Content=String, Content.Binding=[Path=Text, RelativeSource=]
		//N+3					ImplicitTextBlock // DC=String, Text='lalala~'
		//actual:
		//N+2				ContentPresenter // DC=String, Content='', Content.Binding=[Path=Text, RelativeSource=]
		//N+3					ImplicitTextBlock // DC=String, Text=''

		TreeAssert.VerifyTree(expectedTree, setup.Content, describe: Describe);
	}

	[TestMethod]
	public async Task When_ContentPresenter_ContentBindingPath()
	{
		var setup = new ContentPresenter_ContentBindingPath();

		await UITestHelper.Load(setup, x => x.IsLoaded);

		var sutCP = setup.FindFirstDescendantOrThrow<ContentPresenter>("SutContentPresenter");
		var implicitTB = sutCP.FindFirstDescendantOrThrow</*Implicit*/TextBlock>();

		Assert.AreEqual("Asd", sutCP.Content);
		Assert.AreEqual("Asd", implicitTB.Text);
	}

	public static IEnumerable<object[]> GetAlignments()
	{
		var configurations = new List<AlignmentTestConfiguration>();
		// Centered content

		foreach (var outerHorizontalAlignment in Enum.GetValues<HorizontalAlignment>())
		{
			foreach (var outerVerticalAlignment in Enum.GetValues<VerticalAlignment>())
			{
				foreach (var innerHorizontalAlignment in Enum.GetValues<HorizontalAlignment>())
				{
					foreach (var innerVerticalAlignment in Enum.GetValues<VerticalAlignment>())
					{
						double expectedX = 0;
						if (outerHorizontalAlignment == HorizontalAlignment.Center)
						{
							expectedX = 50;
						}
						else if (outerHorizontalAlignment == HorizontalAlignment.Right)
						{
							expectedX = 100;
						}
						else if (outerHorizontalAlignment == HorizontalAlignment.Stretch)
						{
							// Inner alignment matters only now
							if (innerHorizontalAlignment == HorizontalAlignment.Center)
							{
								expectedX = 50;
							}
							else if (innerHorizontalAlignment == HorizontalAlignment.Right)
							{
								expectedX = 100;
							}
						}

						double expectedY = 0;
						if (outerVerticalAlignment == VerticalAlignment.Center)
						{
							expectedY = 50;
						}
						else if (outerVerticalAlignment == VerticalAlignment.Bottom)
						{
							expectedY = 100;
						}
						else if (outerVerticalAlignment == VerticalAlignment.Stretch)
						{
							// Inner alignment matters only now
							if (innerVerticalAlignment == VerticalAlignment.Center)
							{
								expectedY = 50;
							}
							else if (innerVerticalAlignment == VerticalAlignment.Bottom)
							{
								expectedY = 100;
							}
						}

						double expectedWidth = 100;
						if (outerHorizontalAlignment == HorizontalAlignment.Stretch &&
							innerHorizontalAlignment == HorizontalAlignment.Stretch)
						{
							expectedWidth = 200;
						}

						double expectedHeight = 100;
						if (outerVerticalAlignment == VerticalAlignment.Stretch &&
							innerVerticalAlignment == VerticalAlignment.Stretch)
						{
							expectedHeight = 200;
						}

						configurations.Add(new AlignmentTestConfiguration(
							outerHorizontalAlignment,
							outerVerticalAlignment,
							innerHorizontalAlignment,
							innerVerticalAlignment,
							new Point(expectedX, expectedY),
							new Size(expectedWidth, expectedHeight)
						));
					}
				}
			}
		}

		return configurations.Select(c => new object[] { c });
	}

	[TestMethod]
	[DynamicData(nameof(GetAlignments))]
	public async Task When_Content_Aligned_Position_And_Size(AlignmentTestConfiguration configuration)
	{
		var contentPresenter = new ContentPresenter()
		{
			HorizontalContentAlignment = configuration.OuterHorizontal,
			VerticalContentAlignment = configuration.OuterVertical,
			Width = 200,
			Height = 200,
			Background = new SolidColorBrush(Colors.Red)
		};
		var border = new Border()
		{
			HorizontalAlignment = configuration.InnerHorizontal,
			VerticalAlignment = configuration.InnerVertical,
			Background = new SolidColorBrush(Colors.Blue),
			MinWidth = 100,
			MinHeight = 100
		};
		contentPresenter.Content = border;
		TestServices.WindowHelper.WindowContent = contentPresenter;

		await TestServices.WindowHelper.WaitForLoaded(contentPresenter);
		await TestServices.WindowHelper.WaitForIdle();

		var transform = border.TransformToVisual(contentPresenter);
		var point = transform.TransformPoint(new Point());
		Assert.AreEqual(configuration.ExpectedPosition, point);
		Assert.AreEqual(configuration.ExpectedSize, new Size(border.ActualWidth, border.ActualHeight));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Content_Unset_Release()
	{
		var SUT = new ContentPresenter();

		TestServices.WindowHelper.WindowContent = SUT;

		var wref = SetContent();
		Assert.AreEqual(wref.Target, SUT.DataContext);

		SUT.Content = null;

		await TestServices.WindowHelper.WaitForIdle();

		await AssertCollectedReference(wref);

		WeakReference SetContent()
		{
			var o = new object();
			SUT.Content = o;
			return new(o);
		}
	}

#if __SKIA__
	[TestMethod]
	public async Task When_Native_Host_Infinite_Measure_Bounds()
	{
		var SUT = new ContentPresenter();
		SUT.Content = SUT.CreateSampleComponent("test");

		if (SUT.Content is null)
		{
			Assert.Inconclusive("This platform does not support native element hosting.");
		}

		await UITestHelper.Load(new StackPanel()
		{
			Children =
			{
				SUT
			},
			Width = 200
		}, sp => sp.IsLoaded);

		Assert.AreEqual(200, SUT.ActualWidth);
		SUT.ActualHeight.Should().BeLessThanOrEqualTo(200); // WPf returns 200, everywhere else returns 0
	}
#endif
}
public partial class Given_ContentPresenter
{
	private static string GetTextBlockText(FrameworkElement sut, string v)
		=> (sut.FindName(v) as TextBlock)?.Text ?? "";

	private async Task AssertCollectedReference(WeakReference reference)
	{
		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(3))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();

			if (!reference.IsAlive)
			{
				return;
			}

			await Task.Delay(100);
		}

		Assert.IsFalse(reference.IsAlive);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	[DataRow("ListView", false)]
	[DataRow("ListView", true)]
	[DataRow("GridView", false)]
	[DataRow("GridView", true)]
	[DataRow("FlipView", false)]
	[DataRow("ComboBox", false)]
	[DataRow("ItemsControl", false)]
	public async Task When_ItemTemplateSelector_Then_No_Placeholder(string kind, bool customContainerTemplate)
	{
		var item = new ToStringRecordingItem();
		var selector = new RecordingSelector();

		ItemsControl SUT = kind switch
		{
			"ListView" => new ListView(),
			"GridView" => new GridView(),
			"FlipView" => new FlipView { Width = 200, Height = 200 },
			"ComboBox" => new ComboBox(),
			_ => new ItemsControl(),
		};
		SUT.ItemTemplateSelector = selector;
		SUT.ItemsSource = new[] { item };

		if (customContainerTemplate)
		{
			SUT.ItemContainerStyle = LoadBareContainerStyle(kind == "ListView" ? "ListViewItem" : "GridViewItem");
		}

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		if (SUT is ComboBox comboBox)
		{
			comboBox.IsDropDownOpen = true;
			await TestServices.WindowHelper.WaitForIdle();
		}

		var root = SUT.ContainerFromIndex(0) is ContentControl container
			? container.ContentTemplateRoot
			: VisualTreeUtils.FindVisualChildByType<Border>(SUT);

		Assert.IsNotNull(root, "The selected template was not materialized.");
		Assert.AreEqual(0, item.ToStringCalls, $"The item was rendered as text: {item.LastStack}");

		// An ItemsControl's own presenter gets its selector before its Content: #25444.
		if (kind != "ItemsControl")
		{
			Assert.AreEqual(0, selector.NullItemCalls, "The selector was asked about a null item.");
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	[DataRow("Content=\"{TemplateBinding Content}\" ContentTemplate=\"{TemplateBinding ContentTemplate}\"")]
	[DataRow("Content=\"{TemplateBinding Content}\"")]
	[DataRow("ContentTemplate=\"{TemplateBinding ContentTemplate}\"")]
	[DataRow("")]
	[DataRow("Content=\"{TemplateBinding Content}\" ContentTemplateSelector=\"{TemplateBinding ContentTemplateSelector}\"")]
	public async Task When_ContentControl_Selector_Then_No_Placeholder(string attributes)
	{
		var item = new ToStringRecordingItem();
		var selector = new RecordingSelector();

		var SUT = new ContentControl
		{
			Template = XamlHelper.LoadXaml<ControlTemplate>($$"""
				<ControlTemplate TargetType="ContentControl">
					<ContentPresenter {{attributes}} />
				</ControlTemplate>
				"""),
			ContentTemplateSelector = selector,
			Content = item,
		};

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(SUT.ContentTemplateRoot, "The selected template was not materialized.");
		Assert.AreEqual(0, item.ToStringCalls, $"The item was rendered as text: {item.LastStack}");
		Assert.AreEqual(0, selector.NullItemCalls, "The selector was asked about a null item.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	public async Task When_Stock_ContentControl_Selector_Then_No_Placeholder()
	{
		var item = new ToStringRecordingItem();
		var selector = new RecordingSelector();
		var SUT = new ContentControl { ContentTemplateSelector = selector, Content = item };

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(SUT.ContentTemplateRoot, "The selected template was not materialized.");
		Assert.AreEqual(0, item.ToStringCalls, $"The item was rendered as text: {item.LastStack}");
		Assert.AreEqual(0, selector.NullItemCalls, "The selector was asked about a null item.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	public async Task When_ContentControl_ContentTemplate_Not_Forwarded_Then_No_Placeholder()
	{
		var item = new ToStringRecordingItem();
		var SUT = new ContentControl
		{
			Template = XamlHelper.LoadXaml<ControlTemplate>("""
				<ControlTemplate TargetType="ContentControl">
					<ContentPresenter Content="{TemplateBinding Content}" />
				</ControlTemplate>
				"""),
			ContentTemplate = XamlHelper.LoadXaml<DataTemplate>("""
				<DataTemplate>
					<Border Width="50" Height="50" />
				</DataTemplate>
				"""),
			Content = item,
		};

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(SUT.ContentTemplateRoot, "The template was not materialized.");
		Assert.AreEqual(0, item.ToStringCalls, $"The item was rendered as text: {item.LastStack}");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	public async Task When_ListView_ItemTemplate_Not_Forwarded_Then_No_Placeholder()
	{
		var item = new ToStringRecordingItem();
		var SUT = new ListView
		{
			ItemContainerStyle = XamlHelper.LoadXaml<Style>("""
				<Style TargetType="ListViewItem">
					<Setter Property="Template">
						<Setter.Value>
							<ControlTemplate TargetType="ListViewItem">
								<ContentPresenter Content="{TemplateBinding Content}" />
							</ControlTemplate>
						</Setter.Value>
					</Setter>
				</Style>
				"""),
			ItemTemplate = XamlHelper.LoadXaml<DataTemplate>("""
				<DataTemplate>
					<Border Width="50" Height="50" />
				</DataTemplate>
				"""),
			ItemsSource = new[] { item },
		};

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(((ListViewItem)SUT.ContainerFromIndex(0)).ContentTemplateRoot, "The template was not materialized.");
		Assert.AreEqual(0, item.ToStringCalls, $"The item was rendered as text: {item.LastStack}");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	public async Task When_ListViewItem_No_Template_Then_Placeholder_Text()
	{
		var item = new ToStringRecordingItem();
		var SUT = new ListView { ItemsSource = new[] { item } };

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		var presenter = VisualTreeUtils.FindVisualChildByType<ContentPresenter>((ListViewItem)SUT.ContainerFromIndex(0));
		var textBlock = VisualTreeUtils.FindVisualChildByType<ImplicitTextBlock>(presenter);

		Assert.AreEqual(nameof(ToStringRecordingItem), textBlock.Text);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	public async Task When_Content_Has_Own_Binding_Then_Not_Replaced_By_Hookup()
	{
		var SUT = new ContentControl
		{
			Template = XamlHelper.LoadXaml<ControlTemplate>("""
				<ControlTemplate TargetType="ContentControl">
					<ContentPresenter Content="{Binding}" />
				</ControlTemplate>
				"""),
			Content = "from parent",
			DataContext = "from data context",
		};

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		var presenter = VisualTreeUtils.FindVisualChildByType<ContentPresenter>(SUT);
		Assert.AreEqual("from data context", presenter.Content);
		Assert.AreEqual("from data context", VisualTreeUtils.FindVisualChildByType<ImplicitTextBlock>(presenter).Text);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25919")]
	public async Task When_Implicit_Hookup_And_Null_Content_Then_No_Visuals()
	{
		var SUT = new ContentControl
		{
			Template = XamlHelper.LoadXaml<ControlTemplate>("""
				<ControlTemplate TargetType="ContentControl">
					<ContentPresenter />
				</ControlTemplate>
				"""),
		};

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		await TestServices.WindowHelper.WaitForIdle();

		var presenter = VisualTreeUtils.FindVisualChildByType<ContentPresenter>(SUT);
		Assert.AreEqual(0, VisualTreeHelper.GetChildrenCount(presenter));

		SUT.Content = "text";
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual("text", VisualTreeUtils.FindVisualChildByType<ImplicitTextBlock>(presenter).Text);
	}

	private static Style LoadBareContainerStyle(string targetType)
		=> XamlHelper.LoadXaml<Style>($$"""
			<Style TargetType="{{targetType}}">
				<Setter Property="Template">
					<Setter.Value>
						<ControlTemplate TargetType="{{targetType}}">
							<ContentPresenter Content="{TemplateBinding Content}" ContentTemplate="{TemplateBinding ContentTemplate}" />
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>
			""");

	private sealed class ToStringRecordingItem
	{
		public int ToStringCalls { get; private set; }

		public string LastStack { get; private set; }

		public override string ToString()
		{
			var stack = Environment.StackTrace;

			// Selector items legitimately use the text of their item as their automation name.
			if (!stack.Contains("GetPlainText"))
			{
				ToStringCalls++;
				LastStack = stack;
			}

			return nameof(ToStringRecordingItem);
		}
	}

	private sealed partial class RecordingSelector : DataTemplateSelector
	{
		private readonly DataTemplate _template = XamlHelper.LoadXaml<DataTemplate>("""
			<DataTemplate>
				<Border Width="50" Height="50" />
			</DataTemplate>
			""");

		public int NullItemCalls { get; private set; }

		protected override DataTemplate SelectTemplateCore(object item)
		{
			if (item is null)
			{
				NullItemCalls++;
				return null;
			}

			return _template;
		}

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
	}

	public class AlignmentTestConfiguration
	{
		public AlignmentTestConfiguration(HorizontalAlignment outerHorizontal, VerticalAlignment outerVertical, HorizontalAlignment innerHorizontal, VerticalAlignment innerVertical, Point expectedPosition, Size expectedSize)
		{
			OuterHorizontal = outerHorizontal;
			OuterVertical = outerVertical;
			InnerHorizontal = innerHorizontal;
			InnerVertical = innerVertical;
			ExpectedPosition = expectedPosition;
			ExpectedSize = expectedSize;
		}

		public HorizontalAlignment OuterHorizontal { get; }

		public VerticalAlignment OuterVertical { get; }

		public HorizontalAlignment InnerHorizontal { get; }

		public VerticalAlignment InnerVertical { get; }

		public Point ExpectedPosition { get; }

		public Size ExpectedSize { get; }

		public override string ToString()
		{
			return $"{OuterHorizontal}/{OuterVertical}/{InnerHorizontal}/{InnerVertical}/{ExpectedPosition}/{ExpectedSize}";
		}
	}
}
