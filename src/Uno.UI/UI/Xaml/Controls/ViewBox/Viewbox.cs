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
		private readonly Border _container;

		public Viewbox()
		{
			HorizontalAlignment = HorizontalAlignment.Center;
			VerticalAlignment = VerticalAlignment.Center;

			_container = new Border();
			OnChildChangedPartial(null, _container);
		}

		public UIElement Child
		{
			get => _container.Child as UIElement;
			set => _container.Child = value;
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

			var (scaleX, scaleY) = GetScale(availableSize, measuredSize);

			return new Size(
				Math.Min(availableSize.Width, measuredSize.Width * scaleX),
				Math.Min(availableSize.Height, measuredSize.Height * scaleY)
			);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var (scaleX, scaleY) = GetScale(finalSize, _container.DesiredSize);

			_container.RenderTransform = new ScaleTransform()
			{
				ScaleX = scaleX,
				ScaleY = scaleY
			};

			base.ArrangeElement(_container, new Rect(new Point(), _container.DesiredSize));

			return finalSize;
		}

		// Internal for Unit Tests
		internal (double scaleX, double scaleY) GetScale(Size constraint, Size desired)
		{
			switch (Stretch)
			{
				case Uniform:
					var uniformToFillScale = (desired.Width * constraint.Height < desired.Height * constraint.Width)
						? constraint.Height / desired.Height // child is flatter than constraint
						: constraint.Width / desired.Width; // child is taller than constraint

					uniformToFillScale = AdjustWithDirection(uniformToFillScale);

					return (uniformToFillScale, uniformToFillScale);

				case UniformToFill:
					var uniformScale = (desired.Width * constraint.Height < desired.Height * constraint.Width)
						? constraint.Width / desired.Width // child is taller than constraint
						: constraint.Height / desired.Height; // child is flatter than constraint

					uniformScale = AdjustWithDirection(uniformScale);

					return (uniformScale, uniformScale);

				case Fill:
					return (
						AdjustWithDirection(constraint.Width / desired.Width),
						AdjustWithDirection(constraint.Height / desired.Height)
					);

				case None:
				default:
					return (1, 1);
			}
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
