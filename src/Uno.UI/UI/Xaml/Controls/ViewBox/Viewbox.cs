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
		private const double SCALE_EPSILON = 0.00001d;

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
			if(finalSize.Width == 0 || finalSize.Height == 0)
			{
				return default;
			}

			var (scaleX, scaleY) = GetScale(finalSize, _container.DesiredSize);

			if (Math.Abs(scaleX - 1d) < SCALE_EPSILON && Math.Abs(scaleY - 1d) < SCALE_EPSILON)
			{
				_container.RenderTransform = null;
			}
			else
			{
				var transform = _container.RenderTransform as ScaleTransform ?? new ScaleTransform();
				transform.ScaleX = scaleX;
				transform.ScaleY = scaleY;
				_container.RenderTransform = transform;
			}

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
			if (double.IsNaN(uniformToFillScale) || double.IsInfinity(uniformToFillScale))
			{
				return 1;
			}

			switch (StretchDirection)
			{
				case StretchDirection.UpOnly:
					return Math.Max(1, uniformToFillScale);

				case StretchDirection.DownOnly:
					return Math.Min(1, uniformToFillScale);

				default:
				case StretchDirection.Both:
					return uniformToFillScale;
			}
		}
	}
}
