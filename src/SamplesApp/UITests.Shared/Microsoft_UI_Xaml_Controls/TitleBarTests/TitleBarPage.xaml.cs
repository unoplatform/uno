// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Private.Controls;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

[Sample("Windowing", Name = "TitleBar control", IsManualTest = true)]
public sealed partial class TitleBarPage : TestPage
{
	public TitleBarPage()
	{
		this.InitializeComponent();
	}

	private void TitleBarWindowingButton_Click(object sender, RoutedEventArgs e)
	{
		var newWindow = new TitleBarPageWindow();
		newWindow.Activate();
	}
}
