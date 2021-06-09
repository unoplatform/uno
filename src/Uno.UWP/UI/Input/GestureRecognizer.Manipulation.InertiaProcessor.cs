using System;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Uno.Extensions;

namespace Windows.UI.Input
{
	public partial class GestureRecognizer
	{
		internal partial class Manipulation
		{
			internal class InertiaProcessor : IDisposable
			{
				// TODO: We should somehow sync tick with frame rendering
				const double framePerSecond = 25;
				const double durationTicks = 1.5 * TimeSpan.TicksPerSecond;

				private readonly DispatcherQueueTimer _timer;
				private readonly Manipulation _owner;
				private readonly ManipulationDelta _initial;

				private readonly bool _isTranslateInertiaXEnabled;
				private readonly bool _isTranslateInertiaYEnabled;
				private readonly bool _isRotateInertiaEnabled;
				private readonly bool _isScaleInertiaEnabled;

				// Those values can be customized by the application through the ManipInertiaStartingArgs.Inertia<Tr|Rot|Exp>Behavior
				public double DesiredDisplacement;
				public double DesiredDisplacementDeceleration;
				public double DesiredRotation;
				public double DesiredRotationDeceleration;
				public double DesiredExpansion;
				public double DesiredExpansionDeceleration;

				public InertiaProcessor(Manipulation owner, ManipulationDelta cumulative, ManipulationVelocities velocities)
				{
					_owner = owner;
					_initial = cumulative;

					_isTranslateInertiaXEnabled = _owner._isTranslateXEnabled && _owner._settings.HasFlag(Input.GestureSettings.ManipulationTranslateInertia);
					_isTranslateInertiaYEnabled = _owner._isTranslateYEnabled && _owner._settings.HasFlag(Input.GestureSettings.ManipulationTranslateInertia);
					_isRotateInertiaEnabled = _owner._isRotateEnabled && _owner._settings.HasFlag(Input.GestureSettings.ManipulationRotateInertia);
					_isScaleInertiaEnabled = _owner._isScaleEnabled && _owner._settings.HasFlag(Input.GestureSettings.ManipulationScaleInertia);

					_timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
					_timer.Interval = TimeSpan.FromMilliseconds(1000d / framePerSecond);
					_timer.IsRepeating = true;
					_timer.Tick += OnTick;

					// TODO
					DesiredDisplacement = _isTranslateInertiaXEnabled || _isTranslateInertiaYEnabled ? 300 : 0;
					DesiredRotation = _isRotateInertiaEnabled ? 60 : 0;
					DesiredExpansion = _isScaleInertiaEnabled ? 200 : 0;
				}

				public bool IsRunning => _timer.IsRunning;

				/// <summary>
				/// Gets the elapsed time of the inertia (cf. Remarks)
				/// </summary>
				/// <remarks>
				/// Depending of the platform, the timestamp provided by pointer events might not be absolute,
				/// so it's preferable to not compare timestamp between pointers and inertia processor.
				/// </remarks>
				public long Elapsed => _timer.LastTickElapsed.Ticks;

				public void Start()
					=> _timer.Start();

				public ManipulationDelta GetCumulative()
				{
					var progress = 1 - Math.Pow(1 - GetNormalizedTime(), 4); // Source: https://easings.net/#easeOutQuart

					var translateX = _isTranslateInertiaXEnabled ? _initial.Translation.X + progress * DesiredDisplacement : 0;
					var translateY = _isTranslateInertiaYEnabled ? _initial.Translation.Y + progress * DesiredDisplacement : 0;
					var rotate = _isRotateInertiaEnabled ? _initial.Rotation + progress * DesiredRotation : 0;
					var expansion = _isScaleInertiaEnabled ? _initial.Expansion + progress * DesiredExpansion : 0;

					var scale = (_owner._origins.Distance + expansion) / _owner._origins.Distance;

					return new ManipulationDelta
					{
						Translation = new Point(translateX, translateY),
						Rotation = (float)MathEx.NormalizeDegree(rotate),
						Scale = (float)scale,
						Expansion = (float)expansion
					};
				}

				private double GetNormalizedTime()
				{
					var elapsed = _timer.LastTickElapsed;
					var normalizedTime = elapsed.Ticks / durationTicks;

					return normalizedTime;
				}

				private void OnTick(DispatcherQueueTimer sender, object args)
				{
					_owner.NotifyUpdate();

					if (GetNormalizedTime() >= 1)
					{
						_timer.Stop();
						_owner.NotifyUpdate();
					}
				}

				/// <inheritdoc />
				public void Dispose()
					=> _timer.Stop();
			}
		}
	}
}
