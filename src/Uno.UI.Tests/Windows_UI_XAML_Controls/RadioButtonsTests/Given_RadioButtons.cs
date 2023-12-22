using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tests.Windows_UI_XAML_Controls.RadioButtonsTests.Controls;

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.RadioButtonsTests
{
	[TestClass]
	public class Given_RadioButtons
	{
		[TestMethod]
		public void When_TemplateBinding()
		{
			var SUT = new When_RadioButtons_TemplateBinding();
			SUT.ForceLoaded();

			var repeater = SUT.FindName("InnerRepeater") as ItemsRepeater;
			Assert.IsNotNull(repeater);

			var layout = repeater.Layout as ColumnMajorUniformToLargestGridLayout;

			Assert.AreEqual(2, layout.MaxColumns);
		}
	}
}
