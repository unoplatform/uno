#nullable enable

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator.Utils
{
	internal static partial class XBindExpressionParser
	{
		private const string XBindSubstitute = "↔↔";
		private static readonly Regex NamedParam = new(@"^(\w*)=(.*?)", RegexOptions.Compiled);
		private static readonly Regex DocumentPaths = new("\"{x:Bind\\s(.*?)}\"", RegexOptions.Singleline | RegexOptions.Compiled);

		/// <summary>
		/// Rewrites all x:Bind expression Path property to be compatible with the System.Xaml parser.
		/// </summary>
		/// <param name="markup">The original markup</param>
		/// <returns>The adjusted markup</returns>
		/// <remarks>
		/// The System.Xaml parser does not support having extended expressions, with
		/// quotes or commands inside an unquoted property value. The point of this method is
		/// to encode the whole Path property as Base64 to that its content does not interfere
		/// with the default rules of the parser. The expression is then restored using <see cref="RestoreSinglePath"/>.
		/// </remarks>
		internal static string RewriteDocumentPaths(string markup)
		{
			var result = DocumentPaths.Replace(
				markup,
				e => $"\"{{x:Bind {RewriteParameters(e.Groups[1].Value.Trim())}}}\""
			);

			return result;
		}

		/// <summary>
		/// Restores an x:Bind path encoded with <see cref="RewriteDocumentPaths"/>.
		/// </summary>
		internal static string? RestoreSinglePath(string? path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				var bytes = Convert.FromBase64String(path!.Replace("_", "="));
				var rawPath = Encoding.Unicode.GetString(bytes);

				return rawPath
					.Replace('\'', '\"')
					.Replace(XBindExpressionParser.XBindSubstitute, "\\\'");
			}

			return path;
		}

		private static string RewriteParameters(string value)
		{
			var parts = value.Split(',').SelectToArray(v => v.Replace("^'", XBindSubstitute));

			if (parts.Length != 0)
			{
				if (NamedParam.Match(parts[0]).Success)
				{
					// No unnamed parameter at first position, look for named Path parameter
					var pathIndex = parts.IndexOf(
						"Path",
						(s, item) => item.Split('=').FirstOrDefault()?.Trim().Contains(s) ?? false);

					if (pathIndex != -1)
					{
						var adjustedParts = parts.Select(p => p.Trim().TrimStart("Path=")).ToArray();

						GetEncodedPath(adjustedParts, pathIndex, out var endIndex, out var encodedPath);

						var finalParts = adjustedParts
							.Take(pathIndex)
							.Concat("Path=" + encodedPath)
							.Concat(parts.Skip(endIndex));

						return string.Join(",", finalParts);
					}
					else
					{
						return value;
					}
				}
				else
				{
					// First parameter is unnamed, it's the path.
					GetEncodedPath(parts, 0, out var endIndex, out var encodedPath);

					var remainder = string.Join(",", parts.Skip(endIndex)) switch
					{
						var r when r.Length > 0 => "," + r,
						_ => ""
					};

					return encodedPath + remainder;
				}
			}
			else
			{
				return value;
			}
		}

		private static void GetEncodedPath(string[] parts, int startIndex, out int end, out string encodedPath)
		{
			end = GetFunctionRange(parts, startIndex) + 1;
			var sourceString = string.Join(",", parts.Skip(startIndex).Take(end));
			var pathBytes = Encoding.Unicode.GetBytes(sourceString);
			encodedPath = Convert.ToBase64String(pathBytes).Replace("=", "_");
		}

		private static int GetFunctionRange(string[] parts, int startIndex)
		{
			var parenthesisCount = 0;
			int i = startIndex;

			for (; i < parts.Length; i++)
			{
				parenthesisCount += parts[i].Count(c => c == '(');
				parenthesisCount -= parts[i].Count(c => c == ')');

				if (parenthesisCount == 0)
				{
					break;
				}
			}

			return i;
		}
	}
}
