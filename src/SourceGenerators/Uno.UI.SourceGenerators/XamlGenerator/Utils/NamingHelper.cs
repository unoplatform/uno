#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils;

public class NamingHelper
{
	public static string AddUnique(IList<string> names, string desiredName)
	{
		var effective = desiredName;
		var i = 0;
		while (names.Contains(effective))
		{
			effective = $"{desiredName}_Δ{++i}";
		}
		names.Add(effective);

		return effective;
	}

	public static string AddUnique<T>(IDictionary<string, T> names, string desiredName, T value)
	{
		var effective = desiredName;
		var i = 0;
		while (names.ContainsKey(effective))
		{
			effective = $"{desiredName}_Δ{++i}";
		}
		names.Add(effective, value);

		return effective;
	}

	/// <summary>
	/// Gets a short name for the given XAML type name, eg.
	/// * ListView => LisVie
	/// * MyLongControlName123 => MyLonConNam123
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public static string GetShortName(string name)
	{
		const int consecutiveLower = 2;

		var sb = new StringBuilder();
		var i = 0;
		var len = name?.Length ?? 0;
		var allowedLower = consecutiveLower;

		while (i < len)
		{
			if (char.IsLetterOrDigit(name, i))
			{
				var c = name![i];
				if (char.IsUpper(c) || char.IsDigit(c))
				{
					allowedLower = consecutiveLower;
					sb.Append(c);
				}
				else if (allowedLower > 0)
				{
					allowedLower--;
					sb.Append(c);
				}
			}
			else
			{
				allowedLower = consecutiveLower; // We ignore the char, but if's an _ we allow lower next time
			}

			i++;
		}

		return sb.ToString();
	}
}
