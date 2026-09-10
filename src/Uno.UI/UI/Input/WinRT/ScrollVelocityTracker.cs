#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Input;

/// <summary>
/// Estimates pointer velocity by fitting a quadratic to the recent samples.
/// </summary>
/// <remarks>
/// A first-versus-last difference over two samples divides by whatever interval those two happened to
/// have, so one short gap produces an enormous velocity. Inertia distance grows with the square of the
/// launch velocity, so that error is amplified, not absorbed. Fitting instead makes the estimate
/// depend on the whole recent gesture. Parameters match Flutter's <c>VelocityTracker</c>, which
/// Avalonia also mirrors.
/// </remarks>
internal sealed class ScrollVelocityTracker
{
	private const int HistorySize = 20;
	private const double HorizonMs = 100;

	/// <summary>A gap this long means the finger stopped; samples before it describe a different motion.</summary>
	private const double AssumeStoppedMs = 40;

	private const int MinSamples = 3;
	private const int Degree = 2;

	private readonly (double TimeMs, double X, double Y)[] _samples = new (double, double, double)[HistorySize];
	private int _index = -1;
	private int _count;

	public void Reset()
	{
		_index = -1;
		_count = 0;
	}

	public void AddPosition(double timeMs, Point position)
	{
		_index = (_index + 1) % HistorySize;
		_samples[_index] = (timeMs, position.X, position.Y);
		if (_count < HistorySize)
		{
			_count++;
		}
	}

	/// <summary>Velocity in logical pixels per millisecond, or null when there is not enough recent motion.</summary>
	public Point? GetVelocity()
	{
		if (_count < MinSamples)
		{
			return null;
		}

		Span<double> ages = stackalloc double[HistorySize];
		Span<double> xs = stackalloc double[HistorySize];
		Span<double> ys = stackalloc double[HistorySize];

		var newest = _samples[_index];
		var sampleCount = 0;
		var i = _index;
		var previousTime = newest.TimeMs;

		for (var visited = 0; visited < _count; visited++)
		{
			var sample = _samples[i];
			var age = newest.TimeMs - sample.TimeMs;
			var gap = Math.Abs(previousTime - sample.TimeMs);
			previousTime = sample.TimeMs;

			if (age > HorizonMs || gap > AssumeStoppedMs)
			{
				break;
			}

			ages[sampleCount] = -age;
			xs[sampleCount] = sample.X;
			ys[sampleCount] = sample.Y;
			sampleCount++;

			i = (i - 1 + HistorySize) % HistorySize;
		}

		if (sampleCount < MinSamples)
		{
			return null;
		}

		var vx = SolveSlope(ages, xs, sampleCount);
		var vy = SolveSlope(ages, ys, sampleCount);

		return vx is null || vy is null ? null : new Point(vx.Value, vy.Value);
	}

	/// <summary>
	/// First-derivative coefficient of a least-squares polynomial through the samples, via Gram-Schmidt
	/// QR on the Vandermonde matrix. Returns null when the fit is degenerate (duplicate timestamps).
	/// </summary>
	private static double? SolveSlope(ReadOnlySpan<double> time, ReadOnlySpan<double> value, int count)
	{
		const int Terms = Degree + 1;

		Span<double> a = stackalloc double[Terms * HistorySize];
		for (var row = 0; row < count; row++)
		{
			a[row] = 1.0;
			for (var term = 1; term < Terms; term++)
			{
				a[term * HistorySize + row] = a[(term - 1) * HistorySize + row] * time[row];
			}
		}

		Span<double> q = stackalloc double[Terms * HistorySize];
		Span<double> r = stackalloc double[Terms * Terms];

		for (var j = 0; j < Terms; j++)
		{
			for (var h = 0; h < count; h++)
			{
				q[j * HistorySize + h] = a[j * HistorySize + h];
			}

			for (var i = 0; i < j; i++)
			{
				var dot = 0.0;
				for (var h = 0; h < count; h++)
				{
					dot += q[i * HistorySize + h] * q[j * HistorySize + h];
				}

				for (var h = 0; h < count; h++)
				{
					q[j * HistorySize + h] -= dot * q[i * HistorySize + h];
				}
			}

			var norm = 0.0;
			for (var h = 0; h < count; h++)
			{
				norm += q[j * HistorySize + h] * q[j * HistorySize + h];
			}

			norm = Math.Sqrt(norm);
			if (norm < 1e-6)
			{
				return null;
			}

			var inverseNorm = 1.0 / norm;
			for (var h = 0; h < count; h++)
			{
				q[j * HistorySize + h] *= inverseNorm;
			}

			for (var i = 0; i < Terms; i++)
			{
				var dot = 0.0;
				for (var h = 0; h < count; h++)
				{
					dot += q[j * HistorySize + h] * a[i * HistorySize + h];
				}

				r[j * Terms + i] = i < j ? 0.0 : dot;
			}
		}

		Span<double> wy = stackalloc double[HistorySize];
		for (var h = 0; h < count; h++)
		{
			wy[h] = value[h];
		}

		Span<double> coefficients = stackalloc double[Terms];
		for (var i = Terms - 1; i >= 0; i--)
		{
			var acc = 0.0;
			for (var h = 0; h < count; h++)
			{
				acc += q[i * HistorySize + h] * wy[h];
			}

			for (var j = Terms - 1; j > i; j--)
			{
				acc -= r[i * Terms + j] * coefficients[j];
			}

			if (Math.Abs(r[i * Terms + i]) < 1e-12)
			{
				return null;
			}

			coefficients[i] = acc / r[i * Terms + i];
		}

		return coefficients[1];
	}
}
