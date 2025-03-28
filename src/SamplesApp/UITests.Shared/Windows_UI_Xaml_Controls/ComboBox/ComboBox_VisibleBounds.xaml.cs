using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo("ComboBox")]
	public sealed partial class ComboBox_VisibleBounds : UserControl
	{
		public ComboBox_VisibleBounds()
		{
			this.InitializeComponent();

			combo01.ItemsSource = Enumerable.Range(0, 20).Select(u => new { Name = $"{u:00}" }).ToArray();
			combo01.SelectedIndex = 0;

			var previousState = CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar;
			Unloaded += (s, e) => CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = previousState;
		}

		private void OnChangeStatusBarExtended(object sender, object args)
		{
			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
		}
	}
}
