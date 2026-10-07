#nullable enable

using System;

namespace UITests.Shared.Windows_UI_Composition.GitHubContributions;

/// <summary>Mass-spring-damper constants, in the same terms the reference animation states them.</summary>
internal readonly record struct SpringConfig(float Mass, float Damping, float Stiffness)
{
	internal static SpringConfig Fill { get; } = new(1.1f, 13f, 150f);

	internal static SpringConfig Clear { get; } = new(0.9f, 10f, 60f);
}

/// <summary>
/// A damped harmonic oscillator, integrated per frame. XAML easings are fixed-duration curves, so a
/// spring that can overshoot and settle has to be integrated rather than keyframed - and overshoot is
/// most of what gives this grid its feel.
/// </summary>
internal struct Spring
{
	private const float MaxStep = 1f / 240f;
	private const float RestVelocity = 0.01f;
	private const float RestOffset = 0.001f;

	internal float Value;
	internal float Velocity;
	internal float Target;
	internal float Delay;
	internal bool Resting;

	internal void To(float target, float delay)
	{
		Target = target;
		Delay = delay;
		Resting = false;
	}

	internal void Advance(float dt, SpringConfig config)
	{
		if (Resting)
		{
			return;
		}

		if (Delay > 0f)
		{
			Delay -= dt;
			if (Delay > 0f)
			{
				return;
			}

			dt = -Delay;
			Delay = 0f;
		}

		// Substep so a slow frame cannot hand the integrator a step big enough to go unstable.
		var remaining = Math.Min(dt, 0.1f);
		while (remaining > 0f)
		{
			var step = Math.Min(MaxStep, remaining);
			remaining -= step;

			var accel = (-config.Stiffness * (Value - Target) - config.Damping * Velocity) / config.Mass;
			Velocity += accel * step;
			Value += Velocity * step;
		}

		if (Math.Abs(Velocity) < RestVelocity && Math.Abs(Value - Target) < RestOffset)
		{
			Value = Target;
			Velocity = 0f;
			Resting = true;
		}
	}
}
