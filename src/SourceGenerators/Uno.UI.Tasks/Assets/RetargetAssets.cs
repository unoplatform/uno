using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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
			Log.LogMessage($"Retargeting UWP assets to {TargetPlatform}.");

			Func<ResourceCandidate, string> resourceToTargetPath;
			Func<string, string> pathEncoder;
			
			switch (TargetPlatform)
			{
				case "ios":
					resourceToTargetPath = resource => iOSResourceConverter.Convert(resource, DefaultLanguage);
					pathEncoder = p => p;
					break;
				case "android":
					resourceToTargetPath = resource => AndroidResourceConverter.Convert(resource, DefaultLanguage);
					pathEncoder = AndroidResourceNameEncoder.EncodeFileSystemPath;
					break;
				default:
					Log.LogMessage($"Skipping unknown platform {TargetPlatform}");
					return true;
			}

			Assets = ContentItems.ToArray();
			RetargetedAssets = Assets
				.Select((Func<ITaskItem, TaskItem>)(asset => ProcessContentItem(asset, resourceToTargetPath, pathEncoder)))
				.Where(a => a != null)
				.ToArray();

			return true;
		}

		private TaskItem ProcessContentItem(ITaskItem asset, Func<ResourceCandidate, string> resourceToTargetPath, Func<string, string> pathEncoder)
		{
			if (
				!asset.MetadataNames.OfType<string>().Contains("Link")
				&& !asset.MetadataNames.OfType<string>().Contains("DefiningProjectDirectory")
			)
			{
				Log.LogMessage($"Skipping '{asset.ItemSpec}' because 'Link' or 'DefiningProjectDirectory' metadata is not set.");
				return null;
			}

			var fullPath = asset.GetMetadata("FullPath");
			var relativePath = asset.GetMetadata("Link");

			if (string.IsNullOrEmpty(relativePath))
			{
				relativePath = fullPath.Replace(asset.GetMetadata("DefiningProjectDirectory"), "");
			}

			if (IsImageAsset(asset.ItemSpec))
			{
				var resourceCandidate = ResourceCandidate.Parse(fullPath, relativePath);

				if (!UseHighDPIResources && int.TryParse(resourceCandidate.GetQualifierValue("scale"), out var scale) && scale > HighDPIThresholdScale)
				{
					Log.LogMessage($"Skipping '{asset.ItemSpec}' of scale {scale} because {nameof(UseHighDPIResources)} is false.");
					return null;
				}

				var targetPath = resourceToTargetPath(resourceCandidate);

				if (targetPath == null)
				{
					Log.LogMessage($"Skipping '{asset.ItemSpec}' as it's not supported on {TargetPlatform}.");
					return null;
				}

				Log.LogMessage($"Retargeting image '{asset.ItemSpec}' to '{targetPath}'.");
				return new TaskItem(
					asset.ItemSpec,
					new Dictionary<string, string>() {
						{ "LogicalName", targetPath },
						{ "AssetType", "image" }
					});
			}
			else
			{
				var encodedRelativePath = pathEncoder(relativePath);

				Log.LogMessage($"Retargeting generic '{asset.ItemSpec}' to '{encodedRelativePath}'.");
				return new TaskItem(
					asset.ItemSpec,
					new Dictionary<string, string>() {
						{ "LogicalName", encodedRelativePath },
						{ "AssetType", "generic" }
					});
			}
		}

		private static bool IsImageAsset(string path)
		{
			var extension = Path.GetExtension(path).ToLowerInvariant();
			return extension == ".png"
				|| extension == ".jpg"
				|| extension == ".jpeg"
				|| extension == ".gif";
		}
	}
}
