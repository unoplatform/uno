// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
#nullable enable

using System;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Uno.Extensions;
using static System.Math;
using static System.Double;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial class GestureRecognizer
	{
		internal partial class Manipulation
		{
			internal class InertiaProcessor : IDisposable
			{
				// TODO: We should somehow sync tick with frame rendering
				private const double _framesPerSecond = 25;
				private const double _defaultDurationMs = 1000;

				private readonly DispatcherQueueTimer _timer;
				private readonly Manipulation _owner;
				private readonly Point _position0;
				private readonly ManipulationDelta _cumulative0; // Cumulative of the whole manipulation at t=0
				private readonly ManipulationVelocities _velocities0;

				private ManipulationDelta _inertiaCumulative; // Cumulative of the inertia only at last tick

				private readonly bool _isTranslateInertiaXEnabled;
				private readonly bool _isTranslateInertiaYEnabled;
				private readonly bool _isRotateInertiaEnabled;
				private readonly bool _isScaleInertiaEnabled;

				internal const double DefaultDesiredDisplacementDeceleration = .001;
				internal const double DefaultDesiredRotationDeceleration = .0001;
				internal const double DefaultDesiredExpansionDeceleration = .001;

				// Those values can be customized by the application through the ManipInertiaStartingArgs.Inertia<Tr|Rot|Exp>Behavior
				public double DesiredDisplacement = NaN;
				public double DesiredDisplacementDeceleration = NaN;
				public double DesiredRotation = NaN;
				public double DesiredRotationDeceleration = NaN;
				public double DesiredExpansion = NaN;
				public double DesiredExpansionDeceleration = NaN;

				public InertiaProcessor(Manipulation owner, Point position, ManipulationDelta cumulative, ManipulationVelocities velocities)
				{
					_owner = owner;
					_position0 = position;
					_cumulative0 = cumulative;
					_velocities0 = velocities;

					_isTranslateInertiaXEnabled = _owner._isTranslateXEnabled
						&& _owner._settings.HasFlag(Input.GestureSettings.ManipulationTranslateInertia)
						&& Math.Abs(velocities.Linear.X) > _owner._inertiaThresholds.TranslateX;
					_isTranslateInertiaYEnabled = _owner._isTranslateYEnabled
						&& _owner._settings.HasFlag(Input.GestureSettings.ManipulationTranslateInertia)
						&& Math.Abs(velocities.Linear.Y) > _owner._inertiaThresholds.TranslateY;
					_isRotateInertiaEnabled = _owner._isRotateEnabled
						&& _owner._settings.HasFlag(Input.GestureSettings.ManipulationRotateInertia)
						&& Abs(velocities.Angular) > _owner._inertiaThresholds.Rotate;
					_isScaleInertiaEnabled = _owner._isScaleEnabled
						&& _owner._settings.HasFlag(Input.GestureSettings.ManipulationScaleInertia)
						&& Abs(velocities.Expansion) > _owner._inertiaThresholds.Expansion;

					global::System.Diagnostics.Debug.Assert(_isTranslateInertiaXEnabled || _isTranslateInertiaYEnabled || _isRotateInertiaEnabled || _isScaleInertiaEnabled);

					// For better experience, as soon inertia kicked-in on an axis, we bypass threshold on the second axis.
					_isTranslateInertiaXEnabled |= _isTranslateInertiaYEnabled && _owner._isTranslateXEnabled;
					_isTranslateInertiaYEnabled |= _isTranslateInertiaXEnabled && _owner._isTranslateYEnabled;

					_timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
					_timer.Interval = TimeSpan.FromMilliseconds(1000d / _framesPerSecond);
					_timer.IsRepeating = true;
					_timer.Tick += (snd, e) => Process(snd.LastTickElapsed.TotalMilliseconds);
				}

				public bool IsRunning => _timer.IsRunning;

				/// <summary>
				/// Gets the elapsed time of the inertia (cf. Remarks)
				/// </summary>
				/// <remarks>
				/// Depending of the platform, the timestamp provided by pointer events might not be absolute,
				/// so it's preferable to not compare timestamp between pointers and inertia processor.
				/// </remarks>
				public TimeSpan Elapsed => _timer.LastTickElapsed;

				private void CompleteConfiguration()
				{
					// Be aware this method will be invoked twice in case of GetNextCumulative().

					// As of 2021-07-21, according to test, Displacement takes over Deceleration.
					if (!IsNaN(DesiredDisplacement))
					{
						var v0 = (Math.Abs(_velocities0.Linear.X) + Math.Abs(_velocities0.Linear.Y)) / 2;
						DesiredDisplacementDeceleration = GetDecelerationFromDesiredDisplacement(v0, DesiredDisplacement);
					}
					if (!IsNaN(DesiredRotation))
					{
						DesiredRotationDeceleration = GetDecelerationFromDesiredDisplacement(_velocities0.Angular, DesiredRotation);
					}
					if (!IsNaN(DesiredExpansion))
					{
						DesiredExpansionDeceleration = GetDecelerationFromDesiredDisplacement(_velocities0.Expansion, DesiredExpansion);
					}

					// Default values are **inspired** by https://docs.microsoft.com/en-us/windows/win32/wintouch/inertia-mechanics#smooth-object-animation-using-the-velocity-and-deceleration-properties
					if (IsNaN(DesiredDisplacementDeceleration))
					{
						DesiredDisplacementDeceleration = DefaultDesiredDisplacementDeceleration;
					}
					if (IsNaN(DesiredRotationDeceleration))
					{
						DesiredRotationDeceleration = DefaultDesiredRotationDeceleration;
					}
					if (IsNaN(DesiredExpansionDeceleration))
					{
						DesiredExpansionDeceleration = DefaultDesiredExpansionDeceleration;
					}
				}

				public void Start()
				{
					CompleteConfiguration();
					_timer.Start();
				}

				private void Process(double t)
				{
					// First we update the internal state (i.e. the current cumulative manip delta for the current time)
					var previous = _inertiaCumulative;
					var current = GetInertiaCumulative(t, previous);

					_inertiaCumulative = current;

					// Then we request to the owner to raise its events (will cause the GetCumulative())
					// We notify in any cases in order to make sure to raise at least one ManipDelta (even if Delta.IsEmpty ^^) before stop the processor
					_owner.NotifyUpdate();

					if (previous.Translation.X == current.Translation.X
						&& previous.Translation.Y == current.Translation.Y
						&& previous.Rotation == current.Rotation
						&& previous.Expansion == current.Expansion) // Note: we DO NOT compare the scaling, expansion is enough here!
					{
						_timer.Stop();
						_owner.NotifyUpdate();
					}
				}

				public Point GetPosition()
					=> _position0 + _inertiaCumulative.Translation;

				/// <summary>
				/// Gets the cumulative delta, including the manipulation cumulative when this processor was started
				/// </summary>
				public ManipulationDelta GetCumulative()
					=> _cumulative0.Add(_inertiaCumulative);

				/// <summary>
				/// Gets the **expected** cumulative delta at the tick expected close to the given time, including the manipulation cumulative when this processor was started.
				/// </summary>
				/// <remarks>This is only the expected value, actual value might be slightly different depending on how precise is the underlying timer.</remarks>
				public ManipulationDelta GetCumulativeTickAligned(ref TimeSpan dueIn, out int ticks)
				{
					CompleteConfiguration(); // Make sure to compute desired values in case this method is being invoked during the OnManipulationInertiaStarting event

					var interval = _timer.Interval;
					if (dueIn <= TimeSpan.Zero)
					{
						ticks = 1;
						dueIn = interval;
					}
					else
					{
						var linearX = GetCompletionTime(_isTranslateInertiaXEnabled, _velocities0.Linear.X, DesiredDisplacementDeceleration);
						var linearY = GetCompletionTime(_isTranslateInertiaYEnabled, _velocities0.Linear.Y, DesiredDisplacementDeceleration);
						var angular = GetCompletionTime(_isRotateInertiaEnabled, _velocities0.Angular, DesiredRotationDeceleration);
						var expansion = GetCompletionTime(_isScaleInertiaEnabled, _velocities0.Expansion, DesiredExpansionDeceleration);

						var maxTicks = Math.Max(linearX, Math.Max(linearY, Math.Max(angular, expansion))) * TimeSpan.TicksPerMillisecond;
						var dueInTicks = Math.Min(dueIn.Ticks, maxTicks);

						ticks = Math.Max(1, (int)Math.Round(dueInTicks / interval.Ticks));
						dueIn = ticks * interval;
					}

					var inertiaCumulative = GetInertiaCumulative((_timer.LastTickElapsed + dueIn).TotalMilliseconds, _inertiaCumulative);

					return _cumulative0.Add(inertiaCumulative);
				}

				private ManipulationDelta GetInertiaCumulative(double t, ManipulationDelta previousCumulative)
				{
					var linearX = GetValue(_isTranslateInertiaXEnabled, _velocities0.Linear.X, DesiredDisplacementDeceleration, t, (float)previousCumulative.Translation.X);
					var linearY = GetValue(_isTranslateInertiaYEnabled, _velocities0.Linear.Y, DesiredDisplacementDeceleration, t, (float)previousCumulative.Translation.Y);
					var angular = GetValue(_isRotateInertiaEnabled, _velocities0.Angular, DesiredRotationDeceleration, t, previousCumulative.Rotation);
					var expansion = GetValue(_isScaleInertiaEnabled, _velocities0.Expansion, DesiredExpansionDeceleration, t, previousCumulative.Expansion);

					var scale = _isScaleInertiaEnabled ? (_owner._origins.State.Distance + expansion) / _owner._origins.State.Distance : 1;

					var delta = new ManipulationDelta
					{
						Translation = new Point(linearX, linearY),
						Rotation = angular,
						Expansion = expansion,
						Scale = scale
					};

					return delta;
				}

				private float GetValue(bool enabled, double v0, double d, double t, float lastValue)
					=> (enabled, IsCompleted(v0, d, t)) switch
					{
						(false, _) => 0,
						(_, true) => lastValue, // Avoid bounce effect by replaying the last value
						(true, false) => GetValue(v0, d, t)
					};

				// https://docs.microsoft.com/en-us/windows/win32/wintouch/inertia-mechanics#inertia-physics-overview
				private float GetValue(double v0, double d, double t)
					=> v0 >= 0
						? (float)(v0 * t - d * Math.Pow(t, 2))
						: -(float)(-v0 * t - d * Math.Pow(t, 2));

				private bool IsCompleted(double v0, double d, double t)
					=> Math.Abs(v0) - d * 2 * t <= 0; // The derivative of the GetValue function

				private double GetCompletionTime(bool enabled, double v0, double d)
					=> enabled ? GetCompletionTime(v0, d) : 0;

				private double GetCompletionTime(double v0, double d)
					=> Math.Abs(v0) / (2 * d);

				internal static double GetDecelerationFromDesiredDuration(double v0, double durationMs)
					=> Math.Abs(v0) / (2 * durationMs);

				private static double GetDecelerationFromDesiredDisplacement(double v0, double displacement, double durationMs = _defaultDurationMs)
					=> (v0 * durationMs - displacement) / Math.Pow(_defaultDurationMs, 2);

				/// <inheritdoc />
				public void Dispose()
					=> _timer.Stop();

#if IS_UNIT_TESTS
				/// <summary>
				/// For test purposes only!
				/// </summary>
				public void RunSync()
				{
					var frameDuration = 1000 / _framesPerSecond;
					var time = frameDuration;
					while (IsRunning)
					{
						Process(time);
						time += frameDuration;
					}
				}
#endif
			}
		}
	}
}
#endif
