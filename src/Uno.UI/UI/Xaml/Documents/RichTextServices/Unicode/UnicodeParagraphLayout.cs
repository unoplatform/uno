#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Text;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// One <see cref="UnicodeText"/> layout of a paragraph, as <see cref="UnicodeTextFormatter"/> vends its lines.
/// </summary>
/// <remarks>
/// <para>
/// The layout's text holds one U+FFFC per <see cref="InlineUIContainer"/>, while the layout tree counts an object as no
/// position at all. Engine indices are the tree's ("node") indices plus one per placeholder before them.
/// </para>
/// <para>
/// A trimmed line is read from a twin: the same layout with trimming at the line's collapsing width. A base layout keeps
/// the few twins its pages draw, most recently used first.
/// </para>
/// <para>
/// The lines are kept with the layout, so a measure that changes nothing allocates none.
/// </para>
/// </remarks>
internal sealed class UnicodeParagraphLayout
{
	/// <summary>The inputs a layout depends on, besides its content, whose change recreates the layout tree.</summary>
	internal readonly record struct Key(
		TextSource Source,
		double WrappingWidth,
		int ResumeIndex,
		TextWrapping Wrapping,
		bool Justify,
		TextLineBounds LineBounds,
		FlowDirection FlowDirection,
		bool DetectReadingOrder);

	private sealed record ObjectEntry(InlineUIContainer Container, ObjectRun? Run, ObjectRunMetrics Metrics, InlineObjectInfo Info);

	private readonly Inline[] _inlines;
	private readonly TextParagraphProperties _properties;
	private readonly IUnicodeParagraphSource _source;

	// The engine index of each placeholder, and its container, in text order.
	private readonly int[] _placeholders;
	private readonly InlineUIContainer[] _containers;

	// Node positions of the placeholders: _placeholders[k] - k.
	private readonly int[] _placeholderNodePositions;

	// The measured objects (those at or after the resume index), shared with the twins.
	private readonly Dictionary<InlineUIContainer, ObjectEntry> _objects;

	// Shared by the paragraphs without objects, and never written: only an object is measured into it.
	private static readonly Dictionary<InlineUIContainer, ObjectEntry> _noObjects = new();

	private readonly bool _nodeTextIsEmpty;
	private readonly int _firstLineEnd;

	// The source's ForegroundVersion this layout's brushes are from.
	private int _foregroundVersion;

	// The length of each inline's text when this layout was made: the engine's runs follow the inlines that had text.
	private readonly int[] _textLengths;

	private readonly UnicodeParagraphLayout? _base;

	// Trimmed twins, most recently used first: a page draws from one per width, or two with a paragraph ellipsis.
	private const int TwinCapacity = 4;
	private List<UnicodeParagraphLayout>? _twins;

	// The lines vended so far, each for the page's paragraph (source) it was formatted for.
	private UnicodeTextLine?[]? _lines;

	private UnicodeParagraphLayout(
		Key key,
		Inline[] inlines,
		TextParagraphProperties properties,
		IUnicodeParagraphSource source,
		int[] placeholders,
		InlineUIContainer[] containers,
		int[] placeholderNodePositions,
		Dictionary<InlineUIContainer, ObjectEntry> objects,
		bool nodeTextIsEmpty,
		UnicodeParagraphLayout? @base,
		TextTrimming trimming,
		double trimmingWidth,
		int? forcedEllipsisLine)
	{
		LayoutKey = key;
		_inlines = inlines;
		_properties = properties;
		_source = source;
		_placeholders = placeholders;
		_containers = containers;
		_placeholderNodePositions = placeholderNodePositions;
		_objects = objects;
		_nodeTextIsEmpty = nodeTextIsEmpty;
		_base = @base;
		Trimming = trimming;
		TrimmingWidth = trimmingWidth;

		_foregroundVersion = source.ForegroundVersion;
		_textLengths = new int[inlines.Length];
		for (var i = 0; i < inlines.Length; i++)
		{
			_textLengths[i] = inlines[i].GetText().Length;
		}

		Text = Build(forcedEllipsisLine);
		_firstLineEnd = Text.GetLineInfo(0).End;
	}

	public Key LayoutKey { get; }

	public UnicodeText Text { get; }

	/// <summary>The layout this one trims, or itself.</summary>
	public UnicodeParagraphLayout Base => _base ?? this;

	public TextTrimming Trimming { get; }

