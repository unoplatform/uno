using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.App.Views
{
	public partial class MyDependencyObject : DependencyObject
	{
		public int PlainInt { get; set; }

		public int DPInt
		{
			get { return (int)GetValue(DPIntProperty); }
			set { SetValue(DPIntProperty, value); }
		}

		public static readonly DependencyProperty DPIntProperty =
			DependencyProperty.Register("DPInt", typeof(int), typeof(MyDependencyObject), new PropertyMetadata(0));


	}
}
