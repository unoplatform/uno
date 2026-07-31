#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Exponentially-decaying scroll motion, the model both WinUI stacks use for the wheel: a detent is an
/// impulse added to a running velocity, not an ease towards a fixed target.
/// </summary>
/// <remarks>
/// <para>
/// Restarting an ease on every detent is what makes wheel scrolling judder. An ease-out front-loads its
/// motion, so each restart re-injects a large first step; detents arrive asynchronously to the frame
/// clock, so that step lands on an arbitrary frame and the presented displacement alternates between
/// very large and very small.
/// </para>
/// <para>
/// Exponential decay is memoryless, so adding an impulse to the current velocity composes exactly with
/// whatever motion is already in flight — velocity stays continuous and only its slope changes. The
/// integration is closed-form over an arbitrary interval, so a late or early frame produces the correct
/// position rather than accumulating error.
/// </para>
/// </remarks>
internal struct ScrollDecaySimulation
{
	/// <summary>Velocity decay constant, in 1/s. Motion is visually complete after roughly 5/λ.</summary>
	private const double Lambda = 8.0;

	/// <summary>Below this the remaining motion is under a pixel per frame; snap and stop.</summary>
	private const double MinVelocity = 8.0;

	private double _velocity;
	private double _position;
	private long _lastTimestampInTicks;

	public readonly bool IsRunning => _velocity != 0;

	public readonly double Position => _position;

	/// <summary>Where the motion currently in flight will come to rest, ignoring bounds.</summary>
	public readonly double ProjectedEnd => _position + _velocity / Lambda;

	public void Start(double position, long timestampInTicks)
	{
		_position = position;
		_velocity = 0;
		_lastTimestampInTicks = timestampInTicks;
	}

	/// <param name="distance">Signed distance this impulse would travel on its own.</param>
	public void AddImpulse(double distance) => _velocity += distance * Lambda;

	/// <summary>Advances to <paramref name="timestampInTicks"/>, clamped to [<paramref name="min"/>, <paramref name="max"/>].</summary>
	/// <returns>False once the motion has settled.</returns>
	public bool Tick(long timestampInTicks, double min, double max)
	{
		var elapsed = (timestampInTicks - _lastTimestampInTicks) / (double)TimeSpan.TicksPerSecond;
		_lastTimestampInTicks = timestampInTicks;

		if (elapsed <= 0)
		{
			return true;
		}

		var decay = Math.Exp(-Lambda * elapsed);
		_position += _velocity / Lambda * (1 - decay);
		_velocity *= decay;

		if (_position <= min)
		{
			_position = min;
			_velocity = 0;
		}
		else if (_position >= max)
		{
			_position = max;
			_velocity = 0;
		}
		else if (Math.Abs(_velocity) < MinVelocity)
		{
			_velocity = 0;
		}

		return _velocity != 0;
	}

	public void Stop() => _velocity = 0;
}
