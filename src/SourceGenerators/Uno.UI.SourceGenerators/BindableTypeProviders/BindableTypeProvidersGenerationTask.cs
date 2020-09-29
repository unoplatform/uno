using Uno.Logging;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Diagnostics;
using Uno.SourceGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.Build.Execution;
using Uno.UI.SourceGenerators.XamlGenerator;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection.Metadata.Ecma335;

namespace Uno.UI.SourceGenerators.BindableTypeProviders
{
	[GenerateAfter("Uno.ImmutableGenerator")]
	public class BindableTypeProvidersSourceGenerator : SourceGenerator
	{
		private const string TypeMetadataConfigFile = "TypeMetadataConfig.xml";

		private string _defaultNamespace;

		private Dictionary<INamedTypeSymbol, GeneratedTypeInfo> _typeMap = new Dictionary<INamedTypeSymbol, GeneratedTypeInfo>();
		private INamedTypeSymbol[] _bindableAttributeSymbol;
		private ITypeSymbol _dependencyPropertySymbol;
		private INamedTypeSymbol _objectSymbol;
		private INamedTypeSymbol _javaObjectSymbol;
		private INamedTypeSymbol _nsObjectSymbol;
		private INamedTypeSymbol _nonBindableSymbol;
		private INamedTypeSymbol _resourceDictionarySymbol;
		private IModuleSymbol _currentModule;

		public string[] AnalyzerSuppressions { get; set; }

		public INamedTypeSymbol _stringSymbol { get; private set; }

		public override void Execute(SourceGeneratorContext context)
		{
			try
			{
				if (PlatformHelper.IsValidPlatform(context))
				{
					var project = context.GetProjectInstance();

					if (IsApplication(project))
					{
						_defaultNamespace = project.GetPropertyValue("RootNamespace");

						_bindableAttributeSymbol = FindBindableAttributes(context);
						_dependencyPropertySymbol = context.Compilation.GetTypeByMetadataName(XamlConstants.Types.DependencyProperty);

						_objectSymbol = context.Compilation.GetTypeByMetadataName("System.Object");
						_javaObjectSymbol = context.Compilation.GetTypeByMetadataName("Java.Lang.Object");
						_nsObjectSymbol = context.Compilation.GetTypeByMetadataName("Foundation.NSObject");
						_stringSymbol = context.Compilation.GetTypeByMetadataName("System.String");
						_nonBindableSymbol = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.Data.NonBindableAttribute");
						_resourceDictionarySymbol = context.Compilation.GetTypeByMetadataName("Windows.UI.Xaml.ResourceDictionary");
						_currentModule = context.Compilation.SourceModule;

						AnalyzerSuppressions = new string[0];

						var modules = from ext in context.Compilation.ExternalReferences
									  let sym = context.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
									  where sym != null
									  from module in sym.Modules
									  select module;

						modules = modules.Concat(context.Compilation.SourceModule);

						context.AddCompilationUnit("BindableMetadata", GenerateTypeProviders(modules));
					}
				}
			}
			catch(Exception e)
			{
				var message = e.Message + e.StackTrace;

				if (e is AggregateException)
				{
					message = (e as AggregateException).InnerExceptions.Select(ex => ex.Message + e.StackTrace).JoinBy("\r\n");
				}

				this.Log().Error("Failed to generate type providers.", new Exception("Failed to generate type providers." + message, e));
			}
		}

		private static INamedTypeSymbol[] FindBindableAttributes(SourceGeneratorContext context) => 
			SymbolFinder.FindDeclarationsAsync(context.Project, "BindableAttribute", false).Result.OfType<INamedTypeSymbol>().ToArray();

