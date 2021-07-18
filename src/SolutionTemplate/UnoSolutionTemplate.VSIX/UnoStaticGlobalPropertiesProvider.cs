using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Task = System.Threading.Tasks.Task;

namespace UnoSolutionTemplate
{
	// https://github.com/microsoft/VSProjectSystem/blob/4ad5716f8fca76403699b818c3d907ce8a8b9a38/doc/extensibility/IProjectGlobalPropertiesProvider.md
	[ExportBuildGlobalPropertiesProvider]
	[AppliesTo("")]
	public class UnoStaticGlobalPropertiesProvider : StaticGlobalPropertiesProviderBase
	{
#if DEBUG
		static UnoStaticGlobalPropertiesProvider()
		{
			// Note that the type ctor is invoked, but not the ctor, this generally means that the IProjectService
			// version referenced by the package is not using the proper version (has to match vs16 or vs15)
		}
#endif

		[ImportingConstructor]
		public UnoStaticGlobalPropertiesProvider(IProjectService projectService)
			: base(projectService.Services)
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
		}

		public override async System.Threading.Tasks.Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(System.Threading.CancellationToken cancellationToken)
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering GetGlobalPropertiesAsync for: {0}", this.ToString()));

			var current = Empty.PropertiesMap;

			if (UnoPlatformPackage.GlobalFunctionProvider != null)
			{
				foreach (var props in await UnoPlatformPackage.GlobalFunctionProvider())
				{
					current = current.Add(props.Key, props.Value);
				}
			}

			return current;
		}
	}
}
