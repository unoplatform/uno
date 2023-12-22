// MUX commit reference 36f8f8f6d5f11f414fdaa0462d0c4cb845cf4254

using Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;

#pragma warning disable 105 // Required for the WinUI replace pass
using Microsoft.UI.Xaml.Automation.Peers;
#pragma warning restore 105

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class ToggleSplitButton : SplitButton
	{
		public ToggleSplitButton()
		{
			DefaultStyleKey = typeof(ToggleSplitButton);
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
				var peer = FrameworkElementAutomationPeer.FromElement(this);
				if (peer != null)
				{
					var newValue = IsChecked ? ToggleState.On : ToggleState.Off;
					var oldValue = (newValue == ToggleState.On) ? ToggleState.Off : ToggleState.On;
					peer.RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, oldValue, newValue);
				}
			}

			UpdateVisualStates();
		}

		internal override void OnClickPrimary(object sender, RoutedEventArgs args)
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
