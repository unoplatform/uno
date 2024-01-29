using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.UI;

#pragma warning disable CS0219

namespace SamplesApp.Benchmarks.Suite.Windows_UI.ColorBench;

public class ColorBenchmark
{
	Color _color1 = Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), _color2 = Color.FromArgb(0xFF, 0x00, 0xFF, 0x00);

	[Benchmark]
	public void Create_Color()
	{
		var c = new Color();
	}

	[Benchmark]
	public void Create_Color_WithParams()
	{
		var c = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
	}

	[Benchmark]
	public void Create_Color_GetHashCode()
	{
		var c = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF).GetHashCode();
	}

	[Benchmark]
	public void Create_Color_op_Equals()
	{
		var b = _color1 == _color2;
	}

	[Benchmark]
	public void Create_Color_Equals()
	{
		var b = _color1.Equals(_color2);
	}
}
