#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Composition;

/// <summary>
/// Maps a property name (the trigger) to the animation or animation group started whenever that property is assigned.
/// </summary>
public partial class ImplicitAnimationCollection : CompositionObject, IDictionary<string, ICompositionAnimationBase>
{
	private readonly Dictionary<string, ICompositionAnimationBase> _animations = new(StringComparer.OrdinalIgnoreCase);

	internal ImplicitAnimationCollection(Compositor compositor) : base(compositor)
	{
	}

	public ICollection<string> Keys => _animations.Keys;

	public ICollection<ICompositionAnimationBase> Values => _animations.Values;

	public int Count => _animations.Count;

	public bool IsReadOnly => false;

	public ICompositionAnimationBase this[string key]
	{
		get => _animations[key];
		set
		{
			Validate(key, value);
			_animations[key] = value;
		}
	}

	public void Add(string key, ICompositionAnimationBase value)
	{
		Validate(key, value);
		_animations.Add(key, value);
	}

	public bool ContainsKey(string key) => _animations.ContainsKey(key);

	public bool Remove(string key) => _animations.Remove(key);

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out ICompositionAnimationBase value) => _animations.TryGetValue(key, out value);

	public void Add(KeyValuePair<string, ICompositionAnimationBase> item) => Add(item.Key, item.Value);

	public void Clear() => _animations.Clear();

	public bool Contains(KeyValuePair<string, ICompositionAnimationBase> item)
		=> ((ICollection<KeyValuePair<string, ICompositionAnimationBase>>)_animations).Contains(item);

	public void CopyTo(KeyValuePair<string, ICompositionAnimationBase>[] array, int arrayIndex)
		=> ((ICollection<KeyValuePair<string, ICompositionAnimationBase>>)_animations).CopyTo(array, arrayIndex);

	bool ICollection<KeyValuePair<string, ICompositionAnimationBase>>.Remove(KeyValuePair<string, ICompositionAnimationBase> item)
		=> ((ICollection<KeyValuePair<string, ICompositionAnimationBase>>)_animations).Remove(item);

	public IEnumerator<KeyValuePair<string, ICompositionAnimationBase>> GetEnumerator() => _animations.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	// WinUI rejects animations that don't name the property they animate.
	private static void Validate(string key, ICompositionAnimationBase value)
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(value);

		switch (value)
		{
			case CompositionAnimation animation when string.IsNullOrEmpty(animation.Target):
				throw new ArgumentException("An implicit animation must have its Target set.", nameof(value));

			case CompositionAnimationGroup group:
				foreach (var animation in group)
				{
					if (string.IsNullOrEmpty(animation.Target))
					{
						throw new ArgumentException("Every animation of an implicit animation group must have its Target set.", nameof(value));
					}
				}
				break;

			case CompositionAnimation:
				break;

			default:
				throw new ArgumentException($"Unsupported implicit animation type '{value.GetType()}'.", nameof(value));
		}
	}
}
