#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Windows.UI;

namespace Microsoft.UI.Text;

internal readonly record struct MathTextSpan(int Start, int Length)
{
	internal int End => Start + Length;

	internal bool Contains(int position) => position >= Start && position < End;
}

internal enum MathTokenKind
{
	Identifier,
	Number,
	Operator,
	Text,
}

internal enum MathVariant
{
	Unspecified,
	Normal,
	Bold,
	Italic,
	BoldItalic,
}

internal readonly record struct MathStyle(
	MathVariant Variant,
	Color? Foreground,
	Color? Background)
{
	internal static MathStyle Default => new(MathVariant.Unspecified, null, null);
}

internal abstract class MathNode
{
	protected MathNode(MathStyle style)
	{
		Style = style;
	}

	internal MathStyle Style { get; }
}

internal sealed class MathRowNode : MathNode
{
	internal MathRowNode(
		MathStyle style,
		IReadOnlyList<MathNode> children,
		bool collapseSingleEditedToken = false)
		: base(style)
	{
		Children = children;
		CollapseSingleEditedToken = collapseSingleEditedToken;
	}

	internal IReadOnlyList<MathNode> Children { get; }

	internal bool CollapseSingleEditedToken { get; }
}

internal sealed class MathTokenNode : MathNode
{
	internal MathTokenNode(MathStyle style, MathTokenKind kind, string text, bool fenceFalse = false)
		: base(style)
	{
		Kind = kind;
		Text = text;
		FenceFalse = fenceFalse;
	}

	internal MathTokenKind Kind { get; }

	internal string Text { get; }

	internal bool FenceFalse { get; }

	internal string ProjectionText => MathDocument.GetTokenProjection(this);
}

internal sealed class MathFractionNode : MathNode
{
	internal MathFractionNode(MathStyle style, MathNode numerator, MathNode denominator)
		: base(style)
	{
		Numerator = numerator;
		Denominator = denominator;
	}

	internal MathNode Numerator { get; }

	internal MathNode Denominator { get; }
}

internal sealed class MathRadicalNode : MathNode
{
	internal MathRadicalNode(MathStyle style, MathNode radicand, MathNode? degree)
		: base(style)
	{
		Radicand = radicand;
		Degree = degree;
	}

	internal MathNode Radicand { get; }

	internal MathNode? Degree { get; }
}

internal sealed class MathScriptNode : MathNode
{
	internal MathScriptNode(MathStyle style, MathNode @base, MathNode? subscript, MathNode? superscript)
		: base(style)
	{
		Base = @base;
		Subscript = subscript;
		Superscript = superscript;
	}

	internal MathNode Base { get; }

	internal MathNode? Subscript { get; }

	internal MathNode? Superscript { get; }
}

internal sealed class MathFencedNode : MathNode
{
	internal MathFencedNode(
		MathStyle style,
		string open,
		string close,
		MathNode content)
		: base(style)
	{
		Open = open;
		Close = close;
		Content = content;
	}

	internal string Open { get; }

	internal string Close { get; }

	internal MathNode Content { get; }
}

internal sealed class MathTableRow
{
	internal MathTableRow(IReadOnlyList<MathNode> cells)
	{
		Cells = cells;
	}

	internal IReadOnlyList<MathNode> Cells { get; }
}

internal sealed class MathTableNode : MathNode
{
	internal MathTableNode(MathStyle style, IReadOnlyList<MathTableRow> rows)
		: base(style)
	{
		Rows = rows;
	}

	internal IReadOnlyList<MathTableRow> Rows { get; }
}

internal enum MathOverUnderKind
{
	Mover,
	Munder,
	Munderover,
	Nary,
}

internal sealed class MathOverUnderNode : MathNode
{
	internal MathOverUnderNode(
		MathStyle style,
		MathOverUnderKind kind,
		MathNode @base,
		MathNode? under,
		MathNode? over,
		MathNode? operand = null)
		: base(style)
	{
		Kind = kind;
		Base = @base;
		Under = under;
		Over = over;
		Operand = operand;
	}

	internal MathOverUnderKind Kind { get; }

	internal MathNode Base { get; }

	internal MathNode? Under { get; }

	internal MathNode? Over { get; }

	internal MathNode? Operand { get; }
}

internal sealed class MathScriptPair
{
	internal MathScriptPair(MathNode? subscript, MathNode? superscript)
	{
		Subscript = subscript;
		Superscript = superscript;
	}

	internal MathNode? Subscript { get; }

	internal MathNode? Superscript { get; }
}

internal sealed class MathMultiScriptsNode : MathNode
{
	internal MathMultiScriptsNode(
		MathStyle style,
		MathNode body,
		IReadOnlyList<MathScriptPair> prescripts)
		: base(style)
	{
		Body = body;
		Prescripts = prescripts;
	}

	internal MathNode Body { get; }

	internal IReadOnlyList<MathScriptPair> Prescripts { get; }
}

internal readonly record struct MathAtomSpan(MathTokenNode Atom, MathTextSpan Span);

internal readonly record struct MathEditResult(
	MathDocument Document,
	MathTextSpan ReplacedSpan,
	int InsertedProjectionLength,
	int CallerInsertedLength);

internal sealed class MathDocument
{
	internal const string NamespaceName = "http://www.w3.org/1998/Math/MathML";
	internal const char ObjectStart = '\uFDD0';
	internal const char ArgumentSeparator = '\uFDEE';
	internal const char ObjectEnd = '\uFDEF';
	internal const int MaxInputLength = 1024 * 1024;
	internal const int MaxDepth = 64;
	internal const int MaxNodeCount = 4096;
	internal const int MaxAttributeLength = 16 * 1024;
	internal const int MaxProjectionLength = 262_144;
	internal const int MaxTableRows = 64;
	internal const int MaxTableColumns = 64;

	private readonly Dictionary<MathNode, MathTextSpan> _spans;

	private MathDocument(
		MathRowNode root,
		string projection,
		Dictionary<MathNode, MathTextSpan> spans,
		IReadOnlyList<MathAtomSpan> atoms,
		string? liveInputText)
	{
		Root = root;
		Projection = projection;
		_spans = spans;
		Atoms = atoms;
		LiveInputText = liveInputText;
		CanonicalMathML = MathMLSerializer.Serialize(root);
	}

	internal MathRowNode Root { get; }

	internal string Projection { get; }

	internal string CanonicalMathML { get; }

	internal IReadOnlyList<MathAtomSpan> Atoms { get; }

	internal string? LiveInputText { get; }

	internal MathTextSpan GetSpan(MathNode node) => _spans[node];

	internal MathAtomSpan? GetAtomAt(int position)
	{
		foreach (var atom in Atoms)
		{
			if (atom.Span.Contains(position))
			{
				return atom;
			}
		}

		return null;
	}

	internal bool IsStructuralMarkerAt(int position)
		=> (uint)position < (uint)Projection.Length && IsStructuralMarker(Projection[position]);

	internal bool TouchesStructuralMarker(int start, int end)
	{
		start = Math.Clamp(start, 0, Projection.Length);
		end = Math.Clamp(end, start, Projection.Length);
		if (start == end)
		{
			return IsStructuralMarkerAt(start);
		}

		for (var index = start; index < end; index++)
		{
			if (IsStructuralMarkerAt(index))
			{
				return true;
			}
		}

		return false;
	}

