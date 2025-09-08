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

namespace Test01;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();

		MyStaticClass.Configure(something => { });
	}
}

public static class MyStaticClass
{
	public static void Configure(Action<object> action)
	{
		action.ToString();
	}
}
