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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Uno.Xaml.Schema;

namespace Uno.Xaml
{
	internal class ParsedMarkupExtensionInfo
	{
		/// <summary>
		/// This regex returns the members of a binding expression which are separated 
		/// by commas but keeps the commas inside the member value.
		/// e.g. [Property], ConverterParameter='A', TargetNullValue='B', FallbackValue='C,D,E,F' returns
		/// - [Property]
		/// - ConverterParameter='A'
		/// - TargetNullValue='B'
		/// - FallbackValue='C,D,E,F'
		/// </summary>
		private static Regex BindingMembersRegex = new Regex("[^'\",]+'[^^']+'|[^'\",]+\"[^\"]+\"|[^,]+");

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
				throw Error("Invalid markup extension attribute. It should begin with '{{', but was {0}", raw);
			}

			var ret = new ParsedMarkupExtensionInfo();
			int idx = raw.LastIndexOf('}');

			if (idx < 0)
			{
				throw Error("Expected '}}' in the markup extension attribute: '{0}'", raw);
			}
				
			raw = raw.Substring(1, idx - 1);
			idx = raw.IndexOf(' ');
			string name = idx < 0 ? raw : raw.Substring(0, idx);

			XamlTypeName xtn;
			if (!XamlTypeName.TryParse(name, nsResolver, out xtn))
			{
				throw Error("Failed to parse type name '{0}'", name);
			}

			var xt = sctx.GetXamlType(xtn) ?? new XamlType(xtn.Namespace, xtn.Name, null, sctx);
			ret.Type = xt;

			if (idx < 0)
				return ret;

			var valueWithoutBinding = raw.Substring(idx + 1, raw.Length - idx - 1);

			var vpairs = BindingMembersRegex.Matches(valueWithoutBinding)
				.Cast<Match>()
				.Select(m => m.Value.Trim())
				.ToList();

			if (vpairs.Count == 0)
			{
				vpairs.Add(valueWithoutBinding);
			}
			
			List<string> posPrms = null;
			XamlMember lastMember = null;
			foreach (var vpair in vpairs)
			{
				idx = vpair.IndexOf('=');

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
				if (stringValue.StartsWith("'") && stringValue.EndsWith("'"))
				{
					return stringValue.Trim(new[] { '\'' });
				}
				// Remove wrapping double quotes.
				else if (stringValue.StartsWith("\"") && stringValue.EndsWith("\""))
				{
					return stringValue.Trim(new[] { '\"' });
				}
			}

			return stringValue;
		}

		private static bool IsValidMarkupExtension(string valueString) => valueString.StartsWith("{") && !valueString.StartsWith("{}");

		static string UnescapeValue(string s)
		{
			if (s.StartsWith("{}"))
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
			return new XamlParseException(String.Format(format, args));
		}
	}
}
