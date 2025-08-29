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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public static class Uids
	{
		public static readonly DependencyProperty UidProperty = DependencyProperty.RegisterAttached(
			"Uid",
			typeof(string),
			typeof(Uids),
			new PropertyMetadata(default));

		public static string GetUid(DependencyObject dependencyObject)
		{
			return (string)dependencyObject.GetValue(UidProperty);
		}

		public static void SetUid(DependencyObject dependencyObject, string uid)
		{
			dependencyObject.SetValue(UidProperty, uid);
		}
	}

	public sealed partial class When_xUid_Conflicts_With_AttachedProperty_Named_Uid : UserControl
	{
		public When_xUid_Conflicts_With_AttachedProperty_Named_Uid()
		{
			this.InitializeComponent();
			Loaded += When_xUid_Has_Name_Conflict_Loaded;
		}

		private void When_xUid_Has_Name_Conflict_Loaded(object sender, RoutedEventArgs e)
		{
		}
	}
}
