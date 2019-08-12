using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Uno.UI;
using Windows.UI.Xaml.Media;
using static Windows.UI.Xaml.Media.Stretch;
using System;

#if XAMARIN_ANDROID
#elif XAMARIN_IOS_UNIFIED
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Child")]
	public partial class Viewbox : global::Windows.UI.Xaml.FrameworkElement
	{
		private UIElement _child;

		public UIElement Child
		{
			get => _child;
			set
			{
				if (_child != value)
				{
					var previous = _child;
					_child = value;

					OnChildChangedPartial(previous, _child);
				}
			}
		}

		partial void OnChildChangedPartial(UIElement previousValue, UIElement newValue);

		protected override Size MeasureOverride(Size availableSize)
		{
			var measuredSize = base.MeasureOverride(
				new Size(
					double.PositiveInfinity,
					double.PositiveInfinity
				)
			);

			return new Size(
				Math.Min(availableSize.Width, measuredSize.Width),
				Math.Min(availableSize.Height, measuredSize.Height)
			);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (this.FindFirstChild() is UIElement child)
			{
				var finalRect = new Rect(
					0,
					0,
					finalSize.Width,
					finalSize.Height
				);

				(double scaleX, double scaleY) getScale()
				{
					switch (Stretch)
					{
						case Uniform:
							var uniformToFillScale = (child.DesiredSize.Width * finalSize.Height < child.DesiredSize.Height * finalSize.Width)
								? finalSize.Height / child.DesiredSize.Height // child is flatter than finalSize
								: finalSize.Width / child.DesiredSize.Width; // child is taller than finalSize

							uniformToFillScale = AdjustWithDirection(uniformToFillScale);

							return (uniformToFillScale, uniformToFillScale);

						case UniformToFill:
							var uniformScale = (child.DesiredSize.Width * finalSize.Height < child.DesiredSize.Height * finalSize.Width)
								? finalSize.Width / child.DesiredSize.Width // child is taller than finalSize
								: finalSize.Height / child.DesiredSize.Height; // child is flatter than finalSize

							uniformScale = AdjustWithDirection(uniformScale);

							return (uniformScale, uniformScale);

						case Fill:
							return (
								AdjustWithDirection(finalSize.Width / child.DesiredSize.Width),
								AdjustWithDirection(finalSize.Height / child.DesiredSize.Height)
							);

						case None:
						default:
							return (1, 1);
					}
				}

				var (scaleX, scaleY) = getScale();

				child.RenderTransform = new ScaleTransform()
				{
					ScaleX = scaleX,
					ScaleY = scaleY
				};

				base.ArrangeElement(child, new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
			}

			return finalSize;
		}

		private double AdjustWithDirection(double uniformToFillScale)
		{
			switch (StretchDirection)
			{
				case StretchDirection.UpOnly:
					uniformToFillScale = Math.Max(1, uniformToFillScale);
					break;

				case StretchDirection.DownOnly:
					uniformToFillScale = Math.Min(1, uniformToFillScale);
					break;

				default:
				case StretchDirection.Both:
					break;

			}

			return uniformToFillScale;
		}
	}
}
