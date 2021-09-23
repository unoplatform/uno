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
	[RunsOnUIThread]
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

		[TestMethod]
		[RunsOnUIThread]
		public void When_xLoad_Visibility_While_Materializing()
		{
			var SUT = new When_xLoad_Visibility_While_Materializing();

			Assert.AreEqual(0, When_xLoad_Visibility_While_Materializing_Content.Instances);

			TestServices.WindowHelper.WindowContent = SUT;

			Assert.AreEqual(0, When_xLoad_Visibility_While_Materializing_Content.Instances);

			SUT.Model.IsVisible = true;

			Assert.AreEqual(1, When_xLoad_Visibility_While_Materializing_Content.Instances);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_xBind_xLoad_Initial()
		{
			var grid = new Grid();
			TestServices.WindowHelper.WindowContent = grid;

			var SUT = new When_xLoad_xBind_xLoad_Initial();
			grid.Children.Add(SUT);

			Assert.IsNotNull(SUT.tb01);
			Assert.AreEqual(1, SUT.tb01.Tag);

			SUT.Model.MyValue = 42;

			Assert.AreEqual(42, SUT.tb01.Tag);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_xLoad_xBind_xLoad_While_Loading()
		{
			var grid = new Grid();
			TestServices.WindowHelper.WindowContent = grid;

			var SUT = new When_xLoad_xBind_xLoad_While_Loading();
			grid.Children.Add(SUT);

			Assert.IsNotNull(SUT.tb01);
			Assert.AreEqual(1, SUT.tb01.Tag);

			SUT.Model.MyValue = 42;

			Assert.AreEqual(42, SUT.tb01.Tag);
		}
	}
}
#endif
