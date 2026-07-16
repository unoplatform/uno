#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.UI.Text
{
	public partial class RichEditTextDocument
	{
		internal const string MathFontFamilyName = "Cambria Math";
		private const string MathMLNamespaceName = "http://www.w3.org/1998/Math/MathML";
		private const int MaxMathMLLength = 1024 * 1024;
		private const int MaxMathMLDepth = 128;
		private const int MaxMathMLElementCount = 25_000;
		private const int MaxMathMLAttributeLength = 16 * 1024;
		private const int MaxProjectedMathCharacters = 262_144;
		private static readonly XNamespace MathMLNamespace = MathMLNamespaceName;

		private global::Microsoft.UI.Text.RichEditMathMode _mathMode;
		private string? _mathML;

		internal bool IsMathMode => _mathMode == global::Microsoft.UI.Text.RichEditMathMode.MathOnly;

		/// <summary>Retrieves the current math mode setting of the RichEditBox.</summary>
		public global::Microsoft.UI.Text.RichEditMathMode GetMathMode() => _mathMode;

		/// <summary>Configures the RichEditBox to interpret input using the specified math mode.</summary>
		public void SetMathMode(global::Microsoft.UI.Text.RichEditMathMode mode)
		{
			if (!Enum.IsDefined(mode))
			{
				throw new ArgumentException("The math mode is invalid.", nameof(mode));
			}

			if (_mathMode == mode)
			{
				return;
			}

			SetDocumentFragment(EmptyFragment());
			_mathMode = mode;
			_mathML = null;
			ClearUndoRedoHistory();
			_owner.OnDocumentMathModeChanged();
		}

		/// <summary>Retrieves the RichEditBox content as MathML.</summary>
		public void GetMathML(out string value)
		{
			EnsureMathMode();
			value = _plainText.Length == 0
				? string.Empty
				: _mathML ?? CreateMathMLFromPlainText(_plainText);
		}

		/// <summary>Replaces the RichEditBox content with the specified MathML document.</summary>
		public void SetMathML(string value)
		{
			EnsureMathMode();

			RichTextFragment fragment;
			try
			{
				var document = ParseMathML(value);
				ValidateMathMLStructure(document.Root!);
				fragment = new MathMLProjector(DefaultFormatState(), DefaultParagraphState()).Project(document.Root!);
			}
			catch (Exception error) when (error is XmlException or ArgumentException)
			{
				SetDocumentFragment(EmptyFragment());
				throw new ArgumentException("The value is not a valid MathML document.", nameof(value), error);
			}

			var preserveSource = _owner.MaxLength <= 0 || fragment.Text.Length <= _owner.MaxLength;
			SetDocumentFragment(fragment, preserveSource ? value : null);
		}

		private void EnsureMathMode()
		{
			if (!IsMathMode)
			{
				throw new ArgumentException("Math mode must be enabled before using MathML.");
			}
		}

		private RichTextFragment EmptyFragment() => new(string.Empty, new List<CharacterFormatState>(), new List<ParagraphFormatState>());

		private static XDocument ParseMathML(string? value)
		{
			if (string.IsNullOrWhiteSpace(value) || value.Length > MaxMathMLLength)
			{
				throw new ArgumentException("MathML cannot be empty.", nameof(value));
			}

			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null,
			};
			using var stringReader = new StringReader(value);
			using var reader = XmlReader.Create(stringReader, settings);
			var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
			if (document.Root is not { } root
				|| root.Name.LocalName != "math"
				|| root.Name.NamespaceName != MathMLNamespaceName)
			{
				throw new ArgumentException("The root element must be a MathML math element.", nameof(value));
			}

			return document;
		}

		private static void ValidateMathMLStructure(XElement root)
		{
			var stack = new Stack<(XElement element, int depth)>();
			stack.Push((root, 1));
			var count = 0;
			while (stack.Count > 0)
			{
				var (element, depth) = stack.Pop();
				if (++count > MaxMathMLElementCount || depth > MaxMathMLDepth)
				{
					throw new ArgumentException("The MathML document is too complex.");
				}

				foreach (var attribute in element.Attributes())
				{
					if (attribute.Value.Length > MaxMathMLAttributeLength)
					{
						throw new ArgumentException("The MathML attribute value is too long.");
					}
				}

				foreach (var child in element.Elements())
				{
					stack.Push((child, depth + 1));
				}
			}
		}

		private static string CreateMathMLFromPlainText(string text)
		{
			var document = new XDocument(
				new XElement(
					MathMLNamespace + "math",
					new XAttribute(XNamespace.Xmlns + "mml", MathMLNamespaceName),
					new XElement(MathMLNamespace + "mtext", text)));
			return document.ToString(SaveOptions.DisableFormatting);
		}

		private sealed class MathMLProjector
		{
			private readonly CharacterFormatState _defaultCharacterFormat;
			private readonly ParagraphFormatState _defaultParagraphFormat;
			private readonly StringBuilder _text = new();
			private readonly List<CharacterFormatState> _characterStates = new();

			public MathMLProjector(CharacterFormatState defaultCharacterFormat, ParagraphFormatState defaultParagraphFormat)
			{
				_defaultCharacterFormat = defaultCharacterFormat;
				_defaultParagraphFormat = defaultParagraphFormat;
			}

			public RichTextFragment Project(XElement root)
			{
				AppendElement(root, _defaultCharacterFormat);
				var paragraphStates = new List<ParagraphFormatState>(_text.Length);
				for (var i = 0; i < _text.Length; i++)
				{
					paragraphStates.Add(_defaultParagraphFormat.Clone());
				}

				return new RichTextFragment(_text.ToString(), _characterStates, paragraphStates);
			}

			private void AppendElement(XElement element, CharacterFormatState inheritedStyle)
			{
				if (element.Name.Namespace != MathMLNamespace)
				{
					return;
				}

				var style = GetStyle(element, inheritedStyle);
				var name = element.Name.LocalName;
				switch (name)
				{
					case "math":
					case "mrow":
					case "mstyle":
					case "mpadded":
					case "menclose":
					case "merror":
						AppendChildren(element, style);
						break;
					case "mi":
					case "mn":
					case "mo":
					case "mtext":
					case "ms":
						AppendToken(element, style);
						break;
					case "mglyph":
						AppendText(GetAttribute(element, "alt") ?? string.Empty, style);
						break;
					case "mspace":
						AppendText(" ", style);
						break;
					case "msup":
						AppendScript(element, style, superscript: true);
						break;
					case "msub":
						AppendScript(element, style, superscript: false);
						break;
					case "msubsup":
						AppendSubSup(element, style);
						break;
					case "mfrac":
						AppendFraction(element, style);
						break;
					case "msqrt":
						AppendText("√(", style);
						AppendChildren(element, style);
						AppendText(")", style);
						break;
					case "mroot":
						AppendRoot(element, style);
						break;
					case "mfenced":
						AppendFenced(element, style);
						break;
					case "munder":
						AppendUnderOver(element, style, hasUnder: true, hasOver: false);
						break;
					case "mover":
						AppendUnderOver(element, style, hasUnder: false, hasOver: true);
						break;
					case "munderover":
						AppendUnderOver(element, style, hasUnder: true, hasOver: true);
						break;
					case "mtable":
						AppendTable(element, style);
						break;
					case "mtr":
					case "mlabeledtr":
						AppendTableRow(element, style);
						break;
					case "mtd":
						AppendChildren(element, style);
						break;
					case "semantics":
						AppendSemantics(element, style);
						break;
					case "maction":
						if (element.Elements().FirstOrDefault() is { } selected)
						{
							AppendElement(selected, style);
						}
						break;
					case "annotation":
					case "annotation-xml":
					case "maligngroup":
					case "malignmark":
					case "mprescripts":
					case "none":
					case "mphantom":
						break;
					default:
						AppendChildren(element, style);
						break;
				}
			}

			private void AppendChildren(XElement element, CharacterFormatState style)
			{
				foreach (var node in element.Nodes())
				{
					if (node is XElement child)
					{
						AppendElement(child, style);
					}
					else if (node is XText text && !string.IsNullOrWhiteSpace(text.Value))
					{
						AppendText(text.Value.Trim(), style);
					}
				}
			}

			private void AppendToken(XElement element, CharacterFormatState style)
			{
				var value = string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value));
				if (element.Name.LocalName == "mi"
					&& GetAttribute(element, "mathvariant") is null
					&& (value.Length == 1 || value.Length == 2 && char.IsSurrogatePair(value, 0)))
				{
					style = style.Clone();
					style.Italic = true;
				}

				AppendText(value, style);
			}

			private void AppendScript(XElement element, CharacterFormatState style, bool superscript)
			{
				var children = element.Elements().ToArray();
				if (children.Length < 2)
				{
					AppendChildren(element, style);
					return;
				}

				AppendElement(children[0], style);
				var script = GetLinearText(children[1]);
				if (TryConvertScript(script, superscript, out var converted))
				{
					AppendText(converted, GetStyle(children[1], style));
				}
				else
				{
					AppendText(superscript ? "^(" : "_(", style);
					AppendElement(children[1], style);
					AppendText(")", style);
				}
			}

			private void AppendSubSup(XElement element, CharacterFormatState style)
			{
				var children = element.Elements().ToArray();
				if (children.Length < 3)
				{
					AppendChildren(element, style);
					return;
				}

				AppendElement(children[0], style);
				AppendConvertedScript(children[1], style, superscript: false);
				AppendConvertedScript(children[2], style, superscript: true);
			}

			private void AppendConvertedScript(XElement element, CharacterFormatState style, bool superscript)
			{
				if (TryConvertScript(GetLinearText(element), superscript, out var converted))
				{
					AppendText(converted, GetStyle(element, style));
				}
				else
				{
					AppendText(superscript ? "^(" : "_(", style);
					AppendElement(element, style);
					AppendText(")", style);
				}
			}

			private void AppendFraction(XElement element, CharacterFormatState style)
			{
				var children = element.Elements().ToArray();
				if (children.Length < 2)
				{
					AppendChildren(element, style);
					return;
				}

				var numerator = GetLinearText(children[0]);
				var denominator = GetLinearText(children[1]);
				if (TryGetVulgarFraction(numerator, denominator, out var fraction))
				{
					AppendText(fraction, style);
					return;
				}

				AppendText("(", style);
				AppendElement(children[0], style);
				AppendText(")⁄(", style);
				AppendElement(children[1], style);
				AppendText(")", style);
			}

			private void AppendRoot(XElement element, CharacterFormatState style)
			{
				var children = element.Elements().ToArray();
				if (children.Length < 2)
				{
					AppendChildren(element, style);
					return;
				}

				var index = GetLinearText(children[1]);
				if (TryConvertScript(index, superscript: true, out var converted))
				{
					AppendText(converted, GetStyle(children[1], style));
					AppendText("√(", style);
					AppendElement(children[0], style);
					AppendText(")", style);
				}
				else
				{
					AppendText("root(", style);
					AppendElement(children[1], style);
					AppendText(", ", style);
					AppendElement(children[0], style);
					AppendText(")", style);
				}
			}

			private void AppendFenced(XElement element, CharacterFormatState style)
			{
				var open = GetAttribute(element, "open") ?? "(";
				var close = GetAttribute(element, "close") ?? ")";
				var separators = GetAttribute(element, "separators") ?? ",";
				var children = element.Elements().ToArray();
				AppendText(open, style);
				for (var i = 0; i < children.Length; i++)
				{
					if (i > 0 && separators.Length > 0)
					{
						AppendText(separators[Math.Min(i - 1, separators.Length - 1)].ToString(), style);
					}

					AppendElement(children[i], style);
				}
				AppendText(close, style);
			}

			private void AppendUnderOver(XElement element, CharacterFormatState style, bool hasUnder, bool hasOver)
			{
				var children = element.Elements().ToArray();
				if (children.Length == 0)
				{
					return;
				}

				AppendElement(children[0], style);
				var index = 1;
				if (hasUnder && children.Length > index)
				{
					AppendText("_(", style);
					AppendElement(children[index++], style);
					AppendText(")", style);
				}
				if (hasOver && children.Length > index)
				{
					AppendText("^(", style);
					AppendElement(children[index], style);
					AppendText(")", style);
				}
			}

			private void AppendTable(XElement element, CharacterFormatState style)
			{
				var rows = element.Elements().ToArray();
				AppendText("[", style);
				for (var i = 0; i < rows.Length; i++)
				{
					if (i > 0)
					{
						AppendText("; ", style);
					}

					AppendTableRow(rows[i], style);
				}
				AppendText("]", style);
			}

			private void AppendTableRow(XElement element, CharacterFormatState style)
			{
				var cells = element.Elements().ToArray();
				for (var i = 0; i < cells.Length; i++)
				{
					if (i > 0)
					{
						AppendText(", ", style);
					}

					AppendElement(cells[i], style);
				}
			}

			private void AppendSemantics(XElement element, CharacterFormatState style)
			{
				var presentation = element.Elements().FirstOrDefault(child => child.Name.LocalName is not "annotation" and not "annotation-xml");
				if (presentation is not null)
				{
					AppendElement(presentation, style);
				}
			}

			private void AppendText(string value, CharacterFormatState style)
			{
				if (value.Length > MaxProjectedMathCharacters - _text.Length)
				{
					throw new ArgumentException("The projected MathML text is too large.");
				}

				_text.Append(value);
				for (var i = 0; i < value.Length; i++)
				{
					_characterStates.Add(style.Clone());
				}
			}

			private static CharacterFormatState GetStyle(XElement element, CharacterFormatState inherited)
			{
				var style = inherited.Clone();
				if (TryParseColor(GetAttribute(element, "mathcolor"), out var foreground))
				{
					style.Foreground = foreground;
				}
				if (TryParseColor(GetAttribute(element, "mathbackground"), out var background))
				{
					style.Background = background;
				}

				switch (GetAttribute(element, "mathvariant")?.ToLowerInvariant())
				{
					case "normal":
						style.Bold = false;
						style.Weight = 400;
						style.Italic = false;
						break;
					case "bold":
						style.Bold = true;
						style.Weight = 700;
						style.Italic = false;
						break;
					case "italic":
						style.Italic = true;
						break;
					case "bold-italic":
						style.Bold = true;
						style.Weight = 700;
						style.Italic = true;
						break;
				}

				return style;
			}

			private static string GetLinearText(XElement element)
			{
				var builder = new StringBuilder();
				AppendLinearText(element, builder);
				return builder.ToString();
			}

			private static void AppendLinearText(XElement element, StringBuilder builder)
			{
				if (element.Name.LocalName is "annotation" or "annotation-xml" or "mphantom")
				{
					return;
				}

				foreach (var node in element.Nodes())
				{
					if (node is XText text && !string.IsNullOrWhiteSpace(text.Value))
					{
						builder.Append(text.Value.Trim());
					}
					else if (node is XElement child)
					{
						AppendLinearText(child, builder);
					}
				}
			}

			private static string? GetAttribute(XElement element, string localName)
				=> element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == localName)?.Value;

			private static bool TryConvertScript(string value, bool superscript, out string converted)
			{
				var builder = new StringBuilder(value.Length);
				foreach (var character in value)
				{
					var mapped = superscript ? ToSuperscript(character) : ToSubscript(character);
					if (mapped is null)
					{
						converted = string.Empty;
						return false;
					}

					builder.Append(mapped);
				}

				converted = builder.ToString();
				return true;
			}

			private static string? ToSuperscript(char value)
				=> value switch
				{
					'0' => "⁰",
					'1' => "¹",
					'2' => "²",
					'3' => "³",
					'4' => "⁴",
					'5' => "⁵",
					'6' => "⁶",
					'7' => "⁷",
					'8' => "⁸",
					'9' => "⁹",
					'+' => "⁺",
					'-' => "⁻",
					'=' => "⁼",
					'(' => "⁽",
					')' => "⁾",
					'i' => "ⁱ",
					'n' => "ⁿ",
					_ => null,
				};

			private static string? ToSubscript(char value)
				=> value switch
				{
					'0' => "₀",
					'1' => "₁",
					'2' => "₂",
					'3' => "₃",
					'4' => "₄",
					'5' => "₅",
					'6' => "₆",
					'7' => "₇",
					'8' => "₈",
					'9' => "₉",
					'+' => "₊",
					'-' => "₋",
					'=' => "₌",
					'(' => "₍",
					')' => "₎",
					'a' => "ₐ",
					'e' => "ₑ",
					'h' => "ₕ",
					'i' => "ᵢ",
					'j' => "ⱼ",
					'k' => "ₖ",
					'l' => "ₗ",
					'm' => "ₘ",
					'n' => "ₙ",
					'o' => "ₒ",
					'p' => "ₚ",
					'r' => "ᵣ",
					's' => "ₛ",
					't' => "ₜ",
					'x' => "ₓ",
					_ => null,
				};

			private static bool TryGetVulgarFraction(string numerator, string denominator, out string fraction)
			{
				fraction = (numerator, denominator) switch
				{
					("1", "2") => "½",
					("1", "3") => "⅓",
					("2", "3") => "⅔",
					("1", "4") => "¼",
					("3", "4") => "¾",
					("1", "5") => "⅕",
					("2", "5") => "⅖",
					("3", "5") => "⅗",
					("4", "5") => "⅘",
					("1", "6") => "⅙",
					("5", "6") => "⅚",
					("1", "7") => "⅐",
					("1", "8") => "⅛",
					("3", "8") => "⅜",
					("5", "8") => "⅝",
					("7", "8") => "⅞",
					("1", "9") => "⅑",
					("1", "10") => "⅒",
					_ => string.Empty,
				};
				return fraction.Length > 0;
			}

			private static bool TryParseColor(string? value, out global::Windows.UI.Color color)
			{
				color = default;
				if (string.IsNullOrWhiteSpace(value))
				{
					return false;
				}

				if (value[0] == '#')
				{
					var hex = value.AsSpan(1);
					if (hex.Length == 3
						&& byte.TryParse(new string(hex[0], 2), global::System.Globalization.NumberStyles.HexNumber, null, out var shortRed)
						&& byte.TryParse(new string(hex[1], 2), global::System.Globalization.NumberStyles.HexNumber, null, out var shortGreen)
						&& byte.TryParse(new string(hex[2], 2), global::System.Globalization.NumberStyles.HexNumber, null, out var shortBlue))
					{
						color = global::Windows.UI.Color.FromArgb(255, shortRed, shortGreen, shortBlue);
						return true;
					}

					if (hex.Length == 6
						&& byte.TryParse(hex[..2], global::System.Globalization.NumberStyles.HexNumber, null, out var red)
						&& byte.TryParse(hex.Slice(2, 2), global::System.Globalization.NumberStyles.HexNumber, null, out var green)
						&& byte.TryParse(hex.Slice(4, 2), global::System.Globalization.NumberStyles.HexNumber, null, out var blue))
					{
						color = global::Windows.UI.Color.FromArgb(255, red, green, blue);
						return true;
					}
				}

				color = value.ToLowerInvariant() switch
				{
					"black" => global::Windows.UI.Colors.Black,
					"white" => global::Windows.UI.Colors.White,
					"red" => global::Windows.UI.Colors.Red,
					"green" => global::Windows.UI.Colors.Green,
					"blue" => global::Windows.UI.Colors.Blue,
					"yellow" => global::Windows.UI.Colors.Yellow,
					"gray" or "grey" => global::Windows.UI.Colors.Gray,
					"transparent" => global::Windows.UI.Colors.Transparent,
					_ => default,
				};
				return value.Equals("black", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("white", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("red", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("green", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("blue", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("yellow", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("gray", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("grey", StringComparison.OrdinalIgnoreCase)
					|| value.Equals("transparent", StringComparison.OrdinalIgnoreCase);
			}
		}
	}
}
