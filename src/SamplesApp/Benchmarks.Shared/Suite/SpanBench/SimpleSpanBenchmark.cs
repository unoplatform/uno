using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace SamplesApp.Benchmarks.Suite.SpanBench
{
	public class SpanTesting
	{
		[Params(10, 20)]
		public int Items { get; set; }

		[Benchmark(Baseline = true)]
		public void EnumerableSum()
		{
			var r = Enumerable.Range(0, Items).ToArray();

			var s = r.Sum();
		}


		[Benchmark()]
		public void SpanSum()
		{
			var r = Enumerable.Range(0, Items).ToArray();

			var s = ((Span<int>)r).Sum();
		}
	}

	public static class Extensions
	{
		public static double Sum(this Span<int> span)
		{
			double result = 0;

			foreach (var value in span)
			{
				result += value;
			}

			return result;
		}
	}
}
