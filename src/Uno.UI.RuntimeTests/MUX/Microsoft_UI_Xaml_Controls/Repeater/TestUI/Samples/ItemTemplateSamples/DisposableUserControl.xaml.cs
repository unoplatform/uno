using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MUXControlsTestApp.Samples
{
	public sealed partial class DisposableUserControl : UserControl
	{

		public int Number
		{
			get { return (int)GetValue(NumberProperty); }
			set { SetValue(NumberProperty, value); }
		}

		public static readonly DependencyProperty NumberProperty =
			DependencyProperty.Register("Number", typeof(int), typeof(DisposableUserControl), new PropertyMetadata(-1));


		public static int OpenItems { get { return _counter; } }
		private static int _counter;

		public DisposableUserControl()
		{
			Interlocked.Increment(ref _counter);
			this.InitializeComponent();
		}

		~DisposableUserControl()
		{
			Interlocked.Decrement(ref _counter);
		}
	}
}
