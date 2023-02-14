#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

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
			DeleteFolder(Path.Combine(basePath, "src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressBar")); // ProgressBar in WinUI is a replacement of the UWP's version

			var colorsFilepath = Path.Combine(basePath, @"src", "Uno.UWP", "UI", "Colors.cs");
			if (File.Exists(colorsFilepath))
			{
				File.Copy(colorsFilepath, Path.Combine(basePath, @"src", "Uno.UI", "UI", "Colors.cs"), true);
			}

			var colorHelperFilePath = Path.Combine(basePath, @"src", "Uno.UWP", "UI", "ColorHelper.cs");
			if (File.Exists(colorHelperFilePath))
			{
				File.Copy(colorHelperFilePath, Path.Combine(basePath, @"src", "Uno.UI", "UI", "ColorHelper.cs"), true);
			}

			var fontWeightsFilePath = Path.Combine(basePath, @"src", "Uno.UWP", "UI", "Text", "FontWeights.cs");
			if (File.Exists(fontWeightsFilePath))
			{
				Directory.CreateDirectory(Path.Combine(basePath, "src", "Uno.UI", "UI", "Text"));
				File.Copy(fontWeightsFilePath, Path.Combine(basePath, @"src", "Uno.UI", "UI", "Text", "FontWeights.cs"), true);
			}

			var inputPath = Path.Combine(basePath, @"src", "Uno.UWP", "UI", "Input");
			if (Directory.Exists(inputPath))
			{
				Console.WriteLine(@"Copying UI.Input");
				foreach (var file in Directory.GetFiles(inputPath))
				{
					var relativePath = Path.GetRelativePath(inputPath, file);

					var targetFile = Path.Combine(basePath, "src", "Uno.UI", "UI", "Input", relativePath);

					Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
					File.Copy(file, targetFile, true);
				}
			}

			var dispatcherQueuePath = Path.Combine(basePath, @"src", "Uno.UWP", "System");
			if (Directory.Exists(dispatcherQueuePath))
			{
				Console.WriteLine(@"Copying DispatcherQueue types");
				foreach (var file in Directory.GetFiles(dispatcherQueuePath, "DispatcherQueue*.cs"))
				{
					var relativePath = Path.GetRelativePath(dispatcherQueuePath, file);

					var targetFile = Path.Combine(basePath, "src", "Uno.UI.Dispatching", "Dispatching", relativePath);

					Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
					File.Copy(file, targetFile, true);
				}
			}

			// Files/Class that are implemented in both MUX and WUX and which should not be converted
			var duplicatedImplementations = new[]
			{
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icon\BitmapIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icon\SymbolIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icon\PathIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icon\FontIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icon\IconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\RatingControl.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Automation\Peers\RatingControlAutomationPeer.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\SplitButton.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\SplitButtonAutomationPeer.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\ToggleSplitButton.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\ToggleSplitButtonAutomationPeer.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\TreeView.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\TwoPaneView.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\ColorPicker.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\RefreshContainer.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\RefreshVisualizer.cs"),
			};
			DeleteFiles(duplicatedImplementations);

			// Generic replacements
			var genericReplacements = new[] {
				("Microsoft.UI.Xaml", "Microsoft.UI.Xaml"),
				("Microsoft.UI.Composition", "Microsoft.UI.Composition"),
				("Microsoft.UI.Colors", "Microsoft.UI.Colors"),
				("Microsoft.UI.Text.FontWeights", "Microsoft.UI.Text.FontWeights"),
				("Microsoft.UI.ColorHelper", "Microsoft.UI.ColorHelper"),
				("Microsoft.UI.Xaml", "Microsoft.UI.Xaml"),
				("__LinkerHints.Is_Microsoft_UI_Xaml", "__LinkerHints.Is_Microsoft_UI_Xaml"),
				("Microsoft.UI.Xaml.Controls\", \"ProgressRing", "Uno.UI.Controls.Legacy\", \"ProgressRing"),
				("<UNO_UWP_BUILD>false</UNO_UWP_BUILD>", "<UNO_UWP_BUILD>false</UNO_UWP_BUILD>"),
			};

			ReplaceInFolders(basePath, genericReplacements);

			// Restore ProgressRing
			var progressRingReplacements = new[] {
				("Microsoft.UI.Xaml.Controls", "Uno.UI.Controls.Legacy"),
			};

			ReplaceInFolders(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing"), progressRingReplacements);
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "\"ProgressRing\"", "\"legacy:ProgressRing\"");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Style", "Generic", "Generic.Native.xaml"), "ProgressRing", "legacy:ProgressRing");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "Microsoft", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "using:Microsoft.UI.Xaml.Controls", "using:Uno.UI.Controls.Legacy");

			// Restore DualPaneView XAML
			// ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\Microsoft\UI\Xaml\Controls\TwoPaneView\TwoPaneView.xaml"), "using:Microsoft.UI.Xaml.Controls", "using:Microsoft.UI.Xaml.Controls");

			// Adjust Colors
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Colors.cs"), "Windows.UI", "Microsoft.UI");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "ColorHelper.cs"), "Windows.UI", "Microsoft.UI");
			ReplaceInFile(Path.Combine(basePath, @"src", "SourceGenerators", "Uno.UI.SourceGenerators", "XamlGenerator", "XamlConstants.cs"), "Windows.UI", "Microsoft.UI");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Markup", "Reader", "XamlConstants.cs"), "Windows.UI", "Microsoft.UI");

			// Custom animation
			// ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\Animation\Animators\RenderingLoopAnimator.wasm.cs"), "Microsoft", "Windows");

			// Revert partial changes for WinUI 2.4 imported controls
			//foreach (var file in Directory.EnumerateFiles(Path.Combine(basePath, @"src\Uno.UI\Microsoft\UI\Xaml\Controls"), "*.*", SearchOption.AllDirectories))
			//{
			//	ReplaceInFile(file, "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//}

			// Restore RadialGradientBrush
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.Android.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.iOSmacOS.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.wasm.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
		}

		static string[] _exclusions = new string[] {
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
		}
		.Select(AlignPath)
		.ToArray();

		private static string AlignPath(string p) => p.Replace('\\', Path.DirectorySeparatorChar);

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
					File.WriteAllText(file, content, Encoding.UTF8);
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
			File.WriteAllText(filePath, txt, Encoding.UTF8);
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
