#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// The paragraph layouts of a <see cref="BlockLayout.ParagraphNode"/>, shared through its break with the nodes that
/// continue the paragraph on later pages. Without it every page and every hit-test would lay the whole paragraph out
/// again.
/// </summary>
/// <remarks>
/// Each page's paragraph keeps the layout of its current width only: arrange and hit-tests reuse the measure width, and
/// a layout holds the whole paragraph, so an older width's is let go rather than kept for a resize back. A page's layout
/// also goes when its paragraph node is deleted. The cache dies with the master's node, which a content change deletes.
/// </remarks>
internal sealed class UnicodeTextRunCache : TextRunCache
{
	// Most recently used first.
	private readonly List<UnicodeParagraphLayout> _layouts = new();

	public UnicodeParagraphLayout? Find(in UnicodeParagraphLayout.Key key)
	{
		for (var i = 0; i < _layouts.Count; i++)
		{
			var layout = _layouts[i];
			if (layout.LayoutKey == key)
			{
				if (i > 0)
				{
					_layouts.RemoveAt(i);
					_layouts.Insert(0, layout);
				}

				return layout;
			}
		}

		return null;
	}

	// Replaces the layout of the same page's paragraph, whatever its width.
	public void Add(UnicodeParagraphLayout layout)
	{
		Remove(layout.LayoutKey.Source);
		_layouts.Insert(0, layout);
	}

	/// <summary>Lets go of the layouts of a page's paragraph that is no longer laid out.</summary>
	public void Remove(TextSource source)
	{
		for (var i = _layouts.Count - 1; i >= 0; i--)
		{
			if (ReferenceEquals(_layouts[i].LayoutKey.Source, source))
			{
				_layouts.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Lets go of everything a deleted page's paragraph holds: its layouts, and the lines it formatted from the layouts
	/// other pages keep.
	/// </summary>
	public void Release(TextSource source)
	{
		Remove(source);
		foreach (var layout in _layouts)
		{
			layout.ForgetLines(source);
		}
	}

	public override void Clear() => _layouts.Clear();

	internal int CountForTests => _layouts.Count;

	internal IReadOnlyList<UnicodeParagraphLayout> LayoutsForTests => _layouts;
}
