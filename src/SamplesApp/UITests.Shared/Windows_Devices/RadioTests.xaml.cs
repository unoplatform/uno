using System;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Radios;
using System.Collections.Generic;

namespace UITests.Shared.Windows_Devices
{
	[Sample("Windows.Devices", Name = "RadioTests", IgnoreInSnapshotTests = true, Description = "Demonstrates use of Windows.Devices.Radio.GetRadiosAsync")]
	public sealed partial class RadioTests : UserControl
	{
		public RadioTests()
		{
			this.InitializeComponent();
		}

		private async void uiCreate_Click(object sender, RoutedEventArgs e)
		{
			uiResultMsg.Text = "Calling GetRadiosAsync...";
			try
			{
				var radiosList = await Windows.Devices.Radios.Radio.GetRadiosAsync();

				uiResultMsg.Text = "Your current Radios:";

				var intermediateRadiosList = new List<IntermediateRadio>();

				foreach (var oneRadio in radiosList)
				{
					if (oneRadio.State == RadioState.Unknown)
					{
						uiResultMsg.Text = "Your current Radios (RadioState.Unknown means: you probably have no appropriate permission declared):";
					}

					var intermediateRadio = new IntermediateRadio();
					intermediateRadio.RadioKind = oneRadio.Kind.ToString();
					intermediateRadio.RadioName = oneRadio.Name;
					intermediateRadio.RadioState = oneRadio.State.ToString();
					intermediateRadiosList.Add(intermediateRadio);
				}

				uiListItems.ItemsSource = intermediateRadiosList;

			}
			catch (Exception ex)
			{
				uiResultMsg.Text = "Got exception (maybe API is not supported on your platform): " + ex.Message;
			}
		}
	}

	// an intermediate class, between Windows.Devices.Radios.Radio and XAML binding
	public class IntermediateRadio
	{
		public string RadioKind { get; set; }

		public string RadioName { get; set; }

		public string RadioState { get; set; }
	}
}
