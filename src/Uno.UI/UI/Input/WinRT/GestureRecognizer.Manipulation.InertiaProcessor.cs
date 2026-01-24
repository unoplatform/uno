// This file is included in both Uno.UWP and Uno.UI
#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Uno.Extensions;
using Uno.Foundation.Logging;
using static System.Double;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

#if IS_UNO_UI_PROJECT
using Microsoft.UI.Composition;

namespace Microsoft.UI.Input;
#else
namespace Windows.UI.Input;
#endif

public partial class GestureRecognizer
{
	internal partial class Manipulation
	{
		internal class InertiaProcessor : IDisposable
		{
			private const double _defaultDurationMs = 1000;

			private delegate double Get(in ManipulationDelta delta);
			private delegate void Set(ref ManipulationDelta delta, double value);
			private record struct ManipulationDeltaProperty(Get Get, Set Set);

			private static readonly ManipulationDeltaProperty TranslationX = new(
				Get: static (in ManipulationDelta m) => m.Translation.X,
				Set: static (ref ManipulationDelta m, double value) => m.Translation.X = value);

			private static readonly ManipulationDeltaProperty TranslationY = new(
				Get: static (in ManipulationDelta m) => m.Translation.Y,
				Set: static (ref ManipulationDelta m, double value) => m.Translation.Y = value);

			private static readonly ManipulationDeltaProperty Rotation = new(
				Get: static (in ManipulationDelta m) => m.Rotation,
				Set: static (ref ManipulationDelta m, double value) => m.Rotation = (float)value);

			private static readonly ManipulationDeltaProperty Expansion = new(
				Get: static (in ManipulationDelta m) => m.Expansion,
				Set: static (ref ManipulationDelta m, double value) => m.Expansion = (float)value);

			private readonly Manipulation _owner;
			private readonly ulong _t0;
			private readonly Point _position0;
			private readonly ManipulationDelta _cumulative0; // Cumulative of the whole manipulation at t=0
			private readonly ManipulationVelocities _velocities0;

			private IInertiaProcessorTimer? _timer;
			private ManipulationState _stateOnLastTick;

			private bool _isTranslateInertiaXEnabled;
			private bool _isTranslateInertiaYEnabled;
			private bool _isRotateInertiaEnabled;
			private bool _isScaleInertiaEnabled;

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

			/// <summary>
			/// Attempts to start the inertia processor for the given manipulation.
			/// </summary>
			/// <param name="owner">The manipulation instance that owns this inertia processor.</param>
			/// <param name="processor">A reference to the inertia processor. If inertia is successfully started, this will be set to a new instance.</param>
			/// <param name="changeSet">The set of changes that occurred during the manipulation.</param>
			/// <returns>True if inertia was successfully started; otherwise, false.</returns>
			/// <remarks>
			/// Inertia can only be enabled if the following preconditions are met:
			/// <list type="bullet">
			/// <item><description>The <paramref name="processor"/> is null.</description></item>
			/// <item><description>The manipulation is not a drag manipulation (<see cref="Manipulation.IsDragManipulation"/>).</description></item>
			/// <item><description>The manipulation settings include inertia (<see cref="GestureSettingsHelper.Inertia"/>).</description></item>
			/// <item><description>The manipulation settings include manipulations (<see cref="GestureSettingsHelper.Manipulations"/>).</description></item>
			/// </list>
			/// Additionally, inertia for specific properties (e.g., translation, rotation, expansion) is enabled based on the velocities and thresholds defined in the manipulation settings.
			/// </remarks>
			public static bool TryStart(Manipulation owner, [NotNullWhen(true)] ref InertiaProcessor? processor, ManipulationChangeSet changeSet)
			{
				if (processor is not null
					|| owner.IsDragManipulation
					|| (owner._settings & GestureSettingsHelper.Inertia) is 0
					|| (owner._settings & GestureSettingsHelper.Manipulations) is 0) // On pointer removed, we should not start inertia if all manip are disabled (could happen if configured for drag but IsDragManipulation not yet true)
				{
					return false;
				}

				var velocities = changeSet.Velocities;
				var isTranslateInertiaXEnabled = owner._isTranslateXEnabled
					&& owner._settings.HasFlag(Input.GestureSettings.ManipulationTranslateInertia)
					&& Abs(velocities.Linear.X) > owner._inertiaThresholds.TranslateX;
				var isTranslateInertiaYEnabled = owner._isTranslateYEnabled
					&& owner._settings.HasFlag(Input.GestureSettings.ManipulationTranslateInertia)
					&& Abs(velocities.Linear.Y) > owner._inertiaThresholds.TranslateY;
				var isRotateInertiaEnabled = owner._isRotateEnabled
					&& owner._settings.HasFlag(Input.GestureSettings.ManipulationRotateInertia)
					&& Abs(velocities.Angular) > owner._inertiaThresholds.Rotate;
				var isScaleInertiaEnabled = owner._isScaleEnabled
					&& owner._settings.HasFlag(Input.GestureSettings.ManipulationScaleInertia)
					&& Abs(velocities.Expansion) > owner._inertiaThresholds.Expansion;

				if (!isTranslateInertiaXEnabled && !isTranslateInertiaYEnabled && !isRotateInertiaEnabled && !isScaleInertiaEnabled)
				{
					return false;
				}

				// For better experience, as soon inertia kicked-in on an axis, we bypass threshold on the second axis.
				isTranslateInertiaXEnabled |= isTranslateInertiaYEnabled && owner._isTranslateXEnabled;
				isTranslateInertiaYEnabled |= isTranslateInertiaXEnabled && owner._isTranslateYEnabled;

				processor = new InertiaProcessor(owner, changeSet.Timestamp, changeSet.Position, changeSet.Cumulative, changeSet.Velocities)
				{
					_isTranslateInertiaXEnabled = isTranslateInertiaXEnabled,
					_isTranslateInertiaYEnabled = isTranslateInertiaYEnabled,
					_isRotateInertiaEnabled = isRotateInertiaEnabled,
					_isScaleInertiaEnabled = isScaleInertiaEnabled,
				};
				return true;
			}

