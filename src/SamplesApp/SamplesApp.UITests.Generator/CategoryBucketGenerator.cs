using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;

namespace Uno.Samples.UITest.Generator
{
	public class CategoryBuckerGenerator : SourceGenerator
	{
		private INamedTypeSymbol _testAttribute;

		public override void Execute(SourceGeneratorContext context)
		{
#if DEBUG
			// Debugger.Launch();
#endif

			GenerateTests(context);
		}

		private void GenerateTests(SourceGeneratorContext context)
		{
			if(int.TryParse(Environment.GetEnvironmentVariable("UNO_UITEST_BUCKET_COUNT"), out var bucketCount))
			{
				_testAttribute = context.Compilation.GetTypeByMetadataName("NUnit.Framework.TestAttribute");

				var query = from typeSymbol in context.Compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
							where typeSymbol.DeclaredAccessibility == Accessibility.Public
							where typeSymbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.FindAttributeFlattened(_testAttribute) != null)
							select typeSymbol;

				GenerateCategories(context, query, bucketCount);
			}
		}

		private void GenerateCategories(
			SourceGeneratorContext context,
			IEnumerable<INamedTypeSymbol> symbols,
			int bucketCount)
		{
			var sha1 = SHA1.Create();

			foreach (var type in symbols)
			{
				var builder = new IndentedStringBuilder();

				builder.AppendLineInvariant("using System;");

				using (builder.BlockInvariant($"namespace {type.ContainingNamespace}"))
				{
					// Compute a stable hash of the full metadata name
					var buffer = Encoding.UTF8.GetBytes(type.GetFullMetadataName());
					var hash = sha1.ComputeHash(buffer);
					var hashPart64 = BitConverter.ToUInt64(hash, 0);

					var testCategoryBucket = (hashPart64 % (uint)bucketCount) + 1;

					builder.AppendLineInvariant($"[global::NUnit.Framework.Category(\"testBucket:{testCategoryBucket}\")]");
					using (builder.BlockInvariant($"partial class {type.Name}"))
					{

					}
				}

				context.AddCompilationUnit(Sanitize(type.GetFullMetadataName()), builder.ToString());
			}
		}

		private string Sanitize(string category)
			=> category
				.Replace(" ", "_")
				.Replace("-", "_")
				.Replace(".", "_");
	}
}
