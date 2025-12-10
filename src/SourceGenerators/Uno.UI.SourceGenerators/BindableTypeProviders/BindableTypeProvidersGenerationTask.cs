#nullable enable

using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.XamlGenerator;
using Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators;
using Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators.CommunityToolkitMvvm;
using Uno.UI.SourceGenerators.Helpers;
using System.Xml;
using System.Threading;

namespace Uno.UI.SourceGenerators.BindableTypeProviders
{
	[Generator]
	public class BindableTypeProvidersSourceGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			new Generator().Generate(context);
		}

		class Generator
		{
			private string? _defaultNamespace;

			private readonly Dictionary<INamedTypeSymbol, GeneratedTypeInfo> _typeMap = new Dictionary<INamedTypeSymbol, GeneratedTypeInfo>(SymbolEqualityComparer.Default);
			private readonly Dictionary<string, (string type, List<string> members)> _substitutions = new Dictionary<string, (string type, List<string> members)>();
			private ITypeSymbol? _dependencyPropertySymbol;
			private INamedTypeSymbol? _dependencyObjectSymbol;
			private INamedTypeSymbol? _javaObjectSymbol;
			private INamedTypeSymbol? _nsObjectSymbol;
			private INamedTypeSymbol? _nonBindableSymbol;
			private INamedTypeSymbol? _resourceDictionarySymbol;
			private IModuleSymbol? _currentModule;
			private string? _projectFullPath;
			private string? _projectDirectory;
			private string? _intermediateOutputPath;
			private string? _intermediatePath;
			private string? _assemblyName;
			private bool _xamlResourcesTrimming;
			private CancellationToken _cancellationToken;
			private IBindableTypeProvider[] _bindableTypeProviders = Array.Empty<IBindableTypeProvider>();

			public string[] AnalyzerSuppressions { get; set; } = Array.Empty<string>();

			internal void Generate(GeneratorExecutionContext context)
			{
				try
				{
					var validPlatform = PlatformHelper.IsValidPlatform(context);
					var isDesignTime = DesignTimeHelper.IsDesignTime(context);
					var isApplication = PlatformHelper.IsApplication(context);

					_ = bool.TryParse(context.GetMSBuildPropertyValue("UnoDisableBindableTypeProvidersGeneration"), out var disableBindableTypeProvidersGeneration);

					if (validPlatform && !isDesignTime && isApplication && !disableBindableTypeProvidersGeneration)
					{
						_cancellationToken = context.CancellationToken;

						_projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
						_projectDirectory = Path.GetDirectoryName(_projectFullPath)
							?? throw new InvalidOperationException($"MSBuild property MSBuildProjectFullPath value {_projectFullPath} is not valid");

						// Defaults to 'false'
						_ = bool.TryParse(context.GetMSBuildPropertyValue("UnoXamlResourcesTrimming"), out _xamlResourcesTrimming);

						_intermediateOutputPath = context.GetMSBuildPropertyValue("IntermediateOutputPath");
						_intermediatePath = Path.Combine(
							_projectDirectory,
							_intermediateOutputPath
						);
						_assemblyName = context.GetMSBuildPropertyValue("AssemblyName");

						_defaultNamespace = context.GetMSBuildPropertyValue("RootNamespace");

						_dependencyPropertySymbol = context.Compilation.GetTypeByMetadataName(XamlConstants.Types.DependencyProperty);
						_dependencyObjectSymbol = context.Compilation.GetTypeByMetadataName(XamlConstants.Types.DependencyObject);

						_javaObjectSymbol = context.Compilation.GetTypeByMetadataName("Java.Lang.Object");
						_nsObjectSymbol = context.Compilation.GetTypeByMetadataName("Foundation.NSObject");
						_nonBindableSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.Data.NonBindableAttribute");
						_resourceDictionarySymbol = context.Compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.ResourceDictionary");
						_currentModule = context.Compilation.SourceModule;

						// Initialize bindable type providers from third-party generators
						_bindableTypeProviders = new IBindableTypeProvider[]
						{
							new MvvmBindableTypeProvider(context.Compilation)
						};

						var modules = from ext in context.Compilation.ExternalReferences
									  let sym = context.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
									  where sym != null
									  from module in sym.Modules
									  select module;

						modules = modules.Concat(context.Compilation.SourceModule);

						context.AddSource("BindableMetadata.g.cs", GenerateTypeProviders(modules));

						GenerateLinkerSubstitutionDefinition();
					}
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					string? message = e.Message + e.StackTrace;

					if (e is AggregateException)
					{
						message = (e as AggregateException)?.InnerExceptions.Select(ex => ex.Message + e.StackTrace).JoinBy("\r\n");
					}

					var diagnostic = Diagnostic.Create(
						XamlCodeGenerationDiagnostics.GenericXamlErrorRule,
						null,
						$"Failed to generate type providers. ({e.Message})");

					context.ReportDiagnostic(diagnostic);
				}
			}

			private string GenerateTypeProviders(IEnumerable<IModuleSymbol> modules)
			{
				var q = from module in modules
						from type in module.GlobalNamespace.GetNamespaceTypes()
						where
							!type.IsGenericType
							&& !type.IsAbstract
							&& IsBindableType(type)
							&& IsValidProvider(type)
						select type;

				q = q.ToArray();

				var writer = new IndentedStringBuilder();

				writer.AppendLineIndented("// <auto-generated>");
				writer.AppendLineIndented("// *****************************************************************************");
				writer.AppendLineIndented("// This file has been generated by Uno.UI (BindableTypeProvidersSourceGenerator)");
				writer.AppendLineIndented("// *****************************************************************************");
				writer.AppendLineIndented("// </auto-generated>");
				writer.AppendLine();
				writer.AppendLineIndented("#pragma warning disable 618  // Ignore obsolete members warnings");
				writer.AppendLineIndented("#pragma warning disable 1591 // Ignore missing XML comment warnings");
				writer.AppendLineIndented("#pragma warning disable XAOBS001 // Ignore obsolete Android members");
				writer.AppendLineIndented("#pragma warning disable Uno0001 // Ignore not implemented members");
				writer.AppendLineIndented("#pragma warning disable Uno0007 // An assembly required for a component is missing");
				AnalyzerSuppressionsGenerator.Generate(writer, AnalyzerSuppressions);

				using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
				{
					GenerateMetadata(writer, q);
					GenerateProviderTable(writer);
				}

				return writer.ToString();
			}

			private bool IsValidProvider(INamedTypeSymbol type)
				=> type.IsLocallyPublic(_currentModule!)

				// Exclude resource dictionaries for linking constraints (XamlControlsResources in particular)
				// Those are not databound, so there's no need to generate providers for them.
				&& !type.Is(_resourceDictionarySymbol);

			private bool IsBindableType(INamedTypeSymbol type)
			{
				// Check if type has [Bindable] attribute (original behavior)
				if (type.GetAllAttributes().Any(a => a.AttributeClass?.Name == "BindableAttribute"))
				{
					return true;
				}

				// Check with third-party bindable type providers
				foreach (var provider in _bindableTypeProviders)
				{
					if (provider.IsBindableType(type))
					{
						return true;
					}
				}

				return false;
			}

			private void GenerateProviderTable(IndentedStringBuilder writer)
			{
				writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]");
				using (writer.BlockInvariant("public class BindableMetadataProvider : global::Uno.UI.DataBinding.IBindableMetadataProvider"))
				{
					GenerateTypeTable(writer);

					writer.AppendLineIndented(@"#if DEBUG && !UNO_DISABLE_KNOWN_MISSING_TYPES");
					writer.AppendLineIndented(@"private global::System.Collections.Generic.HashSet<global::System.Type> _knownMissingTypes = new global::System.Collections.Generic.HashSet<global::System.Type>();");
					writer.AppendLineIndented(@"#endif");
				}
			}

			private class PropertyNameEqualityComparer : IEqualityComparer<IPropertySymbol>
			{
				public bool Equals(IPropertySymbol? x, IPropertySymbol? y)
				{
					return x?.Name == y?.Name;
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

				public int Index { get; }

				public bool HasProperties { get; }
			}

			private void GenerateType(IndentedStringBuilder writer, INamedTypeSymbol ownerType)
			{
				_cancellationToken.ThrowIfCancellationRequested();

				if (_typeMap.ContainsKey(ownerType))
				{
					return;
				}

				var ownerTypeName = GetGlobalQualifier(ownerType) + ownerType.ToString();

				var flattenedProperties =
					from property in ownerType.GetAllProperties()
					where !property.IsStatic
						&& !(property.ContainingSymbol is INamedTypeSymbol containing && IsIgnoredType(containing))
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
						&& SymbolEqualityComparer.Default.Equals(property.Type, _dependencyPropertySymbol)
					select property.Name;

				var fieldDependencyProperties =
					from field in ownerType.GetFields()
					where field.IsStatic
						&& SymbolEqualityComparer.Default.Equals(field.Type, _dependencyPropertySymbol)
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

				writer.AppendLineIndented("/// <summary>");
				writer.AppendLineInvariantIndented("/// Builder for {0}", ownerType.GetFullyQualifiedTypeExcludingGlobal());
				writer.AppendLineIndented("/// </summary>");
				writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]");
				using (writer.BlockInvariant("static class MetadataBuilder_{0:000}", typeInfo.Index))
				{
					var postWriter = new IndentedStringBuilder();
					postWriter.Indent(writer.CurrentLevel);

					// Generate a parameter-less build to avoid generating a lambda during registration (avoids creating a caching backing field)
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1502:AvoidExcessiveComplexity\", Justification=\"Must be ignored even if generated code is checked.\")]");
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1506:AvoidExcessiveClassCoupling\", Justification = \"Must be ignored even if generated code is checked.\")]");
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1505:AvoidUnmaintainableCode\", Justification = \"Must be ignored even if generated code is checked.\")]");
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026\")]");
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2111\")]");
					using (writer.BlockInvariant("internal static global::Uno.UI.DataBinding.IBindableType Build()"))
					{
						writer.AppendLineIndented("return Build(null);");
					}

					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1502:AvoidExcessiveComplexity\", Justification=\"Must be ignored even if generated code is checked.\")]");
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1506:AvoidExcessiveClassCoupling\", Justification = \"Must be ignored even if generated code is checked.\")]");
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.SuppressMessage(\"Microsoft.Maintainability\", \"CA1505:AvoidUnmaintainableCode\", Justification = \"Must be ignored even if generated code is checked.\")]");
					writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2111\", Justification = \"`typeof(Type)` emits IL2111 because of `Type.TypeInitializer`, which is not used.\")]");
					using (writer.BlockInvariant("internal static global::Uno.UI.DataBinding.IBindableType Build(global::Uno.UI.DataBinding.BindableType parent)"))
					{
						RegisterHintMethod($"MetadataBuilder_{typeInfo.Index:000}", ownerType, "Uno.UI.DataBinding.IBindableType Build(Uno.UI.DataBinding.BindableType)");

						writer.AppendLineInvariantIndented(
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

							writer.AppendLineInvariantIndented(@"MetadataBuilder_{0:000}.Build(bindableType); // {1}", baseTypeMapped.Index, ExpandType(baseType));
						}

						if (IsCreateable(ownerType))
						{
							using (writer.BlockInvariant("if(parent == null)"))
							{
								writer.AppendLineIndented(@"bindableType.AddActivator(CreateInstance);");
								postWriter.AppendLineIndented($@"private static object CreateInstance() => new {ownerTypeName}();");

								RegisterHintMethod($"MetadataBuilder_{typeInfo.Index:000}", ownerType, "System.Object CreateInstance()");
							}
						}

						foreach (var property in properties)
						{
							var propertyTypeName = property.Type.GetFullyQualifiedTypeIncludingGlobal();
							var propertyName = property.Name;

							if (IsStringIndexer(property))
							{
								writer.AppendLineIndented("bindableType.AddIndexer(GetIndexer, SetIndexer);");

								postWriter.AppendLineIndented($@"private static object GetIndexer(object instance, string name) => (({ownerTypeName})instance)[name];");
								RegisterHintMethod($"MetadataBuilder_{typeInfo.Index:000}", ownerType, "System.Object GetIndexer(System.Object, System.String)");

								if (property.SetMethod != null)
								{
									postWriter.AppendLineIndented($@"private static void SetIndexer(object instance, string name, object value) => (({ownerTypeName})instance)[name] = ({propertyTypeName})value;");
									RegisterHintMethod($"MetadataBuilder_{typeInfo.Index:000}", ownerType, "System.Void SetIndexer(System.Object,System.String,System.Object)");
								}
								else
								{
									postWriter.AppendLineIndented("private static void SetIndexer(object instance, string name, object value) {}");
								}

								continue;
							}

							if (property.IsIndexer)
							{
								// Other types of indexers are currently not supported.
								continue;
							}

							// For value types (structs), we cannot generate setters because modifying an unboxed value type
							// creates a temporary copy that is immediately discarded. Only generate getters for structs.
							var isValueType = ownerType.TypeKind == TypeKind.Struct;

							if (
								property.SetMethod != null
								&& !property.SetMethod.IsInitOnly
								&& property.SetMethod.IsLocallyPublic(_currentModule!)
								&& !isValueType
								)
							{
								writer.AppendLineIndented($@"bindableType.AddProperty(""{propertyName}"", typeof({propertyTypeName}), Get{propertyName}, Set{propertyName});");
								postWriter.AppendLineIndented($@"private static object Get{propertyName}(object instance, global::Microsoft.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => (({ownerTypeName})instance).{propertyName};");

								if (property.Type.IsValueType && property.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
								{
									using (postWriter.BlockInvariant($@"private static void Set{propertyName}(object instance, object value, global::Microsoft.UI.Xaml.DependencyPropertyValuePrecedences? precedence)"))
									{
										using (postWriter.BlockInvariant($"if(value != null)"))
										{
											postWriter.AppendLineIndented($"(({ownerTypeName})instance).{propertyName} = ({propertyTypeName})value;");
										}
									}
								}
								else
								{
									postWriter.AppendLineIndented($@"private static void Set{propertyName}(object instance, object value, global::Microsoft.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => (({ownerTypeName})instance).{propertyName} = ({propertyTypeName})value;");
								}

								RegisterHintMethod($"MetadataBuilder_{typeInfo.Index:000}", ownerType, $"System.Object Get{propertyName}(System.Object,System.Nullable`1<Microsoft.UI.Xaml.DependencyPropertyValuePrecedences>)");
								RegisterHintMethod($"MetadataBuilder_{typeInfo.Index:000}", ownerType, $"System.Void Set{propertyName}(System.Object,System.Object,System.Nullable`1<Microsoft.UI.Xaml.DependencyPropertyValuePrecedences>)");

							}
							else if (HasPublicGetter(property))
							{
								writer.AppendLineIndented($@"bindableType.AddProperty(""{propertyName}"", typeof({propertyTypeName}), Get{propertyName});");

								postWriter.AppendLineIndented($@"private static object Get{propertyName}(object instance, global::Microsoft.UI.Xaml.DependencyPropertyValuePrecedences? precedence) => (({ownerTypeName})instance).{propertyName};");

								RegisterHintMethod($"MetadataBuilder_{typeInfo.Index:000}", ownerType, $"System.Object Get{propertyName}(System.Object,System.Nullable`1<Microsoft.UI.Xaml.DependencyPropertyValuePrecedences>)");
							}
						}

						foreach (var dependencyProperty in dependencyProperties)
						{
							var propertyName = dependencyProperty.TrimEnd("Property");

							var getMethod = ownerType.GetMethodsWithName("Get" + propertyName).FirstOrDefault(m => m.Parameters.Length == 1 && m.IsLocallyPublic(_currentModule!));

							if (getMethod == null)
							{
								getMethod = ownerType
									.GetProperties()
									.FirstOrDefault(p => p.Name == propertyName && (p.GetMethod?.IsLocallyPublic(_currentModule!) ?? false))
									?.GetMethod;
							}

							if (getMethod != null)
							{
								writer.AppendLineIndented($@"bindableType.AddProperty({ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{dependencyProperty});");
							}
						}

						writer.AppendLineIndented(@"return bindableType;");
					}

					writer.Append(postWriter.ToString());
				}

				writer.AppendLine();
			}

			private void RegisterHintMethod(string type, INamedTypeSymbol targetType, string signature)
			{
				type = _defaultNamespace + "." + type;

				if (!_substitutions.TryGetValue(type, out var hint))
				{
					_substitutions[type] = hint = (LinkerHintsHelpers.GetPropertyAvailableName(targetType.ToDisplayString()), new List<string>());
				}

				hint.members.Add(signature);
			}

			private static string ExpandType(INamedTypeSymbol ownerType)
			{
				if (ownerType.TypeKind == TypeKind.Error)
				{
					return ownerType.ToString() + "/* Type is error */";
				}
				else
				{
					return ownerType.GetFullyQualifiedTypeIncludingGlobal();
				}
			}

			private static string GetGlobalQualifier(ITypeSymbol ownerType)
			{
				if (ownerType is IArrayTypeSymbol arrayType)
				{
					return GetGlobalQualifier(arrayType.ElementType);
				}

				if (ownerType.IsNullable(out var nullType))
				{
					return GetGlobalQualifier(nullType!);
				}

				var needsGlobal = ownerType.SpecialType == SpecialType.None && !ownerType.IsTupleType;
				return (needsGlobal ? "global::" : "");
			}

			private bool IsCreateable(INamedTypeSymbol type)
			{
				return !type.IsAbstract
					&& type.InstanceConstructors.Any(m => m.Parameters.Length == 0 && m.IsLocallyPublic(_currentModule!));
			}

			private INamedTypeSymbol? GetBaseType(INamedTypeSymbol type)
			{
				if (type.BaseType != null)
				{
					var ignoredByConfig = IsIgnoredType(type.BaseType);

					// These types are know to not be bindable, so ignore them by default.
					var isKnownBaseType = type.BaseType.SpecialType == SpecialType.System_Object
						|| SymbolEqualityComparer.Default.Equals(type.BaseType, _javaObjectSymbol)
						|| SymbolEqualityComparer.Default.Equals(type.BaseType, _nsObjectSymbol);

					if (!ignoredByConfig && !isKnownBaseType)
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

			private bool HasPublicGetter(IPropertySymbol property) => property.GetMethod?.IsLocallyPublic(_currentModule!) ?? false;

			private bool IsStringIndexer(IPropertySymbol property)
			{
				return property.IsIndexer
					&& property.GetMethod!.IsLocallyPublic(_currentModule!)
					&& property.Parameters.Length == 1
					&& property.Parameters.Any(p => p.Type.SpecialType == SpecialType.System_String);
			}

			private bool IsNonBindable(IPropertySymbol property) => property.FindAttributeFlattened(_nonBindableSymbol!) != null;

			private bool IsOverride(IMethodSymbol? methodDefinition)
			{
				return methodDefinition != null
					&& methodDefinition.IsOverride
					&& !methodDefinition.IsVirtual;
			}

			private void GenerateTypeTable(IndentedStringBuilder writer)
			{
				var types = _typeMap.Where(k => !k.Key.IsGenericType).ToArray();

				if (types.Length < 1000)
				{
					// Generate a smaller table to avoid tiering issue
					// with large methods, see https://github.com/dotnet/runtime/issues/93192.
					// This number is arbitrary, based on observations of generated size of
					// switch/case static lookup.
					// As of 2023-10-08, the performance of type reflection enumeration is still 10x
					// slower than using a pre-built dictionaries, on WebAssembly.
					GenerateTypeTableSwitch(writer, types);
				}
				else
				{
					GenerateTypeTableDictionary(writer, types);
				}
			}

			private void GenerateTypeTableDictionary(IndentedStringBuilder writer, KeyValuePair<INamedTypeSymbol, GeneratedTypeInfo>[] types)
			{
				writer.AppendLineIndented("private delegate global::Uno.UI.DataBinding.IBindableType TypeBuilderDelegate();");
				writer.AppendLineIndented(@$"static global::System.Collections.Hashtable _bindableTypeCacheByFullName = new global::System.Collections.Hashtable({types.Length});");

				using (writer.BlockInvariant("public global::Uno.UI.DataBinding.IBindableType GetBindableTypeByFullName(string fullName)"))
				{
					writer.AppendLineIndented(@"var instance = _bindableTypeCacheByFullName[fullName];");
					writer.AppendLineIndented(@"var builder = instance as TypeBuilderDelegate;");

					using (writer.BlockInvariant(@"if(builder != null)"))
					{
						writer.AppendLineIndented(@"_bindableTypeCacheByFullName[fullName] = instance = builder();");
					}

					writer.AppendLineIndented(@"return instance as global::Uno.UI.DataBinding.IBindableType;");
				}

				using (writer.BlockInvariant("public global::Uno.UI.DataBinding.IBindableType GetBindableTypeByType(global::System.Type type)"))
				{
					writer.AppendLineIndented(@"var bindableType = GetBindableTypeByFullName(type.FullName);");

					writer.AppendLineIndented(@"#if DEBUG");
					using (writer.BlockInvariant(@"if(bindableType == null)"))
					{
						using (writer.BlockInvariant(@"lock(_knownMissingTypes)"))
						{
							using (writer.BlockInvariant(@"if(!_knownMissingTypes.Contains(type) && !type.IsGenericType && !type.IsAbstract)"))
							{
								writer.AppendLineIndented(@"_knownMissingTypes.Add(type);");
								writer.AppendLineIndented(@"global::System.Diagnostics.Debug.WriteLine($""The Bindable attribute is missing and the type [{{type.FullName}}] is not known by the MetadataProvider. Reflection was used instead of the binding engine and generated static metadata. Add the Bindable attribute to prevent this message and performance issues."");");
							}
						}
					}
					writer.AppendLineIndented(@"#endif");

					writer.AppendLineIndented(@"return bindableType;");
				}

				using (writer.BlockInvariant("static BindableMetadataProvider()"))
				{
					var linkerHintsClassName = LinkerHintsHelpers.GetLinkerHintsClassName(_defaultNamespace);
					var bindableMetadataProviderCondition = _xamlResourcesTrimming
						? linkerHintsClassName + "." + LinkerHintsHelpers.GetPropertyAvailableName($"{_defaultNamespace}.BindableMetadataProvider")
						: "true";

					using (writer.BlockInvariant($"if ({bindableMetadataProviderCondition})"))
					{
						foreach (var type in _typeMap.Where(k => !k.Key.IsGenericType && !k.Key.IsAbstract))
						{
							writer.AppendLineIndented($"RegisterBuilder{type.Value.Index:000}();");
						}
					}
				}

				// Generate small methods to avoid JIT or interpreter costs associated
				// with large methods.
				foreach (var type in _typeMap.Where(k => !k.Key.IsGenericType && !k.Key.IsAbstract))
				{
					using (writer.BlockInvariant($"static void RegisterBuilder{type.Value.Index:000}()"))
					{
						if (_xamlResourcesTrimming && type.Key.GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, _dependencyObjectSymbol)))
						{
							var linkerHintsClassName = LinkerHintsHelpers.GetLinkerHintsClassName(_defaultNamespace);
							var safeTypeName = LinkerHintsHelpers.GetPropertyAvailableName(type.Key.GetFullMetadataName());

							writer.AppendLineIndented($"if(global::{linkerHintsClassName}.{safeTypeName})");
						}

						writer.AppendLineIndented(
							$"_bindableTypeCacheByFullName[\"{type.Key}\"] = new TypeBuilderDelegate(MetadataBuilder_{type.Value.Index:000}.Build);");
					}
				}
			}

			private void GenerateTypeTableSwitch(IndentedStringBuilder writer, KeyValuePair<INamedTypeSymbol, GeneratedTypeInfo>[] types)
			{
				writer.AppendLineIndented($"private readonly global::Uno.UI.DataBinding.IBindableType[] _bindableTypes = new global::Uno.UI.DataBinding.IBindableType[{types.Length}];");
				writer.AppendLineIndented($"private static global::Uno.UI.DataBinding.IBindableType _null;");

				using (writer.BlockInvariant("public global::Uno.UI.DataBinding.IBindableType GetBindableTypeByFullName(string fullName)"))
				{
					writer.AppendLineIndented("ref global::Uno.UI.DataBinding.IBindableType element = ref _null;");

					var linkerHintsClassName = LinkerHintsHelpers.GetLinkerHintsClassName(_defaultNamespace);
					var bindableMetadataProviderCondition = _xamlResourcesTrimming
						? linkerHintsClassName + "." + LinkerHintsHelpers.GetPropertyAvailableName($"{_defaultNamespace}.BindableMetadataProvider")
						: "true";

					using (writer.BlockInvariant($"if ({bindableMetadataProviderCondition})"))
					{
						using (writer.BlockInvariant(@"switch(fullName)"))
						{
							foreach (var type in types)
							{
								_cancellationToken.ThrowIfCancellationRequested();

								var typeIndex = type.Value.Index;

								using (writer.BlockInvariant($"case \"{type.Key}\":"))
								{
									writer.AppendLineIndented($"element = ref _bindableTypes[{typeIndex}];");
									using (writer.BlockInvariant("if(element == null)"))
									{
										if (_xamlResourcesTrimming && type.Key.GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, _dependencyObjectSymbol)))
										{
											var safeTypeName = LinkerHintsHelpers.GetPropertyAvailableName(type.Key.GetFullMetadataName());

											writer.AppendLineIndented($"if(global::{linkerHintsClassName}.{safeTypeName})");
										}

										writer.AppendLineIndented($"element = MetadataBuilder_{typeIndex:000}.Build();");
									}

									writer.AppendLineIndented("break;");
								}
							}
						}
					}
					writer.AppendLineIndented("return element;");
				}

				using (writer.BlockInvariant("public global::Uno.UI.DataBinding.IBindableType GetBindableTypeByType(global::System.Type type)"))
				{
					writer.AppendLineIndented(@"var bindableType = GetBindableTypeByFullName(type.FullName);");

					writer.AppendLineIndented(@"#if DEBUG && !UNO_DISABLE_KNOWN_MISSING_TYPES");
					using (writer.BlockInvariant(@"if(bindableType == null && !type.IsGenericType && !type.IsAbstract)"))
					{
						using (writer.BlockInvariant(@"lock(_knownMissingTypes)"))
						using (writer.BlockInvariant(@"if (_knownMissingTypes.Add(type))"))
						{
							writer.AppendLineIndented(@"global::System.Diagnostics.Debug.WriteLine($""The Bindable attribute is missing and the type [{type.FullName}] is not known by the MetadataProvider. Reflection was used instead of the binding engine and generated static metadata. Add the Bindable attribute to prevent this message and performance issues."");");
						}
					}
					writer.AppendLineIndented(@"#endif");

					writer.AppendLineIndented(@"return bindableType;");
				}

				using (writer.BlockInvariant("public BindableMetadataProvider()"))
				{
				}
			}

			private void GenerateLinkerSubstitutionDefinition()
			{
				if (!_xamlResourcesTrimming)
				{
					return;
				}

				// <linker>
				//   <assembly fullname="Uno.UI">
				// 	<type fullname="Uno.UI.GlobalStaticResources">
				// 	  <method signature="System.Void Initialize()" body="remove" />
				// 	  <method signature="System.Void RegisterDefaultStyles()" body="remove" />
				// 	</type>
				//   </assembly>
				// </linker>
				var doc = new XmlDocument();

				var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);

				var root = doc.DocumentElement;
				doc.InsertBefore(xmlDeclaration, root);

				var linkerNode = doc.CreateElement(string.Empty, "linker", string.Empty);
				doc.AppendChild(linkerNode);

				var assemblyNode = doc.CreateElement(string.Empty, "assembly", string.Empty);
				assemblyNode.SetAttribute("fullname", _assemblyName);
				linkerNode.AppendChild(assemblyNode);


				foreach (var substitution in _substitutions)
				{
					var typeNode = doc.CreateElement(string.Empty, "type", string.Empty);
					typeNode.SetAttribute("fullname", substitution.Key);
					typeNode.SetAttribute("feature", substitution.Value.type);
					typeNode.SetAttribute("featurevalue", "false");
					assemblyNode.AppendChild(typeNode);

					foreach (var method in substitution.Value.members)
					{
						var methodNode = doc.CreateElement(string.Empty, "method", string.Empty);
						methodNode.SetAttribute("signature", method);
						methodNode.SetAttribute("body", "remove");
						typeNode.AppendChild(methodNode);
					}
				}

				var fileName = Path.Combine(_intermediatePath, "Substitutions", "BindableMetadata.Substitutions.xml");
				Directory.CreateDirectory(Path.GetDirectoryName(fileName));

				doc.Save(fileName);
			}

		}
	}
}
