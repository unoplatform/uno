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
	/// Retargets assets to Android and iOS.
	/// </summary>
	/// <remarks>
	/// Currently supports .png, .jpg, .jpeg and .gif.
	/// </remarks>
	public partial class RetargetAssets_v0 : Task
	{
		private const int HighDPIThresholdScale = 150;

		[Required]
		public bool UseHighDPIResources { get; set; }

		[Required]
		public string TargetPlatform { get; set; }

		[Required]
		public string IntermediateOutputPath { get; set; }

		public string AndroidAssetsPrefix { get; set; }

		[Required]
		public string DefaultLanguage { get; set; }

		[Required]
		public ITaskItem[] ContentItems { get; set; }

		[Output]
		public ITaskItem[] Assets { get; set; }

		[Output]
		public ITaskItem[] RetargetedAssets { get; set; }

		[Output]
		public ITaskItem[] PartialAppManifests { get; set; }

		public override bool Execute()
		{
			Log.LogMessage($"Retargeting assets to {TargetPlatform}.");

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
					pathEncoder = s => AndroidResourceNameEncoder.EncodeFileSystemPath(s, AndroidAssetsPrefix ?? "Assets");
					break;
				default:
					Log.LogMessage($"Skipping unknown platform {TargetPlatform}");
					return true;
			}

			Assets = ContentItems.ToArray();

			ProcessContentItems(Assets, resourceToTargetPath, pathEncoder);

			return true;
		}

		private void ProcessContentItems(ITaskItem[] assets, Func<ResourceCandidate, string> resourceToTargetPath, Func<string, string> pathEncoder)
		{
			List<TaskItem> retargetdAssets = new();
			List<string> fontAssets = new();

			foreach (var asset in assets)
			{
				if (
					!asset.MetadataNames.OfType<string>().Contains("Link")
					&& !asset.MetadataNames.OfType<string>().Contains("TargetPath")
					&& !asset.MetadataNames.OfType<string>().Contains("DefiningProjectDirectory")
				)
				{
					Log.LogMessage($"Skipping '{asset.ItemSpec}' because 'Link', 'TargetPath' or 'DefiningProjectDirectory' metadata is not set.");
					continue;
				}

				var fullPath = asset.GetMetadata("FullPath");
				var relativePath = asset.GetMetadata("Link") is { Length: > 0 } link
					? link
					: asset.GetMetadata("TargetPath");

				if (string.IsNullOrEmpty(relativePath))
				{
					relativePath = fullPath.Replace(asset.GetMetadata("DefiningProjectDirectory"), "");
				}

				relativePath = AlignPath(relativePath);

				if (IsImageAsset(asset.ItemSpec))
				{
					var resourceCandidate = ResourceCandidate.Parse(fullPath, relativePath);

					if (!UseHighDPIResources && int.TryParse(resourceCandidate.GetQualifierValue("scale"), out var scale) && scale > HighDPIThresholdScale)
					{
						Log.LogMessage($"Skipping '{asset.ItemSpec}' of scale {scale} because {nameof(UseHighDPIResources)} is false.");
						continue;
					}

					var targetPath = resourceToTargetPath(resourceCandidate);

					if (targetPath == null)
					{
						Log.LogMessage($"Skipping '{asset.ItemSpec}' as it's not supported on {TargetPlatform}.");
						continue;
					}

					Log.LogMessage($"Retargeting image '{asset.ItemSpec}' to '{targetPath}'.");

					var item = new TaskItem(
						asset.ItemSpec,
						new Dictionary<string, string>
						{
							["LogicalName"] = targetPath,
							["AssetType"] = "image"
						});

					retargetdAssets.Add(item);
				}
				else if (IsFontAsset(asset.ItemSpec))
				{
					var encodedRelativePath = pathEncoder(relativePath);

					Log.LogMessage($"Retargeting font '{asset.ItemSpec}' to '{encodedRelativePath}'.");

					fontAssets.Add(encodedRelativePath);

					retargetdAssets.Add(
						new(
							asset.ItemSpec,
							new Dictionary<string, string>
							{
								["LogicalName"] = encodedRelativePath,
								["AssetType"] = "generic"
							}));
				}
				else
				{
					var encodedRelativePath = pathEncoder(relativePath);

					Log.LogMessage($"Retargeting generic '{asset.ItemSpec}' to '{encodedRelativePath}'.");

					retargetdAssets.Add(
						new(
							asset.ItemSpec,
							new Dictionary<string, string>
							{
								["LogicalName"] = encodedRelativePath,
								["AssetType"] = "generic",
							}));
				}
			}

			RetargetedAssets = retargetdAssets.ToArray();
			PartialAppManifests = GenerateFontPartialManifest(fontAssets);
		}


		private bool IsFontAsset(string path)
			=> Path.GetExtension(path).ToLowerInvariant() is ".ttf"
				or ".otf"
				or ".woff"
				or ".woff2";

		private static bool IsImageAsset(string path)
			=> Path.GetExtension(path).ToLowerInvariant() is ".png"
				or ".jpg"
				or ".jpeg"
				or ".gif";

		private static string AlignPath(string path)
			=> path
			.Replace('/', Path.DirectorySeparatorChar)
			.Replace('\\', Path.DirectorySeparatorChar);
	}
}
