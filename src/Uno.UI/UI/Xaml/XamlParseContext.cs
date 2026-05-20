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

		[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
			"Trimming",
			"IL2026:RequiresUnreferencedCode",
			Justification = "Stack walk inspects the declaring assembly name only; types are not introspected and trimming preserves XAML-emitted code paths.")]
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
					// ALC-aware lookup: when the same assembly name is loaded into multiple
					// AssemblyLoadContexts (e.g. Uno.UI.HotDesign.Client.Core in both the host
					// ALC and a per-sample inner ALC), AppDomain.CurrentDomain.GetAssemblies()
					// enumerates all of them and the first-match wins non-deterministically.
					// 1) Prefer a contextual reflection ALC if one is set on the current thread
					//    (e.g. inner-ALC code that used EnterContextualReflection while creating
					//    UI). 2) Walk the call stack to find the first frame whose declaring
					//    assembly matches AssemblyName — that is the ALC that emitted this parse
					//    context. 3) Last-resort AppDomain enumeration.
					var contextualAlc = System.Runtime.Loader.AssemblyLoadContext.CurrentContextualReflectionContext;
					if (contextualAlc is not null)
					{
						foreach (var assembly in contextualAlc.Assemblies)
						{
							if (string.Equals(assembly.GetName().Name, AssemblyName, System.StringComparison.Ordinal))
							{
								_assemblyLoadContext = contextualAlc;
								return _assemblyLoadContext;
							}
						}
					}

					var stack = new System.Diagnostics.StackTrace(skipFrames: 1, fNeedFileInfo: false);
					for (int i = 0; i < stack.FrameCount; i++)
					{
						var frameAssembly = stack.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
						if (frameAssembly is null)
						{
							continue;
						}

						if (string.Equals(frameAssembly.GetName().Name, AssemblyName, System.StringComparison.Ordinal))
						{
							_assemblyLoadContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(frameAssembly);
							return _assemblyLoadContext;
						}
					}

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
