#if !WINAPPSDK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Uno.Collections;

namespace SamplesApp.Benchmarks.Suite.SpanBench;

public class HashtableBench
{
	[Benchmark(Baseline = true)]
	public void HashtableEx()
	{
		var x = new HashtableEx();
		for (int i = 0; i < 10; i++)
		{
			x.Add(new(), new());
		}
	}


	[Benchmark]
	public void Hashtable()
	{
		var x = new Hashtable();
		for (int i = 0; i < 10; i++)
		{
			x.Add(new(), new());
		}
	}

	[Benchmark]
	public void Dictionary()
	{
		var x = new Dictionary<object, object>();
		for (int i = 0; i < 10; i++)
		{
			x.Add(new(), new());
		}
	}
}

#endif
