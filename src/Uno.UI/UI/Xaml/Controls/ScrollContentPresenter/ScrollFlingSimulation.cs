#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Closed-form fling motion, matching the platform the app is running on.
/// </summary>
/// <remarks>
/// <para>
/// Both curves are analytic in absolute time rather than integrated per tick, so a late or early
/// frame produces the correct position instead of accumulating error — which matters where the frame
/// rate changes mid-fling, as it does on Android browsers when the finger lifts.
/// </para>
/// <para>
/// Replaces a constant-deceleration parabola whose distance was <c>v₀²/4d</c>. Squaring the launch
/// velocity turns any over-estimate into a much larger distance error, and neither the curve nor its
/// per-platform constants corresponded to what Android or iOS actually do.
/// </para>
/// </remarks>
internal readonly struct ScrollFlingSimulation
{
	// Android's OverScroller, as re-derived analytically by Flutter's ClampingScrollSimulation.
	private const double DecelerationRate = 2.3582017; // ln(0.78)/ln(0.9)
	private const double Inflexion = 0.35;
	private const double Friction = 0.015;

	// g (m/s²) · inches per metre · DIPs per inch · Android's "look and feel" factor.
	// Flutter substitutes 160 here because its logical pixel is an Android dp; a WinUI DIP is 1/96in,
	// so using Flutter's value directly would make every fling travel 1.667x too far.
	private const double PhysicalCoefficient = 9.80665 * 39.37 * 96.0 * 0.84;

	// iOS: UIScrollView.decelerationRate.normal is 0.998 per ms, i.e. 0.998^1000 ≈ 0.135 per second.
	private const double AppleDrag = 0.135;

	private readonly double _start;
	private readonly double _velocity;
	private readonly bool _isApple;

	// Android form.
	private readonly double _duration;
	private readonly double _distance;

	private ScrollFlingSimulation(double start, double velocity, bool isApple, double duration, double distance)
	{
		_start = start;
		_velocity = velocity;
		_isApple = isApple;
		_duration = duration;
		_distance = distance;
	}

	/// <param name="velocityPerSecond">Launch velocity in logical pixels per second.</param>
	public static ScrollFlingSimulation Create(double start, double velocityPerSecond)
	{
		if (OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS())
		{
			return new ScrollFlingSimulation(start, velocityPerSecond, isApple: true, duration: 0, distance: 0);
		}

		var referenceVelocity = Friction * PhysicalCoefficient / Inflexion;
		var magnitude = Math.Abs(velocityPerSecond);
		if (magnitude < 1)
		{
			return new ScrollFlingSimulation(start, 0, isApple: false, duration: 0, distance: 0);
		}

		var androidDuration = Math.Pow(magnitude / referenceVelocity, 1.0 / (DecelerationRate - 1.0));
		var duration = DecelerationRate * Inflexion * androidDuration;
		var distance = velocityPerSecond * duration / DecelerationRate;

		return new ScrollFlingSimulation(start, velocityPerSecond, isApple: false, duration, distance);
	}

	/// <summary>Total time the motion takes, in seconds.</summary>
	public double Duration => _isApple
		? (_velocity == 0 ? 0 : Math.Log(1.0 / (Math.Abs(_velocity) + 1)) / Math.Log(AppleDrag))
		: _duration;

	/// <summary>Position at <paramref name="t"/> seconds after the fling started.</summary>
	public double GetPosition(double t)
	{
		if (_velocity == 0)
		{
			return _start;
		}

		if (_isApple)
		{
			// x(t) = x0 + v0 · (drag^t − 1) / ln(drag)
			return _start + _velocity * (Math.Pow(AppleDrag, t) - 1) / Math.Log(AppleDrag);
		}

		var u = 1.0 - Math.Clamp(t / _duration, 0.0, 1.0);
		return _start + _distance * (1.0 - Math.Pow(u, DecelerationRate));
	}

	/// <summary>Velocity at <paramref name="t"/> seconds, in logical pixels per second.</summary>
	public double GetVelocity(double t)
	{
		if (_velocity == 0)
		{
			return 0;
		}

		if (_isApple)
		{
			return _velocity * Math.Pow(AppleDrag, t);
		}

		var u = 1.0 - Math.Clamp(t / _duration, 0.0, 1.0);
		return _velocity * Math.Pow(u, DecelerationRate - 1.0);
	}

	/// <summary>Where the motion comes to rest, ignoring bounds.</summary>
	public double FinalPosition => _isApple
		? _start - _velocity / Math.Log(AppleDrag)
		: _start + _distance;
}
