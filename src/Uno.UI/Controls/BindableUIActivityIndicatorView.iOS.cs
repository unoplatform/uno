using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Windows.UI.Xaml;

namespace Uno.UI.Views.Controls
{
	public partial class BindableUIActivityIndicatorView : UIActivityIndicatorView, DependencyObject
	{
		public BindableUIActivityIndicatorView()
		{
			InitializeBinder();
		}
	}
}
