using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Helpers;

internal static partial class XamlHelper
{
	/// <summary>
	/// Matches right before the &gt; or \&gt; tail of any tag.
	/// </summary>
	/// <remarks>
	/// It will match an opening or closing or self-closing tag.
	/// </remarks>
	[GeneratedRegex(@"(?=( ?/)?>)")]
	private static partial Regex EndOfTagRegex();

	/// <summary>
	/// Matches any tag without xmlns prefix.
	/// </summary>
	[GeneratedRegex(@"<\w+[ />]")]
	private static partial Regex NonXmlnsTagRegex();

	private static readonly IReadOnlyDictionary<string, string> KnownXmlnses = new Dictionary<string, string>
	{
		[string.Empty] = "http://schemas.microsoft.com/winfx/2006/xaml/presentation",
		["x"] = "http://schemas.microsoft.com/winfx/2006/xaml",
		["toolkit"] = "using:Uno.UI.Toolkit", // uno utilities
		["muxc"] = "using:Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls",
	};

	/// <summary>
	/// XamlReader.Load the xaml with automatic xmlns injection.
	/// </summary>
	/// <param name="xaml">Xaml with single or double quotes</param>
	/// <param name="autoInjectXmlns">Toggle automatic detection of xmlns required and inject to the xaml</param>
	public static T LoadXaml<T>(string xaml, bool autoInjectXmlns = true) where T : class
	{
		var xmlnses = new Dictionary<string, string>();

		if (autoInjectXmlns)
		{
			foreach (var xmlns in KnownXmlnses)
			{
				var match = xmlns.Key == string.Empty
					? NonXmlnsTagRegex().IsMatch(xaml)
					// naively match the xmlns-prefix regardless if it is quoted,
					// since false positive doesn't matter.
					: xaml.Contains($"{xmlns.Key}:");
				if (match)
				{
					xmlnses.Add(xmlns.Key, xmlns.Value);
				}
			}
		}

		return LoadXaml<T>(xaml, xmlnses);
	}

	/// <summary>
	/// XamlReader.Load the xaml with the provided xmlnses.
	/// </summary>
	/// <param name="xaml">Xaml with single or double quotes</param>
	/// <param name="xmlnses">Xmlns to inject; use string.Empty for the default xmlns' key</param>
	public static T LoadXaml<T>(string xaml, Dictionary<string, string> xmlnses) where T : class
	{
		var injection = " " + string.Join(" ", xmlnses
			.Select(x => $"xmlns{(string.IsNullOrEmpty(x.Key) ? "" : $":{x.Key}")}=\"{x.Value}\"")
		);

		xaml = EndOfTagRegex().Replace(xaml, injection.TrimEnd(), 1);

		var result = XamlReader.Load(xaml);
		return result as T;
	}
}