		private bool IsApplication(ProjectInstance projectInstance)
		{
			var isAndroidApp = projectInstance.GetPropertyValue("AndroidApplication")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
			var isiOSApp = projectInstance.GetPropertyValue("ProjectTypeGuids")?.Equals("{FEACFBD2-3405-455C-9665-78FE426C6842};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var ismacOSApp = projectInstance.GetPropertyValue("ProjectTypeGuids")?.Equals("{A3F8F2AB-B479-4A4A-A458-A89E7DC349F1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var isExe = projectInstance.GetPropertyValue("OutputType")?.Equals("Exe", StringComparison.OrdinalIgnoreCase) ?? false;
			var isUnoHead = projectInstance.GetPropertyValue("IsUnoHead")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

			return isAndroidApp
				|| (isiOSApp && isExe)
				|| (ismacOSApp && isExe)
				|| isUnoHead;
		}

		private string GenerateTypeProviders(IEnumerable<IModuleSymbol> modules)
		{
			var q = from module in modules
					from type in module.GlobalNamespace.GetNamespaceTypes()
					where (
						_bindableAttributeSymbol.Any(s => type.FindAttributeFlattened(s) != null)
						&& !type.IsGenericType
						&& !type.IsAbstract
						&& IsValidProvider(type)
					)
					select type;

			q = q.ToArray();

			var writer = new IndentedStringBuilder();

			writer.AppendLineInvariant("// <auto-generated>");
			writer.AppendLineInvariant("// *****************************************************************************");
			writer.AppendLineInvariant("// This file has been generated by Uno.UI (BindableTypeProvidersSourceGenerator)");
			writer.AppendLineInvariant("// *****************************************************************************");
			writer.AppendLineInvariant("// </auto-generated>");
			writer.AppendLine();
			writer.AppendLineInvariant("#pragma warning disable 618  // Ignore obsolete members warnings");
			writer.AppendLineInvariant("#pragma warning disable 1591 // Ignore missing XML comment warnings");
			writer.AppendLineInvariant("using System;");
			writer.AppendLineInvariant("using System.Linq;");
			writer.AppendLineInvariant("using System.Diagnostics;");

			using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
			{
				GenerateMetadata(writer, q);
				GenerateProviderTable(q, writer);
			}

			return writer.ToString();
		}

		private bool IsValidProvider(INamedTypeSymbol type)
			=> type.IsLocallyPublic(_currentModule)

			// Exclude resource dictionaries for linking constraints (XamlControlsResources in particular)
			// Those are not databound, so there's no need to generate providers for them.
			&& !type.Is(_resourceDictionarySymbol);

		private void GenerateProviderTable(IEnumerable<INamedTypeSymbol> q, IndentedStringBuilder writer)
		{
			writer.AppendLineInvariant("[System.Runtime.CompilerServices.CompilerGeneratedAttribute]");
			writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1502:AvoidExcessiveComplexity\", Justification=\"Must be ignored even if generated code is checked.\")]");
			writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1506:AvoidExcessiveClassCoupling\", Justification = \"Must be ignored even if generated code is checked.\")]");

			AnalyzerSuppressionsGenerator.Generate(writer, AnalyzerSuppressions);
			using (writer.BlockInvariant("public class BindableMetadataProvider : global::Uno.UI.DataBinding.IBindableMetadataProvider"))
			{
				writer.AppendLineInvariant(@"static global::System.Collections.Hashtable _bindableTypeCacheByFullName = new global::System.Collections.Hashtable({0});", q.Count());

				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1502:AvoidExcessiveComplexity\", Justification=\"Must be ignored even if generated code is checked.\")]");
				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1506:AvoidExcessiveClassCoupling\", Justification = \"Must be ignored even if generated code is checked.\")]");
				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1505:AvoidUnmaintainableCode\", Justification = \"Must be ignored even if generated code is checked.\")]");

				writer.AppendLineInvariant("private delegate global::Uno.UI.DataBinding.IBindableType TypeBuilderDelegate();");

				using (writer.BlockInvariant("private static TypeBuilderDelegate CreateMemoized(TypeBuilderDelegate builder)"))
				{
					writer.AppendLineInvariant(@"global::Uno.UI.DataBinding.IBindableType value = null;
						return () => {{
							if (value == null)
							{{
								value = builder();
							}}

							return value;
						}};"
					);
				}

				using (writer.BlockInvariant("static BindableMetadataProvider()"))
				{
					GenerateTypeTable(writer, q);
				}

				writer.AppendLineInvariant(@"#if DEBUG");
				writer.AppendLineInvariant(@"private global::System.Collections.Generic.List<global::System.Type> _knownMissingTypes = new global::System.Collections.Generic.List<global::System.Type>();");
				writer.AppendLineInvariant(@"#endif");

