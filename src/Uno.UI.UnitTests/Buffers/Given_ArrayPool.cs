using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Buffers;
using Uno.Extensions;
using Windows.System;

namespace Uno.UI.Tests.Buffers
{
	[TestClass]
	public class Given_ArrayPool
	{
		private List<Action> _cleanup = new();

		[TestCleanup]
		public void Cleanup()
		{
			_cleanup.ForEach(c => c());
		}

		[TestMethod]
		public void When_Shared_And_LowMemory()
		{
			ReferenceManager refs = new();

			var SUT = SetupArrayPool<byte>();
			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.Low;

			SUT.provider.Now = TimeSpan.FromSeconds(1);

			void Do()
			{
				var a = SUT.pool.Rent(1024);
				refs.Add(a);
				SUT.pool.Return(a);
				a = null;
			}

			Do();

			SUT.provider.Now = TimeSpan.FromSeconds(2);
			SUT.provider.Callback();
			Assert.AreEqual(1, refs.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(63);
			SUT.provider.Callback();
			Assert.AreEqual(0, refs.Collect());
		}

		[TestMethod]
		public void When_Shared_And_HighMemory()
		{
			ReferenceManager refs = new();

			var SUT = SetupArrayPool<byte>();
			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.High;

			SUT.provider.Now = TimeSpan.FromSeconds(1);

			void Do()
			{
				var a = SUT.pool.Rent(1024);
				refs.Add(a);
				SUT.pool.Return(a);
				a = null;
			}
			Do();

			SUT.provider.Now = TimeSpan.FromSeconds(2);
			SUT.provider.Callback();
			Assert.AreEqual(1, refs.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(13);
			SUT.provider.Callback();
			Assert.AreEqual(0, refs.Collect());
		}

		[TestMethod]
		public void When_Shared_And_MediumMemory_Partial_Clear()
		{
			ReferenceManager refs = new();

			var SUT = SetupArrayPool<byte>();
			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.Medium;

			SUT.provider.Now = TimeSpan.FromSeconds(1);

			void Exec()
			{
				List<byte[]> arrays = new();
				for (int i = 0; i < 3; i++)
				{
					var a = SUT.pool.Rent(1024);
					refs.Add(a);
					arrays.Add(a);
				}

				for (int i = 0; i < arrays.Count; i++)
				{
					SUT.pool.Return(arrays[i]);
					arrays[i] = null;
				}

				arrays.Clear();
				arrays.Capacity = 0;
				arrays = null;
			}

			Exec();

			SUT.provider.Now = TimeSpan.FromSeconds(2);
			SUT.provider.Callback();
			Assert.AreEqual(3, refs.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(63);
			SUT.provider.Callback();
			Assert.AreEqual(1, refs.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(63 + 45);
			SUT.provider.Callback();
			Assert.AreEqual(0, refs.Collect());
		}

		[TestMethod]
		public void When_Shared_And_HighMemory_Partial_Clear()
		{
			ReferenceManager refs = new();

			var SUT = SetupArrayPool<byte>();
			SUT.provider.AppMemoryUsageLevel = AppMemoryUsageLevel.High;

			SUT.provider.Now = TimeSpan.FromSeconds(1);

			void Exec()
			{
				List<byte[]> arrays = new();
				for (int i = 0; i < 16; i++)
				{
					var a = SUT.pool.Rent(1024);
					refs.Add(a);
					arrays.Add(a);
				}

				for (int i = 0; i < arrays.Count; i++)
				{
					SUT.pool.Return(arrays[i]);
					arrays[i] = null;
				}

				arrays.Clear();
				arrays.Capacity = 0;
				arrays = null;
			}

			Exec();

			SUT.provider.Now = TimeSpan.FromSeconds(2);
			SUT.provider.Callback();
			Assert.AreEqual(16, refs.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(13);
			SUT.provider.Callback();
			Assert.AreEqual(8, refs.Collect());

			SUT.provider.Now = TimeSpan.FromSeconds(13 + 2.5);
			SUT.provider.Callback();
			Assert.AreEqual(0, refs.Collect());
		}

		private class ReferenceManager
		{
			List<WeakReference> _refs = new();

			public void Add(object target)
			{
				_refs.Add(new WeakReference(target));
			}

			public int Collect(TimeSpan? maxWait = null)
			{
				maxWait ??= TimeSpan.FromMilliseconds(500);

				var sw = Stopwatch.StartNew();
				while (sw.Elapsed < maxWait)
				{
					GC.Collect(2);
					GC.WaitForPendingFinalizers();
					GC.Collect(2);

					if (_refs.None(r => r.IsAlive))
					{
						break;
					}
				}

				return _refs.Count(r => r.IsAlive);
			}
		}

		private (MockProvider provider, ArrayPool<T> pool) SetupArrayPool<T>()
		{
			var provider = new MockProvider();
			ArrayPool<T>.Shared.SetPlatformProvider(provider);

			var arrayPool = ArrayPool<T>.CreateAutomaticMemoryManaged();

			_cleanup.Add(() => ArrayPool<T>.Shared.SetPlatformProvider(null));

			return (provider, arrayPool);
		}

		private class MockProvider : Uno.Buffers.IArrayPoolPlatformProvider
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
