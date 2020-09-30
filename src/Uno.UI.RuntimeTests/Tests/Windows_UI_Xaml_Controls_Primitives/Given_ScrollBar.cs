using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ScrollBar
	{
		[TestMethod]
		public void When_Value_Changed()
		{
			var sb = new ScrollBar() { Maximum = 30 };
			var timesCalled = 0;
			var newValue = double.NaN;
			sb.ValueChanged += (o, e) =>
			{
				timesCalled++;
				newValue = e.NewValue;
			};

			sb.Value = 22;

			Assert.AreEqual(1, timesCalled);
			Assert.AreEqual(22, newValue);
		}
	}
}
