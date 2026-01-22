#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.Extensions;
using Uno.Roslyn;

namespace Uno.Samples.UITest.Generator
{
	[Generator]
	public class SnapShotTestGenerator : ISourceGenerator
	{
		private static readonly DiagnosticDescriptor _exceptionDiagnosticDescriptor = new(
			"SnapshotGenerator001",
			"Exception is thrown from SnapShotTestGenerator",
			"{0}",
			"Generation",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		private static readonly string[] _defaultCategories = new[] { "Default" };

		private const int GroupCount = 5;

		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
#if DEBUG
			// Debugger.Launch();
#endif
			try
			{
				if (context.Compilation.Assembly.Name == "SamplesApp.UITests")
				{
					GenerateTests(context, "Uno.UI.Samples");
				}
			}
			catch (ReflectionTypeLoadException typeLoadException)
			{
				var loaderExceptions = typeLoadException.LoaderExceptions;

				StringBuilder sb = new();
				foreach (var loaderException in loaderExceptions)
				{
					sb.Append(loaderException.ToString());
				}

				context.ReportDiagnostic(Diagnostic.Create(_exceptionDiagnosticDescriptor, location: null, sb.ToString()));
			}
			catch (Exception e)
			{
				context.ReportDiagnostic(Diagnostic.Create(_exceptionDiagnosticDescriptor, location: null, e.ToString()));
			}
		}

		private void GenerateTests(GeneratorExecutionContext context, string assembly)
		{
			var compilation = GetCompilation(context);

			var sampleSymbol = compilation.GetTypeByMetadataName("Uno.UI.Samples.Controls.SampleAttribute");
			if (sampleSymbol is null)
			{
				throw new Exception("Cannot find 'SampleAttribute'.");
			}

			var query = from typeSymbol in compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
						where typeSymbol.DeclaredAccessibility == Accessibility.Public
						let info = typeSymbol.FindAttributeFlattened(sampleSymbol)
						where info != null
						let sampleInfo = GetSampleInfo(typeSymbol, info)
						orderby sampleInfo.categories.First()
						select (typeSymbol, sampleInfo.categories, sampleInfo.name, sampleInfo.ignoreInSnapshotTests, sampleInfo.isManual);

			query = query.Distinct();

			GenerateTests(assembly, context, query);
		}

		private static (string[] categories, string name, bool ignoreInSnapshotTests, bool isManual) GetSampleInfo(INamedTypeSymbol symbol, AttributeData attr)
		{
			var categories = attr
				.ConstructorArguments
				.Where(arg => arg.Kind == TypedConstantKind.Array)
				.Select(arg => GetCategories(arg.Values))
				.SingleOrDefault()
				?? GetCategories(attr.ConstructorArguments);

			if (categories.Any(string.IsNullOrWhiteSpace))
			{
				throw new InvalidOperationException(
					"Invalid syntax for the SampleAttribute (found an empty category name). "
					+ "Usually this is because you used nameof(Control) to set the categories, which is not supported by the compiler. "
					+ "You should instead use the overload which accepts type (i.e. use typeof() instead of nameof()).");
			}

			return (
				categories: categories.Length > 0 ? categories : _defaultCategories,
				name: AlignName(GetAttributePropertyValue(attr, "Name")?.ToString() ?? symbol.ToDisplayString()),
				ignoreInSnapshotTests: GetAttributePropertyValue(attr, "IgnoreInSnapshotTests") is bool b && b,
				isManual: GetAttributePropertyValue(attr, "IsManualTest") is bool m && m
				)!;

			string?[] GetCategories(ImmutableArray<TypedConstant> args) => args
				.Select(v =>
				{
					switch (v.Kind)
					{
						case TypedConstantKind.Primitive: return v.Value!.ToString();
						case TypedConstantKind.Type: return ((ITypeSymbol)v.Value!).Name;
						default: return null;
					}
				})
				.ToArray();
		}

		private static object? GetAttributePropertyValue(AttributeData attr, string name)
			=> attr.NamedArguments.FirstOrDefault(kvp => kvp.Key == name).Value.Value;

