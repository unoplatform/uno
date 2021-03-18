using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.Foundation
{
	[TestClass]
	public class Given_Point
	{
		[TestMethod]
		public void When_NullComparison()
		{
			var p = new Point(0, 0);
			Assert.IsTrue(p != null);

			var p2 = new Point(1, 0);
			Assert.IsTrue(p2 != null);
		}
	}
}