	public double TrimmingWidth { get; }

	/// <summary>Whether the paragraph holds objects.</summary>
	public bool HasObjects => _containers.Length > 0;

	/// <summary>
	/// The line at <paramref name="engineLine"/> for a page's paragraph; <paramref name="collapsedFrom"/> is the line of
	/// the base layout this twin trims.
	/// </summary>
	public UnicodeTextLine GetLine(TextSource source, int engineLine, UnicodeTextLine? collapsedFrom = null)
	{
		var lines = _lines ??= new UnicodeTextLine?[Text.VisualLineCount];
		if ((uint)engineLine >= (uint)lines.Length)
		{
			return new UnicodeTextLine(source, this, engineLine, collapsedFrom);
		}

		if (lines[engineLine] is not { } line || !ReferenceEquals(line.TextSource, source))
		{
			lines[engineLine] = line = new UnicodeTextLine(source, this, engineLine, collapsedFrom);
		}

		return line;
	}

	public static UnicodeParagraphLayout Create(in Key key, IUnicodeParagraphSource source, TextParagraphProperties properties)
	{
		var inlines = source.GetLeafInlines();

		List<int>? placeholders = null;
		List<InlineUIContainer>? containers = null;
		var index = 0;
		var nodeLength = 0;
		foreach (var inline in inlines)
		{
			var text = inline.GetText();
			if (text.Length > 0)
			{
				index += text.Length;
				nodeLength += text.Length;
			}
			else if (inline is InlineUIContainer container)
			{
				(placeholders ??= new()).Add(index++);
				(containers ??= new()).Add(container);
			}
		}

		if (placeholders is null || containers is null)
		{
			return new UnicodeParagraphLayout(key, inlines, properties, source, [], [], [], _noObjects, nodeLength == 0, null, TextTrimming.None, key.WrappingWidth, null);
		}

		var nodePositions = new int[placeholders.Count];
		for (var k = 0; k < nodePositions.Length; k++)
		{
			nodePositions[k] = placeholders[k] - k;
		}

		return new UnicodeParagraphLayout(
			key,
			inlines,
			properties,
			source,
			placeholders.ToArray(),
			containers.ToArray(),
			nodePositions,
			new Dictionary<InlineUIContainer, ObjectEntry>(),
			nodeLength == 0,
			null,
			TextTrimming.None,
			key.WrappingWidth,
			null);
	}

	private UnicodeText Build(int? forcedEllipsisLine)
	{
		var options = new UnicodeTextOptions
		{
			ResumeIndex = LayoutKey.ResumeIndex,
			TrimmingWidth = Trimming == TextTrimming.None ? null : (float)TrimmingWidth,
			ForcedEllipsisLine = forcedEllipsisLine,
			InlineContainers = _containers.Length == 0 ? null : GetInlineObjectInfo,
		};

		return new UnicodeText(
			new Size(LayoutKey.WrappingWidth, double.PositiveInfinity),
			_inlines,
			_properties.DefaultTextRunProperties.FontTypeface,
			maxLines: 0, // Paging and MaxLines belong to the layout tree.
			lineHeight: 0, // ParagraphNode stacks the lines, as in WinUI.
			LineStackingStrategy.MaxHeight,
			LayoutKey.LineBounds,
			LayoutKey.FlowDirection,
			// The layout tree places each line; the engine only justifies.
			LayoutKey.Justify ? TextAlignment.Justify : TextAlignment.Left,
			LayoutKey.Wrapping,
			Trimming,
			isSpellCheckEnabled: false,
			_source.FontListener,
			includeTrailingWhitespaceInMeasurement: false,
			// WinUI's default tab stops, every four times the font size.
			defaultTabStop: (float)_properties.DefaultIncrementalTab,
			endingParagraphLayout: null,
			endingParagraphAlignment: null,
			_source.DefaultForeground,
			alignmentIncludesTrailingWhitespace: false,
			ignoreTrailingCharacterSpacing: true,
			out _,
			options: options);
	}

	/// <summary>
	/// Takes the content's current brushes when they changed after this layout was made: drawing is the only reader of a
	/// brush, so a hover or a foreground change repaints without laying the paragraph out again. False when the text
	/// changed too and the layout hasn't been made again yet: the runs no longer map to the inlines, so the brushes stay.
	/// </summary>
	public bool UpdateForegrounds(int version)
	{
		if (_foregroundVersion == version)
		{
			return true;
		}

		for (var i = 0; i < _inlines.Length; i++)
		{
			if (_inlines[i].GetText().Length != _textLengths[i])
			{
				return false;
			}
		}

		_foregroundVersion = version;
		Text.UpdateForegrounds(_inlines, withContainers: _containers.Length > 0);
		return true;
	}

