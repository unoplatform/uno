#nullable disable

using Windows.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI
{
	[TestClass]
	public class Given_Color
	{
		[TestMethod]
		public void When_UsingToString()
		{
			var color = new Color() { A = 255, R = 0, G = 128, B = 255 };

			Assert.AreEqual("#FF0080FF", color.ToString());
		}
	}
}
