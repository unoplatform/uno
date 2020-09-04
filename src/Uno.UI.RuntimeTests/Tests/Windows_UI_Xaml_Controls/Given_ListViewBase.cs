using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#else
using Uno.UI;
#endif
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_ListViewBase
	{
		private ResourceDictionary _testsResources;

		private Style BasicContainerStyle => _testsResources["BasicListViewContainerStyle"] as Style;

		private DataTemplate TextBlockItemTemplate => _testsResources["TextBlockItemTemplate"] as DataTemplate;

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
		}

		[TestMethod]
		[RunsOnUIThread]
		public void ValidSelectionChange()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			list.SelectedItem = 5;
			Assert.AreEqual(list.SelectedItem, 5);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidSelectionChangeValidPrevious()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			list.SelectedItem = 17;
			Assert.AreEqual(list.SelectedItem, 3);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void InvalidSelectionChangeInvalidPrevious()
		{
			var source = Enumerable.Range(0, 10).ToArray();
			var list = new ListView { ItemsSource = source };
			list.SelectedItem = 3;
			Assert.AreEqual(list.SelectedItem, 3);
			source[3] = 13;
			list.SelectedItem = 17;
#if NETFX_CORE
			Assert.AreEqual(list.SelectedItem, 3);
#else
			Assert.IsNull(list.SelectedItem);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContainerSet_Then_ContentShouldBeSet()
		{
			var resources = new TestsResources();

			var SUT = new ListView
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = TextBlockItemTemplate
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForIdle();

			var source = new[] {
				"item 0",
			};

			SUT.ItemsSource = source;

			Assert.AreEqual(-1, SUT.SelectedIndex);

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			var tb = si.FindFirstChild<TextBlock>();
			Assert.AreEqual("item 0", tb?.Text);
		}


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_IsItsOwnItemContainer_FromSource()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				new ListViewItem(){ Content = "item 1" },
				new ListViewItem(){ Content = "item 2" },
				new ListViewItem(){ Content = "item 3" },
				new ListViewItem(){ Content = "item 4" },
			};

			SUT.ItemsSource = source;

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			Assert.AreEqual("item 1", si.Content);
		}


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NoItemTemplate()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = null,
				ItemTemplateSelector = null,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new[] {
				"Item 1"
			};

			SUT.ItemsSource = source;

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);

			Assert.AreEqual("Item 1", si.Content);
#if !NETFX_CORE // On iOS and Android (others not tested), ContentTemplateRoot is null, and TemplatedRoot is a ContentPresenter containing an ImplicitTextBlock
			return;
#endif

			Assert.IsInstanceOfType(si.ContentTemplateRoot, typeof(TextBlock));
			Assert.AreEqual("Item 1", (si.ContentTemplateRoot as TextBlock).Text);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_IsItsOwnItemContainer_FromSource_With_DataTemplate()
		{
			var SUT = new ListView()
			{
				ItemContainerStyle = BasicContainerStyle,
				ItemTemplate = TextBlockItemTemplate,
				SelectionMode = ListViewSelectionMode.Single,
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var source = new object[] {
				new ListViewItem(){ Content = "item 1" },
				"item 2"
			};

			SUT.ItemsSource = source;

			SelectorItem si = null;
			await WindowHelper.WaitFor(() => (si = SUT.ContainerFromItem(source[0]) as SelectorItem) != null);
			Assert.AreEqual("item 1", si.Content);
			Assert.AreSame(si, source[0]);
#if !NETFX_CORE
			Assert.IsFalse(si.IsGeneratedContainer);
#endif

			var si2 = SUT.ContainerFromItem(source[1]) as ListViewItem;

			Assert.IsNotNull(si2);
			Assert.AreNotSame(si2, source[1]);
#if NETFX_CORE
			Assert.AreEqual("item 2", si2.Content); 
#else
			Assert.AreEqual("item 2", si2.DataContext);
			Assert.AreEqual("item 2", (si2.Content as TextBlock).Text);
			Assert.IsTrue(si2.IsGeneratedContainer);
#endif
		}
	}
}
