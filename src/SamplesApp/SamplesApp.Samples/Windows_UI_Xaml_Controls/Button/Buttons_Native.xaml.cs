using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Uno.UI.Common;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using ICommand = System.Windows.Input.ICommand;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls
{
	[Sample("Buttons", Name = "Buttons_Native")]

	public sealed partial class Buttons_Native : UserControl
	{
		int clickActionsCounter = 0;
		int commandActionsCounter = 0;
		int tappedActionsCounter = 0;
		int toggleActionsCounter = 0;

		public Buttons_Native()
		{
			this.InitializeComponent();
		}

		public ICommand ClickCommand => new DelegateCommand<object>(o => resultCommand.Text = $"Command {o} ({++commandActionsCounter})");

		private void OnClick(object sender, object args)
		{
			switch (sender)
			{
				case Microsoft.UI.Xaml.Controls.Button b:
					result.Text = $"Button {b.Name} Clicked ({++clickActionsCounter})";
					return;
			}
		}

		private void OnTapped(object sender, object args)
		{
			switch (sender)
			{
				case Microsoft.UI.Xaml.Controls.Button b:
					resultTapped.Text = $"Button {b.Name} Tapped ({++tappedActionsCounter})";
					return;
			}
		}

		private void OnToggled(object sender, object args)
		{
			switch (sender)
			{
				case Microsoft.UI.Xaml.Controls.ToggleSwitch ts:
					result.Text = $"ToggleSwitch {ts.Name} Toggled {ts.IsOn} ({++toggleActionsCounter})";
					return;
			}
		}

		private void OnClickEnableButton02(object sender, object args)
		{
			button02.IsEnabled = true;
		}

		private void OnClickEnableToggleSwitch02(object sender, object args)
		{
			toggleSwitch02.IsEnabled = true;
		}
	}
}
