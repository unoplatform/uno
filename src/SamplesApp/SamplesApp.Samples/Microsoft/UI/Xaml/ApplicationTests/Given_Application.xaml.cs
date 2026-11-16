using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml.ApplicationTests;

[Sample("Application")]
public sealed partial class Given_Application : Page
{
	public Given_Application()
	{
		this.InitializeComponent();
	}

	private void CoreApplication_Exiting(object sender, object e)
	{
		this.Log().LogInformation("Exiting event was triggered");
	}

	public void OnForceExit()
	{
		CoreApplication.Exiting += CoreApplication_Exiting;
		Application.Current.Exit();
	}

	public void OnCoreApplicationForceExit()
	{
		CoreApplication.Exiting += CoreApplication_Exiting;
		CoreApplication.Exit();
	}
}
