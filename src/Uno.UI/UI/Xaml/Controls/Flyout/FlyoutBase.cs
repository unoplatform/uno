using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
#if XAMARIN_IOS
using View = UIKit.UIView;
#elif XAMARIN_ANDROID
using View = Android.Views.View;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase : DependencyObject
	{
		public event EventHandler Opened;
		public event EventHandler Closed;
		public event EventHandler Opening;
		public event TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> Closing;

		private bool _isOpen = false;

		public FlyoutBase()
		{
			InitializeBinder();
		}

		#region Placement

		public FlyoutPlacementMode Placement
		{
			get { return (FlyoutPlacementMode)GetValue(PlacementProperty); }
			set { SetValue(PlacementProperty, value); }
		}

		public static DependencyProperty PlacementProperty { get; } =
			DependencyProperty.Register(
				"Placement",
				typeof(FlyoutPlacementMode),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(default(FlyoutPlacementMode))
			);

		#endregion

		public FrameworkElement Target { get; private set; }

		public void Hide()
		{
			Hide(canCancel: true);
		}

		internal void Hide(bool canCancel)
		{
			if (!_isOpen)
			{
				return;
			}

			if (canCancel)
			{
				var closing = new FlyoutBaseClosingEventArgs();
				Closing?.Invoke(this, closing);
				if (closing.Cancel)
				{
					return;
				}
			}

			Close();
			_isOpen = false;
			Closed?.Invoke(this, EventArgs.Empty);
		}

		internal protected virtual void Close(){}

		public void ShowAt(FrameworkElement placementTarget)
		{
			if (_isOpen)
			{
				if (placementTarget == Target)
				{
					return;
				}
				else
				{
					// Close at previous placement target before opening at new one (without raising Closing)
					Hide(canCancel: false);
				}
			}

			Target = placementTarget;

			Opening?.Invoke(this, EventArgs.Empty);
			Open();
			_isOpen = true;
			Opened?.Invoke(this, EventArgs.Empty);
		}

		internal protected virtual void Open()
		{
		}

		protected virtual Control CreatePresenter()
		{
			return null;
		}
	}
}