	internal bool TryApplyTextEdit(
		int start,
		int end,
		string replacement,
		out MathEditResult result)
	{
		result = default;
		start = Math.Clamp(start, 0, Projection.Length);
		end = Math.Clamp(end, start, Projection.Length);
		if (TouchesStructuralMarker(start, end)
			|| replacement.IndexOfAny(new[] { ObjectStart, ArgumentSeparator, ObjectEnd }) >= 0)
		{
			return false;
		}

		MathAtomSpan? selectedAtom = null;
		foreach (var atom in Atoms)
		{
			if (start >= atom.Span.Start
				&& end <= atom.Span.End
				&& (start < atom.Span.End || start == atom.Span.Start))
			{
				selectedAtom = atom;
				break;
			}
		}

		if (selectedAtom is not { } atomSpan
			|| !TryMapProjectionBoundary(atomSpan.Atom, start - atomSpan.Span.Start, out var tokenStart)
			|| !TryMapProjectionBoundary(atomSpan.Atom, end - atomSpan.Span.Start, out var tokenEnd))
		{
			return false;
		}

		var oldToken = atomSpan.Atom;
		var text = oldToken.Text.Remove(tokenStart, tokenEnd - tokenStart).Insert(tokenStart, replacement);
		var style = oldToken.Style;
		if (oldToken.Kind == MathTokenKind.Identifier
			&& oldToken.Text.Length > 1
			&& IsSingleUnicodeScalar(text))
		{
			style = style with { Variant = MathVariant.Normal };
		}

		var newToken = new MathTokenNode(style, oldToken.Kind, text, oldToken.FenceFalse);
		var replaced = false;
		var root = ReplaceToken(Root, oldToken, newToken, ref replaced) as MathRowNode;
		if (!replaced || root is null)
		{
			return false;
		}

		var document = Create(root);
		var newAtom = document.Atoms.FirstOrDefault(candidate => ReferenceEquals(candidate.Atom, newToken));
		if (newAtom.Atom is null)
		{
			return false;
		}

		var callerInsertedLength = GetProjectedReplacementLength(oldToken, replacement);
		result = new MathEditResult(
			document,
			atomSpan.Span,
			newAtom.Span.Length,
			callerInsertedLength);
		return true;
	}

	internal static bool TryConvertUnicodeMath(string text, out MathDocument document)
	{
		document = null!;
		if (text.Length < 2
			|| text[^1] != ' '
			|| text.Length > MaxProjectionLength)
		{
			return false;
		}

		var candidate = text[..^1];
		var parser = new UnicodeMathParser(candidate);
		if (!parser.TryParse(out var converted))
		{
			return false;
		}

		try
		{
			document = Create(
				converted as MathRowNode
					?? new MathRowNode(MathStyle.Default, AsReadOnly(converted)));
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private sealed class UnicodeMathParser
	{
		private static readonly MathStyle _liveStyle = new(MathVariant.Unspecified, Colors.Black, null);
		private readonly string _text;
		private int _position;
		private int _depth;
		private int _nodeCount;
		private bool _hasConversion;
		private bool _failed;

		internal UnicodeMathParser(string text)
		{
			_text = text;
		}

		internal bool TryParse(out MathNode node)
		{
			node = ParseExpression('\0');
			return !_failed
				&& _hasConversion
				&& _position == _text.Length
				&& _nodeCount <= MaxNodeCount;
		}

		private MathNode ParseExpression(char closing)
		{
			if (++_depth > MaxDepth)
			{
				_failed = true;
				_depth--;
				return EmptyRow();
			}

			var numerator = ParseSequence(closing);
			if (!_failed && Current == '/')
			{
				if (IsEmpty(numerator))
				{
					_failed = true;
					_depth--;
					return numerator;
				}
				_position++;
				if (AtEndOrClosing(closing))
				{
					_failed = true;
				}
				else
				{
					var denominator = ParseExpression(closing);
					if (IsEmpty(denominator))
					{
						_failed = true;
					}
					else
					{
						numerator = AddNode(new MathFractionNode(MathStyle.Default, numerator, denominator));
						_hasConversion = true;
					}
				}
			}

			_depth--;
			return numerator;
		}

		private MathNode ParseSequence(char closing)
		{
			var children = new List<MathNode>();
			while (!_failed && !AtEndOrClosing(closing) && Current != '/')
			{
				var before = _position;
				children.Add(ParseScriptedAtom());
				if (_position == before)
				{
					_failed = true;
				}
			}

			return AddNode(new MathRowNode(MathStyle.Default, CopyAsReadOnly(children)));
		}

		private MathNode ParseScriptedAtom()
		{
			var @base = ParsePrimary();
			MathNode? subscript = null;
			MathNode? superscript = null;
			while (!_failed && Current is '_' or '^')
			{
				var marker = Current;
				_position++;
				if (AtEndOrClosing('\0')
					|| marker == '_' && subscript is not null
					|| marker == '^' && superscript is not null)
				{
					_failed = true;
					break;
				}

				var argument = ParseScriptArgument();
				if (marker == '_')
				{
					subscript = argument;
				}
				else
				{
					superscript = argument;
				}
			}

			if (subscript is null && superscript is null)
			{
				return @base;
			}

			_hasConversion = true;
			return AddNode(new MathScriptNode(
				MathStyle.Default,
				EnsureRow(@base),
				subscript is null ? null : EnsureRow(subscript),
				superscript is null ? null : EnsureRow(superscript)));
		}

		private MathNode ParseScriptArgument()
			=> ParsePrimary();

		private MathNode ParsePrimary()
		{
			if (Current is '√' or '∛' or '∜')
			{
				var radical = Current;
				_position++;
				if (Current == '\0')
				{
					_failed = true;
					return EmptyRow();
				}

				_hasConversion = true;
				var radicand = ParseScriptArgument();
				MathNode? degree = radical switch
				{
					'∛' => EnsureRow(AddNode(CreateLiveToken("3", _liveStyle))),
					'∜' => EnsureRow(AddNode(CreateLiveToken("4", _liveStyle))),
					_ => null,
				};
				return AddNode(new MathRadicalNode(MathStyle.Default, EnsureRow(radicand), degree));
			}

			if (Current is '(' or '{')
			{
				var open = Current;
				var close = open == '(' ? ')' : '}';
				_position++;
				var content = ParseExpression(close);
				if (_failed || Current != close)
				{
					_failed = true;
					return content;
				}

				_position++;
				_hasConversion = true;
				return open == '('
					? AddNode(new MathFencedNode(MathStyle.Default, "(", ")", content))
					: content;
			}

			if (Current == '\\')
			{
				return ParseCommand();
			}

			if (Current is '\0' or ')' or '}')
			{
				_failed = true;
				return EmptyRow();
			}

			var length = char.IsHighSurrogate(Current)
				&& _position + 1 < _text.Length
				&& char.IsLowSurrogate(_text[_position + 1])
					? 2
					: 1;
			var value = _text.Substring(_position, length);
			_position += length;
			return AddNode(CreateLiveToken(value, _liveStyle));
		}

		private MathNode ParseCommand()
		{
			var start = _position++;
			while (_position < _text.Length && char.IsLetter(_text[_position]))
			{
				_position++;
			}

			var command = _text[(start + 1).._position];
			if (command is "sqrt" or "root")
			{
				if (Current is not ('(' or '{'))
				{
					_position = start;
					return ParseLiteralCommand();
				}

				_hasConversion = true;
				var first = ParsePrimary();
				if (command == "sqrt")
				{
					return AddNode(new MathRadicalNode(MathStyle.Default, first, degree: null));
				}
				if (Current is not ('(' or '{'))
				{
					_failed = true;
					return first;
				}

				var radicand = ParsePrimary();
				return AddNode(new MathRadicalNode(MathStyle.Default, radicand, first));
			}

			if (TryMapCommand(command, out var value))
			{
				_hasConversion = true;
				return AddNode(CreateLiveToken(value, _liveStyle));
			}

			_position = start;
			return ParseLiteralCommand();
		}

		private MathNode ParseLiteralCommand()
		{
			var children = new List<MathNode>();
			children.Add(AddNode(CreateLiveToken("\\", _liveStyle)));
			_position++;
			while (!_failed && _position < _text.Length && char.IsLetter(_text[_position]))
			{
				children.Add(AddNode(CreateLiveToken(_text[_position++].ToString(), _liveStyle)));
			}
			return AddNode(new MathRowNode(MathStyle.Default, CopyAsReadOnly(children)));
		}

		private static bool IsEmpty(MathNode node)
			=> node is MathRowNode { Children.Count: 0 };

		private MathNode AddNode(MathNode node)
		{
			if (++_nodeCount > MaxNodeCount)
			{
				_failed = true;
			}
			return node;
		}

		private MathNode EmptyRow()
			=> AddNode(new MathRowNode(MathStyle.Default, Array.Empty<MathNode>()));

		private MathNode EnsureRow(MathNode node)
			=> node is MathRowNode ? node : AddNode(new MathRowNode(MathStyle.Default, AsReadOnly(node)));

		private char Current => _position < _text.Length ? _text[_position] : '\0';

		private bool AtEndOrClosing(char closing)
			=> Current == '\0' || closing != '\0' && Current == closing;

		private static bool TryMapCommand(string command, out string value)
		{
			value = command switch
			{
				"alpha" => "α",
				"beta" => "β",
				"gamma" => "γ",
				"delta" => "δ",
				"zeta" => "ζ",
				"eta" => "η",
				"epsilon" => "ϵ",
				"varepsilon" => "ε",
				"theta" => "θ",
				"vartheta" => "ϑ",
				"iota" => "ι",
				"kappa" => "κ",
				"lambda" => "λ",
				"mu" => "μ",
				"nu" => "ν",
				"xi" => "ξ",
				"omicron" => "ο",
				"pi" => "π",
				"varpi" => "ϖ",
				"rho" => "ρ",
				"sigma" => "σ",
				"tau" => "τ",
				"upsilon" => "υ",
				"phi" => "ϕ",
				"varphi" => "φ",
				"chi" => "χ",
				"psi" => "ψ",
				"omega" => "ω",
				"Gamma" => "Γ",
				"Delta" => "Δ",
				"Theta" => "Θ",
				"Xi" => "Ξ",
				"Lambda" => "Λ",
				"Pi" => "Π",
				"Sigma" => "Σ",
				"Upsilon" => "Υ",
				"Phi" => "Φ",
				"Psi" => "Ψ",
				"Omega" => "Ω",
				"pm" => "±",
				"mp" => "∓",
				"times" => "×",
				"div" => "÷",
				"cdot" => "⋅",
				"ast" => "∗",
				"le" or "leq" => "≤",
				"ge" or "geq" => "≥",
				"ne" or "neq" => "≠",
				"approx" => "≈",
				"equiv" => "≡",
				"infty" => "∞",
				"partial" => "∂",
				"nabla" => "∇",
				"sum" => "∑",
				"prod" => "∏",
				"int" => "∫",
				"rightarrow" or "to" => "→",
				"leftarrow" => "←",
				"leftrightarrow" => "↔",
				"in" => "∈",
				"notin" => "∉",
				"subset" => "⊂",
				"supset" => "⊃",
				"subseteq" => "⊆",
				"supseteq" => "⊇",
				"cup" => "∪",
				"cap" => "∩",
				"wedge" or "land" => "∧",
				"vee" or "lor" => "∨",
				"oplus" => "⊕",
				"otimes" => "⊗",
				"propto" => "∝",
				"forall" => "∀",
				"exists" => "∃",
				_ => string.Empty,
			};
			return value.Length != 0;
		}
	}

	internal RichTextFragment CreateFragment(
		CharacterFormatState defaultCharacterFormat,
		ParagraphFormatState defaultParagraphFormat)
	{
		var baseCharacter = defaultCharacterFormat.Clone();
		baseCharacter.Name = RichEditTextDocument.MathFontFamilyName;
		var characterRuns = new List<FormatRun>();
		var position = 0;

		foreach (var atom in Atoms)
		{
			if (atom.Span.Start > position)
			{
				AppendRun(characterRuns, atom.Span.Start - position, baseCharacter);
			}

			var character = baseCharacter.Clone();
			ApplyStyle(character, atom.Atom);
			AppendRun(characterRuns, atom.Span.Length, character);
			position = atom.Span.End;
		}

		if (position < Projection.Length)
		{
			AppendRun(characterRuns, Projection.Length - position, baseCharacter);
		}

		var paragraphRuns = Projection.Length == 0
			? Array.Empty<ParagraphRun>()
			: new[] { new ParagraphRun(Projection.Length, defaultParagraphFormat.Clone()) };
		return new RichTextFragment(
			Projection,
			characterRuns,
			paragraphRuns,
			new ParagraphFormatState(),
			hasExplicitTerminalParagraphState: false);

		static void AppendRun(List<FormatRun> runs, int length, CharacterFormatState format)
		{
			if (length <= 0)
			{
				return;
			}

			if (runs.Count > 0 && runs[^1].Format.Equals(format))
			{
				runs[^1].Length += length;
			}
			else
			{
				runs.Add(new FormatRun(length, format));
			}
		}
	}

	internal static MathDocument Parse(string? value)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Length > MaxInputLength)
		{
			throw new ArgumentException("MathML cannot be empty or exceed the input limit.", nameof(value));
		}
		if (value.IndexOf("<!DOCTYPE", StringComparison.Ordinal) >= 0)
		{
			throw new ArgumentException("MathML document type declarations are not allowed.", nameof(value));
		}

		var settings = new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null,
			MaxCharactersFromEntities = 0,
			MaxCharactersInDocument = MaxInputLength,
		};
		using var stringReader = new StringReader(value);
		using var reader = XmlReader.Create(stringReader, settings);
		var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
		if (document.Root is not { } root
			|| root.Name.LocalName != "math"
			|| root.Name.NamespaceName != NamespaceName)
		{
			throw new ArgumentException("The root element must be a MathML math element.", nameof(value));
		}

