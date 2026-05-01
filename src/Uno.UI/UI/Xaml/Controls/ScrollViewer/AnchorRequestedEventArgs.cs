#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class AnchorRequestedEventArgs
{
	private readonly List<UIElement> _anchorCandidates = new();

	internal AnchorRequestedEventArgs()
	{
	}

	public UIElement? Anchor { get; set; }

	public IList<UIElement> AnchorCandidates => _anchorCandidates;

	internal void Reset(IEnumerable<UIElement> candidates)
	{
		Anchor = null;
		_anchorCandidates.Clear();
		foreach (var c in candidates)
		{
			_anchorCandidates.Add(c);
		}
	}
}
