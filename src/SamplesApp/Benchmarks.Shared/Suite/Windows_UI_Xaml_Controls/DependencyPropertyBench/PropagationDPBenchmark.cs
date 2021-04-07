using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Benchmarks.Shared.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.DependencyPropertyBench
{
	public class PropagationDPBenchmark
	{
		private ContentControl SUT;
		private int _updateCount;

		[GlobalSetup]
		public void Setup()
		{
			SUT = new ContentControl();
			SUT.Content = new ContentControl();

			BenchmarkUIHost.Root.Content = SUT;
		}

		[Benchmark()]
		public void UpdateInheritedProperty()
		{
			SUT.FontSize = (_updateCount++ % 2) == 0 ? 21 : 42;
		}

		[Benchmark()]
		public void UpdateNonInheritedProperty()
		{
			SUT.Tag = (_updateCount++ % 2) == 0 ? 21 : 42;
		}
	}
}
