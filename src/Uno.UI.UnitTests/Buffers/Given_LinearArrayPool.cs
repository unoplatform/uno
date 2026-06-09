using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Buffers;
using Uno.Extensions;
using Windows.System;

namespace Uno.UI.Tests.Buffers
{
	[TestClass]
	public class Given_LinearArrayPool
	{
		private Action _cleanup;

		[TestCleanup]
		public void Cleanup()
		{
			_cleanup?.Invoke();
		}

		[TestMethod]
		public void When_LowMemory()
		{
			ReferenceManager references = new();

			var SUT = SetupPool<byte>();

			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.Low;

			void Execute()
			{
				var array = SUT.pool.Rent(1024);

				references.Add(array);

				SUT.pool.Return(array);
			}

			Execute();

			SUT.provider.Now = TimeSpan.FromSeconds(1);
			SUT.provider.Callback();
			Assert.AreEqual(1, references.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(62);
			SUT.provider.Callback();
			Assert.AreEqual(0, references.Collect());
		}

		[TestMethod]
		public void When_HighMemory()
		{
			ReferenceManager references = new();

			var SUT = SetupPool<byte>();

			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.High;

			void Execute()
			{
				var array = SUT.pool.Rent(1024);

				references.Add(array);

				SUT.pool.Return(array);
			}

			Execute();

			SUT.provider.Now = TimeSpan.FromSeconds(1);
			SUT.provider.Callback();
			Assert.AreEqual(1, references.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(12);
			SUT.provider.Callback();
			Assert.AreEqual(0, references.Collect());
		}

		[TestMethod]
		public void When_MediumMemory_Partial_Clear()
		{
			ReferenceManager references = new();

			var SUT = SetupPool<byte>();

			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.Medium;

			void Execute()
			{
				var arrays = new List<byte[]>();

				for (var i = 0; i < 3; i++)
				{
					var array = SUT.pool.Rent(1024);

					references.Add(array);

					arrays.Add(array);
				}

				arrays.ForEach(array => SUT.pool.Return(array));
			}

			Execute();

			SUT.provider.Now = TimeSpan.FromSeconds(1);
			SUT.provider.Callback();
			Assert.AreEqual(3, references.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(62);
			SUT.provider.Callback();
			Assert.AreEqual(1, references.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(62 + 15);
			SUT.provider.Callback();
			Assert.AreEqual(0, references.Collect());
		}

		[TestMethod]
		public void When_HighMemory_Partial_Clear()
		{
			ReferenceManager references = new();

			var SUT = SetupPool<byte>();

			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.High;

			void Execute()
			{
				var arrays = new List<byte[]>();

				for (var i = 0; i < 16; i++)
				{
					var array = SUT.pool.Rent(1024);

					references.Add(array);

					arrays.Add(array);
				}

				arrays.ForEach(array => SUT.pool.Return(array));
			}

			Execute();

			SUT.provider.Now = TimeSpan.FromSeconds(1);
			SUT.provider.Callback();
			Assert.AreEqual(16, references.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(12);
			SUT.provider.Callback();
			Assert.AreEqual(8, references.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(12 + 2.5);
			SUT.provider.Callback();
			Assert.AreEqual(0, references.Collect());
		}

		private class ReferenceManager
		{
			private readonly List<WeakReference> _references = new();

			public void Add(object target)
			{
				_references.Add(new WeakReference(target));
			}

			public int Collect(TimeSpan? timeout = null)
			{
				timeout ??= TimeSpan.FromMilliseconds(500);

				var sw = Stopwatch.StartNew();

				while (sw.Elapsed < timeout)
				{
					GC.Collect(GC.MaxGeneration);
					GC.WaitForPendingFinalizers();
					GC.Collect(GC.MaxGeneration);

					if (_references.None(r => r.IsAlive))
					{
						break;
					}
				}

				return _references.Count(r => r.IsAlive);
			}
		}

		private (LinearArrayPool<T> pool, MockProvider provider) SetupPool<T>()
		{
			var provider = new MockProvider();

			LinearArrayPool<T>.SetPlatformProvider(provider);

			var pool = LinearArrayPool<T>.CreateAutomaticallyManaged(1024, 1);

			_cleanup = () => LinearArrayPool<T>.SetPlatformProvider(new DefaultArrayPoolPlatformProvider());

			return (pool, provider);
		}

		private class MockProvider : IArrayPoolPlatformProvider
		{
			public bool CanUseMemoryManager => true;

			public AppMemoryUsageLevel AppMemoryUsageLevel { get; set; }

			public TimeSpan Now { get; set; }

			public Action Callback { get; private set; }

			public void RegisterTrimCallback(Func<object, bool> callback, object arrayPool)
			{
				Callback = () => callback(arrayPool);
			}
		}
	}
}
