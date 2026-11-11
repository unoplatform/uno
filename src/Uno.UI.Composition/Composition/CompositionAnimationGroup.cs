#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UI.Composition;

/// <summary>
/// A group of animations started together on one object, each on the property named by its <see cref="CompositionAnimation.Target"/>.
/// </summary>
public partial class CompositionAnimationGroup : CompositionObject, IEnumerable<CompositionAnimation>, ICompositionAnimationBase
{
	private readonly List<CompositionAnimation> _animations = new();

	internal CompositionAnimationGroup(Compositor compositor) : base(compositor)
	{
	}

	public int Count => _animations.Count;

	public void Add(CompositionAnimation value)
	{
		ArgumentNullException.ThrowIfNull(value);

		if (!_animations.Contains(value))
		{
			_animations.Add(value);
		}
	}

	public void Remove(CompositionAnimation value)
	{
		ArgumentNullException.ThrowIfNull(value);

		_animations.Remove(value);
	}

	public void RemoveAll() => _animations.Clear();

	public IEnumerator<CompositionAnimation> GetEnumerator() => _animations.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	// Snapshot so a start or trigger can't observe the group being changed underneath it.
	internal CompositionAnimation[] GetAnimations() => _animations.ToArray();
}
