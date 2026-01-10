using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.Enability
{
	[Sample("Control", "BasicEnability")]
	public sealed partial class BasicEnability : Page
	{
		public BasicEnability()
		{
			this.InitializeComponent();

			this.Loaded += (_, _) => FocusManager.GotFocus += FocusManager_GotFocus;
			this.Unloaded += (_, _) => FocusManager.GotFocus -= FocusManager_GotFocus;

			disableGroup1.Click += (snd, evt) => enableCtl1.IsChecked = false;
			disableGroup2.Click += (snd, evt) => enableCtl2.IsChecked = false;
			disableGroup3.Click += (snd, evt) => enableCtl3.IsChecked = false;
		}

		private void FocusManager_GotFocus(object sender, FocusManagerGotFocusEventArgs e)
		{
			if (e.NewFocusedElement is FrameworkElement fe)
			{
				focused.Text = string.IsNullOrWhiteSpace(fe.Name) ? $"<unnamed {fe.GetType().Name}>" : fe.Name;
			}
			else
			{
				focused.Text = "<none>";
			}
		}
	}
}
