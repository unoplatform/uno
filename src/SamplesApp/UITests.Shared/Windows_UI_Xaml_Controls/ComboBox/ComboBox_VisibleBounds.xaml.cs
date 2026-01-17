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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox")]
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
