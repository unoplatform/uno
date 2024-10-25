// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.ObjectModel;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.Contacts;
using System.Threading.Tasks;

namespace MUXControlsTestApp
{
	[SampleControlInfo(
		"MUX",
		"PersonPictureLateBinding",
		isManualTest: true)]
#pragma warning disable UXAML0002 // does not explicitly define the Microsoft.UI.Xaml.Controls.UserControl base type in code behind.
	public sealed partial class PersonPictureLateBindingPage
#pragma warning restore UXAML0002 // does not explicitly define the Microsoft.UI.Xaml.Controls.UserControl base type in code behind.
	{
		public string ImagePath { get; } = "ms-appx:///Assets/ingredient2.png";

		public string LeName { get; } = "James Bondi";

		public PersonPictureLateBindingPage()
		{
			this.InitializeComponent();
			_ = SetImagePathWithDelay();
		}

		private async Task SetImagePathWithDelay()
		{
			await Task.Delay(1000);
			DataContext = this;
		}
	}
}
