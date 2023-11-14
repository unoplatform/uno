#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.System;

namespace Uno.Buffers
{
	internal sealed class LinearArrayPool<T>
	{
		private readonly int _baseArraySize;

		private readonly Bucket[] _buckets;

		private int _requestTrimCallbackRegistration;

		private static readonly bool _automaticMemoryManagement;
		private static readonly int _defaultAutomaticBucketSize;
		private static readonly int _defaultBucketSize;

		private static IArrayPoolPlatformProvider _platformProvider = new DefaultArrayPoolPlatformProvider();

		static LinearArrayPool()
		{
			_automaticMemoryManagement = WinRTFeatureConfiguration.ArrayPool.EnableAutomaticMemoryManagement;
			_defaultAutomaticBucketSize = WinRTFeatureConfiguration.ArrayPool.DefaultAutomaticMaxNumberOfArraysPerBucket;
			_defaultBucketSize = WinRTFeatureConfiguration.ArrayPool.DefaultMaxNumberOfArraysPerBucket;
		}

		public LinearArrayPool(int baseArraySize, int bucketCount, int bucketSize, bool enableTrimming)
		{
			if (baseArraySize <= 0 || bucketCount <= 0 || bucketSize <= 0)
			{
				throw new ArgumentException("baseArraySize, bucketCount or bucketSize must be greater than 0.");
			}

			_baseArraySize = baseArraySize;

			_buckets = new Bucket[bucketCount];

			for (var x = 0; x < bucketCount; x++)
			{
				_buckets[x] = new Bucket(bucketSize);
			}

			if (enableTrimming)
			{
				_requestTrimCallbackRegistration = 1;
			}
		}

		public static LinearArrayPool<T> CreateAutomaticallyManaged(int baseArraySize, int bucketCount)
			=> new LinearArrayPool<T>(
				baseArraySize,
				bucketCount,
				_automaticMemoryManagement && _platformProvider.CanUseMemoryManager ? _defaultAutomaticBucketSize : _defaultBucketSize,
				_automaticMemoryManagement && _platformProvider.CanUseMemoryManager);

		public T[] Rent(int multipleOfBaseSize)
		{
			Debug.Assert(multipleOfBaseSize >= 0 && (multipleOfBaseSize % _baseArraySize) == 0);

			if (multipleOfBaseSize != 0)
			{
				var index = (multipleOfBaseSize / _baseArraySize) - 1;

				if (index < _buckets.Length)
				{
					return _buckets[index].TryPop() ?? new T[multipleOfBaseSize];
				}

				return new T[multipleOfBaseSize];
			}

			return Array.Empty<T>();
		}

		public void Return(T[] array, bool clearArray = false)
		{
			if (array != null)
			{
				if (array.Length != 0)
				{
					Debug.Assert(array.Length % _baseArraySize == 0);

					var index = (array.Length / _baseArraySize) - 1;

					if (index < _buckets.Length)
					{
						if (clearArray)
						{
							Array.Clear(array);
						}

						_buckets[index].TryPush(array);

						if (Interlocked.Exchange(ref _requestTrimCallbackRegistration, 0) == 1)
						{
							_platformProvider.RegisterTrimCallback(pool => ((LinearArrayPool<T>)pool).Trim(), this);
						}
					}
				}
			}
			else
			{
				throw new ArgumentNullException(nameof(array));
			}
		}

		internal static void SetPlatformProvider(IArrayPoolPlatformProvider provider) => _platformProvider = provider;

		private bool Trim()
		{
			var memoryUsage = _platformProvider.AppMemoryUsageLevel;

			var now = _platformProvider.Now;

			for (var x = 0; x < _buckets.Length; x++)
			{
				_buckets[x].Trim(memoryUsage, now);
			}

			return true;
		}

		private sealed class Bucket
		{
			private readonly T[]?[] _arrays;

			private int _count;

			private TimeSpan _timestamp;

			private static readonly TimeSpan _highMemoryUsageTrimAfter = TimeSpan.FromSeconds(10);
			private static readonly int _highMemoryUsageTrimCountBaseline = 8 + Unsafe.SizeOf<T>() switch { > 32 => 2, > 16 => 1, _ => 0 };

			private static readonly TimeSpan _normalMemoryUsageTrimAfter = TimeSpan.FromSeconds(60);

			public Bucket(int size)
			{
				_arrays = new T[size][];
			}

			public T[]? TryPop()
			{
				lock (this)
				{
					if (_count > 0)
					{
						var result = _arrays[--_count];

						_arrays[_count] = null;

						return result;
					}

					return null;
				}
			}

			public void TryPush(T[] array)
			{
				lock (this)
				{
					if (_count < _arrays.Length)
					{
						if (_count == 0)
						{
							_timestamp = TimeSpan.Zero;
						}

						_arrays[_count++] = array;
					}
				}
			}

			public void Trim(AppMemoryUsageLevel memoryUsage, TimeSpan now)
			{
				lock (this)
				{
					if (_count == 0)
					{
						return;
					}

					if (_timestamp == TimeSpan.Zero)
					{
						_timestamp = now;

						return;
					}

					var trimDelay =
						memoryUsage == AppMemoryUsageLevel.High || memoryUsage == AppMemoryUsageLevel.OverLimit ?
							_highMemoryUsageTrimAfter :
								_normalMemoryUsageTrimAfter;

					if ((now - _timestamp) <= trimDelay)
					{
						return;
					}

					var trimCount = memoryUsage switch
					{
						AppMemoryUsageLevel.High or AppMemoryUsageLevel.OverLimit => _highMemoryUsageTrimCountBaseline + (_arrays.Length > 16384 ? 1 : 0),
						AppMemoryUsageLevel.Medium => 2,
						_ => 1
					};

					while (_count > 0 && trimCount-- > 0)
					{
						_arrays[--_count] = null;
					}

					_timestamp = _count > 0 ? _timestamp + TimeSpan.FromMilliseconds(trimDelay.TotalMilliseconds / 4) : TimeSpan.Zero;
				}
			}
		}
	}
}
