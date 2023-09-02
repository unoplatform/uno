using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.Animations
{
#if false // requires WPF to run
	[TestClass]
	public class Given_KeySpline
	{
		[DataTestMethod]
		[DataRow(0.0, 0.0, 1.0, 1.0)]
		[DataRow(0.0, 1.0, 1.0, 0.0)]
		[DataRow(1.0, 1.0, 1.0, 1.0)]
		[DataRow(.25, .5, .5, .75)]
		[DataRow(1, 0.0, 0.95, 0.0)]
		[DataRow(1, 0.0, 1.0, 1.0)]
		[DataRow(0.1, 0.9, 0.2, 1.0)]
		[DataRow(0.6, 0.0, 0.9, 0.00)]
		[DataRow(0.2, 0.6, 0.3, 0.9)]
		[DataRow(0.1, 0.9, 0.2, 1)]
		[DataRow(.5, .1, .5, .9)]
		public void When_Matching(double x1, double y1, double x2, double y2)
		{
			var original = new System.Windows.Media.Animation.KeySpline() { ControlPoint1 = new System.Windows.Point(x1, y1), ControlPoint2 = new System.Windows.Point(x2, y2) };
			var originalResults = Compute(original.GetSplineProgress);

			var uno = new Windows.UI.Xaml.Media.Animation.KeySpline() { ControlPoint1 = new Point(x1, y1), ControlPoint2 = new Point(x2, y2) };
			var unoResults = Compute(uno.GetSplineProgress);

			string r1String = string.Join(" ", originalResults.Select(x => x.ToString(".000")));
			string r2String = string.Join(" ", unoResults.Select(x => x.ToString(".000")));
			Console.WriteLine($"[{x1};{y1}]/[{x2};{y2}]");
			Console.WriteLine($"{r1String}");
			Console.WriteLine($"{r2String}");

			// End value is not always 1 in WPF, so we skip it for now
			for (int i = 0; i < originalResults.Count - 1; i++)
			{
				// This is used for animations and while the precision is not exactly the same
				// the visible results difference is not noticeable.
				Assert.AreEqual(originalResults[i], unoResults[i], 0.005);
			}

			Assert.AreEqual(1.0, unoResults[originalResults.Count - 1], 0.00005);
		}

		private static List<double> Compute(Func<double, double> func)
		{
			var r = new List<double>();

			for (float i = 0; i < 1.05f; i += .05f)
			{
				r.Add(func(i));
			}

			return r;
		}
	}
#endif
}
