#nullable disable

using ObjCRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using System.Threading;
using Uno.Foundation.Logging;

namespace Uno.UI
{
	/// <summary>
	/// Pre-fetching helper for Objective-C mapped types, this improves the startup performance.
	/// </summary>
	/// <remarks>
	/// This class preload all known types in advance so that the
	/// registration does not happen inline with the use of the type. It can also
	/// be run during the app's initialization, on a background thread and on the UI Thread.
	/// </remarks>
	public class NativeInstanceHelper
	{
		private static readonly string _statsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cache", "nativeInstances.cache");

		private static bool _cacheInitialized;
		private static bool _persisted;

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
					try
					{
						await Task.Delay(TimeSpan.FromMinutes(1));

						PersistStatistics();
					}
					catch (Exception e)
					{
						typeof(NativeInstanceHelper).Log().Error(e.ToString());
					}
				}
			);
		}

		/// <summary>
		/// Pre-feteches the cached natived instances for the current application package, on a background thread.
		/// </summary>
		public static void InitializeCachedInstances()
		{
			if (_cacheInitialized)
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

						if (typeof(NativeInstanceHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
						{
							typeof(NativeInstanceHelper).Log().Debug($"Loading {types.Length} Objective-C mappings");
						}

						foreach (var type in cachedTypes)
						{
							HydrateNativeClass(type);
						}
					}
				}
				catch (Exception ex)
				{
					typeof(NativeInstanceHelper).Log().Error(ex.ToString());
				}
			});
		}

		private static void HydrateNativeClass(Type type)
		{
			Class.GetHandle(type);
		}

		/// <summary>
		/// Writes the content of the already resolved instances, to be
		/// reused at the next application startup.
		/// </summary>
		public static void PersistStatistics()
		{
			if (!_persisted)
			{
				_persisted = true;

				if (typeof(NativeInstanceHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					typeof(NativeInstanceHelper).Log().Debug($"Writing Objective-C mappings to {_statsFilePath}");
				}

				Directory.CreateDirectory(Path.GetDirectoryName(_statsFilePath));

				using (var stream = File.OpenWrite(_statsFilePath))
				{
					using (var writer = new StreamWriter(stream))
					{
						foreach (var type in GetRegistrarTypes())
						{
							writer.WriteLine(type.AssemblyQualifiedName);
						}
					}
				}
			}
		}

		/// <summary>
		/// Pokes the internals of Xamarin.iOS to get the registered native types.
		/// </summary>
		/// <returns></returns>
		private static IEnumerable<Type> GetRegistrarTypes()
		{
			var registrar = typeof(ObjCRuntime.Runtime)
				.GetField("Registrar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
				.GetValue(null);

			var registrarType = registrar.GetType();

			while (registrarType != null && registrarType.FullName != "Registrar.Registrar")
			{
				registrarType = registrarType.BaseType;
			}

			if (registrarType != null)
			{
				var typesDictionary = registrarType
					.GetField("types", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
					?.GetValue(registrar);

				var types = typesDictionary
					.GetType()
					.GetProperty("Keys")
					?.GetGetMethod()
					?.Invoke(typesDictionary, null) as IEnumerable<Type>;

				if (types != null)
				{
					// Lock on the same type.
					// See https://github.com/xamarin/xamarin-macios/blob/2edb2ae4f5bb371a7006731987717c01f8725420/src/ObjCRuntime/Registrar.cs#L1047
					lock (types)
					{
						return types.ToArray();
					}
				}
				else
				{
					if (typeof(NativeInstanceHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						typeof(NativeInstanceHelper).Log().Debug("Failed to find registrar type, cannot persist Objective-C mappings.");
					}
				}
			}
			else
			{
				if (typeof(NativeInstanceHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					typeof(NativeInstanceHelper).Log().Debug("Failed to find registrar type, cannot persist Objective-C mappings.");
				}
			}

			return Array.Empty<Type>();
		}
	}
}
