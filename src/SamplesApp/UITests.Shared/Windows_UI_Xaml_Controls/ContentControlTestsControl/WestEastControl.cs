using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ContentControlTestsControl
{
	public partial class WestEastControl : Control
	{
		#region DependencyProperty: West

		public static DependencyProperty WestProperty { get; } = DependencyProperty.Register(
			nameof(West),
			typeof(object),
			typeof(WestEastControl),
			new PropertyMetadata(default(object)));

		public object West
		{
			get => (object)GetValue(WestProperty);
			set => SetValue(WestProperty, value);
		}

		#endregion
		#region DependencyProperty: East

		public static DependencyProperty EastProperty { get; } = DependencyProperty.Register(
			nameof(East),
			typeof(object),
			typeof(WestEastControl),
			new PropertyMetadata(default(object)));

		public object East
		{
			get => (object)GetValue(EastProperty);
			set => SetValue(EastProperty, value);
		}

		#endregion
	}
}
