// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ToggleSwitch_Partial.h, ToggleSwitch_Partial.cpp

#nullable enable

using System;
using DirectUI;
using Uno.Disposables;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleSwitch
	{
		private bool _isDragging;
		private bool _wasDragged;
		private bool _isPointerOver;

		private double _knobTranslation;
		private double _minKnobTranslation;
		private double _maxKnobTranslation;
		private double _curtainTranslation;
		private double _minCurtainTranslation;
		private double _maxCurtainTranslation;
		private bool _handledKeyDown;

		// The template parts.

		private UIElement? _tpCurtainClip;
		private FrameworkElement? _tpKnob;
		private FrameworkElement? _tpKnobBounds;
		private FrameworkElement? _tpCurtainBounds;
		private Thumb? _tpThumb;
		private UIElement? _tpHeaderPresenter;

		// The translate transforms from template parts.

		private TranslateTransform? _spKnobTransform;
		private TranslateTransform? _spCurtainTransform;

		private SerialDisposable _dragStarted = new SerialDisposable();
		private SerialDisposable _dragDelta = new SerialDisposable();
		private SerialDisposable _dragCompleted = new SerialDisposable();
		private SerialDisposable _tap = new SerialDisposable();
		private SerialDisposable _knobSizeChanged = new SerialDisposable();
		private SerialDisposable _knobBoundsSizeChanged = new SerialDisposable();

		private protected override void ChangeVisualState(bool useTransitions)
		{
			bool isOn = false;
			bool isEnabled = false;
			FocusState focusState = FocusState.Unfocused;

			base.ChangeVisualState(useTransitions);

			isEnabled = IsEnabled;
			focusState = FocusState;

			if (!isEnabled)
			{
				GoToState(useTransitions, "Disabled");
			}
			else if (_isDragging)
			{
				GoToState(useTransitions, "Pressed");
			}

			else if (_isPointerOver)
			{
				GoToState(useTransitions, "PointerOver");
			}
			else
			{
				GoToState(useTransitions, "Normal");
			}

			if (focusState != FocusState.Unfocused && isEnabled)
			{
				if (focusState == FocusState.Pointer)
				{
					GoToState(useTransitions, "PointerFocused");
				}
				else
				{
					GoToState(useTransitions, "Focused");
				}
			}
			else
			{
				GoToState(useTransitions, "Unfocused");
			}


			if (_isDragging)
			{
				GoToState(useTransitions, "Dragging");
			}
			else
			{
				isOn = IsOn;
				GoToState(useTransitions, isOn ? "On" : "Off");
				GoToState(useTransitions, isOn ? "OnContent" : "OffContent");
			}
		}

		protected override void OnApplyTemplate()
		{
			if (_tpThumb != null)
			{
				_dragStarted.Disposable = null;
				_dragDelta.Disposable = null;
				_dragCompleted.Disposable = null;
				_tap.Disposable = null;
			}

			if (_tpKnob != null)
			{
				_knobSizeChanged.Disposable = null;
			}

			if (_tpKnobBounds != null)
			{
				_knobBoundsSizeChanged.Disposable = null;
			}

			_tpCurtainBounds = null;
			_tpCurtainClip = null;
			_spCurtainTransform = null;
			_tpKnob = null;
			_tpKnobBounds = null;
			_spKnobTransform = null;
			_tpThumb = null;
			_tpHeaderPresenter = null;

			base.OnApplyTemplate();

			var spCurtainIDependencyObject = GetTemplateChild("SwitchCurtain");
			var spCurtainBoundsIDependencyObject = GetTemplateChild("SwitchCurtainBounds");
			var spCurtainClipIDependencyObject = GetTemplateChild("SwitchCurtainClip");
			var spKnobIDependencyObject = GetTemplateChild("SwitchKnob");
			var spKnobBoundsIDependencyObject = GetTemplateChild("SwitchKnobBounds");
			var spThumbIDependencyObject = GetTemplateChild("SwitchThumb");

			_tpCurtainBounds = spCurtainBoundsIDependencyObject as FrameworkElement;
			_tpCurtainClip = spCurtainClipIDependencyObject as UIElement;
			_tpKnob = spKnobIDependencyObject as FrameworkElement;
			_tpKnobBounds = spKnobBoundsIDependencyObject as FrameworkElement;
			_tpThumb = spThumbIDependencyObject as Thumb;

			var spThumbIUIElement = _tpThumb as UIElement;

			var spCurtainIUIElement = spCurtainIDependencyObject as UIElement;
			if (spCurtainIUIElement != null)
			{
				var spCurtainRenderTransform = spCurtainIUIElement.RenderTransform;
				_spCurtainTransform = spCurtainRenderTransform as TranslateTransform;
			}

			var spKnobIUIElement = spKnobIDependencyObject as UIElement;
			if (spKnobIUIElement != null)
			{
				Transform spKnobRenderTransform = spKnobIUIElement.RenderTransform;
				_spKnobTransform = spKnobRenderTransform as TranslateTransform;
			}

			if (spThumbIUIElement != null && _tpThumb != null)
			{
				_tpThumb.DragStarted += DragStartedHandler;
				_dragStarted.Disposable = Disposable.Create(() => _tpThumb.DragStarted -= DragStartedHandler);
				_tpThumb.DragDelta += DragDeltaHandler;
				_dragDelta.Disposable = Disposable.Create(() => _tpThumb.DragDelta -= DragDeltaHandler);
				_tpThumb.DragCompleted += DragCompletedHandler;
				_dragCompleted.Disposable = Disposable.Create(() => _tpThumb.DragCompleted -= DragCompletedHandler);
				spThumbIUIElement.Tapped += TapHandler;
				_tap.Disposable = Disposable.Create(() => spThumbIUIElement.Tapped -= TapHandler);
			}

			if (_tpKnob != null || _tpKnobBounds != null)
			{
				if (_tpKnob != null)
				{
					_tpKnob.SizeChanged += SizeChangedHandler;
					_knobSizeChanged.Disposable = Disposable.Create(() => _tpKnob.SizeChanged -= SizeChangedHandler);
				}

				if (_tpKnobBounds != null)
				{
					_tpKnobBounds.SizeChanged += SizeChangedHandler;
					_knobBoundsSizeChanged.Disposable = Disposable.Create(() => _tpKnobBounds.SizeChanged -= SizeChangedHandler);
				}
			}

			UpdateHeaderPresenterVisibility();

			UpdateVisualState(false);
		}

		private void PrepareState()
		{
			//TODO Uno specific: No base implementation exists yet.
			//base.PrepareState();

			TemplateSettings = new ToggleSwitchTemplateSettings();
		}

		/// <summary>
		/// Gives the default values for our properties.
		/// </summary>
		/// <param name="dependencyProperty"></param>
		/// <param name="value"></param>
		internal override bool GetDefaultValue2(DependencyProperty dependencyProperty, out object? value)
		{
			var core = DXamlCore.Current;
			if (dependencyProperty == OnContentProperty)
			{
				var onString = core.GetLocalizedResourceString("TEXT_TOGGLESWITCH_ON");
				value = onString;
				return true;
			}
			else if (dependencyProperty == OffContentProperty)
			{
				var offString = core.GetLocalizedResourceString("TEXT_TOGGLESWITCH_OFF");
				value = offString;
				return true;
			}

			return base.GetDefaultValue2(dependencyProperty, out value);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			bool hasAutomationListener = false;

			base.OnPropertyChanged2(args);

			if (args.Property == ToggleSwitch.IsOnProperty)
			{
				OnToggled();

				hasAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged);

				if (hasAutomationListener)
				{
					var spAutomationPeer = GetOrCreateAutomationPeer();
					if (spAutomationPeer != null)
					{
						if (spAutomationPeer is ToggleSwitchAutomationPeer spToggleSwitchAutomationPeer)
						{
							spToggleSwitchAutomationPeer.RaiseToggleStatePropertyChangedEvent(args.OldValue, args.NewValue);
						}
					}
				}
			}
			else if (args.Property == HeaderProperty)
			{
				UpdateHeaderPresenterVisibility();
				OnHeaderChanged(args.OldValue, args.NewValue);
			}
			else if (args.Property == HeaderTemplateProperty)
			{
				UpdateHeaderPresenterVisibility();
			}
			else if (args.Property == OffContentProperty)
			{
				OnOffContentChanged(args.OldValue, args.NewValue);
			}
			else if (args.Property == OnContentProperty)
			{
				OnOnContentChanged(args.OldValue, args.NewValue);
			}
			else if (args.Property == VisibilityProperty)
			{
				OnVisibilityChanged();
			}
		}

		private void GetTranslations()
		{
			if (_spKnobTransform != null)
			{
				_knobTranslation = _spKnobTransform.X;
			}

			if (_spCurtainTransform != null)
			{
				_curtainTranslation = _spCurtainTransform.X;
			}
		}

		private void SetTranslations()
		{
			double translation = 0;

			var pToggleSwitchTemplateSettingsNoRef = TemplateSettings;

			if (_spKnobTransform != null)
			{
				translation = Math.Min(_knobTranslation, _maxKnobTranslation);
				translation = Math.Max(translation, _minKnobTranslation);

				_spKnobTransform.X = translation;

				if (pToggleSwitchTemplateSettingsNoRef != null)
				{
					pToggleSwitchTemplateSettingsNoRef.KnobCurrentToOffOffset = translation - _minKnobTranslation;
					pToggleSwitchTemplateSettingsNoRef.KnobCurrentToOnOffset = translation - _maxKnobTranslation;
				}
			}

			if (_spCurtainTransform != null)
			{
				translation = Math.Min(_curtainTranslation, _maxCurtainTranslation);
				translation = Math.Max(translation, _minCurtainTranslation);

				_spCurtainTransform.X = translation;

				if (pToggleSwitchTemplateSettingsNoRef != null)
				{
					pToggleSwitchTemplateSettingsNoRef.CurtainCurrentToOffOffset = translation - _minCurtainTranslation;
					pToggleSwitchTemplateSettingsNoRef.CurtainCurrentToOnOffset = translation - _maxCurtainTranslation;
				}
			}
		}

		private void ClearTranslations()
		{
			if (_spKnobTransform != null)
			{
				_spKnobTransform.ClearValue(TranslateTransform.XProperty);
			}

			if (_spCurtainTransform != null)
			{
				_spCurtainTransform.ClearValue(TranslateTransform.XProperty);
			}
		}

		private void Toggle()
		{
			var isOn = IsOn;
			IsOn = !isOn;

			// Request a play invoke sound
			ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Invoke, this);
		}

