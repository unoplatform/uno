using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.CaptureTests
{
	[Sample("Gesture Recognizer", "Capture with overlap")]
	public sealed partial class OverlappedControls : Page
	{
		public OverlappedControls()
		{
			this.InitializeComponent();

			_orange.PointerEntered += (snd, e) => Log(_orangeOutput, e, "Entered");
			_orange.PointerPressed += (snd, e) =>
			{
				var captured = _orange.CapturePointer(e.Pointer);
				Log(_orangeOutput, e, $"Pressed (captured: {captured})");
			};
			_orange.PointerMoved += (snd, e) =>
			{
				Log(_orangeOutput, e, "Moved");
				e.Handled = true;
			};
			_orange.PointerReleased += (snd, e) => Log(_orangeOutput, e, "Released");
			_orange.PointerCanceled += (snd, e) => Log(_orangeOutput, e, "Canceled");
			_orange.PointerExited += (snd, e) => Log(_orangeOutput, e, "Exited");
			_orange.PointerCaptureLost += (snd, e) => Log(_orangeOutput, e, "Capture lost");

			_green.PointerEntered += (snd, e) => Log(_greenOutput, e, "Entered");
			_green.PointerPressed += (snd, e) => Log(_greenOutput, e, "Pressed");
			_green.PointerMoved += (snd, e) => Log(_greenOutput, e, "Moved");
			_green.PointerReleased += (snd, e) => Log(_greenOutput, e, "Released");
			_green.PointerCanceled += (snd, e) => Log(_greenOutput, e, "Canceled");
			_green.PointerExited += (snd, e) => Log(_greenOutput, e, "Exited");
			_green.PointerCaptureLost += (snd, e) => Log(_greenOutput, e, "Capture lost");
		}

		private void Log(TextBlock output, PointerRoutedEventArgs args, string eventName)
		{
			var point = args.GetCurrentPoint(this);

			output.Text = $"[{point.FrameId:D6}] {point.Position.X:F2}x{point.Position.Y:F2} {eventName}";
		}
	}
}
