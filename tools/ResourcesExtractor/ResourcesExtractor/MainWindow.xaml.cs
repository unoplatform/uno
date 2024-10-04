using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft/* UWP don't rename */.UI.Xaml;
using Windows.Storage;
using Windows.System;

namespace ResourcesExtractor;

public sealed partial class MainWindow : Window
{
	private static List<(string ResourceName, int ResourceId)> GetResources()
	{
		var allResources = new List<(string ResourceName, int ResourceId)>();

		for (var i = 5114; i <= 5155; i++)
		{
			allResources.Add((i.ToString(CultureInfo.InvariantCulture), i));
		}

		allResources.AddRange(GetResourcesFromFile("dxaml\\phone\\lib\\PhoneResource.h"));
		allResources.AddRange(GetResourcesFromFile("dxaml\\xcp\\inc\\localizedResource.h"));

		return allResources;
	}

	private static List<(string ResourceName, int ResourceId)> GetResourcesFromFile(string relativePath)
	{
		var lines = File.ReadAllLines($"C:\\Dev\\microsoft-ui-xaml\\{relativePath}");
		var resources = new List<(string ResourceName, int ResourceId)>();
		foreach (var line in lines)
		{
			if (line.StartsWith("#define"))
			{
				var match = Regex.Match(line, @"#define (.+?)\s+(\d+)");
				var resourceName = match.Groups[1].Value;
				var resourceId = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
				resources.Add((resourceName, resourceId));
			}
		}

		return resources;
	}

	public MainWindow()
	{
		this.InitializeComponent();

		var rootDirectory = "C:\\GeneratedResources\\";

		var resources = GetResources();
		foreach (var lang in Enum.GetValues<Magic.Languages>())
		{
			var filePath = $"{rootDirectory}{lang.ToString().Replace('_', '-')}\\Resources.resw";
			var directory = Path.GetDirectoryName(filePath);
			Directory.CreateDirectory(directory);
			var writer = new StreamWriter(new FileStream(filePath, FileMode.CreateNew));
			writer.Write(Constants.ReswFileStart);
			foreach (var resource in resources)
			{
				string resourceValue = Magic.GetLocalizedResource(resource.ResourceId, (int)lang);
				if (resourceValue != null)
				{
					writer.Write($"""
                      <data name="{resource.ResourceName}" xml:space="preserve">
                        <value>{resourceValue}</value>
                      </data>

                    """);
				}
			}

			writer.Write(Constants.ReswFileEnd);
			writer.Close();
		}

		OpenFileManagerAsync(rootDirectory).ConfigureAwait(false);
	}

	private async Task OpenFileManagerAsync(string directoryPath)
	{
		try
		{
			var folder = await StorageFolder.GetFolderFromPathAsync(directoryPath);
			await Launcher.LaunchFolderAsync(folder);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
		}
	}
}