			private InertiaProcessor(Manipulation owner, ulong timestamp, Point position, ManipulationDelta cumulative, ManipulationVelocities velocities)
			{
				_owner = owner;
				_t0 = timestamp;
				_position0 = position;
				_cumulative0 = cumulative;
				_velocities0 = velocities;

				_stateOnLastTick = new ManipulationState(timestamp, position, cumulative);
			}

			public bool IsRunning => _timer?.IsRunning ?? false;

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

			public void Start(bool useCompositionTimer)
			{
				if (_timer is not null)
				{
					return;
				}

				CompleteConfiguration();

#if IS_UNO_UI_PROJECT
				_timer = useCompositionTimer
					? new CompositionInertiaProcessorTimer(Process)
					: new DispatcherInertiaProcessorTimer(Process);
#else
				_timer = new DispatcherInertiaProcessorTimer(Process);
#endif

				if (this.Log().IsDebugEnabled())
				{
					this.Log().Debug($"InertiaProcessor started with using timer {_timer.GetType().Name} ("
						+ $"tr: (x={_isTranslateInertiaXEnabled}|x={_isTranslateInertiaYEnabled})/(x={_velocities0.Linear.X}|y={_velocities0.Linear.Y})/{DesiredDisplacementDeceleration}/{DesiredDisplacement} "
						+ $"| rot: {_isRotateInertiaEnabled}/{_velocities0.Angular}/{DesiredRotationDeceleration}/{DesiredDisplacement} "
						+ $"| scale: {_isScaleInertiaEnabled}/{_velocities0.Expansion}/{DesiredExpansionDeceleration}/{DesiredExpansion})");
				}

				_timer.Start();
			}

			internal ManipulationState GetStateOnLastTick() => _stateOnLastTick;

			private void Process(TimeSpan elapsed)
			{
				// First we update the internal state (i.e. the current cumulative manip delta for the current time)
				var previous = _stateOnLastTick.Cumulative;
				var current = previous;
				var updated = UpdateCumulative(elapsed.TotalMilliseconds, ref current);

				_stateOnLastTick = new(_t0 + (ulong)elapsed.TotalMicroseconds, _position0 + current.Translation, current);

				// Then we request to the owner to raise its events (will cause the GetCumulative())
				// We notify in any cases in order to make sure to raise at least one ManipDelta (even if Delta.IsEmpty ^^) before stop the processor
				_owner.NotifyUpdate();

				if (!updated)
				{
					_timer?.Stop();
					_owner.NotifyUpdate();
				}
			}

