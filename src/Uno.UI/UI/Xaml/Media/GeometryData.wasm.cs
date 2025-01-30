using System;
using Microsoft.UI.Xaml.Wasm;
using Uno.UI.Xaml;
using Uno.Xaml;

namespace Microsoft.UI.Xaml.Media
{
	public class GeometryData : Geometry
	{
		// This is a special class to support the "Data Path" string on Wasm platform.
		// Since the native underlying system (SVG) is using data paths "as is", there's
		// no need to parse it in managed code, except to extract the FillRule.

		// This special geometry will be created when setting a string data directly in XAML
		// like this:
		//
		// <Path Data="M0,0 L10,10 20,10 20,20, 10,20 Z" />

		private const FillRule DefaultFillRule = FillRule.EvenOdd;

		private readonly SvgElement _svgElement = new SvgElement("path");

		public string Data { get; }

		public FillRule FillRule { get; } = DefaultFillRule;

		public GeometryData()
		{
		}

		public GeometryData(string data)
		{
			(FillRule, Data) = ParseData(data);

			WindowManagerInterop.SetSvgPathAttributes(_svgElement.HtmlId, FillRule == FillRule.Nonzero, Data);
		}

		internal static (FillRule FillRule, string Data) ParseData(string data)
		{
			if (data == "F")
			{
				// uncompleted fill-rule block: missing value (just 'F' without 0/1 after)
				throw new XamlParseException($"Failed to create a 'Data' from the text '{data}'.");
			}

			if (data.Length >= 2 && TryExtractFillRule(data) is { } result)
			{
				return (result.Value, data[result.CurrentPosition..]);
			}
			else
			{
				return (DefaultFillRule, data);
			}
		}
		private static (FillRule Value, int CurrentPosition)? TryExtractFillRule(string data)
		{
			// XamlParseException: 'Failed to create a 'Data' from the text 'F2'.' Line number '1' and line position '7'.
			// "F1" just fill-rule without data is okay

			// syntax: [fillRule] moveCommand drawCommand [drawCommand*] [closeCommand]
			// Fill rule:
			//   There are two possible values for the optional fill rule: F0 or F1. (The F is always uppercase.)
			//   F0 is the default value; it produces EvenOdd fill behavior, so you don't typically specify it.
			//   Use F1 to get the Nonzero fill behavior. These fill values align with the values of the FillRule enumeration.
			// -- https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/move-draw-commands-syntax#the-basic-syntax

			// remark: despite explicitly stated: "The F is always uppercase", WinAppSDK is happily to accept lowercase 'f'.
			// remark: you can use any number of whitespaces before/inbetween/after fill-rule/commands/command-parameters.

			var inFillRule = false;
			for (int i = 0; i < data.Length; i++)
			{
				var c = data[i];

				if (char.IsWhiteSpace(c)) continue;
				if (inFillRule)
				{
					if (c is '1') return (FillRule.Nonzero, i + 1);
					if (c is '0') // legacy uno behavior would be to use an `else` instead here
						return (FillRule.EvenOdd, i + 1);

					throw new XamlParseException($"Failed to create a 'Data' from the text '{data}'.");
				}
				else if (c is 'F' or 'f')
				{
					inFillRule = true;
				}
				else
				{
					return null;
				}
			}

			if (inFillRule)
			{
				// uncompleted fill-rule block: missing value (just 'F' without 0/1 after)
				throw new XamlParseException($"Failed to create a 'Data' from the text '{data}'.");
			}
			return null;
		}

		internal override SvgElement GetSvgElement() => _svgElement;

		// There is no .ToPathData() on GeometryData, because it's not possible
		// to have this in a GeometryGroup.
	}
}
