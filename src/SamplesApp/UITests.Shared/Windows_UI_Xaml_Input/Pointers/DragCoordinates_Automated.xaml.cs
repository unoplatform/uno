using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace UITests.Shared.Windows_UI_Xaml_Input.Pointers
{
	[Sample("Pointers", "DragCoordinates_Automated")]
	public sealed partial class DragCoordinates_Automated : UserControl
	{
		public DragCoordinates_Automated()
		{
			this.InitializeComponent();

			Point startPos = new Point();
			bool pressed = false;

			myBorder.PointerPressed += (s, e) =>
			{
				Console.WriteLine("Pointer pressed");
				startPos = e.GetCurrentPoint(myBorder).Position;
				pressed = true;
				myBorder.CapturePointer(e.Pointer);
			};

			myBorder.PointerMoved += (s, e) =>
			{
				if (pressed)
				{
					Canvas.SetTop(myBorder, e.GetCurrentPoint(rootCanvas).Position.Y - startPos.Y);
					Canvas.SetLeft(myBorder, e.GetCurrentPoint(rootCanvas).Position.X - startPos.X);
				}
			};

			myBorder.PointerCanceled += (s, e) =>
			{
				Console.WriteLine("Pointer cancelled");
			};

			myBorder.PointerReleased += (s, e) =>
			{
				Console.WriteLine("Pointer released");
				myBorder.ReleasePointerCapture(e.Pointer);
				pressed = false;
			};
		}
	}
}
