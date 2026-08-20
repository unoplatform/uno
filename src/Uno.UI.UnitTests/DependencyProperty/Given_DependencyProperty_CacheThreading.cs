#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.DependencyPropertyTests;

[TestClass]
public partial class Given_DependencyProperty_CacheThreading
{
	private const int Concurrency = 8;

	[TestMethod]
	public async Task When_Uncached_Property_Is_Resolved_Concurrently()
	{
		for (var iteration = 0; iteration < 100; iteration++)
		{
			var name = $"ConcurrentProperty_{Guid.NewGuid():N}";
			var expected = DependencyProperty.Register(
				name,
				typeof(int),
				typeof(CacheOwner),
				new PropertyMetadata(0));
			using var ready = new Barrier(Concurrency);

			var tasks = Enumerable
				.Range(0, Concurrency)
				.Select(_ => Task.Run(() =>
				{
					ready.SignalAndWait();
					return DependencyProperty.GetProperty(typeof(CacheOwner), name);
				}))
				.ToArray();

			var actual = await Task.WhenAll(tasks);
			Assert.IsTrue(actual.All(property => property == expected));
		}
	}

	private sealed partial class CacheOwner : DependencyObject
	{
	}
}
