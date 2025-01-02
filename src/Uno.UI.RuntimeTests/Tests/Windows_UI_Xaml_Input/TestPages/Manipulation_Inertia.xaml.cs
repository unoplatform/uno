#pragma warning disable SYSLIB1045
using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Input;
using System.Text.RegularExpressions;

namespace RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages
{
	public sealed partial class Manipulation_Inertia : Page
	{
		public Manipulation_Inertia()
		{
			this.InitializeComponent();
		}

		private long _inertiaStart = 0;

		public event EventHandler<bool> IsRunningChanged;

		public FrameworkElement Element => _element;

		public bool IsRunning { get; private set; }

		private void ManipStarting(object sender, ManipulationStartingRoutedEventArgs e)
		{
			IsRunning = true;
			IsRunningChanged?.Invoke(this, true);
			Reset(null, null);
		}

		private void InertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
		{
			_inertiaStart = DateTimeOffset.UtcNow.Ticks;

			//e.TranslationBehavior.DesiredDisplacement = 5;
			//e.TranslationBehavior.DesiredDeceleration = .00001;
#if HAS_UNO_WINUI || WINAPPSDK
			Log(@$"[INERTIA] Inertia starting: {F(default, e.Delta, e.Cumulative, e.Velocities)}
tr: ↘={e.TranslationBehavior.DesiredDeceleration} | ⌖={e.TranslationBehavior.DesiredDisplacement}
θ: ↘={e.RotationBehavior.DesiredDeceleration} | ⌖={e.RotationBehavior.DesiredRotation}
s: ↘={e.ExpansionBehavior.DesiredDeceleration} | ⌖={e.ExpansionBehavior.DesiredExpansion}");
#endif
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

#if HAS_UNO_WINUI || WINAPPSDK
			Log(
				$"[DELTA] {F(e.Position, e.Delta, e.Cumulative, e.Velocities)} "
				+ (e.IsInertial ? $"{TimeSpan.FromTicks(DateTimeOffset.UtcNow.Ticks - _inertiaStart).TotalMilliseconds} ms" : ""));
#endif
		}

		private void ManipCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			IsRunning = false;
			IsRunningChanged?.Invoke(this, false);
		}

		private void Reset(object sender, RoutedEventArgs e)
		{
			_element.RenderTransform = new CompositeTransform();
			Output.Text = string.Empty;
		}

		public bool Validate()
		{
			Validate(null, null);
			return Output.Text.Contains("PASSED", StringComparison.OrdinalIgnoreCase);
		}

		private void Validate(object sender, RoutedEventArgs e)
		{
			var log = Output.Text;

			Output.Text = log?.IndexOf("[INERTIA]", StringComparison.OrdinalIgnoreCase) switch
			{
				null => "FAILED - Test not ran",
				<= -1 => "FAILED - No inertia detected",
				{ } i when Regex.Count((ReadOnlySpan<char>)log[i..], "\\[DELTA\\]") > 1 => "PASSED - Inertia detected",
				_ => "FAILED - No delta detected after inertia started",
			};
		}

#if HAS_UNO_WINUI || WINAPPSDK
		private static string F(Point position, ManipulationDelta delta, ManipulationDelta cumulative, ManipulationVelocities velocities)
			=> $"@=[{position.X:000.00},{position.Y:000.00}] "
				+ $"| X=(Σ:{cumulative.Translation.X:' '000.00;'-'000.00} / Δ:{delta.Translation.X:' '00.00;'-'00.00} / c:{velocities.Linear.X:F2}) "
				+ $"| Y=(Σ:{cumulative.Translation.Y:' '000.00;'-'000.00} / Δ:{delta.Translation.Y:' '00.00;'-'00.00} / c:{velocities.Linear.Y:F2}) "
				+ $"| θ=(Σ:{cumulative.Rotation:' '000.00;'-'000.00} / Δ:{delta.Rotation:' '00.00;'-'00.00} / c:{velocities.Angular:F2}) "
				+ $"| s=(Σ:{cumulative.Scale:000.00} / Δ:{delta.Scale:00.00} / c:{velocities.Expansion:F2}) "
				+ $"| e=(Σ:{cumulative.Expansion:' '000.00;'-'000.00} / Δ:{delta.Expansion:' '00.00;'-'00.00} / c:{velocities.Expansion:F2})";
#endif

		private void Log(string text)
		{
			global::System.Diagnostics.Debug.WriteLine(text);

			Output.Text += text + Environment.NewLine;
		}
	}
}
