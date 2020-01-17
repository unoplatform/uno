using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input
{
	internal class GestureResult
	{
		public static GestureResult Get(QueryEx textBlock)
			=> Parse(textBlock.GetDependencyPropertyValue<string>("Text"));

		public static GestureResult Parse(string text)
		{
			var regex = new Regex(@"(?<elt>[\w_]+)(-(?<state>\w+))?@(?<x>[\d\.]+),(?<y>[\d\.]+)");
			var result = regex.Match(text);
			if (!result.Success)
			{
				throw new ArgumentOutOfRangeException(nameof(text), $"Cannot parse '{text}'.");
			}

			return new GestureResult(
				result.Groups["elt"].Value,
				float.Parse(result.Groups["x"].Value, CultureInfo.InvariantCulture),
				float.Parse(result.Groups["y"].Value, CultureInfo.InvariantCulture),
				result.Groups["state"].Value);
		}

		private GestureResult(string element, float x, float y, string state)
		{
			Element = element;
			X = x;
			Y = y;
			State = state;
		}

		public string State { get; }

		public string Element { get; }

		public float X { get; }

		public float Y { get; }
	}
}
