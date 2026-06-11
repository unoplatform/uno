#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Resources;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml;

partial class Application
{
	/// <summary>
	/// Monotonically incremented each time a non-default-ALC <see cref="Application"/> is
	/// registered via <see cref="SetCurrentApplication"/>. Used to dedupe by
	/// <c>Type.FullName</c> when iterating secondary applications so that stale
	/// (hot-reloaded-out) instances are skipped in favor of the most recent registration.
	/// Read/written only under <see cref="_applicationsByAlcSync"/>.
	/// </summary>
	private static long _alcRegistrationCounter;

	/// <summary>
	/// The registration order assigned to this <see cref="Application"/> by
	/// <see cref="SetCurrentApplication"/>. Higher value == more recently registered.
	/// Set only for non-default-ALC applications. Read/written only under
	/// <see cref="_applicationsByAlcSync"/>.
	/// </summary>
	internal long AlcRegistrationId;

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
			app.AlcRegistrationId = ++_alcRegistrationCounter;
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
	/// Enumerates all secondary-ALC <see cref="Application"/> instances currently registered.
	/// Used by <see cref="ResourceResolver"/> as a last-resort fallback when a resource
	/// lookup originating from a shared (default-ALC) assembly fails to find a key —
	/// the resource may live in a secondary ALC's <see cref="Application.Resources"/>
	/// (e.g. brushes from a shared theme assembly that reference colors defined only
	/// in the consuming application's merged dictionaries).
	/// </summary>
	/// <remarks>
	/// When the same logical app is hot-reloaded,
	/// the previous ALC may not yet be unloaded by the GC, so multiple <see cref="Application"/>
	/// instances with the same <c>Type.FullName</c> can be registered simultaneously. The
	/// previous instance is stale — its <see cref="Application.Resources"/> reflects the OLD
	/// build's values and would shadow the live build's overrides if returned to a resource
	/// lookup. We therefore dedupe by <c>Type.FullName</c>, keeping only the most recently
	/// registered instance (highest <see cref="AlcRegistrationId"/>), and yield in newest-first
	/// order so callers prefer the live app.
	/// </remarks>
	internal static global::System.Collections.Generic.IEnumerable<Application> EnumerateSecondaryApplications()
	{
		if (!_hasSecondaryApps)
		{
			yield break;
		}

		global::System.Collections.Generic.List<Application> snapshot;
		lock (_applicationsByAlcSync)
		{
			snapshot = new global::System.Collections.Generic.List<Application>();
			foreach (var kvp in _applicationsByAlc)
			{
				if (kvp.Value is not null && kvp.Value != Current)
				{
					snapshot.Add(kvp.Value);
				}
			}
		}

		// Dedupe by Type.FullName, keeping only the most recently registered instance.
		// Stale instances linger when a hot-reloaded app's previous ALC has not yet been
		// unloaded by the GC; their Resources are out of date and must not be returned.
		// Sort newest-first so that callers (e.g., ResourceResolver fallback iteration) try
		// the live registration before any other secondary apps. With Count <= 1, neither
		// dedupe nor sort changes anything, so we keep the cheap path.
		if (snapshot.Count > 1)
		{
			var latestByType = new global::System.Collections.Generic.Dictionary<string, Application>(StringComparer.Ordinal);
			List<Application> orphans = null;
			foreach (var app in snapshot)
			{
				var typeName = app.GetType().FullName;
				if (typeName is null)
				{
					// Defensive: surface anonymous/unnamed types without dedupe so we never lose them.
					(orphans ??= new List<Application>()).Add(app);
					continue;
				}

				if (!latestByType.TryGetValue(typeName, out var existing) || app.AlcRegistrationId > existing.AlcRegistrationId)
				{
					latestByType[typeName] = app;
				}
			}

			snapshot = new List<Application>(latestByType.Values);
			snapshot.Sort(static (a, b) => b.AlcRegistrationId.CompareTo(a.AlcRegistrationId));

			if (orphans is not null)
			{
				snapshot.AddRange(orphans);
			}
		}

		foreach (var app in snapshot)
		{
			yield return app;
		}
	}

	/// <summary>
	/// Returns the most recently registered secondary-ALC <see cref="Application"/> whose
	/// <c>Type.FullName</c> matches <paramref name="typeFullName"/>, or <see langword="null"/>
	/// if no such app is currently registered.
	/// </summary>
	/// <remarks>
	/// Used by <see cref="ResourceResolver"/> to "bump" a parse-context-resolved
	/// <see cref="Application"/> to the live registration when the parse context references a
	/// stale ALC (the typical hot-reload scenario where the previous build's static
	/// <c>__ParseContext_</c> still holds a reference to the previous ALC).
	/// </remarks>
	internal static Application GetLatestSecondaryApplicationForType(string typeFullName)
	{
		if (!_hasSecondaryApps || typeFullName is null)
		{
			return null;
		}

		Application latest = null;
		long latestId = -1;
		lock (_applicationsByAlcSync)
		{
			foreach (var kvp in _applicationsByAlc)
			{
				var app = kvp.Value;
				if (app is null || app == Current)
				{
					continue;
				}

				if (string.Equals(app.GetType().FullName, typeFullName, StringComparison.Ordinal)
					&& app.AlcRegistrationId > latestId)
				{
					latestId = app.AlcRegistrationId;
					latest = app;
				}
			}
		}

		return latest;
	}

