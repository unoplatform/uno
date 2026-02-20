using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ComboBoxTests
{
	public partial class ComboBox_Pointers : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[Ignore("Flaky on iOS/Android native https://github.com/unoplatform/uno/issues/22688")]
		public void When_Tap_PressedReleasedAreHandled()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ComboBox.ComboBox_Pointers");

			QueryEx combo = "_combo";
			QueryEx output = "_output";

			_app.FastTap(combo);

			// Test sanity: close the combo
			_app.FastTap(combo);

			var rawOutput = output.GetDependencyPropertyValue<string>("Text");
			var outputParser = new Regex(@"\[(?<event>\w+)\]\s+(?<params>\|\s*(?<key>\w+)\s*=\s*(?<value>[\w\d]+))*");
			var parsedOutput = rawOutput
				.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(outputLine => outputParser.Match(outputLine))
				.Where(match => match.Success)
				.Select(match =>
				{
					using var keys = match.Groups["key"].Captures.Cast<Capture>().GetEnumerator();
					using var values = match.Groups["value"].Captures.Cast<Capture>().GetEnumerator();

					IEnumerable<(string key, string value)> GetParameters()
					{
						while (keys.MoveNext() && values.MoveNext())
						{
							yield return (keys.Current!.Value, values.Current!.Value);
						}
					}

					return (evt: match.Groups["event"].Value, @params: GetParameters().ToDictionary(pair => pair.key, pair => pair.value, StringComparer.OrdinalIgnoreCase));
				})
				.ToArray();

			Assert.IsTrue(
				parsedOutput.Any(o => o.evt == "PRESSED"),
				"At least one pressed");
			Assert.IsTrue(
				parsedOutput.Where(o => o.evt == "PRESSED").All(o => o.@params["handled"].Equals("True", StringComparison.OrdinalIgnoreCase)),
				"Pressed must be flagged as handled");
			Assert.IsTrue(
				parsedOutput.Any(o => o.evt == "RELEASED"),
				"At least one release");
			Assert.IsTrue(
				parsedOutput.Where(o => o.evt == "RELEASED").All(o => o.@params["handled"].Equals("True", StringComparison.OrdinalIgnoreCase)),
				"Pressed must be flagged as handled");
		}
	}
}