				using (writer.BlockInvariant("public void ForceInitialize()"))
				{
					using (writer.BlockInvariant(@"foreach(TypeBuilderDelegate item in _bindableTypeCacheByFullName.Values)"))
					{
						writer.AppendLineInvariant(@"item();");
					}
				}

				using (writer.BlockInvariant("public global::Uno.UI.DataBinding.IBindableType GetBindableTypeByFullName(string fullName)"))
				{
					writer.AppendLineInvariant(@"var selector = _bindableTypeCacheByFullName[fullName] as TypeBuilderDelegate;");

					using (writer.BlockInvariant(@"if(selector != null)"))
					{
						writer.AppendLineInvariant(@"return selector();");
					}
					using (writer.BlockInvariant(@"else"))
					{
						writer.AppendLineInvariant(@"return null;");
					}
				}

				using (writer.BlockInvariant("public global::Uno.UI.DataBinding.IBindableType GetBindableTypeByType(Type type)"))
				{
					writer.AppendLineInvariant(@"var selector = _bindableTypeCacheByFullName[type.FullName] as TypeBuilderDelegate;");

					using (writer.BlockInvariant(@"if(selector != null)"))
					{
						writer.AppendLineInvariant(@"return selector();");
					}

					writer.AppendLineInvariant(@"#if DEBUG");
					using (writer.BlockInvariant(@"else"))
					{
						using (writer.BlockInvariant(@"lock(_knownMissingTypes)"))
						{
							using (writer.BlockInvariant(@"if(!_knownMissingTypes.Contains(type) || !type.IsGenericType)"))
							{
								writer.AppendLineInvariant(@"_knownMissingTypes.Add(type);");
								writer.AppendLineInvariant(@"Debug.WriteLine($""The Bindable attribute is missing and the type [{{type.FullName}}] is not known by the MetadataProvider. Reflection was used instead of the binding engine and generated static metadata. Add the Bindable attribute to prevent this message and performance issues."");");
							}
						}
					}
					writer.AppendLineInvariant(@"#endif");

					writer.AppendLineInvariant(@"return null;");
				}
			}
		}

		private class PropertyNameEqualityComparer : IEqualityComparer<IPropertySymbol>
		{
			public bool Equals(IPropertySymbol x, IPropertySymbol y)
			{
				return x.Name == y.Name;
			}

			public int GetHashCode(IPropertySymbol obj)
			{
				return obj.Name.GetHashCode();
			}
		}

		private void GenerateMetadata(IndentedStringBuilder writer, IEnumerable<INamedTypeSymbol> types)
		{
			foreach (var type in types)
			{
				GenerateType(writer, type);
			}
		}

		class GeneratedTypeInfo
		{
			public GeneratedTypeInfo(int index, bool hasProperties)
			{
				Index = index;
				HasProperties = hasProperties;
			}

			public int Index { get; private set; }

			public bool HasProperties { get; private set; }
		}

		private void GenerateType(IndentedStringBuilder writer, INamedTypeSymbol ownerType)
		{
			if (_typeMap.ContainsKey(ownerType))
			{
				return;
			}

			var ownerTypeName = GetGlobalQualifier(ownerType) + ownerType.ToString();

			var flattenedProperties =
				from property in ownerType.GetAllProperties()
				where !property.IsStatic
					&& !IsIgnoredType(property.ContainingSymbol as INamedTypeSymbol)
					&& !IsNonBindable(property)
					&& !IsOverride(property.GetMethod)
				select property;

			var properties =
				from property in ownerType.GetProperties()
				where !property.IsStatic
					&& !IsNonBindable(property)
					&& HasPublicGetter(property)
					&& !IsOverride(property.GetMethod)
				select property;

			var propertyDependencyProperties =
				from property in ownerType.GetProperties()
				where property.IsStatic
					&& Equals(property.Type, _dependencyPropertySymbol)
				select property.Name;

			var fieldDependencyProperties =
				from field in ownerType.GetFields()
				where field.IsStatic
					&& Equals(field.Type, _dependencyPropertySymbol)
				select field.Name;

			var dependencyProperties = fieldDependencyProperties
				.Concat(propertyDependencyProperties)
				.ToArray();

			properties = from prop in properties.Distinct(new PropertyNameEqualityComparer())
						 where !dependencyProperties.Contains(prop.Name + "Property")
						 select prop;

			properties = properties
				.ToArray();

			var typeInfo = new GeneratedTypeInfo(
				index: _typeMap.Count,
				hasProperties: properties.Any() || dependencyProperties.Any()
			);

			_typeMap.Add(ownerType, typeInfo);

			var baseType = GetBaseType(ownerType);

			// Call the builders for the base types
			if (baseType != null)
			{
				GenerateType(writer, baseType);
			}

			writer.AppendLineInvariant("/// <summary>");
			writer.AppendLineInvariant("/// Builder for {0}", ownerType.GetFullName());
			writer.AppendLineInvariant("/// </summary>");
			writer.AppendLineInvariant("[System.Runtime.CompilerServices.CompilerGeneratedAttribute]");
			writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1502:AvoidExcessiveComplexity\", Justification=\"Must be ignored even if generated code is checked.\")]");
			writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1506:AvoidExcessiveClassCoupling\", Justification = \"Must be ignored even if generated code is checked.\")]");

			AnalyzerSuppressionsGenerator.Generate(writer, AnalyzerSuppressions);
			using (writer.BlockInvariant("static class MetadataBuilder_{0:000}", typeInfo.Index))
			{
				var postWriter = new IndentedStringBuilder();
				postWriter.Indent(writer.CurrentLevel);

				// Generate a parameter-less build to avoid generating a lambda during registration (avoids creating a caching backing field)
				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1502:AvoidExcessiveComplexity\", Justification=\"Must be ignored even if generated code is checked.\")]");
				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1506:AvoidExcessiveClassCoupling\", Justification = \"Must be ignored even if generated code is checked.\")]");
				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1505:AvoidUnmaintainableCode\", Justification = \"Must be ignored even if generated code is checked.\")]");
				using (writer.BlockInvariant("internal static global::Uno.UI.DataBinding.IBindableType Build()"))
				{
					writer.AppendLineInvariant("return Build(null);");
				}

				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1502:AvoidExcessiveComplexity\", Justification=\"Must be ignored even if generated code is checked.\")]");
				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1506:AvoidExcessiveClassCoupling\", Justification = \"Must be ignored even if generated code is checked.\")]");
				writer.AppendLineInvariant("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1505:AvoidUnmaintainableCode\", Justification = \"Must be ignored even if generated code is checked.\")]");
				using (writer.BlockInvariant("internal static global::Uno.UI.DataBinding.IBindableType Build(global::Uno.UI.DataBinding.BindableType parent)"))
				{
					writer.AppendLineInvariant(
						@"var bindableType = parent ?? new global::Uno.UI.DataBinding.BindableType({0}, typeof({1}));",
						flattenedProperties
							.Where(p => !IsStringIndexer(p) && HasPublicGetter(p))
							.Count()
						, ExpandType(ownerType)
					);

					// Call the builders for the base types
					if (baseType != null)
					{
						var baseTypeMapped = _typeMap.UnoGetValueOrDefault(baseType);

						writer.AppendLineInvariant(@"MetadataBuilder_{0:000}.Build(bindableType); // {1}", baseTypeMapped.Index, ExpandType(baseType));
					}

					var ctor = ownerType.GetMethods().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor && !m.Parameters.Any() && m.IsLocallyPublic(_currentModule));

					if (ctor != null && IsCreateable(ownerType))
					{
						using (writer.BlockInvariant("if(parent == null)"))
						{
							writer.AppendLineInvariant(@"bindableType.AddActivator(CreateInstance);");
							postWriter.AppendLineInvariant($@"private static object CreateInstance() => new {ownerTypeName}();");
						}
					}

					foreach (var property in properties)
					{
						var propertyTypeName = GetFullyQualifiedType(property.Type);
						var propertyName = property.Name;

						if (IsStringIndexer(property))
						{
							writer.AppendLineInvariant("bindableType.AddIndexer(GetIndexer, SetIndexer);");

							postWriter.AppendLineInvariant($@"private static object GetIndexer(object instance, string name) => (({ownerTypeName})instance)[name];");

							if (property.SetMethod != null)
							{
								postWriter.AppendLineInvariant($@"private static void SetIndexer(object instance, string name, object value) => (({ownerTypeName})instance)[name] = ({propertyTypeName})value;");
							}
							else
							{
								postWriter.AppendLineInvariant("private static void SetIndexer(object instance, string name, object value) {{}}");
							}

							continue;
						}

						if (property.IsIndexer)
						{
							// Other types of indexers are currently not supported.
							continue;
						}

						if (
							property.SetMethod != null
							&& property.SetMethod != null
							&& property.SetMethod.IsLocallyPublic(_currentModule)
							)
						{
							if (property.Type.IsValueType)
							{
								writer.AppendLineInvariant($@"bindableType.AddProperty(""{propertyName}"", typeof({propertyTypeName}), Get{propertyName}, Set{propertyName});");

								postWriter.AppendLineInvariant($@"private static object Get{propertyName}(object instance, Windows.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => (({ownerTypeName})instance).{propertyName};");

								using (postWriter.BlockInvariant($@"private static void Set{propertyName}(object instance, object value, Windows.UI.Xaml.DependencyPropertyValuePrecedences? precedence)"))
								{
									using (postWriter.BlockInvariant($"if(value != null)"))
									{
										postWriter.AppendLineInvariant($"(({ownerTypeName})instance).{propertyName} = ({propertyTypeName})value;");
									}
								}
							}
							else
							{
								writer.AppendLineInvariant($@"bindableType.AddProperty(""{propertyName}"", typeof({propertyTypeName}), Get{propertyName}, Set{propertyName});");

								postWriter.AppendLineInvariant($@"private static object Get{propertyName}(object instance,  Windows.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => (({ownerTypeName})instance).{propertyName};");
								postWriter.AppendLineInvariant($@"private static void Set{propertyName}(object instance, object value, Windows.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => (({ownerTypeName})instance).{propertyName} = ({propertyTypeName})value;");
							}
						}
						else if (HasPublicGetter(property))
						{
							writer.AppendLineInvariant($@"bindableType.AddProperty(""{propertyName}"", typeof({propertyTypeName}), Get{propertyName});");

							postWriter.AppendLineInvariant($@"private static object Get{propertyName}(object instance, Windows.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => (({ownerTypeName})instance).{propertyName};");
						}
					}

					foreach (var dependencyProperty in dependencyProperties)
					{
						var propertyName = dependencyProperty.TrimEnd("Property");

						var getMethod = ownerType.GetMethods().FirstOrDefault(m => m.Name == "Get" + propertyName && m.Parameters.Length == 1 && m.IsLocallyPublic(_currentModule));
						var setMethod = ownerType.GetMethods().FirstOrDefault(m => m.Name == "Set" + propertyName && m.Parameters.Length == 2 && m.IsLocallyPublic(_currentModule));

						if (getMethod == null)
						{
							getMethod = ownerType
								.GetProperties()
								.FirstOrDefault(p => p.Name == propertyName && (p.GetMethod?.IsLocallyPublic(_currentModule) ?? false))
								?.GetMethod;
						}

						if (getMethod != null)
						{
							var getter = $"{XamlConstants.Types.DependencyObjectExtensions}.GetValue(instance, {ownerTypeName}.{propertyName}Property, precedence)";
							var setter = $"{XamlConstants.Types.DependencyObjectExtensions}.SetValue(instance, {ownerTypeName}.{propertyName}Property, value, precedence)";

							var propertyType = GetGlobalQualifier(getMethod.ReturnType) + SanitizeTypeName(getMethod.ReturnType.ToString());

							writer.AppendLineInvariant($@"bindableType.AddProperty(""{propertyName}"", typeof({propertyType}),  Get{propertyName}, Set{propertyName});");

							postWriter.AppendLineInvariant($@"private static object Get{propertyName}(object instance,  Windows.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => {getter};");
							postWriter.AppendLineInvariant($@"private static void Set{propertyName}(object instance, object value, Windows.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => {setter};");
						}
					}

					writer.AppendLineInvariant(@"return bindableType;");
				}

				writer.Append(postWriter.ToString());
			}

			writer.AppendLine();
		}

		private object GetFullyQualifiedType(ITypeSymbol type)
		{
			if(type is INamedTypeSymbol namedTypeSymbol)
			{
				if (namedTypeSymbol.IsGenericType && !namedTypeSymbol.IsNullable())
				{
					return SymbolDisplay.ToDisplayString(type, format: new SymbolDisplayFormat(
						globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
						typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
						genericsOptions: SymbolDisplayGenericsOptions.None,
						miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers))
						+ "<" + string.Join(", ", namedTypeSymbol.TypeArguments.Select(GetFullyQualifiedType)) + ">";
				}
			}

			return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}

		private static string ExpandType(INamedTypeSymbol ownerType)
		{
			if(ownerType.TypeKind == TypeKind.Error)
			{
				return ownerType.ToString() + "/* Type is error */";
			}
			else
			{
				return GetGlobalQualifier(ownerType) + ownerType.GetFullName();
			}
		}

		private static string GetGlobalQualifier(ITypeSymbol ownerType)
		{
			var arrayType = ownerType as IArrayTypeSymbol;
			if (arrayType != null)
			{
				return GetGlobalQualifier(arrayType.ElementType);
			}

			ITypeSymbol nullType;
			if (ownerType.IsNullable(out nullType))
			{
				return GetGlobalQualifier(nullType);
			}

			var needsGlobal = ownerType.SpecialType == SpecialType.None && !ownerType.IsTupleType;
			return (needsGlobal ? "global::" : "");
		}

		private bool IsCreateable(INamedTypeSymbol type)
		{
			return !type.IsAbstract  
				&& type
					.GetMethods()
					.Safe()
					.Any(m => 
						m.MethodKind == MethodKind.Constructor 
						&& m.IsLocallyPublic(_currentModule)
						&& m.Parameters.Safe().None()
					);
		}

		private INamedTypeSymbol GetBaseType(INamedTypeSymbol type)
		{
			if (type.BaseType != null)
			{
				var ignoredByConfig = IsIgnoredType(type.BaseType);

				// These types are know to not be bindable, so ignore them by default.
				var isKnownBaseType = Equals(type.BaseType, _objectSymbol)
					|| Equals(type.BaseType, _javaObjectSymbol)
					|| Equals(type.BaseType, _nsObjectSymbol);

				if(!ignoredByConfig && !isKnownBaseType)
				{
					return type.BaseType;
				}
				else
				{
					return GetBaseType(type.BaseType);
				}
			}

			return null;
		}

		private bool IsIgnoredType(INamedTypeSymbol typeSymbol)
		{
			return typeSymbol.IsGenericType;
		}

		private bool HasPublicGetter(IPropertySymbol property) => property.GetMethod?.IsLocallyPublic(_currentModule) ?? false;

		private bool IsStringIndexer(IPropertySymbol property)
		{
			return property.IsIndexer
				&& property.GetMethod.IsLocallyPublic(_currentModule)
				&& property.Parameters.Length == 1
				&& property.Parameters.Any(p => Equals(p.Type, _stringSymbol));
		}

		private bool IsNonBindable(IPropertySymbol property) => property.FindAttributeFlattened(_nonBindableSymbol) != null;

		private bool IsOverride(IMethodSymbol methodDefinition)
		{
			return methodDefinition!=null
				&& methodDefinition.IsOverride
				&& !methodDefinition.IsVirtual;
		}

		private string SanitizeTypeName(string name)
		{
			for (int i = 0; i < 10; i++)
			{
				name = name.Replace("`" + i, "");
			}

			name = name.Replace("/", ".");

			return name;
		}

		private void GenerateTypeTable(IndentedStringBuilder writer, IEnumerable<INamedTypeSymbol> types)
		{
			foreach (var type in _typeMap.Where(k => !k.Key.IsGenericType))
			{
				writer.AppendLineInvariant(
					"_bindableTypeCacheByFullName[\"{0}\"] = CreateMemoized(MetadataBuilder_{1:000}.Build);",
					type.Key, 
					type.Value.Index
				);
			}
		}
	}
}