	/// <summary>
	/// Purges Type-keyed caches of entries from non-default (collectible) ALCs.
	/// Called from <see cref="Window.CloseAlcWindow"/> during ALC teardown.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "ALC cleanup reflection")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ALC cleanup reflection")]
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

		// Shared resources (theme brushes etc.) first consumed by a secondary-ALC element record
		// it as their InheritanceContext parent (DependencyObjectStore._associatedParent); nothing
		// clears that association on unload, so host-lifetime resources pin the collectible ALC.
		// Sweep every dictionary reachable from the host application and the master theme set.
		try
		{
#if !__NETSTD_REFERENCE__
			Uno.UI.GlobalStaticResources.MasterDictionary.ClearCollectibleAssociatedParents();
#endif
			_current?.Resources?.ClearCollectibleAssociatedParents();
		}
		catch (Exception ex)
		{
			if (typeof(Application).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(Application).Log().Debug($"[ALC-CLEANUP] ClearCollectibleAssociatedParents error: {ex.GetType().Name}: {ex.Message}");
			}
		}

		// ContentControl memoizes "does this DefaultStyleKey type have a default template" per
		// Type; keys from the unloaded app's controls pin the ALC.
		try
		{
			Controls.ContentControl.ClearHasDefaultTemplateCache();
		}
		catch (Exception ex)
		{
			if (typeof(Application).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(Application).Log().Debug($"[ALC-CLEANUP] ClearHasDefaultTemplateCache error: {ex.GetType().Name}: {ex.Message}");
			}
		}

		// Secondary-ALC code can subscribe to events on HOST visual-tree elements (e.g. a
		// designer overlay tracking an ancestor's SizeChanged); those subscriptions are never
		// removed when the secondary app unloads and each one pins the collectible ALC. Walk
		// every live content root and prune delegate fields of invocation-list entries whose
		// target or method lives in a collectible ALC.
		try
		{
			PruneCollectibleAlcEventSubscriptions();
		}
		catch (Exception ex)
		{
			if (typeof(Application).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(Application).Log().Debug($"[ALC-CLEANUP] PruneCollectibleAlcEventSubscriptions error: {ex.GetType().Name}: {ex.Message}");
			}
		}

		// NativeDispatcher render registrations are never removed; prune entries whose
		// CompositionTarget's ContentRoot is no longer registered (a closed secondary-app
		// surface). Runs AFTER the tree prune above so orphaned roots are already unregistered.
		try
		{
			var rootCoordinator = Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator;
			Uno.UI.Dispatching.NativeDispatcher.Main.RemoveCompositionTargets(target =>
				target is Media.CompositionTarget compositionTarget
				&& !rootCoordinator.ContentRoots.Contains(compositionTarget.ContentRoot));
		}
		catch (Exception ex)
		{
			if (typeof(Application).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(Application).Log().Debug($"[ALC-CLEANUP] CompositionTarget prune error: {ex.GetType().Name}: {ex.Message}");
			}
		}

		// ResourceDictionary — invalidate _keyNotFoundCache on the host Application.
		// The cache accumulates ResourceKey entries with TypeKey referencing ALC types.
		// These entries pin RuntimeType → LoaderAllocator → ALC.
		// Clearing is cheap — entries are re-cached on demand.
		_current?.Resources.InvalidateNotFoundCache(propagate: false);

		// SystemThemeHelper — unsubscribe event handlers from non-default ALCs
		Uno.Helpers.Theming.SystemThemeHelper.ClearNonDefaultAlcHandlers();

		// DiagnosticViewRegistry — remove views registered by ALC-loaded types
		Uno.Diagnostics.UI.DiagnosticViewRegistry.ClearNonDefaultAlcRegistrations();

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

	// Per-type cache of delegate-typed instance fields, to keep the teardown tree walk cheap.
	private static readonly Dictionary<Type, global::System.Reflection.FieldInfo[]> _delegateFieldsCache = new();

