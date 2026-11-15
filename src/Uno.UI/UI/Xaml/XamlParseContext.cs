using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// Provides additional information on the context in which Xaml is being parsed by Uno.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class XamlParseContext
	{
		// *************** WARNING ***************
		// This class instance is not being replaced when a ResourceDictionary is being reloaded (i.e. we continue to use the instance of the original type)
		// This is valid only because all information hosted by this class doesn't change on hot-reload.
		//
		// The one exception is the AssemblyLoadContext below: it is held WEAKLY (for collectibility — see that
		// field's remarks) and so its resolved value can lapse. That does not violate the reuse rule: while the app
		// is alive the ALC identity is stable, and the reference only dies once that ALC is unloaded — which is
		// teardown, not hot-reload.
		//
		// Any new property on this class should follow this same rule, or the resolution of this has to be changed to support hot-reload properly
		// (search for "__ParseContext_" in the xaml generator).
		// ***************************************

		public string AssemblyName { get; set; }

		// The non-default ALC is held WEAKLY so that long-lived XamlParseContext instances
		// (e.g. captured by ResourceBinding/ThemeResourceReference on elements reachable from
		// non-collectible statics such as generated GlobalStaticResources singletons) never
		// pin a collectible AssemblyLoadContext. While the secondary app is active its ALC is
		// strongly rooted by the hosting side (live windows, visual tree, executing threads);
		// once the host releases it and Unload() completes, this weak reference dies and the
		// previous app's whole object graph becomes collectible.
		private System.WeakReference<System.Runtime.Loader.AssemblyLoadContext> _assemblyLoadContext;

		// The default ALC is process-immortal: hold it directly to avoid a pointless
		// WeakReference allocation (and TryGetTarget would always succeed anyway).
		private System.Runtime.Loader.AssemblyLoadContext _defaultAssemblyLoadContext;

		// Latches the lazy by-AssemblyName resolution when no matching assembly is loaded,
		// so repeated misses don't re-scan AppDomain on every access.
		private bool _assemblyLoadContextResolved;

		// The distinct load contexts of every loaded copy of AssemblyName, recorded when the lazy
		// resolution found MORE than one. Held weakly for the same collectibility reason as the
		// resolved ALC. While at least two of them are alive the context stays ambiguous (see
		// IsAssemblyLoadContextAmbiguous); once unloads leave a single copy the scan re-runs and latches it.
		private System.WeakReference<System.Runtime.Loader.AssemblyLoadContext>[] _ambiguousAssemblyLoadContexts;

		public System.Runtime.Loader.AssemblyLoadContext AssemblyLoadContext
		{
			get
			{
				if (_defaultAssemblyLoadContext is not null)
				{
					return _defaultAssemblyLoadContext;
				}

				if (_assemblyLoadContext is not null)
				{
					if (_assemblyLoadContext.TryGetTarget(out var alc))
					{
						return alc;
					}

					// The previously resolved ALC was unloaded and collected. Drop the dead
					// reference and re-run the lazy resolution below: when the same logical
					// app has been re-loaded (hot reload), a same-name assembly now lives in
					// a NEW ALC and must be picked up — mirroring the "bump to the live
					// registration" behavior in ResourceResolver.
					_assemblyLoadContext = null;
					_assemblyLoadContextResolved = false;
				}

				if (_ambiguousAssemblyLoadContexts is { } candidates)
				{
					if (CountAlive(candidates) >= 2)
					{
						// Still loaded in several contexts: the name alone cannot say which copy this is.
						return null;
					}

					// Enough copies were unloaded that a single one may remain: re-scan and latch it.
					_ambiguousAssemblyLoadContexts = null;
					_assemblyLoadContextResolved = false;
				}

				if (_assemblyLoadContextResolved)
				{
					return null;
				}

				// Lazily resolve from AssemblyName so secondary-ALC consumers are reachable
				// even when the XAML codegen wasn't invoked with EnableAlcAppSupport (i.e.
				// the AssemblyLoadContext setter was not emitted into __ParseContext_).
				// Without this, ResourceResolver.TryTopLevelRetrieval would fall back to
				// Application.Current (the host) and miss resources defined only in the
				// secondary application's Resources.
				//
				// The name identifies an assembly, not a copy of it. When the same assembly is
				// loaded in several contexts — a library the host and a hosted app both reference,
				// each side holding its own copy — no choice is right for every caller, and taking
				// the first match would silently pick the host's copy for the app's XAML. That case
				// is recorded as ambiguous and resolves to null; ResourceResolver then treats the
				// lookup as provisional (see ShouldDeferStaticResourceToLoading) instead of final.
				_assemblyLoadContextResolved = true;
				if (!string.IsNullOrEmpty(AssemblyName))
				{
					System.Runtime.Loader.AssemblyLoadContext single = null;
					List<System.Runtime.Loader.AssemblyLoadContext> distinct = null;

					foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
					{
						if (!string.Equals(assembly.GetName().Name, AssemblyName, System.StringComparison.Ordinal))
						{
							continue;
						}

						var candidate = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
						if (candidate is null)
						{
							continue;
						}

						if (single is null)
						{
							single = candidate;
						}
						else if (!ReferenceEquals(single, candidate))
						{
							distinct ??= new List<System.Runtime.Loader.AssemblyLoadContext> { single };
							if (!distinct.Contains(candidate))
							{
								distinct.Add(candidate);
							}
						}
					}

					if (distinct is not null)
					{
						var weak = new System.WeakReference<System.Runtime.Loader.AssemblyLoadContext>[distinct.Count];
						for (var i = 0; i < weak.Length; i++)
						{
							weak[i] = new System.WeakReference<System.Runtime.Loader.AssemblyLoadContext>(distinct[i]);
						}

						_ambiguousAssemblyLoadContexts = weak;
						return null;
					}

					if (single is not null)
					{
						SetAssemblyLoadContext(single);
						return single;
					}
				}

				return null;
			}
			set => SetAssemblyLoadContext(value);
		}

		/// <summary>
		/// True when <see cref="AssemblyName"/> is loaded in more than one <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
		/// and no context was stamped explicitly, so <see cref="AssemblyLoadContext"/> cannot identify
		/// the copy this context belongs to and returns null. Resource lookups made through an ambiguous
		/// context are provisional: they cannot be attributed to an owning application up front.
		/// </summary>
		internal bool IsAssemblyLoadContextAmbiguous
		{
			get
			{
				// Runs the lazy resolution (and its re-scan after unloads) so the answer is current.
				_ = AssemblyLoadContext;
				return _ambiguousAssemblyLoadContexts is not null;
			}
		}

		private static int CountAlive(System.WeakReference<System.Runtime.Loader.AssemblyLoadContext>[] candidates)
		{
			var alive = 0;
			foreach (var candidate in candidates)
			{
				if (candidate.TryGetTarget(out _))
				{
					alive++;
				}
			}

			return alive;
		}

		private void SetAssemblyLoadContext(System.Runtime.Loader.AssemblyLoadContext value)
		{
			_ambiguousAssemblyLoadContexts = null;

			if (value is null)
			{
				_defaultAssemblyLoadContext = null;
				_assemblyLoadContext = null;
				_assemblyLoadContextResolved = false;
			}
			else if (ReferenceEquals(value, System.Runtime.Loader.AssemblyLoadContext.Default))
			{
				_defaultAssemblyLoadContext = value;
				_assemblyLoadContext = null;
				_assemblyLoadContextResolved = true;
			}
			else
			{
				_defaultAssemblyLoadContext = null;
				_assemblyLoadContext = new System.WeakReference<System.Runtime.Loader.AssemblyLoadContext>(value);
				_assemblyLoadContextResolved = true;
			}
		}
	}
}
