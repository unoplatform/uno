using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Uno.Sdk.MacDev;
using Uno.Sdk.Services;

namespace Uno.Sdk.Tasks;
public class GenerateSkiaDesktopMacOSBundle_v0 : SdkTask
{
	[Required]
	public string AssemblyName { get; set; } = null!;

	[Required]
	public string BundledDotNetVersion { get; set; } = null!;

	[Required]
	public string UnoVersion { get; set; } = null!;
	public string TargetCulture { get; set; } = null!;
	public string ApplicationDisplayVersion { get; set; } = null!;
	public string ApplicationVersion { get; set; } = null!;

	[Required]
	public string AppBundleDirectory { get; set; } = null!;

	public ITaskItem[] PartialPlists { get; set; } = [];

	public ITaskItem[] EntitlementPlists { get; set; } = [];

	public ITaskItem[] AppIconSets { get; set; } = [];

	[Required]
	public ITaskItem[] EmbeddedResources { get; set; } = [];

	protected override void ExecuteInternal()
	{
		var infoPlistPathInfo = new FileInfo(Path.Combine(AppBundleDirectory, "Info.plist"));

		GetMergePlist(PartialPlists, CreateInfoPlist()).Save(infoPlistPathInfo.FullName);
		var entitlements = GetMergePlist(EntitlementPlists);
		if (entitlements.Any())
		{
			var entitlementsPlistPathInfo = new FileInfo(Path.Combine(AppBundleDirectory, "Extras", "Entitlements.plist"));
			entitlementsPlistPathInfo.Directory.Create();
			entitlements.Save(entitlementsPlistPathInfo.FullName);
		}
	}

	private PDictionary CreateInfoPlist()
	{
		var packageAppxManifestItem = EmbeddedResources.Single(x => x.HasMetadata("LogicalName") && x.GetMetadata("LogicalName") == "Package.appxmanifest");
		if (packageAppxManifestItem is null || !File.Exists(packageAppxManifestItem.ItemSpec))
		{
			throw new FileNotFoundException("Could not locate the Package.appxmanifest.");
		}
		var appxManifest = new AppxManifestReader(packageAppxManifestItem.ItemSpec, this);

		var appIconSetItem = AppIconSets.SingleOrDefault() ?? throw new FileNotFoundException("Could not locate appiconst.");
		var iconAppSet = new DirectoryInfo(appIconSetItem.ItemSpec);

		return new PDictionary
		{
			{ "UnoVersion", UnoVersion },
			{ "BundledNETCorePlatformsPackageVersion", BundledDotNetVersion },
			{ "CFBundleDisplayName", appxManifest.DisplayName },
			{ "CFBundleName", appxManifest.DisplayName },
			{ "CFBundleExecutable", $"{AssemblyName}.exe" },
			{ "CFBundleGetInfoString", appxManifest.Version },
			{ "CFBundleLongVersionString", appxManifest.Version },
			{ "CFBundleInfoDictionaryVersion", ApplicationDisplayVersion },
			{ "CFBundleVersion", ApplicationDisplayVersion },
			{ "CFBundleShortVersionString", ApplicationVersion },
			{ "CFBundleIdentifier", appxManifest.Name },
			{ "CFBundlePackageType", "APPL" },
			{ "CFBundleSignature", "????" },
			{ "CFBundleSupportedPlatforms", new PArray { "MacOSX" } },
			{ "NSPrincipalClass", "NSApplication" },
			{ "CFBundleDevelopmentRegion", TargetCulture },
			{ "CFBundleIconFile", Path.GetFileNameWithoutExtension(iconAppSet.Name) }
		};
	}

	public static PDictionary GetMergePlist(ITaskItem[] items, PDictionary? initialDictionary = null)
	{
		var defaultPlistPath = items.SingleOrDefault(x => x.HasMetadata("IsDefaultItem") && bool.TryParse(x.GetMetadata("IsDefaultItem"), out var isDefault) && isDefault);
		var plist = initialDictionary?.Clone() as PDictionary ?? [];

		if (!string.IsNullOrEmpty(defaultPlistPath?.ItemSpec))
		{
			Merge(plist, PDictionary.FromFile(defaultPlistPath!.ItemSpec)!);
		}

		foreach (var item in items)
		{
			if (item == defaultPlistPath)
			{
				continue;
			}

			var partial = PDictionary.FromFile(item.ItemSpec);
			Merge(plist, partial);
		}

		return plist;
	}

	private static void Merge(PDictionary source, PDictionary? newSource)
	{
		if (newSource is null || newSource.Count == 0)
		{
			return;
		}

		foreach (var kvp in newSource)
		{
			if (string.IsNullOrEmpty(kvp.Key))
			{
				continue;
			}

			source[kvp.Key!] = kvp.Value;
		}
	}
}
