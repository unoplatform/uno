using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.ViewLibrary
{
	public partial class MyExtControl : Control
	{
		public MyExtControl()
		{
			DefaultStyleKey = typeof(MyExtControl);
		}
		public string MyTag
		{
			get { return (string)GetValue(MyTagProperty); }
			set { SetValue(MyTagProperty, value); }
		}

		public static readonly DependencyProperty MyTagProperty =
			DependencyProperty.Register("MyTag", typeof(string), typeof(MyExtControl), new PropertyMetadata("Default"));

		public string MyTag2
		{
			get { return (string)GetValue(MyTag2Property); }
			set { SetValue(MyTag2Property, value); }
		}

		public static readonly DependencyProperty MyTag2Property =
			DependencyProperty.Register("MyTag2", typeof(string), typeof(MyExtControl), new PropertyMetadata("Default"));


		public string MyTagThemed1
		{
			get { return (string)GetValue(MyTagThemed1Property); }
			set { SetValue(MyTagThemed1Property, value); }
		}

		// Using a DependencyProperty as the backing store for MyTagThemed1.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyTagThemed1Property =
			DependencyProperty.Register("MyTagThemed1", typeof(string), typeof(MyExtControl), new PropertyMetadata("Default"));


		public string MyTagThemed2
		{
			get { return (string)GetValue(MyTagThemed2Property); }
			set { SetValue(MyTagThemed2Property, value); }
		}

		// Using a DependencyProperty as the backing store for MyTagThemed2.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyTagThemed2Property =
			DependencyProperty.Register("MyTagThemed2", typeof(string), typeof(MyExtControl), new PropertyMetadata("Default"));

	}
}
