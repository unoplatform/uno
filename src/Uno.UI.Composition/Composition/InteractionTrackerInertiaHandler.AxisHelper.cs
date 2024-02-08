#nullable enable

using System;
using System.Numerics;

namespace Microsoft.UI.Composition.Interactions;

internal sealed partial class InteractionTrackerInertiaHandler
{
	private enum Axis
	{
		X,
		Y,
		Z
	}

	private sealed class AxisHelper
	{
		private float? _dampingStateTimeInSeconds;
		private float? _dampingStatePosition;

		internal InteractionTrackerInertiaHandler Handler { get; }
		internal float DecayRate { get; }
		internal float InitialVelocity { get; }
		internal float InitialValue { get; }
		internal float FinalValue { get; }
		internal float FinalModifiedValue { get; }
		internal float TimeToMinimumVelocity { get; }
		internal Axis Axis { get; }

		internal bool HasCompleted { get; private set; }

		public AxisHelper(InteractionTrackerInertiaHandler handler, Vector3 velocities, Axis axis)
		{
			Axis = axis;
			Handler = handler;
			InitialVelocity = GetValue(velocities);
			DecayRate = GetValue(Handler._interactionTracker.PositionInertiaDecayRate ?? new(0.95f));
			InitialValue = GetValue(Handler._interactionTracker.Position);

			TimeToMinimumVelocity = GetTimeToMinimumVelocity();

			var deltaPosition = CalculateDeltaPosition(TimeToMinimumVelocity);

			FinalValue = InitialValue + deltaPosition;
			FinalModifiedValue = Math.Clamp(FinalValue, GetValue(Handler._interactionTracker.MinPosition), GetValue(Handler._interactionTracker.MaxPosition));
		}

		private float GetValue(Vector3 vector)
		{
			return Axis switch
			{
				Axis.X => vector.X,
				Axis.Y => vector.Y,
				Axis.Z => vector.Z,
				_ => throw new ArgumentException("Invalid value for axis.")
			};
		}

		private float GetTimeToMinimumVelocity()
		{
			var epsilon = 0.0000011920929f;

			var minimumVelocity = 50.0f;

			return TimeToMinimumVelocityCore(MathF.Abs(InitialVelocity), 1 - DecayRate, InitialValue);

			float TimeToMinimumVelocityCore(float initialVelocity, float decayRate, float initialPosition)
			{
				var time = 0.0f;
				if (initialVelocity > minimumVelocity)
				{
					if (!IsCloseReal(decayRate, 1.0f, epsilon))
					{
						if (IsCloseRealZero(decayRate, epsilon) /*|| !_isInertiaEnabled*/)
						{
							return 0.0f;
						}
						else
						{
							return (MathF.Log(minimumVelocity) - MathF.Log(initialVelocity)) / MathF.Log(decayRate);
						}
					}

					time = (Math.Sign(initialVelocity) * float.MaxValue - initialPosition) / initialVelocity;

					if (time < 0.0f)
					{
						return 0.0f;
					}
				}

				return time;
			}
		}

		private float CalculateDeltaPosition(float time)
		{
			var decayRate = 1.0f - DecayRate;
			float epsilon = 0.0000011920929f;

			if (IsCloseReal(decayRate, 1.0f, epsilon))
			{
				return InitialVelocity * time;
			}
			else if (IsCloseRealZero(decayRate, epsilon) /*|| !_isInertiaEnabled*/)
			{
				return 0.0f;
			}
			else
			{
				float val = MathF.Pow(decayRate, time);
				return ((val - 1.0f) * InitialVelocity) / MathF.Log(decayRate);
			}
		}

		private float CalculatePosition(float t)
		{
			if (t >= TimeToMinimumVelocity)
			{
				return _dampingStateTimeInSeconds.HasValue ? FinalModifiedValue : FinalValue;
			}

			if (_dampingStateTimeInSeconds.HasValue)
			{
				var settlingTime = TimeToMinimumVelocity - _dampingStateTimeInSeconds.Value;
				var wn = 5.8335 / settlingTime;
				// It seems WinUI can use an underdamped animation in some cases. For now we only use critically damped animation.
				var value = DampingHelper.SolveCriticallyDamped(wn, t - _dampingStateTimeInSeconds.Value);
				value = value * (FinalModifiedValue - _dampingStatePosition!.Value) + _dampingStatePosition.Value;

				return (float)value;
			}

			var currentPosition = GetValue(Handler._interactionTracker.Position);
			var minPosition = GetValue(Handler._interactionTracker.MinPosition);
			var maxPosition = GetValue(Handler._interactionTracker.MaxPosition);
			var decayRate = 1.0f - DecayRate;
			if (currentPosition < minPosition || currentPosition > maxPosition)
			{
				// This is an overpan from Interacting state. Use damping animation.
				_dampingStateTimeInSeconds = Handler._stopwatch!.ElapsedMilliseconds / 1000.0f;
				_dampingStatePosition = currentPosition;
			}

			var valueAtZero = myFunc(0);
			var valueAtEnd = myFunc(TimeToMinimumVelocity) - valueAtZero;

			var scaleFactor = (FinalValue - InitialValue) / valueAtEnd;

			return (myFunc(t) - valueAtZero) * scaleFactor + InitialValue;

			float myFunc(float t)
			{
				var term1 = MathF.Pow(decayRate, 2 * t) / (2 * MathF.Log(decayRate));
				var term2 = MathF.Pow(decayRate, t) / MathF.Log(decayRate);
				return (float)(InitialVelocity / decayRate) * (term1 - term2);
			}
		}

		public float GetPosition(float currentElapsedInSeconds)
		{
			var minValue = GetValue(Handler._interactionTracker.MinPosition);
			var maxValue = GetValue(Handler._interactionTracker.MaxPosition);

			if (currentElapsedInSeconds >= TimeToMinimumVelocity)
			{
				HasCompleted = true;
				return Math.Clamp(FinalValue, minValue, maxValue);
			}

			return CalculatePosition(currentElapsedInSeconds);
		}
	}

}
