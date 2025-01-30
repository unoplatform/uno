using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft/* UWP don't rename */.UI.Xaml;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;
using Microsoft/* UWP don't rename */.UI.Xaml.Data;
using Microsoft/* UWP don't rename */.UI.Xaml.Input;
using Microsoft/* UWP don't rename */.UI.Xaml.Media;
using Microsoft/* UWP don't rename */.UI.Xaml.Navigation;

namespace Test01;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
	}

	public string MyProperty => "42";
}
