using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml.ResourceDictionaryBench
{
	public class XamlControlsResourcesReadBenchmark
	{
		XamlControlsResources SUT;

		[GlobalSetup]
		public void Setup()
		{
			SUT = new XamlControlsResources();
			if (!(SUT["ListViewItemExpanded"] is Style)) {
				throw new InvalidOperationException($"ListViewItemExpanded does not exist");
			}

		}

		[Benchmark]
		public void ReadInvalidKey()
		{
			SUT.TryGetValue("InvalidKey", out var style);
		}

		[Benchmark]
		public void ReadExistingKey()
		{
			SUT.TryGetValue("ListViewItemExpanded", out var style);
		}

		[Benchmark]
		public void ReadExistingType()
		{
			SUT.TryGetValue(typeof(Frame), out var style);
		}
	}
}
