using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft/* UWP don't rename */.UI.Xaml;

namespace ResourcesExtractor;

public sealed partial class MainWindow : Window
{
	private static List<(string ResourceName, int ResourceId)> GetResources()
	{
		var allResources = new List<(string ResourceName, int ResourceId)>();
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

		var resources = GetResources();
		foreach (var lang in Enum.GetValues<Magic.Languages>())
		{
			var filePath = $"C:\\GeneratedResources\\{lang.ToString().Replace('_', '-')}\\Resources.resw";
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
	}
}
