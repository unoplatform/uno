using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions;

namespace Uno.UWPSyncGenerator
{
	class SyncGenerator : Generator
	{
		protected override void ProcessType(INamedTypeSymbol type, INamespaceSymbol ns)
		{
			var folder = $@"{GetNamespaceBasePath(type)}\{ns}";
			_ = Directory.CreateDirectory(folder);

			// Console.WriteLine(type.ToString());

			// if (type.Name == "BrushCollection" || type.Name == "StringMap")
			// if (type.Name == "StatusBar")
			{
				using (var typeWriter = new StreamWriter(Path.Combine(folder, type.Name) + ".cs"))
				{
					var b = new IndentedStringBuilder();
					b.AppendLineInvariant($"#pragma warning disable 108 // new keyword hiding");
					b.AppendLineInvariant($"#pragma warning disable 114 // new keyword hiding");
					using (b.BlockInvariant($"namespace {ns}"))
					{
						WriteType(type, b);
					}

					typeWriter.Write(b.ToString());
				}
			}
		}
		private void WriteType(INamedTypeSymbol type, IndentedStringBuilder b)
		{
			var kind = type.TypeKind;
			var partialModifier = type.TypeKind != TypeKind.Enum ? "partial" : "";
			var allSymbols = GetAllSymbols(type);

			var staticQualifier = type.IsStatic ? "static" : "";

			if (SkippedType(type))
			{
				b.AppendLineInvariant($"// Skipped type, see SkippedType method");
				return;
			}

			var writtenMethods = new List<IMethodSymbol>();

			if (type.TypeKind == TypeKind.Delegate)
			{
				BuildDelegate(type, b, allSymbols);
			}
			else
			{
				if (type.Name == "DependencyObject")
				{
					kind = TypeKind.Interface;
				}

				if (type.TypeKind == TypeKind.Enum)
				{
					allSymbols.AppendIf(b);

					if (type.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, FlagsAttributeSymbol)))
					{
						b.AppendLineInvariant($"[global::System.FlagsAttribute]");
					}
				}
				else
				{
					allSymbols.AppendIf(b);
					b.AppendLineInvariant($"[global::Uno.NotImplemented]");
					b.AppendLineInvariant($"#endif");
				}

				var enumBaseType =
					type.TypeKind == TypeKind.Enum &&
					type.EnumUnderlyingType.SpecialType != SpecialType.System_Int32 ?
						$": {type.EnumUnderlyingType.ToDisplayString()}" :
							string.Empty;

				using (b.BlockInvariant($"public {staticQualifier} {partialModifier} {kind.ToString().ToLower()} {type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {enumBaseType}{BuildInterfaces(type)}"))
				{
					if (type.TypeKind != TypeKind.Enum)
					{
						// TODO: https://github.com/unoplatform/uno/issues/6895
						//if (type.TypeKind == TypeKind.Class && !type.IsStatic &&
						//	type.GetMembers(WellKnownMemberNames.InstanceConstructorName).Length == 0)
						//{
						//	b.AppendLineInvariant($"private {type.Name}() {{{{ }}}}");
						//}
						BuildProperties(type, b, allSymbols);
						BuildMethods(type, b, allSymbols, writtenMethods);
						BuildEvents(type, b, allSymbols);
					}

					BuildFields(type, b, allSymbols);

					if (type.TypeKind != TypeKind.Enum)
					{
						BuildInterfaceImplementations(type, b, allSymbols, writtenMethods);
					}
				}

				if (type.TypeKind == TypeKind.Enum)
				{
					b.AppendLineInvariant($"#endif");
				}
			}
		}
	}
}
