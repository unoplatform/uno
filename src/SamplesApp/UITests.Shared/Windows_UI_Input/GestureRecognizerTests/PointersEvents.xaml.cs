using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using System;
using System.Diagnostics;
using Windows.UI.Input;
using Windows.UI.Xaml;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizer
{
	[SampleControlInfo("Gesture recognizer", "Pointer Events")]
	public sealed partial class PointersEvents : Page
	{
		public PointersEvents()
		{
			this.InitializeComponent();

			// Those are the base pointers events from which the gestures are built
			_touchTarget.PointerEntered += (snd, e) =>
			{
				Log(e, "Entered");
			};
			_touchTarget.PointerPressed += (snd, e) =>
			{
				Log(e, "Pressed");
				CapturePointer(e.Pointer);
			};
			_touchTarget.PointerMoved += (snd, e) =>
			{
				Log(e, "Moved");
			};
			_touchTarget.PointerReleased += (snd, e) =>
			{
				Log(e, "Released");
			};
			_touchTarget.PointerCanceled += (snd, e) =>
			{
				Log(e, "Canceled");
			};
			_touchTarget.PointerExited += (snd, e) =>
			{
				Log(e, "Exited");
			};
			_touchTarget.PointerCaptureLost += (snd, e) =>
			{
				Log(e, "Capture lost");
			};

			// Those events are built using the GestureRecognizer
			_touchTarget.Tapped += (snd, e) =>
			{
				Log($"[GESTURE] Tapped: type={e.PointerDeviceType} | position={e.GetPosition(this)}");
			};
			_touchTarget.DoubleTapped += (snd, e) =>
			{
				Log($"[GESTURE] Double tapped: type={e.PointerDeviceType} | position={e.GetPosition(this)}");
			};
		}

		private void OnButtonClicked(object sender, RoutedEventArgs e)
		{
			Log("[BUTTON] Button clicked");
		}

		private void OnButtonTapped(object sender, TappedRoutedEventArgs e)
		{
			Log("[BUTTON] Button tapped");
		}

		private void Log(PointerRoutedEventArgs e, string eventName)
		{
			var point = e.GetCurrentPoint(this);
			var message = $"[POINTER] {eventName}: id={e.Pointer.PointerId} "
				+ $"| frame={point.FrameId}"
				+ $"| type={e.Pointer.PointerDeviceType} "
				+ $"| position={point.Position} "
				+ $"| rawPosition={point.RawPosition} "
				+ $"| inContact={point.IsInContact} "
				+ $"| inRange={point.Properties.IsInRange} "
				+ $"| intermediates={e.GetIntermediatePoints(this)?.Count.ToString() ?? "null"}"
				+ $"| primary={point.Properties.IsPrimary}";

			Log(message);
		}

		void Log(string message)
		{
			Debug.WriteLine(message);
			_output.Text += message + "\r\n";
		}
	}
}
