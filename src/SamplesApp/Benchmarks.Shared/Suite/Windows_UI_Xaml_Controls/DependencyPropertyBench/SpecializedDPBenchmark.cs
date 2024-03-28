using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.DependencyPropertyBench
{
	public class SpecializedDPBenchmark
	{

		[GlobalSetup]
		public void Setup()
		{
		}

		[Benchmark()]
		public void Property_GetMetadataNotSelf()
		{
			Border.TagProperty.GetMetadata(typeof(Border));
		}

#if HAS_UNO
		[Benchmark()]
		public void Property_ByName()
		{
			DependencyProperty.GetProperty(typeof(Border), "Child");
		}
#endif
	}
}
