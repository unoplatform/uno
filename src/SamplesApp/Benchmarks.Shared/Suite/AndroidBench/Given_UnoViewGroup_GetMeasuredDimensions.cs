#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Views;
using BenchmarkDotNet.Attributes;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.AndroidBench;

public class Given_UnoViewGroup_GetMeasuredDimensions
{
	private Grid _view;

	public Given_UnoViewGroup_GetMeasuredDimensions()
	{
		_view = new Grid();
	}

	[Benchmark(Baseline = true)]
	public void When_Original_GetMeasuredDimensions()
	{
		var r = Uno.UI.UnoViewGroup.GetMeasuredDimensions(_view);
	}

	[Benchmark()]
	public void When_Slim_GetMeasuredDimensions()
	{
		var r = Uno.UI.UnoViewGroup.GetMeasuredDimensions_Slim(_view);
	}
}
#endif
