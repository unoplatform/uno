using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;
using Windows.UI;
using FluentAssertions;
using FluentAssertions.Execution;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
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
#if !NETFX_CORE // This is an Uno implementation detail
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

			Assert.AreEqual(4, CounterGrid.CreationCount);
			Assert.AreEqual(4, CounterGrid2.CreationCount);
			Assert.AreEqual(4, CounterGrid.BindCount);
			Assert.AreEqual(4, CounterGrid2.BindCount);
		}

	}
	internal partial class ContentControlItemsControl : ItemsControl
	{
		protected override DependencyObject GetContainerForItemOverride() => new ContentControl();
	}
}
