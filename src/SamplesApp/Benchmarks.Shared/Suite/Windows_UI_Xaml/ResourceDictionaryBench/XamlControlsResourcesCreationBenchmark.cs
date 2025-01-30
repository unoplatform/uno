using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml.ResourceDictionaryBench
{
	public class XamlControlsResourcesCreationBenchmark
	{
		[Benchmark]
		public void Create_XamlControlsResources()
		{
			var xcr = new XamlControlsResources();
		}
	}
}
