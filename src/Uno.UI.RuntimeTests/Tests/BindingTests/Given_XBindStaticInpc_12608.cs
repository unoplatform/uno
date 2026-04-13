using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.XBindStaticInpcTests
{
	using Uno.UI.RuntimeTests.Tests;

	[TestClass]
	public class Given_XBindStaticInpc_12608
	{
		// Reproduction for https://github.com/unoplatform/uno/issues/12608
		// x:Bind with a static member in the path (e.g. {x:Bind local:MyApp.MyObj.Value, Mode=OneWay})
		// where MyApp is a static class holding an instance with INotifyPropertyChanged was reported
		// to not update on Skia when MyObj.Value changes (works on Windows).
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Static_Member_Root_INPC_Updates_Propagate_12608()
		{
			XBindStaticInpcApp_12608.MyObj = new XBindStaticInpcObject_12608 { Value = 0 };

			var page = new XBindStaticInpcPage_12608();
			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("0", page.BoundTextBlock.Text, "Initial bound text should reflect Value=0.");

			XBindStaticInpcApp_12608.MyObj.Value = 42;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(
				"42",
				page.BoundTextBlock.Text,
				"x:Bind (OneWay) through a static-class root should propagate INPC updates. " +
				"See https://github.com/unoplatform/uno/issues/12608");
		}
	}
}
