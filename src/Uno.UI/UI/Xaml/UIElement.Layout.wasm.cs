using Windows.Foundation;
using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		private Size _size;
		private Rect _finalRect;
		private Size _desiredSize;
		private Size _previousAvailableSize;

		private bool _isMeasureValid = false;
		private bool _isArrangeValid = false;

		public Size DesiredSize => Visibility == Visibility.Collapsed ? new Size(0, 0) : _desiredSize;

		public void InvalidateMeasure()
		{
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
				this.Log().Warn($"{this} is not a FrameworkElement: Measure() is stopped here.");

				return;
			}

			var isCloseToPreviousMeasure = availableSize == _previousAvailableSize;

			if (Visibility == Visibility.Collapsed)
			{
				this.Log().LogTrace($"{this} Visibility is Collapsed.");

				if (!isCloseToPreviousMeasure)
				{
					this.Log().LogTrace($"{this} is Collapsed and measure size similar to previous one: We're terminating the measure phase here.");

					_isMeasureValid = false;
					_previousAvailableSize = availableSize;
				}

				return;
			}

			if (_isMeasureValid && isCloseToPreviousMeasure)
			{
				this.Log().LogTrace($"{this} measure size similar to previous one: We're terminating the measure phase here.");

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
				_finalRect = finalRect;
				return;
			}

			if (!_isArrangeValid || finalRect != _finalRect)
			{
				ArrangeCore(finalRect);
				_finalRect = finalRect;
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
			set
			{
				var previousSize = _size;
				_size = value;

				if (_size != previousSize)
				{
					if (this is FrameworkElement frameworkElement)
					{
						frameworkElement.ActualHeight = _size.Height;
						frameworkElement.ActualWidth = _size.Width;
						frameworkElement.RaiseSizeChanged(new SizeChangedEventArgs(previousSize, _size));
					}
				}
			}
		}
	}
}
