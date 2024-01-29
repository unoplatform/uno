#pragma warning disable CS0169

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using _TextBox = Microsoft.UI.Xaml.Controls.TextBox;

namespace UITests.Windows_UI_Xaml_Controls.ContentDialogTests
{
	[Sample("Dialogs", IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class ContentDialog_TextBox : UserControl
	{
#if __ANDROID__
		private bool _initialEnableNativePopups;
#endif

		public ContentDialog_TextBox()
		{
			this.InitializeComponent();
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
#if __ANDROID__
			_initialEnableNativePopups = Uno.UI.FeatureConfiguration.Popup.UseNativePopup;
			EnableNativeCheckBox.IsChecked = _initialEnableNativePopups;
#endif
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
#if __ANDROID__
			Uno.UI.FeatureConfiguration.Popup.UseNativePopup = _initialEnableNativePopups;
#endif
		}

		private void OnEnableNativeCheckedChanged(object sender, object args)
		{
#if __ANDROID__
			var isChecked = EnableNativeCheckBox.IsChecked ?? _initialEnableNativePopups;
			Uno.UI.FeatureConfiguration.Popup.UseNativePopup = isChecked;
#endif
		}

		private async void ShowContentDialog(object sender, object args)
		{
			var tb = new _TextBox
			{
				Height = 1200
			};

			var dummyButton = new Button { Content = "Dummy" };

			var SUT = new ContentDialog
			{
				Title = "Dialog title",
				Content = new ScrollViewer
				{
					Content = new StackPanel
					{
						Children =
							{
								dummyButton,
								tb
							}
					}
				},
				PrimaryButtonText = "Accept",
				SecondaryButtonText = "Nope"
			};

			SUT.XamlRoot = this.XamlRoot;
			await SUT.ShowAsync();
		}
	}
}
