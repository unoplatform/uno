#nullable disable

using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Resources;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml;

partial class Application
{
	private static void SetCurrentApplication(Application app)
	{
		if (app is null)
		{
			_current = null;

			// When the host clears Application.Current (during ALC teardown),
			// purge all Type-keyed caches for non-default ALCs. These caches
			// hold strong references to ALC-loaded Types that prevent GC
			// from collecting the ALC. The caches are rebuilt on demand.
			if (_hasSecondaryApps)
			{
				CleanupNonDefaultAlcCaches();
			}

			return;
		}

		var alc = AssemblyLoadContext.GetLoadContext(app.GetType().Assembly) ?? AssemblyLoadContext.Default;

		if (alc == AssemblyLoadContext.Default)
		{
			_current = app;
			return;
		}

		lock (_applicationsByAlcSync)
		{
			// ConditionalWeakTable lacks an update helper; remove the previous entry first so re-registration succeeds when the
			// same ALC bootstraps multiple Application instances (e.g., AlcApp runtime tests).
			_applicationsByAlc.Remove(alc);
			_applicationsByAlc.Add(alc, app);
			_hasSecondaryApps = true;
		}
	}

	/// <summary>
	/// Terminates a secondary (non-default ALC) application by closing its windows
	/// and removing it from the ALC registry. Does NOT call <see cref="CoreApplication.Exit"/>.
	/// </summary>
	internal void ExitAlcApplication()
	{
		var alc = AssemblyLoadContext.GetLoadContext(GetType().Assembly);
		if (alc is null || alc == AssemblyLoadContext.Default)
		{
			// Not an ALC app — fall through to normal Exit
			CoreApplication.Exit();
			return;
		}

#if __SKIA__ || __WASM__
		// Close all windows belonging to secondary ALCs.
		// ALC app loading only happens on Skia and WASM; on native platforms
		// (iOS, Android, macCatalyst) Window maps to the native window type
		// which doesn't have the ALC partial.
		Window.CloseAlcWindows();
#endif

		// Remove this app from the ALC registry and purge type-keyed caches.
		RemoveAlcApplication(alc);
	}

	/// <summary>
	/// Removes a secondary ALC application from the registry and triggers cache cleanup.
	/// </summary>
	internal static void RemoveAlcApplication(AssemblyLoadContext alc)
	{
		lock (_applicationsByAlcSync)
		{
			_applicationsByAlc.Remove(alc);
		}

		CleanupNonDefaultAlcCaches();
	}

	internal static Application GetForInstance(object instance)
		=> instance is null ? null : GetForType(instance.GetType());

	internal static Application GetForType(Type type)
		=> type is null ? Current : GetForAssemblyLoadContext(AssemblyLoadContext.GetLoadContext(type.Assembly));

	internal static Application GetForAssemblyLoadContext(AssemblyLoadContext alc)
	{
		if (alc is null || alc == AssemblyLoadContext.Default)
		{
			return Current;
		}

		lock (_applicationsByAlcSync)
		{
			return _applicationsByAlc.TryGetValue(alc, out var app) ? app : null;
		}
	}

	/// <summary>
	/// Purges Type-keyed caches of entries from non-default (collectible) ALCs.
	/// Called from <see cref="Window.CloseAlcWindow"/> during ALC teardown.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "ALC cleanup reflection")]
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ALC cleanup reflection")]
	internal static void CleanupNonDefaultAlcCaches()
	{
		// Remove Application instances registered for non-default ALCs from the CWT.
		// Without this, the CWT keeps the inner app's Application subclass alive.
		ClearNonDefaultAlcApplications();

		// Type-keyed caches
		DependencyProperty.ClearCachesForNonDefaultAlc();
		Style.ClearCachesForNonDefaultAlc();
		DirectUI.MetadataAPI.ClearCachesForNonDefaultAlc();
		Uno.UI.Extensions.UIElementExtensions.ClearDependencyPropertyCacheForNonDefaultAlc();
		Uno.UI.DataBinding.BindingPropertyHelper.ClearCachesForNonDefaultAlc();
		Uno.UI.Xaml.UIElementGeneratedProxy.ClearCachesForNonDefaultAlc();
		ApiInformation.ClearCachesForNonDefaultAlc();

		// FrameworkElementHelper — remove CWT entries for ALC DependencyObjects
		FrameworkElementHelper.ClearNonDefaultAlcEntries();

		// ResourceResolver — remove Func delegates whose Target is from a non-default ALC
		ResourceResolver.ClearNonDefaultAlcRegistrations();

		// SystemThemeHelper — unsubscribe event handlers from non-default ALCs
		Uno.Helpers.Theming.SystemThemeHelper.ClearNonDefaultAlcHandlers();

		// Diagnostic: deep scan is expensive — only run when trace logging is enabled
		if (typeof(Application).Log().IsEnabled(LogLevel.Trace))
		{
			try
			{
				DeepScanForAlcReferences();
			}
			catch (Exception ex)
			{
				typeof(Application).Log().Trace($"[ALC-SCAN] Error during deep scan: {ex.GetType().Name}: {ex.Message}");
			}
		}
	}

