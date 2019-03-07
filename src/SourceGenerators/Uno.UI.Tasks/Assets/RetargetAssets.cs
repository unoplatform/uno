using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Uno.Logging;
using Uno.UI.Tasks.Helpers;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tasks.Assets
{
	/// <summary>
	/// Retargets UWP assets to Android and iOS.
	/// </summary>
	/// <remarks>
	/// Currently supports .png, .jpg, .jpeg and .gif.
	/// </remarks>
	public class RetargetAssets_v0 : Task
	{
		private const int HighDPIThresholdScale = 150;

		[Required]
		public bool UseHighDPIResources { get; set; }

		[Required]
		public string TargetPlatform { get; set; }

		[Required]
		public string DefaultLanguage { get; set; }

		[Required]
		public ITaskItem[] ContentItems { get; set; }

		[Output]
		public ITaskItem[] Assets { get; set; }

		[Output]
		public ITaskItem[] RetargetedAssets { get; set; }

		public override bool Execute()
		{
			LogExtensionPoint.AmbientLoggerFactory.AddProvider(new TaskLoggerProvider(Log));

			this.Log().Info($"Retargeting UWP assets to {TargetPlatform}.");

			Func<ResourceCandidate, string> resourceToTargetPath;
			switch (TargetPlatform)
			{
				case "ios":
					resourceToTargetPath = resource => iOSResourceConverter.Convert(resource, DefaultLanguage);
					break;
				case "android":
					resourceToTargetPath = resource => AndroidResourceConverter.Convert(resource, DefaultLanguage);
					break;
				default:
					this.Log().Info($"Skipping unknown platform {TargetPlatform}");
					return true;
			}

			Assets = ContentItems.Where(content => IsAsset(content.ItemSpec)).ToArray();
			RetargetedAssets = Assets
				.Select(asset =>
				{
					if (
						!asset.MetadataNames.Contains("Link")
						&& !asset.MetadataNames.Contains("DefiningProjectDirectory")
					)
					{
						this.Log().Info($"Skipping '{asset.ItemSpec}' because 'Link' or 'DefiningProjectDirectory' metadata is not set.");
						return null;
					}

					var fullPath = asset.GetMetadata("FullPath");
					var relativePath = asset.GetMetadata("Link");

					if (string.IsNullOrEmpty(relativePath))
					{
						relativePath = fullPath.Replace(asset.GetMetadata("DefiningProjectDirectory"), "");
					}

					var resourceCandidate = ResourceCandidate.Parse(fullPath, relativePath);

					if (!UseHighDPIResources && int.TryParse(resourceCandidate.GetQualifierValue("scale"), out var scale) && scale > HighDPIThresholdScale)
					{
						this.Log().Info($"Skipping '{asset.ItemSpec}' of scale {scale} because {nameof(UseHighDPIResources)} is false.");
						return null;
					}

					var targetPath = resourceToTargetPath(resourceCandidate);

					if (targetPath == null)
					{
						this.Log().Info($"Skipping '{asset.ItemSpec}' as it's not supported on {TargetPlatform}.");
						return null;
					}
					
					this.Log().Info($"Retargeting '{asset.ItemSpec}' to '{targetPath}'.");
					return new TaskItem(asset.ItemSpec, new Dictionary<string, string>() { { "LogicalName", targetPath } });
				})
				.Trim()
				.ToArray();

			return true;
		}

		private static bool IsAsset(string path)
		{
			var extension = Path.GetExtension(path).ToLowerInvariant();
			return extension == ".png"
				|| extension == ".jpg"
				|| extension == ".jpeg"
				|| extension == ".gif";
		}
	}
}
