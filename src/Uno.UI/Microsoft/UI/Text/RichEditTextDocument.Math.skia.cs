#nullable enable

using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Text;

public partial class RichEditTextDocument
{
	internal const string MathFontFamilyName = "Cambria Math";
	private static readonly Lazy<string> _mathRenderingFontFamilyName = new(ResolveMathRenderingFontFamilyName);

	private global::Microsoft.UI.Text.RichEditMathMode _mathMode;
	private MathDocument? _mathDocument;
	private bool _mathMLUnavailable;

	internal bool IsMathMode => _mathMode == global::Microsoft.UI.Text.RichEditMathMode.MathOnly;

	internal MathDocument? StructuredMath => _mathDocument;

	internal string? MathProjection => _mathDocument?.Projection;

	internal IReadOnlyList<MathAtomSpan> MathAtoms
		=> _mathDocument?.Atoms ?? Array.Empty<MathAtomSpan>();

	internal static string MathRenderingFontFamilyName => _mathRenderingFontFamilyName.Value;

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

		SetDocumentFragment(EmptyMathFragment(), mathDocument: null);
		_mathMode = mode;
		_mathDocument = null;
		_mathMLUnavailable = false;
		ClearUndoRedoHistory();
		_owner.OnDocumentMathModeChanged();
	}

	/// <summary>Retrieves the RichEditBox content as canonical MathML.</summary>
	public void GetMathML(out string value)
	{
		EnsureMathMode();
		value = _textBuffer.Length == 0 || _mathMLUnavailable
			? string.Empty
			: _mathDocument?.CanonicalMathML ?? MathDocument.SerializePlainText(PlainText);
	}

	/// <summary>Replaces the RichEditBox content with the specified MathML document.</summary>
	public void SetMathML(string value)
	{
		EnsureMathMode();

		MathDocument mathDocument;
		RichTextFragment fragment;
		try
		{
			mathDocument = MathDocument.Parse(value);
			fragment = mathDocument.CreateFragment(DefaultFormatState(), DefaultParagraphState());
		}
		catch (XmlException error)
		{
			SetDocumentFragment(EmptyMathFragment(), mathDocument: null);
			throw new ArgumentException("The value is not a valid MathML document.", nameof(value), error);
		}
		catch (ArgumentException error)
		{
			throw new ArgumentException("The value is not a valid MathML document.", nameof(value), error);
		}

		var preserveStructure = _owner.MaxLength <= 0 || fragment.Text.Length <= _owner.MaxLength;
		SetDocumentFragment(fragment, preserveStructure ? mathDocument : null);
	}

	private void EnsureMathMode()
	{
		if (!IsMathMode)
		{
			throw new ArgumentException("Math mode must be enabled before using MathML.");
		}
	}

	private static RichTextFragment EmptyMathFragment()
		=> new(
			string.Empty,
			Array.Empty<FormatRun>(),
			Array.Empty<ParagraphRun>(),
			new ParagraphFormatState(),
			hasExplicitTerminalParagraphState: false);

	private static string ResolveMathRenderingFontFamilyName()
	{
		string[] candidates =
		[
			MathFontFamilyName,
			"STIX Two Math",
			"STIX Math",
			"Latin Modern Math",
			"Libertinus Math",
			"Noto Sans Math",
			"DejaVu Math TeX Gyre",
		];
		foreach (var candidate in candidates)
		{
			var (_, loadedTask) = global::Microsoft.UI.Xaml.Documents.TextFormatting.FontDetailsCache.GetFont(
				candidate,
				16,
				global::Microsoft.UI.Text.FontWeights.Normal,
				global::Windows.UI.Text.FontStretch.Normal,
				global::Windows.UI.Text.FontStyle.Normal);
			// Only an installed font answers synchronously; a missing family resolves to the default font, which is rejected by name.
			if (loadedTask.IsCompletedSuccessfully
				&& loadedTask.Result.FontHandle is { } font
				&& string.Equals(font.FamilyName, candidate, StringComparison.OrdinalIgnoreCase)
				&& MathFontMetrics.HasOpenTypeMathTable(font))
			{
				return font.FamilyName;
			}
		}

		return global::Uno.UI.FeatureConfiguration.Font.DefaultTextFontFamily;
	}
}
