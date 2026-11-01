#nullable enable

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
	/// The objects <paramref name="target"/> is associated with (CDependencyObject::GetParentCount / GetParentItem).
	/// </summary>
	/// <remarks>
	/// TODO Uno: WinUI tracks every association of a shareable object (a brush or transform used by several elements).
	/// Uno tracks a single parent.
	/// </remarks>
	private static IEnumerable<DependencyObject> GetParentItems(DependencyObject target)
	{
		if (target.GetParent() is DependencyObject parent)
		{
			yield return parent;
		}
	}

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
