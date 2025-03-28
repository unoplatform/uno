using Microsoft/* UWP don't rename */.UI.Xaml;
#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif
using Uno.UI.Samples.Controls;
using System.Collections.Generic;
using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.UI.Input;

namespace SamplesApp.Wasm.Windows_UI_Input
{
	[SampleControlInfo("Windows.UI.Input", "SetProtectedCursor", isManualTest: true, description: "Demonstrates use of UIElement.ProtectedCursor / InputSystemCursor / InputSystemCursorShape")]
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
