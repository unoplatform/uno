using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;

public sealed partial class Uno7497 : Page
{
	public Uno7497()
	{
		this.InitializeComponent();
	}
}


public partial class Control7497 : Control
{
	#region DependencyProperty: TestList

	public static DependencyProperty TestListProperty { get; } = DependencyProperty.Register(
		nameof(TestList),
		typeof(ObservableCollection<string>),
		typeof(Control7497),
		new PropertyMetadata(default(ObservableCollection<string>)));

	public ObservableCollection<string> TestList
	{
		get => (ObservableCollection<string>)GetValue(TestListProperty);
		set => SetValue(TestListProperty, value);
	}

	#endregion

	public Control7497()
	{
		DefaultStyleKey = typeof(Control7497);
		TestList = new ObservableCollection<string>()
		{
			"Test 1",
			"Test 2"
		};
	}
}
