using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class Expander : ContentControl
    {
		private readonly string c_expanderHeader = "ExpanderHeader";

		public Expander()
		{
			// Uno Doc: Not supported
			//__RP_Marker_ClassById(RuntimeProfiler::ProfId_Expander);

			SetDefaultStyleKey(this);
		}

		// IUIElement

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ExpanderAutomationPeer(this);
		}

		// IFrameworkElement

		protected override void OnApplyTemplate()
		{
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

			UpdateExpandState(false);
			UpdateExpandDirection(false);
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
			if (IsExpanded)
			{
				RaiseExpandingEvent(this);
			}
			else
			{
				RaiseCollapsedEvent(this);
			}
			UpdateExpandState(true);
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

			if (isExpanded)
			{
				VisualStateManager.GoToState(this, "Expanded", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Collapsed", useTransitions);
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
    }
}
