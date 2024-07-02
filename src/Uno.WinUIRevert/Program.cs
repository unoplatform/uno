#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace UnoWinUIRevert
{
	class Program
	{
		static void Main(string[] args)
		{
			var basePath = args[0];

			DeleteFolder(Path.Combine(basePath, "src", "Uno.UI", "Generated"));
			DeleteFolder(Path.Combine(basePath, "src", "Uno.UI.Composition", "Generated"));
			DeleteFolder(Path.Combine(basePath, "src", "Uno.UWP", "Generated"));
			DeleteFolder(Path.Combine(basePath, "src", "Uno.UI", "tsBindings")); // Generated

			var colorsFilepath = Path.Combine(basePath, @"src", "Uno.UI", "UI", "Colors.cs");
			if (File.Exists(colorsFilepath))
			{
				File.Delete(colorsFilepath);
			}

			var colorHelperFilePath = Path.Combine(basePath, @"src", "Uno.UI", "UI", "ColorHelper.cs");
			if (File.Exists(colorHelperFilePath))
			{
				File.Delete(colorHelperFilePath);
			}

			var fontWeightsFilePath = Path.Combine(basePath, @"src", "Uno.UI", "UI", "Text", "FontWeights.cs");
			if (File.Exists(fontWeightsFilePath))
			{
				File.Delete(fontWeightsFilePath);
			}

			var inputPath = Path.Combine(basePath, "src", "Uno.UI", "UI", "Input");
			if (Directory.Exists(inputPath))
			{
				Directory.Delete(inputPath, true);
			}

			var dispatcherQueuePath = Path.Combine(basePath, "src", "Uno.UI.Dispatching", "Dispatching");
			if (Directory.Exists(dispatcherQueuePath))
			{
				Directory.Delete(dispatcherQueuePath, true);
			}

			ReplaceInFile(Path.Combine(basePath, @"src", "Directory.Build.props"), "<UNO_UWP_BUILD>false</UNO_UWP_BUILD>", "<UNO_UWP_BUILD>true</UNO_UWP_BUILD>");

			// Generic replacements
			var genericReplacements = new[] {
				("Windows.UI.Xaml", "Windows.UI.Xaml"),
				("Windows.UI.Composition", "Windows.UI.Composition"),
				("Windows.UI.Colors", "Windows.UI.Colors"),
				("Windows.UI.Text", "Windows.UI.Text"),
				("Windows.UI.ColorHelper", "Windows.UI.ColorHelper"),
				("__LinkerHints.Is_Windows_UI_Xaml", "__LinkerHints.Is_Windows_UI_Xaml"),
				("__LinkerHints.Is_Microsoft_UI_Xaml_Controls_LayoutPanel", "__LinkerHints.Is_Microsoft_UI_Xaml_Controls_LayoutPanel"),
			};

			ReplaceInFolders(basePath, genericReplacements);

			// Restore ProgressRing
			var progressRingReplacements = new[] {
				("Uno.UI.Controls.Legacy", "Windows.UI.Xaml.Controls"),
			};

			ReplaceInFolders(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing"), progressRingReplacements);
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "\"legacy:ProgressRing\"", "\"ProgressRing\"");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Style", "Generic", "Generic.Native.xaml"), "legacy:ProgressRing", "ProgressRing");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "Microsoft", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "using:Uno.UI.Controls.Legacy", "using:Windows.UI.Xaml.Controls");

			ReplaceInFile(Path.Combine(basePath, @"src", "SourceGenerators", "Uno.UI.SourceGenerators", "XamlGenerator", "XamlConstants.cs"), "Microsoft.UI", "Windows.UI");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Markup", "Reader", "XamlConstants.cs"), "Microsoft.UI", "Windows.UI");

			// Adjust lottie namespace
			var lottieReplacements = new[]
			{
				("CommunityToolkit.WinUI.Lottie", "Microsoft.Toolkit.Uwp.UI.Lottie"),
			};
			ReplaceInFolders(Path.Combine(basePath, "src", "SamplesApp", "UITests.Shared"), lottieReplacements, "*.xaml");


			UncommentWinUISpecificBlock(Path.Combine(basePath, "build", "nuget", "Uno.WinUI.nuspec"));
			UncommentWinUISpecificBlock(Path.Combine(basePath, "build", "nuget", "Uno.WinUI.MSAL.nuspec"));

			// Replace microsoft namespaces in a reversible way
			// This particular section assumes that UWP controls are not prefixed with `using:Windows.UI.Xaml`
			var styleFolders = new[] {
				Path.Combine(basePath, "src", "Uno.UI", "Microsoft", "UI", "Xaml", "Controls"),
				Path.Combine(basePath, "src", "Uno.UI", "UI", "Xaml", "Style"),
				Path.Combine(basePath, "src", "Uno.UI.FluentTheme.v2"),
				Path.Combine(basePath, "src", "Uno.UI.FluentTheme.v1"),
				Path.Combine(basePath, "src", "Uno.UI.Tests"),
				Path.Combine(basePath, "src", "Uno.UI.RuntimeTests"),
				Path.Combine(basePath, "src", "SamplesApp"),
			};

			foreach (var styleFolder in styleFolders)
			{
				ReplaceInFolders(
					styleFolder,
					new[] {
					("using:Windows.UI.Xaml", "using:Windows.UI.Xaml") }
					, searchPattern: "*.xaml"
				);
				ReplaceInFolders(
					styleFolder,
					new[] {
					("using:Windows.UI.Xaml", "using:Windows.UI.Xaml") }
					, searchPattern: "*.xamltest"
				);
			}

			// Revert specifically for pathless casting test where
			// the namespace needs to be explicitly specified for a downcast
			// diverging from the common use of explicit "using:Windows.UI.Xaml"
			// which reference MUX controls in a WUX source tree.
			ReplaceInFolders(
				Path.Combine(basePath, "src", "Uno.UI.Tests"),
				new[] {
				("using:Windows.UI.Xaml", "using:Windows.UI.Xaml") }
				, searchPattern: "xBind_PathLessCasting.xaml"
			);
		}

		static string[] _exclusions = new string[] {
			"Uno.UWPSyncGenerator.Reference.csproj",
			"SamplesApp.Windows.csproj",
			@"Uno.UWPSyncGenerator",
			@"PackageDiffIgnore.xml",
			@"src\Uno.UWP\",
			@"src\Uno.UI\UI\Xaml\Controls\NavigationView\",
			@"src\Uno.UI.RuntimeTests\Tests\Windows_UI_Xaml_Controls\Given_NavigationView.cs",
			@"\obj\",
			@"\bin\",
			@"\.git",
			@"\.vs",
			@"\doc\",
			@"\src\SolutionTemplate\",
		}
		.Select(AlignPath)
		.ToArray();

		private static string AlignPath(string p) => p.Replace('\\', Path.DirectorySeparatorChar);

		private static void ReplaceInFolders(string basePath, (string from, string to)[] replacements, string searchPattern = "*.*")
		{
			Directory.EnumerateFiles(basePath, searchPattern, SearchOption.AllDirectories)
				.AsParallel()
				.ForAll(file =>
				{
					if (_exclusions.Any(e => file.Contains(e, StringComparison.OrdinalIgnoreCase)))
					{
						return;
					}

					var updated = false;
					var content = File.ReadAllText(file);

					for (int i = 0; i < replacements.Length; i++)
					{
						if (content.Contains(replacements[i].from))
						{
							content = content.Replace(replacements[i].from, replacements[i].to);
							updated = true;
						}
					}

					if (updated)
					{
						Console.WriteLine($"Updating [{file}]");

						int retry = 3;
						while (retry-- > 0)
						{
							try
							{
								File.WriteAllText(file, content, Encoding.UTF8);
							}
							catch
							{
								System.Threading.Thread.Sleep(500);
							}
						}
					}
				});
		}

		private static void DeleteFolder(string path)
		{
			if (Directory.Exists(path))
			{
				Console.WriteLine($"Deleting {path}");
				Directory.Delete(path, true);
			}
		}

		private static void ReplaceInFile(string filePath, string from, string to)
		{
			Console.WriteLine($"Updating [{filePath}]");

			var txt = File.ReadAllText(filePath);
			txt = txt.Replace(from, to);
			File.WriteAllText(filePath, txt, Encoding.UTF8);
		}

		private static void UncommentWinUISpecificBlock(string nuspecPath)
		{
			ReplaceInFile(nuspecPath, @"<!-- BEGIN UWP-specific", "<!-- BEGIN UWP-specific -->");
			ReplaceInFile(nuspecPath, @"END UWP-specific -->", "<!-- END UWP-specific -->");

			ReplaceInFile(nuspecPath, @"<!-- BEGIN WinUI-specific -->", "<!-- BEGIN WinUI-specific");
			ReplaceInFile(nuspecPath, @"<!-- END WinUI-specific -->", "END WinUI-specific -->");
		}
	}
}
