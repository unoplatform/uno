#nullable enable

using System;
using System.Numerics;

namespace Windows.UI.Composition.Interactions;

internal sealed partial class InteractionTrackerActiveInputInertiaHandler
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

		internal InteractionTrackerActiveInputInertiaHandler Handler { get; }
		internal float DecayRate { get; }
		internal float InitialVelocity { get; }
		internal float InitialValue { get; }
		internal float FinalValue { get; }
		internal float FinalModifiedValue { get; }
		internal float TimeToMinimumVelocity { get; }
		internal Axis Axis { get; }

		internal bool HasCompleted { get; private set; }

		public AxisHelper(InteractionTrackerActiveInputInertiaHandler handler, Vector3 velocities, Axis axis)
		{
			Axis = axis;
			Handler = handler;
			InitialVelocity = GetValue(velocities);
			DecayRate = 1.0f - GetValue(Handler._interactionTracker.PositionInertiaDecayRate ?? new(0.95f));
			InitialValue = GetValue(Handler._interactionTracker.Position);

			TimeToMinimumVelocity = GetTimeToMinimumVelocity();

			var deltaPosition = CalculateDeltaPosition(TimeToMinimumVelocity);

			FinalValue = InitialValue + deltaPosition;
			FinalModifiedValue = Math.Clamp(FinalValue, GetValue(Handler._interactionTracker.MinPosition), GetValue(Handler._interactionTracker.MaxPosition));
		}

		private static bool IsCloseReal(float a, float b, float epsilon)
			=> MathF.Abs(a - b) <= epsilon;

		private static bool IsCloseRealZero(float a, float epsilon)
			=> MathF.Abs(a) < epsilon;

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

			var minimumVelocity = 30.0f;

			return TimeToMinimumVelocityCore(MathF.Abs(InitialVelocity), DecayRate, InitialValue);

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
			float epsilon = 0.0000011920929f;

			if (IsCloseReal(DecayRate, 1.0f, epsilon))
			{
				return InitialVelocity * time;
			}
			else if (IsCloseRealZero(DecayRate, epsilon) /*|| !_isInertiaEnabled*/)
			{
				return 0.0f;
			}
			else
			{
				float val = MathF.Pow(DecayRate, time);
				return ((val - 1.0f) * InitialVelocity) / MathF.Log(DecayRate);
			}
		}

		public float GetPosition(float currentElapsedInSeconds)
		{
			if (currentElapsedInSeconds >= TimeToMinimumVelocity)
			{
				HasCompleted = true;
				return FinalModifiedValue;
			}

			if (_dampingStateTimeInSeconds.HasValue)
			{
				var settlingTime = TimeToMinimumVelocity - _dampingStateTimeInSeconds.Value;
				var wn = 5.8335 / settlingTime;
				// It seems WinUI can use an underdamped animation in some cases. For now we only use critically damped animation.
				var value = DampingHelper.SolveCriticallyDamped(wn, currentElapsedInSeconds - _dampingStateTimeInSeconds.Value);
				value = value * (FinalModifiedValue - _dampingStatePosition!.Value) + _dampingStatePosition.Value;

				return (float)value;
			}

			var currentPosition = GetValue(Handler._interactionTracker.Position);
			var minPosition = GetValue(Handler._interactionTracker.MinPosition);
			var maxPosition = GetValue(Handler._interactionTracker.MaxPosition);
			if (currentPosition < minPosition || currentPosition > maxPosition)
			{
				// This is an overpan from Interacting state. Use damping animation.
				_dampingStateTimeInSeconds = Handler._stopwatch!.ElapsedMilliseconds / 1000.0f;
				_dampingStatePosition = currentPosition;
			}

			return InitialValue + CalculateDeltaPosition(currentElapsedInSeconds);
		}
	}

}
