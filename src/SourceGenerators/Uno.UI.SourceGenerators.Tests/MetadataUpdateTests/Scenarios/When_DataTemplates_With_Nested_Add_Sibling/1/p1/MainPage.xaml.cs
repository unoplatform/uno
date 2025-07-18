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
	}
}

[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Template1))]
public class MyControl
{
	public DataTemplate Template1 { get; set; }
	public DataTemplate Template2 { get; set; }
}
