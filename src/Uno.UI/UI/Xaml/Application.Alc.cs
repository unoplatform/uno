#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

	// Per-type cache of delegate-typed instance fields, to keep the teardown tree walk cheap.
	// Concurrent: the cleanup can be triggered from multiple teardown paths at once
	// (Window.CloseAlcWindows, Application.RemoveAlcApplication, host sweeps).
	private static readonly global::System.Collections.Concurrent.ConcurrentDictionary<Type, global::System.Reflection.FieldInfo[]> _delegateFieldsCache = new();

	// Set while pruning a single content root's tree to record whether that tree contained an
	// element from an unloading ALC (so an orphaned root can be removed). [ThreadStatic] because ALC
	// teardown sweeps can run concurrently on multiple threads; the flag is transient per-walk state
	// and must stay thread-local, otherwise one walk would observe another's result.
	[ThreadStatic]
	private static bool _treeContainsUnloadingAlcElement;

	private static void SetCurrentApplication(Application app)
	{
		if (app is null)
		{
			_current = null;

			// When the host clears Application.Current (during ALC teardown),
			// purge all Type-keyed caches for non-default ALCs. These caches
			// hold strong references to ALC-loaded Types that prevent GC
			// from collecting the ALC. The caches are rebuilt on demand.
			// Global shutdown: no single dying ALC is identifiable here, so the explicit
			// all-secondary sweep is intentional (every secondary app is going away).
			if (_hasSecondaryApps)
			{
				CleanupAllSecondaryAlcCaches();
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

		CleanupNonDefaultAlcCaches(alc);
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

#if UNO_HAS_ENHANCED_LIFECYCLE
	/// <summary>
	/// Returns the <see cref="Application"/> that owns <paramref name="contentRoot"/>. The owner window's
	/// <see cref="Window.OwnerAssemblyLoadContext"/> — tagged at construction with the ALC of the code
	/// that created the window — is authoritative: it stays correct when a secondary app's root content
	/// is a shared default-ALC type (e.g. a plain <c>Frame</c>) or null (content redirected to an
	/// <c>AlcContentHost</c>), both cases where type-based inference misattributes the root to the host.
	/// Falls back to inferring from the public root content's ALC (default-ALC content maps to
	/// <see cref="Current"/>; secondary-ALC content maps to its registered application). Returns
	/// <see langword="null"/> when the owner cannot be determined, in which case callers fall back to
	/// the current application. Used by <c>OnResourcesChanged</c> so that one app's theme refresh does
	/// not re-theme a content root owned by a different app sharing the process-global content-root
	/// list. Guarded by the same symbol as its only call site (the enhanced-lifecycle theme walk) so
	/// non-enhanced variants do not flag it as unused.
	/// </summary>
	private static Application GetOwningApplication(global::Uno.UI.Xaml.Core.ContentRoot contentRoot)
	{
		if (contentRoot is null)
		{
			return null;
		}

		if (contentRoot.GetOwnerWindow()?.OwnerAssemblyLoadContext is { } ownerAlc
			&& GetForAssemblyLoadContext(ownerAlc) is { } owner)
		{
			return owner;
		}

		return contentRoot.XamlRoot?.Content is { } content ? GetForInstance(content) : null;
	}

	/// <summary>
	/// The theme to walk a content root owned by this application with. For the host (default-ALC)
	/// app this is the shared <c>FrameworkTheming</c> theme; for a secondary-ALC app with an explicit
	/// theme it is that theme plus the global high-contrast axis, so refreshing a secondary-owned
	/// root never bleeds the host theme over the app's own theme (or vice versa).
	/// </summary>
	internal Theme GetEffectiveWalkTheme()
		=> _isSecondaryAlcApplication && _alcRequestedTheme is { } alcTheme
			? (alcTheme == ApplicationTheme.Dark ? Theme.Dark : Theme.Light)
				| global::Uno.UI.Xaml.Core.CoreServices.Instance.Theming.GetHighContrastTheme()
			: global::Uno.UI.Xaml.Core.CoreServices.Instance.Theming.GetTheme();
#endif

	/// <summary>
	/// The explicit <see cref="ApplicationTheme"/> of a secondary-ALC application, if set.
	/// Secondary apps must not mutate the shared <c>FrameworkTheming</c> (single per process, owned
	/// by the host app — WinUI's one-FrameworkTheming-per-core model, corep.h:2207). Their theme is
	/// instead pinned as an element-level <see cref="FrameworkElement.RequestedTheme"/> on the
	/// <c>AlcContentHost</c> boundary — the same mechanism WinUI uses to theme an island/subtree
	/// independently of the app theme (CFrameworkElement::GetRequestedThemeOverride,
	/// framework.cpp:3399-3418).
	/// </summary>
	private ApplicationTheme? _alcRequestedTheme;

	/// <summary>
	/// Whether this <see cref="Application"/> instance was created in a non-default (secondary) ALC.
	/// </summary>
	private readonly bool _isSecondaryAlcApplication;

	/// <summary>
	/// Sets (or clears, with <see langword="null"/>) the explicit theme of a secondary-ALC app and
	/// re-applies the element-level pin at its content-host boundary.
	/// </summary>
	private void SetAlcRequestedTheme(ApplicationTheme? explicitTheme)
	{
		if (_alcRequestedTheme == explicitTheme)
		{
			return;
		}

		var previousTheme = RequestedTheme;
		_alcRequestedTheme = explicitTheme;

#if __SKIA__ || __WASM__
		// ALC app hosting only exists on Skia and WASM (see ExitAlcApplication); on native platforms
		// Window maps to the native window type which doesn't have the ALC partial.
		Window.ApplyAlcRequestedTheme(this, AlcElementTheme);
#endif

		if (RequestedTheme != previousTheme)
		{
			RequestedThemeChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// The element-level pin corresponding to this secondary app's explicit theme
	/// (<see cref="ElementTheme.Default"/> when the app follows the host/system theme).
	/// </summary>
	internal ElementTheme AlcElementTheme => _alcRequestedTheme switch
	{
		ApplicationTheme.Light => ElementTheme.Light,
		ApplicationTheme.Dark => ElementTheme.Dark,
		_ => ElementTheme.Default,
	};

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
	/// <param name="dyingAlc">
	/// The <see cref="AssemblyLoadContext"/> being torn down. REQUIRED: it scopes the DESTRUCTIVE
	/// removals below to that ALC so that, with two live secondary apps, closing one does not
	/// destroy the other's state. Global-shutdown paths, where no single dying ALC exists, must
	/// opt in to the wide sweep explicitly via <see cref="CleanupAllSecondaryAlcCaches"/>.
	/// </param>
	internal static void CleanupNonDefaultAlcCaches(AssemblyLoadContext dyingAlc)
		=> CleanupNonDefaultAlcCachesCore(dyingAlc, allowUnscopedDestructive: false);

	/// <summary>
	/// Purges Type-keyed caches of entries from ALL non-default (collectible) ALCs, including the
	/// DESTRUCTIVE removals (ResourceLoader lookup assemblies, CompositionTarget.Rendering
	/// handlers) that <see cref="CleanupNonDefaultAlcCaches"/> scopes to a single dying ALC.
	/// Reserved for global shutdown, where every secondary app is going away and no live sibling
	/// can be harmed — the unscoped semantics are opt-in at the call site rather than a default.
	/// </summary>
	internal static void CleanupAllSecondaryAlcCaches()
		=> CleanupNonDefaultAlcCachesCore(dyingAlc: null, allowUnscopedDestructive: true);

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "ALC cleanup reflection")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "ALC cleanup reflection")]
	/// <param name="dyingAlc">
	/// The single AssemblyLoadContext being torn down, or <see langword="null"/> when the caller
	/// cannot name one (a per-window teardown that failed to capture its owner, or a global shutdown).
	/// </param>
	/// <param name="allowUnscopedDestructive">
	/// Whether the DESTRUCTIVE sweeps (whose dropped state is never re-created — ResourceLoader lookup
	/// assemblies, CompositionTarget.Rendering handlers, user Style overrides) may run wide (all
	/// non-default ALCs) when <paramref name="dyingAlc"/> is <see langword="null"/>. Only the explicit
	/// global-shutdown entry <see cref="CleanupAllSecondaryAlcCaches"/> passes <see langword="true"/>
	/// (every secondary app is going away, so no live sibling can be harmed). The per-window entry
	/// <see cref="CleanupNonDefaultAlcCaches"/> passes <see langword="false"/> so a window close that
	/// could not identify its ALC SKIPS the destructive sweeps rather than clobbering a live sibling.
	/// </param>
	private static void CleanupNonDefaultAlcCachesCore(AssemblyLoadContext dyingAlc, bool allowUnscopedDestructive)
	{
		// The unscoped destructive branch drops state of EVERY non-default ALC; it is only safe at a
		// genuine global shutdown. A per-window teardown that could not identify its dying ALC must
		// NEVER run it (it would strip a live sibling's never-re-created registrations) — the
		// destructive steps below no-op in that case. Surface each situation for field diagnostics.
		if (dyingAlc is null && allowUnscopedDestructive && typeof(Application).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(Application).Log().Warn("[ALC-CLEANUP] Running the UNSCOPED all-secondary-ALC sweep: destructive removals apply to every non-default AssemblyLoadContext (global shutdown).");
		}
		else if (dyingAlc is null && !allowUnscopedDestructive && typeof(Application).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(Application).Log().Warn("[ALC-CLEANUP] Per-window teardown could not identify its dying AssemblyLoadContext: rebuildable caches are cleared, but the destructive sweeps are SKIPPED to avoid clobbering a live sibling secondary app.");
		}
		else if (dyingAlc is not null && typeof(Application).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(Application).Log().Debug($"[ALC-CLEANUP] Sweeping caches for dying AssemblyLoadContext '{dyingAlc.Name}'.");
		}

		// THREADING CONTRACT for the sweeps below: the rebuildable Type-keyed caches
		// (Application registry, DependencyProperty, Style, MetadataAPI, ResourceResolver, …) are
		// populated and swept on the same UI/teardown thread, so they need no gate. The caches that
		// DO carry their own _*Gate lock (UIElementNativeRegistrar, HtmlElementHelper,
		// CompositionTarget, PagePool) are the ones additionally reachable off that thread — the WASM
		// rendering frame callback and native element creation. Every step is isolated through
		// RunCleanupStep so that, even if that contract is ever violated and an enumeration throws
		// (e.g. a concurrent-modification), one failing sweep cannot abort the remaining sweeps and
		// silently re-leak the ALC.

		// Remove Application instances registered for non-default ALCs from the CWT.
		// Without this, the CWT keeps the inner app's Application subclass alive.
		RunCleanupStep(nameof(ClearNonDefaultAlcApplications), ClearNonDefaultAlcApplications);

		// Type-keyed caches. These (and the FrameworkElementHelper/ResourceResolver/
		// UIElementNativeRegistrar sweeps below) intentionally stay all-non-default even when the
		// dying ALC is known: cache entries rebuild on demand, so over-clearing a live sibling
		// app's entries is a perf hiccup, not state loss. The DESTRUCTIVE removals further down
		// (ResourceLoader lookup assemblies, CompositionTarget.Rendering handlers, user Style
		// overrides) are ALC-scoped because a dropped registration/subscription is never re-created.
		RunCleanupStep(nameof(DependencyProperty.ClearCachesForNonDefaultAlc), DependencyProperty.ClearCachesForNonDefaultAlc);
		RunCleanupStep(nameof(Style.ClearCachesForNonDefaultAlc), Style.ClearCachesForNonDefaultAlc);
		RunCleanupStep(nameof(DirectUI.MetadataAPI.ClearCachesForNonDefaultAlc), DirectUI.MetadataAPI.ClearCachesForNonDefaultAlc);
		RunCleanupStep(nameof(Uno.UI.Extensions.UIElementExtensions.ClearDependencyPropertyCacheForNonDefaultAlc), Uno.UI.Extensions.UIElementExtensions.ClearDependencyPropertyCacheForNonDefaultAlc);
		RunCleanupStep(nameof(Uno.UI.DataBinding.BindingPropertyHelper.ClearCachesForNonDefaultAlc), Uno.UI.DataBinding.BindingPropertyHelper.ClearCachesForNonDefaultAlc);
		RunCleanupStep(nameof(Uno.UI.Xaml.UIElementGeneratedProxy.ClearCachesForNonDefaultAlc), Uno.UI.Xaml.UIElementGeneratedProxy.ClearCachesForNonDefaultAlc);
		RunCleanupStep(nameof(ApiInformation.ClearCachesForNonDefaultAlc), ApiInformation.ClearCachesForNonDefaultAlc);

		// FrameworkElementHelper — remove CWT entries for ALC DependencyObjects
		RunCleanupStep(nameof(FrameworkElementHelper.ClearNonDefaultAlcEntries), FrameworkElementHelper.ClearNonDefaultAlcEntries);

		// ResourceResolver — remove Func delegates whose Target is from a non-default ALC
		RunCleanupStep(nameof(ResourceResolver.ClearNonDefaultAlcRegistrations), ResourceResolver.ClearNonDefaultAlcRegistrations);

		// FeatureConfiguration.Style.UseUWPDefaultStylesOverride is USER CONFIGURATION, not a
		// rebuildable cache: a dropped override is never re-created, so it must not ride the
		// all-non-default cache-clear group above. DESTRUCTIVE — resolve the scope consistently with
		// the other destructive sweeps: scoped to the dying ALC when known, wide only at a genuine
		// global shutdown, and skipped when a per-window teardown could not name its ALC.
		RunCleanupStep(nameof(Style.RemoveAlcScopedUserStyleOverrides), () =>
		{
			if (dyingAlc is not null)
			{
				Style.RemoveAlcScopedUserStyleOverrides(dyingAlc);
			}
			else if (allowUnscopedDestructive)
			{
				Style.RemoveAllNonDefaultAlcUserStyleOverrides();
			}
			// else: unknown ALC in a per-window teardown — never wholesale-drop a live sibling's overrides.
		});

		// ResourceLoader — drop previewed-app lookup assemblies (and their parsed-resource
		// markers). The process-lifetime _lookupAssemblies list keeps a strong reference to
		// every app assembly registered via AddLookupAssembly, pinning the collectible ALC.
		// DESTRUCTIVE (registrations are never re-added): scope to the dying ALC when known.
		RunCleanupStep(nameof(global::Windows.ApplicationModel.Resources.ResourceLoader.ClearAlcAssemblies), () =>
		{
			if (dyingAlc is not null)
			{
				global::Windows.ApplicationModel.Resources.ResourceLoader.ClearAlcAssemblies(dyingAlc);
			}
			else if (allowUnscopedDestructive)
			{
				global::Windows.ApplicationModel.Resources.ResourceLoader.ClearNonDefaultAlcAssemblies();
			}
			// else: unknown ALC in a per-window teardown — skip; a wide sweep would break a live sibling's lookups.
		});

#if __WASM__
		// UIElementNativeRegistrar (WASM) — the Type→registration-id map keys on every element
		// type ever created, including the previewed app's control types. Desktop ClrMD guards
		// never see this cache; drop its collectible-ALC keys here. REBUILDABLE (the JS-side map
		// holds only strings and is left intact; a re-registration simply mints a fresh id), so the
		// sweep stays all-non-default even when the dying ALC is known.
		RunCleanupStep(nameof(UIElement.ClearNonDefaultAlcElementRegistrations), UIElement.ClearNonDefaultAlcElementRegistrations);

		// CompositionTarget.Rendering (WASM) — a secondary-ALC subscriber that did not detach
		// before unload keeps its ALC pinned through the static handler list.
		// DESTRUCTIVE (a dropped handler is never re-subscribed): scope to the dying ALC when known,
		// wide only at a genuine global shutdown, skipped when a per-window teardown lacks its ALC.
		RunCleanupStep(nameof(Media.CompositionTarget.ClearAlcHandlers), () =>
		{
			if (dyingAlc is not null)
			{
				Media.CompositionTarget.ClearAlcHandlers(dyingAlc);
			}
			else if (allowUnscopedDestructive)
			{
				Media.CompositionTarget.ClearNonDefaultAlcHandlers();
			}
			// else: unknown ALC in a per-window teardown — skip; a wide sweep would drop a live sibling's handlers.
		});

		// HtmlElementHelper (WASM) — Type→HtmlTag cache keyed by external (app) element types.
		// REBUILDABLE (re-cached on demand), so the sweep stays all-non-default.
		RunCleanupStep(nameof(HtmlElementHelper.ClearNonDefaultAlcEntries), HtmlElementHelper.ClearNonDefaultAlcEntries);
#endif

		// Shared resources (theme brushes etc.) first consumed by a secondary-ALC element record
		// it as their InheritanceContext parent (DependencyObjectStore._associatedParent); nothing
		// clears that association on unload, so host-lifetime resources pin the collectible ALC.
		// Sweep every dictionary reachable from the host application and the master theme set.
		RunCleanupStep(nameof(ClearCollectibleResourceAssociations), ClearCollectibleResourceAssociations);

		// ContentControl memoizes "does this DefaultStyleKey type have a default template" per
		// Type; keys from the unloaded app's controls pin the ALC.
		RunCleanupStep(nameof(Controls.ContentControl.ClearHasDefaultTemplateCache), Controls.ContentControl.ClearHasDefaultTemplateCache);

		// PagePool pools Page instances keyed by their (previewed-app) page Type; a pooled page from
		// an unloaded app pins its ALC. Sweeps every live per-Frame pool via the weak registry.
		RunCleanupStep(nameof(PagePool.ClearNonDefaultAlcEntries), static () => PagePool.ClearNonDefaultAlcEntries());

		// Secondary-ALC code can subscribe to events on HOST visual-tree elements (e.g. a
		// designer overlay tracking an ancestor's SizeChanged); those subscriptions are never
		// removed when the secondary app unloads and each one pins the collectible ALC. Walk
		// every live content root and prune delegate fields of invocation-list entries whose
		// target or method lives in a collectible ALC.
		RunCleanupStep(nameof(PruneCollectibleAlcEventSubscriptions), PruneCollectibleAlcEventSubscriptions);

		// The delegate-field cache is keyed by Type; a collectible key (including a host generic
		// instantiated over a collectible argument) pins its ALC. Drop them — re-cached on demand.
		RunCleanupStep(nameof(PruneCollectibleDelegateFieldsCache), PruneCollectibleDelegateFieldsCache);

		// NativeDispatcher render registrations are never removed; prune entries whose
		// CompositionTarget's ContentRoot is no longer registered (a closed secondary-app
		// surface). Runs AFTER the tree prune above so orphaned roots are already unregistered.
		RunCleanupStep(nameof(PruneOrphanedCompositionTargets), PruneOrphanedCompositionTargets);

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

	// Runs a teardown cleanup step in isolation: teardown is best-effort, so a failing step must not
	// abort the rest. A failure may indicate a real defect, so it is logged as a warning with the
	// full exception. Internal (rather than private) so the fault-isolation contract can be unit tested.
	internal static void RunCleanupStep(string name, Action step)
	{
		try
		{
			step();
		}
		catch (Exception ex)
		{
			if (typeof(Application).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(Application).Log().Warn($"[ALC-CLEANUP] {name} failed", ex);
			}
		}
	}

	private static void ClearCollectibleResourceAssociations()
	{
#if !__NETSTD_REFERENCE__
		Uno.UI.GlobalStaticResources.MasterDictionary.ClearCollectibleAssociatedParents();
#endif
		_current?.Resources?.ClearCollectibleAssociatedParents();
	}

	// The delegate-field cache (keyed by Type) outlives a teardown; a collectible key — including a
	// host generic instantiated over a collectible argument — pins its ALC. Drop every collectible
	// key on teardown; entries are cheap to rebuild on demand.
	private static void PruneCollectibleDelegateFieldsCache()
	{
		foreach (var key in _delegateFieldsCache.Keys)
		{
			if (key.IsCollectible)
			{
				_delegateFieldsCache.TryRemove(key, out _);
			}
		}
	}

	private static void PruneOrphanedCompositionTargets()
	{
		var rootCoordinator = Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator;
		Uno.UI.Dispatching.NativeDispatcher.Main.RemoveCompositionTargets(target =>
			target is Media.CompositionTarget compositionTarget
			&& !rootCoordinator.ContentRoots.Contains(compositionTarget.ContentRoot));
	}

	/// <summary>
	/// Whether the type's collectibility means "owned by a dying AssemblyLoadContext". This is
	/// the discriminator for DESTRUCTIVE prunes. The single shared implementation lives in
	/// <see cref="global::Uno.UI.Helpers.AlcCacheSweep.IsFromUnloadInitiatedAlc"/> (see its
	/// remarks for the constructed-generic and dynamic-assembly rules) so every sweep applies
	/// the same ownership rules.
	/// </summary>
	private static bool IsFromUnloadInitiatedAlc(Type type)
		=> global::Uno.UI.Helpers.AlcCacheSweep.IsFromUnloadInitiatedAlc(type);

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
		// Window-owned content roots are NEVER removed here, no matter what their trees contain:
		// the host main window's tree legitimately hosts a previewed app's (collectible-typed)
		// content, and unregistering the host root would take down the host window.
		var windows = global::Uno.UI.ApplicationHelper.WindowsInternal.ToArray();
		var windowRoots = new HashSet<object>();
		foreach (var window in windows)
		{
			if (window.WindowImplementation?.XamlRoot?.VisualTree?.ContentRoot is { } windowRoot)
			{
				windowRoots.Add(windowRoot);
			}
		}

		var coordinator = Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator;
		var roots = coordinator.ContentRoots;
		// Reverse iteration: RemoveContentRoot below mutates this collection, so walking from the end
		// keeps the remaining indices valid as entries are removed.
		for (var i = roots.Count - 1; i >= 0; i--)
		{
			if (i < roots.Count && roots[i] is { } root && root.VisualTree?.RootElement is { } rootElement)
			{
				_treeContainsUnloadingAlcElement = false;
				PruneCollectibleAlcEventSubscriptions(rootElement);

				// A non-window content root whose tree contains unloading-ALC elements belongs to
				// a dead secondary app's surface (e.g. a designer popup island) — the window's
				// own root is removed at CloseAlcWindow, but popup/island roots otherwise stay
				// registered forever and pin the ALC through the coordinator.
				if (_treeContainsUnloadingAlcElement && !windowRoots.Contains(root))
				{
					coordinator.RemoveContentRoot(root);
				}
			}
		}

		// Window-level events (Activated / SizeChanged / VisibilityChanged / Closed) live on the
		// window implementation object, which is not part of any visual tree — a secondary-ALC
		// subscriber on the HOST window (e.g. a designer client hooking Closed) is never undone
		// by the tree walk above and would pin its ALC.
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

	private static void PruneCollectibleAlcEventSubscriptions(DependencyObject element)
	{
		if (IsFromUnloadInitiatedAlc(element.GetType()))
		{
			_treeContainsUnloadingAlcElement = true;
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
	internal static void PruneCollectibleDelegateFields(object instance, AssemblyLoadContext dyingAlc = null)
	{
		var type = instance.GetType();
		if (IsFromUnloadInitiatedAlc(type))
		{
			// Instances genuinely owned by the dying ALC die with it, so there is nothing to prune.
			// Type.IsCollectible alone would also skip host-lifetime instances that are collectible
			// only via a generic argument (e.g. HostType<TAddIn>), leaving dying-ALC handlers attached.
			return;
		}

		var fields = _delegateFieldsCache.GetOrAdd(type, static t =>
		{
			var list = new List<global::System.Reflection.FieldInfo>();
			for (var current = t; current is not null && current != typeof(object); current = current.BaseType)
			{
				foreach (var field in current.GetFields(global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.DeclaredOnly))
				{
					if (typeof(Delegate).IsAssignableFrom(field.FieldType))
					{
						list.Add(field);
					}
				}
			}

			return list.ToArray();
		});

		if (fields.Length == 0)
		{
			return;
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
					// Destructive prune: only remove subscriptions whose owner is DYING — the
					// explicitly provided ALC (known at ALC-window close, before Unload() is
					// initiated) or an ALC whose unload has begun. A merely-collectible target
					// can belong to a live session-lifetime add-in ALC and must survive.
					var ownerType = invocation.Target?.GetType() ?? invocation.Method.DeclaringType;
					if (ownerType is null)
					{
						continue;
					}

					var prune = IsFromUnloadInitiatedAlc(ownerType)
						|| (dyingAlc is not null && ownerType.IsCollectible && AssemblyLoadContext.GetLoadContext(ownerType.Assembly) == dyingAlc);

					if (prune)
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
	[UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "Diagnostic")]
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
