#nullable enable

#if !UNO_REFERENCE_API
using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml;

internal partial interface ILayouterElement
{
	internal ILayouter Layouter { get; }

	internal Size LastAvailableSize { get; }

	internal bool IsMeasureDirty { get; }

	internal bool IsFirstMeasureDoneAndManagedElement { get; }

	internal bool IsMeasureDirtyPathDisabled { get; }
}

internal static class LayouterElementExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool DoMeasure(this ILayouterElement element, Size availableSize, out Size measuredSizeLogical)
	{
		var isFirstMeasure = !element.IsFirstMeasureDoneAndManagedElement;

		// "isDirty" here means this element's MeasureOverride
		// method NEEDS to be called.
		var isDirty =
			isFirstMeasure // first time here since attached to parent
			|| (availableSize != element.LastAvailableSize) // size changed
			|| element.IsMeasureDirty // .InvalidateMeasure() called
			|| !FeatureConfiguration.UIElement.UseInvalidateMeasurePath // dirty_path disabled globally
			|| element.IsMeasureDirtyPathDisabled; // dirty_path disabled locally

		var frameworkElement = element as FrameworkElement;
		if (frameworkElement is null) // native unmanaged element?
		{
			isDirty = true;
		}
		else if (!isDirty)
		{
			if (!frameworkElement.IsMeasureDirtyPath)
			{
				// That's a weird case, but we need to return something meaningful.
				measuredSizeLogical = frameworkElement.DesiredSize;
				return false;
			}
			if (element.GetParent() is not UIElement and not null)
			{
				// If the parent if this element is not managed (UIElement),
				// .MeasureOverride() needs to be called.
				isDirty = true;
			}
		}

		if (isFirstMeasure)
		{
			frameworkElement?.SetLayoutFlags(UIElement.LayoutFlag.FirstMeasureDone);
		}

		var remainingTries = UIElement.MaxLayoutIterations;
		measuredSizeLogical = default;

		while (--remainingTries > 0)
		{
			if (isDirty || frameworkElement is null)
			{
				// We must reset the flag **BEFORE** doing the actual measure, so the elements are able to re-invalidate themselves
				frameworkElement?.ClearLayoutFlags(UIElement.LayoutFlag.MeasureDirty);

				// The dirty flag is explicitly set on this element
				try
				{
					measuredSizeLogical = element.Layouter.Measure(availableSize);
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledExceptionOrLog(e, element);
					return false;
				}
				finally
				{
					LayoutInformation.SetAvailableSize(element, availableSize);
				}

				return true; // end of isDirty processing
			}

			// The measure dirty flag is set on one of the descendents:
			// it will bypass the current element's .MeasureOverride()
			// since it shouldn't produce a different result and it's
			// just a waste of precious CPU time to call it.
			using var children = frameworkElement.GetChildren().GetEnumerator();

			while (children.MoveNext())
			{
				var child = children.Current;
				// If the child is dirty (or is a path to a dirty descendant child),
				// We're remeasuring it.

				if (child is UIElement { IsMeasureDirtyOrMeasureDirtyPath: true })
				{
					var previousDesiredSize = LayoutInformation.GetDesiredSize(child);
					element.Layouter.MeasureChild(child, LayoutInformation.GetAvailableSize(child));
					var newDesiredSize = LayoutInformation.GetDesiredSize(child);
					if (newDesiredSize != previousDesiredSize)
					{
						isDirty = true;
						break;
					}
				}
				else if (child is not UIElement)
				{
					isDirty = true;
					break;
				}
			}

			if (!isDirty)
			{
				measuredSizeLogical = LayoutInformation.GetDesiredSize(element);
				return true; // end of DIRTY_PATH processing
			}

			// When the end of the loop is reached here, it means the
			// DIRTY_PATH process has been _upgraded_ to a standard _isDirty
			// process instead.
		}

		return false; // UIElement.MaxLayoutIterations reached. Maybe an exception should be raised instead.
	}
}
#endif
