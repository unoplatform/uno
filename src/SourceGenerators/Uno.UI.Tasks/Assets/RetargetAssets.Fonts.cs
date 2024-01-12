using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Uno.UI.Tasks.Helpers;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tasks.Assets;

/// <summary>
/// Retargets assets to Android and iOS.
/// </summary>
/// <remarks>
/// Currently supports .png, .jpg, .jpeg and .gif.
/// </remarks>
public partial class RetargetAssets_v0
{
	const string PListHeader = """
		<?xml version="1.0" encoding="UTF-8"?>

		<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
		<plist version="1.0">
		<dict>
		""";

	const string PListFooter = """
		</dict>
		</plist>
		""";

	private ITaskItem[] GenerateFontPartialManifest(List<string> fontAssets, string iOSAppManifest)
	{
		if (TargetPlatform == "ios")
		{
			// For compatibility measures, get the fonts from the iOS app manifest
			// and merge them with the generated ones, so existing apps don't lose
			// explicitly specified ones.
			var existingFonts = EnumerateFontsFromPList(IosAppManifest!);

			var outputManifestFile = Path.Combine(IntermediateOutputPath, "FontsPartialInfo.plist");

			using var writer = File.CreateText(outputManifestFile);

			writer.WriteLine(PListHeader);

			if (fontAssets.Count > 0)
			{
				writer.WriteLine("  <key>UIAppFonts</key>");
				writer.WriteLine("  <array>");

				foreach (var font in fontAssets.Concat(existingFonts))
				{
					writer.WriteLine("    <string>" + font + "</string>");
				}

				writer.WriteLine("  </array>");
			}

			writer.WriteLine(PListFooter);

			return new[] { new TaskItem(outputManifestFile) };
		}
		else
		{
			return Array.Empty<ITaskItem>();
		}
	}

	private string[] EnumerateFontsFromPList(string iosAppManifest)
	{
		XmlDocument doc = new();
		doc.Load(iosAppManifest);

		// Get the list of registered fonts in the info.plist
		return doc
			.SelectNodes("//key[text()='UIAppFonts']/following-sibling::array[1]/string")
			.OfType<XmlNode>()
			.Select(n => n.InnerText)
			.ToArray();
	}
}
