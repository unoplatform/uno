// Least-squares polynomial velocity tracker.
// Ported from Flutter (velocity_tracker.dart) via Avalonia (VelocityTracker.cs).
// Provides much smoother fling velocity estimates than simple delta/time,
// especially when pointer events arrive at irregular intervals.

using System;
using System.Numerics;

namespace Microsoft.UI.Input;

/// <summary>
/// Computes pointer velocity using a 2nd-degree least-squares polynomial fit
/// over recent position history. This produces smoother and more accurate
/// fling velocity estimates than simple Δposition/Δtime.
/// </summary>
internal sealed class VelocityTracker
{
	private const int AssumePointerMoveStoppedMicroseconds = 40_000; // 40 ms
	private const int HistorySize = 20;
	private const int HorizonMicroseconds = 100_000; // 100 ms
	private const int MinSampleSize = 3;

	private readonly Sample[] _samples = new Sample[HistorySize];
	private int _index;

	public void AddPosition(ulong timestampMicroseconds, double x, double y)
	{
		_index = (_index + 1) % HistorySize;
		_samples[_index] = new Sample(true, timestampMicroseconds, x, y);
	}

	public void Reset()
	{
		Array.Clear(_samples, 0, HistorySize);
		_index = 0;
	}

	/// <summary>
	/// Estimates velocity in units per millisecond (matching Uno's ManipulationVelocities convention).
	/// Returns null if insufficient data.
	/// </summary>
	public (double VelocityX, double VelocityY)? GetVelocityEstimate()
	{
		Span<double> x = stackalloc double[HistorySize];
		Span<double> y = stackalloc double[HistorySize];
		Span<double> w = stackalloc double[HistorySize];
		Span<double> time = stackalloc double[HistorySize];
		int sampleCount = 0;
		int index = _index;

		ref readonly var newest = ref _samples[index];
		if (!newest.Valid)
		{
			return null;
		}

		var previousTs = newest.Timestamp;

		// Walk backwards from newest, collecting samples within the horizon.
		do
		{
			ref readonly var sample = ref _samples[index];
			if (!sample.Valid)
			{
				break;
			}

			var age = newest.Timestamp - sample.Timestamp;
			var delta = previousTs > sample.Timestamp ? previousTs - sample.Timestamp : sample.Timestamp - previousTs;
			previousTs = sample.Timestamp;

			if (age > HorizonMicroseconds || delta > AssumePointerMoveStoppedMicroseconds)
			{
				break;
			}

			x[sampleCount] = sample.X;
			y[sampleCount] = sample.Y;
			w[sampleCount] = 1.0;
			time[sampleCount] = -(double)age / 1000.0; // Convert to milliseconds (negative = in the past)

			index = index == 0 ? HistorySize - 1 : index - 1;
			sampleCount++;
		}
		while (sampleCount < HistorySize);

		if (sampleCount >= MinSampleSize)
		{
			var xSlice = time[..sampleCount];
			var xFit = LeastSquaresSolve(2, xSlice, x[..sampleCount], w[..sampleCount]);
			if (xFit is not null)
			{
				var yFit = LeastSquaresSolve(2, xSlice, y[..sampleCount], w[..sampleCount]);
				if (yFit is not null)
				{
					// Coefficients[1] is the first derivative (velocity) in units/ms
					return (xFit.Value.C1, yFit.Value.C1);
				}
			}
		}

		if (sampleCount >= 2)
		{
			// Fallback: linear velocity from oldest to newest
			ref readonly var oldest = ref _samples[(index + 1) % HistorySize];
			var elapsed = (double)(newest.Timestamp - oldest.Timestamp) / 1000.0; // ms
			if (elapsed > 0)
			{
				return ((newest.X - oldest.X) / elapsed, (newest.Y - oldest.Y) / elapsed);
			}
		}

		return null;
	}

	/// <summary>
	/// Fits a polynomial of the given degree to weighted data using QR decomposition.
	/// Returns the polynomial coefficients, or null if the system is under-determined.
	/// </summary>
	private static PolyResult? LeastSquaresSolve(int degree, ReadOnlySpan<double> xData, ReadOnlySpan<double> yData, ReadOnlySpan<double> wData)
	{
		int m = xData.Length;
		int n = degree + 1;

		if (n > m)
		{
			return null;
		}

		// Expand X to matrix A, pre-multiplied by weights.
		// Using stackalloc for small matrices (max 20 samples × 3 columns = 60 doubles)
		Span<double> aData = stackalloc double[n * m];
		for (int h = 0; h < m; h++)
		{
			aData[h] = wData[h]; // a[0, h]
			for (int i = 1; i < n; i++)
			{
				aData[i * m + h] = aData[(i - 1) * m + h] * xData[h]; // a[i, h] = a[i-1, h] * x[h]
			}
		}

		// QR decomposition via Gram-Schmidt
		Span<double> qData = stackalloc double[n * m];
		Span<double> rData = stackalloc double[n * n];

		for (int j = 0; j < n; j++)
		{
			// Copy column j of A into Q
			for (int h = 0; h < m; h++)
			{
				qData[j * m + h] = aData[j * m + h];
			}

			// Orthogonalize against previous columns
			for (int i = 0; i < j; i++)
			{
				double dot = Dot(qData.Slice(j * m, m), qData.Slice(i * m, m));
				for (int h = 0; h < m; h++)
				{
					qData[j * m + h] -= dot * qData[i * m + h];
				}
			}

			// Normalize
			double norm = Math.Sqrt(Dot(qData.Slice(j * m, m), qData.Slice(j * m, m)));
			if (norm < 1e-10)
			{
				return null; // Linearly dependent
			}

			double invNorm = 1.0 / norm;
			for (int h = 0; h < m; h++)
			{
				qData[j * m + h] *= invNorm;
			}

			// Compute R
			for (int i = 0; i < n; i++)
			{
				rData[j * n + i] = i < j ? 0.0 : Dot(qData.Slice(j * m, m), aData.Slice(i * m, m));
			}
		}

		// Solve R * B = Q^T * W * Y via back-substitution
		Span<double> wy = stackalloc double[m];
		for (int h = 0; h < m; h++)
		{
			wy[h] = yData[h] * wData[h];
		}

		Span<double> coefficients = stackalloc double[n];
		for (int i = n - 1; i >= 0; i--)
		{
			coefficients[i] = Dot(qData.Slice(i * m, m), wy);
			for (int j = n - 1; j > i; j--)
			{
				coefficients[i] -= rData[i * n + j] * coefficients[j];
			}
			coefficients[i] /= rData[i * n + i];
		}

		return new PolyResult(coefficients[0], n > 1 ? coefficients[1] : 0);
	}

	private static double Dot(ReadOnlySpan<double> a, ReadOnlySpan<double> b)
	{
		double result = 0;
		for (int i = 0; i < a.Length; i++)
		{
			result += a[i] * b[i];
		}
		return result;
	}

	private record struct Sample(bool Valid, ulong Timestamp, double X, double Y);

	/// <summary>
	/// Polynomial fit result. C0 = constant, C1 = first derivative (velocity).
	/// </summary>
	private record struct PolyResult(double C0, double C1);
}
