using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using AwesomeAssertions.Execution;
using Microsoft.UI.Xaml.Markup;
#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_ItemsControl
	{
		private ResourceDictionary _testsResources;

		private DataTemplate TextBlockItemTemplate => _testsResources["TextBlockItemTemplate"] as DataTemplate;

		private Style CounterItemsControlContainerStyle => _testsResources["CounterItemsControlContainerStyle"] as Style;

		private DataTemplate CounterItemTemplate => _testsResources["CounterItemTemplate"] as DataTemplate;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();

			CounterGrid.Reset();
			CounterGrid2.Reset();
		}


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContainerSet_Then_ContentShouldBeSet()
		{
			var resources = new TestsResources();

			var SUT = new ItemsControl
			{
				ItemTemplate = TextBlockItemTemplate
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			var source = new[] {
				"item 0",
			};

			SUT.ItemsSource = source;

			ContentPresenter cp = null;
			await WindowHelper.WaitFor(() => (cp = SUT.ContainerFromItem(source[0]) as ContentPresenter) != null);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("item 0", cp.Content);

			var tb = cp.FindFirstChild<TextBlock>();
			Assert.IsNotNull(tb);
			Assert.AreEqual("item 0", tb.Text);
		}

		public class ItemForDisplayMemberPath
		{
			public string DisplayName { get; set; }
			public override string ToString() => "This is .ToString() result - should not be used";
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SpecifyingDisplayMemberPath_Then_ContentShouldBeSet()
		{
			var resources = new TestsResources();

			var SUT = new ItemsControl
			{
				DisplayMemberPath = "DisplayName"
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			var source = new[]
			{
				new ItemForDisplayMemberPath {DisplayName = "item 0"},
				new ItemForDisplayMemberPath {DisplayName = "item 1"},
				new ItemForDisplayMemberPath {DisplayName = "item 2"}
			};

			SUT.ItemsSource = source;

			async Task Assert(int index, string s)
			{
				ContentPresenter cp = null;
				await WindowHelper.WaitFor(() => (cp = SUT.ContainerFromItem(source[index]) as ContentPresenter) != null);
				await WindowHelper.WaitForIdle();
#if !WINAPPSDK // This is an Uno implementation detail
				cp.Content.Should().Be(s, $"ContainerFromItem() at index {index}");
#endif

				var tb = cp.FindFirstChild<TextBlock>();
				tb.Should().NotBeNull($"Item at index {index}");
				tb.Text.Should().Be(s, $"TextBlock.Text at index {index}");
			}

			using var _ = new AssertionScope();

			await Assert(0, "item 0");
			await Assert(1, "item 1");
			await Assert(2, "item 2");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_IsItsOwnItemContainer_FromSource()
		{
			var SUT = new ItemsControl();

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new FrameworkElement[] {
				new TextBlock {Text = "item 1"},
				new ContentControl { Content = "item 2"},
				new Ellipse() {Fill = new SolidColorBrush(Colors.HotPink), Width = 50, Height = 50}
			};

			SUT.ItemsSource = source;

			TextBlock tb = null;
			await WindowHelper.WaitFor(() => (tb = SUT.ContainerFromItem(source[0]) as TextBlock) != null);

			Assert.AreEqual("item 1", tb.Text);

			SUT.ItemsSource = source;

			ContentControl cc = null;
			await WindowHelper.WaitFor(() => (cc = SUT.ContainerFromItem(source[1]) as ContentControl) != null);

			Assert.AreEqual("item 2", cc.Content);

			await WindowHelper.WaitFor(() => (SUT.ContainerFromItem(source[2]) as Ellipse) != null);
		}


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NoItemTemplate()
		{
			var SUT = new ItemsControl()
			{
				ItemTemplate = null,
				ItemTemplateSelector = null,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				"Item 1"
			};

			SUT.ItemsSource = source;

			ContentPresenter cp = null;
			await WindowHelper.WaitFor(() => (cp = SUT.ContainerFromItem(source[0]) as ContentPresenter) != null);

			Assert.AreEqual("Item 1", cp.Content);

			var tb = cp.FindFirstChild<TextBlock>();
			Assert.IsNotNull(tb);
			Assert.AreEqual("Item 1", tb.Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_Creation_Count_ItemsSource_Before_Load()
		{
			var source = new[] { "Zero", "One", "Two", "Three" };

			var SUT = new ContentControlItemsControl
			{
				ItemContainerStyle = CounterItemsControlContainerStyle,
				ItemTemplate = CounterItemTemplate,
				ItemsSource = source
			};

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, CounterGrid.CreationCount);
			Assert.AreEqual(0, CounterGrid2.CreationCount);

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(4, CounterGrid.CreationCount);
			Assert.AreEqual(4, CounterGrid2.CreationCount);
			Assert.AreEqual(4, CounterGrid.BindCount);
			Assert.AreEqual(4, CounterGrid2.BindCount);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_Creation_Count_ItemsSource_After_Load()
		{
			var SUT = new ContentControlItemsControl
			{
				ItemContainerStyle = CounterItemsControlContainerStyle,
				ItemTemplate = CounterItemTemplate
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, CounterGrid.CreationCount);
			Assert.AreEqual(0, CounterGrid2.CreationCount);

			var source = new[] { "Zero", "One", "Two", "Three" };

			SUT.ItemsSource = source;

			ContentControl cc = null;
			await WindowHelper.WaitFor(() => (cc = SUT.ContainerFromItem(source[0]) as ContentControl) != null);

			// Not required on WinUI.
			// In Uno, ContainerFromItem is available very early, making the above WaitFor completes synchronously.
			// In WinUI, ContainerFromItem is available more late, so WaitFor completes asynchronously allowing for a next layout pass to happen.
			// This should be okay for now.
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(4, CounterGrid.CreationCount);
			Assert.AreEqual(4, CounterGrid2.CreationCount);
			Assert.AreEqual(4, CounterGrid.BindCount);
			Assert.AreEqual(4, CounterGrid2.BindCount);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_ItemContainerStyle_TextBlock()
		{
			var containerStyle = (Style)XamlReader.Load(
			   @"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='TextBlock'> 
		                        <Setter Property='Foreground' Value='Green'/>
		                    </Style>");
			var itemStyle = (Style)XamlReader.Load(
			   @"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='TextBlock'> 
		                        <Setter Property='Foreground' Value='Red'/>
		                    </Style>");



			var source = new[]
			{
				new TextBlock { Text = "First" },
				new TextBlock { Text = "Second", Style = itemStyle },
				new TextBlock { Text = "Third" },
			};

			var SUT = new ItemsControl()
			{
				ItemContainerStyle = containerStyle,
				ItemsSource = source,
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			TextBlock firstTb = null;
			await WindowHelper.WaitFor(() => (firstTb = SUT.ContainerFromItem(source[0]) as TextBlock) != null);

			TextBlock secondTb = null;
			await WindowHelper.WaitFor(() => (secondTb = SUT.ContainerFromItem(source[1]) as TextBlock) != null);

			TextBlock thirdTb = null;
			await WindowHelper.WaitFor(() => (thirdTb = SUT.ContainerFromItem(source[2]) as TextBlock) != null);

			Assert.AreEqual(firstTb.Style, containerStyle);
			Assert.AreEqual(secondTb.Style, itemStyle);
			Assert.AreEqual(thirdTb.Style, containerStyle);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task Check_ItemContainerStyle_ContentControl()
		{
			var containerStyle = (Style)XamlReader.Load(
			   @"<Style xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
		   TargetType=""ContentControl"">
           <Setter Property=""Foreground"" Value=""Red""/>
		<Setter Property=""Template"">
			<Setter.Value>
				<ControlTemplate TargetType=""ContentControl"">
						<ContentPresenter Content=""{TemplateBinding Content}""
                                          Foreground=""{TemplateBinding Foreground}""
										  ContentTemplate=""{TemplateBinding ContentTemplate}"" />
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>");
			var itemStyle = (Style)XamlReader.Load(
			   @"<Style xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
		   TargetType=""ContentControl"">
           <Setter Property=""Foreground"" Value=""Green""/>
		<Setter Property=""Template"">
			<Setter.Value>
				<ControlTemplate TargetType=""ContentControl"">
						<ContentPresenter Content=""{TemplateBinding Content}""
                                          Foreground=""{TemplateBinding Foreground}""
										  ContentTemplate=""{TemplateBinding ContentTemplate}"" />
				</ControlTemplate>
			</Setter.Value>
		</Setter>
	</Style>");



			var source = new[]
			{
				new ContentControl { Content = "First"},
				new ContentControl { Content = "Second", Style = itemStyle},
				new ContentControl { Content = "Third"},
			};

			var SUT = new ItemsControl()
			{
				ItemContainerStyle = containerStyle,
				ItemsSource = source,
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			ContentControl first = null;
			await WindowHelper.WaitFor(() => (first = SUT.ContainerFromItem(source[0]) as ContentControl) != null);

			ContentControl second = null;
			await WindowHelper.WaitFor(() => (second = SUT.ContainerFromItem(source[1]) as ContentControl) != null);

			ContentControl third = null;
			await WindowHelper.WaitFor(() => (third = SUT.ContainerFromItem(source[2]) as ContentControl) != null);

			Assert.AreEqual(first.Style, containerStyle);
			Assert.AreEqual(second.Style, itemStyle);
			Assert.AreEqual(third.Style, containerStyle);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContentControl_ContainerRecycled_And_Explicit_Item()
		{
			var dataTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<TextBlock Text="{Binding}"/>
				</DataTemplate>
				""");

			var selector = new TestTemplateSelector();

			var source = new[]
			{
				new ContentControl { Content = "First", ContentTemplate = dataTemplate, ContentTemplateSelector = selector },
				new ContentControl { Content = "Second", ContentTemplate = dataTemplate, ContentTemplateSelector = selector },
				new ContentControl { Content = "Third", ContentTemplate = dataTemplate, ContentTemplateSelector = selector },
			};

			var SUT = new ItemsControl()
			{
				ItemsSource = source,
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			ContentControl first = null;
			await WindowHelper.WaitFor(() => (first = SUT.ContainerFromItem(source[0]) as ContentControl) != null);

			ContentControl second = null;
			await WindowHelper.WaitFor(() => (second = SUT.ContainerFromItem(source[1]) as ContentControl) != null);

			ContentControl third = null;
			await WindowHelper.WaitFor(() => (third = SUT.ContainerFromItem(source[2]) as ContentControl) != null);

			Assert.IsNotNull(first);
			Assert.IsNotNull(second);
			Assert.IsNotNull(third);
			Assert.IsNotNull(first.ContentTemplate);
			Assert.IsNotNull(second.ContentTemplate);
			Assert.IsNotNull(third.ContentTemplate);
			Assert.IsNotNull(first.ContentTemplateSelector);
			Assert.IsNotNull(second.ContentTemplateSelector);
			Assert.IsNotNull(third.ContentTemplateSelector);

			SUT.ItemsSource = null;

			Assert.AreEqual("First", source[0].Content);
			Assert.AreEqual("Second", source[1].Content);
			Assert.AreEqual("Third", source[2].Content);
			Assert.IsNotNull(first.ContentTemplate);
			Assert.IsNotNull(second.ContentTemplate);
			Assert.IsNotNull(third.ContentTemplate);
			Assert.IsNotNull(first.ContentTemplateSelector);
			Assert.IsNotNull(second.ContentTemplateSelector);
			Assert.IsNotNull(third.ContentTemplateSelector);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContentPresenter_ContainerRecycled_And_Explicit_Item()
		{
			var dataTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<TextBlock Text="{Binding}"/>
				</DataTemplate>
				""");

			var selector = new TestTemplateSelector();

			var source = new[]
			{
				new ContentPresenter { Content = "First", ContentTemplate = dataTemplate, ContentTemplateSelector = selector },
				new ContentPresenter { Content = "Second", ContentTemplate = dataTemplate, ContentTemplateSelector = selector },
				new ContentPresenter { Content = "Third", ContentTemplate = dataTemplate, ContentTemplateSelector = selector },
			};

			var SUT = new ItemsControl()
			{
				ItemsSource = source,
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			ContentPresenter first = null;
			await WindowHelper.WaitFor(() => (first = SUT.ContainerFromItem(source[0]) as ContentPresenter) != null);

			ContentPresenter second = null;
			await WindowHelper.WaitFor(() => (second = SUT.ContainerFromItem(source[1]) as ContentPresenter) != null);

			ContentPresenter third = null;
			await WindowHelper.WaitFor(() => (third = SUT.ContainerFromItem(source[2]) as ContentPresenter) != null);

			Assert.IsNotNull(first);
			Assert.IsNotNull(second);
			Assert.IsNotNull(third);
			Assert.IsNotNull(first.ContentTemplate);
			Assert.IsNotNull(second.ContentTemplate);
			Assert.IsNotNull(third.ContentTemplate);
			Assert.IsNotNull(first.ContentTemplateSelector);
			Assert.IsNotNull(second.ContentTemplateSelector);
			Assert.IsNotNull(third.ContentTemplateSelector);

			SUT.ItemsSource = null;

			Assert.AreEqual("First", source[0].Content);
			Assert.AreEqual("Second", source[1].Content);
			Assert.AreEqual("Third", source[2].Content);
			Assert.IsNotNull(first.ContentTemplate);
			Assert.IsNotNull(second.ContentTemplate);
			Assert.IsNotNull(third.ContentTemplate);
			Assert.IsNotNull(first.ContentTemplateSelector);
			Assert.IsNotNull(second.ContentTemplateSelector);
			Assert.IsNotNull(third.ContentTemplateSelector);
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContentPresenter_ContainerRecycled_And_ContentControl_Template()
		{
			var dataTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<ContentControl Content="{Binding}"/>
				</DataTemplate>
				""");

			var selector = new TestTemplateSelector();

			var source = new[]
			{
				"First",
				"Second",
			};

			var SUT = new ItemsControl()
			{
				ItemsSource = source,
				ItemTemplate = dataTemplate
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			{
				ContentPresenter first = null;
				await WindowHelper.WaitFor(() => (first = SUT.ContainerFromItem(source[0]) as ContentPresenter) != null);

				ContentPresenter second = null;
				await WindowHelper.WaitFor(() => (second = SUT.ContainerFromItem(source[1]) as ContentPresenter) != null);

				Assert.IsNotNull(first);
				Assert.IsNotNull(second);

				Assert.IsNotNull(first.Content);
				Assert.IsNotNull(second.Content);

				var firstInnerContent = first.ContentTemplateRoot as ContentControl;
				Assert.AreEqual(source[0], firstInnerContent?.Content);
				Assert.IsNotNull(firstInnerContent.GetBindingExpression(ContentControl.ContentProperty));

				var secondInnerContent = second.ContentTemplateRoot as ContentControl;
				Assert.AreEqual(source[1], secondInnerContent.Content);
				Assert.IsNotNull(secondInnerContent.GetBindingExpression(ContentControl.ContentProperty));
			}

			SUT.ItemsSource = null;
			await WindowHelper.WaitForIdle();

			SUT.ItemsSource = source;
			await WindowHelper.WaitForIdle();

			{
				ContentPresenter first = null;
				await WindowHelper.WaitFor(() => (first = SUT.ContainerFromItem(source[0]) as ContentPresenter) != null);

				ContentPresenter second = null;
				await WindowHelper.WaitFor(() => (second = SUT.ContainerFromItem(source[1]) as ContentPresenter) != null);

				Assert.IsNotNull(first);
				Assert.IsNotNull(second);

				Assert.IsNotNull(first.Content);
				Assert.IsNotNull(second.Content);

				var firstInnerContent = first.ContentTemplateRoot as ContentControl;
				Assert.AreEqual(source[0], firstInnerContent?.Content);
				Assert.IsNotNull(firstInnerContent.GetBindingExpression(ContentControl.ContentProperty));

				var secondInnerContent = second.ContentTemplateRoot as ContentControl;
				Assert.AreEqual(source[1], secondInnerContent.Content);
				Assert.IsNotNull(secondInnerContent.GetBindingExpression(ContentControl.ContentProperty));
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if __APPLE_UIKIT__
		[Ignore("Currently fails on iOS https://github.com/unoplatform/uno/issues/9080")]
#endif
		public async Task When_NestedItemsControl_RecycleTemplate()
		{
			var template = (DataTemplate)XamlReader.Load(@"
				<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<ItemsControl ItemsSource='{Binding NestedSource}'
								  BorderBrush='Black' BorderThickness='1'>
						<ItemsControl.ItemTemplate>
							<DataTemplate>
								<Rectangle Fill='Red' Height='50' Width='50' />
							</DataTemplate>
						</ItemsControl.ItemTemplate>
					</ItemsControl>
				</DataTemplate>
			".Replace('\'', '"'));
			var initialSource = new object[] { new { NestedSource = new object[1] }, };
			var resetSource = new object[] { new { NestedSource = new object[0] }, };
			var SUT = new ItemsControl()
			{
				ItemsSource = initialSource,
				ItemTemplate = template,
			};
			WindowHelper.WindowContent = SUT;

			var item = default(ContentPresenter);

			// [initial stage]: load the nested ItemsControl with items, so it has an initial height
			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitFor(() => (item = SUT.ContainerFromItem(initialSource[0]) as ContentPresenter) != null, message: "initial state: failed to find the item");
			Assert.AreEqual(50, item.ActualHeight, delta: 1.0, "initial state: expecting the item to have an starting height of 50");

			// [reset stage]: ItemsSource is reset with empty NestedSource, and we expected the height to be RE-measured
			SUT.ItemsSource = resetSource;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitFor(() => (item = SUT.ContainerFromItem(resetSource[0]) as ContentPresenter) != null, message: "reset state: failed to find the item");
			Assert.AreEqual(0, item.ActualHeight, "reset state: expecting the item's height to be remeasured to 0");
		}

	}

	internal partial class ContentControlItemsControl : ItemsControl
	{
		protected override DependencyObject GetContainerForItemOverride() => new ContentControl();
	}

	internal class TestTemplateSelector : DataTemplateSelector
	{
		public DataTemplate Template { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => Template;
	}
}
