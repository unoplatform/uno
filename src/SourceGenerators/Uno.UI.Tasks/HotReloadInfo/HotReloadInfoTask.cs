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
		var attributePath = Path.Combine(IntermediateOutputPath, "uno.hot-reload.info", "HotReloadInfo.Attribute.g.cs");
		var infoPath = Path.Combine(IntermediateOutputPath, "uno.hot-reload.info", "HotReloadInfo.g.cs");

		Log.LogMessage($"Generating hot-reload info to : {infoPath}");

		try
		{
			Directory.CreateDirectory(Path.Combine(IntermediateOutputPath, "uno.hot-reload.info"));

			Write(attributePath, HotReloadInfoHelper.GenerateAttribute(infoPath));
			Write(infoPath, HotReloadInfoHelper.GenerateInfo());

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

	private void Write(string path, string content)
	{
		using var stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

		// First we validate if the content is the same, if so, we don't even touch the file (to avoid unnecessary rebuilds)
		using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, leaveOpen: true))
		{
			if (content.Equals(reader.ReadToEnd()))
			{
				return;
			}
		}

		// Rewrite the content ... from the beginning!
		stream.SetLength(0);
		stream.Position = 0;

		using var writer = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true);
		writer.Write(content);
		writer.Flush();
	}
}
