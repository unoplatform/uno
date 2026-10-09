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
	// Android's OverScroller spline (AOSP OverScroller.SplineOverScroller).
	private const double DecelerationRate = 2.3582017; // ln(0.78)/ln(0.9)
	private const double Inflexion = 0.35;
	private const double Friction = 0.015;
	private const double StartTension = 0.5;
	private const double EndTension = 1.0;
	private const double P1 = StartTension * Inflexion;
	private const double P2 = 1.0 - EndTension * (1.0 - Inflexion);

	// Android's physical coefficient is g (m/s²) · inches per metre · pixels per inch · its "look and feel" factor,
	// so in logical units it takes the logical pixels per inch: a dp (1/160in) on Android, which is also what a
	// CSS px is in an Android browser, and a DIP (1/96in) elsewhere.
	private static readonly double _logicalPixelsPerInch =
		OperatingSystem.IsAndroid() || Uno.UI.Helpers.DeviceTargetHelper.BrowserHost is Uno.UI.Helpers.BrowserHostPlatform.Android
			? 160.0
			: 96.0;

	// iOS: UIScrollView.decelerationRate.normal is 0.998 per ms, i.e. 0.998^1000 ≈ 0.135 per second.
	private const double AppleDrag = 0.135;

	// A UI stall drops the velocity tracker's pre-stall samples, so the fit sees only a drained input
	// burst and reports a launch velocity orders of magnitude too high — which flings to the extent end.
	private const double MaxLaunchVelocityPerSecond = 5000;

	private const double VelocityEpsilon = 1e-9;

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

	/// <summary>
	/// Whether this device scrolls the way Apple's does. Includes a WebAssembly app running in an iOS or
	/// iPadOS browser, where the OS APIs only ever report "browser": the user's other apps still decelerate
	/// on UIScrollView's curve, so the app has to as well. Probed once, not per fling.
	/// </summary>
	private static readonly bool _isApplePlatform =
		OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS()
		|| Uno.UI.Helpers.DeviceTargetHelper.BrowserHost is Uno.UI.Helpers.BrowserHostPlatform.iOS;

	/// <param name="velocityPerSecond">Launch velocity in logical pixels per second.</param>
	public static ScrollFlingSimulation Create(double start, double velocityPerSecond)
		=> Create(start, velocityPerSecond, _isApplePlatform, _logicalPixelsPerInch);

	internal static ScrollFlingSimulation Create(double start, double velocityPerSecond, bool isApple, double logicalPixelsPerInch)
	{
		velocityPerSecond = Math.Clamp(velocityPerSecond, -MaxLaunchVelocityPerSecond, MaxLaunchVelocityPerSecond);

		if (isApple)
		{
			return new ScrollFlingSimulation(start, velocityPerSecond, isApple: true, duration: 0, distance: 0);
		}

		var physicalCoefficient = 9.80665 * 39.37 * logicalPixelsPerInch * 0.84;
		var referenceVelocity = Friction * physicalCoefficient / Inflexion;
		var magnitude = Math.Abs(velocityPerSecond);
		if (magnitude < 1)
		{
			return new ScrollFlingSimulation(start, 0, isApple: false, duration: 0, distance: 0);
		}

		// getSplineFlingDuration and getSplineFlingDistance, in seconds.
		var duration = Math.Pow(magnitude / referenceVelocity, 1.0 / (DecelerationRate - 1.0));
		var distance = velocityPerSecond * Inflexion * duration;

		return new ScrollFlingSimulation(start, velocityPerSecond, isApple: false, duration, distance);
	}

	/// <summary>Total time the motion takes, in seconds.</summary>
	public double Duration => _isApple
		? (Math.Abs(_velocity) <= VelocityEpsilon ? 0 : Math.Log(1.0 / (Math.Abs(_velocity) + 1)) / Math.Log(AppleDrag))
		: _duration;

	/// <summary>Position at <paramref name="t"/> seconds after the fling started.</summary>
	public double GetPosition(double t)
	{
		if (Math.Abs(_velocity) <= VelocityEpsilon)
		{
			return _start;
		}

		if (_isApple)
		{
			// x(t) = x0 + v0 · (drag^t − 1) / ln(drag)
			return _start + _velocity * (Math.Pow(AppleDrag, t) - 1) / Math.Log(AppleDrag);
		}

		return _start + _distance * GetSpline(t / _duration).Position;
	}

	/// <summary>Velocity at <paramref name="t"/> seconds, in logical pixels per second.</summary>
	public double GetVelocity(double t)
	{
		if (Math.Abs(_velocity) <= VelocityEpsilon)
		{
			return 0;
		}

		if (_isApple)
		{
			return _velocity * Math.Pow(AppleDrag, t);
		}

		return _distance / _duration * GetSpline(t / _duration).Slope;
	}

	/// <summary>
	/// The spline OverScroller samples into SPLINE_POSITION: a cubic Bézier in a parameter x that maps
	/// to both the time fraction and the distance fraction. Solved exactly rather than read from the
	/// 100-entry table, so the fling leaves at exactly its launch velocity and its velocity is the slope of its position.
	/// </summary>
	/// <returns>The distance fraction at <paramref name="timeFraction"/>, and its derivative over the time fraction.</returns>
	private static (double Position, double Slope) GetSpline(double timeFraction)
	{
		if (timeFraction >= 1)
		{
			return (1, 0);
		}

		timeFraction = Math.Max(0, timeFraction);

		// The time fraction is monotonic in x, so bisection always converges.
		double lo = 0, hi = 1, x = 0;
		for (var i = 0; i < 40; i++)
		{
			x = (lo + hi) / 2;
			if (Bezier(x, P1, P2) > timeFraction)
			{
				hi = x;
			}
			else
			{
				lo = x;
			}
		}

		var slope = BezierSlope(x, StartTension, 1.0) / BezierSlope(x, P1, P2);
		return (Bezier(x, StartTension, 1.0), slope);

		static double Bezier(double x, double c1, double c2)
			=> 3 * x * (1 - x) * ((1 - x) * c1 + x * c2) + x * x * x;

		static double BezierSlope(double x, double c1, double c2)
			=> 3 * (1 - x) * (1 - x) * c1 + 6 * x * (1 - x) * (c2 - c1) + 3 * x * x * (1 - c2);
	}

	/// <summary>Where the motion comes to rest, ignoring bounds.</summary>
	public double FinalPosition => _isApple
		? _start - _velocity / Math.Log(AppleDrag)
		: _start + _distance;
}
