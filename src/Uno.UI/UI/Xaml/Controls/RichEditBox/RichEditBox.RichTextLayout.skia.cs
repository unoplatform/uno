#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Text;
using RichEditTextDocument = Microsoft.UI.Text.RichEditTextDocument;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	private sealed class RichTextLayoutSource : ICustomTextLayout
	{
		private const int MaxCachedResources = 64;
		private readonly RichEditBox _owner;
		private readonly ReusableRichLayoutRun _run = new();
		private readonly Dictionary<global::Windows.UI.Color, SolidColorBrush> _brushes = new();
		private readonly Dictionary<string, FontFamily> _fontFamilies = new(StringComparer.Ordinal);
		private readonly List<RichParagraphLayoutCacheEntry> _paragraphs = new();
		private readonly UnicodeText.ShapingCache _shapingCache = new();
		private readonly RichTextFontCacheListener _fontCacheListener;
		private RichTextParsedText? _parsedText;
		private ParagraphListMarkerState _finalListState = new();
		private bool _hadListFormatting;
		private long _textVersion = -1;
		private long _characterFormatVersion = -1;
		private long _paragraphFormatVersion = -1;
		private int _fragmentCount;
		private int _paragraphLayoutRebuildCount;

		internal RichTextLayoutSource(RichEditBox owner)
		{
			_owner = owner;
			_fontCacheListener = new RichTextFontCacheListener(this);
		}

		internal int RetainedInlineCount => 1;

		internal int RetainedResourceCount => _brushes.Count + _fontFamilies.Count;

		internal int CachedParagraphCount => _paragraphs.Count;

		internal int FragmentCount => _fragmentCount;

		internal int ParagraphLayoutRebuildCount => _paragraphLayoutRebuildCount;

		internal long ShapingOperationCount => _shapingCache.ShapeOperationCount;

		internal ParagraphListMarkerState GetFinalListState() => _finalListState.Clone();

		internal void Synchronize(
			RichEditTextDocument document,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			bool renderParagraphAlignments,
			bool renderParagraphLayouts,
			bool hasListFormatting,
			RichEditTextDocument.RenderInvalidation? invalidation)
		{
			var versionsMatch = _textVersion == document.TextVersion
				&& _characterFormatVersion == document.CharacterFormatVersion
				&& _paragraphFormatVersion == document.ParagraphFormatVersion;
			if (versionsMatch && _paragraphs.Count > 0)
			{
				return;
			}

			if (_paragraphs.Count == 0
				|| invalidation is null
				|| invalidation.Value.Full
				|| invalidation.Value.ParagraphSemanticsChanged && (hasListFormatting || _hadListFormatting))
			{
				RebuildAllParagraphs(
					document,
					runs,
					paragraphRuns,
					renderParagraphAlignments,
					renderParagraphLayouts);
			}
			else
			{
				RebuildAffectedParagraphs(
					document,
					runs,
					paragraphRuns,
					renderParagraphAlignments,
					renderParagraphLayouts,
					invalidation.Value);
			}

			UpdateVersions(document);
			_hadListFormatting = hasListFormatting;
		}

		public IParsedText Create(
			Size availableSize,
			Inline[] inlines,
			FontDetails defaultFontDetails,
			UnicodeText.IFontCacheUpdateListener fontListener,
			Brush? defaultForeground,
			TextAlignment? textAlignment,
			out Size size)
		{
			if (_owner._textBoxView?.DisplayBlock is not { } block)
			{
				size = default;
				return ParsedText.Empty;
			}

			_owner._boundedRichLayoutCreateCount++;
			_fontCacheListener.Inner = fontListener;
			_run.FontListener = _fontCacheListener;
			if (_paragraphs.Count == 0)
			{
				return new UnicodeText(
					availableSize,
					EnumerateRuns(block, defaultForeground),
					defaultFontDetails,
					block.MaxLines,
					(float)block.LineHeight,
					block.LineStackingStrategy,
					block.FlowDirection,
					textAlignment,
					block.TextWrapping,
					block.TextTrimming,
					block.IsSpellCheckEnabled,
					_fontCacheListener,
					includeTrailingWhitespaceInMeasurement: true,
					block.DefaultTabStop,
					block.EndingParagraphLayout,
					block.EndingParagraphAlignment,
					defaultForeground,
					block.AlignmentIncludesTrailingWhitespace,
					block.IgnoreTrailingCharacterSpacing,
					out size,
					_shapingCache);
			}

			var paragraphAvailableSize = new Size(availableSize.Width, double.PositiveInfinity);
			var layoutKey = new RichParagraphLayoutKey(
				availableSize.Width,
				block.FontFamily.Source,
				block.FontSize,
				block.FontWeight,
				block.FontStyle,
				block.FontStretch,
				block.CharacterSpacing,
				block.IsTextScaleFactorEnabled,
				defaultFontDetails.SKFontSize,
				defaultFontDetails.SKFontScaleX,
				block.LineHeight,
				block.LineStackingStrategy,
				block.FlowDirection,
				textAlignment,
				block.TextWrapping,
				block.TextTrimming,
				block.IsSpellCheckEnabled,
				block.DefaultTabStop,
				block.EndingParagraphLayout,
				block.EndingParagraphAlignment,
				defaultForeground,
				block.AlignmentIncludesTrailingWhitespace,
				block.IgnoreTrailingCharacterSpacing);
			var layoutChanged = false;
			for (var paragraphIndex = 0; paragraphIndex < _paragraphs.Count; paragraphIndex++)
			{
				var paragraph = _paragraphs[paragraphIndex];
				if (paragraph.TrySelectLayout(layoutKey, out var selectionChanged))
				{
					layoutChanged |= selectionChanged;
					continue;
				}

				var suppressEndingNewLineLine = paragraphIndex < _paragraphs.Count - 1;
				var parsedText = new UnicodeText(
					paragraphAvailableSize,
					EnumerateRuns(block, paragraph.Specs, defaultForeground),
					defaultFontDetails,
					maxLines: 0,
					(float)block.LineHeight,
					block.LineStackingStrategy,
					block.FlowDirection,
					textAlignment,
					block.TextWrapping,
					block.TextTrimming,
					block.IsSpellCheckEnabled,
					_fontCacheListener,
					includeTrailingWhitespaceInMeasurement: true,
					block.DefaultTabStop,
					block.EndingParagraphLayout,
					block.EndingParagraphAlignment,
					defaultForeground,
					block.AlignmentIncludesTrailingWhitespace,
					block.IgnoreTrailingCharacterSpacing,
					out var paragraphSize,
					_shapingCache,
					suppressEndingNewLineLine);
				paragraph.StoreLayout(layoutKey, parsedText, paragraphSize);
				_paragraphLayoutRebuildCount++;
				layoutChanged = true;
			}

			if (layoutChanged || _parsedText is null)
			{
				_parsedText = new RichTextParsedText(_paragraphs);
			}
			size = _parsedText.Size;
			return _parsedText;
		}

		private IEnumerable<Inline> EnumerateRuns(TextBlock block, Brush? defaultForeground)
		{
			var document = _owner.Document;
			foreach (var spec in _owner.EnumerateRenderFragmentSpecs(
				document,
				document.FormatRuns,
				document.ParagraphRuns,
				0,
				document.TextLength,
				document.HasMixedParagraphAlignments,
				document.HasVisualParagraphFormatting))
			{
				ConfigureRun(block, spec, defaultForeground);
				_owner._boundedRichLayoutRunVisitCount++;
				yield return _run;
			}
		}

		private IEnumerable<Inline> EnumerateRuns(
			TextBlock block,
			IReadOnlyList<RenderFragmentSpec> specs,
			Brush? defaultForeground)
		{
			foreach (var spec in specs)
			{
				ConfigureRun(block, spec, defaultForeground);
				_owner._boundedRichLayoutRunVisitCount++;
				yield return _run;
			}
		}

		private void RebuildAllParagraphs(
			RichEditTextDocument document,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			bool renderParagraphAlignments,
			bool renderParagraphLayouts)
		{
			_paragraphs.Clear();
			_paragraphs.AddRange(BuildParagraphs(
				document,
				runs,
				paragraphRuns,
				0,
				document.TextLength,
				renderParagraphAlignments,
				renderParagraphLayouts,
				new ParagraphListMarkerState(),
				out _finalListState));
			_fragmentCount = 0;
			foreach (var paragraph in _paragraphs)
			{
				_fragmentCount += paragraph.Specs.Count;
			}
			_parsedText = null;
		}

		private void RebuildAffectedParagraphs(
			RichEditTextDocument document,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			bool renderParagraphAlignments,
			bool renderParagraphLayouts,
			RichEditTextDocument.RenderInvalidation invalidation)
		{
			var oldTextLength = _paragraphs[^1].End;
			var first = FindParagraphIndex(Math.Max(0, invalidation.OldStart - 1));
			var lastProbe = invalidation.OldEnd > invalidation.OldStart
				? invalidation.OldEnd - 1
				: invalidation.OldStart;
			var last = FindParagraphIndex(Math.Min(oldTextLength, lastProbe));
			if (invalidation.ParagraphSemanticsChanged)
			{
				first = Math.Max(0, first - 1);
				last = Math.Min(_paragraphs.Count - 1, last + 1);
			}

			var oldReplaceStart = _paragraphs[first].Start;
			var oldReplaceEnd = _paragraphs[last].End;
			var listState = _paragraphs[first].ListStateBefore.Clone();
			var textLengthDelta = document.TextLength - oldTextLength;
			var mappedStart = MapOldRenderPosition(oldReplaceStart, invalidation, textLengthDelta, mapEnd: false);
			var mappedEnd = MapOldRenderPosition(oldReplaceEnd, invalidation, textLengthDelta, mapEnd: true);
			var newReplaceStart = document.GetParagraphStartForRender(Math.Clamp(mappedStart, 0, document.TextLength));
			int newReplaceEnd;
			if (document.TextLength == 0)
			{
				newReplaceEnd = 0;
			}
			else
			{
				var endProbe = Math.Clamp(Math.Max(newReplaceStart, mappedEnd - 1), 0, document.TextLength - 1);
				newReplaceEnd = document.GetParagraphEndForRender(endProbe);
			}

			var replacement = BuildParagraphs(
				document,
				runs,
				paragraphRuns,
				newReplaceStart,
				newReplaceEnd,
				renderParagraphAlignments,
				renderParagraphLayouts,
				listState,
				out _);
			var removedFragmentCount = 0;
			for (var i = first; i <= last; i++)
			{
				removedFragmentCount += _paragraphs[i].Specs.Count;
			}
			_paragraphs.RemoveRange(first, last - first + 1);
			_paragraphs.InsertRange(first, replacement);
			for (var i = first + replacement.Count; i < _paragraphs.Count; i++)
			{
				_paragraphs[i].Start += textLengthDelta;
			}

			var addedFragmentCount = 0;
			foreach (var paragraph in replacement)
			{
				addedFragmentCount += paragraph.Specs.Count;
			}
			_fragmentCount += addedFragmentCount - removedFragmentCount;
			_parsedText = null;

			if (!AreParagraphsContiguous(document.TextLength))
			{
				RebuildAllParagraphs(
					document,
					runs,
					paragraphRuns,
					renderParagraphAlignments,
					renderParagraphLayouts);
			}
		}

		private List<RichParagraphLayoutCacheEntry> BuildParagraphs(
			RichEditTextDocument document,
			IndexedRunCollection<FormatRun> runs,
			IndexedRunCollection<ParagraphRun> paragraphRuns,
			int start,
			int end,
			bool renderParagraphAlignments,
			bool renderParagraphLayouts,
			ParagraphListMarkerState listState,
			out ParagraphListMarkerState finalListState)
		{
			var paragraphs = new List<RichParagraphLayoutCacheEntry>();
			if (document.TextLength == 0)
			{
				paragraphs.Add(new RichParagraphLayoutCacheEntry(0, 0, [], listState.Clone()));
				finalListState = listState.Clone();
				return paragraphs;
			}

			var position = Math.Clamp(start, 0, document.TextLength);
			end = Math.Clamp(end, position, document.TextLength);
			while (position < end)
			{
				var paragraphEnd = Math.Min(end, document.GetParagraphEndForRender(position));
				if (paragraphEnd <= position)
				{
					break;
				}

				var listStateBefore = listState.Clone();
				var specs = new List<RenderFragmentSpec>(_owner.EnumerateRenderFragmentSpecs(
					document,
					runs,
					paragraphRuns,
					position,
					paragraphEnd,
					renderParagraphAlignments,
					renderParagraphLayouts,
					listState));
				paragraphs.Add(new RichParagraphLayoutCacheEntry(
					position,
					paragraphEnd - position,
					specs,
					listStateBefore));
				position = paragraphEnd;
			}

			finalListState = listState.Clone();
			return paragraphs;
		}

		private int FindParagraphIndex(int position)
		{
			if (_paragraphs.Count == 1)
			{
				return 0;
			}

			var low = 0;
			var high = _paragraphs.Count;
			while (low < high)
			{
				var middle = low + ((high - low) / 2);
				if (_paragraphs[middle].Start <= position)
				{
					low = middle + 1;
				}
				else
				{
					high = middle;
				}
			}

			return Math.Clamp(low - 1, 0, _paragraphs.Count - 1);
		}

		private bool AreParagraphsContiguous(int textLength)
		{
			if (_paragraphs.Count == 0 || _paragraphs[0].Start != 0)
			{
				return false;
			}

			var position = 0;
			foreach (var paragraph in _paragraphs)
			{
				if (paragraph.Start != position)
				{
					return false;
				}
				position = paragraph.End;
			}
			return position == textLength;
		}

		private void UpdateVersions(RichEditTextDocument document)
		{
			_textVersion = document.TextVersion;
			_characterFormatVersion = document.CharacterFormatVersion;
			_paragraphFormatVersion = document.ParagraphFormatVersion;
		}

		private void InvalidateFontLayouts()
		{
			foreach (var paragraph in _paragraphs)
			{
				paragraph.InvalidateLayouts();
			}
			_parsedText = null;
		}

		private void ConfigureRun(TextBlock block, RenderFragmentSpec spec, Brush? defaultForeground)
		{
			var format = spec.CharacterFormat;
			_run.BeginUpdate();
			try
			{
				_run.Text = format.AllCaps
					? ToUpperPreservingUtf16Length(spec.SourceText, format.LanguageTag)
					: spec.SourceText;
				_run.IsTextScaleFactorEnabled = block.IsTextScaleFactorEnabled;
				_run.CharacterBackground = format.Background;
				_run.IsHidden = format.Hidden;
				_run.RichEditKerningThreshold = format.Kerning;
				_run.RichEditLanguageTag = string.IsNullOrEmpty(format.LanguageTag) ? null : format.LanguageTag;
				_run.RichEditTextScript = format.TextScript;
				_run.RichEditSmallCaps = format.SmallCaps && !format.AllCaps;
				_run.RichEditOutline = format.Outline;
				_run.InlineObject = format.InlineImage is { } inlineImage
					? new InlineObjectInfo(
						inlineImage.GetDecodedImage(),
						inlineImage.Width,
						inlineImage.Height,
						inlineImage.Ascent,
						inlineImage.VerticalAlignment)
					: null;

				_run.FontWeight = format.WeightExplicit || format.Weight != 400
					? new FontWeight((ushort)Math.Clamp(format.Weight, 0, 999))
					: _owner.FontWeight;
				_run.FontStyle = format.Italic ? FontStyle.Italic : _owner.FontStyle;
				_run.FontStretch = format.FontStretch != FontStretch.Normal
					? format.FontStretch
					: _owner.FontStretch;

				var decorations = TextDecorations.None;
				var hasExplicitUnderline = format.Underline is not global::Microsoft.UI.Text.UnderlineType.None
					and not global::Microsoft.UI.Text.UnderlineType.Undefined;
				if (hasExplicitUnderline || format.Link is not null)
				{
					decorations |= TextDecorations.Underline;
					_run.RichEditUnderlineType = hasExplicitUnderline
						? format.Underline
						: global::Microsoft.UI.Text.UnderlineType.Single;
				}
				else
				{
					_run.RichEditUnderlineType = null;
				}
				if (format.Strikethrough)
				{
					decorations |= TextDecorations.Strikethrough;
				}
				_run.TextDecorations = decorations;

				_run.Foreground = format.Foreground is { } foreground
					? GetBrush(foreground)
					: format.Link is not null
						? GetLinkForeground() ?? defaultForeground ?? _owner.Foreground
						: defaultForeground ?? _owner.Foreground;
				var sourceFontSize = format.Size > 0 ? format.Size * DipsPerPoint : block.FontSize;
				_run.FontSize = format.Superscript || format.Subscript
					? sourceFontSize * ScriptFontScale
					: sourceFontSize;
				_run.RichEditBaselineOffset = format.Position * (float)DipsPerPoint;
				if (format.Superscript || format.Subscript)
				{
					_run.RichEditBaselineOffset += (float)(
						sourceFontSize * (format.Superscript ? SuperscriptOffsetEm : SubscriptOffsetEm));
				}

				if (format.Spacing != 0)
				{
					var fontSizeInPoints = format.Size > 0 ? format.Size : (float)(block.FontSize / DipsPerPoint);
					_run.CharacterSpacing = fontSizeInPoints > 0
						? (int)Math.Round(format.Spacing / fontSizeInPoints * 1000, MidpointRounding.AwayFromZero)
						: 0;
				}
				else
				{
					_run.CharacterSpacing = 0;
				}

				_run.FontFamily = string.IsNullOrEmpty(format.Name)
					? _owner.FontFamily
					: GetFontFamily(format.Name);
				_run.ParagraphAlignment = spec.ParagraphAlignment;
				_run.ParagraphLayout = spec.ParagraphLayout;
				_run.FlowDirection = spec.FlowDirection;
			}
			finally
			{
				_run.EndUpdate();
			}
		}

		private SolidColorBrush GetBrush(global::Windows.UI.Color color)
		{
			if (!_brushes.TryGetValue(color, out var brush))
			{
				brush = new SolidColorBrush(color);
				if (_brushes.Count < MaxCachedResources)
				{
					_brushes.Add(color, brush);
				}
			}
			return brush;
		}

		private FontFamily GetFontFamily(string name)
		{
			if (!_fontFamilies.TryGetValue(name, out var family))
			{
				family = new FontFamily(name);
				if (_fontFamilies.Count < MaxCachedResources)
				{
					_fontFamilies.Add(name, family);
				}
			}
			return family;
		}

		private Brush? GetLinkForeground()
		{
			var core = CoreServices.Instance;
			var theme = ThemeResolution.ResolveOwnerTheme(_owner);
			return core.LookupThemeResource(theme, "HyperlinkForeground") as Brush
				?? core.LookupThemeResource(theme, "SystemControlHyperlinkTextBrush") as Brush;
		}

		private sealed class RichTextFontCacheListener : UnicodeText.IFontCacheUpdateListener
		{
			private readonly RichTextLayoutSource _owner;

			internal RichTextFontCacheListener(RichTextLayoutSource owner)
			{
				_owner = owner;
			}

			internal UnicodeText.IFontCacheUpdateListener? Inner { get; set; }

			public void Invalidate()
			{
				_owner.InvalidateFontLayouts();
				Inner?.Invalidate();
			}
		}
	}

	private sealed class ReusableRichLayoutRun : Run
	{
		private bool _isUpdating;

		internal UnicodeText.IFontCacheUpdateListener? FontListener { get; set; }

		internal void BeginUpdate() => _isUpdating = true;

		internal void EndUpdate() => _isUpdating = false;

		protected override void OnFontFamilyChanged()
		{
			base.OnFontFamilyChanged();
			if (!_isUpdating)
			{
				FontListener?.Invalidate();
			}
		}
	}
}
