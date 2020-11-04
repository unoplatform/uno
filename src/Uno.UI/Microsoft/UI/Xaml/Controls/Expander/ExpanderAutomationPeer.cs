using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Controls
{
	public class ExpanderAutomationPeer : AutomationPeer, IExpandCollapseProvider
	{
		// Uno Doc: Added for the Uno Platform
		private readonly Expander _owner;

		// WPF ExpanderAutomationPeer:
		// https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/ExpanderAutomationPeer.cs

		public ExpanderAutomationPeer(Expander owner)
		{
			_owner = owner;
		}

		// IAutomationPeerOverrides

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.ExpandCollapse)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			// WPF uses "Expander" as its class name
			return nameof(Expander);
		}

		protected override string GetNameCore()
		{
			return base.GetNameCore();
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			// WPF uses "Group" as its control type core
			return AutomationControlType.Group;
		}

		protected override bool HasKeyboardFocusCore()
		{
			/*
			// We are not going to call the overriden one because that one doesn't have the toggle button.
			var childrenPeers = GetInner().as< IAutomationPeerOverrides > ().GetChildrenCore();

			foreach (var peer in childrenPeers)
			{
				if (peer.GetAutomationId() == "ExpanderToggleButton")
				{
					// Since the EventsSource of the toggle button
					// is the same as the expander's, we need to
					// redirect the focus of the expander and base it on the toggle button's.
					return peer.HasKeyboardFocus();
				}
			}
			*/
			// If the toggle button doesn't have the current focus, then
			// the expander's not focused.
			return false;
		}

		// This function gets called when there's narrator and the user is trying to touch the expander
		// If this happens, we will return the toggle button's peer and focus it programmatically,
		// to synchronize this touch focus with the keyboard one.
		protected override AutomationPeer GetPeerFromPointCore(Point point)
		{
			/*
			var childrenPeers = GetInner.as< winrt::IAutomationPeerOverrides > ().GetChildrenCore();

			foreach (var peer in childrenPeers)
			{
				if (peer.GetAutomationId() == "ExpanderToggleButton")
				{
					var frameworkElementPeer = peer as FrameworkElementAutomationPeer;
					var toggleButton = frameworkElementPeer.Owner as ToggleButton;
					toggleButton.Focus(FocusState.Programmatic);
					return peer;
				}
			}
			*/
			return base.GetPeerFromPointCore(point);
		}

		// We are going to take out the toggle button off the children, because we are setting
		// the toggle button's event source to this automation peer. This removes any cyclical
		// dependency.
		protected override IList<AutomationPeer> GetChildrenCore()
		{
			/*
			var childrenPeers = GetInner().as< winrt::IAutomationPeerOverrides > ().GetChildrenCore();
			var peers = winrt::make < Vector < winrt::AutomationPeer, MakeVectorParam< VectorFlag::DependencyObjectBase > () >> (
													static_cast<int>(childrenPeers.Size() - 1) // capacity //);

			foreach (var peer in childrenPeers)
			{
				if (peer.GetAutomationId() != "ExpanderToggleButton")
				{
					peers.Append(peer);
				}
			}
			
			return peers;
			*/
			return new List<AutomationPeer>();
		}

		// IExpandCollapseProvider

		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				var state = ExpandCollapseState.Collapsed;

				if (_owner is Expander expander)
				{
					state = expander.IsExpanded ?
						ExpandCollapseState.Expanded :
						ExpandCollapseState.Collapsed;
				}

				return state;
			}
		}

		public void Expand()
		{
			if (_owner is Expander expander)
			{
				expander.IsExpanded = true;
				RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Expanded);
			}
		}

		public void Collapse()
		{
			if (_owner is Expander expander)
			{
				expander.IsExpanded = false;
				RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Collapsed);
			}
		}

		public void RaiseExpandCollapseAutomationEvent(ExpandCollapseState newState)
		{
			// Uno Doc: AutomationEvents not currently implemented so added an API check
			if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Automation.Peers.AutomationEvents", nameof(AutomationEvents.PropertyChanged)))
			{
				if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
				{
					ExpandCollapseState oldState = (newState == ExpandCollapseState.Expanded) ?
						ExpandCollapseState.Collapsed :
						ExpandCollapseState.Expanded;

					// if box_value(oldState) doesn't work here, use ReferenceWithABIRuntimeClassName to make Narrator unbox it.
					RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
						oldState,
						newState);
				}
			}
		}

		private Expander GetImpl()
		{
			Expander impl = null;

			if (_owner is Expander expander)
			{
				impl = expander;
			}

			return impl;
		}
	}
}
