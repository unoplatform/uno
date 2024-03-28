using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Uno.UI.Extensions;
using DependencyObjectExtensions = Uno.UI.Extensions.DependencyObjectExtensions;
using Windows.UI.Xaml.Shapes;
using Uno.UI.Runtime.WebAssembly;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	public partial class Given_UIElement
	{
		[TestMethod]
		[RunsOnUIThread]
		public void When_HTMLElement_InternalElement()
		{
			var SUT = new Line();

			Assert.AreEqual("svg", SUT.HtmlTag);

		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_HTMLElement_ExternalElement_NoOverride()
		{
			var SUT = new MyLine();

			Assert.AreEqual("svg", SUT.HtmlTag);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_HTMLElement_ExternalElement_Override()
		{
			var SUT = new MyLineOverride();

			Assert.AreEqual("p", SUT.HtmlTag);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HTMLElement_ExternalElement_Override_Then_IsHitTestable()
		{
			var SUT = new MyCustomComponent();

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => SUT.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(HitTestability.Visible, SUT.HitTestVisibility);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_HTMLElement_ExternalElement_Override_Twice()
		{
			var SUT = new MyLine2();

			Assert.AreEqual("p", SUT.HtmlTag);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_VisibilityCollapsed_Then_ScrollViewerIgnoresElement()
		{
			var item1 = new Border { Height = 128 };
			var item2 = new Border { Height = 4096, Visibility = Visibility.Collapsed };
			var sv = new ScrollViewer { Content = new Grid { Children = { item1, item2 } } };

			TestServices.WindowHelper.WindowContent = sv;
			await Render();
			var sut = sv.FindFirstChild<ScrollContentPresenter>();

			Assert.AreEqual(128.0, double.Parse(sut.GetProperty("scrollHeight")));

			item2.Visibility = Visibility.Visible;
			await Render();

			Assert.AreEqual(4096.0, double.Parse(sut.GetProperty("scrollHeight")));

			item2.Visibility = Visibility.Collapsed;
			await Render();

			Assert.AreEqual(128.0, double.Parse(sut.GetProperty("scrollHeight")));

			async Task Render()
			{
				await TestServices.WindowHelper.WaitForIdle();
				sv.InvalidateArrange();
				await TestServices.WindowHelper.WaitForIdle();
			}
		}
	}

	public class MyLine : Line
	{

	}

	[HtmlElement("p")]
	public class MyLineOverride : Line
	{

	}

	[HtmlElement("div")]
	public class MyCustomComponent : FrameworkElement
	{

	}


	public class MyLine2 : MyLineOverride
	{

	}
}
