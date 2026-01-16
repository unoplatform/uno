using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using System.Runtime.CompilerServices;

using Microsoft.UI.Dispatching;

#if __APPLE_UIKIT__
using View = UIKit.UIView;
#elif __ANDROID__
using View = Android.Views.View;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase : DependencyObject
	{
		public event EventHandler<object> Opened;
		public event EventHandler<object> Closed;
		public event EventHandler<object> Opening;
		public event TypedEventHandler<FlyoutBase, FlyoutBaseClosingEventArgs> Closing;

		private static readonly List<FlyoutBase> _openFlyouts = new List<FlyoutBase>();

		internal bool m_isPositionedAtPoint;

		private bool _isClosedPending;

		protected internal Popup _popup;
		private bool _isLightDismissEnabled = true;
		private readonly SerialDisposable _sizeChangedDisposable = new SerialDisposable();

		private bool m_hasPlacementOverride;
		private FlyoutPlacementMode m_placementOverride;

		private bool m_isTargetPositionSet;
		private Point m_targetPoint;

		internal bool IsTargetPositionSet => m_isTargetPositionSet;

		private bool m_isPositionedForDateTimePicker;

		private bool m_openingCanceled;

		[NotImplemented]
		private InputDeviceType m_inputDeviceTypeUsedToOpen;

		internal FlyoutPlacementMode EffectivePlacement => m_hasPlacementOverride ? m_placementOverride : Placement;

		protected FlyoutBase()
		{
		}

		internal static IReadOnlyList<FlyoutBase> OpenFlyouts => _openFlyouts.AsReadOnly();

		private void EnsurePopupCreated()
		{
			if (_popup == null)
			{
				ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "FlyoutLightDismissOverlayBackground", isThemeResourceExtension: true, isHotReloadSupported: true);

				var child = CreatePresenter();
				_popup = new Popup()
				{
					Child = child,
					IsLightDismissEnabled = _isLightDismissEnabled,
					AssociatedFlyout = this,
				};

				_popup.Opened += OnPopupOpened;
				_popup.Closed += OnPopupClosed;
				child.Loaded += OnPresenterLoaded;

				_popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));

				InitializePopupPanel();

				SynchronizePropertyToPopup(Popup.DataContextProperty, DataContext);
				SynchronizePropertyToPopup(Popup.AllowFocusOnInteractionProperty, AllowFocusOnInteraction);
				SynchronizePropertyToPopup(Popup.AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled);
			}
		}

		private void OnPresenterLoaded(object sender, RoutedEventArgs args)
		{
			var allowFocusOnInteraction = AllowFocusOnInteraction;
			DependencyObject target = this;

			if (allowFocusOnInteraction && Target is { } t)
			{
				allowFocusOnInteraction = t.AllowFocusOnInteraction;
				target = t;
			}

			var contentRoot = VisualTree.GetContentRootForElement(target);

			var focusState = contentRoot.FocusManager.GetRealFocusStateForFocusedElement();

			if (focusState != FocusState.Unfocused)
			{
				var presenter = GetPresenter();
				if (presenter.AllowFocusOnInteraction && _popup?.AssociatedFlyout.AllowFocusOnInteraction is true)
				{
					var childFocused = presenter.Focus(focusState);

					if (!childFocused)
					{
						_popup.Focus(focusState);
					}
				}
			}

			OnOpened();
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
				SizeChangedEventHandler handler = (_, __) => SetPopupPosition(Target, PopupPositionInTarget);

				child.SizeChanged += handler;

				_sizeChangedDisposable.Disposable = Disposable
					.Create(() => child.SizeChanged -= handler);
			}
		}

		public bool IsOpen
		{
			get => (bool)GetValue(IsOpenProperty);
			private set => SetValue(IsOpenProperty, value);
		}
		public static DependencyProperty IsOpenProperty { get; } =
			DependencyProperty.Register(
				nameof(IsOpen), typeof(bool),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(default(bool)));

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
		Microsoft.UI.Xaml.DependencyProperty.Register(
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

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get; } =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(FlyoutBase), new FrameworkPropertyMetadata(null, (s, e) => ((FlyoutBase)s).OnLightDismissOverlayBackgroundChanged(e)));

		private void OnLightDismissOverlayBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			if (_popup is Popup winUIPopup)
			{
				winUIPopup.LightDismissOverlayBackground = (Brush)e.NewValue;
			}
		}

		public DependencyObject OverlayInputPassThroughElement
		{
			get => (DependencyObject)GetValue(OverlayInputPassThroughElementProperty);
			set => SetValue(OverlayInputPassThroughElementProperty, value);
		}

		public static DependencyProperty OverlayInputPassThroughElementProperty { get; } =
			DependencyProperty.Register(nameof(OverlayInputPassThroughElement), typeof(DependencyObject), typeof(FlyoutBase), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		/// <summary>
		/// Gets or sets whether a disabled control can receive focus.
		/// </summary>
		public bool AllowFocusWhenDisabled
		{
			get => GetAllowFocusWhenDisabledValue();
			set => SetAllowFocusWhenDisabledValue(value);
		}

		/// <summary>
		/// Identifies the AllowFocusWhenDisabled  dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = false, Options = FrameworkPropertyMetadataOptions.Inherits, ChangedCallback = true)]
		public static DependencyProperty AllowFocusWhenDisabledProperty { get; } = CreateAllowFocusWhenDisabledProperty();

		private void OnAllowFocusWhenDisabledChanged(bool oldValue, bool newValue) =>
			SynchronizePropertyToPopup(Popup.AllowFocusWhenDisabledProperty, AllowFocusWhenDisabled);

		/// <summary>
		/// Gets or sets a value that indicates whether the element automatically gets focus when the user interacts with it.
		/// </summary>
		public bool AllowFocusOnInteraction
		{
			get => GetAllowFocusOnInteractionValue();
			set => SetAllowFocusOnInteractionValue(value);
		}

		/// <summary>
		/// Identifies for the AllowFocusOnInteraction dependency property.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true, Options = FrameworkPropertyMetadataOptions.Inherits, ChangedCallback = true)]
		public static DependencyProperty AllowFocusOnInteractionProperty { get; } = CreateAllowFocusOnInteractionProperty();

		/// <summary>
		/// Gets or sets a value that indicates how a flyout behaves when shown.
		/// </summary>
		public FlyoutShowMode ShowMode
		{
			get => (FlyoutShowMode)this.GetValue(ShowModeProperty);
			set => this.SetValue(ShowModeProperty, value);
		}

		/// <summary>
		/// Identifies the ShowMode dependency property.
		/// </summary>
		public static DependencyProperty ShowModeProperty { get; } =
			DependencyProperty.Register(
				nameof(ShowMode),
				typeof(FlyoutShowMode),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(FlyoutShowMode.Standard));

		private void OnAllowFocusOnInteractionChanged(bool oldValue, bool newValue) =>
			SynchronizePropertyToPopup(Popup.AllowFocusOnInteractionProperty, AllowFocusOnInteraction);

		public FrameworkElement Target { get; private set; }

		/// <summary>
		/// Defines an optional position of the popup in the <see cref="Target"/> element.
		/// </summary>
		internal Point? PopupPositionInTarget => m_isPositionedAtPoint ? m_targetPoint : default(Point?);

		public void Hide()
		{
			Hide(canCancel: true);
		}

		internal bool Hide(bool canCancel)
		{
			var cancel = false;
			if (canCancel)
			{
				OnClosing(ref cancel);
			}

			if (!cancel && canCancel)
			{
				var flyout = _openFlyouts.SkipWhile(f => f != this).Skip(1).FirstOrDefault();
				flyout?.Hide(true);
			}

			if (!cancel)
			{
				m_openingCanceled = true;

				if (_popup != null)
				{
					_popup.IsOpen = false;
				}
				IsOpen = false;

				OnClosed();

				RemoveFromOpenFlyouts();
			}

			return cancel;
		}

		private protected void RemoveFromOpenFlyouts()
		{
			if (_openFlyouts.Count > 0 && _openFlyouts[0] == this)
			{
				_openFlyouts.Remove(this);

				_isClosedPending = true;

				// TODO Uno: Closed should occur on PresenterUnloaded,
				// but that requires aligned loading/unloading lifecycle. #2895
				_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					Closed?.Invoke(this, EventArgs.Empty);
					_isClosedPending = false;

					if (_openFlyouts.Count > 0)
					{
						_openFlyouts[0].Hide();
					}
				});
			}
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

			m_hasPlacementOverride = false;

			if (_isClosedPending)
			{
				return;
			}

			if (IsOpen)
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
			XamlRoot = placementTarget?.XamlRoot;
			_popup.XamlRoot = XamlRoot;
			_popup.PlacementTarget = placementTarget;
			UpdatePopupPanelSizePartial();

			if (showOptions != null)
			{
				if (showOptions.Position is { } positionValue)
				{
					m_isPositionedAtPoint = true;

					if (placementTarget != null)
					{
						// Uno TODO: Calling TransformToVisual(null) on an element within the main visual tree will include the status bar height, which we don't
						// want because the status bar is otherwise excluded from layout calculations. We get the transform relative to the managed root view instead.
						UIElement reference =
#if __ANDROID__
							// TODO: Adjust for multiwindow #13827
							Window.CurrentSafe?.Content;
#else
							null;
#endif

						var transformToRoot = placementTarget.TransformToVisual(reference);
						positionValue = transformToRoot.TransformPoint(positionValue);
					}

					if (double.IsNaN(positionValue.X) || double.IsNaN(positionValue.Y))
					{
						throw new ArgumentException("Invalid flyout position");
					}

					var xamlRoot = XamlRoot ?? placementTarget?.XamlRoot;
					Rect visibleBounds = xamlRoot.VisualTree.VisibleBounds;
					positionValue = new Point(
						Math.Clamp(positionValue.X, visibleBounds.Left, visibleBounds.Right),
						Math.Clamp(positionValue.Y, visibleBounds.Top, visibleBounds.Bottom));

					SetTargetPosition(positionValue);
				}

				if (showOptions.Placement != FlyoutPlacementMode.Auto)
				{
					m_hasPlacementOverride = true;
					m_placementOverride = showOptions.Placement;
				}
			}

			_popup.DesiredPlacement = EffectivePlacement switch
			{
				FlyoutPlacementMode.Top => PopupPlacementMode.Top,
				FlyoutPlacementMode.Bottom => PopupPlacementMode.Bottom,
				FlyoutPlacementMode.Left => PopupPlacementMode.Left,
				FlyoutPlacementMode.Right => PopupPlacementMode.Right,
				FlyoutPlacementMode.TopEdgeAlignedLeft => PopupPlacementMode.TopEdgeAlignedLeft,
				FlyoutPlacementMode.TopEdgeAlignedRight => PopupPlacementMode.TopEdgeAlignedRight,
				FlyoutPlacementMode.BottomEdgeAlignedLeft => PopupPlacementMode.BottomEdgeAlignedLeft,
				FlyoutPlacementMode.BottomEdgeAlignedRight => PopupPlacementMode.BottomEdgeAlignedRight,
				FlyoutPlacementMode.LeftEdgeAlignedTop => PopupPlacementMode.LeftEdgeAlignedTop,
				FlyoutPlacementMode.LeftEdgeAlignedBottom => PopupPlacementMode.LeftEdgeAlignedBottom,
				FlyoutPlacementMode.RightEdgeAlignedTop => PopupPlacementMode.RightEdgeAlignedTop,
				FlyoutPlacementMode.RightEdgeAlignedBottom => PopupPlacementMode.RightEdgeAlignedBottom,
				_ => PopupPlacementMode.Auto,
			};

			ShowMode = showOptions?.ShowMode ?? FlyoutShowMode.Standard;

			if (ShowMode == FlyoutShowMode.Auto)
			{
				ShowMode = FlyoutShowMode.Standard;
			}

			OnOpening();

			if (m_openingCanceled)
			{
				return;
			}

			Open();
			IsOpen = true;

			// **************************************************************************************
			// UNO-FIX: Defer the raising of the Opened event to ensure everything is well
			// initialized before opening it.
			// **************************************************************************************
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Idle, () =>
			// **************************************************************************************
			{
				if (IsOpen)
				{
					OnOpened();
					Opened?.Invoke(this, EventArgs.Empty);
				}
			});
		}

		partial void UpdatePopupPanelSizePartial();

		private void SetTargetPosition(Point targetPoint)
		{
			m_isTargetPositionSet = true;
			m_targetPoint = targetPoint;
		}

		private void ApplyTargetPosition()
		{
			if (m_isTargetPositionSet && _popup != null)
			{
				_popup.HorizontalOffset = m_targetPoint.X;
				_popup.VerticalOffset = m_targetPoint.Y;
			}
		}

		private protected virtual void OnOpening()
		{
			m_openingCanceled = false;
			Opening?.Invoke(this, EventArgs.Empty);
		}

		private void OnClosing(ref bool cancel)
		{
			var closing = new FlyoutBaseClosingEventArgs();
			Closing?.Invoke(this, closing);
			cancel = closing.Cancel;
		}

		private protected virtual void OnClosed()
		{
			m_isTargetPositionSet = false;
		}

		private protected virtual void OnOpened() { }

		protected virtual Control CreatePresenter() => null;

		private void OnPopupClosed(object sender, object e)
		{
			Hide(canCancel: false);
			_sizeChangedDisposable.Disposable = null;
		}

		protected internal virtual void Close()
		{
			Hide(canCancel: true);
		}

		protected internal virtual void Open()
		{
			EnsurePopupCreated();

			SetPopupPosition(Target, PopupPositionInTarget);
			ApplyTargetPosition();

			if (XamlRoot is not null && _popup.XamlRoot is null)
			{
				_popup.XamlRoot = XamlRoot;
			}
			UpdatePopupPanelSizePartial();

			_popup.IsOpen = true;

			AddToOpenFlyouts();
		}

		private protected void AddToOpenFlyouts()
		{
			if (!_openFlyouts.Contains(this))
			{
				_openFlyouts.Add(this);
			}
		}

		private void SetPopupPosition(FrameworkElement placementTarget, Point? positionInTarget)
		{
			_popup.PlacementTarget = placementTarget;

			if (positionInTarget is Point position)
			{
				_popup.HorizontalOffset = position.X;
				_popup.VerticalOffset = position.Y;
			}
		}

		partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e) =>
			SynchronizePropertyToPopup(Popup.DataContextProperty, DataContext);

		private void SynchronizePropertyToPopup(DependencyProperty property, object value)
		{
			// This is present to force properties to be propagated to the popup of the flyout
			// since it is not directly a child in the visual tree of the flyout.
			_popup?.SetValue(property, value, precedence: DependencyPropertyValuePrecedences.Local);
		}

		public static FlyoutBase GetAttachedFlyout(FrameworkElement element)
		{
			return (FlyoutBase)element.GetValue(AttachedFlyoutProperty);
		}

		public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase value)
		{
			element.SetValue(AttachedFlyoutProperty, value);
		}

		public static DependencyProperty AttachedFlyoutProperty
		{
			[DynamicDependency(nameof(GetAttachedFlyout))]
			[DynamicDependency(nameof(SetAttachedFlyout))]
			get;
		} = DependencyProperty.RegisterAttached(
				"AttachedFlyout",
				typeof(FlyoutBase),
				typeof(FlyoutBase),
				new FrameworkPropertyMetadata(null));

		public static void ShowAttachedFlyout(FrameworkElement flyoutOwner)
		{
			var flyout = GetAttachedFlyout(flyoutOwner);

			flyout?.SetValue(
				FlyoutBase.DataContextProperty,
				flyoutOwner.DataContext,
				precedence: DependencyPropertyValuePrecedences.Inheritance
			);

			flyout?.ShowAt(flyoutOwner);
		}

		internal static Rect CalculateAvailableWindowRect(bool isMenuFlyout, Popup popup, object placementTarget, bool hasTargetPosition, Point positionPoint, bool isFull)
		{
			// UNO TODO: UWP also uses values coming from the input pane and app bars, if any.
			// Make sure of migrate to XamlRoot: https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.xamlroot
			var xamlRoot = popup.XamlRoot ?? popup.Child?.XamlRoot;
			return xamlRoot.VisualTree.VisibleBounds;
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

		internal virtual Control GetPresenter() => _popup?.Child as Control;

		internal Rect UpdateTargetPosition(Rect availableWindowRect, Size presenterSize, Rect presenterRect)
		{
			double horizontalOffset = 0.0;
			double verticalOffset = 0.0;
			double maxWidth = double.NaN;
			double maxHeight = double.NaN;
			FrameworkElement spPopupAsFE;
			FlowDirection flowDirection = FlowDirection.LeftToRight;
			//FlowDirection targetFlowDirection = FlowDirection.LeftToRight;
			bool isMenuFlyout = this is MenuFlyout;
			bool preferTopPlacement = false;

			Debug.Assert(_popup != null);
			Debug.Assert(m_isTargetPositionSet);

			horizontalOffset = m_targetPoint.X;
			verticalOffset = m_targetPoint.Y;

			FlyoutPlacementMode placementMode = EffectivePlacement;

			// We want to preserve existing MenuFlyout behavior - it will continue to ignore the Placement property.
			// We also don't want to adjust anything if we've been positioned for a DatePicker or TimePicker -
			// in those cases, we've already been put at exactly the position we want to be at.
			if (!isMenuFlyout && !m_isPositionedForDateTimePicker)
			{
				switch (placementMode)
				{
					case FlyoutPlacementMode.Top:
						horizontalOffset -= presenterSize.Width / 2;
						verticalOffset -= presenterSize.Height;
						break;
					case FlyoutPlacementMode.Bottom:
						horizontalOffset -= presenterSize.Width / 2;
						break;
					case FlyoutPlacementMode.Left:
						horizontalOffset -= presenterSize.Width;
						verticalOffset -= presenterSize.Height / 2;
						break;
					case FlyoutPlacementMode.Right:
						verticalOffset -= presenterSize.Height / 2;
						break;
					case FlyoutPlacementMode.TopEdgeAlignedLeft:
					case FlyoutPlacementMode.RightEdgeAlignedBottom:
						verticalOffset -= presenterSize.Height;
						break;
					case FlyoutPlacementMode.TopEdgeAlignedRight:
					case FlyoutPlacementMode.LeftEdgeAlignedBottom:
						horizontalOffset -= presenterSize.Width;
						verticalOffset -= presenterSize.Height;
						break;
					case FlyoutPlacementMode.BottomEdgeAlignedLeft:
					case FlyoutPlacementMode.RightEdgeAlignedTop:
						// Nothing changes in this case - we want the point to be the top-left corner of the flyout,
						// which it already is.
						break;
					case FlyoutPlacementMode.BottomEdgeAlignedRight:
					case FlyoutPlacementMode.LeftEdgeAlignedTop:
						horizontalOffset -= presenterSize.Width;
						break;
				}
			}

			preferTopPlacement = (m_inputDeviceTypeUsedToOpen == InputDeviceType.Touch) && isMenuFlyout;
			//bool useHandednessPlacement = (m_inputDeviceTypeUsedToOpen == DirectUI.InputDeviceType.Pen) && isMenuFlyout;
			//var useHandednessPlacement = false; // Uno TODO

			if (preferTopPlacement)
			{
				verticalOffset -= presenterSize.Height;
			}

			// Uno TODO: support ExclusionRect
			//// If the desired placement of the flyout is inside the exclusion area, we'll shift it in the direction of the placement direction
			//// so that it no longer is inside that area.
			//if (!RectUtil.AreDisjoint(m_exclusionRect, { (float)(horizontalOffset), (float)(verticalOffset), presenterSize.Width, presenterSize.Height }))
			//{
			//	FlyoutBase.MajorPlacementMode majorPlacementMode = preferTopPlacement
			//		? FlyoutBase.MajorPlacementMode.Top
			//		: GetMajorPlacementFromPlacement(placementMode);

			//	switch (majorPlacementMode)
			//	{
			//		case FlyoutBase.MajorPlacementMode.Top:
			//			verticalOffset = m_exclusionRect.Y - presenterSize.Height;
			//			break;
			//		case FlyoutBase.MajorPlacementMode.Bottom:
			//			verticalOffset = m_exclusionRect.Y + m_exclusionRect.Height;
			//			break;
			//		case FlyoutBase.MajorPlacementMode.Left:
			//			horizontalOffset = m_exclusionRect.X - presenterSize.Width;
			//			break;
			//		case FlyoutBase.MajorPlacementMode.Right:
			//			horizontalOffset = m_exclusionRect.X + m_exclusionRect.Width;
			//			break;
			//	}
			//}

			spPopupAsFE = _popup;
			//flowDirection = (spPopupAsFE.FlowDirection);
			//if (m_isPositionedAtPoint)
			//{
			//	targetFlowDirection = (m_tpPlacementTarget.FlowDirection);
			//	Debug.Assert(flowDirection == targetFlowDirection);
			//}

			//bool isRTL = (flowDirection == xaml.FlowDirection_RightToLeft);
			//bool shiftLeftForRightHandedness = useHandednessPlacement && (IsRightHandedHandedness() != isRTL);
			//if (shiftLeftForRightHandedness)
			//{
			//	if (!isRTL)
			//	{
			//		horizontalOffset -= presenterSize.Width;
			//	}
			//	else
			//	{
			//		horizontalOffset += presenterSize.Width;
			//	}
			//}

			// Get the current presenter max width/height
			maxWidth = (GetPresenter() as Control).MaxWidth;
			maxHeight = (GetPresenter() as Control).MaxHeight;

			// Uno TODO: windowed popup mode
			//// Set the target position to the out of Xaml window if it is a windowed Popup.
			//// Set the target position to the inner Xaml window position if it isn't.
			//if (IsWindowedPopup())
			//{
			//	wf.Point targetPoint = { (FLOAT)(horizontalOffset), (FLOAT)(verticalOffset) };
			//	wf.Rect availableMonitorRect = default;

			//	// Calculate the available monitor bounds to set the target position within the monitor bounds
			//	(DXamlCore.GetCurrent().CalculateAvailableMonitorRect(m_tpPopup as Popup, targetPoint, &availableMonitorRect));

			//	// Set the max width and height with the available monitor bounds
			//	(m_tpPresenter as Control.put_MaxWidth(
			//		double.IsNaN(maxWidth) ? availableMonitorRect.Width : Math.Min(maxWidth, availableMonitorRect.Width)));
			//	(m_tpPresenter as Control.put_MaxHeight(
			//		double.IsNaN(maxHeight) ? availableMonitorRect.Height : Math.Min(maxHeight, availableMonitorRect.Height)));

			//	// Adjust the target position if the current target is out of the monitor bounds
			//	if (flowDirection == FlowDirection.LeftToRight)
			//	{
			//		if (targetPoint.X + presenterSize.Width > (availableMonitorRect.X + availableMonitorRect.Width))
			//		{
			//			// Update the target horizontal position if the target is out of the available monitor.
			//			// If the presenter width is greater than the current target left point from the screen,
			//			// the menu target left position is set to the begin of the screen position.
			//			horizontalOffset -= Math.Min(
			//				presenterSize.Width,
			//				Math.Max(0, targetPoint.X - availableMonitorRect.X));
			//		}
			//	}
			//	else
			//	{
			//		if (targetPoint.X - availableMonitorRect.X < presenterSize.Width)
			//		{
			//			// Update the target horizontal position if the target is outside the available monitor
			//			// if the presenter width is greater than the current target right point from the screen,
			//			// the menu target left position is set to the end of the screen position.
			//			horizontalOffset += Math.Min(
			//				presenterSize.Width,
			//				Math.Max(0, availableMonitorRect.Width - targetPoint.X + availableMonitorRect.X));
			//		}
			//	}

			//	// If we couldn't actually fit to the left, flip back to show right.
			//	if (shiftLeftForRightHandedness)
			//	{
			//		if (!isRTL && targetPoint.X < availableMonitorRect.X)
			//		{
			//			horizontalOffset += presenterSize.Width;
			//			targetPoint.X += presenterSize.Width;
			//		}
			//		else if (isRTL && targetPoint.X + presenterSize.Width >= availableMonitorRect.Width)
			//		{
			//			horizontalOffset -= presenterSize.Width;
			//			targetPoint.X -= presenterSize.Width;
			//		}
			//	}

			//	if (preferTopPlacement && targetPoint.Y < availableMonitorRect.Y)
			//	{
			//		verticalOffset += presenterSize.Height;
			//		targetPoint.Y += presenterSize.Height;

			//		// Nudge down if necessary to avoid the exclusion rect
			//		if (!RectUtil.AreDisjoint(m_exclusionRect, { (float)(horizontalOffset), (float)(verticalOffset), presenterSize.Width, presenterSize.Height }))
			//         {
			//			verticalOffset = m_exclusionRect.Y + m_exclusionRect.Height;
			//		}
			//	}

			//	if (targetPoint.Y + presenterSize.Height > (availableMonitorRect.Y + availableMonitorRect.Height))
			//	{
			//		// Update the target vertical position if the target is out of the available monitor.
			//		// If the presenter height is greater than the current target top point from the screen,
			//		// the menu target top position is set to the begin of the screen position.
			//		if (verticalOffset > 0)
			//		{
			//			verticalOffset = verticalOffset - Math.Min(
			//				presenterSize.Height,
			//				Math.Max(0, targetPoint.Y - availableMonitorRect.Y));
			//		}
			//		else // if it spans two monitors, make it start at the second.
			//		{
			//			verticalOffset = 0;
			//		}
			//	}
			//	(m_tpPopup.HorizontalOffset = horizontalOffset);
			//	(m_tpPopup.VerticalOffset = verticalOffset);
			//}
			//else
			{
				// Uno TODO: currently the Flyout layout calculations are done from the popup panel's ArrangeOverride(), which is too late to be setting MaxWidth/MaxHeight.
				//// Set the max width and height with the available windows bounds
				//(m_tpPresenter as Control.put_MaxWidth(
				//	double.IsNaN(maxWidth) ? availableWindowRect.Width : Math.Min(maxWidth, availableWindowRect.Width)));
				//(m_tpPresenter as Control.put_MaxHeight(
				//	double.IsNaN(maxHeight) ? availableWindowRect.Height : Math.Min(maxHeight, availableWindowRect.Height)));

				if (flowDirection == FlowDirection.LeftToRight)
				{
					// Adjust the target position if the current target is out of the Xaml window bounds
					if (horizontalOffset + presenterSize.Width > availableWindowRect.X + availableWindowRect.Width)
					{
						if (m_isPositionedAtPoint)
						{
							// Update the target horizontal position if the target is out of the available rect
							horizontalOffset -= Math.Min(presenterSize.Width, horizontalOffset);
						}
						else
						{
							// Used for date and time picker flyouts
							horizontalOffset = availableWindowRect.X + availableWindowRect.Width - presenterSize.Width;
							horizontalOffset = Math.Max(availableWindowRect.X, horizontalOffset);
						}
					}
				}
				else
				{
					// Adjust the target position if the current target is out of the Xaml window bounds
					if (horizontalOffset - presenterSize.Width < availableWindowRect.X)
					{
						if (m_isPositionedAtPoint)
						{
							// Update the target horizontal position if the target is out of the available rect
							horizontalOffset += Math.Min(presenterSize.Width, (availableWindowRect.Width + availableWindowRect.X - horizontalOffset));
						}
						else
						{
							// Used for date and time picker flyouts
							horizontalOffset = presenterSize.Width + availableWindowRect.X;
							horizontalOffset = Math.Min(availableWindowRect.Width + availableWindowRect.X, horizontalOffset);
						}
					}
				}

				//// If we couldn't actually fit to the left, flip back to show right.
				//if (shiftLeftForRightHandedness)
				//{
				//	if (!isRTL && horizontalOffset < availableWindowRect.X)
				//	{
				//		horizontalOffset += presenterSize.Width;
				//	}
				//	else if (isRTL && horizontalOffset + presenterSize.Width >= availableWindowRect.Width)
				//	{
				//		horizontalOffset -= presenterSize.Width;
				//	}
				//}

				// If opening up would cause the flyout to get clipped, we fall back to opening down:
				if (preferTopPlacement && verticalOffset < availableWindowRect.Y)
				{
					verticalOffset += presenterSize.Height;

					//// Nudge down if necessary to avoid the exclusion rect
					//if (!RectUtil.AreDisjoint(m_exclusionRect, { (float)(horizontalOffset), (float)(verticalOffset), presenterSize.Width, presenterSize.Height }))
					//       {
					//	verticalOffset = m_exclusionRect.Y + m_exclusionRect.Height;
					//}
				}

				if (verticalOffset + presenterSize.Height > availableWindowRect.Y + availableWindowRect.Height)
				{
					// Update the target vertical position if the target is out of the available rect
					if (m_isPositionedAtPoint)
					{
						verticalOffset -= Math.Min(presenterSize.Height, verticalOffset);
					}
					else
					{
						verticalOffset = availableWindowRect.Y + availableWindowRect.Height - presenterSize.Height;
					}
				}

				verticalOffset = Math.Max(availableWindowRect.Y, verticalOffset);
				// Uno TODO: scrap PlacementPopupPanel and rely on setting Popup.HOffset/VOffset
				//m_tpPopup.HorizontalOffset = horizontalOffset;
				//m_tpPopup.VerticalOffset = verticalOffset;
			}

			double leftMostEdge = (flowDirection == FlowDirection.LeftToRight) ? horizontalOffset : horizontalOffset - presenterSize.Width;

			presenterRect.X = leftMostEdge;
			presenterRect.Y = verticalOffset;
			presenterRect.Width = presenterSize.Width;
			presenterRect.Height = presenterSize.Height;

			return presenterRect;
		}

		internal static PreferredJustification GetJustificationFromPlacementMode(PopupPlacementMode placement, bool fullPlacementRequested)
		{
			if (fullPlacementRequested)
			{
				return PreferredJustification.Center;
			}

			switch (placement)
			{
				case PopupPlacementMode.Top:
				case PopupPlacementMode.Bottom:
				case PopupPlacementMode.Left:
				case PopupPlacementMode.Right:
					return PreferredJustification.Center;
				case PopupPlacementMode.TopEdgeAlignedLeft:
				case PopupPlacementMode.BottomEdgeAlignedLeft:
					return PreferredJustification.Left;
				case PopupPlacementMode.TopEdgeAlignedRight:
				case PopupPlacementMode.BottomEdgeAlignedRight:
					return PreferredJustification.Right;
				case PopupPlacementMode.LeftEdgeAlignedTop:
				case PopupPlacementMode.RightEdgeAlignedTop:
					return PreferredJustification.Top;
				case PopupPlacementMode.LeftEdgeAlignedBottom:
				case PopupPlacementMode.RightEdgeAlignedBottom:
					return PreferredJustification.Bottom;
				default:
					if (typeof(FlyoutBase).Log().IsEnabled(LogLevel.Error))
					{
						typeof(FlyoutBase).Log().LogError("Unsupported PopupPlacementMode");
					}
					return PreferredJustification.Center;
			}
		}

		internal static MajorPlacementMode GetMajorPlacementFromPlacement(PopupPlacementMode placement, bool fullPlacementRequested)
		{
			if (fullPlacementRequested)
			{
				return MajorPlacementMode.Full;
			}

			switch (placement)
			{
				case PopupPlacementMode.Top:
				case PopupPlacementMode.TopEdgeAlignedLeft:
				case PopupPlacementMode.TopEdgeAlignedRight:
					return MajorPlacementMode.Top;
				case PopupPlacementMode.Bottom:
				case PopupPlacementMode.BottomEdgeAlignedLeft:
				case PopupPlacementMode.BottomEdgeAlignedRight:
					return MajorPlacementMode.Bottom;
				case PopupPlacementMode.Left:
				case PopupPlacementMode.LeftEdgeAlignedTop:
				case PopupPlacementMode.LeftEdgeAlignedBottom:
					return MajorPlacementMode.Left;
				case PopupPlacementMode.Right:
				case PopupPlacementMode.RightEdgeAlignedTop:
				case PopupPlacementMode.RightEdgeAlignedBottom:
					return MajorPlacementMode.Right;
				default:
					if (typeof(FlyoutBase).Log().IsEnabled(LogLevel.Error))
					{
						typeof(FlyoutBase).Log().LogError("Unsupported PopupPlacementMode");
					}
					return MajorPlacementMode.Full;
			}
		}
	}
}
