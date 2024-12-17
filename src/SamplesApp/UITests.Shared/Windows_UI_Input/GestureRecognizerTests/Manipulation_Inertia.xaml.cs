using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace UITests.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture Recognizer")]
	public sealed partial class Manipulation_Inertia : Page
	{
		public Manipulation_Inertia()
		{
			this.InitializeComponent();
		}

		private long _inertiaStart = 0;

		private void InertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
		{
			_inertiaStart = DateTimeOffset.UtcNow.Ticks;

			//e.TranslationBehavior.DesiredDisplacement = 5;
			//e.TranslationBehavior.DesiredDeceleration = .00001;

			Log(@$"[INERTIA] Inertia starting: {F(default, e.Delta, e.Cumulative, e.Velocities)}
tr: ↘={e.TranslationBehavior.DesiredDeceleration} | ⌖={e.TranslationBehavior.DesiredDisplacement}
θ: ↘={e.RotationBehavior.DesiredDeceleration} | ⌖={e.RotationBehavior.DesiredRotation}
s: ↘={e.ExpansionBehavior.DesiredDeceleration} | ⌖={e.ExpansionBehavior.DesiredExpansion}");
		}

		private void ManipDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (!(_element.RenderTransform is CompositeTransform tr))
			{
				_element.RenderTransform = tr = new CompositeTransform();
			}

			tr.TranslateX = e.Cumulative.Translation.X;
			tr.TranslateY = e.Cumulative.Translation.Y;
			tr.Rotation = e.Cumulative.Rotation;
			tr.ScaleX = e.Cumulative.Scale;
			tr.ScaleY = e.Cumulative.Scale;

			Log(
				$"[DELTA] {F(e.Position, e.Delta, e.Cumulative, e.Velocities)} "
				+ (e.IsInertial ? $"{TimeSpan.FromTicks(DateTimeOffset.UtcNow.Ticks - _inertiaStart).TotalMilliseconds} ms" : ""));
		}

		private void Reset(object sender, RoutedEventArgs e)
		{
			_element.RenderTransform = new CompositeTransform();
		}

		private static string F(Point position, ManipulationDelta delta, ManipulationDelta cumulative, ManipulationVelocities velocities)
			=> $"@=[{position.X:000.00},{position.Y:000.00}] "
				+ $"| X=(Σ:{cumulative.Translation.X:' '000.00;'-'000.00} / Δ:{delta.Translation.X:' '00.00;'-'00.00} / c:{velocities.Linear.X:F2}) "
				+ $"| Y=(Σ:{cumulative.Translation.Y:' '000.00;'-'000.00} / Δ:{delta.Translation.Y:' '00.00;'-'00.00} / c:{velocities.Linear.Y:F2}) "
				+ $"| θ=(Σ:{cumulative.Rotation:' '000.00;'-'000.00} / Δ:{delta.Rotation:' '00.00;'-'00.00} / c:{velocities.Angular:F2}) "
				+ $"| s=(Σ:{cumulative.Scale:000.00} / Δ:{delta.Scale:00.00} / c:{velocities.Expansion:F2}) "
				+ $"| e=(Σ:{cumulative.Expansion:' '000.00;'-'000.00} / Δ:{delta.Expansion:' '00.00;'-'00.00} / c:{velocities.Expansion:F2})";

		private static void Log(string text)
			=> global::System.Diagnostics.Debug.WriteLine(text);
	}
}
