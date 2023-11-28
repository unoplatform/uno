using Microsoft/* UWP don't rename */.UI.Xaml;
#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif
using Uno.UI.Samples.Controls;
using System.Collections.Generic;
using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

#if !WINAPPSDK
using Microsoft.UI.Input;
#endif

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
#if !WINAPPSDK
			var SizeWestEastCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
			this.ProtectedCursor = SizeWestEastCursor;
#endif
		}

		private void ChangeBorderCursor(object sender, PointerRoutedEventArgs e)
		{
#if !WINAPPSDK
			var waitCursor = InputSystemCursor.Create(InputSystemCursorShape.Wait);
			this.ProtectedCursor = waitCursor;
#endif
		}

		private void CaptureCursor(object sender, PointerRoutedEventArgs e)
		{
#if !WINAPPSDK
			var border = (Border)sender;
			border.CapturePointer(e.Pointer);
#endif
		}
	}
}
