using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	public class Given_Path
	{
		[TestMethod]
		[RunsOnUIThread]
		public void Should_not_throw_if_Path_Data_is_set_to_null()
		{
			// This bug is an illustration of issue
			// https://github.com/unoplatform/uno/issues/6846

			// Set initial Data
			var SUT = new Path { Data = new RectangleGeometry() };

			// Switch back to null.  Should not throw an exception.
			SUT.Data = null;
		}
	}
}
