using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture Recognizer")]
	public sealed partial class Manipulation_Basics : Page
	{
		private bool _isReady;

		public Manipulation_Basics()
		{
			this.InitializeComponent();

			_isReady = true;
			UpdateManipulation(null, null);
		}

		private void UpdateManipulation(object sender, RoutedEventArgs e)
		{
			if (!_isReady) return;

			var mode = default(ManipulationModes);

			if (ManipSystem.IsChecked ?? false)
			{
				ManipTranslateX.IsEnabled = false;
				ManipTranslateY.IsEnabled = false;
				ManipRotate.IsEnabled = false;
				ManipScale.IsEnabled = false;

				mode = ManipulationModes.System;
			}
			else
			{
				ManipTranslateX.IsEnabled = true;
				ManipTranslateY.IsEnabled = true;
				ManipRotate.IsEnabled = true;
				ManipScale.IsEnabled = true;

				if (ManipTranslateX.IsChecked ?? false)
				{
					mode |= ManipulationModes.TranslateX | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateInertia;
				}
				if (ManipTranslateY.IsChecked ?? false)
				{
					mode |= ManipulationModes.TranslateY | ManipulationModes.TranslateRailsY | ManipulationModes.TranslateInertia;
				}
				if (ManipRotate.IsChecked ?? false)
				{
					mode |= ManipulationModes.Rotate | ManipulationModes.RotateInertia;
				}
				if (ManipScale.IsChecked ?? false)
				{
					mode |= ManipulationModes.Scale | ManipulationModes.ScaleInertia;
				}
			}

			TouchTarget.ManipulationMode = mode;
		}

		/// <inheritdoc />
		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			var pt = args.GetCurrentPoint(TouchTarget);
			var position = pt.Position;

#if WINAPPSDK
			MoveOutput.Text += $"{args.Pointer.PointerId}@[{position.X:000.00},{position.Y:000.00}] ";
#else
			var raw = pt.RawPosition;
			MoveOutput.Text += $"{args.Pointer.PointerId}@[{position.X:000.00},{position.Y:000.00}][{raw.X:000.00},{raw.Y:000.00}] ";
#endif
		}

		private void OnManipStarting(object sender, ManipulationStartingRoutedEventArgs e)
			=> Write("[Starting]");

		private void OnManipStarted(object sender, ManipulationStartedRoutedEventArgs e)
			=> Write($"[Started] {F(e.Position, new ManipulationDelta { Scale = 1f }, e.Cumulative)}");

		private void OnManipDelta(object sender, ManipulationDeltaRoutedEventArgs e)
			=> Write($"[Delta] {F(e.Position, e.Delta, e.Cumulative)}");

		private void OnManipInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
			=> Write($"[Inertia] {F(new Point(-1, -1), e.Delta, e.Cumulative)}");

		private void OnManipCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
			=> Write($"[Completed] {F(e.Position, new ManipulationDelta { Scale = 1f }, e.Cumulative)}");

		private void Write(string text)
		{
			var previous = Output.Text + "\r\n" + Previous.Text;
			if (previous.Length > 8192)
			{
				previous = previous.Substring(0, 8192);
			}

			Previous.Text = previous;
			Output.Text = text;
		}

		private static string F(Point position, ManipulationDelta delta, ManipulationDelta cumulative)
			=> $"@=[{position.X:000.00},{position.Y:000.00}] "
			+ $"| X=(Σ:{cumulative.Translation.X:' '000.00;'-'000.00} / Δ:{delta.Translation.X:' '00.00;'-'00.00}) "
			+ $"| Y=(Σ:{cumulative.Translation.Y:' '000.00;'-'000.00} / Δ:{delta.Translation.Y:' '00.00;'-'00.00}) "
			+ $"| θ=(Σ:{cumulative.Rotation:' '000.00;'-'000.00} / Δ:{delta.Rotation:' '00.00;'-'00.00}) "
			+ $"| s=(Σ:{cumulative.Scale:000.00} / Δ:{delta.Scale:00.00}) "
			+ $"| e=(Σ:{cumulative.Expansion:' '000.00;'-'000.00} / Δ:{delta.Expansion:' '00.00;'-'00.00})";
	}
}
