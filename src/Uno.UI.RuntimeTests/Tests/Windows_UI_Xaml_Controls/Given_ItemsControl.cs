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
#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
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

		[TestInitialize]
		public void Init()
		{
			_testsResources = new TestsResources();
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
	}
}
