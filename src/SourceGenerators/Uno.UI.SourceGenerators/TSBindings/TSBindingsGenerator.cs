using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;

#if NETFRAMEWORK
using Uno.SourceGeneration;
using Uno.UI.SourceGenerators.Helpers;
#endif

namespace Uno.UI.SourceGenerators.TSBindings
{
	[Generator]
	class TSBindingsGenerator : ISourceGenerator
	{
		private string _bindingsPaths;
		private string[] _sourceAssemblies;

		private static INamedTypeSymbol _stringSymbol;
		private static INamedTypeSymbol _intSymbol;
		private static INamedTypeSymbol _floatSymbol;
		private static INamedTypeSymbol _doubleSymbol;
		private static INamedTypeSymbol _byteSymbol;
		private static INamedTypeSymbol _shortSymbol;
		private static INamedTypeSymbol _intPtrSymbol;
		private static INamedTypeSymbol _boolSymbol;
		private static INamedTypeSymbol _longSymbol;
		private static INamedTypeSymbol _structLayoutSymbol;
		private static INamedTypeSymbol _interopMessageSymbol;

		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			DependenciesInitializer.Init(context);

			if (!DesignTimeHelper.IsDesignTime(context))
			{
				_bindingsPaths = context.GetMSBuildPropertyValue("TSBindingsPath")?.ToString();
				_sourceAssemblies = context.GetMSBuildItems("TSBindingAssemblySource").Select(i => i.Identity).ToArray();

				if (!string.IsNullOrEmpty(_bindingsPaths))
				{
					_stringSymbol = context.Compilation.GetTypeByMetadataName("System.String");
					_intSymbol = context.Compilation.GetTypeByMetadataName("System.Int32");
					_floatSymbol = context.Compilation.GetTypeByMetadataName("System.Single");
					_doubleSymbol = context.Compilation.GetTypeByMetadataName("System.Double");
					_byteSymbol = context.Compilation.GetTypeByMetadataName("System.Byte");
					_shortSymbol = context.Compilation.GetTypeByMetadataName("System.Int16");
					_intPtrSymbol = context.Compilation.GetTypeByMetadataName("System.IntPtr");
					_boolSymbol = context.Compilation.GetTypeByMetadataName("System.Boolean");
					_longSymbol = context.Compilation.GetTypeByMetadataName("System.Int64");
					_structLayoutSymbol = context.Compilation.GetTypeByMetadataName(typeof(System.Runtime.InteropServices.StructLayoutAttribute).FullName);
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
			var messageTypes = from module in modules
							   from type in GetNamespaceTypes(module)
							   where (
								   type.FindAttributeFlattened(_interopMessageSymbol) != null
								   && type.TypeKind == TypeKind.Struct
							   )
							   select type;

			messageTypes = messageTypes.ToArray();

			foreach (var messageType in messageTypes)
			{
				var packValue = GetStructPack(messageType);

				var sb = new IndentedStringBuilder();

				sb.AppendLineInvariant($"/* {nameof(TSBindingsGenerator)} Generated code -- this code is regenerated on each build */");

				using (sb.BlockInvariant($"class {messageType.Name}"))
				{
					sb.AppendLineInvariant($"/* Pack={packValue} */");

					foreach (var field in messageType.GetFields())
					{
						sb.AppendLineInvariant($"public {field.Name} : {GetTSFieldType(field.Type)};");
					}

					if (messageType.Name.EndsWith("Params"))
					{
						GenerateUmarshaler(messageType, sb, packValue);
					}

					if (messageType.Name.EndsWith("Return"))
					{
						GenerateMarshaler(messageType, sb, packValue);
					}
				}

				var outputPath = Path.Combine(_bindingsPaths, $"{messageType.Name}.ts");

				var fileExists = File.Exists(outputPath);
				var output = sb.ToString();

				if (
					(fileExists && File.ReadAllText(outputPath) != output)
					|| !fileExists)
				{
					File.WriteAllText(outputPath, output);
				}
			}
		}

		private static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(IModuleSymbol module)
		{
			foreach(var type in module.GlobalNamespace.GetNamespaceTypes())
			{
				yield return type;

				foreach(var inner in type.GetTypeMembers())
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
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is PropertyInfo info
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
			if (symbol.GetType().GetProperty("UnderlyingSymbol", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is PropertyInfo info)
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
					bool isStringField = Equals(field.Type, _stringSymbol);

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

		private void GenerateUmarshaler(INamedTypeSymbol parametersType, IndentedStringBuilder sb, int packValue)
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
						var isElementString = Equals(elementType, _stringSymbol);
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
							if(Equals(field.Type, _stringSymbol))
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
								sb.AppendLineInvariant($"ret.{field.Name} = {GetTSType(field.Type)}(Module.getValue(pData + {fieldOffset}, \"{GetEMField(field.Type)}\"));");
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

		private int GetNativeFieldSize(IFieldSymbol field)
		{
			if(
				Equals(field.Type, _stringSymbol)
				|| Equals(field.Type, _intSymbol)
				|| Equals(field.Type, _intPtrSymbol)
				|| Equals(field.Type, _floatSymbol)
				|| Equals(field.Type, _boolSymbol)
				|| field.Type is IArrayTypeSymbol
			)
			{
				return 4;
			}
			else if(Equals(field.Type, _doubleSymbol))
			{
				return 8;
			}
			else
			{
				throw new NotSupportedException($"The field [{field} {field.Type}] is not supported");
			}
		}

		private static string GetEMField(ITypeSymbol fieldType)
		{
			if (
				Equals(fieldType, _stringSymbol)
				|| Equals(fieldType, _intPtrSymbol)
				|| fieldType is IArrayTypeSymbol
			)
			{
				return "*";
			}
			else if (
				Equals(fieldType, _intSymbol)
				|| Equals(fieldType, _boolSymbol)
			)
			{
				return "i32";
			}
			else if (Equals(fieldType, _longSymbol))
			{
				return "i64";
			}
			else if (Equals(fieldType, _shortSymbol))
			{
				return "i16";
			}
			else if (Equals(fieldType, _byteSymbol))
			{
				return "i8";
			}
			else if (Equals(fieldType, _floatSymbol))
			{
				return "float";
			}
			else if (Equals(fieldType, _doubleSymbol))
			{
				return "double";
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
			else if (Equals(type, _stringSymbol))
			{
				return "String";
			}
			else if (
				Equals(type, _intSymbol)
				|| Equals(type, _floatSymbol)
				|| Equals(type, _doubleSymbol)
				|| Equals(type, _byteSymbol)
				|| Equals(type, _shortSymbol)
				|| Equals(type, _intPtrSymbol)
			)
			{
				return "Number";
			}
			else if (Equals(type, _boolSymbol))
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
			else if (Equals(type, _stringSymbol))
			{
				return "string";
			}
			else if (
				Equals(type, _intSymbol)
				|| Equals(type, _floatSymbol)
				|| Equals(type, _doubleSymbol)
				|| Equals(type, _byteSymbol)
				|| Equals(type, _shortSymbol)
				|| Equals(type, _intPtrSymbol)
			)
			{
				return "number";
			}
			else if (Equals(type, _boolSymbol))
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
