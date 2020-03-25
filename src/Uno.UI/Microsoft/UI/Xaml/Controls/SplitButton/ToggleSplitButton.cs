using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using SplitButton = Microsoft.UI.Xaml.Controls.SplitButton;
using ToggleSplitButton = Microsoft.UI.Xaml.Controls.ToggleSplitButton;
using ToggleSplitButtonIsCheckedChangedEventArgs = Microsoft.UI.Xaml.Controls.ToggleSplitButtonIsCheckedChangedEventArgs;

namespace Microsoft.UI.Xaml.Controls
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
