using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_XAML_Controls.UserControlTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class UserControl_TopLevelBinding_UserControl : UserControl
	{
		public UserControl_TopLevelBinding_UserControl()
		{
			this.InitializeComponent();
		}
	}

	public static class UserControl_TopLevelBinding_AttachedProperty
	{
		public static int GetMyProperty(DependencyObject obj)
		{
			return (int)obj.GetValue(MyPropertyProperty);
		}

		public static void SetMyProperty(DependencyObject obj, int value)
		{
			obj.SetValue(MyPropertyProperty, value);
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.RegisterAttached("MyProperty", typeof(int), typeof(UserControl_TopLevelBinding_AttachedProperty), new FrameworkPropertyMetadata(0, OnMyPropertyChanged));

		public static int MyPropertyChangedCount { get; private set; }

		private static void OnMyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			MyPropertyChangedCount++;
		}
	}
}
