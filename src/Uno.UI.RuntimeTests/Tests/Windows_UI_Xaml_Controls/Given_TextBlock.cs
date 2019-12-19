using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_TextBlock
	{
		[TestMethod]
		[RunsOnUIThread]
		public void Check_ActualWidth_After_Measure()
		{
			var SUT = new TextBlock { Text = "Some text" };
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.IsTrue(SUT.DesiredSize.Width > 0);
			Assert.IsTrue(SUT.DesiredSize.Height > 0);

			// For simplicity, currently we don't insist on a specific value here. The exact details of text measurement are highly
			// platform-specific, and additionally on UWP the ActualWidth and DesiredSize.Width are not exactly the same, a subtlety Uno
			// currently doesn't try to replicate.
			Assert.IsTrue(SUT.ActualWidth > 0);
			Assert.IsTrue(SUT.ActualHeight > 0);
		}
		[TestMethod]
		[RunsOnUIThread]
		public void Check_ActualWidth_After_Measure_Collapsed()
		{
			var SUT = new TextBlock { Text = "Some text" , Visibility = Visibility.Collapsed};
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.AreEqual(0, SUT.DesiredSize.Width);
			Assert.AreEqual(0, SUT.DesiredSize.Height);

			Assert.AreEqual(0, SUT.ActualWidth);
			Assert.AreEqual(0, SUT.ActualHeight);
		}
	}
}
