using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

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

		protected internal Windows.UI.Xaml.Controls.Popup _popup;
		private readonly SerialDisposable _sizeChangedDisposable = new SerialDisposable();

		public FlyoutBase()
		{
			LightDismissOverlayBackground = Application.Current?.Resources["FlyoutLightDismissOverlayBackground"] as Brush ??
				// This is normally a no-op - the above line should retrieve the framework-level resource. This is purely to fail the build when
				// Resources/Styles are overhauled (and the above will no longer be valid)
				Uno.UI.GlobalStaticResources.FlyoutLightDismissOverlayBackground as Brush;

			_popup = new Windows.UI.Xaml.Controls.Popup()
			{
				Child = CreatePresenter(),
			};

			_popup.Opened += OnPopupOpened;
			_popup.Closed += OnPopupClosed;

			_popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
			_popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));

			InitializePopupPanel();
		}

		protected virtual void InitializePopupPanel()
		{
			InitializePopupPanelPartial();
		}

		partial void InitializePopupPanelPartial();

		private void OnPopupOpened(object sender, object e)
		{
			if (_popup.Child is FrameworkElement child)
			{
				SizeChangedEventHandler handler = (_, __) => SetPopupPositionPartial(Target);

				child.SizeChanged += handler;

				_sizeChangedDisposable.Disposable = Disposable
					.Create(() => child.SizeChanged -= handler);
			}
		}

		#region Placement

		/// <summary>
		/// Preferred placement of the flyout.
		/// </summary>
		/// <remarks>
		/// If there's not enough place, the following logic will be used:
		/// https://docs.microsoft.com/en-us/previous-versions/windows/apps/dn308515(v=win.10)#placing-a-flyout
		/// </remarks>
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

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode),
			typeof(FlyoutBase),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));


		/// <summary>
		/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc, static resource values.
		/// </summary>
		internal Brush LightDismissOverlayBackground
		{
			get { return (Brush)GetValue(LightDismissOverlayBackgroundProperty); }
			set { SetValue(LightDismissOverlayBackgroundProperty, value); }
		}

		internal static readonly DependencyProperty LightDismissOverlayBackgroundProperty =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(FlyoutBase), new PropertyMetadata(null));

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

		protected virtual Control CreatePresenter()
		{
			return null;
		}

		private void OnPopupClosed(object sender, object e)
		{
			Hide(canCancel: false);
			_sizeChangedDisposable.Disposable = null;
		}

		protected internal virtual void Close()
		{
			_popup.IsOpen = false;
		}

		protected internal virtual void Open()
		{
			SetPopupPositionPartial(Target);

			_popup.IsOpen = true;
		}

		partial void SetPopupPositionPartial(UIElement placementTarget);

		partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			// This is present to force the dataContext to be passed to the popup of the flyout since it is not directly a child in the visual tree of the flyout. 
			_popup?.SetValue(Popup.DataContextProperty, this.DataContext, precedence: DependencyPropertyValuePrecedences.Local);
		}

		partial void OnTemplatedParentChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			_popup?.SetValue(Popup.TemplatedParentProperty, TemplatedParent, precedence: DependencyPropertyValuePrecedences.Local);
		}

		public static FlyoutBase GetAttachedFlyout(FrameworkElement element)
		{
			return (FlyoutBase)element.GetValue(AttachedFlyoutProperty);
		}

		public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase value)
		{
			element.SetValue(AttachedFlyoutProperty, value);
		}

		public static void ShowAttachedFlyout(FrameworkElement flyoutOwner)
		{
			var flyout = GetAttachedFlyout(flyoutOwner);
			flyout?.ShowAt(flyoutOwner);
		}
	}
}
