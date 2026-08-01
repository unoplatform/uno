#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Text;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents;

internal sealed class MathTextLayoutSource : ICustomTextLayout
{
	private readonly MathDocument _document;

	internal MathTextLayoutSource(MathDocument document)
	{
		_document = document;
	}

	public IParsedText Create(
		Size availableSize,
		Inline[] inlines,
		FontDetails defaultFontDetails,
		UnicodeText.IFontCacheUpdateListener fontListener,
		Brush? defaultForeground,
		TextAlignment? textAlignment,
		out Size size)
		=> new MathParsedText(
			_document,
			availableSize,
			inlines,
			defaultFontDetails,
			fontListener,
			defaultForeground,
			textAlignment,
			out size);
}

internal sealed class MathParsedText : IParsedText
{
	private static readonly IReadOnlyList<TextHighlighter> _noHighlighters = Array.Empty<TextHighlighter>();
	private static readonly SKPaint _rulePaint = new()
	{
		IsAntialias = true,
		StrokeCap = SKStrokeCap.Butt,
		Style = SKPaintStyle.Fill,
	};
	private static readonly SKPaint _compositionPaint = new()
	{
		Color = SKColors.Black,
		IsAntialias = true,
		StrokeCap = SKStrokeCap.Butt,
		Style = SKPaintStyle.Stroke,
		StrokeWidth = 1,
	};

	private readonly MathDocument _document;
	private readonly List<TextPlacement> _textPlacements = new();
	private readonly List<GlyphPlacement> _glyphPlacements = new();
	private readonly List<RulePlacement> _rulePlacements = new();
	private readonly MathIndexLayout[] _indexLayout;
	private readonly double _width;
	private readonly double _height;
	private readonly double _baseline;
	private readonly double _xOffset;

	internal MathParsedText(
		MathDocument document,
		Size availableSize,
		Inline[] inlines,
		FontDetails defaultFontDetails,
		UnicodeText.IFontCacheUpdateListener fontListener,
		Brush? defaultForeground,
		TextAlignment? textAlignment,
		out Size size)
	{
		_document = document;
		var resolver = new InlineStyleResolver(inlines, defaultFontDetails, defaultForeground);
		Metrics = MathFontMetrics.Create(defaultFontDetails);
		var builder = new BoxBuilder(document, resolver, fontListener, defaultForeground, Metrics, defaultFontDetails.SKFont);
		var root = builder.Build(document.Root, 1);
		VerticalVariantGlyphCount = builder.VerticalVariantGlyphCount;
		VerticalAssemblyGlyphCount = builder.VerticalAssemblyGlyphCount;
		VerticalGlyphFallbackCount = builder.VerticalGlyphFallbackCount;
		_width = Math.Max(0, root.Width);
		_baseline = Math.Max(root.Ascent, defaultFontDetails.SKFontMetrics.Ascent * -1);
		_height = Math.Max(defaultFontDetails.LineHeight, _baseline + root.Descent);
		_xOffset = GetAlignmentOffset(availableSize.Width, _width, textAlignment);
		_indexLayout = new MathIndexLayout[document.Projection.Length + 1];
		var context = new ArrangeContext(
			document,
			_textPlacements,
			_glyphPlacements,
			_rulePlacements,
			_indexLayout);
		root.Arrange(context, (float)_xOffset, (float)_baseline);
		FillMissingIndexes();
		size = new Size(_width, _height);
	}

	internal MathFontMetrics Metrics { get; }

	internal bool UsesOpenTypeMath => Metrics.UsesOpenTypeMath;

	internal int VerticalVariantGlyphCount { get; }

	internal int VerticalAssemblyGlyphCount { get; }

	internal int VerticalGlyphFallbackCount { get; }

	internal int IndexStorageByteCount => checked(_indexLayout.Length * 24);

	public bool IsBaseDirectionRightToLeft => false;

	public void Draw(
		in Visual.PaintingSession session,
		(int index, CompositionBrush brush, float thickness)? caret,
		IEnumerable<TextHighlighter> highlighters,
		(int startIndex, int length)? compositionRange)
	{
		DrawHighlighterBackgrounds(session, highlighters);

		foreach (var placement in _textPlacements)
		{
			session.Canvas.Save();
			session.Canvas.Translate(placement.X, placement.Y);
			placement.Layout.Draw(session, caret: null, _noHighlighters, compositionRange: null);
			session.Canvas.Restore();
		}

		using (var textBlobBuilder = new SKTextBlobBuilder())
		{
			foreach (var placement in _glyphPlacements)
			{
				textBlobBuilder.AddPositionedRun(placement.Glyphs, placement.Font, placement.Positions);
				using var textBlob = textBlobBuilder.Build();
				_rulePaint.Color = GetColor(placement.Brush, session.Opacity);
				session.Canvas.DrawText(textBlob, placement.X, placement.Y, _rulePaint);
			}
		}

		foreach (var rule in _rulePlacements)
		{
			_rulePaint.Color = GetColor(rule.Brush, session.Opacity);
			session.Canvas.DrawRect(rule.Rect, _rulePaint);
		}

		if (compositionRange is { length: > 0 } composition)
		{
			var start = Math.Clamp(composition.startIndex, 0, _document.Projection.Length);
			var end = Math.Clamp(start + composition.length, start, _document.Projection.Length);
			for (var index = start; index < end; index++)
			{
				var rect = _indexLayout[index].Rect;
				session.Canvas.DrawLine(
					(float)rect.X,
					(float)Math.Max(rect.Y, rect.Bottom - 1),
					(float)Math.Max(rect.Right, rect.X + 1),
					(float)Math.Max(rect.Y, rect.Bottom - 1),
					_compositionPaint);
			}
		}

		if (caret is { } caretValue)
		{
			var index = Math.Clamp(caretValue.index, 0, _document.Projection.Length);
			var rect = _indexLayout[index].Rect;
			var caretRect = new SKRect(
				(float)rect.X,
				(float)rect.Y,
				(float)rect.X + caretValue.thickness,
				(float)rect.Bottom);
			caretValue.brush.Paint(session.Canvas, session.Opacity, caretRect);
		}
	}

	public Rect GetRectForIndex(int adjustedIndex)
		=> _indexLayout[Math.Clamp(adjustedIndex, 0, _document.Projection.Length)].Rect;

	public TextGeometryPositionInfo GetGeometryPosition(int adjustedIndex)
	{
		var index = Math.Clamp(adjustedIndex, 0, _document.Projection.Length);
		var characterRect = _indexLayout[index].Rect;
		var kind = TextGeometryPositionKind.StructuredMath;
		if (index == _document.Projection.Length)
		{
			kind |= TextGeometryPositionKind.Caret
				| TextGeometryPositionKind.FinalEndOfParagraph
				| TextGeometryPositionKind.TrailingEdge;
		}
		else
		{
			kind |= TextGeometryPositionKind.Text | TextGeometryPositionKind.LeadingEdge;
		}

		return new TextGeometryPositionInfo(
			characterRect,
			characterRect with { Width = 0 },
			kind);
	}

	public double GetBaselineForIndex(int adjustedIndex)
		=> _indexLayout[Math.Clamp(adjustedIndex, 0, _document.Projection.Length)].Baseline;

	public int VisualLineCount => 1;

	public TextVisualLineInfo GetVisualLine(int lineIndex)
	{
		if (lineIndex != 0)
		{
			throw new ArgumentOutOfRangeException(nameof(lineIndex));
		}

		return new TextVisualLineInfo(
			0,
			_document.Projection.Length,
			0,
			new Rect(_xOffset, 0, _width, _height),
			_baseline,
			true,
			true);
	}

