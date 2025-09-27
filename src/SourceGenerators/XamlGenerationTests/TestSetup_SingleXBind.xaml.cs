using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace XamlGenerationTests;

public sealed partial class TestSetup_SingleXBind : Page
{
	public TestSetup_SingleXBind()
	{
		this.InitializeComponent();
	}

	public TestSetup_SingleXBind_Data VM { get; set; }

	public class TestSetup_SingleXBind_Data
	{
		public string Asd { get; set; }
	}
}
