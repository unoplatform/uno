#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.UI.Tasks.Assets
{
	/// <summary>
	/// Expands content assets from folders containing an uprimarker file
	/// </summary>
	public class ExpandPackageAssets_v0 : Task
	{
		[Required]
		public ITaskItem[]? MarkerFiles { get; set; }

		[Output]
		public ITaskItem[]? Assets { get; set; }
			= Array.Empty<ITaskItem>();

		public override bool Execute()
		{
			Log.LogMessage($"Expanding package assets");

			if (MarkerFiles != null)
			{
				List<ITaskItem> assets = new();

				foreach (var markerFile in MarkerFiles)
				{
					var markerFileFullPath = markerFile.GetMetadata("FullPath");
					var markerFileDirectory = Path.GetDirectoryName(markerFileFullPath);
					var basePath = Path.Combine(markerFileDirectory, Path.GetFileNameWithoutExtension(markerFileFullPath));

					if (Directory.Exists(basePath))
					{
						Log.LogMessage(MessageImportance.Low, $"Scanning directory ({basePath})");

						var markerFileDirectoryForReplace = markerFileDirectory + Path.DirectorySeparatorChar;

						foreach (var asset in Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories))
						{
							var newItem = new TaskItem(
								asset,
								new Dictionary<string, string>
								{
									["TargetPath"] = asset.Replace(markerFileDirectoryForReplace, "")
								});

							assets.Add(newItem);
						}
					}
					else
					{
						Log.LogMessage(MessageImportance.Low, $"No assets directory found ({basePath})");
					}
				}

				Assets = assets.ToArray();
			}

			return true;
		}
	}
}