		var parser = new MathMLParser();
		var parsedRoot = parser.ParseRoot(root);
		return Create(parsedRoot);
	}

	internal static MathDocument FromPlainText(string text)
		=> Create(
			new MathRowNode(
				MathStyle.Default,
				AsReadOnly<MathNode>(new MathTokenNode(MathStyle.Default, MathTokenKind.Text, text))));

	internal static string SerializePlainText(string text) => MathMLSerializer.SerializePlainText(text);

	internal static MathDocument CreateLinearUnicodeMath(string text)
	{
		var style = new MathStyle(MathVariant.Unspecified, Colors.Black, null);
		var children = new List<MathNode>();
		foreach (var rune in text.EnumerateRunes())
		{
			if (children.Count == MaxNodeCount)
			{
				return FromPlainText(text);
			}
			children.Add(CreateLiveToken(rune.ToString(), style));
		}
		return Create(new MathRowNode(MathStyle.Default, CopyAsReadOnly(children)), text);
	}

	private static MathDocument Create(MathRowNode root, string? liveInputText = null)
	{
		var projection = MathProjectionBuilder.Build(root);
		return new MathDocument(root, projection.Text, projection.Spans, projection.Atoms, liveInputText);
	}

	internal static string GetTokenProjection(MathTokenNode token)
	{
		if (!IsSingleUnicodeScalar(token.Text))
		{
			return token.Text;
		}

		var scalar = char.ConvertToUtf32(token.Text, 0);
		if (token.Kind == MathTokenKind.Identifier)
		{
			return token.Style.Variant switch
			{
				MathVariant.Normal => token.Text,
				MathVariant.Bold => MapAsciiLetter(scalar, 0x1D400, 0x1D41A),
				MathVariant.BoldItalic => MapAsciiLetter(scalar, 0x1D468, 0x1D482),
				MathVariant.Unspecified or MathVariant.Italic => MapItalicLetter(scalar),
				_ => token.Text,
			};
		}
		if (token.Kind == MathTokenKind.Number
			&& token.Style.Variant == MathVariant.Bold
			&& scalar is >= '0' and <= '9')
		{
			return char.ConvertFromUtf32(0x1D7CE + scalar - '0');
		}

		return token.Text;
	}

	private static bool IsStructuralMarker(char value)
		=> value is ObjectStart or ArgumentSeparator or ObjectEnd;

	private static bool TryMapProjectionBoundary(MathTokenNode token, int projectionOffset, out int tokenOffset)
	{
		var projected = token.ProjectionText;
		if ((uint)projectionOffset > (uint)projected.Length)
		{
			tokenOffset = 0;
			return false;
		}
		if (projected.Length == token.Text.Length)
		{
			tokenOffset = projectionOffset;
			return true;
		}
		if (projectionOffset == 0)
		{
			tokenOffset = 0;
			return true;
		}
		if (projectionOffset == projected.Length)
		{
			tokenOffset = token.Text.Length;
			return true;
		}

		tokenOffset = 0;
		return false;
	}

	private static int GetProjectedReplacementLength(MathTokenNode oldToken, string replacement)
	{
		if (replacement.Length == 0)
		{
			return 0;
		}
		if (oldToken.Kind != MathTokenKind.Identifier
			|| oldToken.Style.Variant == MathVariant.Normal
			|| !IsSingleUnicodeScalar(replacement))
		{
			return replacement.Length;
		}

		return GetTokenProjection(new MathTokenNode(oldToken.Style, oldToken.Kind, replacement)).Length;
	}

	private static MathNode ReplaceToken(
		MathNode node,
		MathTokenNode target,
		MathTokenNode replacement,
		ref bool replaced)
	{
		if (ReferenceEquals(node, target))
		{
			replaced = true;
			return replacement;
		}

		switch (node)
		{
			case MathRowNode row:
				{
					var children = ReplaceNodes(row.Children, target, replacement, ref replaced);
					if (row.CollapseSingleEditedToken
						&& children.Count == 1
						&& children[0] is MathTokenNode { Text: var text }
						&& IsSingleUnicodeScalar(text))
					{
						return children[0];
					}
					return ReferenceEquals(children, row.Children)
						? row
						: new MathRowNode(row.Style, children, row.CollapseSingleEditedToken);
				}
			case MathFractionNode fraction:
				{
					var numerator = ReplaceToken(fraction.Numerator, target, replacement, ref replaced);
					var denominator = ReplaceToken(fraction.Denominator, target, replacement, ref replaced);
					return ReferenceEquals(numerator, fraction.Numerator)
						&& ReferenceEquals(denominator, fraction.Denominator)
							? fraction
							: new MathFractionNode(fraction.Style, numerator, denominator);
				}
			case MathRadicalNode radical:
				{
					var radicand = ReplaceToken(radical.Radicand, target, replacement, ref replaced);
					var degree = radical.Degree is null
						? null
						: ReplaceToken(radical.Degree, target, replacement, ref replaced);
					return ReferenceEquals(radicand, radical.Radicand)
						&& ReferenceEquals(degree, radical.Degree)
							? radical
							: new MathRadicalNode(radical.Style, radicand, degree);
				}
			case MathScriptNode script:
				{
					var @base = ReplaceToken(script.Base, target, replacement, ref replaced);
					var subscript = script.Subscript is null
						? null
						: ReplaceToken(script.Subscript, target, replacement, ref replaced);
					var superscript = script.Superscript is null
						? null
						: ReplaceToken(script.Superscript, target, replacement, ref replaced);
					return ReferenceEquals(@base, script.Base)
						&& ReferenceEquals(subscript, script.Subscript)
						&& ReferenceEquals(superscript, script.Superscript)
							? script
							: new MathScriptNode(script.Style, @base, subscript, superscript);
				}
			case MathFencedNode fenced:
				{
					var content = ReplaceToken(fenced.Content, target, replacement, ref replaced);
					return ReferenceEquals(content, fenced.Content)
						? fenced
						: new MathFencedNode(fenced.Style, fenced.Open, fenced.Close, content);
				}
			case MathTableNode table:
				{
					var rowsChanged = false;
					var rows = new MathTableRow[table.Rows.Count];
					for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
					{
						var cells = ReplaceNodes(table.Rows[rowIndex].Cells, target, replacement, ref replaced);
						rows[rowIndex] = ReferenceEquals(cells, table.Rows[rowIndex].Cells)
							? table.Rows[rowIndex]
							: new MathTableRow(cells);
						rowsChanged |= !ReferenceEquals(rows[rowIndex], table.Rows[rowIndex]);
					}
					return rowsChanged
						? new MathTableNode(table.Style, Array.AsReadOnly(rows))
						: table;
				}
			case MathOverUnderNode overUnder:
				{
					var @base = ReplaceToken(overUnder.Base, target, replacement, ref replaced);
					var under = overUnder.Under is null
						? null
						: ReplaceToken(overUnder.Under, target, replacement, ref replaced);
					var over = overUnder.Over is null
						? null
						: ReplaceToken(overUnder.Over, target, replacement, ref replaced);
					var operand = overUnder.Operand is null
						? null
						: ReplaceToken(overUnder.Operand, target, replacement, ref replaced);
					return ReferenceEquals(@base, overUnder.Base)
						&& ReferenceEquals(under, overUnder.Under)
						&& ReferenceEquals(over, overUnder.Over)
						&& ReferenceEquals(operand, overUnder.Operand)
							? overUnder
							: new MathOverUnderNode(
								overUnder.Style,
								overUnder.Kind,
								@base,
								under,
								over,
								operand);
				}
			case MathMultiScriptsNode multiScripts:
				{
					var body = ReplaceToken(multiScripts.Body, target, replacement, ref replaced);
					var pairsChanged = false;
					var pairs = new MathScriptPair[multiScripts.Prescripts.Count];
					for (var index = 0; index < pairs.Length; index++)
					{
						var pair = multiScripts.Prescripts[index];
						var subscript = pair.Subscript is null
							? null
							: ReplaceToken(pair.Subscript, target, replacement, ref replaced);
						var superscript = pair.Superscript is null
							? null
							: ReplaceToken(pair.Superscript, target, replacement, ref replaced);
						pairs[index] = ReferenceEquals(subscript, pair.Subscript)
							&& ReferenceEquals(superscript, pair.Superscript)
								? pair
								: new MathScriptPair(subscript, superscript);
						pairsChanged |= !ReferenceEquals(pairs[index], pair);
					}
					return ReferenceEquals(body, multiScripts.Body) && !pairsChanged
						? multiScripts
						: new MathMultiScriptsNode(
							multiScripts.Style,
							body,
							Array.AsReadOnly(pairs));
				}
			default:
				return node;
		}
	}

	private static IReadOnlyList<MathNode> ReplaceNodes(
		IReadOnlyList<MathNode> nodes,
		MathTokenNode target,
		MathTokenNode replacement,
		ref bool replaced)
	{
		MathNode[]? copy = null;
		for (var index = 0; index < nodes.Count; index++)
		{
			var child = ReplaceToken(nodes[index], target, replacement, ref replaced);
			if (!ReferenceEquals(child, nodes[index]))
			{
				copy ??= nodes.ToArray();
				copy[index] = child;
			}
		}

		return copy is null ? nodes : Array.AsReadOnly(copy);
	}

	private static MathRowNode CreateLiveRow(string value, MathStyle style)
	{
		var children = new List<MathNode>();
		foreach (var rune in value.EnumerateRunes())
		{
			children.Add(CreateLiveToken(rune.ToString(), style));
		}
		return new MathRowNode(MathStyle.Default, CopyAsReadOnly(children));
	}

	private static MathTokenNode CreateLiveToken(string value, MathStyle style)
		=> new(
			style,
			value.Length > 0 && char.IsLetter(value, 0)
				? MathTokenKind.Identifier
				: value.Length > 0 && char.IsDigit(value, 0)
					? MathTokenKind.Number
					: MathTokenKind.Operator,
			value,
			fenceFalse: value is "(" or ")");

	private static string MapAsciiLetter(int scalar, int upperStart, int lowerStart)
		=> scalar switch
		{
			>= 'A' and <= 'Z' => char.ConvertFromUtf32(upperStart + scalar - 'A'),
			>= 'a' and <= 'z' => char.ConvertFromUtf32(lowerStart + scalar - 'a'),
			_ => char.ConvertFromUtf32(scalar),
		};

	private static string MapItalicLetter(int scalar)
		=> scalar switch
		{
			'h' => "\u210E",
			>= 'A' and <= 'Z' => char.ConvertFromUtf32(0x1D434 + scalar - 'A'),
			>= 'a' and <= 'z' => char.ConvertFromUtf32(0x1D44E + scalar - 'a'),
			_ => char.ConvertFromUtf32(scalar),
		};

	private static void ApplyStyle(CharacterFormatState state, MathTokenNode atom)
	{
		if (atom.Style.Foreground is { } foreground)
		{
			state.Foreground = foreground;
		}
		if (atom.Style.Background is { } background)
		{
			state.Background = background;
		}

		switch (atom.Style.Variant)
		{
			case MathVariant.Normal:
				state.Bold = false;
				state.Weight = 400;
				state.Italic = false;
				break;
			case MathVariant.Bold:
				state.Bold = true;
				state.Weight = 700;
				state.Italic = false;
				break;
			case MathVariant.Italic:
				state.Italic = true;
				break;
			case MathVariant.BoldItalic:
				state.Bold = true;
				state.Weight = 700;
				state.Italic = true;
				break;
			case MathVariant.Unspecified:
				if (atom.Kind == MathTokenKind.Identifier && IsSingleUnicodeScalar(atom.Text))
				{
					state.Italic = true;
				}
				break;
		}
	}

	private static bool IsSingleUnicodeScalar(string text)
		=> text.Length == 1 || text.Length == 2 && char.IsSurrogatePair(text, 0);

	private static ReadOnlyCollection<T> AsReadOnly<T>(params T[] values)
		=> Array.AsReadOnly(values);

	private sealed class MathMLParser
	{
		private int _nodeCount;
		private int _textLength;

		internal MathRowNode ParseRoot(XElement root)
		{
			ValidateElement(root, 1);
			var style = ReadStyle(root, MathStyle.Default);
			return CreateRow(style, ParseChildren(root, style, 2), detectFences: true);
		}

		private MathNode ParseElement(XElement element, MathStyle inheritedStyle, int depth)
		{
			ValidateElement(element, depth);
			var style = ReadStyle(element, inheritedStyle);
			if (element.Name.NamespaceName != NamespaceName)
			{
				return ParseUnknown(element, style, depth);
			}

			return element.Name.LocalName switch
			{
				"math" or "mrow" or "mstyle" or "mpadded" or "menclose" or "merror"
					=> CreateRow(style, ParseChildren(element, style, depth + 1), detectFences: true),
				"mi" => ParseToken(element, style, MathTokenKind.Identifier),
				"mn" => ParseToken(element, style, MathTokenKind.Number),
				"mo" => ParseToken(element, style, MathTokenKind.Operator),
				"mtext" or "ms" => ParseToken(element, style, MathTokenKind.Text),
				"mspace" => ParseTextToken(style, " "),
				"mglyph" => ParseTextToken(style, GetAttribute(element, "alt") ?? string.Empty),
				"mfrac" => ParseFraction(element, style, depth),
				"msqrt" => ParseSquareRoot(element, style, depth),
				"mroot" => ParseRoot(element, style, depth),
				"msub" => ParseScript(element, style, depth, hasSubscript: true, hasSuperscript: false),
				"msup" => ParseScript(element, style, depth, hasSubscript: false, hasSuperscript: true),
				"msubsup" => ParseScript(element, style, depth, hasSubscript: true, hasSuperscript: true),
				"mfenced" => ParseFenced(element, style, depth),
				"mtable" => ParseTable(element, style, depth),
				"mover" => ParseOverUnder(element, style, depth, MathOverUnderKind.Mover),
				"munder" => ParseOverUnder(element, style, depth, MathOverUnderKind.Munder),
				"munderover" => ParseOverUnder(element, style, depth, MathOverUnderKind.Munderover),
				"mmultiscripts" => ParseMultiScripts(element, style, depth),
				"mtd" => CreateRow(style, ParseChildren(element, style, depth + 1), detectFences: true),
				"semantics" => ParseSemantics(element, style, depth),
				"maction" => ParseAction(element, style, depth),
				"annotation" or "annotation-xml" or "mphantom" or "maligngroup" or "malignmark"
					or "mprescripts" or "none" => CreateRow(style, Array.Empty<MathNode>(), detectFences: false),
				_ => ParseUnknown(element, style, depth),
			};
		}

		private MathNode ParseToken(XElement element, MathStyle style, MathTokenKind kind)
		{
			var text = NormalizeWhitespace(string.Concat(element.Nodes().OfType<XText>().Select(node => node.Value)));
			if (kind == MathTokenKind.Operator)
			{
				text = NormalizeOperator(text);
			}

			return ParseTextToken(
				style,
				text,
				kind,
				fenceFalse: kind == MathTokenKind.Operator
					&& string.Equals(GetAttribute(element, "fence"), "false", StringComparison.OrdinalIgnoreCase));
		}

		private MathTokenNode ParseTextToken(
			MathStyle style,
			string text,
			MathTokenKind kind = MathTokenKind.Text,
			bool fenceFalse = false)
		{
			if (text.Length > MaxProjectionLength - _textLength)
			{
				throw new ArgumentException("The projected MathML text is too large.");
			}

			_textLength += text.Length;
			return new MathTokenNode(style, kind, text, fenceFalse);
		}

		private MathNode ParseFraction(XElement element, MathStyle style, int depth)
		{
			var children = ParseElementChildren(element, style, depth + 1);
			RequireArity(element, children, 2);
			return new MathFractionNode(
				style,
				WrapCanonicalArgument(children[0]),
				WrapCanonicalArgument(children[1]));
		}

		private MathNode ParseSquareRoot(XElement element, MathStyle style, int depth)
		{
			var children = ParseChildren(element, style, depth + 1);
			if (children.Count == 0)
			{
				throw new ArgumentException("msqrt requires a radicand.");
			}

			return new MathRadicalNode(style, CreateRow(style, children, detectFences: true), degree: null);
		}

		private MathNode ParseRoot(XElement element, MathStyle style, int depth)
		{
			var children = ParseElementChildren(element, style, depth + 1);
			RequireArity(element, children, 2);
			return new MathRadicalNode(style, children[0], children[1]);
		}

		private MathNode ParseScript(
			XElement element,
			MathStyle style,
			int depth,
			bool hasSubscript,
			bool hasSuperscript)
		{
			var children = ParseElementChildren(element, style, depth + 1);
			RequireArity(element, children, 1 + (hasSubscript ? 1 : 0) + (hasSuperscript ? 1 : 0));
			var index = 1;
			var subscript = hasSubscript ? children[index++] : null;
			var superscript = hasSuperscript ? children[index] : null;
			return new MathScriptNode(style, children[0], subscript, superscript);
		}

		private MathNode ParseFenced(XElement element, MathStyle style, int depth)
		{
			var open = NormalizeFence(GetAttribute(element, "open"), "(");
			var close = NormalizeFence(GetAttribute(element, "close"), ")");
			var separators = NormalizeSeparators(GetAttribute(element, "separators"));
			var sourceChildren = ParseElementChildren(element, style, depth + 1);
			var content = new List<MathNode>();
			var separatorStyle = style with { Foreground = style.Foreground ?? Colors.Black };
			for (var index = 0; index < sourceChildren.Count; index++)
			{
				if (index > 0)
				{
					var separator = separators.Length == 0
						? string.Empty
						: separators[Math.Min(index - 1, separators.Length - 1)].ToString();
					if (separator.Length > 0)
					{
						content.Add(ParseTextToken(separatorStyle, separator, MathTokenKind.Operator));
					}
				}
				content.Add(sourceChildren[index]);
			}
			return new MathFencedNode(
				style,
				open,
				close,
				new MathRowNode(style, CopyAsReadOnly(content)));
		}

		private MathNode ParseOverUnder(
			XElement element,
			MathStyle style,
			int depth,
			MathOverUnderKind kind)
		{
			var children = ParseElementChildren(element, style, depth + 1);
			RequireArity(element, children, kind == MathOverUnderKind.Munderover ? 3 : 2);
			if (kind == MathOverUnderKind.Mover)
			{
				var over = NormalizeMoverAccent(children[1]);
				return new MathOverUnderNode(style, kind, children[0], under: null, over);
			}
			if (kind == MathOverUnderKind.Munder)
			{
				return new MathOverUnderNode(style, kind, children[0], children[1], over: null);
			}
			if (IsNaryOperator(children[0]))
			{
				return new MathOverUnderNode(
					style,
					MathOverUnderKind.Nary,
					children[0],
					children[1],
					children[2],
					new MathRowNode(style, Array.Empty<MathNode>()));
			}

			return new MathOverUnderNode(style, kind, children[0], children[1], children[2]);
		}

		private MathNode ParseMultiScripts(XElement element, MathStyle style, int depth)
		{
			var elements = element.Elements().ToList();
			if (elements.Count == 0)
			{
				throw new ArgumentException("mmultiscripts requires a base.");
			}

			var @base = ParseElement(elements[0], style, depth + 1);
			var postScripts = new List<MathScriptPair>();
			var prescripts = new List<MathScriptPair>();
			var active = postScripts;
			for (var index = 1; index < elements.Count;)
			{
				if (elements[index].Name.NamespaceName == NamespaceName
					&& elements[index].Name.LocalName == "mprescripts")
				{
					ValidateElement(elements[index], depth + 1);
					if (ReferenceEquals(active, prescripts))
					{
						throw new ArgumentException("mmultiscripts can only contain one mprescripts marker.");
					}
					active = prescripts;
					index++;
					continue;
				}
				if (index + 1 >= elements.Count)
				{
					throw new ArgumentException("mmultiscripts requires subscript/superscript pairs.");
				}

				active.Add(new MathScriptPair(
					ParseOptionalScript(elements[index], style, depth + 1),
					ParseOptionalScript(elements[index + 1], style, depth + 1)));
				index += 2;
			}

			MathNode body = @base;
			foreach (var pair in postScripts)
			{
				body = new MathScriptNode(style, body, pair.Subscript, pair.Superscript);
			}
			if (postScripts.Count > 0)
			{
				body = new MathRowNode(style, AsReadOnly(body));
			}

			return new MathMultiScriptsNode(style, body, CopyAsReadOnly(prescripts));
		}

		private MathNode? ParseOptionalScript(XElement element, MathStyle style, int depth)
		{
			if (element.Name.NamespaceName == NamespaceName && element.Name.LocalName == "none")
			{
				ValidateElement(element, depth);
				return null;
			}

			return ParseElement(element, style, depth);
		}

		private static MathNode WrapCanonicalArgument(MathNode node)
			=> node is MathTokenNode { Kind: MathTokenKind.Identifier, Text.Length: > 1 }
				? new MathRowNode(node.Style, AsReadOnly(node), collapseSingleEditedToken: true)
				: node;

		private static MathNode NormalizeMoverAccent(MathNode node)
			=> node is MathTokenNode { Kind: MathTokenKind.Operator, Text: "¯" } token
				? new MathTokenNode(token.Style, token.Kind, "-", token.FenceFalse)
				: node;

		private static bool IsNaryOperator(MathNode node)
			=> node is MathTokenNode { Kind: MathTokenKind.Operator, Text: "∑" or "∏" or "∐" or "∫" or "∬" or "∭" };

		private MathNode ParseTable(XElement element, MathStyle style, int depth)
		{
			var rows = new List<MathTableRow>();
			foreach (var rowElement in element.Elements())
			{
				if (rowElement.Name.NamespaceName != NamespaceName
					|| rowElement.Name.LocalName is not ("mtr" or "mlabeledtr"))
				{
					throw new ArgumentException("mtable can only contain mtr rows.");
				}

				ValidateElement(rowElement, depth + 1);
				if (rows.Count >= MaxTableRows)
				{
					throw new ArgumentException("The MathML table has too many rows.");
				}

				var cells = new List<MathNode>();
				foreach (var cellElement in rowElement.Elements())
				{
					if (cellElement.Name.NamespaceName != NamespaceName || cellElement.Name.LocalName != "mtd")
					{
						throw new ArgumentException("mtr can only contain mtd cells.");
					}
					if (cells.Count >= MaxTableColumns)
					{
						throw new ArgumentException("The MathML table has too many columns.");
					}

					cells.Add(ParseElement(cellElement, style, depth + 2));
				}

				rows.Add(new MathTableRow(CopyAsReadOnly(cells)));
			}

			return new MathTableNode(style, CopyAsReadOnly(rows));
		}

		private MathNode ParseSemantics(XElement element, MathStyle style, int depth)
		{
			var presentation = element.Elements().FirstOrDefault(
				child => child.Name.LocalName is not ("annotation" or "annotation-xml"));
			return presentation is null
				? CreateRow(style, Array.Empty<MathNode>(), detectFences: false)
				: ParseElement(presentation, style, depth + 1);
		}

		private MathNode ParseAction(XElement element, MathStyle style, int depth)
			=> element.Elements().FirstOrDefault() is { } selected
				? ParseElement(selected, style, depth + 1)
				: CreateRow(style, Array.Empty<MathNode>(), detectFences: false);

		private MathNode ParseUnknown(XElement element, MathStyle style, int depth)
		{
			var children = ParseElementChildren(element, style, depth + 1);
			if (children.Count > 0)
			{
				return CreateRow(style, children, detectFences: true);
			}

			return ParseTextToken(style, NormalizeWhitespace(element.Value));
		}

		private IReadOnlyList<MathNode> ParseChildren(XElement element, MathStyle style, int depth)
		{
			var children = new List<MathNode>();
			foreach (var node in element.Nodes())
			{
				if (node is XElement child)
				{
					children.Add(ParseElement(child, style, depth));
				}
				else if (node is XText text)
				{
					var normalized = NormalizeWhitespace(text.Value);
					if (normalized.Length > 0)
					{
						children.Add(ParseTextToken(style, normalized));
					}
				}
			}

			return CopyAsReadOnly(children);
		}

		private IReadOnlyList<MathNode> ParseElementChildren(XElement element, MathStyle style, int depth)
			=> CopyAsReadOnly(element.Elements().Select(child => ParseElement(child, style, depth)).ToList());

		private MathRowNode CreateRow(MathStyle style, IReadOnlyList<MathNode> children, bool detectFences)
		{
			if (detectFences
				&& children.Count >= 3
				&& children[0] is MathTokenNode { Kind: MathTokenKind.Operator } open
				&& children[^1] is MathTokenNode { Kind: MathTokenKind.Operator } close
				&& IsMatchingFence(open.Text, close.Text))
			{
				var interior = new MathNode[children.Count - 2];
				for (var index = 0; index < interior.Length; index++)
				{
					interior[index] = children[index + 1];
				}

				return new MathRowNode(
					style,
					AsReadOnly<MathNode>(
						new MathFencedNode(
							style,
							open.Text,
							close.Text,
							new MathRowNode(style, Array.AsReadOnly(interior)))));
			}

			return new MathRowNode(style, children);
		}

		private void ValidateElement(XElement element, int depth)
		{
			if (depth > MaxDepth || ++_nodeCount > MaxNodeCount)
			{
				throw new ArgumentException("The MathML document is too complex.");
			}

			foreach (var attribute in element.Attributes())
			{
				if (attribute.Value.Length > MaxAttributeLength)
				{
					throw new ArgumentException("The MathML attribute value is too long.");
				}
			}
		}

		private static void RequireArity(XElement element, IReadOnlyList<MathNode> children, int count)
		{
			if (children.Count != count)
			{
				throw new ArgumentException($"{element.Name.LocalName} requires exactly {count} children.");
			}
		}

		private static MathStyle ReadStyle(XElement element, MathStyle inherited)
		{
			var variant = GetAttribute(element, "mathvariant")?.ToLowerInvariant() switch
			{
				"normal" => MathVariant.Normal,
				"bold" => MathVariant.Bold,
				"italic" => MathVariant.Italic,
				"bold-italic" => MathVariant.BoldItalic,
				_ => inherited.Variant,
			};
			var foreground = TryParseColor(GetAttribute(element, "mathcolor"), out var parsedForeground)
				? parsedForeground
				: inherited.Foreground;
			var background = TryParseColor(GetAttribute(element, "mathbackground"), out var parsedBackground)
				? parsedBackground
				: inherited.Background;
			return new MathStyle(variant, foreground, background);
		}

		private static string NormalizeFence(string? value, string fallback)
		{
			var normalized = NormalizeWhitespace(value ?? string.Empty);
			if (normalized.Length == 0)
			{
				return fallback;
			}
			if (normalized.Length > 8)
			{
				throw new ArgumentException("The MathML fence is too long.");
			}

			return normalized;
		}

		private static string NormalizeSeparators(string? value)
		{
			if (value is null)
			{
				return ",";
			}

			var builder = new StringBuilder();
			foreach (var character in value)
			{
				if (!char.IsWhiteSpace(character))
				{
					builder.Append(character);
				}
			}

			if (builder.Length > 16)
			{
				throw new ArgumentException("The MathML separator list is too long.");
			}

			return builder.ToString();
		}

		private static bool IsMatchingFence(string open, string close)
			=> (open, close) is ("(", ")") or ("[", "]") or ("{", "}") or ("|", "|")
				or ("⌈", "⌉") or ("⌊", "⌋") or ("⟨", "⟩");
	}

	private sealed class MathProjectionBuilder
	{
		private readonly StringBuilder _text = new();
		private readonly Dictionary<MathNode, MathTextSpan> _spans = new(ReferenceComparer<MathNode>.Instance);
		private readonly List<MathAtomSpan> _atoms = new();

		private MathProjectionBuilder()
		{
		}

		internal static (string Text, Dictionary<MathNode, MathTextSpan> Spans, IReadOnlyList<MathAtomSpan> Atoms) Build(MathRowNode root)
		{
			var builder = new MathProjectionBuilder();
			builder.Append(root);
			return (builder._text.ToString(), builder._spans, CopyAsReadOnly(builder._atoms));
		}

		private void Append(MathNode node)
		{
			var start = _text.Length;
			switch (node)
			{
				case MathRowNode row:
					foreach (var child in row.Children)
					{
						Append(child);
					}
					break;
				case MathTokenNode token:
					AppendText(token.ProjectionText);
					break;
				case MathFractionNode fraction:
					AppendMarker(ObjectStart);
					Append(fraction.Numerator);
					AppendMarker(ArgumentSeparator);
					Append(fraction.Denominator);
					AppendMarker(ObjectEnd);
					break;
				case MathRadicalNode radical:
					AppendMarker(ObjectStart);
					if (radical.Degree is { } degree)
					{
						Append(degree);
					}
					AppendMarker(ArgumentSeparator);
					Append(radical.Radicand);
					AppendMarker(ObjectEnd);
					break;
				case MathScriptNode script:
					AppendMarker(ObjectStart);
					Append(script.Base);
					if (script.Subscript is { } subscript)
					{
						AppendMarker(ArgumentSeparator);
						Append(subscript);
					}
					if (script.Superscript is { } superscript)
					{
						AppendMarker(ArgumentSeparator);
						Append(superscript);
					}
					AppendMarker(ObjectEnd);
					break;
				case MathFencedNode fenced:
					AppendMarker(ObjectStart);
					Append(fenced.Content);
					AppendMarker(ObjectEnd);
					break;
				case MathTableNode table:
					AppendMarker(ObjectStart);
					var firstCell = true;
					for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
					{
						var row = table.Rows[rowIndex];
						for (var columnIndex = 0; columnIndex < row.Cells.Count; columnIndex++)
						{
							if (!firstCell)
							{
								AppendMarker(ArgumentSeparator);
							}
							Append(row.Cells[columnIndex]);
							firstCell = false;
						}
					}
					AppendMarker(ObjectEnd);
					break;
				case MathOverUnderNode overUnder:
					AppendMarker(ObjectStart);
					if (overUnder.Kind == MathOverUnderKind.Nary)
					{
						AppendHidden(overUnder.Base);
						AppendOptional(overUnder.Under);
						AppendMarker(ArgumentSeparator);
						AppendOptional(overUnder.Over);
						AppendMarker(ArgumentSeparator);
						AppendOptional(overUnder.Operand);
					}
					else if (overUnder.Kind is MathOverUnderKind.Mover or MathOverUnderKind.Munder)
					{
						Append(overUnder.Base);
						AppendHidden(overUnder.Under);
						AppendHidden(overUnder.Over);
					}
					else
					{
						Append(overUnder.Base);
						AppendMarker(ArgumentSeparator);
						AppendOptional(overUnder.Under);
						AppendMarker(ArgumentSeparator);
						AppendOptional(overUnder.Over);
					}
					AppendMarker(ObjectEnd);
					break;
				case MathMultiScriptsNode multiScripts:
					AppendMarker(ObjectStart);
					foreach (var pair in multiScripts.Prescripts)
					{
						AppendOptional(pair.Subscript);
						AppendMarker(ArgumentSeparator);
						AppendOptional(pair.Superscript);
						AppendMarker(ArgumentSeparator);
					}
					Append(multiScripts.Body);
					AppendMarker(ObjectEnd);
					break;
			}

			var span = new MathTextSpan(start, _text.Length - start);
			_spans.Add(node, span);
			if (node is MathTokenNode atom)
			{
				_atoms.Add(new MathAtomSpan(atom, span));
			}
		}

		private void AppendOptional(MathNode? node)
		{
			if (node is not null)
			{
				Append(node);
			}
		}

		private void AppendHidden(MathNode? node)
		{
			if (node is null)
			{
				return;
			}

			var position = _text.Length;
			switch (node)
			{
				case MathRowNode row:
					foreach (var child in row.Children)
					{
						AppendHidden(child);
					}
					break;
				case MathFractionNode fraction:
					AppendHidden(fraction.Numerator);
					AppendHidden(fraction.Denominator);
					break;
				case MathRadicalNode radical:
					AppendHidden(radical.Degree);
					AppendHidden(radical.Radicand);
					break;
				case MathScriptNode script:
					AppendHidden(script.Base);
					AppendHidden(script.Subscript);
					AppendHidden(script.Superscript);
					break;
				case MathFencedNode fenced:
					AppendHidden(fenced.Content);
					break;
				case MathTableNode table:
					foreach (var row in table.Rows)
					{
						foreach (var cell in row.Cells)
						{
							AppendHidden(cell);
						}
					}
					break;
				case MathOverUnderNode overUnder:
					AppendHidden(overUnder.Base);
					AppendHidden(overUnder.Under);
					AppendHidden(overUnder.Over);
					AppendHidden(overUnder.Operand);
					break;
				case MathMultiScriptsNode multiScripts:
					AppendHidden(multiScripts.Body);
					foreach (var pair in multiScripts.Prescripts)
					{
						AppendHidden(pair.Subscript);
						AppendHidden(pair.Superscript);
					}
					break;
			}
			_spans.Add(node, new MathTextSpan(position, 0));
		}

		private void AppendMarker(char marker) => AppendText(marker.ToString());

		private void AppendText(string value)
		{
			if (value.Length > MaxProjectionLength - _text.Length)
			{
				throw new ArgumentException("The projected MathML text is too large.");
			}

			_text.Append(value);
		}
	}

	private static class MathMLSerializer
	{
		internal static string SerializePlainText(string text)
		{
			var builder = new StringBuilder(text.Length + 128);
			using (var writer = XmlWriter.Create(builder, CreateSettings()))
			{
				WriteRootStart(writer);
				writer.WriteStartElement("mml", "mtext", NamespaceName);
				writer.WriteString(text);
				writer.WriteEndElement();
				writer.WriteEndElement();
			}

			return builder.ToString();
		}

		internal static string Serialize(MathRowNode root)
		{
			var builder = new StringBuilder();
			using (var writer = XmlWriter.Create(builder, CreateSettings()))
			{
				WriteRootStart(writer);
				foreach (var child in root.Children)
				{
					WriteNode(writer, child);
				}
				writer.WriteEndElement();
			}

			return builder.ToString();
		}

		private static XmlWriterSettings CreateSettings()
			=> new()
			{
				ConformanceLevel = ConformanceLevel.Fragment,
				Indent = false,
				OmitXmlDeclaration = true,
			};

		private static void WriteRootStart(XmlWriter writer)
		{
			writer.WriteStartElement("mml", "math", NamespaceName);
			writer.WriteAttributeString("xmlns", "mml", null, NamespaceName);
			writer.WriteAttributeString("display", "block");
		}

		private static void WriteNode(XmlWriter writer, MathNode node)
		{
			switch (node)
			{
				case MathRowNode row:
					writer.WriteStartElement("mml", "mrow", NamespaceName);
					WriteStyle(writer, row.Style);
					foreach (var child in row.Children)
					{
						WriteNode(writer, child);
					}
					writer.WriteEndElement();
					break;
				case MathTokenNode token:
					WriteToken(writer, token);
					break;
				case MathFractionNode fraction:
					writer.WriteStartElement("mml", "mfrac", NamespaceName);
					WriteStyle(writer, fraction.Style);
					WriteNode(writer, fraction.Numerator);
					WriteNode(writer, fraction.Denominator);
					writer.WriteEndElement();
					break;
				case MathRadicalNode { Degree: null } radical:
					writer.WriteStartElement("mml", "msqrt", NamespaceName);
					WriteStyle(writer, radical.Style);
					WriteNode(writer, radical.Radicand);
					writer.WriteEndElement();
					break;
				case MathRadicalNode radical:
					writer.WriteStartElement("mml", "mroot", NamespaceName);
					WriteStyle(writer, radical.Style);
					WriteNode(writer, radical.Radicand);
					WriteNode(writer, radical.Degree!);
					writer.WriteEndElement();
					break;
				case MathScriptNode script:
					var scriptName = script.Subscript is not null && script.Superscript is not null
						? "msubsup"
						: script.Subscript is not null ? "msub" : "msup";
					writer.WriteStartElement("mml", scriptName, NamespaceName);
					WriteStyle(writer, script.Style);
					WriteNode(writer, script.Base);
					if (script.Subscript is { } subscript)
					{
						WriteNode(writer, subscript);
					}
					if (script.Superscript is { } superscript)
					{
						WriteNode(writer, superscript);
					}
					writer.WriteEndElement();
					break;
				case MathFencedNode fenced:
					writer.WriteStartElement("mml", "mfenced", NamespaceName);
					WriteStyle(writer, fenced.Style);
					if (fenced.Open != "(")
					{
						writer.WriteAttributeString("open", fenced.Open);
					}
					if (fenced.Close != ")")
					{
						writer.WriteAttributeString("close", fenced.Close);
					}
					WriteNode(writer, fenced.Content);
					writer.WriteEndElement();
					break;
				case MathTableNode table:
					writer.WriteStartElement("mml", "mtable", NamespaceName);
					WriteStyle(writer, table.Style);
					foreach (var row in table.Rows)
					{
						writer.WriteStartElement("mml", "mtr", NamespaceName);
						foreach (var cell in row.Cells)
						{
							writer.WriteStartElement("mml", "mtd", NamespaceName);
							WriteNode(writer, cell);
							writer.WriteEndElement();
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
					break;
				case MathOverUnderNode overUnder:
					WriteOverUnder(writer, overUnder);
					break;
				case MathMultiScriptsNode multiScripts:
					writer.WriteStartElement("mml", "mmultiscripts", NamespaceName);
					WriteStyle(writer, multiScripts.Style);
					WriteNode(writer, multiScripts.Body);
					if (multiScripts.Prescripts.Count > 0)
					{
						writer.WriteStartElement("mml", "mprescripts", NamespaceName);
						writer.WriteEndElement();
						foreach (var pair in multiScripts.Prescripts)
						{
							WriteOptionalScript(writer, pair.Subscript);
							WriteOptionalScript(writer, pair.Superscript);
						}
					}
					writer.WriteEndElement();
					break;
			}
		}

		private static void WriteToken(XmlWriter writer, MathTokenNode token, bool stretchy = false)
		{
			var tokenName = token.Kind switch
			{
				MathTokenKind.Identifier => "mi",
				MathTokenKind.Number => "mn",
				MathTokenKind.Operator => "mo",
				_ => "mtext",
			};
			writer.WriteStartElement("mml", tokenName, NamespaceName);
			WriteStyle(writer, token.Style);
			if (token.FenceFalse)
			{
				writer.WriteAttributeString("fence", "false");
			}
			if (stretchy)
			{
				writer.WriteAttributeString("stretchy", "true");
			}
			writer.WriteString(token.Text);
			writer.WriteEndElement();
		}

		private static void WriteOverUnder(XmlWriter writer, MathOverUnderNode node)
		{
			var elementName = node.Kind switch
			{
				MathOverUnderKind.Mover => "mover",
				MathOverUnderKind.Munder => "munder",
				_ => "munderover",
			};
			writer.WriteStartElement("mml", elementName, NamespaceName);
			WriteStyle(writer, node.Style);
			if (node.Kind == MathOverUnderKind.Mover)
			{
				writer.WriteAttributeString("accent", "true");
			}
			else if (node.Kind == MathOverUnderKind.Munder)
			{
				writer.WriteAttributeString("accentunder", "false");
			}
			WriteNode(writer, node.Base);
			if (node.Under is { } under)
			{
				if (node.Kind == MathOverUnderKind.Munder
					&& under is MathTokenNode { Kind: MathTokenKind.Operator, Text: "_" } token)
				{
					WriteToken(writer, token, stretchy: true);
				}
				else
				{
					WriteNode(writer, under);
				}
			}
			if (node.Over is { } over)
			{
				WriteNode(writer, over);
			}
			writer.WriteEndElement();
			if (node.Kind == MathOverUnderKind.Nary && node.Operand is { } operand)
			{
				WriteNode(writer, operand);
			}
		}

		private static void WriteOptionalScript(XmlWriter writer, MathNode? node)
		{
			if (node is not null)
			{
				WriteNode(writer, node);
				return;
			}

			writer.WriteStartElement("mml", "none", NamespaceName);
			writer.WriteEndElement();
		}

		private static void WriteStyle(XmlWriter writer, MathStyle style)
		{
			var variant = style.Variant switch
			{
				MathVariant.Normal => "normal",
				MathVariant.Bold => "bold",
				MathVariant.Italic => "italic",
				MathVariant.BoldItalic => "bold-italic",
				_ => null,
			};
			if (variant is not null)
			{
				writer.WriteAttributeString("mathvariant", variant);
			}
			if (style.Foreground is { } foreground)
			{
				writer.WriteAttributeString("mathcolor", FormatColor(foreground));
			}
			if (style.Background is { } background)
			{
				writer.WriteAttributeString("mathbackground", FormatColor(background));
			}
		}

		private static string FormatColor(Color color)
			=> color.A == byte.MaxValue
				? FormattableString.Invariant($"#{color.R:X2}{color.G:X2}{color.B:X2}")
				: FormattableString.Invariant($"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}");
	}

	private sealed class ReferenceComparer<T> : IEqualityComparer<T>
		where T : class
	{
		internal static ReferenceComparer<T> Instance { get; } = new();

		public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

		public int GetHashCode(T obj) => global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
	}

	private static ReadOnlyCollection<T> CopyAsReadOnly<T>(IReadOnlyList<T> values)
	{
		var copy = new T[values.Count];
		for (var index = 0; index < values.Count; index++)
		{
			copy[index] = values[index];
		}

		return Array.AsReadOnly(copy);
	}

	private static string? GetAttribute(XElement element, string localName)
		=> element.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == localName)?.Value;

	private static string NormalizeWhitespace(string value)
	{
		var builder = new StringBuilder(value.Length);
		var pendingSpace = false;
		foreach (var character in value)
		{
			if (char.IsWhiteSpace(character))
			{
				pendingSpace = builder.Length > 0;
			}
			else
			{
				if (pendingSpace)
				{
					builder.Append(' ');
					pendingSpace = false;
				}
				builder.Append(character);
			}
		}

		return builder.ToString();
	}

	private static string NormalizeOperator(string value)
		=> value switch
		{
			"-" => "−",
			"*" => "×",
			"<=" => "≤",
			">=" => "≥",
			"!=" => "≠",
			"->" => "→",
			_ => value,
		};

	private static bool TryParseColor(string? value, out Color color)
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
				&& byte.TryParse(new string(hex[0], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var shortRed)
				&& byte.TryParse(new string(hex[1], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var shortGreen)
				&& byte.TryParse(new string(hex[2], 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var shortBlue))
			{
				color = Color.FromArgb(byte.MaxValue, shortRed, shortGreen, shortBlue);
				return true;
			}
			if (hex.Length == 6
				&& byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red)
				&& byte.TryParse(hex.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green)
				&& byte.TryParse(hex.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
			{
				color = Color.FromArgb(byte.MaxValue, red, green, blue);
				return true;
			}
			if (hex.Length == 8
				&& byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out red)
				&& byte.TryParse(hex.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out green)
				&& byte.TryParse(hex.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out blue)
				&& byte.TryParse(hex.Slice(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var alpha))
			{
				color = Color.FromArgb(alpha, red, green, blue);
				return true;
			}
		}

		color = value.ToLowerInvariant() switch
		{
			"black" => Colors.Black,
			"white" => Colors.White,
			"red" => Colors.Red,
			"green" => Colors.Green,
			"blue" => Colors.Blue,
			"yellow" => Colors.Yellow,
			"gray" or "grey" => Colors.Gray,
			"transparent" => Colors.Transparent,
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
