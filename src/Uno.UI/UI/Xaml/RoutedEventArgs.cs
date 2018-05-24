using System;

namespace Windows.UI.Xaml
{

	public delegate void ExceptionRoutedEventHandler(object sender, ExceptionRoutedEventArgs e);

	public delegate void RoutedEventHandler (object sender, RoutedEventArgs e);
	
	public delegate void RoutedEventHandler<in TArgs> (object sender, TArgs e)
		where TArgs : RoutedEventArgs;

	public partial class RoutedEventArgs : EventArgs
	{
		new public static readonly RoutedEventArgs Empty;

		public RoutedEventArgs ()
		{
		}

		public RoutedEventArgs (object originalSource)
		{
			OriginalSource = originalSource;
		}

		public object OriginalSource { 
			get;
			internal set;
		}
	}
}

