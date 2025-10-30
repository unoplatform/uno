//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Uno.Xaml.Schema;

namespace Uno.Xaml
{
	internal class ParsedMarkupExtensionInfo
	{
		private static readonly char[] _singleQuoteArray = new[] { '\'' };
		private static readonly char[] _doubleQuoteArray = new[] { '\"' };

		Dictionary<XamlMember, object> args = new Dictionary<XamlMember, object>();
		public Dictionary<XamlMember, object> Arguments
		{
			get { return args; }
		}

		public XamlType Type { get; set; }

		public static ParsedMarkupExtensionInfo Parse(string raw, IXamlNamespaceResolver nsResolver, XamlSchemaContext sctx)
		{
			if (raw == null)
			{
				throw new ArgumentNullException(nameof(raw));
			}

			if (raw.Length == 0 || raw[0] != '{')
			{
				throw Error("Invalid markup extension attribute. Expected '{{' not found at the start: \"{0}\"", raw);
			}

			if (raw.Length >= 2 && raw[1] == '}')
			{
				throw Error("Markup extension can not begin with an '{}' escape: \"{0}\"", raw);
			}

			var ret = new ParsedMarkupExtensionInfo();
			if (raw[raw.Length - 1] != '}')
			{
				// Any character after the final closing bracket is not accepted. Therefore, the last character should be '}'.
				// Ideally, we should still ran the entire markup through the parser to get a more meaningful error.
				if (raw.TrimEnd() is string trimmed && trimmed[trimmed.Length - 1] == '}')
				{
					// Technically this is salvageable, but since uwp throws on this, we will do the same.
					throw Error("White space is not allowed after end of markup extension: \"{0}\"", raw);
				}

				throw Error("Expected '}}' in the markup extension attribute: \"{0}\"", raw);
			}

			var nameSeparatorIndex = raw.IndexOf(' ');
			var name = nameSeparatorIndex != -1 ? raw.Substring(1, nameSeparatorIndex - 1) : raw.Substring(1, raw.Length - 2);
			if (!XamlTypeName.TryParse(name, nsResolver, out var xtn))
			{
				throw Error("Failed to parse type name '{0}'", name);
			}

			var xt = sctx.GetXamlType(xtn) ?? new XamlType(xtn.Namespace, xtn.Name, null, sctx);
			ret.Type = xt;

			if (nameSeparatorIndex < 0)
				return ret;

			var valueWithoutBinding = raw.Substring(nameSeparatorIndex + 1, raw.Length - 1 - (nameSeparatorIndex + 1));
			var vpairs = SliceParameters(valueWithoutBinding, raw);

			List<string> posPrms = null;
			XamlMember lastMember = null;
			foreach (var vpair in vpairs)
			{
				var idx = vpair.IndexOf('=');

				// FIXME: unescape string (e.g. comma)
				if (idx < 0)
				{
					if (vpair.ElementAtOrDefault(0) == ')')
					{
						if (lastMember != null)
						{
							if (ret.Arguments[lastMember] is string s)
							{
								ret.Arguments[lastMember] = s + ')';
							}
						}
						else
						{
							posPrms[posPrms.Count - 1] += ')';
						}
					}
					else
					{
						if (posPrms == null)
						{
							posPrms = new List<string>();
							ret.Arguments.Add(XamlLanguage.PositionalParameters, posPrms);
						}

						posPrms.Add(UnescapeValue(vpair.Trim()));
					}
				}
				else
				{
					var key = vpair.Substring(0, idx).Trim();
					// FIXME: is unknown member always isAttacheable = false?
					var xm = xt.GetMember(key) ?? new XamlMember(key, xt, false);

					// Binding member values may be wrapped in quotes (single or double) e.g. 'A,B,C,D'.
					// Remove those wrapping quotes from the resulting string value.
					var valueString = RemoveWrappingStringQuotes(vpair.Substring(idx + 1).Trim());

					var value = IsValidMarkupExtension(valueString)
						? (object)Parse(valueString, nsResolver, sctx) : UnescapeValue(valueString);

					ret.Arguments.Add(xm, value);
					lastMember = xm;
				}
			}
			return ret;
		}

		private static string RemoveWrappingStringQuotes(string stringValue)
		{
			if (stringValue != null && stringValue != string.Empty)
			{
				// Remove wrapping single quotes.
				if (stringValue.StartsWith("'", StringComparison.Ordinal) && stringValue.EndsWith("'", StringComparison.Ordinal))
				{
					return stringValue.Trim(_singleQuoteArray);
				}
				// Remove wrapping double quotes.
				else if (stringValue.StartsWith("\"", StringComparison.Ordinal) && stringValue.EndsWith("\"", StringComparison.Ordinal))
				{
					return stringValue.Trim(_doubleQuoteArray);
				}
			}

			return stringValue;
		}

		private static bool IsValidMarkupExtension(string valueString) => valueString.StartsWith("{", StringComparison.Ordinal) && !valueString.StartsWith("{}", StringComparison.Ordinal);

		static string UnescapeValue(string s)
		{
			if (s.StartsWith("{}", StringComparison.Ordinal))
			{
				return s.Substring(2);
			}

			if (s.Contains("\\{") || s.Contains("\\}"))
			{
				return s
					.Replace("\\{", "{")
					.Replace("\\}", "}");
			}

			// change XamlXmlWriter too if we change here.
			if (s == "\"\"") // FIXME: there could be some escape syntax.
			{
				return String.Empty;
			}
			else
			{
				return s;
			}
		}

		static Exception Error(string format, params object[] args)
		{
			return new XamlParseException(String.Format(CultureInfo.InvariantCulture, format, args));
		}

		internal static IEnumerable<string> SliceParameters(string vargs, string raw)
		{
			vargs = vargs.Trim();

			// We need to split the parameters by the commas, but with two catches:
			// 1. Nested markup extension can also contains multiple parameters, but they are a single parameter to the current context
			// 2. Comma can appear within a single-quoted string.
			// 3. a little bit of #1 and a little bit #2...
			// While we can use regex to match #1 and #2, #3 cannot be solved with regex.

			// It seems that single-quot(`'`) can't be escaped when used in the parameters.
			// So we don't have to worry about escaping it.

			var isInQuot = false;
			var bracketDepth = 0;
			var lastSliceIndex = -1;

			for (int i = 0; i < vargs.Length; i++)
			{
				var c = vargs[i];
				if (false) { }
				else if (c == '\'') isInQuot = !isInQuot;
				else if (isInQuot) { }
				else if (c == '{') bracketDepth++;
				else if (c == '}') bracketDepth--;
				else if (c == ',' && bracketDepth == 0)
				{
					yield return vargs.Substring(lastSliceIndex + 1, i - lastSliceIndex - 1).Trim();
					lastSliceIndex = i;
				}
			}

			if (bracketDepth > 0)
			{
				throw Error("Expected '}}' in markup extension:", raw);
			}

			yield return vargs.Substring(lastSliceIndex + 1).Trim();
		}
	}
}
