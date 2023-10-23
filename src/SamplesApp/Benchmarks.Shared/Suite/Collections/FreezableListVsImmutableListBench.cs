#if !WINDOWS_UWP
using BenchmarkDotNet.Attributes;
using Uno.Collections;

namespace SamplesApp.Benchmarks.Suite;

[MemoryDiagnoser]
public class FreezableListVsImmutableListBench
{
	[Benchmark(Baseline = true)]
	public void ImmutableList()
	{
		var x = new ImmutableList<string>();
		for (int i = 0; i < 10; i++)
		{
			x = x.Add(null);
		}
	}


	[Benchmark]
	public void FreezableList()
	{
		var x = new FreezableList<string>();
		for (int i = 0; i < 10; i++)
		{
			x.Add(null);
		}
	}
}

#endif
