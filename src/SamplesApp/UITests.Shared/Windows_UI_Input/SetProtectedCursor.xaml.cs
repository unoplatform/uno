#nullable disable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Collections.Generic;
using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

#if !WINDOWS_UWP
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
#if !WINDOWS_UWP
			var SizeWestEastCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
			this.ProtectedCursor = SizeWestEastCursor;
#endif
		}

		private void ChangeBorderCursor(object sender, PointerRoutedEventArgs e)
		{
#if !WINDOWS_UWP
			var waitCursor = InputSystemCursor.Create(InputSystemCursorShape.Wait);
			this.ProtectedCursor = waitCursor;
#endif
		}

		private void CaptureCursor(object sender, PointerRoutedEventArgs e)
		{
#if !WINDOWS_UWP
			var border = (Border)sender;
			border.CapturePointer(e.Pointer);
#endif
		}
	}
}