			private bool UpdateCumulative(double t, ref ManipulationDelta cumulative)
			{
				var isUpdated = false;
				isUpdated |= _isTranslateInertiaXEnabled && Update(TranslationX, ref cumulative, _velocities0.Linear.X, DesiredDisplacementDeceleration, t);
				isUpdated |= _isTranslateInertiaYEnabled && Update(TranslationY, ref cumulative, _velocities0.Linear.Y, DesiredDisplacementDeceleration, t);
				isUpdated |= _isRotateInertiaEnabled && Update(Rotation, ref cumulative, _velocities0.Angular, DesiredRotationDeceleration, t);
				if (_isScaleInertiaEnabled && Update(Expansion, ref cumulative, _velocities0.Expansion, DesiredExpansionDeceleration, t))
				{
					cumulative.Scale = (_owner._origins.State.Distance + cumulative.Expansion) / _owner._origins.State.Distance;
					isUpdated = true;
				}

				return isUpdated;
			}

			private bool Update(ManipulationDeltaProperty property, ref ManipulationDelta cumulative, double v0, double d, double t)
			{
				if (IsCompleted(v0, d, t))
				{
					return false;
				}

				var current = property.Get(in cumulative);
				var updated = property.Get(in _cumulative0) + GetValue(v0, d, t);
				if (current == updated)
				{
					return false;
				}

				property.Set(ref cumulative, updated);
				return true;
			}

			// https://docs.microsoft.com/en-us/windows/win32/wintouch/inertia-mechanics#inertia-physics-overview
			internal static double GetValue(double v0, double d, double t)
				=> v0 >= 0
					? v0 * t - d * Math.Pow(t, 2)
					: -(-v0 * t - d * Math.Pow(t, 2));

			private static bool IsCompleted(double v0, double d, double t)
				=> Math.Abs(v0) - d * 2 * t <= 0; // The derivative of the GetValue function

			internal static double GetCompletionTime(double v0, double d)
				=> Math.Abs(v0) / (2 * d);

			internal static double GetDecelerationFromDesiredDuration(double v0, double durationMs)
				=> Math.Abs(v0) / (2 * durationMs);

			private static double GetDecelerationFromDesiredDisplacement(double v0, double displacement, double durationMs = _defaultDurationMs)
				=> (v0 * durationMs - displacement) / Math.Pow(_defaultDurationMs, 2);

			/// <inheritdoc />
			public void Dispose()
				=> _timer?.Stop();

#if IS_UNIT_TESTS
			/// <summary>
			/// For test purposes only!
			/// </summary>
			public void RunSync(double framePerSeconds = 25)
			{
				var frameDuration = 1000 / framePerSeconds;
				var time = frameDuration;
				while (IsRunning)
				{
					Process(TimeSpan.FromMilliseconds(time));
					time += frameDuration;
				}
			}
#endif
		}

		private interface IInertiaProcessorTimer
		{
			public bool IsRunning { get; }
			public void Start();

			public void Stop();
		}

		private sealed class DispatcherInertiaProcessorTimer : IInertiaProcessorTimer
		{
			private readonly DispatcherQueueTimer _timer;

			public const double DefaultFramePerSeconds = 30d;

			public DispatcherInertiaProcessorTimer(Action<TimeSpan> OnTick, double framePerSeconds = DefaultFramePerSeconds)
			{
				_timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
				_timer.Interval = TimeSpan.FromMilliseconds(1000d / framePerSeconds);
				_timer.IsRepeating = true;
				_timer.Tick += (snd, e) => OnTick(snd.LastTickElapsed);
			}

			public bool IsRunning => _timer.IsRunning;

			public void Start() => _timer.Start();
			public void Stop() => _timer.Stop();
		}

#if IS_UNO_UI_PROJECT
		private sealed class CompositionInertiaProcessorTimer(Action<TimeSpan> onTick) : IInertiaProcessorTimer
		{
			private EventHandler<object>? _handler;
			private Stopwatch? _time;

			public bool IsRunning => _handler is not null;

			public void Start()
			{
				Stop();

				_time = Stopwatch.StartNew();
				_handler = (_, args) =>
				{
					// Note: We are not using the ((Microsoft.UI.Xaml.Media.RenderingEventArgs)args).RenderingTime as we are not able to have the value at t0
					onTick(_time.Elapsed);
				};

				CompositionTarget.Rendering += _handler;
			}

			public void Stop()
			{
				if (_handler is not null)
				{
					CompositionTarget.Rendering -= _handler;
					_handler = null;
				}
			}

			~CompositionInertiaProcessorTimer() => Stop();
		}
#endif
	}
}
