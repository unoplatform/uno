#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;

namespace Uno.UI.Tasks.WinAppSDKValidations;

/// <summary>
/// A task used to merge linker definition files and embed the result in an assembly
/// </summary>
public class ValidateWinAppSDKReferences_v0 : Microsoft.Build.Utilities.Task
{
	[Required]
	public ITaskItem[] ReferencedProjects { get; set; } = Array.Empty<ITaskItem>();

	public override bool Execute()
	{
		foreach (var project in ReferencedProjects)
		{
			string nearestTargetFramework = project.GetMetadata("NearestTargetFramework");

			if (string.IsNullOrEmpty(nearestTargetFramework) || !(project.GetMetadata("AdditionalPropertiesFromProject") is { Length: > 0 }))
			{
				//  Referenced project doesn't have the right metadata.  This may be because it's a different project type (C++, for example)
				//  In this case just skip the checks
				continue;
			}

			var additionalPropertiesXml = XElement.Parse(project.GetMetadata("AdditionalPropertiesFromProject"));
			var targetFrameworkElement = additionalPropertiesXml.Element(nearestTargetFramework);

			Dictionary<string, string> projectAdditionalProperties = new(StringComparer.OrdinalIgnoreCase);

			if (targetFrameworkElement is not null)
			{
				foreach (var propertyElement in targetFrameworkElement.Elements())
				{
					projectAdditionalProperties[propertyElement.Name.LocalName] = propertyElement.Value;
				}

				if (projectAdditionalProperties.TryGetValue("_IsUnoPlatform", out var isUnoPlatformValue)
					&& bool.TryParse(isUnoPlatformValue, out bool isUnoPlatform)
					&& isUnoPlatform)
				{
					Log.LogError(
						subcategory: null,
						errorCode: "UNOB0002",
						helpKeyword: null,
						file: null,
						lineNumber: 0,
						columnNumber: 0,
						endLineNumber: 0,
						endColumnNumber: 0,
						message: "Project {0} contains a reference to Uno Platform but does not contain a WinAppSDK compatible target framework. https://aka.platform.uno/UNOB0002",
						messageArgs: project.ItemSpec
					);

					return false;
				}
			}
		}

		return true;
	}
}
