// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ToggleSplitButton.cpp

using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ToggleSplitButton : SplitButton
	{
		public ToggleSplitButton()
		{
			this.SetDefaultStyleKey();
		}

		public event TypedEventHandler<ToggleSplitButton, ToggleSplitButtonIsCheckedChangedEventArgs> IsCheckedChanged;

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;

			if (property == IsCheckedProperty)
			{
				OnIsCheckedChanged();
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ToggleSplitButtonAutomationPeer(this);
		}

		private void OnIsCheckedChanged()
		{
			if (m_hasLoaded)
			{
				var eventArgs = new ToggleSplitButtonIsCheckedChangedEventArgs();
				IsCheckedChanged?.Invoke(this, eventArgs);
				if (FrameworkElementAutomationPeer.FromElement(this) is { } peer)
				{
					var newValue = IsChecked ? ToggleState.On : ToggleState.Off;
					var oldValue = (newValue == ToggleState.On) ? ToggleState.Off : ToggleState.On;
					peer.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
				}
			}

			UpdateVisualStates();
		}

		protected override void OnClickPrimary(object sender, RoutedEventArgs args)
		{
			Toggle();

			base.OnClickPrimary(sender, args);
		}

		internal override bool InternalIsChecked() => IsChecked;

		internal void Toggle()
		{
			IsChecked = !IsChecked;
		}
	}
}
