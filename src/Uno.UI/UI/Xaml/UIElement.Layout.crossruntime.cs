#if !__NETSTD_REFERENCE__
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		public Size DesiredSize => Visibility == Visibility.Collapsed ? new Size(0, 0) : ((IUIElement)this).DesiredSize;

		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a re-layout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }

		public void InvalidateMeasure()
		{
			if (ShouldInterceptInvalidate)
			{
				return;
			}

			if (IsMeasureDirty)
			{
				return; // already dirty
			}

			SetLayoutFlags(LayoutFlag.MeasureDirty);

			if (FeatureConfiguration.UIElement.UseInvalidateMeasurePath && !IsMeasureDirtyPathDisabled)
			{
				InvalidateParentMeasureDirtyPath();
			}
			else
			{
				(this.GetParent() as UIElement)?.InvalidateMeasure();
				if (IsVisualTreeRoot)
				{
					XamlRoot.InvalidateMeasure();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateMeasureDirtyPath()
		{
			if (IsMeasureDirtyOrMeasureDirtyPath)
			{
				return; // Already invalidated
			}

			SetLayoutFlags(LayoutFlag.MeasureDirtyPath);

			InvalidateParentMeasureDirtyPath();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InvalidateParentMeasureDirtyPath()
		{
			if (this.GetParent() is UIElement parent) //TODO: Should this use VisualTree.GetParent as fallback? https://github.com/unoplatform/uno/issues/8978
			{
				parent.InvalidateMeasureDirtyPath();
			}
			else if (IsVisualTreeRoot)
			{
				XamlRoot.InvalidateMeasure();
			}
		}

		public void InvalidateArrange()
		{
			if (ShouldInterceptInvalidate)
			{
				return;
			}

			if (IsArrangeDirty)
			{
				return; // Already dirty
			}

			SetLayoutFlags(LayoutFlag.ArrangeDirty);

			if (FeatureConfiguration.UIElement.UseInvalidateArrangePath && !IsArrangeDirtyPathDisabled)
			{
				InvalidateParentArrangeDirtyPath();
			}
			else
			{
				(this.GetParent() as UIElement)?.InvalidateArrange();
				if (IsVisualTreeRoot)
				{
					XamlRoot.InvalidateArrange();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateArrangeDirtyPath()
		{
			if (IsArrangeDirtyOrArrangeDirtyPath)
			{
				return; // Already invalidated
			}

			SetLayoutFlags(LayoutFlag.ArrangeDirtyPath);

			InvalidateParentArrangeDirtyPath();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateParentArrangeDirtyPath()
		{
			if (this.GetParent() is UIElement parent) //TODO: Should this use VisualTree.GetParent as fallback? https://github.com/unoplatform/uno/issues/8978
			{
				parent.InvalidateArrangeDirtyPath();
			}
			else //TODO: Why not check IsVisualTreeRoot as in InvalidateParentMeasureDirtyPath?
			{
				XamlRoot?.InvalidateArrange();
			}
		}

		public void Measure(Size availableSize)
		{
			if (!_isFrameworkElement)
			{
				return; // Only FrameworkElements are measurable
			}

			if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
			{
				throw new InvalidOperationException($"Cannot measure [{GetType()}] with NaN");
			}

			if (Visibility == Visibility.Collapsed)
			{
				if (availableSize == LastAvailableSize)
				{
					return;
				}

				SetLayoutFlags(LayoutFlag.MeasureDirty);

				return;
			}

			if (IsVisualTreeRoot)
			{
				MeasureVisualTreeRoot(availableSize);
			}
			else
			{
				// If possible we avoid the try/finally which might be costly on some platforms
				DoMeasure(availableSize);
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MeasureVisualTreeRoot(Size availableSize)
		{
			try
			{
				_isLayoutingVisualTreeRoot = true;
				DoMeasure(availableSize);
			}
			finally
			{
				_isLayoutingVisualTreeRoot = false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DoMeasure(Size availableSize)
		{
			var isFirstMeasure = !IsLayoutFlagSet(LayoutFlag.FirstMeasureDone);

			var isDirty =
				isFirstMeasure
				|| (availableSize != LastAvailableSize)
				|| IsMeasureDirty
				|| !FeatureConfiguration.UIElement.UseInvalidateMeasurePath // dirty_path disabled globally
				|| IsMeasureDirtyPathDisabled;

			var isMeasureDirtyPath = IsMeasureDirtyPath;

			if (!isDirty && !isMeasureDirtyPath)
			{
				return; // Nothing to do
			}

			if (isFirstMeasure)
			{
				SetLayoutFlags(LayoutFlag.FirstMeasureDone);
			}

			var remainingTries = MaxLayoutIterations;

			while (--remainingTries > 0)
			{
				if (isDirty)
				{
					// We must reset the flag **BEFORE** doing the actual measure, so the elements are able to re-invalidate themselves
					ClearLayoutFlags(LayoutFlag.MeasureDirty | LayoutFlag.MeasureDirtyPath);

					// The dirty flag is explicitly set on this element
#if DEBUG
					try
#endif
					{
						MeasureCore(availableSize);
						InvalidateArrange();
					}
#if DEBUG
					catch (Exception ex)
					{
						_log.Error($"Error measuring {this}", ex);
						throw;
					}
					finally
#endif
					{
						LayoutInformation.SetAvailableSize(this, availableSize);
					}

					break;
				}

				// isMeasureDirtyPath is always true here
				ClearLayoutFlags(LayoutFlag.MeasureDirtyPath);

				// The dirty flag is set on one of the descendents:
				// it will bypass the current element's MeasureOverride()
				// since it shouldn't produce a different result and it's
				// just a waste of precious CPU time to call it.
				var children = GetChildren().GetEnumerator();

				//foreach (var child in children)
				while (children.MoveNext())
				{
					if (children.Current is { IsMeasureDirtyOrMeasureDirtyPath: true } child)
					{
						// If the child is dirty (or is a path to a dirty descendant child),
						// We're remeasuring it.

						var previousDesiredSize = child.DesiredSize;
						child.Measure(child.LastAvailableSize);
						if (child.DesiredSize != previousDesiredSize)
						{
							isDirty = true;
							break;
						}
					}
				}

				children.Dispose(); // no "using" operator here to prevent an implicit try-catch on Wasm

				if (isDirty)
				{
					continue;
				}

				break;
			}
		}

		internal virtual void MeasureCore(Size availableSize)
		{
			throw new NotSupportedException("UIElement doesn't implement MeasureCore. Inherit from FrameworkElement, which properly implements MeasureCore.");
		}

		public void Arrange(Rect finalRect)
		{
			if (!_isFrameworkElement)
			{
				return;
			}

			var firstArrangeDone = IsFirstArrangeDone;

			if (Visibility == Visibility.Collapsed)
			{
				LayoutInformation.SetLayoutSlot(this, finalRect);
				HideVisual();
				ClearLayoutFlags(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath);
				return;
			}

			if (firstArrangeDone && !IsArrangeDirtyOrArrangeDirtyPath && finalRect == LayoutSlot)
			{
				ClearLayoutFlags(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath);
				return; // Calling Arrange would be a waste of CPU time here.
			}

			if (IsVisualTreeRoot)
			{
				ArrangeVisualTreeRoot(finalRect);
			}
			else
			{
				// If possible we avoid the try/finally which might be costly on some platforms
				DoArrange(finalRect);
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ArrangeVisualTreeRoot(Rect finalRect)
		{
			try
			{
				_isLayoutingVisualTreeRoot = true;
				DoArrange(finalRect);
			}
			finally
			{
				_isLayoutingVisualTreeRoot = false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DoArrange(Rect finalRect)
		{
			var isFirstArrange = !IsLayoutFlagSet(LayoutFlag.FirstArrangeDone);

			var isDirty =
				isFirstArrange
				|| IsArrangeDirty
				|| finalRect != LayoutSlot;

			if (!isDirty && !IsArrangeDirtyPath)
			{
				return; // Nothing do to
			}

			if (GetUseLayoutRounding())
			{
				finalRect.X = LayoutRound(finalRect.X);
				finalRect.Y = LayoutRound(finalRect.Y);
				finalRect.Width = LayoutRound(finalRect.Width);
				finalRect.Height = LayoutRound(finalRect.Height);
			}

			var remainingTries = MaxLayoutIterations;

			while (--remainingTries > 0)
			{
				if (IsMeasureDirtyOrMeasureDirtyPath)
				{
					DoMeasure(LastAvailableSize);
				}

				if (isDirty)
				{
					ShowVisual();

					// We must store the updated slot before natively arranging the element,
					// so the updated value can be read by indirect code that is being invoked on arrange.
					// For instance, the EffectiveViewPort computation reads that value to detect slot changes (cf. PropagateEffectiveViewportChange)
					LayoutInformation.SetLayoutSlot(this, finalRect);

					// We must reset the flag **BEFORE** doing the actual arrange, so the elements are able to re-invalidate themselves
					ClearLayoutFlags(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath);

					ArrangeCore(finalRect);

					SetLayoutFlags(LayoutFlag.FirstArrangeDone);

					break;
				}
				else if (IsArrangeDirtyPath)
				{
					ClearLayoutFlags(LayoutFlag.ArrangeDirtyPath);

					var children = GetChildren().GetEnumerator();

					while (children.MoveNext())
					{
						var child = children.Current;

						if (child is { IsArrangeDirtyOrArrangeDirtyPath: true })
						{
							var previousRenderSize = child.RenderSize;
							child.Arrange(child.LayoutSlot);

							if (child.RenderSize != previousRenderSize)
							{
								isDirty = true;
								break;
							}
						}
					}

					children.Dispose(); // no "using" operator here to prevent an implicit try-catch on Wasm

					if (!isDirty)
					{
						break;
					}
				}
				else
				{
					break;
				}
			}

		}

		partial void HideVisual();
		partial void ShowVisual();

		internal virtual void ArrangeCore(Rect finalRect)
		{
			throw new NotSupportedException("UIElement doesn't implement ArrangeCore. Inherit from FrameworkElement, which properly implements ArrangeCore.");
		}
	}
}
#endif
