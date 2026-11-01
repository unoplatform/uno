#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml;

/// <summary>
/// The objects that hold a shareable object (a brush, transform, geometry or projection) as the value of one of their
/// properties, like the parent list of WinUI's CMultiParentShareableDependencyObject.
/// </summary>
/// <remarks>
/// Uno parents only some property values, and a single parent at most. The animation engine needs every user of an
/// animated brush or transform to tell whether the animation is independent (CAnimation::CheckElementUsingAnimatedTarget).
/// </remarks>
internal static class SharedParentAssociations
{
	private static readonly ConditionalWeakTable<DependencyObject, List<WeakReference<DependencyObject>>> _parents = new();

	internal static void Update(DependencyObject owner, object? previousValue, object? newValue)
	{
		if (ReferenceEquals(previousValue, newValue))
		{
			return;
		}

		if (IsTracked(previousValue) && _parents.TryGetValue((DependencyObject)previousValue!, out var previousParents))
		{
			// One entry per property value, so an object used by two properties of the same owner keeps one.
			for (var i = 0; i < previousParents.Count; i++)
			{
				if (previousParents[i].TryGetTarget(out var parent) && ReferenceEquals(parent, owner))
				{
					previousParents.RemoveAt(i);
					break;
				}
			}
		}

		if (IsTracked(newValue))
		{
			var parents = _parents.GetOrCreateValue((DependencyObject)newValue!);
			parents.RemoveAll(static parent => !parent.TryGetTarget(out _));
			parents.Add(new WeakReference<DependencyObject>(owner));
		}
	}

	internal static IEnumerable<DependencyObject> GetParents(DependencyObject value)
	{
		if (_parents.TryGetValue(value, out var parents))
		{
			// A copy, since walking the parents can change the associations.
			foreach (var parent in parents.ToArray())
			{
				if (parent.TryGetTarget(out var target))
				{
					yield return target;
				}
			}
		}
	}

	private static bool IsTracked(object? value)
		=> value is IMultiParentShareableDependencyObject or Geometry or Projection;
}
