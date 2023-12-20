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

			// Replace microsoft namespaces in a reversible way
			ReplaceInFolders(basePath,
				new[] {
				("Microsoft/* UWP don't rename */.UI.Xaml", "Microsoft/* UWP don't rename */.UI.Xaml") }
				, searchPattern: "*.cs"
			);

			// Generic replacements
			var genericReplacements = new[] {
				("Microsoft.UI.Xaml", "Microsoft/* UWP don't rename */.UI.Xaml"),
				("Microsoft.UI.Composition", "Microsoft.UI.Composition"),
				("Microsoft.UI.Colors", "Microsoft.UI.Colors"),
				("Microsoft.UI.Text.FontWeights", "Microsoft.UI.Text.FontWeights"),
				("Microsoft.UI.ColorHelper", "Microsoft.UI.ColorHelper"),
				("Microsoft.UI.Xaml", "Microsoft/* UWP don't rename */.UI.Xaml"),
				("__LinkerHints.Is_Microsoft_UI_Xaml", "__LinkerHints.Is_Microsoft_UI_Xaml"),
				("Microsoft/* UWP don't rename */.UI.Xaml.Controls\", \"ProgressRing", "Uno.UI.Controls.Legacy\", \"ProgressRing"),
				("<UNO_UWP_BUILD>false</UNO_UWP_BUILD>", "<UNO_UWP_BUILD>false</UNO_UWP_BUILD>"),
			};

			ReplaceInFolders(basePath, genericReplacements);

			// Restore ProgressRing
			var progressRingReplacements = new[] {
				("Microsoft/* UWP don't rename */.UI.Xaml.Controls", "Uno.UI.Controls.Legacy"),
			};

			ReplaceInFolders(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing"), progressRingReplacements);
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "\"ProgressRing\"", "\"legacy:ProgressRing\"");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "UI", "Xaml", "Style", "Generic", "Generic.Native.xaml"), "ProgressRing", "legacy:ProgressRing");
			ReplaceInFile(Path.Combine(basePath, @"src", "Uno.UI", "Microsoft", "UI", "Xaml", "Controls", "ProgressRing", "ProgressRing.xaml"), "using:Microsoft.UI.Xaml.Controls", "using:Uno.UI.Controls.Legacy");

			// Restore DualPaneView XAML
			// ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\Microsoft\UI\Xaml\Controls\TwoPaneView\TwoPaneView.xaml"), "using:Microsoft.UI.Xaml.Controls", "using:Microsoft.UI.Xaml.Controls");

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
			//	ReplaceInFile(file, "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//}

			// Restore RadialGradientBrush
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.Android.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.iOSmacOS.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");
			//ReplaceInFile(Path.Combine(basePath, @"src\Uno.UI\UI\Xaml\Media\RadialGradientBrush.wasm.cs"), "namespace Microsoft.UI.Xaml.Controls", "namespace Microsoft.UI.Xaml.Controls");

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
			@"Uno.UWPSyncGenerator",
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
