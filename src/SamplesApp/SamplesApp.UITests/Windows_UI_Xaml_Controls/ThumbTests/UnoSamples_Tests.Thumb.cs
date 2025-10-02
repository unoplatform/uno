using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ThumbTests
{
	[TestFixture]
	public partial class ThumbTests : SampleControlUITestBase
	{
		private const string _sample = "UITests.Windows_UI_Xaml_Controls.ThumbTests.Thumb_DragEvents";

		[Test]
		[AutoRetry]
		[Ignore("Test fails with Chrome 84")] // https://github.com/unoplatform/uno/issues/4097
		public void When_DragAndDrop()
		{
			Run(_sample);

			var originalLocation = _app.WaitForElement("TheThumb").Single().Rect;
			var dpiScale = originalLocation.Width / 20;

			_app.DragCoordinates(
				originalLocation.CenterX,
				originalLocation.CenterY,
				originalLocation.CenterX + 100 * dpiScale,
				originalLocation.CenterY + 100 * dpiScale);

			var start = GetResult("DragStartedOutput").Single().Value;
			var lastDelta = GetResult("DragDeltaOutput")["Δ"];
			var total = GetResult("DragCompletedOutput")["Σ"];

			IsCloseTo(start, 50, 50).Should().BeTrue();
			IsCloseTo(lastDelta, 1, 1, tolerance: 15).Should().BeTrue();
			IsCloseTo(total, 100, 100, tolerance: dpiScale * 1.5).Should().BeTrue();
		}

		public IDictionary<string, (double x, double y)> GetResult(string resultElementName)
		{
			var regex = new Regex(@"(?<value>(?<name>.)x=(?<x>\d+.\d{0,2}),.y=(?<y>\d+.\d{0,2}))");
			var raw = _app.Marked(resultElementName).GetDependencyPropertyValue<string>("Text");
			var result = regex.Match(raw);

			if (!result.Success)
			{
				throw new InvalidOperationException("Cannot parse result: " + raw);
			}

			return GetValues().ToDictionary(x => x.name, x => x.value);

			IEnumerable<(string name, (double x, double y) value)> GetValues()
			{
				while (result.Success)
				{
					var x = double.Parse(result.Groups["x"].Value, CultureInfo.InvariantCulture);
					var y = double.Parse(result.Groups["y"].Value, CultureInfo.InvariantCulture);

					yield return (result.Groups["name"].Value, (x, y));

					result = result.NextMatch();
				}
			}
		}

		private bool IsCloseTo((double x, double y) value, double x, double y, double tolerance = 1.5)
			=> Math.Abs(value.x - x) < tolerance
			&& Math.Abs(value.y - y) < tolerance;
	}
}
