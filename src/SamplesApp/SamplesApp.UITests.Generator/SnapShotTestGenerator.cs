using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		private const int GroupCount = 4;

		private INamedTypeSymbol _sampleControlInfoSymbol;
		private INamedTypeSymbol _sampleSymbol;

		public override void Execute(SourceGeneratorContext context)
		{
#if DEBUG
			// Debugger.Launch();
#endif

			GenerateTests(context, "Uno.UI.Samples");
		}

		private void GenerateTests(SourceGeneratorContext context, string assembly)
		{
			var compilation = GetCompilation(context, assembly);

			_sampleControlInfoSymbol = compilation.compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleControlInfoAttribute");
			_sampleSymbol = compilation.compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleAttribute");

			var query = from typeSymbol in compilation.compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
						where typeSymbol.DeclaredAccessibility == Accessibility.Public
						let info = typeSymbol.FindAttributeFlattened(_sampleSymbol) ?? typeSymbol.FindAttributeFlattened(_sampleControlInfoSymbol)
						where info != null
						let sampleInfo = GetSampleInfo(typeSymbol, info)
						orderby sampleInfo.categories.First()
						select (typeSymbol, sampleInfo.categories, sampleInfo.name, sampleInfo.ignoreInSnapshotTests);

			query = query.Distinct();

			GenerateTests(assembly, context, query);
		}

		private (string[] categories, string name, bool ignoreInSnapshotTests) GetSampleInfo(INamedTypeSymbol symbol, AttributeData attr)
		{
			if (attr.AttributeClass == _sampleControlInfoSymbol)
			{
				return (
					categories: new[] { GetConstructorParameterValue(attr, "category")?.ToString() ?? "Default" },
					name: AlignName(GetConstructorParameterValue(attr, "controlName")?.ToString() ?? symbol.ToDisplayString()),
					ignoreInSnapshotTests: GetConstructorParameterValue(attr, "ignoreInSnapshotTests") is bool b && b
				);
			}
			else
			{
				var categories = attr
					.ConstructorArguments
					.Where(arg => arg.Kind == TypedConstantKind.Array)
					.Select(arg => GetCategories(arg.Values))
					.SingleOrDefault()
					?? GetCategories(attr.ConstructorArguments);

				if (categories?.Any(string.IsNullOrWhiteSpace) ?? false)
				{
					throw new InvalidOperationException(
						"Invalid syntax for the SampleAttribute (found an empty category name). "
						+ "Usually this is because you used nameof(Control) to set the categories, which is not supported by the compiler. "
						+ "You should instead use the overload which accepts type (i.e. use typeof() instead of nameof()).");
				}

				return (
					categories: (categories?.Any() ?? false) ? categories : new[] { "Default" },
					name: AlignName(GetAttributePropertyValue(attr, "Name")?.ToString() ?? symbol.ToDisplayString()),
					ignoreInSnapshotTests: GetAttributePropertyValue(attr, "IgnoreInSnapshotTests") is bool b && b
				);

				string[] GetCategories(ImmutableArray<TypedConstant> args) => args
					.Select(v =>
					{
						switch (v.Kind)
						{
							case TypedConstantKind.Primitive: return v.Value.ToString();
							case TypedConstantKind.Type: return ((ITypeSymbol)v.Value).Name;
							default: return null;
						}
					})
					.ToArray();
			}
		}

		private object GetAttributePropertyValue(AttributeData attr, string name)
			=> attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == name).Value.Value;

		private object GetConstructorParameterValue(AttributeData info, string name)
			=> info.ConstructorArguments.IsDefaultOrEmpty
				? default
				: info.ConstructorArguments.ElementAt(GetParameterIndex(info, name)).Value;

		private int GetParameterIndex(AttributeData info, string name)
			=> info
				.AttributeConstructor
				.Parameters
				.Select((p, i) => (p, i))
				.Single(p => p.p.Name == name)
				.i;
	
		private string AlignName(string v)
			=> v.Replace("/", "_").Replace(" ", "_").Replace("-", "_");

		private void GenerateTests(
			string assembly,
			SourceGeneratorContext context,
			IEnumerable<(INamedTypeSymbol symbol, string[] categories, string name, bool ignoreInSnapshotTests)> symbols)
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
					builder.AppendLineInvariant("[global::NUnit.Framework.TestFixture]");

					// Required for https://github.com/unoplatform/uno/issues/1955
					builder.AppendLineInvariant("[global::SamplesApp.UITests.TestFramework.TestAppModeAttribute(cleanEnvironment: true, platform: Uno.UITest.Helpers.Queries.Platform.iOS)]");

					using (builder.BlockInvariant($"public partial class {groupName} : SampleControlUITestBase"))
					{
						foreach (var test in group.Symbols)
						{
							builder.AppendLineInvariant("[global::NUnit.Framework.Test]");
							builder.AppendLineInvariant($"[global::NUnit.Framework.Description(\"runGroup:{group.Index % GroupCount:00}, automated:{test.symbol.ToDisplayString()}\")]");

							if (test.ignoreInSnapshotTests)
							{
								builder.AppendLineInvariant("[global::NUnit.Framework.Ignore(\"ignoreInSnapshotTests is set for attribute\")]");
							}

							builder.AppendLineInvariant("[global::SamplesApp.UITests.TestFramework.AutoRetry]");
							// Set to 60 seconds to cover possible restart of the device
							builder.AppendLineInvariant("[global::NUnit.Framework.Timeout(60000)]");
							var testName = $"{Sanitize(test.categories.First())}_{Sanitize(test.name)}";
							using (builder.BlockInvariant($"public void {testName}()"))
							{
								builder.AppendLineInvariant($"Console.WriteLine(\"Running test [{testName}]\");");
								builder.AppendLineInvariant($"Run(\"{test.symbol}\", waitForSampleControl: false);");
								builder.AppendLineInvariant($"Console.WriteLine(\"Ran test [{testName}]\");");
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
				case "monoandroid90":
					yield return $@"{devEnvDir}\..\Common7\IDE\ReferenceAssemblies\Microsoft\Framework\MonoAndroid\v9.0";
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
