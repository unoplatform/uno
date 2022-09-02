using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.Foundation.Interop;
using Uno.UI.SourceGenerators.Helpers;
using Uno.Roslyn;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.TSBindings
{
	[Generator]
	class TSBindingsGenerator : ISourceGenerator
	{
		private string _bindingsPaths;
		private string[] _sourceAssemblies;

		private static INamedTypeSymbol _intPtrSymbol;
		private static INamedTypeSymbol _structLayoutSymbol;
		private static INamedTypeSymbol _interopMessageSymbol;

		public void Initialize(GeneratorInitializationContext context)
		{
			DependenciesInitializer.Init();
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!DesignTimeHelper.IsDesignTime(context)
				&& PlatformHelper.IsValidPlatform(context)

				// This generator is only valid inside of Uno.UI
				&& context.Compilation.AssemblyName.Equals("Uno.UI", StringComparison.OrdinalIgnoreCase))
			{
				_bindingsPaths = context.GetMSBuildPropertyValue("TSBindingsPath")?.ToString();
				_sourceAssemblies = context.GetMSBuildItems("TSBindingAssemblySource").Select(i => i.Identity).ToArray();

				if (!string.IsNullOrEmpty(_bindingsPaths))
				{
					Directory.CreateDirectory(_bindingsPaths);

					_intPtrSymbol = context.Compilation.GetTypeByMetadataName("System.IntPtr");
					_structLayoutSymbol = context.Compilation.GetTypeByMetadataName(typeof(StructLayoutAttribute).FullName);
					_interopMessageSymbol = context.Compilation.GetTypeByMetadataName("Uno.Foundation.Interop.TSInteropMessageAttribute");

					var modules = from ext in context.Compilation.ExternalReferences
								  let sym = context.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
								  where _sourceAssemblies.Contains(sym.Name)
								  from module in sym.Modules
								  select module;

					modules = modules.Concat(context.Compilation.SourceModule);

					GenerateTSMarshallingLayouts(modules);
				}
			}
		}

		internal void GenerateTSMarshallingLayouts(IEnumerable<IModuleSymbol> modules)
		{
			var messages = from module in modules
				from type in GetNamespaceTypes(module)
				let attr = type.FindAttributeFlattened(_interopMessageSymbol)
				where attr is not null && type.TypeKind is TypeKind.Struct
				select (type, attr);

			messages = messages.ToArray();

			foreach (var message in messages)
			{
				var packValue = GetStructPack(message.type);

				var sb = new IndentedStringBuilder();

				sb.AppendLineInvariant($"/* {nameof(TSBindingsGenerator)} Generated code -- this code is regenerated on each build */");

				var ns = message.type.ContainingNamespace.ToDisplayString();
				if (message.type.ContainingType?.Name?.Contains("WindowManagerInterop", StringComparison.OrdinalIgnoreCase) ?? false)
				{
					// For backward compatibility, we include the namespace only for types that are not part of the WindowsManagerInterop.
					// We should include the namespace for all messages, but it would require to update all usages.
					ns = null;
				}

				using (ns is null ? null : sb.BlockInvariant($"namespace {ns}"))
				{
					using (sb.BlockInvariant($"{(ns is null ? "": "export ")}class {message.type.Name}"))
					{
						sb.AppendLineInvariant($"/* Pack={packValue} */");

						foreach (var field in message.type.GetFields())
						{
							sb.AppendLineInvariant($"public {field.Name} : {GetTSFieldType(field.Type)};");
						}

						var needsUnMarshaller = message.attr.GetNamedValue<CodeGeneration>(nameof(TSInteropMessageAttribute.UnMarshaller)) switch
						{
							CodeGeneration.Enabled => true,
							CodeGeneration.Disabled => false,
							_ => message.type.Name.EndsWith("Params") || message.type.Name.EndsWith("EventArgs"),
						};
						if (needsUnMarshaller)
						{
							GenerateUnmarshaler(message.type, sb, packValue);
						}

						var needsMarshaller = message.attr.GetNamedValue<CodeGeneration>(nameof(TSInteropMessageAttribute.Marshaller)) switch
						{
							CodeGeneration.Enabled => true,
							CodeGeneration.Disabled => false,
							_ => message.type.Name.EndsWith("Return") || message.type.Name.EndsWith("EventArgs"),
						};
						if (needsMarshaller)
						{
							GenerateMarshaler(message.type, sb, packValue);
						}
					}
				}

				var outputPath = Path.Combine(_bindingsPaths, $"{(ns is null ? "" : ns.Replace('.', '_') + "_")}{message.type.Name}.ts");

				var fileExists = File.Exists(outputPath);
				var output = sb.ToString();

				if (!fileExists || File.ReadAllText(outputPath) != output)
				{
					File.WriteAllText(outputPath, output);
				}
			}
		}

		private static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(IModuleSymbol module)
		{
			foreach (var type in module.GlobalNamespace.GetNamespaceTypes())
			{
				yield return type;

				foreach (var inner in type.GetTypeMembers())
				{
					yield return inner;
				}
			}
		}

		private int GetStructPack(ISymbol parametersType)
		{
			// https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/Symbols/TypeLayout.cs is not available.

			var actualSymbol = GetActualSymbol(parametersType);

			if (actualSymbol.GetType().GetProperty("Layout", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is PropertyInfo info)
			{
				if (info.GetValue(actualSymbol) is { } typeLayout)
				{
					if (typeLayout.GetType().GetProperty("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is PropertyInfo layoutKingProperty)
					{
						if (((System.Runtime.InteropServices.LayoutKind)layoutKingProperty.GetValue(typeLayout)) == System.Runtime.InteropServices.LayoutKind.Sequential)
						{
							if (typeLayout.GetType().GetProperty("Alignment", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) is PropertyInfo alignmentProperty)
							{
								return (short)alignmentProperty.GetValue(typeLayout);
							}
						}
						else
						{
							throw new InvalidOperationException($"The LayoutKind for {parametersType} must be LayoutKind.Sequential");
						}
					}
				}
			}

			throw new InvalidOperationException($"Failed to get structure layout, unknown roslyn internal structure");
		}

		private bool IsMarshalledExplicitly(IFieldSymbol fieldSymbol)
		{
			// https://github.com/dotnet/roslyn/blob/0610c79807fa59d0815f2b89e5283cf6d630b71e/src/Compilers/CSharp/Portable/Symbols/Metadata/PE/PEFieldSymbol.cs#L133 is not available.

			var actualSymbol = GetActualSymbol(fieldSymbol);

			if (actualSymbol.GetType().GetProperty(
				"IsMarshalledExplicitly",
				BindingFlags.Instance | BindingFlags.NonPublic) is PropertyInfo info
			)
			{
				if (info.GetValue(actualSymbol) is bool isMarshalledExplicitly)
				{
					return isMarshalledExplicitly;
				}
			}

			throw new InvalidOperationException($"Failed to IsMarshalledExplicitly, unknown roslyn internal structure");
		}

		/// <summary>
		/// Reads the actual symbol as Roslyn 3.6+ wraps symbols and we need access to the original type properties.
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		private object GetActualSymbol(ISymbol symbol)
		{
			if (symbol.GetType().GetProperty("UnderlyingSymbol", BindingFlags.Instance | BindingFlags.NonPublic) is PropertyInfo info)
			{
				if (info.GetValue(symbol) is { } underlyingSymbol)
				{
					return underlyingSymbol;
				}
			}

			return symbol;
		}

		private void GenerateMarshaler(INamedTypeSymbol parametersType, IndentedStringBuilder sb, int packValue)
		{
			using (sb.BlockInvariant($"public marshal(pData:number)"))
			{
				var fieldOffset = 0;

				foreach (var field in parametersType.GetFields())
				{
					var fieldSize = GetNativeFieldSize(field);
					bool isStringField = field.Type.SpecialType == SpecialType.System_String;

					if (field.Type is IArrayTypeSymbol arraySymbol)
					{
						throw new NotSupportedException($"Return value array fields are not supported ({field})");
					}
					else
					{
						var value = $"this.{field.Name}";

						if (isStringField)
						{
							using (sb.BlockInvariant(""))
							{
								sb.AppendLineInvariant($"const stringLength = lengthBytesUTF8({value});");
								sb.AppendLineInvariant($"const pString = Module._malloc(stringLength + 1);");
								sb.AppendLineInvariant($"stringToUTF8({value}, pString, stringLength + 1);");

								sb.AppendLineInvariant(
									$"Module.setValue(pData + {fieldOffset}, pString, \"*\");"
								);
							}
						}
						else
						{
							sb.AppendLineInvariant(
								$"Module.setValue(pData + {fieldOffset}, {value}, \"{GetEMField(field.Type)}\");"
							);
						}
					}

					fieldOffset += fieldSize;

					var adjust = fieldOffset % packValue;
					if (adjust != 0)
					{
						fieldOffset += (packValue - adjust);
					}
				}
			}
		}

		private void GenerateUnmarshaler(INamedTypeSymbol parametersType, IndentedStringBuilder sb, int packValue)
		{
			using (sb.BlockInvariant($"public static unmarshal(pData:number) : {parametersType.Name}"))
			{
				sb.AppendLineInvariant($"const ret = new {parametersType.Name}();");

				var fieldOffset = 0;
				foreach (var field in parametersType.GetFields())
				{
					var fieldSize = GetNativeFieldSize(field);

					if (field.Type is IArrayTypeSymbol arraySymbol)
					{
						if (!IsMarshalledExplicitly(field))
						{
							// This is required by the mono-wasm AOT engine for fields to be properly considered.
							throw new InvalidOperationException($"The field {field} is an array but does not have a [MarshalAs(UnmanagedType.LPArray)] attribute");
						}

						var elementType = arraySymbol.ElementType;
						var elementTSType = GetTSType(elementType);
						var isElementString = elementType.SpecialType == SpecialType.System_String;
						var elementSize = isElementString ? 4 : fieldSize;

						using (sb.BlockInvariant(""))
						{
							sb.AppendLineInvariant($"const pArray = Module.getValue(pData + {fieldOffset}, \"*\");");

							using (sb.BlockInvariant("if(pArray !== 0)"))
							{
								sb.AppendLineInvariant($"ret.{field.Name} = new Array<{GetTSFieldType(elementType)}>();");

								using (sb.BlockInvariant($"for(var i=0; i<ret.{field.Name}_Length; i++)"))
								{
									sb.AppendLineInvariant($"const value = Module.getValue(pArray + i * {elementSize}, \"{GetEMField(field.Type)}\");");

									if (isElementString)
									{
										using (sb.BlockInvariant("if(value !== 0)"))
										{
											sb.AppendLineInvariant($"ret.{field.Name}.push({elementTSType}(MonoRuntime.conv_string(value)));");
										}
										sb.AppendLineInvariant("else");
										using (sb.BlockInvariant(""))
										{
											sb.AppendLineInvariant($"ret.{field.Name}.push(null);");
										}
									}
									else
									{
										sb.AppendLineInvariant($"ret.{field.Name}.push({elementTSType}(value));");
									}
								}
							}
							sb.AppendLineInvariant("else");
							using (sb.BlockInvariant(""))
							{
								sb.AppendLineInvariant($"ret.{field.Name} = null;");
							}
						}
					}
					else
					{
						using (sb.BlockInvariant(""))
						{
							if (field.Type.SpecialType == SpecialType.System_String)
							{
								sb.AppendLineInvariant($"const ptr = Module.getValue(pData + {fieldOffset}, \"{GetEMField(field.Type)}\");");

								using (sb.BlockInvariant("if(ptr !== 0)"))
								{
									sb.AppendLineInvariant($"ret.{field.Name} = {GetTSType(field.Type)}(Module.UTF8ToString(ptr));");
								}
								sb.AppendLineInvariant("else");
								using (sb.BlockInvariant(""))
								{
									sb.AppendLineInvariant($"ret.{field.Name} = null;");
								}
							}
							else
							{
								if (CanUseEMHeapProperty(field.Type))
								{
									sb.AppendLineInvariant($"ret.{field.Name} = Module.{GetEMHeapProperty(field.Type)}[(pData + {fieldOffset}) >> {GetEMTypeShift(field)}];");
								}
								else
								{
									sb.AppendLineInvariant($"ret.{field.Name} = {GetTSType(field.Type)}(Module.getValue(pData + {fieldOffset}, \"{GetEMField(field.Type)}\"));");
								}
							}
						}
					}

					fieldOffset += fieldSize;

					var adjust = fieldOffset % packValue;
					if (adjust != 0)
					{
						fieldOffset += (packValue - adjust);
					}
				}

				sb.AppendLineInvariant($"return ret;");
			}
		}

		private bool CanUseEMHeapProperty(ITypeSymbol type)
			=> type.SpecialType == SpecialType.System_UInt32;

		private int GetNativeFieldSize(IFieldSymbol field)
		{
			if (
				field.Type.SpecialType is SpecialType.System_String ||
				field.Type.SpecialType is SpecialType.System_Int32 ||
				field.Type.SpecialType is SpecialType.System_UInt32 ||
				SymbolEqualityComparer.Default.Equals(field.Type, _intPtrSymbol) ||
				field.Type.SpecialType is SpecialType.System_Single ||
				field.Type.SpecialType is SpecialType.System_Boolean ||
				field.Type.SpecialType is SpecialType.System_Byte ||
				field.Type is IArrayTypeSymbol
			)
			{
				return 4;
			}
			else if (field.Type.SpecialType == SpecialType.System_Double)
			{
				return 8;
			}
			else
			{
				throw new NotSupportedException($"The field [{field} {field.Type}] is not supported");
			}
		}

		private int GetEMTypeShift(IFieldSymbol field)
		{
			var fieldType = field.Type;

			if (
				fieldType.SpecialType == SpecialType.System_String
				|| SymbolEqualityComparer.Default.Equals(fieldType, _intPtrSymbol)
				|| fieldType is IArrayTypeSymbol
			)
			{
				return 2;
			}
			else if (
				fieldType.SpecialType == SpecialType.System_Int32 ||
				fieldType.SpecialType == SpecialType.System_UInt32 ||
				fieldType.SpecialType == SpecialType.System_Boolean
			)
			{
				return 2;
			}
			else if (fieldType.SpecialType == SpecialType.System_Int64)
			{
				return 3;
			}
			else if (fieldType.SpecialType == SpecialType.System_Int16)
			{
				return 1;
			}
			else if (fieldType.SpecialType == SpecialType.System_Byte)
			{
				return 0;
			}
			else if (fieldType.SpecialType == SpecialType.System_Single)
			{
				return 2;
			}
			else if (fieldType.SpecialType == SpecialType.System_Double)
			{
				return 3;
			}
			else
			{
				throw new NotSupportedException($"Unsupported EM type conversion [{fieldType}]");
			}
		}

		private static string GetEMField(ITypeSymbol fieldType)
		{
			if (
				fieldType.SpecialType == SpecialType.System_String ||
				SymbolEqualityComparer.Default.Equals(fieldType, _intPtrSymbol) ||
				fieldType is IArrayTypeSymbol
			)
			{
				return "*";
			}
			else if (
				fieldType.SpecialType == SpecialType.System_Int32 ||
				fieldType.SpecialType == SpecialType.System_UInt32 ||
				fieldType.SpecialType == SpecialType.System_Boolean
			)
			{
				return "i32";
			}
			else if (fieldType.SpecialType == SpecialType.System_Int64)
			{
				return "i64";
			}
			else if (fieldType.SpecialType == SpecialType.System_Int16)
			{
				return "i16";
			}
			else if (fieldType.SpecialType == SpecialType.System_Byte)
			{
				return "i8";
			}
			else if (fieldType.SpecialType == SpecialType.System_Single)
			{
				return "float";
			}
			else if (fieldType.SpecialType == SpecialType.System_Double)
			{
				return "double";
			}
			else
			{
				throw new NotSupportedException($"Unsupported EM type conversion [{fieldType}]");
			}
		}

		private object GetEMHeapProperty(ITypeSymbol fieldType)
		{
			if (
				fieldType.SpecialType == SpecialType.System_String ||
				SymbolEqualityComparer.Default.Equals(fieldType, _intPtrSymbol) ||
				fieldType is IArrayTypeSymbol ||
				fieldType.SpecialType == SpecialType.System_Int32 ||
				fieldType.SpecialType == SpecialType.System_Boolean
			)
			{
				return "HEAP32";
			}
			else if (fieldType.SpecialType == SpecialType.System_UInt32)
			{
				return "HEAPU32";
			}
			else if (fieldType.SpecialType == SpecialType.System_Int64)
			{
				// Might overflow
				return "HEAP32";
			}
			else if (fieldType.SpecialType == SpecialType.System_Int16)
			{
				return "HEAP16";
			}
			else if (fieldType.SpecialType == SpecialType.System_Byte)
			{
				return "HEAP8";
			}
			else if (fieldType.SpecialType == SpecialType.System_Single)
			{
				return "HEAPF32";
			}
			else if (fieldType.SpecialType == SpecialType.System_Double)
			{
				return "HEAPF64";
			}
			else
			{
				throw new NotSupportedException($"Unsupported EM type conversion [{fieldType}]");
			}
		}

		private static string GetTSType(ITypeSymbol type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			if (type is IArrayTypeSymbol array)
			{
				return $"Array<{GetTSType(array.ElementType)}>";
			}
			else if (type.SpecialType == SpecialType.System_String)
			{
				return "String";
			}
			else if (
				type.SpecialType == SpecialType.System_Int32 ||
				type.SpecialType == SpecialType.System_UInt32 ||
				type.SpecialType == SpecialType.System_Single ||
				type.SpecialType == SpecialType.System_Double ||
				type.SpecialType == SpecialType.System_Byte ||
				type.SpecialType == SpecialType.System_Int16 ||
				SymbolEqualityComparer.Default.Equals(type, _intPtrSymbol)
			)
			{
				return "Number";
			}
			else if (type.SpecialType == SpecialType.System_Boolean)
			{
				return "Boolean";
			}
			else
			{
				throw new NotSupportedException($"The type {type} is not supported");
			}
		}

		private static string GetTSFieldType(ITypeSymbol type)
		{
			if (type == null)
			{
				throw new ArgumentNullException(nameof(type));
			}

			if (type is IArrayTypeSymbol array)
			{
				return $"Array<{GetTSFieldType(array.ElementType)}>";
			}
			else if (type.SpecialType == SpecialType.System_String)
			{
				return "string";
			}
			else if (
				type.SpecialType == SpecialType.System_Int32 ||
				type.SpecialType == SpecialType.System_UInt32 ||
				type.SpecialType == SpecialType.System_Single ||
				type.SpecialType == SpecialType.System_Double ||
				type.SpecialType == SpecialType.System_Byte ||
				type.SpecialType == SpecialType.System_Int16 ||
				SymbolEqualityComparer.Default.Equals(type, _intPtrSymbol)
			)
			{
				return "number";
			}
			else if (type.SpecialType == SpecialType.System_Boolean)
			{
				return "boolean";
			}
			else
			{
				throw new NotSupportedException($"The type {type} is not supported");
			}
		}

	}
}
