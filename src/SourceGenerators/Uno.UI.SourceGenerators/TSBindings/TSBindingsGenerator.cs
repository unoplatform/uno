using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.SourceGeneration;

namespace Uno.UI.SourceGenerators.TSBindings
{
	class TSBindingsGenerator : SourceGenerator
	{
		private string _bindingsPaths;
		private INamedTypeSymbol _windowManagerSymbol;
		private static ITypeSymbol _stringSymbol;
		private static ITypeSymbol _intSymbol;
		private static ITypeSymbol _floatSymbol;
		private static ITypeSymbol _doubleSymbol;
		private static ITypeSymbol _byteSymbol;
		private static ITypeSymbol _shortSymbol;
		private static ITypeSymbol _intPtrSymbol;
		private static ITypeSymbol _boolSymbol;
		private static ITypeSymbol _longSymbol;
		private INamedTypeSymbol _structLayoutSymbol;

		public override void Execute(SourceGeneratorContext context)
		{
			var project = context.GetProjectInstance();
			_bindingsPaths = project.GetPropertyValue("TSBindingsPath")?.ToString();

			if(!string.IsNullOrEmpty(_bindingsPaths))
			{
				_windowManagerSymbol = context.Compilation.GetTypeByMetadataName("Uno.UI.Xaml.WindowManagerInterop");

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

				GenerateTSMarshallingLayouts();
			}
		}

		internal void GenerateTSMarshallingLayouts()
		{
			foreach (var parametersType in _windowManagerSymbol.GetTypeMembers().Where(t => t.IsValueType))
			{
				var packValue = GetStructPack(parametersType);

				var sb = new IndentedStringBuilder();

				sb.AppendLineInvariant($"/* {nameof(TSBindingsGenerator)} Generated code -- this code is regenerated on each build */");

				using (sb.BlockInvariant($"class {parametersType.Name}"))
				{
					sb.AppendLineInvariant($"/* Pack={packValue} */");

					foreach (var field in parametersType.GetFields())
					{
						sb.AppendLineInvariant($"{field.Name} : {GetTSFieldType(field.Type)};");
					}

					if (parametersType.Name.EndsWith("Params"))
					{
						GenerateUmarshaler(parametersType, sb, packValue);
					}

					if (parametersType.Name.EndsWith("Return"))
					{
						GenerateMarshaler(parametersType, sb, packValue);
					}
				}

				var outputPath = Path.Combine(_bindingsPaths, $"{parametersType.Name}.ts");

				File.WriteAllText(outputPath, sb.ToString());
			}
		}

		private int GetStructPack(INamedTypeSymbol parametersType)
		{
			// https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/Symbols/TypeLayout.cs is not available.

			if(parametersType.GetType().GetProperty("Layout", System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic) is PropertyInfo info)
			{
				if(info.GetValue(parametersType) is object typeLayout)
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

		private void GenerateMarshaler(INamedTypeSymbol parametersType, IndentedStringBuilder sb, int packValue)
		{
			using (sb.BlockInvariant($"public marshal(pData:number)"))
			{
				var fieldOffset = 0;

				foreach (var field in parametersType.GetFields())
				{
					var fieldSize = GetNativeFieldSize(field);

					var stringConv = field.Type == _stringSymbol ? "Module.UTF8ToString" : "";

					if (field.Type is IArrayTypeSymbol arraySymbol)
					{
						throw new NotSupportedException($"Return value array fields are not supported ({field})");
					}
					else
					{
						var value = $"this.{field.Name}";

						sb.AppendLineInvariant(
							$"Module.setValue(pData + {fieldOffset}, {value}, \"{GetEMField(field.Type)}\");"
						);
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
				sb.AppendLineInvariant($"let ret = new {parametersType.Name}();");

				var fieldOffset = 0;
				foreach (var field in parametersType.GetFields())
				{
					var fieldSize = GetNativeFieldSize(field);

					if (field.Type is IArrayTypeSymbol arraySymbol)
					{
						var elementType = arraySymbol.ElementType;
						var elementTSType = GetTSType(elementType);
						var isElementString = elementType == _stringSymbol;
						var elementSize = isElementString ? 4 : fieldSize;

						using (sb.BlockInvariant(""))
						{
							sb.AppendLineInvariant($"ret.{field.Name} = new Array<{GetTSFieldType(elementType)}>();");
							sb.AppendLineInvariant($"var pArray = Module.getValue(pData + {fieldOffset}, \"*\");");

							using (sb.BlockInvariant($"for(var i=0; i<ret.{field.Name}_Length; i++)"))
							{
								var stringConv = isElementString ? "MonoRuntime.conv_string" : "";
								var getValue = $"{stringConv}(Module.getValue(pArray + i*{elementSize}, \"{GetEMField(field.Type)}\"))";

								sb.AppendLineInvariant($"ret.{field.Name}.push({elementTSType}({getValue}));");
							}
						}
					}
					else
					{
						var stringConv = field.Type == _stringSymbol ? "Module.UTF8ToString" : "";

						sb.AppendLineInvariant($"ret.{field.Name} = {GetTSType(field.Type)}({stringConv}(Module.getValue(pData + {fieldOffset}, \"{GetEMField(field.Type)}\")));");
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
				field.Type == _stringSymbol
				|| field.Type == _intSymbol
				|| field.Type == _intPtrSymbol
				|| field.Type == _floatSymbol
				|| field.Type == _boolSymbol
				|| field.Type is IArrayTypeSymbol
			)
			{
				return 4;
			}
			else if(field.Type == _doubleSymbol)
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
				fieldType == _stringSymbol
				|| fieldType == _intPtrSymbol
				|| fieldType is IArrayTypeSymbol
			)
			{
				return "*";
			}
			else if (
				fieldType == _intSymbol
				|| fieldType == _boolSymbol
			)
			{
				return "i32";
			}
			else if (fieldType == _longSymbol)
			{
				return "i64";
			}
			else if (fieldType == _shortSymbol)
			{
				return "i16";
			}
			else if (fieldType == _byteSymbol)
			{
				return "i8";
			}
			else if (fieldType == _floatSymbol)
			{
				return "float";
			}
			else if (fieldType == _doubleSymbol)
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
			else if (type == _stringSymbol)
			{
				return "String";
			}
			else if (
				type == _intSymbol
				|| type == _floatSymbol
				|| type == _doubleSymbol
				|| type == _byteSymbol
				|| type == _shortSymbol
				|| type == _intPtrSymbol
			)
			{
				return "Number";
			}
			else if (type == _boolSymbol)
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
			else if (type == _stringSymbol)
			{
				return "string";
			}
			else if (
				type == _intSymbol
				|| type == _floatSymbol
				|| type == _doubleSymbol
				|| type == _byteSymbol
				|| type == _shortSymbol
				|| type == _intPtrSymbol
			)
			{
				return "number";
			}
			else if (type == _boolSymbol)
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
