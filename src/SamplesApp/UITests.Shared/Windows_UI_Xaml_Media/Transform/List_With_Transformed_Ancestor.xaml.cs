using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

namespace UITests.Shared.Windows_UI_Xaml_Media.Transform
{
	[Sample("Transform", "List_With_Transformed_Ancestor", description: "Items can be swiped. This shouldn't cause visual 'tearing' on Android.")]
	public sealed partial class List_With_Transformed_Ancestor : UserControl
	{
		public List_With_Transformed_Ancestor()
		{
			this.InitializeComponent();
		}

		Point _initialPoint;
		bool _isPressed;
		private void Border_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!_isPressed)
			{
				return;
			}
			var currentPoint = e.GetCurrentPoint(null).Position;
			var offset = currentPoint.X - _initialPoint.X;
			SetHorizontalTransform(sender as Border, offset);
		}

		private void Border_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_isPressed = true;
			_initialPoint = e.GetCurrentPoint(null).Position;
		}

		private void SetHorizontalTransform(Border border, double x)
		{
			(border.RenderTransform as TranslateTransform).X = x;
		}

		private void Border_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			_isPressed = false;
			SetHorizontalTransform(sender as Border, 0);
		}

		private void Border_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			_isPressed = false;
			SetHorizontalTransform(sender as Border, 0);
		}
	}
}
