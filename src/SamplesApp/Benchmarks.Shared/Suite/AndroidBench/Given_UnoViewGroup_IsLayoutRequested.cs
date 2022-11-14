#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Views;
using BenchmarkDotNet.Attributes;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.AndroidBench;

public class Given_UnoViewGroup_IsLayoutRequested
{
	private Grid _view;

	public Given_UnoViewGroup_IsLayoutRequested()
	{
		_view = new Grid();
	}

	[Benchmark(Baseline = true)]
	public void When_Original_IsLayoutRequested()
	{
		var r = _view.IsLayoutRequested;
	}

	[Benchmark()]
	public void When_Slim_IsLayoutRequested()
	{
		var r = _view.IsLayoutRequested_Slim;
	}
}
#endif
