#nullable enable
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

	internal bool IsFirstMeasureDone { get; }

	internal bool IsMeasureDirtyPathDisabled { get; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool DoMeasure(Size availableSize, out Size measuredSizeLogical)
	{
		var isFirstMeasure = !IsFirstMeasureDone;

		var isDirty =
			(availableSize != LastAvailableSize)
			|| IsMeasureDirty
			|| !FeatureConfiguration.UIElement.UseInvalidateMeasurePath
			|| isFirstMeasure
			|| IsMeasureDirtyPathDisabled;

		var frameworkElement = this as FrameworkElement;
		if (frameworkElement is null)
		{
			//measuredSizeLogical = LayoutInformation.GetDesiredSize(this);
			measuredSizeLogical = availableSize;
		}
		else if (!isDirty && !frameworkElement.IsMeasureDirtyPath)
		{
			measuredSizeLogical = frameworkElement.DesiredSize;
			return false;
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
					measuredSizeLogical = Layouter.Measure(availableSize);
					//frameworkElement.InvalidateArrange();
				}
				catch (Exception e)
				{
					Application.Current.RaiseRecoverableUnhandledExceptionOrLog(e, this);
					throw;
				}
				finally
				{
					LayoutInformation.SetAvailableSize(this, availableSize);
				}

				return true;
			}

			// The dirty flag is set on one of the descendents:
			// it will bypass the current element's MeasureOverride()
			// since it shouldn't produce a different result and it's
			// just a waste of precious CPU time to call it.
			using var children = frameworkElement.GetChildren().GetEnumerator();

			//foreach (var child in children)
			while (children.MoveNext())
			{
				var child = children.Current;
				// If the child is dirty (or is a path to a dirty descendant child),
				// We're remeasuring it.

				if (child is UIElement { IsMeasureOrMeasureDirtyPath: true } or { IsLayoutRequested: true })
				{
					var previousDesiredSize = LayoutInformation.GetDesiredSize(child);
					Layouter.MeasureChild(child, LayoutInformation.GetAvailableSize(child));
					var newDesiredSize = LayoutInformation.GetDesiredSize(child);
					if (newDesiredSize != previousDesiredSize)
					{
						isDirty = true;
						break;
					}
				}
			}

			if (!isDirty)
			{
				measuredSizeLogical = LayoutInformation.GetDesiredSize(this);
				break;
			}
		}

		return false;
	}
}
