#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Resources;

[TestClass]
public class Given_GenericXaml
{
	private const string GenericFolder = "src/Uno.UI/UI/Xaml/Style/Generic";

	[TestMethod]
	public void When_Generic_Xaml_Is_Hashed_Then_Matches_WinUI_Provenance()
	{
		var folder = FindGenericFolder();
		var provenance = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "Generic.xaml.provenance.json")));
		var expected = provenance.RootElement.GetProperty("sha256").GetString();
		var tag = provenance.RootElement.GetProperty("tag").GetString();

		var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(folder, "Generic.xaml")))).ToLowerInvariant();

		Assert.AreEqual(
			expected,
			actual,
			$"Generic.xaml must stay byte-identical to the WinUI source at {tag}. " +
			"Put Uno-specific styles in UnoGenericOverlay.xaml, or refresh the file with build/Update-WinUIGenericXaml.ps1.");
	}

	private static string FindGenericFolder()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			var candidate = Path.Combine(directory.FullName, GenericFolder);
			if (File.Exists(Path.Combine(candidate, "Generic.xaml")))
			{
				return candidate;
			}

			directory = directory.Parent;
		}

		Assert.Fail($"Could not locate {GenericFolder} above {AppContext.BaseDirectory}.");
		return string.Empty;
	}
}