	// A base layout measures each object as the engine reaches it; a twin or a rebuild reuses the sizes already known.
	private InlineObjectInfo? GetInlineObjectInfo(InlineUIContainer container)
	{
		if (_objects.TryGetValue(container, out var entry))
		{
			return entry.Info;
		}

		var formatted = _source.FormatInlineObject(container, (float)LayoutKey.WrappingWidth);
		var metrics = formatted?.Metrics ?? default;
		entry = new ObjectEntry(container, formatted?.Run, metrics, ToInfo(metrics));
		_objects[container] = entry;
		return entry.Info;
	}

	private static bool Matches(ObjectRunMetrics metrics, InlineObjectInfo info)
		=> metrics.Width == info.Width && metrics.Height == info.Height && metrics.Baseline == info.Ascent;

	private static InlineObjectInfo ToInfo(ObjectRunMetrics metrics)
		=> new(null, null, metrics.Width, metrics.Height, metrics.Baseline, VerticalCharacterAlignment.Baseline);

	/// <summary>
	/// Formats the objects again, as WinUI formats object runs on every fresh line format, so that each child is
	/// re-hosted and measured. While measuring, a size change lays the paragraph out again and returns the new layout;
	/// otherwise the change waits for the measure it scheduled.
	/// </summary>
	public UnicodeParagraphLayout RefreshObjects(IUnicodeParagraphSource source)
	{
		if (_base is not null || _objects.Count == 0)
		{
			return this;
		}

		// Compared with the sizes the layout was made with, not the last ones seen: a hit-test between two layout passes
		// may have measured a child's new size already, and the measure it scheduled must still lay the paragraph out.
		var changed = false;
		foreach (var container in _containers)
		{
			if (!_objects.TryGetValue(container, out var entry))
			{
				// Before the resume index: another layout hosts it.
				continue;
			}

			var formatted = source.FormatInlineObject(container, (float)LayoutKey.WrappingWidth);
			var metrics = formatted?.Metrics ?? default;
			changed |= !Matches(metrics, entry.Info);
			_objects[container] = entry with { Run = formatted?.Run, Metrics = metrics };
		}

		if (!changed || !source.CanHostElements)
		{
			return this;
		}

		var objects = new Dictionary<InlineUIContainer, ObjectEntry>(_objects.Count);
		foreach (var (container, entry) in _objects)
		{
			objects[container] = entry with { Info = ToInfo(entry.Metrics) };
		}

		return new UnicodeParagraphLayout(
			LayoutKey,
			_inlines,
			_properties,
			_source,
			_placeholders,
			_containers,
			_placeholderNodePositions,
			objects,
			_nodeTextIsEmpty,
			null,
			TextTrimming.None,
			LayoutKey.WrappingWidth,
			null);
	}

	/// <summary>Drops the lines formatted for a page's paragraph that is deleted, in this layout and its twins.</summary>
	public void ForgetLines(TextSource source)
	{
		if (_lines is { } lines)
		{
			for (var i = 0; i < lines.Length; i++)
			{
				if (lines[i] is { } line && ReferenceEquals(line.TextSource, source))
				{
					lines[i] = null;
				}
			}
		}

		if (_twins is { } twins)
		{
			foreach (var twin in twins)
			{
				twin.ForgetLines(source);
			}
		}
	}

	/// <summary>
	/// The twin that trims <paramref name="line"/> at <paramref name="width"/>: a paragraph ellipsis when
	/// <paramref name="forced"/>.
	/// </summary>
	public UnicodeParagraphLayout GetTrimmed(int line, double width, TextTrimming style, bool forced)
	{
		var twins = _twins ??= new(TwinCapacity);
		for (var i = 0; i < twins.Count; i++)
		{
			var twin = twins[i];
			if (TrimsAt(twin, line, width, style))
			{
				if (i > 0)
				{
					twins.RemoveAt(i);
					twins.Insert(0, twin);
				}

				return twin;
			}
		}

		if (twins.Count == TwinCapacity)
		{
			twins.RemoveAt(TwinCapacity - 1);
		}

		var created = new UnicodeParagraphLayout(
			LayoutKey,
			_inlines,
			_properties,
			_source,
			_placeholders,
			_containers,
			_placeholderNodePositions,
			_objects,
			_nodeTextIsEmpty,
			this,
			style,
			width,
			forced ? line : null);
		twins.Insert(0, created);
		return created;
	}

