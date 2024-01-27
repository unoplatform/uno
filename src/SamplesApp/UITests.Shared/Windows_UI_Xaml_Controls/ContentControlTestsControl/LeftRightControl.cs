using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ContentControlTestsControl
{
	public partial class LeftRightControl : Control
	{
		#region DependencyProperty: Left

		public static DependencyProperty LeftProperty { get; } = DependencyProperty.Register(
			nameof(Left),
			typeof(object),
			typeof(LeftRightControl),
			new PropertyMetadata(default(object)));

		public object Left
		{
			get => (object)GetValue(LeftProperty);
			set => SetValue(LeftProperty, value);
		}

		#endregion
		#region DependencyProperty: Right

		public static DependencyProperty RightProperty { get; } = DependencyProperty.Register(
			nameof(Right),
			typeof(object),
			typeof(LeftRightControl),
			new PropertyMetadata(default(object)));

		public object Right
		{
			get => (object)GetValue(RightProperty);
			set => SetValue(RightProperty, value);
		}

		#endregion
	}
}
