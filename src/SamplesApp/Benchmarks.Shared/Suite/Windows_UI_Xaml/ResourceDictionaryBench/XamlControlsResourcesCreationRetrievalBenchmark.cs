#pragma warning disable 105 // Disabled until the tree is migrate to WinUI

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml.ResourceDictionaryBench
{
	public class XamlControlsResourcesCreationRetrievalBenchmark
	{
		[Benchmark]
		public void Create_XamlControlsResources_And_Retrieve_Style()
		{
			var xcr = new XamlControlsResources();

			var style = xcr["ListViewItemExpanded"] as Style;
			var templateSetter = style.Setters.OfType<Setter>().First(s => s.Property == Control.TemplateProperty);
			var template = templateSetter.Value;
		}
	}
}
