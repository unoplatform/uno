using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
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
