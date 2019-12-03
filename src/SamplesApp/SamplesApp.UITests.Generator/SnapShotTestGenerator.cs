using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;

namespace Uno.Samples.UITest.Generator
{
	public class SnapShotTestGenerator : SourceGenerator
	{
		private INamedTypeSymbol _sampleControlInfoSymbol;

		public override void Execute(SourceGeneratorContext context)
		{
			Debugger.Launch();

			GenerateTests(context, "Uno.UI.Samples");
		}

		private void GenerateTests(SourceGeneratorContext context, string assembly)
		{
			var compilation = GetCompilation(context, assembly);

			_sampleControlInfoSymbol = compilation.compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleControlInfoAttribute");

			var query = from typeSymbol in compilation.compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
						where typeSymbol.DeclaredAccessibility == Accessibility.Public
						let info = typeSymbol.FindAttributeFlattened(_sampleControlInfoSymbol)
						where info != null && info.ConstructorArguments != null
						let sampleInfo = GetSampleInfo(typeSymbol, info)
						orderby sampleInfo.category
						select (typeSymbol, sampleInfo.category, sampleInfo.name);

			query = query.Distinct();

			GenerateTests(assembly, context, query);
		}

		private (string category, string name) GetSampleInfo(INamedTypeSymbol symbol, AttributeData info) 
			=> (
				category: info.ConstructorArguments.ElementAt(0).Value?.ToString() ?? "Default",
				name: AlignName(info.ConstructorArguments.ElementAtOrDefault(1).Value?.ToString() ?? symbol.ToDisplayString())
			);
		private string AlignName(string v) => v.Replace("/", "_").Replace(" ", "_").Replace("-", "_");

		private void GenerateTests(string assembly, SourceGeneratorContext context, IEnumerable<(INamedTypeSymbol symbol, string category, string name)> symbols)
		{
			var groups = 
				from symbol in symbols.Select((v, i) => (index:i, value:v))
				group symbol by symbol.index / 50 into g
				select new {
					Index = g.Key,
					Symbols = g.AsEnumerable().Select(v => v.value)
				};

			foreach(var group in groups)
			{
				string sanitizedAssemblyName = assembly.Replace(".", "_");
				var groupName = $"Generated_{sanitizedAssemblyName}_{group.Index:000}";

				var builder = new IndentedStringBuilder();

				builder.AppendLineInvariant("using System;");

				using (builder.BlockInvariant($"namespace {context.GetProjectInstance().GetPropertyValue("RootNamespace")}.Snap"))
				{
					builder.AppendLineInvariant("[NUnit.Framework.TestFixture]");
					using (builder.BlockInvariant($"public partial class {groupName} : SampleControlUITestBase"))
					{
						foreach (var test in group.Symbols) // .Where(s => s.symbol.ToString()).Contains("Border_Simple")))
						{
							var info = GetSampleInfo(test.symbol, test.symbol.FindAttributeFlattened(_sampleControlInfoSymbol));

							builder.AppendLineInvariant("[NUnit.Framework.Test]");
							using (builder.BlockInvariant($"public void {Sanitize(test.category)}_{Sanitize(info.name)}()"))
							{
								builder.AppendLineInvariant($"Run(\"{test.symbol}\");");
							}
						}
					}
				}

				context.AddCompilationUnit(groupName, builder.ToString());
			}
		}

		private object Sanitize(string category) 
			=> category
				.Replace(" ", "_")
				.Replace("-", "_")
				.Replace(".", "_");

		private (Compilation compilation, Project project) GetCompilation(SourceGeneratorContext context, string assembly)
		{
			// Used to get the reference assemblies
			var devEnvDir = context.GetProjectInstance().GetPropertyValue("MSBuildExtensionsPath");

			if (devEnvDir.StartsWith("*"))
			{
				throw new Exception($"The reference assemblies path is not defined");
			}

			var ws = new AdhocWorkspace();

			var project = ws.CurrentSolution.AddProject("temp", "temp", LanguageNames.CSharp);

			var referenceFiles = new[] {
				typeof(object).Assembly.CodeBase,
				typeof(Attribute).Assembly.CodeBase,
			};

			foreach (var file in referenceFiles.Distinct())
			{
				project = project.AddMetadataReference(MetadataReference.CreateFromFile(new Uri(file).LocalPath));
			}

			project = AddFiles(context, project, "UITests.Shared");
			project = AddFiles(context, project, "SamplesApp.UnitTests.Shared");

			var compilation = project.GetCompilationAsync().Result;

			return (compilation, project);
		}

		private static Project AddFiles(SourceGeneratorContext context, Project project, string baseName)
		{
			var sourcePath = Path.Combine(Path.GetDirectoryName(context.Project.FilePath), "..", baseName);
			foreach (var file in Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories))
			{
				project = project.AddDocument(Path.GetFileName(file), File.ReadAllText(file)).Project;
			}

			return project;
		}

		private IEnumerable<string> GetFrameworkPath(string devEnvDir, string path)
		{
			switch (Path.GetFileName(path).ToLowerInvariant())
			{
				case "monoandroid80":
					yield return $@"{devEnvDir}\..\Common7\IDE\ReferenceAssemblies\Microsoft\Framework\MonoAndroid\v8.0";
					yield return $@"{devEnvDir}\..\Common7\IDE\ReferenceAssemblies\Microsoft\Framework\MonoAndroid\v1.0";
					yield break;
				case "xamarinios10":
					yield return $@"{devEnvDir}\..\Common7\IDE\ReferenceAssemblies\Microsoft\Framework\Xamarin.iOS\v1.0";
					yield break;
			}

			throw new InvalidOperationException("Unknown framework");
		}
	}
}