		private static string AlignName(string v)
			=> v.Replace('/', '_').Replace(' ', '_').Replace('-', '_').Replace(':', '_');

		private static void GenerateTests(
			string assembly,
			GeneratorExecutionContext context,
			IEnumerable<(INamedTypeSymbol symbol, string[] categories, string name, bool ignoreInSnapshotTests, bool isManual)> symbols)
		{
			var groups =
				from symbol in symbols.Select((v, i) => (index: i, value: v))
				group symbol by symbol.index / 50 into g
				select new
				{
					Index = g.Key,
					Symbols = g.AsEnumerable().Select(v => v.value)
				};

			foreach (var group in groups)
			{
				string sanitizedAssemblyName = assembly.Replace('.', '_');
				var groupName = $"Generated_{sanitizedAssemblyName}_{group.Index:000}";

				var builder = new IndentedStringBuilder();
				builder.AppendLineIndented("// <auto-generated />");
				builder.AppendLineIndented("using System;");

				using (builder.BlockInvariant($"namespace {context.GetMSBuildPropertyValue("RootNamespace")}.Snap"))
				{
					builder.AppendLineIndented("[global::NUnit.Framework.TestFixture]");

					// Required for https://github.com/unoplatform/uno/issues/1955
					builder.AppendLineIndented("[global::SamplesApp.UITests.TestFramework.TestAppModeAttribute(cleanEnvironment: true, platform: Uno.UITest.Helpers.Queries.Platform.iOS)]");

					using (builder.BlockInvariant($"public partial class {groupName} : SampleControlUITestBase"))
					{
						foreach (var test in group.Symbols)
						{
							builder.AppendLineIndented("[global::NUnit.Framework.Test]");
							builder.AppendLineIndented($"[global::NUnit.Framework.Category(\"runGroup:{group.Index % GroupCount:00}, automated:{test.symbol.ToDisplayString()}\")]");

							var (ignored, ignoreReason) = (test.ignoreInSnapshotTests, test.isManual) switch
							{
								(true, true) => (true, "ignoreInSnapshotTests and isManual are set for attribute"),
								(true, false) => (true, "ignoreInSnapshotTests is are set for attribute"),
								(false, true) => (true, "isManualTest is set for attribute"),
								_ => (false, default),
							};
							if (ignored)
							{
								builder.AppendLineIndented($"[global::NUnit.Framework.Ignore(\"{ignoreReason}\")]");
							}

							builder.AppendLineIndented("[global::SamplesApp.UITests.TestFramework.AutoRetry]");
							// Set to 60 seconds to cover possible restart of the device
							builder.AppendLineIndented("[global::NUnit.Framework.Timeout(60000)]");
							var testName = $"{Sanitize(test.categories.First())}_{Sanitize(test.name)}";
							using (builder.BlockInvariant($"public void {testName}()"))
							{
								builder.AppendLineIndented($"Console.WriteLine(\"Running test [{testName}]\");");
								builder.AppendLineIndented($"Run(\"{test.symbol}\", waitForSampleControl: false);");
								builder.AppendLineIndented($"Console.WriteLine(\"Ran test [{testName}]\");");
							}
						}
					}
				}

				context.AddSource(groupName, builder.ToString());
			}
		}

		private static object Sanitize(string category)
			=> string.Join("", category.Select(c => char.IsLetterOrDigit(c) ? c : '_'));

		private static Compilation GetCompilation(GeneratorExecutionContext context)
		{
			return context.Compilation.AddSyntaxTrees(GetSyntaxTrees(context, "UITests.Shared").Concat(GetSyntaxTrees(context, "SamplesApp.UnitTests.Shared")));
		}

		private static IEnumerable<SyntaxTree> GetSyntaxTrees(GeneratorExecutionContext context, string baseName)
		{
			var sourcePath = Path.Combine(context.GetMSBuildPropertyValue("MSBuildProjectDirectory"), "..", baseName);
			foreach (var file in Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories))
			{
				yield return SyntaxFactory.ParseSyntaxTree(File.ReadAllText(file), context.ParseOptions);
			}
		}
	}
}
