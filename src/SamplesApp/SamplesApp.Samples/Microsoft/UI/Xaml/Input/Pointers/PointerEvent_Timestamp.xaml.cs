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
	[Sample(
		"Pointers",
		Description =
		"Click the red rectangle repeatedly. You should see tickmarks in the logs (??) indicating that time delta matches timestamp delta.",
		IsManualTest = true)]
	public sealed partial class PointerEvent_Timestamp : UserControl
	{
		private ulong? _lastTimestamp;
		private uint? _lastFrameId;
		private double? _lastElapsedTime;
		private readonly Stopwatch _stopwatch = new();

		public PointerEvent_Timestamp()
		{
			this.InitializeComponent();
			TestBorder.PointerPressed += PointerEventArgsTests_PointerPressed;
			_stopwatch.Start();
			Unloaded += (s, e) => _stopwatch.Stop();
		}

		public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

		private void PointerEventArgsTests_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var point = e.GetCurrentPoint(TestBorder);
			var timestamp = point.Timestamp;
			var frameId = point.FrameId;
			var time = _stopwatch.Elapsed.TotalMicroseconds;

			var log = $"Timestamp: {timestamp}, FrameId: {frameId}" + Environment.NewLine;
			if (_lastTimestamp.HasValue)
			{
				var timeDelta = (ulong)(time - _lastElapsedTime.Value);
				var timestampDelta = (timestamp - _lastTimestamp.Value);
				log += $"Time ?: {timeDelta}";

				// As long as the delta differs by less than 100ms, it probably is correct.
				var seemsCorrect = Math.Abs((double)timeDelta - timestampDelta) < 50_000;
				log += $", Timestamp ?: {timestampDelta} {(seemsCorrect ? "??" : "?")}";

				var frameIdDelta = frameId - _lastFrameId.Value;
				log += $", FrameId ?: {frameIdDelta}";
			}
			_lastElapsedTime = time;
			_lastTimestamp = timestamp;
			_lastFrameId = frameId;
			Logs.Add(log);
		}
	}
}
