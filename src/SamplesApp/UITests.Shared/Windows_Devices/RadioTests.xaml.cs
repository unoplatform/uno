using System;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Radios;

namespace UITests.Shared.Windows_Devices
{
	[SampleControlInfo("Windows.Devices", "RadioTests", description: "Demonstrates use of Windows.Devices.Radio.GetRadiosAsyncTask")]
	public sealed partial class RadioTests : UserControl
	{
		public RadioTests()
		{
			this.InitializeComponent();
		}

		private async void uiCreate_Click(object sender, RoutedEventArgs e)
		{
			uiResultMsg.Text = "Calling GetRadiosAsyncTask...";
			try
			{
				var radiosList = await Windows.Devices.Radios.Radio.GetRadiosAsync();

				uiListItems.ItemsSource = radiosList;
				uiResultMsg.Text = "Your current Radios:";

				foreach (var oneRadio in radiosList)
                {
					if(oneRadio.State == RadioState.Unknown)
                    {
						uiResultMsg.Text = "Your current Radios (RadioState.Unknown means: you probably have no appropriate permission declared):";
					}
                }
			}
			catch (Exception ex)
			{
				uiResultMsg.Text = "Got exception (maybe API is not supported on your platform): " + ex.Message;
			}
		}
	}
}
