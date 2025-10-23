#nullable enable

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.Extensions;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;

namespace Uno.UI.SourceGenerators;

[Generator]
public partial class AndroidResourcesGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (PlatformHelper.IsAndroid(context))
		{
			BuildDrawableResourcesIdResolver(context);
		}
	}

	private static void BuildDrawableResourcesIdResolver(GeneratorExecutionContext context)
	{
		var writer = new IndentedStringBuilder();
		var defaultNamespace = context.GetMSBuildPropertyValue("RootNamespace");

		using (writer.BlockInvariant($"namespace {defaultNamespace}"))
		{
			writer.AppendLineIndented("/// <summary>");
			writer.AppendLineIndented("/// Resolves the Id of a bundled image.");
			writer.AppendLineIndented("/// </summary>");
			using (writer.BlockInvariant("internal static class DrawableResourcesIdResolver"))
			{
				using (writer.BlockInvariant("internal static int Resolve(string imageName)"))
				{
					using (writer.BlockInvariant("switch (imageName)"))
					{
						var drawables = context.Compilation.GetTypeByMetadataName($"{defaultNamespace}.Resource")?
							.GetTypeMembers("Drawable")
							.SingleOrDefault();

						// Support for net9.0+ resource constants
						drawables ??= context.Compilation?
							.GetTypeByMetadataName("_Microsoft.Android.Resource.Designer.ResourceConstant")
							?.GetTypeMembers("Drawable")
							.SingleOrDefault();

						if (drawables?.GetFields() is { } drawableFields)
						{
							foreach (var drawable in drawableFields)
							{
								writer.AppendLineInvariantIndented("case \"{0}\":", drawable.Name);
								using (writer.Indent())
								{
									writer.AppendLineInvariantIndented("return {0};", drawable.ConstantValue);
								}
							}
						}

						writer.AppendLineIndented("default:");
						using (writer.Indent())
						{
							writer.AppendLineIndented("return 0;");
						}
					}
				}

				// TODO: Should we call from application instead of being module initializer?
				// Would the call be part of the templates? Would it be a friction for updating old solutions?
				// Or is it okay to keep as ModuleInitializer?
				writer.AppendLineIndented("[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]");
				using (writer.BlockInvariant("internal static void AndroidResourcesInitializer()"))
				{
					writer.AppendLineIndented($"global::Uno.Helpers.DrawableHelper.SetDrawableResolver(global::{defaultNamespace}.DrawableResourcesIdResolver.Resolve);");
				}
			}
		}

		context.AddSource("DrawableResourcesIdResolver.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
	}
}
