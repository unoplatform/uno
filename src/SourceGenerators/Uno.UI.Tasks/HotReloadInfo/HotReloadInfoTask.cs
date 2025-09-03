using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.UI.Tasks.HotReloadInfo;

public class HotReloadInfoTask_v0 : Microsoft.Build.Utilities.Task
{
	[Required]
	public string IntermediateOutputPath { get; set; }

	[Output]
	public ITaskItem[] GeneratedFiles { get; set; }

	/// <inheritdoc />
	public override bool Execute()
	{
		var attributePath = Path.Combine(IntermediateOutputPath, "Uno.HotReloadInfo.Attribute.g.cs");
		var infoPath = Path.Combine(IntermediateOutputPath, "Uno.HotReloadInfo.g.cs");

		Log.LogMessage($"Generating hot-reload info to : {infoPath}");

		try
		{
			File.WriteAllText(attributePath, HotReloadInfoHelper.GenerateAttribute(infoPath));
			File.WriteAllText(infoPath, HotReloadInfoHelper.GenerateInfo());

			GeneratedFiles =
			[
				new TaskItem(attributePath, new Dictionary<string, string>
				{
					{ "Generator", "Uno.UI.Tasks.HotReloadInfo" },
					{ "GeneratorVersion", typeof(HotReloadInfoTask_v0).Assembly.GetName().Version.ToString() }
				}),
				new TaskItem(infoPath, new Dictionary<string, string>
				{
					{ "Generator", "Uno.UI.Tasks.HotReloadInfo" },
					{ "GeneratorVersion", typeof(HotReloadInfoTask_v0).Assembly.GetName().Version.ToString() }
				})
			];

			return true;
		}
		catch (Exception ex)
		{
			Log.LogError($"Failed to generate hot-reload info. Details: {ex.Message}");
		}

		return false;
	}
}
