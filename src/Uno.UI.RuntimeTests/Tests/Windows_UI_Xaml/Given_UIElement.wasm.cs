#if __WASM__
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
		public async Task When_HTMLElement_InternalElement()
		{
			var SUT = new Line();

			Assert.AreEqual("svg", SUT.HtmlTag);

		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HTMLElement_ExternalElement_NoOverride()
		{
			var SUT = new MyLine();

			Assert.AreEqual("svg", SUT.HtmlTag);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HTMLElement_ExternalElement_Override()
		{
			var SUT = new MyLineOverride();

			Assert.AreEqual("p", SUT.HtmlTag);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HTMLElement_ExternalElement_Override_Twice()
		{
			var SUT = new MyLine2();

			Assert.AreEqual("p", SUT.HtmlTag);
		}
	}

	public class MyLine : Line
	{

	}

	[HtmlElement("p")]
	public class MyLineOverride : Line
	{

	}


	public class MyLine2 : MyLineOverride
	{

	}
}
#endif
