#nullable enable

using BenchmarkDotNet.Attributes;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks.Suites.Micro;

[BenchmarkCategory(BenchmarkCategory.Micro)]
[MemoryDiagnoser]
public partial class SimpleDPBenchmark
{
	private DependencyObjectStub _sut = null!;

	[GlobalSetup]
	public void Setup() => _sut = new DependencyObjectStub();

	[Benchmark]
	public int Get_Set()
	{
		_sut.SetValue(DependencyObjectStub.IntProperty, 42);
		return (int)_sut.GetValue(DependencyObjectStub.IntProperty);
	}

	private partial class DependencyObjectStub : DependencyObject
	{
		public static readonly DependencyProperty IntProperty =
			DependencyProperty.Register(
				nameof(Int),
				typeof(int),
				typeof(DependencyObjectStub),
				new PropertyMetadata(0));

		public int Int
		{
			get => (int)GetValue(IntProperty);
			set => SetValue(IntProperty, value);
		}
	}
}
