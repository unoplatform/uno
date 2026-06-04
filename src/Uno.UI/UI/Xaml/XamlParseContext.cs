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
				_assemblyLoadContextResolved = true;
				if (!string.IsNullOrEmpty(AssemblyName))
				{
					foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
					{
						if (string.Equals(assembly.GetName().Name, AssemblyName, System.StringComparison.Ordinal))
						{
							var resolved = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
							SetAssemblyLoadContext(resolved);
							return resolved;
						}
					}
				}

				return null;
			}
			set => SetAssemblyLoadContext(value);
		}

		private void SetAssemblyLoadContext(System.Runtime.Loader.AssemblyLoadContext value)
		{
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
