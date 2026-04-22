#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;
using Uno.DevTools.Telemetry;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	[Generator]
	public partial class XamlCodeGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			//var process = Process.GetCurrentProcess().ProcessName;
			//if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			//	|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
			//{
			//	Debugger.Launch();
			//}

			LogXamlInputsForDiag(context);

			if (PlatformHelper.IsValidPlatform(context))
			{
				var gen = new XamlCodeGeneration(context);
				var generatedTrees = gen.Generate();

				foreach (var tree in generatedTrees)
				{
					context.AddSource(tree.Key, tree.Value);
				}

				DumpXamlSourceGeneratorState(context, generatedTrees);
			}
		}

		// [HR-Diag-Gen] Temporary instrumentation for unoplatform/uno-private#1956 investigation — do NOT ship.
		// Logs, for every XAML AdditionalFile visible to this Execute() call, the content length and a short
		// SHA-256 hash. Compared across successive HR passes, this reveals whether the generator actually
		// receives fresh content after a runtime edit, or whether it sees a stale/cached AdditionalText.
		private static void LogXamlInputsForDiag(GeneratorExecutionContext context)
		{
			try
			{
				var assemblyName = context.Compilation.AssemblyName ?? "<unknown>";
				var xamlFiles = context.AdditionalFiles
					.Where(f => f.Path?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) == true)
					.ToList();

				if (xamlFiles.Count == 0)
				{
					Console.WriteLine($"[HR-Diag-Gen] Execute in {assemblyName}: no XAML AdditionalFiles (total additional: {context.AdditionalFiles.Length})");
					return;
				}

				Console.WriteLine($"[HR-Diag-Gen] Execute in {assemblyName}: saw {xamlFiles.Count} XAML AdditionalFile(s)");
				using var sha = SHA256.Create();
				foreach (var file in xamlFiles)
				{
					var text = file.GetText(context.CancellationToken);
					if (text is null)
					{
						Console.WriteLine($"[HR-Diag-Gen]   {file.Path} text=null");
						continue;
					}
					var content = text.ToString();
					var bytes = Encoding.UTF8.GetBytes(content);
					var hashBytes = sha.ComputeHash(bytes);
					// Short prefix is enough for correlation.
					var hash = BitConverter.ToString(hashBytes, 0, 8).Replace("-", "").ToLowerInvariant();
					Console.WriteLine($"[HR-Diag-Gen]   {file.Path} len={content.Length} hash={hash}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[HR-Diag-Gen] Execute instrumentation failed: {ex.GetType().Name}: {ex.Message}");
			}
		}
	}
}
