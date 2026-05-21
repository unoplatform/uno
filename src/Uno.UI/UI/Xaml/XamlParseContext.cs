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

		private System.Runtime.Loader.AssemblyLoadContext _assemblyLoadContext;
		private bool _assemblyLoadContextResolved;

		public System.Runtime.Loader.AssemblyLoadContext AssemblyLoadContext
		{
			get
			{
				if (_assemblyLoadContext is not null || _assemblyLoadContextResolved)
				{
					return _assemblyLoadContext;
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
							_assemblyLoadContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(assembly);
							break;
						}
					}
				}

				return _assemblyLoadContext;
			}
			set
			{
				_assemblyLoadContext = value;
				_assemblyLoadContextResolved = value is not null;
			}
		}
	}
}
