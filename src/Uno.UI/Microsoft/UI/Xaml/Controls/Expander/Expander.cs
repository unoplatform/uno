using System;
using System.Numerics;
using Uno.Disposables;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Expander : ContentControl
	{
		private readonly string c_expanderHeader = "ExpanderHeader";
		private readonly string c_expanderContent = "ExpanderContent";
		private readonly string c_expanderContentClip = "ExpanderContentClip";

		// Uno Doc: Added to dispose event handlers
		private SerialDisposable _eventSubscriptions = new SerialDisposable();

		public Expander()
		{
			// Uno Doc: Not supported
			//__RP_Marker_ClassById(RuntimeProfiler::ProfId_Expander);

			DefaultStyleKey = typeof(AnimatedIcon);

			SetValue(TemplateSettingsProperty, new ExpanderTemplateSettings());

#if HAS_UNO
			Loaded += Expander_Loaded;
			Unloaded += Expander_Unloaded;
#endif
		}

		// IUIElement

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ExpanderAutomationPeer(this);
		}

		// IFrameworkElement

		protected override void OnApplyTemplate()
		{
			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = null;
			var registrations = new CompositeDisposable();

			if (GetTemplateChild<Control>(c_expanderHeader) is ToggleButton toggleButton)
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
						//var expanderEventsSource = expanderPeer.EventsSource != null ?
						//	expanderPeer.EventsSource :
						//	expanderPeer;
						//toggleButtonPeer.EventsSource = expanderEventsSource;
					}

					// 2. If the expander doesn't have any AutomationProperties.Name set,
					// we will try setting one based on the header. This is how
					// WPF's expanders work.
					if (string.IsNullOrEmpty(AutomationProperties.GetName(this))
						&& !string.IsNullOrEmpty(toggleButtonPeer.GetName()))
					{
						// Uno Doc: The equivalent '.GetName()' substituted for '.GetNameCore()' in WinUI
						AutomationProperties.SetName(this, toggleButtonPeer.GetName());
					}
				}
			}

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

			// Uno Doc: Added to dispose event handlers
			_eventSubscriptions.Disposable = registrations;
		}

#if HAS_UNO
		protected void OnContentClipSizeChanged(object sender, SizeChangedEventArgs args)
		{
			// TODO Uno specific: LayoutInformation.GetLayoutSlot(element) is currently inconsistent
			// on some platforms, so construct the Rect from ActualSize. A non-zero Point
			// is not required for this case.
			var expanderContentClip = sender as Border;
			var layoutSlot = new Rect(new Point(0, 0), expanderContentClip.ActualSize.ToSize());
			expanderContentClip.Clip = new RectangleGeometry() { Rect = layoutSlot };
		}
#endif

		protected void OnContentSizeChanged(object sender, SizeChangedEventArgs args)
		{
			var templateSettings = TemplateSettings;

			var height = args.NewSize.Height;
			templateSettings.ContentHeight = height;
			templateSettings.NegativeContentHeight = -1 * height;
		}

		protected void RaiseExpandingEvent(Expander container)
		{
			Expanding?.Invoke(this, new ExpanderExpandingEventArgs()); // Uno Doc: We won't use null for args like WinUI
		}

		protected void RaiseCollapsedEvent(Expander container)
		{
			Collapsed?.Invoke(this, new ExpanderCollapsedEventArgs()); // Uno Doc: We won't use null for args like WinUI
		}

		protected void OnIsExpandedPropertyChanged(DependencyPropertyChangedEventArgs args)
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

		protected void OnExpandDirectionPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			UpdateExpandDirection(true);
		}

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

#if HAS_UNO
		private void Expander_Loaded(object sender, RoutedEventArgs e)
		{
			if (_eventSubscriptions.Disposable is null)
			{
				OnApplyTemplate();
			}
		}

		private void Expander_Unloaded(object sender, RoutedEventArgs e) => _eventSubscriptions.Disposable = null;
#endif
	}
}
