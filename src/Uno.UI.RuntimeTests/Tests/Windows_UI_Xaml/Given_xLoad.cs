#nullable enable
#if !WINDOWS_UWP
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_xLoad
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_Literal()
		{
			var sut = new xLoad_Literal();

			TestServices.WindowHelper.WindowContent = sut;
			var loadBorderFalse = sut.LoadBorderFalse;
			var loadBorderTrue = sut.LoadBorderTrue;

			Assert.IsNull(loadBorderFalse);
			Assert.IsNotNull(loadBorderTrue);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_xBind()
		{
			var sut = new xLoad_xBind();

			TestServices.WindowHelper.WindowContent = sut;
			TestServices.WindowHelper.WaitForLoaded(sut);

			var loadBorder = sut.LoadBorder;
			Assert.IsNull(sut.LoadBorder);

			sut.IsLoad = true;

			Assert.IsNotNull(sut.LoadBorder);
			var parent = sut.LoadBorder.Parent as Border;

			sut.IsLoad = false;

			Assert.IsFalse((parent.Child as ElementStub).Load);
		}
	}
}
#endif
