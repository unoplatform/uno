using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Benchmarks.Shared.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.GridBench
{
	public class ControlCreationBenchmark
	{
		[Benchmark()]
		public void TextBoxCreation()
			=> BenchmarkUIHost.Root.Content = new TextBox();

		[Benchmark()]
		public void ButtonCreation()
			=> BenchmarkUIHost.Root.Content = new Button();

		[Benchmark()]
		public void CheckBoxCreation()
			=> BenchmarkUIHost.Root.Content = new CheckBox();

		[Benchmark()]
		public void TextBlockCreation()
			=> BenchmarkUIHost.Root.Content = new TextBlock();

		[Benchmark()]
		public void SplitViewCreation()
			=> BenchmarkUIHost.Root.Content = new SplitView();

		[Benchmark()]
		public void ScrollViewerCreation()
			=> BenchmarkUIHost.Root.Content = new ScrollViewer();

		[Benchmark()]
		public void NavigationViewCreation()
			=> BenchmarkUIHost.Root.Content = new NavigationView();

		[Benchmark()]
		public void ScrollBarCreation()
			=> BenchmarkUIHost.Root.Content = new ScrollBar();

		[Benchmark()]
		public void ListViewItemCreation()
			=> BenchmarkUIHost.Root.Content = new ListViewItem();

		[Benchmark()]
		public void ComboBoxCreation()
			=> BenchmarkUIHost.Root.Content = new ComboBox();

		[Benchmark()]
		public void ComboBoxItemCreation()
			=> BenchmarkUIHost.Root.Content = new ComboBoxItem();

		[GlobalCleanup]
		public void Setup()
		{
			BenchmarkUIHost.Root.Content = null;
		}
	}
}
