using Microsoft.UI.Xaml;
#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif
using Uno.UI.Samples.Controls;
using System.Collections.Generic;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;

namespace SamplesApp.Wasm.Windows_UI_Input
{
	[Sample("Windows.UI.Input", Name = "SetProtectedCursor", IsManualTest = true, Description = "Demonstrates use of UIElement.ProtectedCursor / InputSystemCursor / InputSystemCursorShape")]
	public sealed partial class SetProtectedCursor : Page
	{
		public SetProtectedCursor()
		{
			this.InitializeComponent();
		}

		private void ChangeButtonCursor(object sender, PointerRoutedEventArgs e)
		{
			var SizeWestEastCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
			this.ProtectedCursor = SizeWestEastCursor;
		}

		private void ChangeBorderCursor(object sender, PointerRoutedEventArgs e)
		{
			var waitCursor = InputSystemCursor.Create(InputSystemCursorShape.Wait);
			this.ProtectedCursor = waitCursor;
		}

		private void CaptureCursor(object sender, PointerRoutedEventArgs e)
		{
			var border = (Border)sender;
			border.CapturePointer(e.Pointer);
		}
	}
}
