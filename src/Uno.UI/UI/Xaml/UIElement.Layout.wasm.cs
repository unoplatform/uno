using Windows.Foundation;
using System;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		private Size _size;
		private Size _desiredSize;
		private Size _previousAvailableSize;

		private bool _isMeasureValid = false;
		private bool _isArrangeValid = false;

		public Size DesiredSize => Visibility == Visibility.Collapsed ? new Size(0, 0) : _desiredSize;

        /// <summary>
        /// Backing property for <see cref="Windows.UI.Xaml.Controls.Primitives.LayoutInformation.GetAvailableSize(UIElement)"/>
        /// </summary>
        internal Size LastAvailableSize => _previousAvailableSize;

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

			var isCloseToPreviousMeasure = availableSize == _previousAvailableSize;

			if (Visibility == Visibility.Collapsed)
			{
				if (!isCloseToPreviousMeasure)
				{
					_isMeasureValid = false;
					_previousAvailableSize = availableSize;
				}

				return;
			}

			if (_isMeasureValid && isCloseToPreviousMeasure)
			{
				return;
			}

			InvalidateArrange();

			var desiredSize = MeasureCore(availableSize);
			_previousAvailableSize = availableSize;
			_isMeasureValid = true;
			_desiredSize = desiredSize;
		}

		public void Arrange(Rect finalRect)
		{
			if (!(this is FrameworkElement))
			{
				return;
			}

			if (Visibility == Visibility.Collapsed)
			{
				LayoutSlot = finalRect;
				return;
			}

			if (!_isArrangeValid || finalRect != LayoutSlot)
			{
				ArrangeCore(finalRect);
				LayoutSlot = finalRect;
				_isArrangeValid = true;
			}
		}

		internal virtual Size MeasureCore(Size availableSize)
		{
			throw new NotSupportedException("UIElement doesn't implement MeasureCore. Inherit from FrameworkElement, which properly implements MeasureCore.");
		}

		internal virtual void ArrangeCore(Rect finalRect)
		{
			throw new NotSupportedException("UIElement doesn't implement ArrangeCore. Inherit from FrameworkElement, which properly implements ArrangeCore.");
		}

		public Size RenderSize
		{
			get => Visibility == Visibility.Collapsed ? new Size() : _size;
			internal set
			{
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
