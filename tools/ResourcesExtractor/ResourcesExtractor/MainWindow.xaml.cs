using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;

namespace ResourcesExtractor;

public sealed partial class MainWindow : Window
{
    private static List<(string ResourceName, int ResourceId)> GetResources()
    {
        var lines = File.ReadAllLines("C:\\Users\\PC\\Downloads\\microsoft-ui-xaml-winui3-release-1.4-stable\\microsoft-ui-xaml-winui3-release-1.4-stable\\dxaml\\xcp\\inc\\localizedResource.h");
        var resources = new List<(string ResourceName, int ResourceId)>();
        foreach (var line in lines)
        {
            if (line.StartsWith("#define"))
            {
                var match = Regex.Match(line, @"#define (.+?)\s+(\d+)");
                var resourceName = match.Groups[1].Value;
                var resourceId = int.Parse(match.Groups[2].Value);
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
                writer.Write($"""
                      <data name="{resource.ResourceName}" xml:space="preserve">
                        <value>{resourceValue}</value>
                      </data>

                    """);
            }

            writer.Write(Constants.ReswFileEnd);
            writer.Close();
        }
    }
}
