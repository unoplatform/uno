#nullable enable

using System.Collections.Generic;
using Uno.UI.Media;

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
	/// Uno sets the parent for some properties only (not for RenderTransform, Projection or Clip), so the users of
	/// transforms, projections and geometries are found through the objects that listen to their changes.
	/// </remarks>
	private static IEnumerable<DependencyObject> GetParentItems(DependencyObject target)
	{
		var parent = target.GetParent() as DependencyObject;
		if (parent is not null)
		{
			yield return parent;
		}

		switch (target)
		{
			case Transform transform:
				foreach (var listener in transform.GetChangedListeners())
				{
					var user = listener.Target switch
					{
						NativeRenderTransformAdapter adapter => adapter.Owner as DependencyObject,
						DependencyObject dependencyObject => dependencyObject,
						_ => null,
					};

					if (user is not null && user != parent)
					{
						yield return user;
					}
				}

				break;

			case Projection { Owner: { } owner } when owner != parent:
				yield return owner;
				break;

			case Geometry geometry:
				foreach (var listener in geometry.GetGeometryChangedListeners())
				{
					if (listener.Target is DependencyObject user && user != parent)
					{
						yield return user;
					}
				}

				break;
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
