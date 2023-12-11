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
			DeleteFolder(Path.Combine(basePath, "src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressBar")); // ProgressBar in WinUI is a replacement of the UWP's version
			DeleteFolder(Path.Combine(basePath, "src", "Uno.UI", "UI", "Xaml", "Controls", "NavigationView")); // NavigationView in WinUI is a replacement of the UWP's version

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
			Directory.Delete(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported"), recursive: true);
			var duplicatedImplementations = new[]
			{
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icons\BitmapIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icons\SymbolIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icons\PathIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icons\FontIconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Icons\IconSource.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RevealBrush.Android.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Controls\Unsupported\RatingControl.cs"),
				Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Automation\Peers\RatingControlAutomationPeer.cs"),
			};
			DeleteFiles(duplicatedImplementations);

			// Generic replacements
			var genericReplacements = new[] {
				("Windows.UI.Xaml", "Microsoft.UI.Xaml"),
				("Windows.UI.Composition", "Microsoft.UI.Composition"),
				("Windows.UI.Colors", "Microsoft.UI.Colors"),
				("Windows.UI.Text.FontWeights", "Microsoft.UI.Text.FontWeights"),
				("Windows.UI.ColorHelper", "Microsoft.UI.ColorHelper"),
				("Windows.UI.Xaml", "Microsoft.UI.Xaml"),
				("__LinkerHints.Is_Windows_UI_Xaml", "__LinkerHints.Is_Microsoft_UI_Xaml"),
				("Microsoft.UI.Xaml.Controls\", \"ProgressRing", "Uno.UI.Controls.Legacy\", \"ProgressRing"),
				("<UNO_UWP_BUILD>true</UNO_UWP_BUILD>", "<UNO_UWP_BUILD>false</UNO_UWP_BUILD>"),
			};

			ReplaceInFolders(basePath, genericReplacements);

			// Restore ProgressRing
			var progressRingReplacements = new[] {
				("Microsoft.UI.Xaml.Controls", "Uno.UI.Controls.Legacy"),
			};

			ReplaceInFolders(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing"), progressRingReplacements);
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "\"ProgressRing\"", "\"legacy:ProgressRing\"");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Style", "Generic", "Generic.Native.xaml"), "ProgressRing", "legacy:ProgressRing");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "Microsoft", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "using:Windows.UI.Xaml.Controls", "using:Uno.UI.Controls.Legacy");

			// Restore DualPaneView XAML
			// ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\Microsoft\UI\Xaml\Controls\TwoPaneView\TwoPaneView.xaml"), "using:Windows.UI.Xaml.Controls", "using:Windows.UI.Xaml.Controls");

			// Adjust lottie namespace
			var lottieReplacements = new[]
			{
				("Microsoft.Toolkit.Uwp.UI.Lottie", "CommunityToolkit.WinUI.Lottie"),
			};
			ReplaceInFolders(Path.Combine(basePath, "src", "SamplesApp", "UITests.Shared"), lottieReplacements, "*.xaml");

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
			//	ReplaceInFile(file, "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//}

			// Restore RadialGradientBrush
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.Android.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.iOSmacOS.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.wasm.cs"), "namespace Windows.UI.Xaml.Controls", "namespace Windows.UI.Xaml.Controls");

			// Replacements for nuspec files
			string[] nuspecTransformedFiles = new[]{
				Path.Combine(basePath, "build", "nuget", "Uno.WinUI.nuspec"),
				Path.Combine(basePath, "build", "nuget", "Uno.WinUI.MSAL.nuspec"),
			};

			foreach (var nuspecTransformedFile in nuspecTransformedFiles)
			{
				UncommentWinUISpecificBlock(nuspecTransformedFile);
				CommentUWPSpecificBlock(nuspecTransformedFile);
			}
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
			@"\docs\",
		}
		.Select(AlignPath)
		.ToArray();

		private static string AlignPath(string p) => p.Replace('\\', Path.DirectorySeparatorChar);

		private static void ReplaceInFolders(string basePath, (string from, string to)[] replacements, string searchPattern = "*.*")
		{
			foreach (var file in Directory.EnumerateFiles(basePath, searchPattern, SearchOption.AllDirectories))
			{
				if (_exclusions.Any(e => file.Contains(e, StringComparison.Ordinal)))
				{
					continue;
				}

				var originalContent = File.ReadAllText(file);
				var updatedContent = originalContent;

				for (int i = 0; i < replacements.Length; i++)
				{
					updatedContent = updatedContent.Replace(replacements[i].from, replacements[i].to);
				}

				if (!object.ReferenceEquals(originalContent, updatedContent))
				{
					Console.WriteLine($"Updating [{file}]");
					File.WriteAllText(file, updatedContent, Encoding.UTF8);
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

		private static void UncommentWinUISpecificBlock(string nuspecPath)
		{
			ReplaceInFile(nuspecPath, @"<!-- BEGIN WinUI-specific", string.Empty);
			ReplaceInFile(nuspecPath, @"END WinUI-specific -->", string.Empty);
		}

		private static void CommentUWPSpecificBlock(string nuspecPath)
		{
			ReplaceInFile(nuspecPath, @"<!-- BEGIN UWP-specific -->", "<!--");
			ReplaceInFile(nuspecPath, @"<!-- END UWP-specific -->", "-->");
		}
	}
}
