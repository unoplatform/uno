using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using System.Collections.Generic;

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample(
		"Pointers",
		IgnoreInSnapshotTests = true)]
	public sealed partial class PointersOriginalSource : Page
	{
		private object _lastManipSource;

		public PointersOriginalSource()
		{
			this.InitializeComponent();

			this.Tapped += (snd, e) => Log("TAPPED", e.OriginalSource);
			this.DoubleTapped += (snd, e) => Log("DOUBLE TAPPED", e.OriginalSource);
			this.RightTapped += (snd, e) => Log("RIGHT TAPPED", e.OriginalSource);
			this.Holding += (snd, e) => Log("HOLDING", e.OriginalSource);

			this.ManipulationStarting += (snd, e) => Log("STARTING", e.OriginalSource);
			this.ManipulationStarted += (snd, e) => Log("STARTED", e.OriginalSource);
			this.ManipulationDelta += (snd, e) => Log("DELTA", e.OriginalSource);
			this.ManipulationInertiaStarting += (snd, e) => Log("INERTIA STARTING", e.OriginalSource);
			this.ManipulationCompleted += (snd, e) => Log("COMPLETED", e.OriginalSource);
		}

		private void Log(string eventName, object originalSource)
			=> _output.Text = $"{(originalSource as FrameworkElement)?.Name ?? originalSource?.GetType().Name ?? "** null **"}: {eventName.ToUpperInvariant()}";

		private void LogManip(string eventName, object originalSource)
		{
			if (eventName == "STARTING")
			{
				_output.Text = $"{(originalSource as FrameworkElement)?.Name ?? originalSource?.GetType().Name ?? "** null **"}:";
			}
			else if (_lastManipSource != originalSource)
			{
				_output.Text += $" / {(originalSource as FrameworkElement)?.Name ?? originalSource?.GetType().Name ?? "** null **"}:";
			}

			_lastManipSource = originalSource;
			_output.Text += " " + eventName;
		}
	}
}
