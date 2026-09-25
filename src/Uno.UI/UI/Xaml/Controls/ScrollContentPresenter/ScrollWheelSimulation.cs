#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// The wheel's scroll motion, as measured on WinUI 3's ScrollViewer: a fixed 220ms quadratic ease-out whose first
/// frame covers 22% of the distance, independent of that distance. Every notch moves the target by the distance it
/// carries and restarts the curve from the current position.
/// </summary>
/// <remarks>
/// The curve is closed-form in time, so a late or early frame produces the correct position. The target is kept as
/// the exact sum of the notches, so the motion lands on precisely the offset they add up to.
/// </remarks>
internal struct ScrollWheelSimulation
{
	/// <summary>Duration of the curve, in seconds.</summary>
	private const double Duration = 0.22;

	/// <summary>Share of the distance the curve covers on its first frame.</summary>
	private const double FirstFrameShare = 0.22;

	private bool _isRunning;
	private double _position;
	private double _from;
	private double _target;

	// 0 until the first frame after a notch, which the curve restarts from.
	private long _startTimestampInTicks;

	public readonly bool IsRunning => _isRunning;

	public readonly double Position => _position;

	/// <summary>Where the motion currently in flight will come to rest, ignoring bounds.</summary>
	public readonly double ProjectedEnd => _target;

	public void Start(double position)
	{
		_position = position;
		_target = position;
		_isRunning = false;
	}

	/// <param name="distance">Signed distance to move the target by.</param>
	public void AddDistance(double distance) => MoveTargetTo(_target + distance);

	/// <summary>Restarts the curve towards exactly <paramref name="target"/>.</summary>
	public void MoveTargetTo(double target)
	{
		if (target == _target)
		{
			return;
		}

		_isRunning = true;
		_target = target;
		_from = _position;
		_startTimestampInTicks = 0;
	}

	/// <summary>Advances to <paramref name="timestampInTicks"/>, clamped to [<paramref name="min"/>, <paramref name="max"/>].</summary>
	/// <returns>False once the motion has settled.</returns>
	public bool Tick(long timestampInTicks, double min, double max)
	{
		if (!_isRunning)
		{
			return false;
		}

		if (_startTimestampInTicks == 0)
		{
			_startTimestampInTicks = timestampInTicks;
		}

		var s = Math.Clamp((timestampInTicks - _startTimestampInTicks) / (double)TimeSpan.TicksPerSecond / Duration, 0, 1);
		var progress = FirstFrameShare + (1 - FirstFrameShare) * (1 - (1 - s) * (1 - s));
		_position = _from + (_target - _from) * progress;

		if (s >= 1)
		{
			_position = _target;
			_isRunning = false;
		}

		if (_position <= min || _position >= max)
		{
			_position = _target = Math.Clamp(_position, min, max);
			_isRunning = false;
		}

		return _isRunning;
	}

	public void Stop()
	{
		_isRunning = false;
		_target = _position;
	}
}
