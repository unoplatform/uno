using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Uno.UI;

#if XAMARIN_ANDROID
#elif XAMARIN_IOS_UNIFIED
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name="Child")]
	public  partial class Viewbox : global::Windows.UI.Xaml.FrameworkElement
	{
		private UIElement _child;

		public  UIElement Child
		{
			get => _child;
			set
			{
				if(_child != value)
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
					availableSize.Width,
					availableSize.Height
				)
			);

			return new Size(
				measuredSize.Width,
				measuredSize.Height
			);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var child = this.FindFirstChild();

			if (child != null)
			{
				var finalRect = new Rect(
					0,
					0,
					finalSize.Width,
					finalSize.Height
				);

				base.ArrangeElement(child, finalRect);
			}

			return finalSize;
		}
	}
}