#if false
		private void AutomationToggleSwitchOnToggle()
		{
			Toggle();
		}
#endif

		//private void
		//AutomationGetClickablePoint(
		//    _Out_ wf.Point* result)
		//{
		//	auto clickableElement =
		//		_tpThumb ?
		//		static_cast<UIElement*>(_tpThumb.Cast<Thumb>()) :
		//		static_cast<UIElement*>(this);

		//	XPOINTF point;
		//	static_cast<CUIElement*>(clickableElement.GetHandle()).GetClickablePointRasterizedClient(&point));

		//	*result = { point.x, point.y };

		//	return S_OK;
		//}

		protected override AutomationPeer OnCreateAutomationPeer() =>
			new ToggleSwitchAutomationPeer(this);

		private void MoveDelta(double translationDelta)
		{
			_curtainTranslation += translationDelta;
			_knobTranslation += translationDelta;

			SetTranslations();
		}

		private void MoveCompleted(bool wasMoved)
		{
			bool wasToggled = false;

			if (wasMoved)
			{
				double halfOfTranslationRange = (_maxKnobTranslation - _minKnobTranslation) / 2;

				var isOn = IsOn;
				wasToggled = isOn ? _knobTranslation <= halfOfTranslationRange : _knobTranslation >= halfOfTranslationRange;
			}

			ClearTranslations();

			if (wasToggled)
			{
				Toggle();
			}
			else
			{
				UpdateVisualState(true);
			}
		}

		protected virtual void OnToggled()
		{
			var args = new RoutedEventArgs();
			args.OriginalSource = this;

			// This workaround can be removed if pooling is removed. See https://github.com/unoplatform/uno/issues/12189
			if (!_suppressToggled) // Uno workaround.
			{
				Toggled?.Invoke(this, args);
			}

			if (!_isDragging)
			{
				UpdateVisualState(true);
			}
		}

		protected virtual void OnHeaderChanged(object oldContent, object newContent)
		{
		}

		protected virtual void OnOffContentChanged(object oldContent, object newContent)
		{
		}

		protected virtual void OnOnContentChanged(object oldContent, object newContent)
		{
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			FocusChanged();
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			FocusChanged();
		}

		private void FocusChanged() => UpdateVisualState(true);

		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);

			_isPointerOver = true;
			UpdateVisualState(true);
		}

		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			base.OnPointerExited(e);

			_isPointerOver = false;
			UpdateVisualState();
		}

		/// <inheritdoc />
		protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
		{
			/// We need to add this event handler because in the "Vertical Pan" case, the pointer would most likely
			/// move out of the control but OnPointerExit() would not be invoked so the control might remain in PointerOver
			/// visual state. This handler rectifies this issue by clearing the PointerOver state.
			base.OnPointerCaptureLost(e);

			//We are checking to make sure dragging has finished before resetting the PointerOver state,
			// because in the "Vertical Pan" case, we get a call to DragCompletedHandler() before
			// OnPointerCaptureLost() unlike the case of "Tap"/"Horizontal Drag" where Thumb.OnPointerReleased()
			// invokes ReleasePointerCapture() so OnPointerCaptureLost() is called before DragCompletedHandler().
			if (!_isDragging)
			{
				_isPointerOver = false;
			}
			UpdateVisualState();
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			// We don't perform any action in OnKeyDown because we wait for the key to be
			// released before performing a Toggle().  However, if we don't handle OnKeyDown,
			// it bubbles up to the parent ScrollViewer and may cause scrolling, which is
			// undesirable.  Therefore, we check to see if we will handle OnKeyUp for this key
			// press, and if so, we set Handled=true for OnKeyDown to stop bubbling this event.
			base.OnKeyDown(args);

			var isHandled = args.Handled;

			if (isHandled || _isDragging)
			{
				return;
			}

			var key = args.OriginalKey;
			isHandled = KeyProcess.KeyDown(key, this);

			args.Handled = isHandled;
			_handledKeyDown = isHandled;
		}

		protected override void OnKeyUp(KeyRoutedEventArgs args)
		{
			base.OnKeyUp(args);

			var key = args.OriginalKey;
			var isHandled = args.Handled;

			isHandled = KeyProcess.KeyUp(key, this);
			args.Handled = isHandled;
		}

		private void DragStartedHandler(object sender, DragStartedEventArgs args)
		{
			bool isFocused = false;

			_isDragging = true;
			_wasDragged = false;

			isFocused = Focus(FocusState.Pointer);
			GetTranslations();
			UpdateVisualState(true);
			SetTranslations();
		}

		private void DragDeltaHandler(object sender, DragDeltaEventArgs args)
		{
			var translation = args.HorizontalChange;

			// Allow Y movement, which prevents scrolling.
			if (translation != 0)
			{
				_wasDragged = true;
				MoveDelta(translation);
			}
		}

		private void DragCompletedHandler(object sender, DragCompletedEventArgs args)
		{
			var isCanceled = args.Canceled;

			if (isCanceled)
			{
				return;
			}

			_isDragging = false;

			MoveCompleted(_wasDragged);
		}

		private void TapHandler(object sender, TappedRoutedEventArgs args)
		{
			var isHandled = args.Handled;

			// TODO Uno specific: In our case, _isDragging is set to true
			// before tapped event (caused by Thumb source code).
			if (isHandled) //|| _isDragging)
			{
				return;
			}

			Toggle();

			args.Handled = true;
		}

		private void SizeChangedHandler(object sender, SizeChangedEventArgs args)
		{
			double curtainBoundsWidth = 0;
			double curtainBoundsHeight = 0;
			Rect clipRect = new Rect();

			// Set the clip.
			if (_tpCurtainBounds != null)
			{
				var spClipRectangleGeometry = new RectangleGeometry();

				curtainBoundsWidth = _tpCurtainBounds.ActualWidth;

				if (_tpCurtainClip != null)
				{
					curtainBoundsHeight = _tpCurtainBounds.ActualHeight;

					clipRect.X = clipRect.Y = 0;
					clipRect.Width = curtainBoundsWidth;
					clipRect.Height = curtainBoundsHeight;

					spClipRectangleGeometry.Rect = clipRect;
					_tpCurtainClip.Clip = spClipRectangleGeometry;
				}
			}

			var isOn = IsOn;

			// Compute the knob translation bounds.
			if (_tpKnob != null && _tpKnobBounds != null && _spKnobTransform != null)
			{
				var knobTranslation = _spKnobTransform.X;
				var knobBoundsWidth = _tpKnobBounds.ActualWidth;
				var knobWidth = _tpKnob.ActualWidth;
				var knobMarginThickness = _tpKnob.Margin;

				if (isOn)
				{
					_maxKnobTranslation = knobTranslation;
					_minKnobTranslation = _maxKnobTranslation - knobBoundsWidth + knobWidth;
				}
				else
				{
					_minKnobTranslation = knobTranslation;
					_maxKnobTranslation = _minKnobTranslation + knobBoundsWidth - knobWidth;
				}

				// Enable the negative margin effects used with the phone version.
				if (knobMarginThickness.Left < 0)
				{
					_maxKnobTranslation -= knobMarginThickness.Left;
				}

				if (knobMarginThickness.Right < 0)
				{
					_maxKnobTranslation -= knobMarginThickness.Right;
				}
			}

			// Compute the curtain translation bounds.
			if (_tpCurtainBounds != null && _spCurtainTransform != null)
			{
				var curtainTranslation = _spCurtainTransform.X;

				if (isOn)
				{
					_maxCurtainTranslation = curtainTranslation;
					_minCurtainTranslation = _maxCurtainTranslation - curtainBoundsWidth;
				}
				else
				{
					_minCurtainTranslation = curtainTranslation;
					_maxCurtainTranslation = _minCurtainTranslation + curtainBoundsWidth;
				}
			}

			// flow these values into interested parties
			var spTemplateSettings = TemplateSettings;
			if (spTemplateSettings is ToggleSwitchTemplateSettings pTemplateSettingsConcreteNoRef)
			{
				pTemplateSettingsConcreteNoRef.KnobOffToOnOffset = _minKnobTranslation - _maxKnobTranslation;
				pTemplateSettingsConcreteNoRef.KnobOnToOffOffset = _maxKnobTranslation - _minKnobTranslation;

				pTemplateSettingsConcreteNoRef.CurtainOffToOnOffset = _minCurtainTranslation - _maxCurtainTranslation;
				pTemplateSettingsConcreteNoRef.CurtainOnToOffOffset = _maxCurtainTranslation - _minCurtainTranslation;
			}
		}

		/// <summary>
		/// Whether the given key may cause the ToggleSwitch to toggle.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Handled?</returns>
		private bool HandlesKey(VirtualKey key) =>
			key == VirtualKey.Space ||
			key == VirtualKey.GamepadA;

		/// <summary>
		/// Called when the IsEnabled property changes.
		/// </summary>
		/// <param name="e">Event args.</param>
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			var isEnabled = IsEnabled;
			if (!isEnabled)
			{
				_isDragging = false;
				_isPointerOver = false;
			}
			UpdateVisualState();
		}

		/// <summary>
		/// Update the visual states when the Visibility property is changed.
		/// </summary>
		private void OnVisibilityChanged()
		{
			var visibility = Visibility;
			if (visibility != Visibility.Visible)
			{
				_isDragging = false;
				_isPointerOver = false;
			}

			UpdateVisualState();
		}

		/// <summary>
		/// Updates the visibility of the Header ContentPresenter
		/// </summary>
		private void UpdateHeaderPresenterVisibility()
		{
			var spHeaderTemplate = HeaderTemplate;
			var spHeader = Header;

			ConditionallyGetTemplatePartAndUpdateVisibility(
				"HeaderContentPresenter",
				(spHeader != null || spHeaderTemplate != null),
				ref _tpHeaderPresenter);
		}
	}
}