	public int GetIndexAt(Point point, bool ignoreEndingNewLine, bool extendedSelection)
	{
		if (_document.Projection.Length == 0)
		{
			return extendedSelection ? 0 : -1;
		}

		var bounds = new Rect(_xOffset, 0, _width, _height);
		if (!extendedSelection && !bounds.Contains(point))
		{
			return -1;
		}

		for (var index = 0; index < _document.Projection.Length; index++)
		{
			var rect = ExpandForHitTesting(_indexLayout[index].Rect);
			if (rect.Contains(point))
			{
				return point.X <= rect.X + rect.Width / 2 ? index : index + 1;
			}
		}

		var nearestIndex = 0;
		var nearestDistance = double.PositiveInfinity;
		for (var index = 0; index <= _document.Projection.Length; index++)
		{
			var rect = _indexLayout[index].Rect;
			var centerX = rect.Width > 0 ? rect.X + rect.Width / 2 : rect.X;
			var centerY = rect.Y + rect.Height / 2;
			var dx = point.X - centerX;
			var dy = point.Y - centerY;
			var distance = dx * dx + dy * dy;
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestIndex = index;
			}
		}

		return nearestIndex;
	}

	public Hyperlink? GetHyperlinkAt(Point point) => null;

	public (int start, int length) GetWordAt(int index, bool right)
	{
		index = Math.Clamp(index, 0, _document.Projection.Length);
		var lookup = index == _document.Projection.Length && index > 0 ? index - 1 : index;
		if (_document.GetAtomAt(lookup) is { } atom)
		{
			return (atom.Span.Start, atom.Span.Length);
		}

		return index < _document.Projection.Length ? (index, 1) : (index, 0);
	}

	public (int start, int length, bool firstLine, bool lastLine, int lineIndex) GetLineAt(int index)
		=> (0, _document.Projection.Length, true, true, 0);

	private void DrawHighlighterBackgrounds(in Visual.PaintingSession session, IEnumerable<TextHighlighter> highlighters)
	{
		var canvas = session.Canvas;
		var opacity = session.Opacity;
		foreach (var highlighter in highlighters)
		{
			var brush = highlighter.Background.GetOrCreateCompositionBrush(Compositor.GetSharedCompositor());
			foreach (var range in highlighter.Ranges)
			{
				var start = Math.Clamp(range.StartIndex, 0, _document.Projection.Length);
				var end = Math.Clamp(start + range.Length, start, _document.Projection.Length);
				Rect? pending = null;
				for (var index = start; index < end; index++)
				{
					var rect = ExpandForHighlight(_indexLayout[index].Rect);
					if (pending is { } previous
						&& Math.Abs(previous.Y - rect.Y) < 0.5
						&& Math.Abs(previous.Height - rect.Height) < 0.5
						&& rect.X <= previous.Right + 0.5)
					{
						pending = new Rect(
							previous.X,
							Math.Min(previous.Y, rect.Y),
							Math.Max(previous.Right, rect.Right) - previous.X,
							Math.Max(previous.Bottom, rect.Bottom) - Math.Min(previous.Y, rect.Y));
					}
					else
					{
						Paint(pending);
						pending = rect;
					}
				}
				Paint(pending);
			}

			void Paint(Rect? rect)
			{
				if (rect is not { } value)
				{
					return;
				}
				brush.Paint(
					canvas,
					opacity,
					new SKRect((float)value.X, (float)value.Y, (float)value.Right, (float)value.Bottom));
			}
		}
	}

	private void FillMissingIndexes()
	{
		if (_document.Projection.Length == 0)
		{
			AssignIndex(0, new Rect(_xOffset, 0, 0, _height), _baseline, force: true);
			return;
		}

		AssignIndex(
			_document.Projection.Length,
			new Rect(_xOffset + _width, 0, 0, _height),
			_baseline,
			force: true);
		var previous = -1;
		for (var index = 0; index < _indexLayout.Length; index++)
		{
			if (!_indexLayout[index].IsAssigned)
			{
				continue;
			}

			for (var missing = previous + 1; missing < index; missing++)
			{
				var source = previous >= 0 ? _indexLayout[previous] : _indexLayout[index];
				var sourceRect = source.Rect;
				var x = previous >= 0 ? sourceRect.Right : sourceRect.X;
				AssignIndex(missing, new Rect(x, sourceRect.Y, 0, sourceRect.Height), source.Baseline, force: true);
			}
			previous = index;
		}
	}

	private void AssignIndex(int index, Rect rect, double baseline, bool force)
	{
		if ((uint)index >= (uint)_indexLayout.Length || _indexLayout[index].IsAssigned && !force)
		{
			return;
		}

		_indexLayout[index] = new MathIndexLayout(rect, baseline);
	}

	private static double GetAlignmentOffset(double availableWidth, double contentWidth, TextAlignment? alignment)
	{
		if (double.IsInfinity(availableWidth) || availableWidth <= contentWidth)
		{
			return 0;
		}

		return alignment switch
		{
			TextAlignment.Center => (availableWidth - contentWidth) / 2,
			TextAlignment.Right => availableWidth - contentWidth,
			_ => 0,
		};
	}

	private static Rect ExpandForHitTesting(Rect rect)
	{
		var width = Math.Max(rect.Width, 4);
		var height = Math.Max(rect.Height, 4);
		return new Rect(rect.X - (width - rect.Width) / 2, rect.Y - (height - rect.Height) / 2, width, height);
	}

	private static Rect ExpandForHighlight(Rect rect)
		=> rect.Width > 0
			? rect
			: new Rect(rect.X - 1, rect.Y, 2, Math.Max(1, rect.Height));

	private static SKColor GetColor(Brush? brush, float opacity)
	{
		if (brush is SolidColorBrush solid)
		{
			var color = solid.Color;
			return new SKColor(color.R, color.G, color.B, (byte)(color.A * solid.Opacity * opacity));
		}
		if (brush is GradientBrush gradient)
		{
			var color = gradient.FallbackColorWithOpacity;
			return new SKColor(color.R, color.G, color.B, (byte)(color.A * opacity));
		}
		if (brush is XamlCompositionBrushBase composition)
		{
			var color = composition.FallbackColorWithOpacity;
			return new SKColor(color.R, color.G, color.B, (byte)(color.A * opacity));
		}

		return SKColors.Black.WithAlpha((byte)(byte.MaxValue * opacity));
	}

	private sealed record TextPlacement(UnicodeText Layout, float X, float Y);

	private sealed record GlyphPlacement(
		SKFont Font,
		ushort[] Glyphs,
		SKPoint[] Positions,
		float X,
		float Y,
		Brush? Brush);

	private sealed record RulePlacement(SKRect Rect, Brush? Brush);

	private readonly struct MathIndexLayout
	{
		private readonly float _x;
		private readonly float _y;
		private readonly float _width;
		private readonly float _height;
		private readonly double _encodedBaseline;

		internal MathIndexLayout(Rect rect, double baseline)
		{
			_x = (float)rect.X;
			_y = (float)rect.Y;
			_width = (float)rect.Width;
			_height = (float)rect.Height;
			_encodedBaseline = baseline + 1;
		}

		internal bool IsAssigned => _encodedBaseline != 0;

		internal Rect Rect => new(_x, _y, _width, _height);

		internal double Baseline => _encodedBaseline - 1;
	}

	private sealed class ArrangeContext
	{
		private readonly MathDocument _document;
		private readonly List<TextPlacement> _textPlacements;
		private readonly List<GlyphPlacement> _glyphPlacements;
		private readonly List<RulePlacement> _rules;
		private readonly MathIndexLayout[] _indexLayout;

		internal ArrangeContext(
			MathDocument document,
			List<TextPlacement> textPlacements,
			List<GlyphPlacement> glyphPlacements,
			List<RulePlacement> rules,
			MathIndexLayout[] indexLayout)
		{
			_document = document;
			_textPlacements = textPlacements;
			_glyphPlacements = glyphPlacements;
			_rules = rules;
			_indexLayout = indexLayout;
		}

		internal MathTextSpan GetSpan(MathNode node) => _document.GetSpan(node);

		internal void AddText(UnicodeText layout, float x, float top)
			=> _textPlacements.Add(new TextPlacement(layout, x, top));

		internal void AddGlyphs(
			SKFont font,
			ushort[] glyphs,
			SKPoint[] positions,
			float x,
			float top,
			Brush? brush)
			=> _glyphPlacements.Add(new GlyphPlacement(font, glyphs, positions, x, top, brush));

		internal void AddRule(SKRect rect, Brush? brush) => _rules.Add(new RulePlacement(rect, brush));

		internal void SetIndex(int index, Rect rect, double baseline, bool force = false)
		{
			if ((uint)index >= (uint)_indexLayout.Length || _indexLayout[index].IsAssigned && !force)
			{
				return;
			}

			_indexLayout[index] = new MathIndexLayout(rect, baseline);
		}

		internal void SetNodeBounds(MathNode node, float x, float baseline, float width, float ascent, float descent)
		{
			var span = GetSpan(node);
			SetIndex(span.Start, new Rect(x, baseline - ascent, 0, ascent + descent), baseline);
			if (span.Length > 1 && _document.IsStructuralMarkerAt(span.End - 1))
			{
				SetIndex(
					span.End - 1,
					new Rect(x + width, baseline - ascent, 0, ascent + descent),
					baseline);
			}
			SetIndex(span.End, new Rect(x + width, baseline - ascent, 0, ascent + descent), baseline);
		}
	}

	private sealed class BoxBuilder
	{
		private readonly MathDocument _document;
		private readonly InlineStyleResolver _resolver;
		private readonly UnicodeText.IFontCacheUpdateListener _fontListener;
		private readonly Brush? _defaultForeground;
		private readonly float _em;
		private readonly SKFont _mathFont;

		internal BoxBuilder(
			MathDocument document,
			InlineStyleResolver resolver,
			UnicodeText.IFontCacheUpdateListener fontListener,
			Brush? defaultForeground,
			MathFontMetrics metrics,
			SKFont mathFont)
		{
			_document = document;
			_resolver = resolver;
			_fontListener = fontListener;
			_defaultForeground = defaultForeground;
			Metrics = metrics;
			_em = Math.Max(1, resolver.DefaultFontSize);
			_mathFont = mathFont;
		}

		internal MathFontMetrics Metrics { get; }

		internal int VerticalVariantGlyphCount { get; private set; }

		internal int VerticalAssemblyGlyphCount { get; private set; }

		internal int VerticalGlyphFallbackCount { get; private set; }

		internal MathBox Build(MathNode node, float scale)
			=> node switch
			{
				MathRowNode row => BuildRow(row, scale),
				MathTokenNode token => CreateTokenBox(token, scale),
				MathFractionNode fraction => BuildFraction(fraction, scale),
				MathRadicalNode radical => BuildRadical(radical, scale),
				MathScriptNode script => BuildScript(script, scale),
				MathFencedNode fenced => BuildFenced(fenced, scale),
				MathTableNode table => BuildTable(table, scale),
				MathOverUnderNode overUnder => BuildOverUnder(overUnder, scale),
				MathMultiScriptsNode multiScripts => BuildMultiScripts(multiScripts, scale),
				_ => new RowBox(node, Array.Empty<MathBox>()),
			};

		private MathBox BuildRow(MathRowNode row, float scale)
		{
			var children = new MathBox[row.Children.Count];
			for (var index = 0; index < children.Length; index++)
			{
				children[index] = Build(row.Children[index], scale);
			}

			return new RowBox(row, children);
		}

		private MathBox BuildFraction(MathFractionNode fraction, float scale)
		{
			var childScale = scale * 0.9f;
			var numerator = Build(fraction.Numerator, childScale);
			var denominator = Build(fraction.Denominator, childScale);
			return new FractionBox(
				fraction,
				numerator,
				denominator,
				Metrics,
				scale,
				_em,
				GetBrush(fraction));
		}

		private MathBox BuildRadical(MathRadicalNode radical, float scale)
		{
			var radicand = Build(radical.Radicand, scale);
			var degree = radical.Degree is { } degreeNode
				? Build(degreeNode, scale * Metrics.ScriptScale)
				: null;
			var targetHeight = radicand.Ascent + radicand.Descent + Metrics.RadicalGap * scale;
			var fenceScale = Math.Clamp(targetHeight / Math.Max(1, _em), 1, 2.75f);
			var radicalSpan = _document.GetSpan(radical);
			var sign = CreateVerticalGlyphBox(
					radical,
					"√",
					new MathTextSpan(radicalSpan.Start, 1),
					targetHeight,
					scale,
					GetBrush(radical))
				?? CreateLiteralBox(
					radical,
					"√",
					new MathTextSpan(radicalSpan.Start, 1),
					scale * fenceScale,
					GetBrush(radical));
			return new RadicalBox(
				radical,
				radicand,
				degree,
				sign,
				Metrics,
				scale,
				GetBrush(radical));
		}

		private MathBox BuildScript(MathScriptNode script, float scale)
		{
			var @base = Build(script.Base, scale);
			var scriptScale = scale * Metrics.ScriptScale;
			var subscript = script.Subscript is { } subscriptNode ? Build(subscriptNode, scriptScale) : null;
			var superscript = script.Superscript is { } superscriptNode ? Build(superscriptNode, scriptScale) : null;
			return new ScriptBox(script, @base, subscript, superscript, Metrics, scale);
		}

		private MathBox BuildFenced(MathFencedNode fenced, float scale)
		{
			var inner = Build(fenced.Content, scale);
			var targetHeight = inner.Ascent + inner.Descent;
			var fenceScale = Math.Clamp(targetHeight / Math.Max(1, _em), 1, 3);
			var span = _document.GetSpan(fenced);
			var openSpan = new MathTextSpan(span.Start, 1);
			var closeSpan = new MathTextSpan(Math.Max(span.Start, span.End - 1), span.Length > 0 ? 1 : 0);
			var open = CreateVerticalGlyphBox(
					fenced,
					fenced.Open,
					openSpan,
					targetHeight,
					scale,
					GetBrush(fenced),
					allowVariant: !OperatingSystem.IsBrowser())
				?? CreateLiteralBox(fenced, fenced.Open, openSpan, scale * fenceScale, GetBrush(fenced));
			var close = CreateVerticalGlyphBox(
					fenced,
					fenced.Close,
					closeSpan,
					targetHeight,
					scale,
					GetBrush(fenced),
					allowVariant: !OperatingSystem.IsBrowser())
				?? CreateLiteralBox(fenced, fenced.Close, closeSpan, scale * fenceScale, GetBrush(fenced));
			return new FencedBox(fenced, open, inner, close);
		}

		private MathBox BuildTable(MathTableNode table, float scale)
		{
			var rows = new MathBox[table.Rows.Count][];
			for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
			{
				var row = table.Rows[rowIndex];
				rows[rowIndex] = new MathBox[row.Cells.Count];
				for (var columnIndex = 0; columnIndex < row.Cells.Count; columnIndex++)
				{
					rows[rowIndex][columnIndex] = Build(row.Cells[columnIndex], scale * 0.9f);
				}
			}

			var span = _document.GetSpan(table);
			var contentHeight = TableBox.GetContentHeight(rows, _em * 0.25f * scale);
			var openSpan = new MathTextSpan(span.Start, 1);
			var closeSpan = new MathTextSpan(Math.Max(span.Start, span.End - 1), span.Length > 0 ? 1 : 0);
			var open = CreateVerticalGlyphBox(
					table,
					"[",
					openSpan,
					contentHeight,
					scale,
					GetBrush(table),
					allowVariant: !OperatingSystem.IsBrowser())
				?? CreateTableFenceBox(table, openSpan, contentHeight, scale, isOpen: true, GetBrush(table));
			var close = CreateVerticalGlyphBox(
					table,
					"]",
					closeSpan,
					contentHeight,
					scale,
					GetBrush(table),
					allowVariant: !OperatingSystem.IsBrowser())
				?? CreateTableFenceBox(table, closeSpan, contentHeight, scale, isOpen: false, GetBrush(table));
			return new TableBox(
				table,
				rows,
				open,
				close,
				_em * 0.55f * scale,
				_em * 0.25f * scale,
				Metrics.AxisHeight * scale);
		}

		private VerticalRuleFenceBox CreateTableFenceBox(
			MathTableNode table,
			MathTextSpan span,
			float targetHeight,
			float scale,
			bool isOpen,
			Brush? brush)
			=> new(
				table,
				span,
				Math.Max(_em * scale, targetHeight),
				Metrics.AxisHeight * scale,
				Math.Max(1, _em * 0.3f * scale),
				Math.Max(1, Metrics.FractionRuleThickness * scale),
				isOpen,
				brush);

		private MathBox BuildOverUnder(MathOverUnderNode node, float scale)
		{
			var @base = node.Kind == MathOverUnderKind.Nary
				? BuildDetached(node.Base, scale)
				: Build(node.Base, scale);
			var attachmentScale = scale * Metrics.ScriptScale;
			var under = node.Under is { } underNode
				? node.Kind is MathOverUnderKind.Mover or MathOverUnderKind.Munder
					? BuildDetached(underNode, attachmentScale)
					: Build(underNode, attachmentScale)
				: null;
			var over = node.Over is { } overNode
				? node.Kind is MathOverUnderKind.Mover or MathOverUnderKind.Munder
					? BuildDetached(overNode, attachmentScale)
					: Build(overNode, attachmentScale)
				: null;
			var operand = node.Operand is { } operandNode ? Build(operandNode, scale) : null;
			return new OverUnderBox(
				node,
				@base,
				under,
				over,
				operand,
				_em * 0.12f * scale);
		}

		private MathBox BuildMultiScripts(MathMultiScriptsNode node, float scale)
		{
			var body = Build(node.Body, scale);
			var pairs = new ScriptPairBox[node.Prescripts.Count];
			var scriptScale = scale * Metrics.ScriptScale;
			for (var index = 0; index < pairs.Length; index++)
			{
				var pair = node.Prescripts[index];
				pairs[index] = new ScriptPairBox(
					pair,
					pair.Subscript is { } subscript ? Build(subscript, scriptScale) : null,
					pair.Superscript is { } superscript ? Build(superscript, scriptScale) : null);
			}
			return new MultiScriptsBox(
				node,
				body,
				pairs,
				Metrics,
				scale,
				_em * 0.08f * scale);
		}

		private MathBox BuildDetached(MathNode node, float scale)
			=> node is MathTokenNode token
				? CreateGlyphBox(
					token,
					token.ProjectionText,
					_document.GetSpan(token),
					scale,
					token.Style.Foreground is { } foreground ? new SolidColorBrush(foreground) : null,
					0,
					0,
					mapsIndexes: false)
				: Build(node, scale);

		private GlyphBox CreateTokenBox(MathTokenNode token, float scale)
		{
			var span = _document.GetSpan(token);
			var operatorPadding = token.Kind == MathTokenKind.Operator && !IsFenceOrSeparator(token.Text)
				? _em * 0.12f * scale
				: 0;
			return CreateGlyphBox(
				token,
				token.ProjectionText,
				span,
				scale,
				token.Style.Foreground is { } foreground ? new SolidColorBrush(foreground) : null,
				operatorPadding,
				operatorPadding);
		}

		private GlyphBox CreateLiteralBox(
			MathNode owner,
			string text,
			MathTextSpan span,
			float scale,
			Brush? brush,
			bool mapsIndexes = true)
			=> CreateGlyphBox(owner, text, span, scale, brush, 0, 0, mapsIndexes);

		private MathBox? CreateVerticalGlyphBox(
			MathNode owner,
			string text,
			MathTextSpan span,
			float targetHeight,
			float scale,
			Brush? brush,
			bool allowVariant = true)
		{
			if (!Metrics.TryGetVerticalGlyph(
				_mathFont,
				text,
				targetHeight,
				out var run,
				allowVariant))
			{
				VerticalGlyphFallbackCount++;
				return null;
			}

			var glyphs = new ushort[run.Parts.Length];
			var widths = new float[glyphs.Length];
			var bounds = new SKRect[glyphs.Length];
			for (var index = 0; index < glyphs.Length; index++)
			{
				glyphs[index] = run.Parts[index].Glyph;
				if (glyphs[index] >= _mathFont.Typeface.GlyphCount)
				{
					VerticalGlyphFallbackCount++;
					return null;
				}
			}
			_mathFont.GetGlyphWidths(glyphs, widths, bounds, null);

			var left = float.MaxValue;
			var right = float.MinValue;
			var top = float.MaxValue;
			var bottom = float.MinValue;
			var hasInk = false;
			for (var index = 0; index < glyphs.Length; index++)
			{
				left = Math.Min(left, bounds[index].Left);
				right = Math.Max(right, bounds[index].Right);
				top = Math.Min(top, run.Parts[index].Offset + bounds[index].Top);
				bottom = Math.Max(bottom, run.Parts[index].Offset + bounds[index].Bottom);
				hasInk |= bounds[index].Width > 0 && bounds[index].Height > 0;
			}
			if (!hasInk || !float.IsFinite(left) || !float.IsFinite(top))
			{
				VerticalGlyphFallbackCount++;
				return null;
			}

			var height = Math.Max(run.Advance, bottom - top);
			var width = Math.Max(1, right - left);
			var positions = new SKPoint[glyphs.Length];
			for (var index = 0; index < glyphs.Length; index++)
			{
				positions[index] = new SKPoint(-left, run.Parts[index].Offset - top);
			}
			var ascent = Math.Clamp(height / 2 + Metrics.AxisHeight * scale, 0, height);
			if (run.IsAssembly)
			{
				VerticalAssemblyGlyphCount++;
			}
			else
			{
				VerticalVariantGlyphCount++;
			}
			return new VerticalGlyphBox(owner, span, _mathFont, glyphs, positions, width, ascent, height - ascent, brush);
		}

		private GlyphBox CreateGlyphBox(
			MathNode owner,
			string text,
			MathTextSpan span,
			float scale,
			Brush? brush,
			float leftPadding,
			float rightPadding,
			bool mapsIndexes = true)
		{
			var run = _resolver.CreateRun(span.Start, text, scale, brush);
			var layout = new UnicodeText(
				new Size(double.PositiveInfinity, double.PositiveInfinity),
				new Inline[] { run },
				run.FontInfo,
				1,
				0,
				LineStackingStrategy.MaxHeight,
				FlowDirection.LeftToRight,
				TextAlignment.Left,
				TextWrapping.NoWrap,
				TextTrimming.None,
				isSpellCheckEnabled: false,
				_fontListener,
				includeTrailingWhitespaceInMeasurement: false,
				defaultTabStop: 48,
				endingParagraphLayout: null,
				endingParagraphAlignment: null,
				_defaultForeground,
				alignmentIncludesTrailingWhitespace: false,
				ignoreTrailingCharacterSpacing: false,
				out var size);
			var baseline = (float)layout.GetBaselineForIndex(0);
			return new GlyphBox(
				owner,
				span,
				text,
				layout,
				(float)size.Width,
				baseline,
				Math.Max(0, (float)size.Height - baseline),
				leftPadding,
				rightPadding,
				mapsIndexes);
		}

		private Brush? GetBrush(MathNode node)
			=> node.Style.Foreground is { } foreground
				? new SolidColorBrush(foreground)
				: _resolver.GetBrush(_document.GetSpan(node).Start) ?? _defaultForeground;

		private static bool IsFenceOrSeparator(string value)
			=> value is "(" or ")" or "[" or "]" or "{" or "}" or "|" or "," or ";" or "⌈" or "⌉"
				or "⌊" or "⌋" or "⟨" or "⟩";
	}

	private abstract class MathBox
	{
		protected MathBox(MathNode node, float width, float ascent, float descent)
		{
			Node = node;
			Width = Math.Max(0, width);
			Ascent = Math.Max(0, ascent);
			Descent = Math.Max(0, descent);
		}

		internal MathNode Node { get; }

		internal float Width { get; }

		internal float Ascent { get; }

		internal float Descent { get; }

		internal abstract void Arrange(ArrangeContext context, float x, float baseline);
	}

	private sealed class GlyphBox : MathBox
	{
		private readonly MathTextSpan _span;
		private readonly string _text;
		private readonly UnicodeText _layout;
		private readonly float _leftPadding;
		private readonly bool _mapsIndexes;

		internal GlyphBox(
			MathNode node,
			MathTextSpan span,
			string text,
			UnicodeText layout,
			float textWidth,
			float ascent,
			float descent,
			float leftPadding,
			float rightPadding,
			bool mapsIndexes)
			: base(node, leftPadding + textWidth + rightPadding, ascent, descent)
		{
			_span = span;
			_text = text;
			_layout = layout;
			_leftPadding = leftPadding;
			_mapsIndexes = mapsIndexes;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			var textX = x + _leftPadding;
			var top = baseline - Ascent;
			if (_text.Length > 0)
			{
				context.AddText(_layout, textX, top);
			}
			if (!_mapsIndexes)
			{
				return;
			}

			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			for (var index = 0; index < _span.Length; index++)
			{
				var localIndex = Math.Min(index, _text.Length);
				var localRect = _layout.GetRectForIndex(localIndex);
				var nextRect = _layout.GetRectForIndex(Math.Min(localIndex + 1, _text.Length));
				var localBaseline = _layout.GetBaselineForIndex(localIndex);
				var left = Math.Min(localRect.X, nextRect.X);
				var right = Math.Max(localRect.X, nextRect.X);
				if (right - left < 0.5)
				{
					right = left + Math.Max(localRect.Width, nextRect.Width);
				}
				var rectTop = Math.Min(localRect.Y, nextRect.Y);
				var rectBottom = Math.Max(localRect.Bottom, nextRect.Bottom);
				context.SetIndex(
					_span.Start + index,
					new Rect(
						textX + left,
						top + rectTop,
						right - left,
						rectBottom - rectTop),
					top + localBaseline,
					force: true);
			}
			if (_span.Length == 0)
			{
				context.SetIndex(_span.Start, new Rect(textX, top, 0, Ascent + Descent), baseline);
			}
		}
	}

	private sealed class VerticalGlyphBox : MathBox
	{
		private readonly MathTextSpan _span;
		private readonly SKFont _font;
		private readonly ushort[] _glyphs;
		private readonly SKPoint[] _positions;
		private readonly Brush? _brush;

		internal VerticalGlyphBox(
			MathNode node,
			MathTextSpan span,
			SKFont font,
			ushort[] glyphs,
			SKPoint[] positions,
			float width,
			float ascent,
			float descent,
			Brush? brush)
			: base(node, width, ascent, descent)
		{
			_span = span;
			_font = font;
			_glyphs = glyphs;
			_positions = positions;
			_brush = brush;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			var top = baseline - Ascent;
			context.AddGlyphs(_font, _glyphs, _positions, x, top, _brush);
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			if (_span.Length > 0)
			{
				context.SetIndex(
					_span.Start,
					new Rect(x, top, Width, Ascent + Descent),
					baseline,
					force: true);
			}
			else
			{
				context.SetIndex(_span.Start, new Rect(x, top, 0, Ascent + Descent), baseline);
			}
		}
	}

	private sealed class VerticalRuleFenceBox : MathBox
	{
		private readonly MathTextSpan _span;
		private readonly float _thickness;
		private readonly bool _isOpen;
		private readonly Brush? _brush;

		internal VerticalRuleFenceBox(
			MathNode node,
			MathTextSpan span,
			float height,
			float axisHeight,
			float width,
			float thickness,
			bool isOpen,
			Brush? brush)
			: base(
				node,
				width,
				Math.Clamp(height / 2 + axisHeight, 0, height),
				Math.Clamp(height / 2 - axisHeight, 0, height))
		{
			_span = span;
			_thickness = Math.Min(thickness, Math.Min(width, height));
			_isOpen = isOpen;
			_brush = brush;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			var top = baseline - Ascent;
			var bottom = baseline + Descent;
			var verticalX = _isOpen ? x : x + Width - _thickness;
			context.AddRule(new SKRect(verticalX, top, verticalX + _thickness, bottom), _brush);
			context.AddRule(new SKRect(x, top, x + Width, top + _thickness), _brush);
			context.AddRule(new SKRect(x, bottom - _thickness, x + Width, bottom), _brush);
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			context.SetIndex(
				_span.Start,
				new Rect(x, top, Width, Ascent + Descent),
				baseline,
				force: _span.Length > 0);
		}
	}

	private sealed class RowBox : MathBox
	{
		private readonly IReadOnlyList<MathBox> _children;

		internal RowBox(MathNode node, IReadOnlyList<MathBox> children)
			: base(
				node,
				children.Sum(child => child.Width),
				children.Count == 0 ? 0 : children.Max(child => child.Ascent),
				children.Count == 0 ? 0 : children.Max(child => child.Descent))
		{
			_children = children;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			foreach (var child in _children)
			{
				child.Arrange(context, x, baseline);
				x += child.Width;
			}
		}
	}

	private sealed class FractionBox : MathBox
	{
		private readonly MathBox _numerator;
		private readonly MathBox _denominator;
		private readonly float _numeratorBaseline;
		private readonly float _denominatorBaseline;
		private readonly float _ruleOffset;
		private readonly float _ruleThickness;
		private readonly float _padding;
		private readonly Brush? _brush;

		internal FractionBox(
			MathFractionNode node,
			MathBox numerator,
			MathBox denominator,
			MathFontMetrics metrics,
			float scale,
			float em,
			Brush? brush)
			: this(
				node,
				numerator,
				denominator,
				metrics,
				scale,
				em,
				brush,
				Math.Max(1, metrics.FractionRuleThickness * scale))
		{
		}

		private FractionBox(
			MathFractionNode node,
			MathBox numerator,
			MathBox denominator,
			MathFontMetrics metrics,
			float scale,
			float em,
			Brush? brush,
			float ruleThickness)
			: base(
				node,
				Math.Max(numerator.Width, denominator.Width) + em * 0.24f * scale,
				GetAscent(numerator, metrics, scale, ruleThickness),
				GetDescent(denominator, metrics, scale, ruleThickness))
		{
			_numerator = numerator;
			_denominator = denominator;
			_ruleOffset = -metrics.AxisHeight * scale;
			_ruleThickness = ruleThickness;
			_numeratorBaseline = Math.Min(
				-metrics.FractionNumeratorShift * scale,
				_ruleOffset - ruleThickness / 2 - metrics.FractionNumeratorGap * scale - numerator.Descent);
			_denominatorBaseline = Math.Max(
				metrics.FractionDenominatorShift * scale,
				_ruleOffset + ruleThickness / 2 + metrics.FractionDenominatorGap * scale + denominator.Ascent);
			_padding = em * 0.12f * scale;
			_brush = brush;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			_numerator.Arrange(context, x + (Width - _numerator.Width) / 2, baseline + _numeratorBaseline);
			_denominator.Arrange(context, x + (Width - _denominator.Width) / 2, baseline + _denominatorBaseline);
			var ruleY = baseline + _ruleOffset - _ruleThickness / 2;
			context.AddRule(
				new SKRect(x + _padding, ruleY, x + Width - _padding, ruleY + _ruleThickness),
				_brush);
			var numeratorSpan = context.GetSpan(((MathFractionNode)Node).Numerator);
			context.SetIndex(
				numeratorSpan.End,
				new Rect(x + _padding, ruleY, Width - _padding * 2, _ruleThickness),
				baseline,
				force: true);
		}

		private static float GetAscent(MathBox numerator, MathFontMetrics metrics, float scale, float ruleThickness)
		{
			var ruleOffset = -metrics.AxisHeight * scale;
			var numeratorBaseline = Math.Min(
				-metrics.FractionNumeratorShift * scale,
				ruleOffset - ruleThickness / 2 - metrics.FractionNumeratorGap * scale - numerator.Descent);
			return Math.Max(-ruleOffset + ruleThickness / 2, -numeratorBaseline + numerator.Ascent);
		}

		private static float GetDescent(MathBox denominator, MathFontMetrics metrics, float scale, float ruleThickness)
		{
			var ruleOffset = -metrics.AxisHeight * scale;
			var denominatorBaseline = Math.Max(
				metrics.FractionDenominatorShift * scale,
				ruleOffset + ruleThickness / 2 + metrics.FractionDenominatorGap * scale + denominator.Ascent);
			return Math.Max(ruleOffset + ruleThickness / 2, denominatorBaseline + denominator.Descent);
		}
	}

	private sealed class RadicalBox : MathBox
	{
		private readonly MathBox _radicand;
		private readonly MathBox? _degree;
		private readonly MathBox _sign;
		private readonly float _radicandX;
		private readonly float _barY;
		private readonly float _ruleThickness;
		private readonly float _degreeBaseline;
		private readonly float _degreeX;
		private readonly Brush? _brush;

		internal RadicalBox(
			MathRadicalNode node,
			MathBox radicand,
			MathBox? degree,
			MathBox sign,
			MathFontMetrics metrics,
			float scale,
			Brush? brush)
			: base(
				node,
				GetWidth(radicand, degree, sign, metrics, scale),
				GetAscent(radicand, degree, metrics, scale),
				Math.Max(radicand.Descent, sign.Descent))
		{
			_radicand = radicand;
			_degree = degree;
			_sign = sign;
			_ruleThickness = Math.Max(1, metrics.RadicalRuleThickness * scale);
			_radicandX = sign.Width + metrics.RadicalKernAfterDegree * scale;
			_barY = -radicand.Ascent - metrics.RadicalGap * scale - _ruleThickness;
			_degreeBaseline = _barY + radicand.Ascent * (1 - metrics.RadicalDegreeRaisePercent / 100f);
			_degreeX = Math.Max(0, sign.Width * 0.15f + metrics.RadicalKernBeforeDegree * scale);
			_brush = brush;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			_sign.Arrange(context, x + Math.Max(0, _radicandX - _sign.Width), baseline);
			_radicand.Arrange(context, x + _radicandX, baseline);
			var ruleY = baseline + _barY;
			context.AddRule(
				new SKRect(
					x + _radicandX - 1,
					ruleY,
					x + _radicandX + _radicand.Width,
					ruleY + _ruleThickness),
				_brush);
			if (_degree is not null)
			{
				_degree.Arrange(context, x + _degreeX, baseline + _degreeBaseline);
			}

			var node = (MathRadicalNode)Node;
			var span = context.GetSpan(node);
			context.SetIndex(
				span.Start,
				new Rect(x, baseline - Ascent, Math.Max(1, _radicandX), Ascent + Descent),
				baseline,
				force: true);
			var radicandSpan = context.GetSpan(node.Radicand);
			context.SetIndex(
				Math.Max(span.Start + 1, radicandSpan.Start - 1),
				new Rect(x + _radicandX, ruleY, 0, Ascent + Descent),
				baseline,
				force: true);
		}

		private static float GetWidth(
			MathBox radicand,
			MathBox? degree,
			MathBox sign,
			MathFontMetrics metrics,
			float scale)
			=> Math.Max(sign.Width, (degree?.Width ?? 0) * 0.65f)
				+ metrics.RadicalKernAfterDegree * scale
				+ radicand.Width;

		private static float GetAscent(MathBox radicand, MathBox? degree, MathFontMetrics metrics, float scale)
		{
			var barAscent = radicand.Ascent
				+ metrics.RadicalGap * scale
				+ metrics.RadicalRuleThickness * scale
				+ metrics.RadicalExtraAscender * scale;
			var degreeAscent = degree is null
				? 0
				: radicand.Ascent * metrics.RadicalDegreeRaisePercent / 100f + degree.Ascent;
			return Math.Max(barAscent, degreeAscent);
		}
	}

	private sealed class ScriptBox : MathBox
	{
		private readonly MathBox _base;
		private readonly MathBox? _subscript;
		private readonly MathBox? _superscript;
		private readonly float _scriptX;
		private readonly float _subscriptBaseline;
		private readonly float _superscriptBaseline;

		internal ScriptBox(
			MathScriptNode node,
			MathBox @base,
			MathBox? subscript,
			MathBox? superscript,
			MathFontMetrics metrics,
			float scale)
			: base(
				node,
				@base.Width + Math.Max(subscript?.Width ?? 0, superscript?.Width ?? 0) + metrics.SpaceAfterScript * scale,
				GetAscent(@base, superscript, metrics, scale),
				GetDescent(@base, subscript, metrics, scale))
		{
			_base = @base;
			_subscript = subscript;
			_superscript = superscript;
			_scriptX = @base.Width + metrics.SpaceAfterScript * scale;
			_superscriptBaseline = superscript is null
				? 0
				: -Math.Max(metrics.SuperscriptShift * scale, superscript.Descent + metrics.SuperscriptBottom * scale);
			_subscriptBaseline = subscript is null
				? 0
				: Math.Max(metrics.SubscriptShift * scale, subscript.Ascent - metrics.SubscriptTop * scale);
			if (subscript is not null
				&& superscript is not null
				&& _subscriptBaseline - subscript.Ascent
					- (_superscriptBaseline + superscript.Descent) < metrics.SubSuperscriptGap * scale)
			{
				_subscriptBaseline += metrics.SubSuperscriptGap * scale;
			}
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			_base.Arrange(context, x, baseline);
			var node = (MathScriptNode)Node;
			var baseSpan = context.GetSpan(node.Base);
			if (_subscript is not null && node.Subscript is { } subscriptNode)
			{
				context.SetIndex(
					baseSpan.End,
					new Rect(x + _scriptX, baseline + _subscriptBaseline - _subscript.Ascent, 0, _subscript.Ascent + _subscript.Descent),
					baseline + _subscriptBaseline,
					force: true);
				_subscript.Arrange(context, x + _scriptX, baseline + _subscriptBaseline);
			}
			if (_superscript is not null && node.Superscript is { } superscriptNode)
			{
				var markerIndex = node.Subscript is null
					? baseSpan.End
					: context.GetSpan(node.Subscript).End;
				context.SetIndex(
					markerIndex,
					new Rect(x + _scriptX, baseline + _superscriptBaseline - _superscript.Ascent, 0, _superscript.Ascent + _superscript.Descent),
					baseline + _superscriptBaseline,
					force: true);
				_superscript.Arrange(context, x + _scriptX, baseline + _superscriptBaseline);
			}
		}

		private static float GetAscent(MathBox @base, MathBox? superscript, MathFontMetrics metrics, float scale)
			=> superscript is null
				? @base.Ascent
				: Math.Max(
					@base.Ascent,
					Math.Max(metrics.SuperscriptShift * scale, superscript.Descent + metrics.SuperscriptBottom * scale)
						+ superscript.Ascent);

		private static float GetDescent(MathBox @base, MathBox? subscript, MathFontMetrics metrics, float scale)
			=> subscript is null
				? @base.Descent
				: Math.Max(
					@base.Descent,
					Math.Max(metrics.SubscriptShift * scale, subscript.Ascent - metrics.SubscriptTop * scale)
						+ subscript.Descent);
	}

	private sealed class OverUnderBox : MathBox
	{
		private readonly MathOverUnderNode _node;
		private readonly MathBox _base;
		private readonly MathBox? _under;
		private readonly MathBox? _over;
		private readonly MathBox? _operand;
		private readonly float _gap;
		private readonly float _coreWidth;
		private readonly float _overBaseline;
		private readonly float _underBaseline;

		internal OverUnderBox(
			MathOverUnderNode node,
			MathBox @base,
			MathBox? under,
			MathBox? over,
			MathBox? operand,
			float gap)
			: this(
				node,
				@base,
				under,
				over,
				operand,
				gap,
				Math.Max(@base.Width, Math.Max(under?.Width ?? 0, over?.Width ?? 0)),
				over is null ? 0 : -(@base.Ascent + gap + over.Descent),
				under is null ? 0 : @base.Descent + gap + under.Ascent)
		{
		}

		private OverUnderBox(
			MathOverUnderNode node,
			MathBox @base,
			MathBox? under,
			MathBox? over,
			MathBox? operand,
			float gap,
			float coreWidth,
			float overBaseline,
			float underBaseline)
			: base(
				node,
				coreWidth + (operand is { Width: > 0 } ? gap + operand.Width : 0),
				Math.Max(
					@base.Ascent,
					Math.Max(
						over is null ? 0 : -overBaseline + over.Ascent,
						operand?.Ascent ?? 0)),
				Math.Max(
					@base.Descent,
					Math.Max(
						under is null ? 0 : underBaseline + under.Descent,
						operand?.Descent ?? 0)))
		{
			_node = node;
			_base = @base;
			_under = under;
			_over = over;
			_operand = operand;
			_gap = gap;
			_coreWidth = coreWidth;
			_overBaseline = overBaseline;
			_underBaseline = underBaseline;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			_base.Arrange(context, x + (_coreWidth - _base.Width) / 2, baseline);
			if (_over is not null)
			{
				_over.Arrange(
					context,
					x + (_coreWidth - _over.Width) / 2,
					baseline + _overBaseline);
			}
			if (_under is not null)
			{
				_under.Arrange(
					context,
					x + (_coreWidth - _under.Width) / 2,
					baseline + _underBaseline);
			}
			if (_operand is not null)
			{
				_operand.Arrange(context, x + _coreWidth + _gap, baseline);
			}

			if (_node.Kind == MathOverUnderKind.Nary)
			{
				if (_node.Under is { } underNode)
				{
					SetSeparatorAfter(
						context,
						underNode,
						x,
						baseline + _underBaseline,
						_coreWidth,
						_under?.Ascent ?? Ascent,
						_under?.Descent ?? Descent);
				}
				if (_node.Over is { } overNode)
				{
					SetSeparatorAfter(
						context,
						overNode,
						x + _coreWidth,
						baseline,
						_gap,
						Ascent,
						Descent);
				}
			}
			else if (_node.Kind == MathOverUnderKind.Munderover)
			{
				SetSeparatorAfter(
					context,
					_node.Base,
					x,
					baseline,
					_coreWidth,
					Ascent,
					Descent);
				if (_node.Under is { } underNode)
				{
					SetSeparatorAfter(
						context,
						underNode,
						x,
						baseline + _underBaseline,
						_coreWidth,
						_under?.Ascent ?? Ascent,
						_under?.Descent ?? Descent);
				}
			}
		}

		private static void SetSeparatorAfter(
			ArrangeContext context,
			MathNode node,
			float x,
			float baseline,
			float width,
			float ascent,
			float descent)
		{
			var span = context.GetSpan(node);
			context.SetIndex(
				span.End,
				new Rect(x, baseline - ascent, Math.Max(0, width), ascent + descent),
				baseline,
				force: true);
		}
	}

	private sealed class ScriptPairBox
	{
		internal ScriptPairBox(MathScriptPair pair, MathBox? subscript, MathBox? superscript)
		{
			Pair = pair;
			Subscript = subscript;
			Superscript = superscript;
			Width = Math.Max(subscript?.Width ?? 0, superscript?.Width ?? 0);
		}

		internal MathScriptPair Pair { get; }

		internal MathBox? Subscript { get; }

		internal MathBox? Superscript { get; }

		internal float Width { get; }
	}

	private sealed class MultiScriptsBox : MathBox
	{
		private readonly MathMultiScriptsNode _node;
		private readonly MathBox _body;
		private readonly ScriptPairBox[] _pairs;
		private readonly float _gap;
		private readonly float _superscriptBaseline;
		private readonly float _subscriptBaseline;
		private readonly float _prescriptWidth;

		internal MultiScriptsBox(
			MathMultiScriptsNode node,
			MathBox body,
			ScriptPairBox[] pairs,
			MathFontMetrics metrics,
			float scale,
			float gap)
			: this(
				node,
				body,
				pairs,
				gap,
				GetSuperscriptBaseline(pairs, metrics, scale),
				GetSubscriptBaseline(pairs, metrics, scale),
				pairs.Sum(pair => pair.Width) + pairs.Length * gap)
		{
		}

		private MultiScriptsBox(
			MathMultiScriptsNode node,
			MathBox body,
			ScriptPairBox[] pairs,
			float gap,
			float superscriptBaseline,
			float subscriptBaseline,
			float prescriptWidth)
			: base(
				node,
				prescriptWidth + body.Width,
				Math.Max(
					body.Ascent,
					pairs.Length == 0
						? 0
						: pairs.Max(pair => pair.Superscript is null
							? 0
							: -superscriptBaseline + pair.Superscript.Ascent)),
				Math.Max(
					body.Descent,
					pairs.Length == 0
						? 0
						: pairs.Max(pair => pair.Subscript is null
							? 0
							: subscriptBaseline + pair.Subscript.Descent)))
		{
			_node = node;
			_body = body;
			_pairs = pairs;
			_gap = gap;
			_superscriptBaseline = superscriptBaseline;
			_subscriptBaseline = subscriptBaseline;
			_prescriptWidth = prescriptWidth;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			var pairX = x;
			foreach (var pair in _pairs)
			{
				if (pair.Superscript is not null)
				{
					pair.Superscript.Arrange(
						context,
						pairX + pair.Width - pair.Superscript.Width,
						baseline + _superscriptBaseline);
				}
				if (pair.Subscript is not null)
				{
					pair.Subscript.Arrange(
						context,
						pairX + pair.Width - pair.Subscript.Width,
						baseline + _subscriptBaseline);
				}

				var markerIndex = pair.Pair.Subscript is { } subscript
					? context.GetSpan(subscript).End
					: GetNextMarkerIndex(context, pair.Pair.Superscript, _node);
				context.SetIndex(
					markerIndex,
					new Rect(pairX, baseline, pair.Width, Math.Max(1, Descent)),
					baseline,
					force: true);
				markerIndex = pair.Pair.Superscript is { } superscript
					? context.GetSpan(superscript).End
					: markerIndex + 1;
				context.SetIndex(
					markerIndex,
					new Rect(pairX + pair.Width, baseline - Ascent, _gap, Ascent + Descent),
					baseline,
					force: true);
				pairX += pair.Width + _gap;
			}

			_body.Arrange(context, x + _prescriptWidth, baseline);
		}

		private static int GetNextMarkerIndex(ArrangeContext context, MathNode? next, MathMultiScriptsNode owner)
			=> next is null
				? context.GetSpan(owner).Start + 1
				: Math.Max(context.GetSpan(owner).Start + 1, context.GetSpan(next).Start - 1);

		private static float GetSuperscriptBaseline(
			IReadOnlyList<ScriptPairBox> pairs,
			MathFontMetrics metrics,
			float scale)
		{
			var maxDescent = pairs.Count == 0
				? 0
				: pairs.Max(pair => pair.Superscript?.Descent ?? 0);
			return -Math.Max(metrics.SuperscriptShift * scale, maxDescent + metrics.SuperscriptBottom * scale);
		}

		private static float GetSubscriptBaseline(
			IReadOnlyList<ScriptPairBox> pairs,
			MathFontMetrics metrics,
			float scale)
		{
			var maxAscent = pairs.Count == 0
				? 0
				: pairs.Max(pair => pair.Subscript?.Ascent ?? 0);
			return Math.Max(metrics.SubscriptShift * scale, maxAscent - metrics.SubscriptTop * scale);
		}
	}

	private sealed class FencedBox : MathBox
	{
		private readonly MathBox _open;
		private readonly MathBox _inner;
		private readonly MathBox _close;

		internal FencedBox(MathFencedNode node, MathBox open, MathBox inner, MathBox close)
			: base(
				node,
				open.Width + inner.Width + close.Width,
				Math.Max(inner.Ascent, Math.Max(open.Ascent, close.Ascent)),
				Math.Max(inner.Descent, Math.Max(open.Descent, close.Descent)))
		{
			_open = open;
			_inner = inner;
			_close = close;
		}

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			_open.Arrange(context, x, baseline);
			x += _open.Width;
			_inner.Arrange(context, x, baseline);
			x += _inner.Width;
			_close.Arrange(context, x, baseline);
		}
	}

	private sealed class TableBox : MathBox
	{
		private readonly MathBox[][] _rows;
		private readonly MathBox _open;
		private readonly MathBox _close;
		private readonly float[] _columnWidths;
		private readonly float[] _rowAscents;
		private readonly float[] _rowDescents;
		private readonly float _horizontalGap;
		private readonly float _verticalGap;
		private readonly float _contentWidth;

		internal TableBox(
			MathTableNode node,
			MathBox[][] rows,
			MathBox open,
			MathBox close,
			float horizontalGap,
			float verticalGap,
			float axisHeight)
			: this(
				node,
				rows,
				open,
				close,
				horizontalGap,
				verticalGap,
				axisHeight,
				GetColumnWidths(rows),
				GetRowAscents(rows),
				GetRowDescents(rows))
		{
		}

		private TableBox(
			MathTableNode node,
			MathBox[][] rows,
			MathBox open,
			MathBox close,
			float horizontalGap,
			float verticalGap,
			float axisHeight,
			float[] columnWidths,
			float[] rowAscents,
			float[] rowDescents)
			: base(
				node,
				open.Width + GetContentWidth(columnWidths, horizontalGap) + close.Width,
				GetContentHeight(rowAscents, rowDescents, verticalGap) / 2 + axisHeight,
				Math.Max(0, GetContentHeight(rowAscents, rowDescents, verticalGap) / 2 - axisHeight))
		{
			_rows = rows;
			_open = open;
			_close = close;
			_horizontalGap = horizontalGap;
			_verticalGap = verticalGap;
			_columnWidths = columnWidths;
			_rowAscents = rowAscents;
			_rowDescents = rowDescents;
			_contentWidth = GetContentWidth(columnWidths, horizontalGap);
		}

		internal static float GetContentHeight(MathBox[][] rows, float verticalGap)
			=> GetContentHeight(GetRowAscents(rows), GetRowDescents(rows), verticalGap);

		internal override void Arrange(ArrangeContext context, float x, float baseline)
		{
			context.SetNodeBounds(Node, x, baseline, Width, Ascent, Descent);
			_open.Arrange(context, x, baseline);
			var contentX = x + _open.Width;
			var rowTop = baseline - Ascent;
			var node = (MathTableNode)Node;
			for (var rowIndex = 0; rowIndex < _rows.Length; rowIndex++)
			{
				var rowBaseline = rowTop + _rowAscents[rowIndex];
				var columnX = contentX;
				for (var columnIndex = 0; columnIndex < _rows[rowIndex].Length; columnIndex++)
				{
					var cell = _rows[rowIndex][columnIndex];
					cell.Arrange(
						context,
						columnX + (_columnWidths[columnIndex] - cell.Width) / 2,
						rowBaseline);
					if (columnIndex + 1 < _rows[rowIndex].Length)
					{
						var cellSpan = context.GetSpan(node.Rows[rowIndex].Cells[columnIndex]);
						context.SetIndex(
							cellSpan.End,
							new Rect(
								columnX + _columnWidths[columnIndex],
								rowTop,
								_horizontalGap,
								_rowAscents[rowIndex] + _rowDescents[rowIndex]),
							rowBaseline,
							force: true);
					}
					columnX += _columnWidths[columnIndex] + _horizontalGap;
				}

				rowTop += _rowAscents[rowIndex] + _rowDescents[rowIndex];
				if (rowIndex + 1 < _rows.Length)
				{
					var cells = node.Rows[rowIndex].Cells;
					var lastCell = cells.Count == 0 ? null : cells[^1];
					if (lastCell is not null)
					{
						var lastSpan = context.GetSpan(lastCell);
						context.SetIndex(
							lastSpan.End,
							new Rect(contentX, rowTop, _contentWidth, _verticalGap),
							rowTop + _verticalGap / 2,
							force: true);
					}
					rowTop += _verticalGap;
				}
			}

			_close.Arrange(context, contentX + _contentWidth, baseline);
		}

		private static float[] GetColumnWidths(MathBox[][] rows)
		{
			var columnCount = rows.Length == 0 ? 0 : rows.Max(row => row.Length);
			var widths = new float[columnCount];
			foreach (var row in rows)
			{
				for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
				{
					widths[columnIndex] = Math.Max(widths[columnIndex], row[columnIndex].Width);
				}
			}

			return widths;
		}

		private static float[] GetRowAscents(MathBox[][] rows)
		{
			var values = new float[rows.Length];
			for (var index = 0; index < rows.Length; index++)
			{
				values[index] = rows[index].Length == 0 ? 0 : rows[index].Max(cell => cell.Ascent);
			}

			return values;
		}

		private static float[] GetRowDescents(MathBox[][] rows)
		{
			var values = new float[rows.Length];
			for (var index = 0; index < rows.Length; index++)
			{
				values[index] = rows[index].Length == 0 ? 0 : rows[index].Max(cell => cell.Descent);
			}

			return values;
		}

		private static float GetContentWidth(float[] columnWidths, float horizontalGap)
			=> columnWidths.Sum() + Math.Max(0, columnWidths.Length - 1) * horizontalGap;

		private static float GetContentHeight(float[] ascents, float[] descents, float verticalGap)
		{
			var height = 0f;
			for (var index = 0; index < ascents.Length; index++)
			{
				height += ascents[index] + descents[index];
			}

			return height + Math.Max(0, ascents.Length - 1) * verticalGap;
		}
	}

	private sealed class InlineStyleResolver
	{
		private readonly List<(int End, Run Run)> _runs = new();
		private readonly Run _fallback;

		internal InlineStyleResolver(Inline[] inlines, FontDetails defaultFontDetails, Brush? defaultForeground)
		{
			var end = 0;
			foreach (var inline in inlines)
			{
				var text = inline.GetText();
				end += text.Length;
				if (inline is Run run)
				{
					_runs.Add((end, run));
				}
			}

			_fallback = new Run
			{
				FontFamily = new FontFamily(defaultFontDetails.SKFont.Typeface.FamilyName),
				FontSize = defaultFontDetails.SKFontSize,
				Foreground = defaultForeground,
			};
		}

		internal float DefaultFontSize => (float)(_runs.FirstOrDefault().Run?.FontSize ?? _fallback.FontSize);

		internal Brush? GetBrush(int position) => GetRun(position).Foreground;

		internal Run CreateRun(int position, string text, float scale, Brush? foreground)
		{
			var source = GetRun(position);
			return new Run
			{
				Text = text,
				FontFamily = source.FontFamily,
				FontSize = Math.Max(1, source.FontSize * scale),
				FontStretch = source.FontStretch,
				FontStyle = source.FontStyle,
				FontWeight = source.FontWeight,
				Foreground = foreground ?? source.Foreground,
				FlowDirection = FlowDirection.LeftToRight,
			};
		}

		private Run GetRun(int position)
		{
			foreach (var (end, run) in _runs)
			{
				if (position < end)
				{
					return run;
				}
			}

			return _runs.Count > 0 ? _runs[^1].Run : _fallback;
		}
	}
}
