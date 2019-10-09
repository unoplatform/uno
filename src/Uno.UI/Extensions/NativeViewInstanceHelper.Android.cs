using Android.Runtime;
using Uno.Extensions;
using Uno.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Uno.UI
{
    public class NativeInstanceHelper
	{
		private static readonly string _statsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cache", "nativeInstances.cache");

		private static Dictionary<Type, System.Tuple<IntPtr, IntPtr>> _typeCache = new Dictionary<Type, System.Tuple<IntPtr, IntPtr>>(Uno.Core.Comparison.FastTypeComparer.Default);
		private static bool _cacheInitialized;

		static NativeInstanceHelper()
		{
			InitializeCachedInstances();
			ScheduleCacheDump();
		}

		/// <summary>
		/// Schedules the writing of the content of the cache to the disk.
		/// </summary>
		private static void ScheduleCacheDump()
		{
			ThreadPool.QueueUserWorkItem(
				async _ =>
				{
					await Task.Delay(TimeSpan.FromMinutes(1));
					WriteUsageStats();
				}
			);
		}

		/// <summary>
		/// Pre-feteches the cached natived instances for the current application package, on a background thread.
		/// </summary>
		public static void InitializeCachedInstances()
		{
			if(_cacheInitialized)
			{
				// This will always happen, as the method is called in the
				// cctor. Yet, it gives the method more visibility, as it must 
				// be called as soon as possible at app launch time.
				return;
			}

			_cacheInitialized = true;

			ThreadPool.QueueUserWorkItem(_ => 
			{ 
				try
				{
					if (File.Exists(_statsFilePath))
					{
						var types = File.ReadAllLines(_statsFilePath);

						var cachedTypes = types
							.Select(t => Type.GetType(t, false))
							.Trim();

						foreach (var type in cachedTypes)
						{
							if (typeof(NativeInstanceHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
							{
								typeof(NativeInstanceHelper).Log().DebugFormat("Pre-fecthing native type information {0}", type);
                            }

							CreateTypeReference(type);
						}
					}
				}
				catch (Exception ex)
				{
					typeof(NativeInstanceHelper).Log().Error("Failed to initialize native view cache", ex);
				}
			});
		}

		/// <summary>
		/// Creates a native view using a fast instantiation path, by caching references to the native class and constructor.
		/// </summary>
		/// <param name="type">The .NET class top-level type</param>
		/// <param name="target">The .NET class instance being created</param>
		/// <param name="context">The context of the View to be created</param>
		/// <param name="setInstance">A delegate that provides access to the <see cref="Java.Lang.Object.SetHandle(IntPtr, JniHandleOwnership)"/> method.</param>
		public static unsafe void CreateNativeInstance(Type type, Java.Lang.Object target, Android.Content.Context context, Action<IntPtr, JniHandleOwnership> setInstance)
		{
			System.Tuple<IntPtr, IntPtr> typeRefInfo;

			typeRefInfo = CreateTypeReference(type);

			JValue* ptr = stackalloc JValue[1];
			*ptr = new JValue(context);

			// Create the allocation, this only works if the target SDK is higher than 10.
			var instance = JNIEnv.AllocObject(typeRefInfo.Item1);

			// Set the instance on the target object.
			setInstance(instance, JniHandleOwnership.TransferLocalRef);

			// invoke the native constructor
			JNIEnv.FinishCreateInstance(target.Handle, typeRefInfo.Item1, typeRefInfo.Item2, ptr);
		}

		private static unsafe System.Tuple<IntPtr, IntPtr> CreateTypeReference(Type type)
		{
			System.Tuple<IntPtr, IntPtr> typeRefInfo;

			lock(_typeCache)
			{
				if (!_typeCache.TryGetValue(type, out typeRefInfo))
				{
					// cache the resolutions, which cost a lot of processing.
					var typeRef = JNIEnv.FindClass(type);
					var ctorRef = JNIEnv.GetMethodID(typeRef, "<init>", "(Landroid/content/Context;)V");

					_typeCache[type] = typeRefInfo = Tuple.Create(typeRef, ctorRef);
				}
			}

			return typeRefInfo;
		}

		/// <summary>
		/// Writes the content of the already resolved instances, to be
		/// reused at the next application startup.
		/// </summary>
		private static void WriteUsageStats()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(_statsFilePath));

			using (var stream = File.OpenWrite(_statsFilePath))
			{
				using (var writer = new StreamWriter(stream))
				{
					lock(_typeCache)
					{
						foreach (var pairs in _typeCache)
						{
							writer.WriteLine(pairs.Key.AssemblyQualifiedName);
						}
					}
				}
			}
		}
	}
}
