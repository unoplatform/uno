#if !__NETSTD_REFERENCE__
using Windows.Foundation;
using System;
using System.Diagnostics;
using Windows.UI.Xaml.Controls.Primitives;
using System.Runtime.CompilerServices;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		private Size _size;

		public Size DesiredSize => Visibility == Visibility.Collapsed ? new Size(0, 0) : ((IUIElement)this).DesiredSize;

		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a re-layout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }

		internal bool IsMeasureDirty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.MeasureDirty);
		}

		internal bool IsMeasureDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.MeasureDirtyPath);
		}

		internal bool IsMeasureOrMeasureDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsAnyLayoutFlagsSet(LayoutFlag.MeasureDirty | LayoutFlag.MeasureDirtyPath);
		}

		internal bool IsArrangeDirty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.ArrangeDirty);
		}

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
				InvalidateParentMeasurePath();
			}
			else
			{
				(this.GetParent() as UIElement)?.InvalidateMeasure();
				if (IsVisualTreeRoot)
				{
					Window.InvalidateMeasure();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateMeasurePath()
		{
			if(IsMeasureOrMeasureDirtyPath)
			{
				return; // Already invalidated
			}

			SetLayoutFlags(LayoutFlag.MeasureDirtyPath);

			InvalidateParentMeasurePath();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InvalidateParentMeasurePath()
		{
			if (this.GetParent() is UIElement parent)
			{
				parent.InvalidateMeasurePath();
			}
			else if (IsVisualTreeRoot)
			{
				Window.InvalidateMeasure();
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
			if (this.GetParent() is UIElement parent)
			{
				parent.InvalidateArrange();
			}
			else
			{
				Window.InvalidateArrange();
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

		private void DoMeasure(Size availableSize)
		{
			var isFirstMeasure = !IsLayoutFlagSet(LayoutFlag.FirstMeasureDone);

			var isDirty =
				(availableSize != LastAvailableSize)
				|| IsMeasureDirty
				|| isFirstMeasure;

			if (!isDirty && !IsMeasureDirtyPath)
			{
				return; // Nothing to do
			}

			if (isFirstMeasure)
			{
				SetLayoutFlags(LayoutFlag.FirstMeasureDone);
				isDirty = true;
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

				if (IsMeasureDirtyPath)
				{
					ClearLayoutFlags(LayoutFlag.MeasureDirtyPath);

					// The dirty flag is set on one of the descendents:
					// it will bypass the current element's MeasureOverride()
					// since it shouldn't produce a different result and it's
					// just a waste of precious CPU time to call it.
					var children = GetChildren().GetEnumerator();

					//foreach (var child in children)
					while(children.MoveNext())
					{
						var child = children.Current;
						// If the child is dirty (or is a path to a dirty descendant child),
						// We're remeasuring it.

						if (child.IsMeasureOrMeasureDirtyPath)
						{
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

			if (Visibility == Visibility.Collapsed
				// If the layout is clipped, and the arranged size is empty, we can skip arranging children
				// This scenario is particularly important for the Canvas which always sets its desired size
				// zero, even after measuring its children.
				|| (finalRect == default && (this is not ICustomClippingElement clipElement || clipElement.AllowClippingToLayoutSlot)))
			{
				LayoutInformation.SetLayoutSlot(this, finalRect);
				HideVisual();
				ClearLayoutFlags(LayoutFlag.ArrangeDirty);
				return;
			}

			if (!IsArrangeDirty && finalRect == LayoutSlot)
			{
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

		private void DoArrange(Rect finalRect)
		{
			ShowVisual();

			// We must store the updated slot before natively arranging the element,
			// so the updated value can be read by indirect code that is being invoked on arrange.
			// For instance, the EffectiveViewPort computation reads that value to detect slot changes (cf. PropagateEffectiveViewportChange)
			LayoutInformation.SetLayoutSlot(this, finalRect);

			// We must reset the flag **BEFORE** doing the actual arrange, so the elements are able to re-invalidate themselves
			ClearLayoutFlags(LayoutFlag.ArrangeDirty);

			ArrangeCore(finalRect);
		}

		partial void HideVisual();
		partial void ShowVisual();

		

		internal virtual void ArrangeCore(Rect finalRect)
		{
			throw new NotSupportedException("UIElement doesn't implement ArrangeCore. Inherit from FrameworkElement, which properly implements ArrangeCore.");
		}

		public Size RenderSize
		{
			get => Visibility == Visibility.Collapsed ? new Size() : _size;
			internal set
			{
				Debug.Assert(value.Width >= 0, "Invalid width");
				Debug.Assert(value.Height >= 0, "Invalid height");

				var previousSize = _size;
				_size = value;

				if (_size != previousSize)
				{
					if (this is FrameworkElement frameworkElement)
					{
						frameworkElement.SetActualSize(_size);
						frameworkElement.RaiseSizeChanged(new SizeChangedEventArgs(this, previousSize, _size));
					}
				}
			}
		}
	}
}
#endif
