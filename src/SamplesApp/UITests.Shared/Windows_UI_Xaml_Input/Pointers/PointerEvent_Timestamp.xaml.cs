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
using System.Collections.ObjectModel;

namespace UITests.Shared.Windows_UI_Xaml_Input.Pointers
{
	[SampleControlInfo(
		"Pointers",
		Description =
		"Click the red rectangle and wait for 1 second and click again. Then see if the delta between the reported timestamps is close to 1 000 000 (1 million microseconds = 1 second).",
		IsManualTest = true)]
	public sealed partial class PointerEvent_Timestamp : UserControl
	{
		private ulong? _lastTimestamp;
		private uint? _lastFrameId;
		private DateTimeOffset? _lastTime;

		public PointerEvent_Timestamp()
		{
			this.InitializeComponent();
			TestBorder.PointerPressed += PointerEventArgsTests_PointerPressed;
		}

		public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

		private void PointerEventArgsTests_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var point = e.GetCurrentPoint(TestBorder);
			var timestamp = point.Timestamp;
			var frameId = point.FrameId;
			var time = DateTimeOffset.Now;

			var log = $"Timestamp: {timestamp}, FrameId: {frameId}" + Environment.NewLine;
			if (_lastTimestamp.HasValue)
			{
				var timeDelta = (time - _lastTime.Value).TotalMicroseconds;
				var timestampDelta = timestamp - _lastTimestamp.Value;
				log += $"Time Δ: {timeDelta}";

				var seemsCorrect = Math.Abs(timeDelta - timestampDelta) < 1000;
				log += $", Timestamp Δ: {timeDelta} {(seemsCorrect ? "✔️" : "❌")}";

				var frameIdDelta = frameId - _lastFrameId.Value;
				log += $", FrameId Δ: {frameIdDelta}";
			}
			_lastTime = time;
			_lastTimestamp = timestamp;
			_lastFrameId = frameId;
			Logs.Add(log);
		}
	}
}
