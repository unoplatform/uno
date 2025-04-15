#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;
using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Uno.UI.Tasks.RuntimeAssetsValidator;
/// <summary>
/// A task used to merge linker definition files and embed the result in an assembly
/// </summary>
public class RuntimeAssetsValidatorTask_v0 : Microsoft.Build.Utilities.Task
{
	[Required]
	public Microsoft.Build.Framework.ITaskItem[]? RuntimeCopyLocalItemsInput { get; set; }

	public string UnoRuntimeIdentifier { get; set; } = "";

	public string UnoUIRuntimeIdentifier { get; set; } = "";

	public override bool Execute()
	{
		bool succeeded = true;

		try
		{
			if (UnoRuntimeIdentifier == "reference")
			{
				return true;
			}

			foreach (var assembly in RuntimeCopyLocalItemsInput ?? [])
			{
				var originalAssembly = AssemblyDefinition.ReadAssembly(assembly.GetMetadata("FullPath"));

				if (!originalAssembly.MainModule.AssemblyReferences.Any(m => m.Name == "Uno.UI"))
				{
					// We only need to validate assemblies that reference Uno.UI, because this is the only layer
					// that is replaced for the Skia UI layer
					this.Log.LogMessage(MessageImportance.Low, $"Skipping {originalAssembly} validation");
					continue;
				}

				if (
					originalAssembly.HasCustomAttributes
					&& originalAssembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.Reflection.AssemblyMetadataAttribute") is { } metadata
					&& metadata.HasConstructorArguments
					&& metadata.ConstructorArguments.Count == 2
					&& (metadata.ConstructorArguments[0].Value?.ToString().Equals("UnoUIRuntimeIdentifier", StringComparison.OrdinalIgnoreCase) ?? false)
					)
				{
					var identifier = metadata.ConstructorArguments[0].Value.ToString();

					if (!string.IsNullOrEmpty(identifier)
						&& !UnoUIRuntimeIdentifier.Equals(identifier, StringComparison.OrdinalIgnoreCase))
					{
						succeeded = false;

						Log.LogError(
							$"The assembly {assembly.ItemSpec} has a different UnoUIRuntimeIdentifier than the one used to build the project. " +
							$"(Expected: {UnoUIRuntimeIdentifier}, Actual: {identifier})"
						);
					}
				}
			}

			return succeeded;
		}
		catch (Exception e)
		{
			// Require because the task is running out of process
			// and can't marshal non-CLR known exceptions.
			throw new Exception(e.ToString());
		}
	}
}