	/// <summary>
	/// Walks every live content root's visual tree and removes, from each element's delegate
	/// fields (compiler-generated event backing fields included), any invocation-list entry
	/// whose target or declaring method lives in a collectible AssemblyLoadContext. Such
	/// subscriptions are made by secondary-ALC code on host elements and are never undone when
	/// the secondary app unloads — each one pins the unloaded ALC. Runs only during ALC teardown.
	/// </summary>
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "ALC cleanup reflection over live instances")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ALC cleanup reflection over live instances")]
	private static void PruneCollectibleAlcEventSubscriptions()
	{
		var coordinator = Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator;
		var roots = coordinator.ContentRoots;
		for (var i = roots.Count - 1; i >= 0; i--)
		{
			if (i < roots.Count && roots[i] is { } root && root.VisualTree?.RootElement is { } rootElement)
			{
				_treeContainsCollectibleElement = false;
				PruneCollectibleAlcEventSubscriptions(rootElement);

				// A content root whose tree contains collectible-ALC elements belongs to an
				// unloaded secondary app's surface (e.g. a designer popup island) — the window's
				// own root is removed at CloseAlcWindow, but popup/island roots otherwise stay
				// registered forever and pin the ALC through the coordinator.
				if (_treeContainsCollectibleElement)
				{
					coordinator.RemoveContentRoot(root);
				}
			}
		}

		// Window-level events (Activated / SizeChanged / VisibilityChanged / Closed) live on the
		// window implementation object, which is not part of any visual tree — a secondary-ALC
		// subscriber on the HOST window (e.g. a designer client hooking Closed) is never undone
		// by the tree walk above and would pin its ALC.
		var windows = global::Uno.UI.ApplicationHelper.WindowsInternal.ToArray();
		foreach (var window in windows)
		{
			try
			{
				PruneCollectibleDelegateFields(window);
				if (window.WindowImplementation is { } windowImplementation)
				{
					PruneCollectibleDelegateFields(windowImplementation);
				}
			}
			catch (Exception)
			{
				// One window refusing reflection must not abort pruning of the remaining windows.
			}
		}
	}

	[ThreadStatic]
	private static bool _treeContainsCollectibleElement;

	private static void PruneCollectibleAlcEventSubscriptions(DependencyObject element)
	{
		if (element.GetType().IsCollectible)
		{
			_treeContainsCollectibleElement = true;
		}

		try
		{
			PruneCollectibleDelegateFields(element);
		}
		catch (Exception)
		{
			// One element refusing reflection must not abort the sweep of the rest of the tree.
		}

		// Element-level Resources dictionaries are not reachable from Application.Resources or
		// the master theme set; their shared values can hold InheritanceContext associations
		// into the unloaded app's elements just like app-level resources do.
		try
		{
			if (element is FrameworkElement { Resources: { } elementResources })
			{
				elementResources.ClearCollectibleAssociatedParents();
			}
		}
		catch (Exception)
		{
		}

		// FrameworkElement content (e.g. a ContentControl's value) is normally also a visual
		// child, but prune the Content DP value explicitly in case the visual link differs
		// during teardown.
		try
		{
			if (element is Controls.ContentControl { Content: DependencyObject contentChild })
			{
				PruneCollectibleDelegateFields(contentChild);
			}
		}
		catch (Exception)
		{
		}

		try
		{
			var childCount = Media.VisualTreeHelper.GetChildrenCount(element);
			for (var i = 0; i < childCount; i++)
			{
				PruneCollectibleAlcEventSubscriptions(Media.VisualTreeHelper.GetChild(element, i));
			}
		}
		catch (Exception)
		{
			// A subtree that cannot be enumerated mid-teardown must not abort the rest of the sweep.
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "ALC cleanup reflection over live instances")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ALC cleanup reflection over live instances")]
	internal static void PruneCollectibleDelegateFields(object instance)
	{
		var type = instance.GetType();
		if (type.IsCollectible)
		{
			// Elements that themselves live in the collectible ALC die with it — and their
			// subtree is theirs to keep; pruning is only needed on host-lifetime elements.
			return;
		}

		if (!_delegateFieldsCache.TryGetValue(type, out var fields))
		{
			var list = new List<global::System.Reflection.FieldInfo>();
			for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
			{
				foreach (var field in t.GetFields(global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.DeclaredOnly))
				{
					if (typeof(Delegate).IsAssignableFrom(field.FieldType))
					{
						list.Add(field);
					}
				}
			}

			fields = list.ToArray();
			_delegateFieldsCache[type] = fields;
		}

		foreach (var field in fields)
		{
			try
			{
				if (field.GetValue(instance) is not Delegate current)
				{
					continue;
				}

				Delegate updated = current;
				foreach (var invocation in current.GetInvocationList())
				{
					if (invocation.Method.DeclaringType?.IsCollectible == true
						|| invocation.Target?.GetType().IsCollectible == true)
					{
						updated = Delegate.Remove(updated, invocation);
					}
				}

				if (!ReferenceEquals(updated, current))
				{
					field.SetValue(instance, updated);
				}
			}
			catch (Exception)
			{
				// A single inaccessible field must not abort pruning of the remaining fields.
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

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Diagnostic")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Diagnostic")]
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Diagnostic")]
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
						global::System.Reflection.BindingFlags.Static |
						global::System.Reflection.BindingFlags.NonPublic |
						global::System.Reflection.BindingFlags.Public))
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

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Diagnostic")]
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Diagnostic")]
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
		if (obj is global::System.Collections.IEnumerable enumerable && obj is not string)
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
					global::System.Reflection.BindingFlags.Instance |
					global::System.Reflection.BindingFlags.NonPublic |
					global::System.Reflection.BindingFlags.Public))
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