	private static void ClearNonDefaultAlcApplications()
	{
		lock (_applicationsByAlcSync)
		{
			// ConditionalWeakTable supports enumeration in .NET 8+
			var toRemove = new List<AssemblyLoadContext>();
			foreach (var kvp in _applicationsByAlc)
			{
				toRemove.Add(kvp.Key);
			}

			foreach (var alc in toRemove)
			{
				_applicationsByAlc.Remove(alc);
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Diagnostic")]
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Diagnostic")]
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Diagnostic")]
	private static void DeepScanForAlcReferences()
	{
		var defaultAlc = AssemblyLoadContext.Default;
		var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
		var results = new List<string>();

		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				var asmAlc = AssemblyLoadContext.GetLoadContext(asm);
				if (asmAlc != defaultAlc && asmAlc != null)
				{
					continue; // skip ALC assemblies themselves
				}

				foreach (var type in asm.GetTypes())
				{
					foreach (var field in type.GetFields(
						System.Reflection.BindingFlags.Static |
						System.Reflection.BindingFlags.NonPublic |
						System.Reflection.BindingFlags.Public))
					{
						try
						{
							var value = field.GetValue(null);
							if (value is null)
							{
								continue;
							}

							var path = $"{type.FullName}.{field.Name}";
							ScanObject(value, path, visited, results, defaultAlc, depth: 0);
						}
						catch
						{
							// Skip inaccessible fields
						}
					}
				}
			}
			catch
			{
				// Skip assemblies that can't be reflected
			}
		}

		var log = typeof(Application).Log();
		if (results.Count == 0)
		{
			if (log.IsEnabled(LogLevel.Trace))
			{
				log.Trace("[ALC-DEEP] No ALC references found in any static field graph.");
			}
		}
		else
		{
			if (log.IsEnabled(LogLevel.Debug))
			{
				log.Debug($"[ALC-DEEP] Found {results.Count} ALC reference path(s):");
				foreach (var r in results)
				{
					log.Debug($"  {r}");
				}
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Diagnostic")]
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Diagnostic")]
	private static void ScanObject(
		object obj,
		string path,
		HashSet<object> visited,
		List<string> results,
		AssemblyLoadContext defaultAlc,
		int depth)
	{
		if (obj is null || depth > 4 || results.Count > 50)
		{
			return;
		}

		// Avoid cycles and re-scanning
		if (!visited.Add(obj))
		{
			return;
		}

		var objType = obj.GetType();

		// Check if THIS object's type is from a non-default ALC
		var objAlc = AssemblyLoadContext.GetLoadContext(objType.Assembly);
		if (objAlc is not null && objAlc != defaultAlc)
		{
			results.Add($"{path} → {objType.FullName} [ALC: {objAlc.Name}]");
			return; // Found a root — no need to go deeper
		}

		// Check if this is a Type object referencing an ALC assembly
		if (obj is Type t)
		{
			var tAlc = AssemblyLoadContext.GetLoadContext(t.Assembly);
			if (tAlc is not null && tAlc != defaultAlc)
			{
				results.Add($"{path} → Type({t.FullName}) [ALC: {tAlc.Name}] IsCollectible={t.IsCollectible}");
			}
			return;
		}

		// Check if this is a Delegate — scan Target and invocation list
		if (obj is Delegate del)
		{
			if (del.Target is not null)
			{
				ScanObject(del.Target, path + "→Target", visited, results, defaultAlc, depth + 1);
			}

			if (del is MulticastDelegate mcd)
			{
				try
				{
					var list = mcd.GetInvocationList();
					for (var i = 0; i < list.Length; i++)
					{
						if (list[i].Target is not null)
						{
							ScanObject(list[i].Target, path + $"→Inv[{i}].Target", visited, results, defaultAlc, depth + 1);
						}
					}
				}
				catch { }
			}

			return;
		}

		// Check inside IEnumerable (Dictionary, List, etc.) — sample first N items
		if (obj is System.Collections.IEnumerable enumerable && obj is not string)
		{
			var count = 0;
			try
			{
				foreach (var item in enumerable)
				{
					if (item is null)
					{
						continue;
					}

					ScanObject(item, path + $"[{count}]", visited, results, defaultAlc, depth + 1);
					if (++count > 200 || results.Count > 50)
					{
						break;
					}
				}
			}
			catch { }

			return;
		}

		// For other objects, scan instance fields (depth-limited)
		if (depth < 3)
		{
			try
			{
				foreach (var f in objType.GetFields(
					System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.NonPublic |
					System.Reflection.BindingFlags.Public))
				{
					try
					{
						var fVal = f.GetValue(obj);
						if (fVal is not null)
						{
							ScanObject(fVal, path + "." + f.Name, visited, results, defaultAlc, depth + 1);
						}
					}
					catch { }
				}
			}
			catch { }
		}
	}
}
