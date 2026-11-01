#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline
{
	// CDependencyObject::IsAnimatedProperty
	private static bool IsAnimatedProperty(DependencyObject target, DependencyProperty property)
		=> target.GetAnimatedValue(property) != DependencyProperty.UnsetValue;

	// CDependencyObject::GetAnimationBaseValue: the value the property has under its animated value.
	private static object? GetAnimationBaseValue(DependencyObject target, DependencyProperty property)
		=> target.GetBaseValue(property).value;

	// CAnimation::IsIndependentAnimation, for the visual transitions generator.
	internal bool IsIndependentAnimationFor(DependencyObject targetObject) => IsIndependentAnimation(targetObject);

	// CDependencyObject::SetAnimatedValue
	private static void SetAnimatedValue(DependencyObject target, DependencyProperty property, object? value)
		=> target.SetValue(property, value, DependencyPropertyValuePrecedences.Animations);

	// CDependencyObject::ClearAnimatedValue
	private static void ClearAnimatedValue(DependencyObject target, DependencyProperty property)
		=> target.ClearValue(property, DependencyPropertyValuePrecedences.Animations);

	// CCustomDependencyProperty: a property registered by app code rather than by the framework.
	private static bool IsCustomProperty(DependencyProperty property)
		=> property.OwnerType.Assembly != typeof(Timeline).Assembly;

	/// <summary>
	/// The objects <paramref name="target"/> is associated with (CDependencyObject::GetParentCount / GetParentItem), in a
	/// pooled list to give back with <see cref="ReturnParentItems"/>.
	/// </summary>
	/// <remarks>
	/// TODO Uno: WinUI tracks every association of a shareable object. Uno parents only some property values, so the
	/// holders of brushes, transforms, geometries and projections come from <see cref="SharedParentAssociations"/>.
	/// The independence check runs on every tick, and recursively: the lists are pooled rather than allocated.
	/// </remarks>
	private static List<DependencyObject> RentParentItems(DependencyObject target)
	{
		var items = t_parentItemsPool is { Count: > 0 } pool ? pool.Pop() : new List<DependencyObject>();

		var parent = target.GetParent() as DependencyObject;
		if (parent is not null)
		{
			items.Add(parent);
		}

		SharedParentAssociations.AddParents(target, items, except: parent);
		return items;
	}

	private static void ReturnParentItems(List<DependencyObject> items)
	{
		items.Clear();
		(t_parentItemsPool ??= new Stack<List<DependencyObject>>()).Push(items);
	}

	[ThreadStatic]
	private static Stack<List<DependencyObject>>? t_parentItemsPool;

	private static int CountKeyFrames(IEnumerable<IKeyFrame> keyFrames)
	{
		var count = 0;
		foreach (var _ in keyFrames)
		{
			count++;
		}

		return count;
	}
}
