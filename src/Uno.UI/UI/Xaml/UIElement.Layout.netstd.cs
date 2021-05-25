#if !__NETSTD_REFERENCE__
using Windows.Foundation;
using System;
using System.Diagnostics;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		private Size _size;

		private bool _isMeasureValid = false;
		private bool _isArrangeValid = false;

		public Size DesiredSize => Visibility == Visibility.Collapsed ? new Size(0, 0) : ((IUIElement)this).DesiredSize;

		internal bool IsMeasureDirty => !_isMeasureValid;
		internal bool IsArrangeDirty => !_isArrangeValid;

		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a relayout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }

		public void InvalidateMeasure()
		{
			if (ShouldInterceptInvalidate)
			{
				return;
			}

			// TODO: Figure out why this condition breaks layouting in some cases
			//if (_isMeasureValid)
			{
				_isMeasureValid = false;
				_isArrangeValid = false;
				if (this.GetParent() is UIElement parent)
				{
					parent.InvalidateMeasure();
				}
				else
				{
					Window.InvalidateMeasure();
				}
			}
		}

		public void InvalidateArrange()
		{
			if (ShouldInterceptInvalidate)
			{
				return;
			}

			if (_isArrangeValid)
			{
				_isArrangeValid = false;
				if (this.GetParent() is UIElement parent)
				{
					parent.InvalidateArrange();
				}
				else
				{
					Window.InvalidateMeasure();
				}
			}
		}

		public void Measure(Size availableSize)
		{
			if (!(this is FrameworkElement))
			{
				return;
			}

			if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
			{
				throw new InvalidOperationException($"Cannot measure [{GetType()}] with NaN");
			}

			var isCloseToPreviousMeasure = availableSize == LastAvailableSize;

			if (Visibility == Visibility.Collapsed)
			{
				if (!isCloseToPreviousMeasure)
				{
					_isMeasureValid = false;
					LayoutInformation.SetAvailableSize(this, availableSize);
				}

				return;
			}

			if (_isMeasureValid && isCloseToPreviousMeasure)
			{
				return;
			}

			if (IsVisualTreeRoot)
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
			else
			{
				// If possible we avoid the try/finally which might be costly on some platforms
				DoMeasure(availableSize);
			}
		}

		private void DoMeasure(Size availableSize)
		{
			InvalidateArrange();

			MeasureCore(availableSize);
			LayoutInformation.SetAvailableSize(this, availableSize);
			_isMeasureValid = true;
		}

		internal virtual void MeasureCore(Size availableSize)
		{
			throw new NotSupportedException("UIElement doesn't implement MeasureCore. Inherit from FrameworkElement, which properly implements MeasureCore.");
		}

		public void Arrange(Rect finalRect)
		{
			if (!(this is FrameworkElement))
			{
				return;
			}

			if (Visibility == Visibility.Collapsed || finalRect == default)
			{
				LayoutInformation.SetLayoutSlot(this, finalRect);
				HideVisual();
				_isArrangeValid = true;
				return;
			}

			if (_isArrangeValid && finalRect == LayoutSlot)
			{
				return;
			}

			if (IsVisualTreeRoot)
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
			else
			{
				// If possible we avoid the try/finally which might be costly on some platforms
				DoArrange(finalRect);
			}
		}

		private void DoArrange(Rect finalRect)
		{
			ShowVisual();

			// We must store the updated slot before natively arranging the element,
			// so the updated value can be read by indirect code that is being invoked on arrange.
			// For instance, the EffectiveViewPort computation reads that value to detect slot changes (cf. PropagateEffectiveViewportChange)
			LayoutInformation.SetLayoutSlot(this, finalRect);

			ArrangeCore(finalRect);
			_isArrangeValid = true;
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
