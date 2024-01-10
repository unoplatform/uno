using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SampleControl.Entities;
using Uno.Extensions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Uno.UI.Samples.Controls;

public sealed partial class SampleInfoControl : UserControl
{
	public SampleInfoControl()
	{
		this.InitializeComponent();
	}

	public void CopyContentClick(object sender, RoutedEventArgs e)
	{
		var button = (Button)sender;
		var content = (string)button.CommandParameter;
		var dataPackage = new DataPackage();
		dataPackage.SetText(content);
		Clipboard.SetContent(dataPackage);
	}
}
