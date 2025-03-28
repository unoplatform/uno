// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference Expander.cpp, tag winui3/release/1.4.2

using System.Numerics;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class Expander : ContentControl
{
	private const string c_expanderHeader = "ExpanderHeader";
	private const string c_expanderContent = "ExpanderContent";
	private const string c_expanderContentClip = "ExpanderContentClip";

	// Uno specific: Added to dispose event handlers
	private readonly SerialDisposable _eventSubscriptions = new SerialDisposable();

	public Expander()
	{
		DefaultStyleKey = typeof(AnimatedIcon);

		SetValue(TemplateSettingsProperty, new ExpanderTemplateSettings());

		// Uno specific: we might leak on iOS: https://github.com/unoplatform/uno/pull/14257#discussion_r1381585268
		Loaded += Expander_Loaded;
		Unloaded += Expander_Unloaded;
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ExpanderAutomationPeer(this);

	protected override void OnApplyTemplate()
	{
		if (GetTemplateChild<Control>(c_expanderHeader) is Control toggleButton)
		{
			// We will do 2 things with the toggle button's peer:
			// 1. Set the events source of the toggle button peer to
			// the expander's automation peer. This is is because we
			// don't want to announce the toggle button's on/off property
			// changes, but the expander's expander/collapse property changes
			// (or on the events source that's set, if it's set) and
			//
			// 2. Set the expander's automation properties name to the
			// toggleButton's in case the expander doesn't have one. This just follows
			// what WPF does.
			if (FrameworkElementAutomationPeer.FromElement(toggleButton) is AutomationPeer toggleButtonPeer)
			{
				// 1. Set the events source of the toggle button peer to the expander's.
				if (FrameworkElementAutomationPeer.FromElement(this) is AutomationPeer expanderPeer)
				{
					// Uno Doc: EventSource is not implemented in the Uno Platform
					var expanderEventsSource = expanderPeer.EventsSource != null ?
						expanderPeer.EventsSource :
						expanderPeer;
					toggleButtonPeer.EventsSource = expanderEventsSource;
				}

				// 2. If the expander doesn't have any AutomationProperties.Name set,
				// we will try setting one based on the header. This is how
				// WPF's expanders work.
				if (string.IsNullOrEmpty(AutomationProperties.GetName(this))
					&& !string.IsNullOrEmpty(toggleButtonPeer.GetName()))
				{
					AutomationProperties.SetName(this, toggleButtonPeer.GetName());
				}
			}
		}

		// Uno specific: Added to dispose event handlers
		_eventSubscriptions.Disposable = null;
		var registrations = new CompositeDisposable();
		_eventSubscriptions.Disposable = registrations;

		if (GetTemplateChild<Border>(c_expanderContentClip) is Border expanderContentClip)
		{
			// TODO Uno specific: The Composition clipping APIs are currently unsupported,
			// so UIElement.Clip a layout slot for the Expander content when the
			// SizeChanged event is fired.
#if HAS_UNO
			expanderContentClip.SizeChanged += OnContentClipSizeChanged;
			registrations.Add(() => expanderContentClip.SizeChanged -= OnContentClipSizeChanged);
#else
			var visual = ElementCompositionPreview.GetElementVisual(expanderContentClip);
			visual.Clip = visual.Compositor.CreateInsetClip();
#endif
		}

		if (GetTemplateChild<Border>(c_expanderContent) is Border expanderContent)
		{
			expanderContent.SizeChanged += OnContentSizeChanged;
			registrations.Add(() => expanderContent.SizeChanged -= OnContentSizeChanged);
		}

		UpdateExpandState(false);
		UpdateExpandDirection(false);
	}

#if HAS_UNO
	private void OnContentClipSizeChanged(object sender, SizeChangedEventArgs args)
	{
		// TODO Uno specific: LayoutInformation.GetLayoutSlot(element) is currently inconsistent
		// on some platforms, so construct the Rect from ActualSize. A non-zero Point
		// is not required for this case.
		var expanderContentClip = sender as Border;
		var layoutSlot = new Rect(new Point(0, 0), expanderContentClip.ActualSize.ToSize());
		expanderContentClip.Clip = new RectangleGeometry() { Rect = layoutSlot };
	}
#endif

	private void OnContentSizeChanged(object sender, SizeChangedEventArgs args)
	{
		var templateSettings = TemplateSettings;

		var height = args.NewSize.Height;
		templateSettings.ContentHeight = height;
		templateSettings.NegativeContentHeight = -1 * height;
	}

	private void RaiseExpandingEvent(Expander container) =>
		Expanding?.Invoke(this, null);

	private void RaiseCollapsedEvent(Expander container) =>
		Collapsed?.Invoke(this, null);

	private void OnIsExpandedPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var isExpanded = IsExpanded;

		if (isExpanded)
		{
			RaiseExpandingEvent(this);
		}
		else
		{
			// Available for a 'Collapsing' event
		}

		UpdateExpandState(true);

		if (isExpanded)
		{
			// Available for an 'Expanded' event
		}
		else
		{
			RaiseCollapsedEvent(this);
		}
	}

	private void OnExpandDirectionPropertyChanged(DependencyPropertyChangedEventArgs args) =>
		UpdateExpandDirection(true);

	private void UpdateExpandDirection(bool useTransitions)
	{
		var direction = ExpandDirection;

		switch (direction)
		{
			case ExpandDirection.Down:
				VisualStateManager.GoToState(this, "Down", useTransitions);
				break;
			case ExpandDirection.Up:
				VisualStateManager.GoToState(this, "Up", useTransitions);
				break;
		}
	}

	private void UpdateExpandState(bool useTransitions)
	{
		var isExpanded = IsExpanded;
		var direction = ExpandDirection;

		if (isExpanded)
		{
			if (direction == ExpandDirection.Down)
			{
				VisualStateManager.GoToState(this, "ExpandDown", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "ExpandUp", useTransitions);
			}
		}
		else
		{
			if (direction == ExpandDirection.Down)
			{
				VisualStateManager.GoToState(this, "CollapseUp", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "CollapseDown", useTransitions);
			}
		}

		if (FrameworkElementAutomationPeer.FromElement(this) is AutomationPeer peer)
		{
			var expanderPeer = peer as ExpanderAutomationPeer;
			expanderPeer?.RaiseExpandCollapseAutomationEvent(
				isExpanded ?
					ExpandCollapseState.Expanded :
					ExpandCollapseState.Collapsed
			);
		}
	}

	// Uno specific: We need to ensure events are subscribed/unsubscribed on load/unload to avoid memory leaks.
	private void Expander_Loaded(object sender, RoutedEventArgs e)
	{
		if (_eventSubscriptions.Disposable is null)
		{
			OnApplyTemplate();
		}
	}

	private void Expander_Unloaded(object sender, RoutedEventArgs e) => _eventSubscriptions.Disposable = null;
}