	// Whether a twin gives the trimmed line for that width. It does for any width between its trimming width and its
	// trimmed line's: hit-tests collapse the line again at the width it was drawn at, which can be either side of the
	// trimming width when the line keeps more than fits (its first character, or an object it overflows at).
	private static bool TrimsAt(UnicodeParagraphLayout twin, int line, double width, TextTrimming style)
	{
		if (twin.Trimming != style || line >= twin.Text.VisualLineCount)
		{
			return false;
		}

		var info = twin.Text.GetLineInfo(line);
		return info.HasEllipsis
			&& width >= Math.Min(info.Width, twin.TrimmingWidth)
			&& width <= Math.Max(info.Width, twin.TrimmingWidth);
	}

	/// <summary>
	/// Lets another page of the same width vend this layout's lines from <paramref name="engineStart"/> on. The objects
	/// there are formatted again for that page, whose paragraph hosts them. False when one of them changed size, which
	/// needs a layout of its own.
	/// </summary>
	public bool ContinueOn(IUnicodeParagraphSource source, int engineStart)
	{
		for (var k = 0; k < _containers.Length; k++)
		{
			if (_placeholders[k] < engineStart)
			{
				continue;
			}

			var container = _containers[k];
			if (!_objects.TryGetValue(container, out var entry))
			{
				return false;
			}

			var formatted = source.FormatInlineObject(container, (float)LayoutKey.WrappingWidth);
			var metrics = formatted?.Metrics ?? default;
			_objects[container] = entry with { Run = formatted?.Run, Metrics = metrics };
			if (!Matches(metrics, entry.Info))
			{
				return false;
			}
		}

		return true;
	}

	internal IReadOnlyList<UnicodeParagraphLayout> TwinsForTests => _twins ?? (IReadOnlyList<UnicodeParagraphLayout>)Array.Empty<UnicodeParagraphLayout>();

	/// <summary>The run of the object at an engine index, for arranging it.</summary>
	public ObjectRun? GetObjectRun(int engineIndex)
	{
		var k = Array.BinarySearch(_placeholders, engineIndex);
		return k >= 0 && Base._objects.TryGetValue(_containers[k], out var entry) ? entry.Run : null;
	}

	/// <summary>The container of the object at an engine index.</summary>
	public InlineUIContainer? GetContainer(int engineIndex)
	{
		var k = Array.BinarySearch(_placeholders, engineIndex);
		return k >= 0 ? _containers[k] : null;
	}

	/// <summary>
	/// The node index of an engine index. A paragraph without text (empty, or objects only) still takes one position, on
	/// its first line, as WinUI's paragraph terminator does.
	/// </summary>
	public int ToNode(int engineIndex)
	{
		if (_base is not null)
		{
			return _base.ToNode(engineIndex);
		}

		if (_nodeTextIsEmpty)
		{
			return engineIndex > 0 && engineIndex >= _firstLineEnd ? 1 : 0;
		}

		return engineIndex - CountLess(_placeholders, engineIndex);
	}

	/// <summary>The engine index of a node index, before the objects at that position.</summary>
	public int ToEngineStart(int nodeIndex)
		=> _nodeTextIsEmpty ? 0 : nodeIndex + CountLess(_placeholderNodePositions, nodeIndex);

	/// <summary>The engine index of a node index, after the objects at that position.</summary>
	public int ToEngineEnd(int nodeIndex)
		=> _nodeTextIsEmpty ? 0 : nodeIndex + CountLess(_placeholderNodePositions, nodeIndex + 1);

	/// <summary>Whether a paragraph without text raises its first line to one position.</summary>
	public bool NodeTextIsEmpty => _nodeTextIsEmpty;

	// The number of values below value in an ascending array.
	private static int CountLess(int[] sorted, int value)
	{
		int lo = 0, hi = sorted.Length;
		while (lo < hi)
		{
			var mid = (lo + hi) / 2;
			if (sorted[mid] < value)
			{
				lo = mid + 1;
			}
			else
			{
				hi = mid;
			}
		}

		return lo;
	}
}
