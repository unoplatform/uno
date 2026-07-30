#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml
{
	public partial class DependencyObjectStore
	{
		/// <summary>
		/// Drops <see cref="_associatedParent"/> — and any DataContext it propagated into this store —
		/// when the parent is owned by an unloading collectible ALC, is an unloaded
		/// <see cref="FrameworkElement"/>, or is orphaned from the content-root coordinator, so a
		/// host-lifetime resource cannot pin a secondary app's ALC. A live host element re-associates
		/// on its next value assignment.
		/// </summary>
		internal void ClearCollectibleAssociatedParent()
		{
			var associationCleared = false;

			// Two flavors of secondary-app parents: objects whose TYPE lives in the collectible
			// ALC, and instances of SHARED types (Grid, Border, …) owned by the unloaded app —
			// the latter are indistinguishable by type, but after unload they are detached
			// (unloaded, parentless), so a detached-element association is also dropped. A live
			// host element re-associates naturally on its next value assignment.
			// IsLoaded == false also matches host elements that are merely unloaded at sweep time
			// (e.g. a collapsed pane); clearing their association only resets InheritanceContext
			// DataContext propagation for the shared resource, which re-establishes on the next
			// value assignment — an acceptable cost for guaranteeing the unloaded app's detached
			// elements (shared types, indistinguishable by ALC) release their hold. An element
			// whose ContentRoot has been unregistered from the coordinator (a closed secondary
			// app's tree — popups included, which keep IsLoaded) is orphaned and also released.
			// The collectible branch is gated on the parent's ALC unload having actually been
			// initiated: a still-live session-lifetime add-in ALC (e.g. a designer host) is also
			// collectible, but its associations must be preserved until it really unloads.
			var collectibleAndUnloading = false;
			if (_associatedParent is { } collectibleCandidate && collectibleCandidate.GetType().IsCollectible)
			{
				var parentAlc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(collectibleCandidate.GetType().Assembly);
				// Conservative when the unload state can't be read: do NOT clear, so a still-live add-in
				// ALC's associations (and inherited DataContext) are never dropped on an unreadable state.
				// FeatureConfiguration.Alc.ThrowOnUnloadStateReadFailure surfaces such a runtime change in dev.
				collectibleAndUnloading = parentAlc is not null
					&& global::Uno.UI.Xaml.Core.AlcStateHelper.IsUnloadInitiated(parentAlc, valueIfUnknown: false);
			}

			if (_associatedParent is { } parent
				&& (collectibleAndUnloading
					|| parent is FrameworkElement { IsLoaded: false }
					|| (parent is FrameworkElement orphanCandidate && IsOrphanedFromContentRoots(orphanCandidate))))
			{
				_associatedParent = null;
				associationCleared = true;
			}

			// The association may already have propagated the parent's DataContext into this
			// store at Inheritance precedence; that propagated value (e.g. the secondary app's
			// view model) survives the parent's removal and pins the value's ALC on its own.
			// Drop it only when the association was cleared, or when the value itself belongs to
			// a collectible ALC whose unload has begun — a value from a still-live add-in ALC
			// must be preserved (mirrors the conservative _associatedParent gating above).
			var dataContextFromUnloadingAlc = false;
			if (!associationCleared
				&& GetValue(_dataContextProperty) is { } dataContext
				&& dataContext.GetType().IsCollectible)
			{
				var valueAlc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(dataContext.GetType().Assembly);
				dataContextFromUnloadingAlc = valueAlc is not null
					&& global::Uno.UI.Xaml.Core.AlcStateHelper.IsUnloadInitiated(valueAlc, valueIfUnknown: false);
			}

			if (associationCleared || dataContextFromUnloadingAlc)
			{
				ClearInheritedDataContext();
			}
		}

		/// <summary>
		/// Returns whether the element's visual tree no longer has a registered ContentRoot —
		/// i.e. it belonged to a closed (secondary-app) tree whose root was unregistered from
		/// the <see cref="Uno.UI.Xaml.Core.ContentRootCoordinator"/> on Window close.
		/// </summary>
		private static bool IsOrphanedFromContentRoots(FrameworkElement element)
		{
			try
			{
				var contentRoot = element.XamlRoot?.VisualTree?.ContentRoot;
				if (contentRoot is null)
				{
					return true;
				}

				return !Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator.ContentRoots.Contains(contentRoot);
			}
			catch (Exception)
			{
				// Fail leak-safe: if orphan state can't be determined, treat the element as orphaned
				// so its association is cleared rather than left to pin a potentially-unloaded ALC.
				return true;
			}
		}

		// Stores that recorded a collectible-ALC object as their associated parent, grouped by
		// that parent's ALC. Entries are swept (associated parent cleared) when the ALC unloads,
		// so a host-lifetime shared resource can never outlive-pin a secondary app's ALC.
		// CWT-keyed by the ALC so the registry itself never extends the ALC's lifetime.
		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<global::System.Runtime.Loader.AssemblyLoadContext, List<WeakReference<DependencyObjectStore>>> _collectibleParentAssociations = new();

		private void RegisterCollectibleParentAssociation(object parent)
		{
			// Collectible-ALC association tracking is only meaningful when secondary ALCs exist.
			// Gate on the substitutable Application.HasSecondaryApps so that, for the overwhelmingly
			// common single-ALC app, this whole path stays off and is linkable away.
			if (!Application.HasSecondaryApps)
			{
				return;
			}

			// Fast path: Type.IsCollectible is a cached runtime flag; non-collectible parents
			// (the overwhelmingly common case) exit here without touching the registry.
			if (!parent.GetType().IsCollectible)
			{
				return;
			}

			var alc = global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(parent.GetType().Assembly);
			if (alc is null || !alc.IsCollectible)
			{
				return;
			}

			var list = _collectibleParentAssociations.GetValue(alc, static a =>
			{
				a.Unloading += static unloading =>
				{
					// ClearCollectibleAssociatedParent reads/writes DependencyProperty values, which is
					// only valid on the UI thread. Unloading fires on whichever thread called
					// AssemblyLoadContext.Unload(); marshal to the UI thread so the clear actually runs
					// instead of being swallowed as an off-thread DP-access failure.
					static void Sweep(global::System.Runtime.Loader.AssemblyLoadContext unloading)
					{
						if (_collectibleParentAssociations.TryGetValue(unloading, out var stores))
						{
							lock (stores)
							{
								foreach (var weakStore in stores)
								{
									if (weakStore.TryGetTarget(out var store))
									{
										try
										{
											store.ClearCollectibleAssociatedParent();
										}
										catch (global::System.Exception)
										{
											// Defensive: one store throwing must not abort the sweep for the rest.
										}
									}
								}

								stores.Clear();
							}

							_collectibleParentAssociations.Remove(unloading);
						}
					}

					if (global::Uno.UI.Dispatching.NativeDispatcher.Main.HasThreadAccess)
					{
						Sweep(unloading);
					}
					else
					{
						global::Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(() => Sweep(unloading));
					}
				};
				return new List<WeakReference<DependencyObjectStore>>();
			});

			lock (list)
			{
				list.Add(new WeakReference<DependencyObjectStore>(this));
			}
		}
	}
}
