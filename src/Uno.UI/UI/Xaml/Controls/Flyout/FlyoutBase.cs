using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Uno.Disposables;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.ViewManagement;
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

		internal bool m_isPositionedAtPoint;

		protected internal Windows.UI.Xaml.Controls.Popup _popup;
		private bool _isLightDismissEnabled = true;
		private Point? _popupPositionInTarget;
		private readonly SerialDisposable _sizeChangedDisposable = new SerialDisposable();

		public FlyoutBase()
		{
		}

		private void EnsurePopupCreated()
		{
			if (_popup == null)
			{
				ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "FlyoutLightDismissOverlayBackground", isThemeResourceExtension: true);

				_popup = new Windows.UI.Xaml.Controls.Popup()
				{
					Child = CreatePresenter(),
					IsLightDismissEnabled = _isLightDismissEnabled,
				};

				SynchronizeTemplatedParent();

				_popup.Opened += OnPopupOpened;
				_popup.Closed += OnPopupClosed;

				_popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
				_popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));

				InitializePopupPanel();

				SynchronizeDataContext();
			}
		}

		/// <summary>
		/// Controls the appeareance of <see cref="MenuFlyout"/>, when true the native popups and appearance
		/// is used, otherwise the UWP appeareance is used. The default value is provided by <see cref="FeatureConfiguration.Style.UseUWPDefaultStyles"/>.
		/// </summary>
		public bool UseNativePopup { get; set; } = !FeatureConfiguration.Style.UseUWPDefaultStyles;

		protected virtual void InitializePopupPanel()
		{
			InitializePopupPanelPartial();
		}

		private protected bool IsLightDismissOverlayEnabled
		{
			get => _isLightDismissEnabled;
			set
			{
				_isLightDismissEnabled = value;

				if (_popup != null)
				{
					_popup.IsLightDismissEnabled = value;
				}
			}
		}

		partial void InitializePopupPanelPartial();

		private void OnPopupOpened(object sender, object e)
		{
			if (_popup.Child is FrameworkElement child)
			{
				SizeChangedEventHandler handler = (_, __) => SetPopupPositionPartial(Target, _popupPositionInTarget);

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

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get ; } =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(FlyoutBase), new FrameworkPropertyMetadata(null));

		public FrameworkElement Target { get; private set; }

		/// <summary>
		/// Defines an optional position of the popup in the <see cref="Target"/> element.
		/// </summary>
		internal Point? PopupPositionInTarget => _popupPositionInTarget;

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
				bool cancel = false;
				OnClosing(ref cancel);
				var closing = new FlyoutBaseClosingEventArgs();
				Closing?.Invoke(this, closing);
				if (cancel || closing.Cancel)
				{
					return;
				}
			}

			Close();
			_isOpen = false;
			OnClosed();
			Closed?.Invoke(this, EventArgs.Empty);
		}

		public void ShowAt(FrameworkElement placementTarget)
		{
			ShowAtCore(placementTarget, null);
		}

		public void ShowAt(DependencyObject placementTarget, FlyoutShowOptions showOptions)
		{
			if (placementTarget is FrameworkElement fe)
			{
				ShowAtCore(fe, showOptions);
			}
		}

		private protected virtual void ShowAtCore(FrameworkElement placementTarget, FlyoutShowOptions showOptions)
		{
			EnsurePopupCreated();

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

			if(showOptions != null)
			{
				_popupPositionInTarget = showOptions.Position;
			}

			OnOpening();
			Opening?.Invoke(this, EventArgs.Empty);
			Open();
			_isOpen = true;
			OnOpened();
			Opened?.Invoke(this, EventArgs.Empty);
		}

		private protected virtual void OnOpening() { }

		private protected virtual void OnClosing(ref bool cancel) { }

		private protected virtual void OnClosed() { }

		private protected virtual void OnOpened() { }

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
			if (_popup != null)
			{
				_popup.IsOpen = false; 
			}
		}

		protected internal virtual void Open()
		{
			SetPopupPositionPartial(Target, _popupPositionInTarget);

			_popup.IsOpen = true;
		}

		partial void SetPopupPositionPartial(UIElement placementTarget, Point? absolutePosition);

		partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			SynchronizeDataContext();
		}

		private void SynchronizeDataContext() =>
			// This is present to force the dataContext to be passed to the popup of the flyout since it is not directly a child in the visual tree of the flyout. 
			_popup?.SetValue(Popup.DataContextProperty, this.DataContext, precedence: DependencyPropertyValuePrecedences.Local);

		partial void OnTemplatedParentChangedPartial(DependencyPropertyChangedEventArgs e)
			=> SynchronizeTemplatedParent();

		private void SynchronizeTemplatedParent()
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

		internal static Rect CalculateAvailableWindowRect(bool isMenuFlyout, Controls.Popup popup, object placementTarget, bool hasTargetPosition, Point positionPoint, bool isFull)
		{
			// UNO TODO: UWP also uses values coming from the input pane and app bars, if any.
			// Make sure of migrate to XamlRoot: https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.xamlroot
			return ApplicationView.GetForCurrentView().VisibleBounds;
		}

		internal void SetPresenterStyle(
			Control pPresenter,
			Style pStyle)
		{
			Debug.Assert(pPresenter != null);

			if (pStyle != null)
			{
				pPresenter.Style = pStyle;
			}
			else
			{
				pPresenter.ClearValue(Control.StyleProperty);
			}
		}

		internal Control GetPresenter() => _popup?.Child as Control;
	}
}
