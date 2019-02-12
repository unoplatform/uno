using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleSwitch : Control, IFrameworkTemplatePoolAware
	{
		private readonly SerialDisposable _eventSubscriptions = new SerialDisposable();

		private Thumb _switchThumb;
		private FrameworkElement _switchKnob;
		private FrameworkElement _switchKnobBounds;
		private TranslateTransform _knobTranslateTransform;

		// If the user only drags the thumb by a few pixels before releasing it, 
		// we interpret it as a Tap rather than a drag gesture.
		// TODO: Implement and use the Tapped event instead?
		private const double TapDistanceThreshold = 4;
		private double _maxDragDistance = 0;
		private bool InterpretAsTap => _maxDragDistance < TapDistanceThreshold;

		public event RoutedEventHandler Toggled;

		public ToggleSwitch()
		{
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			AddHandler(PointerPressedEvent, (PointerEventHandler) OnPointerPressed, true);
			AddHandler(PointerExitedEvent, (PointerEventHandler) OnPointerExited, true);
			AddHandler(PointerReleasedEvent, (PointerEventHandler) OnPointerReleased, true);
			AddHandler(PointerCanceledEvent, (PointerEventHandler) OnPointerCanceled, true);
			AddHandler(PointerEnteredEvent, (PointerEventHandler) OnPointerEntered, true);

			OnLoadedPartial();
		}

		partial void OnLoadedPartial();

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			RemoveHandler(PointerPressedEvent, (PointerEventHandler) OnPointerPressed);
			RemoveHandler(PointerExitedEvent, (PointerEventHandler) OnPointerExited);
			RemoveHandler(PointerReleasedEvent, (PointerEventHandler) OnPointerReleased);
			RemoveHandler(PointerCanceledEvent, (PointerEventHandler) OnPointerCanceled);
			RemoveHandler(PointerEnteredEvent, (PointerEventHandler) OnPointerEntered);
		}

		private void OnPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);
			IsPointerOver = true;
			IsPointerPressed = true;
			args.Handled = true;
			Focus(FocusState.Pointer);
			UpdateCommonState();
		}

		private void OnPointerExited(object sender, PointerRoutedEventArgs args)
		{
			base.OnPointerExited(args);
			IsPointerOver = false;
			UpdateCommonState();
		}

		private void OnPointerReleased(object sender, PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);
			IsPointerOver = false;
			IsPointerPressed = false;
			UpdateCommonState();

			if (_switchThumb == null)
			{
				IsOn = !IsOn;
			}
		}

		private void OnPointerCanceled(object sender, PointerRoutedEventArgs args)
		{
			base.OnPointerCanceled(args);
			IsPointerOver = false;
			IsPointerPressed = false;
			UpdateCommonState();
		}

		private void OnPointerEntered(object sender, PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);
			IsPointerOver = true;
			UpdateCommonState();
		}

		public global::Windows.UI.Xaml.Controls.Primitives.ToggleSwitchTemplateSettings TemplateSettings { get; } = new ToggleSwitchTemplateSettings();

		public bool IsOn
		{
			get { return (bool)this.GetValue(IsOnProperty); }
			set { this.SetValue(IsOnProperty, value); }
		}

		public static readonly DependencyProperty IsOnProperty =
			DependencyProperty.Register("IsOn", typeof(bool), typeof(ToggleSwitch), new PropertyMetadata(false, propertyChangedCallback: (s, e) => ((ToggleSwitch)s).OnIsOnChanged(e)));

		public DataTemplate OnContentTemplate
		{
			get { return (DataTemplate)this.GetValue(OnContentTemplateProperty); }
			set { this.SetValue(OnContentTemplateProperty, value); }
		}

		public static readonly DependencyProperty OnContentTemplateProperty =
			DependencyProperty.Register("OnContentTemplate", typeof(DataTemplate), typeof(ToggleSwitch), new PropertyMetadata(null));

		public DataTemplate OffContentTemplate
		{
			get { return (DataTemplate)this.GetValue(OffContentTemplateProperty); }
			set { this.SetValue(OffContentTemplateProperty, value); }
		}

		public static readonly DependencyProperty OffContentTemplateProperty =
			DependencyProperty.Register("OffContentTemplate", typeof(DataTemplate), typeof(ToggleSwitch), new PropertyMetadata(null));

		public DataTemplate HeaderTemplate
		{
			get { return (DataTemplate)this.GetValue(HeaderTemplateProperty); }
			set { this.SetValue(HeaderTemplateProperty, value); }
		}

		public static readonly DependencyProperty HeaderTemplateProperty =
			DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(ToggleSwitch), new PropertyMetadata(null));

		public object OnContent
		{
			get { return (object)this.GetValue(OnContentProperty); }
			set { this.SetValue(OnContentProperty, value); }
		}

		public static readonly DependencyProperty OnContentProperty =
			DependencyProperty.Register("OnContent", typeof(object), typeof(ToggleSwitch), new PropertyMetadata(null));

		public object OffContent
		{
			get { return (object)this.GetValue(OffContentProperty); }
			set { this.SetValue(OffContentProperty, value); }
		}

		public static readonly DependencyProperty OffContentProperty =
			DependencyProperty.Register("OffContent", typeof(object), typeof(ToggleSwitch), new PropertyMetadata(null));

		public object Header
		{
			get { return (object)this.GetValue(HeaderProperty); }
			set { this.SetValue(HeaderProperty, value); }
		}

		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register("Header", typeof(object), typeof(ToggleSwitch), new PropertyMetadata(null));

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_switchThumb = GetTemplateChild("SwitchThumb") as Thumb;
			_switchKnob = GetTemplateChild("SwitchKnob") as FrameworkElement;
			_switchKnobBounds = GetTemplateChild("SwitchKnobBounds") as FrameworkElement;
			_knobTranslateTransform = GetTemplateChild("KnobTranslateTransform") as TranslateTransform;

			_eventSubscriptions.Disposable = RegisterHandlers();

			UpdateCommonState(false);
			UpdateToggleState(false);
			UpdateContentState(false);
		}

		private IDisposable RegisterHandlers()
		{
			// Setup the thumb's event listeners
			if (_switchThumb != null)
			{
				_switchThumb.DragStarted += OnDragStarted;
				_switchThumb.DragDelta += OnDragDelta;
				_switchThumb.DragCompleted += OnDragCompleted;
			}

			return Disposable.Create(() =>
			{
				// Dispose of the thumb's event listeners
				if (_switchThumb != null)
				{
					_switchThumb.DragStarted -= OnDragStarted;
					_switchThumb.DragDelta -= OnDragDelta;
					_switchThumb.DragCompleted -= OnDragCompleted;
				}
			});
		}

		private void OnIsOnChanged(DependencyPropertyChangedEventArgs e)
		{
			// On windows the event is raised first, then the ui is updated
			Toggled?.Invoke(this, new RoutedEventArgs());
			UpdateSwitchKnobPosition(0);
			UpdateToggleState();
			UpdateContentState();
		}

		private void OnDragStarted(object sender, DragStartedEventArgs e)
		{
			_maxDragDistance = 0;
			UpdateSwitchKnobPosition(e.HorizontalOffset);

			IsPointerPressed = true;
			IsPointerOver = true;
			UpdateCommonState();
			UpdateToggleState();
		}

		private void OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			var dragDistance = Math.Abs(e.HorizontalChange);
			_maxDragDistance = Math.Max(dragDistance, _maxDragDistance);
			UpdateSwitchKnobPosition(e.HorizontalChange);
		}

		private void OnDragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (InterpretAsTap)
			{
				IsOn = !IsOn;
			}
			else
			{
				IsOn = GetAbsoluteOffset(e.HorizontalChange) > (GetMaxOffset() / 2);
			}

			UpdateSwitchKnobPosition(0);

			IsPointerPressed = false;
			IsPointerOver = false;
			UpdateCommonState();
			UpdateToggleState();
		}

		private double GetMaxOffset()
		{
			if (_switchKnobBounds == null || _switchKnob == null)
			{
				return 0;
			}

			return _switchKnobBounds.ActualWidth - _switchKnob.ActualWidth;
		}

		private double GetAbsoluteOffset(double relativeOffset)
		{
			var minOffset = 0;
			var maxOffset = GetMaxOffset();
			var startOffset = IsOn ? maxOffset : 0;
			var absoluteOffset = startOffset + relativeOffset;
			absoluteOffset = Math.Max(minOffset, absoluteOffset);
			absoluteOffset = Math.Min(maxOffset, absoluteOffset);
			return absoluteOffset;
		}

		private void UpdateSwitchKnobPosition(double relativeOffset)
		{
			if (_knobTranslateTransform != null)
			{
				_knobTranslateTransform.X = GetAbsoluteOffset(relativeOffset);
			}
		}

		private void UpdateCommonState(bool useTransitions = true)
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", useTransitions);
			}
			else if (IsPointerPressed && IsPointerOver)
			{
				VisualStateManager.GoToState(this, "Pressed", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", useTransitions);
			}
		}

		private void UpdateToggleState(bool useTransitions = true)
		{
			if (_switchThumb?.IsDragging ?? false)
			{
				VisualStateManager.GoToState(this, "Dragging", useTransitions);
			}
			else if (IsOn)
			{
				VisualStateManager.GoToState(this, "On", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Off", useTransitions);
			}
		}

		private void UpdateContentState(bool useTransition = true)
		{
			if (IsOn)
			{
				VisualStateManager.GoToState(this, "OnContent", useTransition);
			}
			else
			{
				VisualStateManager.GoToState(this, "OffContent", useTransition);
			}
		}

		public void OnTemplateRecycled()
		{
			IsOn = false;
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ToggleSwitchAutomationPeer(this);
		}

		internal void AutomationPeerToggle()
		{
			IsOn = !IsOn;
		}
	}
}
