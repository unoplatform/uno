#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace UnoWinUIRevert
{
	class Program
	{
		static void Main(string[] args)
		{
			var basePath = args[0];

			DeleteFolder(Path.Combine(basePath, @"src\Uno.UI\Generated"));
			DeleteFolder(Path.Combine(basePath, @"src\Uno.UWP\Generated"));
			DeleteFolder(Path.Combine(basePath, @"src\Uno.UI\UI\Composition"));
			DeleteFolder(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\ProgressBar")); // ProgressBar in WinUI is a replacement of the UWP's version.

			var compositionPath = Path.Combine(basePath, @"src\Uno.UWP\UI\Composition");
			if (Directory.Exists(compositionPath))
			{
				Console.WriteLine(@"Moving composition");
				Directory.Move(compositionPath, Path.Combine(basePath, @"src\Uno.UI\UI\Composition"));
			}

			var colorsFilepath = Path.Combine(basePath, @"src\Uno.UWP\UI\Colors.cs");
			if (File.Exists(colorsFilepath))
			{
				File.Copy(colorsFilepath, Path.Combine(basePath, @"src\Uno.UI\UI\Colors.cs"), true);
			}

			var colorHelperFilePath = Path.Combine(basePath, @"src\Uno.UWP\UI\ColorHelper.cs");
			if (File.Exists(colorHelperFilePath))
			{
				File.Copy(colorHelperFilePath, Path.Combine(basePath, @"src\Uno.UI\UI\ColorHelper.cs"), true);
			}

			// Files/Class that are implemented in both MUX and WUX and which should not be converted
			var duplicatedImplementations = new[]
			{
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icon\FontIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icon\IconSource.cs")
			};
			DeleteFiles(duplicatedImplementations);

			// Generic replacements
			var genericReplacements = new[] {
				("Windows.UI.Xaml", "Microsoft.UI.Xaml"),
				("Windows.UI.Composition", "Microsoft.UI.Composition"),
				("Windows.UI.Colors", "Microsoft.UI.Colors"),
				("Windows.UI.ColorHelper", "Microsoft.UI.ColorHelper"),
				("Windows.UI.Xaml", "Microsoft.UI.Xaml"),
				("Microsoft.UI.Xaml.Controls\", \"ProgressRing", "Uno.UI.Controls.Legacy\", \"ProgressRing"),
				("<UNO_UWP_BUILD>true</UNO_UWP_BUILD>", "<UNO_UWP_BUILD>false</UNO_UWP_BUILD>"),
			};

			ReplaceInFolders(basePath, genericReplacements);

			// Restore ProgressRing
			var progressRingReplacements = new[] {
				("Microsoft.UI.Xaml.Controls", "Uno.UI.Controls.Legacy"),
			};

			ReplaceInFolders(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\ProgressRing"), progressRingReplacements);
			ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\ProgressRing\ProgressRing.xaml"), "\"ProgressRing\"", "\"legacy:ProgressRing\"");
			ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Style\Generic\Generic.Native.xaml"), "ProgressRing", "legacy:ProgressRing");
			ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\Microsoft\UI\Xaml\Controls\ProgressRing\ProgressRing.xaml"), "using:Windows.UI.Xaml.Controls", "using:Uno.UI.Controls.Legacy");

			// Restore DualPaneView XAML
			// ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\Microsoft\UI\Xaml\Controls\TwoPaneView\TwoPaneView.xaml"), "using:Windows.UI.Xaml.Controls", "using:Windows.UI.Xaml.Controls");

			// Adjust Colors
			ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Colors.cs"), "Windows.UI", "Microsoft.UI");
			ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\ColorHelper.cs"), "Windows.UI", "Microsoft.UI");
			ReplaceInFile(Path.Combine(basePath, @"src\SourceGenerators\Uno.UI.SourceGenerators\XamlGenerator\XamlConstants.cs"), "Windows.UI", "Microsoft.UI");
			ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Markup\Reader\XamlConstants.cs"), "Windows.UI", "Microsoft.UI");

			// Custom animation
			// ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\Animation\Animators\RenderingLoopAnimator.wasm.cs"), "Microsoft", "Windows");

			// Revert partial changes for WinUI 2.4 imported controls
			//foreach (var file in Directory.EnumerateFiles(Path.Combine(basePath, @"src\Uno.UI\Microsoft\UI\Xaml\Controls"), "*.*", SearchOption.AllDirectories))
			//{
			//	ReplaceInFile(file, "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//}

			// Restore RadialGradientBrush
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.Android.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.iOSmacOS.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.wasm.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
		}

		static string[] _exclusions = new[] {
			"Uno.UWPSyncGenerator.Reference.csproj",
			"SamplesApp.UWP.csproj",
			"SamplesApp.UWP.Design.csproj",
			@"Uno.UWPSyncGenerator\Generator.cs",
			@"src\Uno.UWP\",
			@"src\Uno.UI\UI\Xaml\Controls\NavigationView\",
			@"src\Uno.UI.RuntimeTests\Tests\Windows_UI_Xaml_Controls\Given_NavigationView.cs",
			@"\obj\",
			@"\bin\",
			@"\.git",
			@"\.vs",
		};

		private static void ReplaceInFolders(string basePath, (string from, string to)[] replacements, string searchPattern = "*.*")
		{
			foreach (var file in Directory.EnumerateFiles(basePath, searchPattern, SearchOption.AllDirectories))
			{
				if (_exclusions.Any(e => file.Contains(e, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
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
					File.WriteAllText(file, content);
				}
			}
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
			File.WriteAllText(filePath, txt);
		}

		private static void DeleteFiles(string[] filePaths)
		{
			foreach (var filePath in filePaths)
			{
				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}
			}
		}
	}
}
