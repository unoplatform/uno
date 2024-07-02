#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Uno.Disposables;
using Uno.Extensions;
using Uno.MsBuildTasks.Utils;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.BindableTypeProviders;
using Uno.UI.SourceGenerators.Helpers;
using Uno.UI.SourceGenerators.Utils;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;
using Uno.UI.Xaml;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlFileGenerator
	{
		private const string GlobalPrefix = "global::";
		private const string QualifiedNamespaceMarker = ".";

		private static readonly char[] _dotArray = new[] { '.' };
		private static readonly char[] _parenthesesArray = new[] { '(', ')' };

		private static readonly Dictionary<string, string[]> _knownNamespaces = new Dictionary<string, string[]>
		{
			{
				XamlConstants.PresentationXamlXmlNamespace,
				XamlConstants.Namespaces.PresentationNamespaces
			},
			{
				XamlConstants.XamlXmlNamespace,
				new [] {
					"System",
				}
			},
		};

		private static readonly string[] FrameworkTemplateTypes = new[] { "DataTemplate", "ControlTemplate", "ItemsPanelTemplate" };

		private readonly Dictionary<string, XamlObjectDefinition> _namedResources = new Dictionary<string, XamlObjectDefinition>();
		private readonly List<string> _partials = new List<string>();

		/// <summary>
		/// Names of disambiguated keys associated with resource definitions. These are created for top-level ResourceDictionary declarations only.
		/// </summary>
		private readonly Dictionary<(string? Theme, string ResourceKey), string> _topLevelQualifiedKeys = new Dictionary<(string?, string), string>();
		private readonly Stack<NameScope> _scopeStack = new Stack<NameScope>();
		private readonly Stack<XLoadScope> _xLoadScopeStack = new Stack<XLoadScope>();
		private int _resourceOwner;
		private readonly XamlFileDefinition _fileDefinition;
		private readonly string _defaultNamespace;
		private readonly RoslynMetadataHelper _metadataHelper;
		private readonly string _fileUniqueId;
		private readonly string[] _analyzerSuppressions;
		private readonly ResourceDetailsCollection _resourceDetailsCollection;
		private int _applyIndex;
		private int _collectionIndex;
		private int _subclassIndex;
		private int _dictionaryPropertyIndex;
		private int _xBindCounter;
		private string? _themeDictionaryCurrentlyBuilding;
		private readonly XamlGlobalStaticResourcesMap _globalStaticResourcesMap;
		private readonly bool _isUiAutomationMappingEnabled;
		private readonly Dictionary<string, string[]> _uiAutomationMappings;
		private readonly string _defaultLanguage;
		private readonly bool _isHotReloadEnabled;
		private readonly bool _isInsideMainAssembly;
		private readonly bool _isDesignTimeBuild;
		private readonly string _relativePath;

		/// <summary>
		/// x:Name cache for the lookups performed in the document.
		/// </summary>
		private Dictionary<string, List<XamlObjectDefinition>> _nameCache = new();

		/// <summary>
		/// True if the file currently being parsed contains a top-level ResourceDictionary definition.
		/// </summary>
		private bool _isTopLevelDictionary;
		private readonly bool _isUnoAssembly;
		private readonly bool _isUnoFluentAssembly;

		/// <summary>
		/// True if VisualStateManager children can be set lazily
		/// </summary>
		private readonly bool _isLazyVisualStateManagerEnabled;

		private readonly bool _enableFuzzyMatching;

		/// <summary>
		/// True if XAML resource trimming is enabled
		/// </summary>
		private readonly bool _xamlResourcesTrimming;

		/// <summary>
		/// True if the generator is currently creating child subclasses (for templates, etc)
		/// </summary>
		private bool _isInChildSubclass;
		/// <summary>
		/// True if the generator is currently creating the inner singleton class associated with a top-level resource dictionary
		/// </summary>
		private bool _isInSingletonInstance;

		private Stack<INamedTypeSymbol?> _currentStyleTargetTypeStack = new();

		private INamedTypeSymbol? CurrentStyleTargetType
			=> _currentStyleTargetTypeStack.Count > 0 ? _currentStyleTargetTypeStack.Peek() : null;

		/// <summary>
		/// Context to report diagnostics to
		/// </summary>
		private readonly GeneratorExecutionContext _generatorContext;

		/// <summary>
		/// The current DefaultBindMode for x:Bind bindings, as set by app code for the current Xaml subtree.
		/// </summary>
		private readonly Stack<string> _currentDefaultBindMode = new Stack<string>(new[] { "OneTime" });

		private readonly IDictionary<INamedTypeSymbol, XamlType> _xamlTypeToXamlTypeBaseMap;

		private readonly string[] _includeXamlNamespaces;

		private readonly bool _disableBindableTypeProvidersGeneration;

		/// <summary>
		/// Information about types used in .Apply() scenarios
		/// </summary>
		/// <remarks>
		/// The key of the dictionary is the globalized fully qualified name.
		/// </remarks>
		private readonly Dictionary<string, int> _xamlAppliedTypes = new();

		private readonly bool _isWasm;

		/// <summary>
		/// If set, code generated from XAML will be annotated with the source method and line # in this file, for easier debugging.
		/// </summary>
		private readonly bool _shouldAnnotateGeneratedXaml;
		private static readonly Regex splitRegex = new Regex(@"\s*,\s*|\s+");

		private string ParseContextPropertyAccess =>
			 (_isTopLevelDictionary, _isInSingletonInstance) switch
			 {
				 (true, false) => "{0}{1}.GlobalStaticResources.{2}.{3}()".InvariantCultureFormat(
					GlobalPrefix,
					_defaultNamespace,
					SingletonClassName,
					XamlCodeGeneration.ParseContextGetterMethod
				),
				 (true, true) => "this.{0}".InvariantCultureFormat(XamlCodeGeneration.ParseContextPropertyName),
				 _ => "{0}{1}.GlobalStaticResources.{2}".InvariantCultureFormat(
					GlobalPrefix,
					_defaultNamespace,
					XamlCodeGeneration.ParseContextPropertyName
				)
			 };

		private string SingletonInstanceAccess => _isInSingletonInstance ?
			"this" :
			"{0}{1}.GlobalStaticResources.{2}.Instance".InvariantCultureFormat(
					GlobalPrefix,
					_defaultNamespace,
					SingletonClassName
				);

		/// <summary>
		/// Name to use for inner singleton containing top-level ResourceDictionary properties
		/// </summary>
		private string SingletonClassName => $"ResourceDictionarySingleton__{_fileDefinition.UniqueID}";

		private const string DictionaryProviderInterfaceName = "global::Uno.UI.IXamlResourceDictionaryProvider";

		public XamlCodeGeneration Generation { get; }

		public XamlFileGenerator(
			XamlCodeGeneration generation,
			XamlFileDefinition file,
			string targetPath,
			string defaultNamespace,
			RoslynMetadataHelper metadataHelper,
			string fileUniqueId,
			string[] analyzerSuppressions,
			ResourceDetailsCollection resourceDetailsCollection,
			XamlGlobalStaticResourcesMap globalStaticResourcesMap,
			bool isUiAutomationMappingEnabled,
			Dictionary<string, string[]> uiAutomationMappings,
			string defaultLanguage,
			bool shouldWriteErrorOnInvalidXaml,
			bool isWasm,
			bool isHotReloadEnabled,
			bool isDesignTimeBuild,
			bool isInsideMainAssembly,
			bool shouldAnnotateGeneratedXaml,
			bool isUnoAssembly,
			bool isUnoFluentAssembly,
			bool isLazyVisualStateManagerEnabled,
			bool enableFuzzyMatching,
			bool disableBindableTypeProvidersGeneration,
			GeneratorExecutionContext generatorContext,
			bool xamlResourcesTrimming,
			IDictionary<INamedTypeSymbol, XamlType> xamlTypeToXamlTypeBaseMap,
			string[] includeXamlNamespaces)
		{
			Generation = generation;
			_fileDefinition = file;
			_defaultNamespace = defaultNamespace;
			_metadataHelper = metadataHelper;
			_fileUniqueId = fileUniqueId;
			_analyzerSuppressions = analyzerSuppressions;
			_resourceDetailsCollection = resourceDetailsCollection;
			_globalStaticResourcesMap = globalStaticResourcesMap;
			_isUiAutomationMappingEnabled = isUiAutomationMappingEnabled;
			_uiAutomationMappings = uiAutomationMappings;
			_defaultLanguage = defaultLanguage.IsNullOrEmpty() ? "en-US" : defaultLanguage;
			_isHotReloadEnabled = isHotReloadEnabled;
			_isInsideMainAssembly = isInsideMainAssembly;
			_isDesignTimeBuild = isDesignTimeBuild;
			_shouldAnnotateGeneratedXaml = shouldAnnotateGeneratedXaml;
			_isLazyVisualStateManagerEnabled = isLazyVisualStateManagerEnabled;
			_enableFuzzyMatching = enableFuzzyMatching;
			_generatorContext = generatorContext;
			_xamlResourcesTrimming = xamlResourcesTrimming;
			_xamlTypeToXamlTypeBaseMap = xamlTypeToXamlTypeBaseMap;
			_includeXamlNamespaces = includeXamlNamespaces;
			_disableBindableTypeProvidersGeneration = disableBindableTypeProvidersGeneration;

			InitCaches();

			_relativePath = PathHelper.GetRelativePath(targetPath, _fileDefinition.FilePath);
			ShouldWriteErrorOnInvalidXaml = shouldWriteErrorOnInvalidXaml;

			_isWasm = isWasm;

			_isUnoAssembly = isUnoAssembly;
			_isUnoFluentAssembly = isUnoFluentAssembly;
		}

		/// <summary>
		/// Indicates if the code generation should write #error in the generated code (and break at compile time) or write a // Warning, which would be silent.
		/// </summary>
		/// <remarks>Initial behavior is to write // Warning, hence the default value to false, but we suggest setting this to true.</remarks>
		public bool ShouldWriteErrorOnInvalidXaml { get; }

		public SourceText GenerateFile()
		{
#if DEBUG
			Console.WriteLine("Processing file {0} (cs: {1})".InvariantCultureFormat(_fileDefinition.FilePath, _fileDefinition.Checksum));
#endif

			try
			{
				return InnerGenerateFile();
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new Exception($"Processing failed for file {_fileDefinition.FilePath} ({e})", e);
			}
		}

		private void TryGenerateWarningForInconsistentBaseType(IndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			EnsureXClassName();

			var xamlDefinedBaseType = GetType(topLevelControl.Type);
			var classDefinedBaseType = _xClassName.Symbol?.BaseType;

			if (!SymbolEqualityComparer.Default.Equals(xamlDefinedBaseType, classDefinedBaseType))
			{
				var locations = _xClassName.Symbol?.Locations;
				var fullClassName = _xClassName.Namespace + "." + _xClassName.ClassName;
				if (locations != null && locations.Value.Length > 0)
				{
					var diagnostic = Diagnostic.Create(XamlCodeGenerationDiagnostics.GenericXamlWarningRule,
													   locations.Value[0],
													   $"{fullClassName} does not explicitly define the {xamlDefinedBaseType} base type in code behind.");
					_generatorContext.ReportDiagnostic(diagnostic);
				}
				else
				{
					writer.AppendLineIndented($"#warning {fullClassName} does not explicitly define the {xamlDefinedBaseType} base type in code behind.");
				}
			}
		}

		private SourceText InnerGenerateFile()
		{
			var writer = new IndentedStringBuilder();

			writer.AppendLineIndented("// <autogenerated />");

			AnalyzerSuppressionsGenerator.GenerateCSharpPragmaSupressions(writer, _analyzerSuppressions);

			writer.AppendLineIndented("#pragma warning disable CS0114");
			writer.AppendLineIndented("#pragma warning disable CS0108");
			writer.AppendLineIndented("using System;");
			writer.AppendLineIndented("using System.Collections.Generic;");
			writer.AppendLineIndented("using System.Diagnostics;");
			writer.AppendLineIndented("using System.Linq;");
			writer.AppendLineIndented("using Uno.UI;");
			writer.AppendLineIndented("using Uno.UI.Xaml;");

			//TODO Determine the list of namespaces to use
			foreach (var ns in XamlConstants.Namespaces.All)
			{
				writer.AppendLineIndented($"using {ns};");
			}

			writer.AppendLineIndented("using Uno.Extensions;");
			writer.AppendLineIndented("using Uno;");
			writer.AppendLineIndented("using Uno.UI.Helpers;");
			writer.AppendLineIndented("using Uno.UI.Helpers.Xaml;");

			writer.AppendLineInvariantIndented("using {0};", _defaultNamespace);

			// For Subclass build functionality
			writer.AppendLineIndented("");
			writer.AppendLineIndented("#if __ANDROID__");
			writer.AppendLineIndented("using _View = Android.Views.View;");
			writer.AppendLineIndented("#elif __IOS__");
			writer.AppendLineIndented("using _View = UIKit.UIView;");
			writer.AppendLineIndented("#elif __MACOS__");
			writer.AppendLineIndented("using _View = AppKit.NSView;");
			writer.AppendLineIndented("#else");
			writer.AppendLineIndented("using _View = Windows.UI.Xaml.UIElement;");
			writer.AppendLineIndented("#endif");

			writer.AppendLineIndented("");

			var topLevelControl = _fileDefinition.Objects.First();

			BuildNameCache(topLevelControl);

			if (topLevelControl.Type.Name == "ResourceDictionary")
			{
				_isTopLevelDictionary = true;
				_xClassName = FindClassName(topLevelControl);

				using (TrySetDefaultBindMode(topLevelControl))
				{
					BuildResourceDictionaryBackingClass(writer, topLevelControl);
					BuildTopLevelResourceDictionary(writer, topLevelControl);
				}
			}
			else
			{
				_xClassName = GetClassName(topLevelControl);

				using (writer.BlockInvariant("namespace {0}", _xClassName.Namespace))
				{
					AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);
					TryGenerateWarningForInconsistentBaseType(writer, topLevelControl);

					var controlBaseType = GetType(topLevelControl.Type);

					WriteMetadataNewTypeAttribute(writer);

					using (writer.BlockInvariant("partial class {0} : {1}", _xClassName.ClassName, controlBaseType.GetFullyQualifiedTypeIncludingGlobal()))
					{
						if (_isHotReloadEnabled)
						{
							// Create a public member to avoid having to remove all unused member warnings
							// The member is a method to avoid this error: error ENC0011: Updating the initializer of const field requires restarting the application.
							writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
							writer.AppendLineIndented($"internal string __checksum() => \"{_fileDefinition.Checksum}\";");
						}

						BuildBaseUri(writer);

						using (Scope(_xClassName.Namespace, _xClassName.ClassName))
						{
							BuildInitializeComponent(writer, topLevelControl, controlBaseType);
							if (IsApplication(controlBaseType) && PlatformHelper.IsAndroid(_generatorContext))
							{
								BuildDrawableResourcesIdResolver(writer);
							}

							TryBuildElementStubHolders(writer);

							BuildPartials(writer);

							BuildBackingFields(writer);

							BuildChildSubclasses(writer);

							BuildComponentFields(writer);

							BuildCompiledBindings(writer);

							BuildXBindTryGetDeclarations(writer);
						}
					}
				}
			}

			BuildXamlApplyBlocks(writer);

			// var formattedCode = ReformatCode(writer.ToString());
			return new StringBuilderBasedSourceText(writer.Builder);
		}

		/// <summary>
		/// Builds the BaseUri strings constants to be set to all FrameworkElement instances
		/// </summary>
		private void BuildBaseUri(IIndentedStringBuilder writer)
		{
			var assembly = _isInsideMainAssembly ? "" : _generatorContext.Compilation.AssemblyName + "/";

			// Note that the assembly name is lower-cased in order for file resolution on case-sensitive file systems to work.
			writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
			writer.AppendLineInvariantIndented($"private const string __baseUri_prefix_{_fileUniqueId} = \"ms-appx:///{assembly}\";");
			writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
			writer.AppendLineInvariantIndented($"private const string __baseUri_{_fileUniqueId} = \"ms-appx:///{assembly}{_fileDefinition.TargetFilePath.TrimStart("/")}\";");
		}

		private void BuildInitializeComponent(IndentedStringBuilder writer, XamlObjectDefinition topLevelControl, INamedTypeSymbol controlBaseType)
		{
			writer.AppendLineIndented("private global::Windows.UI.Xaml.NameScope __nameScope = new global::Windows.UI.Xaml.NameScope();");

			using (writer.BlockInvariant($"private void InitializeComponent()"))
			{
				if (IsApplication(controlBaseType))
				{
					BuildApplicationInitializerBody(writer, topLevelControl);
				}
				else
				{
					if (_isHotReloadEnabled)
					{
						// Insert hot reload support
						writer.AppendLineIndented($"var __resourceLocator = new global::System.Uri(\"file:///{_fileDefinition.FilePath.Replace("\\", "/")}\");");

						using (writer.BlockInvariant($"if(global::Uno.UI.ApplicationHelper.IsLoadableComponent(__resourceLocator))"))
						{
							writer.AppendLineIndented($"global::Windows.UI.Xaml.Application.LoadComponent(this, __resourceLocator);");
							writer.AppendLineIndented($"return;");
						}
					}

					BuildGenericControlInitializerBody(writer, topLevelControl);
					BuildNamedResources(writer, _namedResources);
				}

				EnsureXClassName();
				BuildCompiledBindingsInitializer(writer, controlBaseType);
			}
		}

		private void BuildXamlApplyBlocks(IndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);

			if (!_isHotReloadEnabled)
			{
				using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
				{
					WriteMetadataNewTypeAttribute(writer);

					using (writer.BlockInvariant("static class {0}XamlApplyExtensions", _fileUniqueId))
					{
						foreach (var typeInfo in _xamlAppliedTypes)
						{
							writer.AppendLineIndented($"public delegate void XamlApplyHandler{typeInfo.Value}({typeInfo.Key} instance);");

							writer.AppendLineIndented($"[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
							using (writer.BlockInvariant(
								$"public static {typeInfo.Key} {_fileUniqueId}_XamlApply(this {typeInfo.Key} instance, XamlApplyHandler{typeInfo.Value} handler)"
							))
							{
								writer.AppendLineIndented($"handler(instance);");
								writer.AppendLineIndented($"return instance;");
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Processes the 'App.xaml' file.
		/// </summary>
		private void BuildApplicationInitializerBody(IndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			writer.AppendLineIndented($"var __that = this;");

			TryAnnotateWithGeneratorSource(writer);
			InitializeRemoteControlClient(writer);
			GenerateApiExtensionRegistrations(writer);

			GenerateResourceLoader(writer);
			writer.AppendLine();
			ApplyLiteralProperties();
			writer.AppendLine();

			writer.AppendLineIndented($"global::{_defaultNamespace}.GlobalStaticResources.Initialize();");
			writer.AppendLineIndented($"global::{_defaultNamespace}.GlobalStaticResources.RegisterResourceDictionariesBySourceLocal();");

			if (!_isDesignTimeBuild && !_disableBindableTypeProvidersGeneration)
			{
				writer.AppendLineIndented($"global::Uno.UI.DataBinding.BindableMetadata.Provider = new global::{_defaultNamespace}.BindableMetadataProvider();");
			}

			writer.AppendLineIndented($"#if __ANDROID__");
			writer.AppendLineIndented($"global::Uno.Helpers.DrawableHelper.SetDrawableResolver(global::{_xClassName?.Namespace}.{_xClassName?.ClassName}.DrawableResourcesIdResolver.Resolve);");
			writer.AppendLineIndented($"#endif");

			if (_isWasm)
			{
				writer.AppendLineIndented($"// Workaround for https://github.com/dotnet/runtime/issues/44269");
				writer.AppendLineIndented($"typeof(global::Uno.UI.Runtime.WebAssembly.HtmlElementAttribute).GetHashCode();");
			}

			RegisterAndBuildResources(writer, topLevelControl, isInInitializer: false);
			BuildProperties(writer, topLevelControl, isInline: false);

			ApplyFontsOverride(writer);

			if (_isUiAutomationMappingEnabled)
			{
				writer.AppendLineIndented("global::Uno.UI.FrameworkElementHelper.IsUiAutomationMappingEnabled = true;");

				if (_isWasm)
				{
					// When automation mapping is enabled, remove the element ID from the ToString so test screenshots stay the same.
					writer.AppendLineIndented("global::Uno.UI.FeatureConfiguration.UIElement.RenderToStringWithId = false;");
				}
			}

			void ApplyLiteralProperties()
			{
				writer.AppendLineIndented("this");

				using (var blockWriter = CreateApplyBlock(writer, null, out var closure))
				{
					blockWriter.AppendLineInvariantIndented(
						"// Source {0} (Line {1}:{2})",
						_relativePath,
						topLevelControl.LineNumber,
						topLevelControl.LinePosition
					);
					BuildLiteralProperties(blockWriter, topLevelControl, closure);
				}

				writer.AppendLineIndented(";");
			}
		}

		/// <summary>
		/// Override font properties at the end of the app.xaml ctor.
		/// </summary>
		/// <param name="writer"></param>
		private void ApplyFontsOverride(IndentedStringBuilder writer)
		{
			if (_generatorContext.GetMSBuildPropertyValue("UnoPlatformDefaultSymbolsFontFamily") is { Length: > 0 } fontOverride)
			{
				writer.AppendLineInvariantIndented($"global::Uno.UI.FeatureConfiguration.Font.SymbolsFont = \"{fontOverride}\";");
			}
		}

		private void BuildDrawableResourcesIdResolver(IndentedStringBuilder writer)
		{
			writer.AppendLine();
			writer.AppendLineIndented("/// <summary>");
			writer.AppendLineIndented("/// Resolves the Id of a bundled image.");
			writer.AppendLineIndented("/// </summary>");

			AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);

			writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

			using (writer.BlockInvariant("internal static class DrawableResourcesIdResolver"))
			{
				using (writer.BlockInvariant("internal static int Resolve(string imageName)"))
				{
					using (writer.BlockInvariant("switch (imageName)"))
					{
						var drawables = _metadataHelper
							.GetTypeByFullName($"{_defaultNamespace}.Resource")
							.GetTypeMembers("Drawable")
							.SingleOrDefault();

						// Support for net8.0+ resource constants
						drawables ??= _metadataHelper
							.GetTypeByFullName("_Microsoft.Android.Resource.Designer.ResourceConstant")
							.GetTypeMembers("Drawable")
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
			}
		}

		private void GenerateApiExtensionRegistrations(IndentedStringBuilder writer)
		{
			var apiExtensionAttributeSymbol = _metadataHelper.FindTypeByFullName("Uno.Foundation.Extensibility.ApiExtensionAttribute");

			var query = from ext in _metadataHelper.Compilation.ExternalReferences
						let sym = _metadataHelper.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
						where sym != null
						from attribute in sym.GetAllAttributes()
						where SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, apiExtensionAttributeSymbol)
						select attribute.ConstructorArguments;

			foreach (var registration in query)
			{
				writer.AppendLineIndented($"global::Uno.Foundation.Extensibility.ApiExtensibility.Register(typeof(global::{registration.ElementAt(0).Value}), o => new global::{registration.ElementAt(1).Value}(o));");
			}
		}

		private void InitializeRemoteControlClient(IndentedStringBuilder writer)
		{
			if (IsInsideUnoSolution(_generatorContext))
			{
				// Inside the Uno Solution we do not start the remote control
				// client, as the location of the RC server is not coming from 
				// a nuget package.
				writer.AppendLineIndented($"// Automatic remote control startup is disabled");
			}
			else if (_isHotReloadEnabled)
			{
				if (_metadataHelper.FindTypeByFullName("Uno.UI.RemoteControl.RemoteControlClient") != null)
				{
					writer.AppendLineIndented($"global::Uno.UI.RemoteControl.RemoteControlClient.Initialize(GetType());");
				}
				else
				{
					writer.AppendLineIndented($"// Remote control client is disabled (Type Uno.UI.RemoteControl.RemoteControlClient cannot be found)");
				}
			}
		}
		private static bool IsInsideUnoSolution(GeneratorExecutionContext context)
			=> context.GetMSBuildPropertyValue("_IsUnoUISolution") == "true";

		private void GenerateResourceLoader(IndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);
			writer.AppendLineIndented($"global::Windows.ApplicationModel.Resources.ResourceLoader.DefaultLanguage = \"{_defaultLanguage}\";");

			writer.AppendLineIndented($"global::Windows.ApplicationModel.Resources.ResourceLoader.AddLookupAssembly(GetType().Assembly);");

			foreach (var reference in _metadataHelper.Compilation.References)
			{
				if (_metadataHelper.Compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
				{
					BuildResourceLoaderFromAssembly(writer, assembly);
				}
				else
				{
					throw new InvalidOperationException($"Unsupported resource type for {reference.Display} ({reference.GetType()})");
				}
			}
		}

		private void BuildResourceLoaderFromAssembly(IndentedStringBuilder writer, IAssemblySymbol assembly)
		{
			var unoHasLocalizationResourcesAttribute = assembly.GetAttributes().FirstOrDefault(a =>
				SymbolEqualityComparer.Default.Equals(a.AttributeClass, Generation.AssemblyMetadataSymbol.Value)
				&& a.ConstructorArguments.Length == 2
				&& a.ConstructorArguments[0].Value is "UnoHasLocalizationResources");
			var unoHasLocalizationResourcesAttributeDefined = unoHasLocalizationResourcesAttribute is not null;

			var hasUnoHasLocalizationResourcesAttributeEnabled = unoHasLocalizationResourcesAttribute
				?.ConstructorArguments[1]
				.Value
				?.ToString()
				.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;

			// Legacy behavior relying on the fact that GlobalStaticResources is generated using the default namespace.
			var globalStaticResourcesSymbol = assembly.GetTypeByMetadataName(assembly.Name + ".GlobalStaticResources");

			if (
				// The assembly contains resources to be used
				hasUnoHasLocalizationResourcesAttributeEnabled

				// The assembly does not have the UnoHasLocalizationResources attribute defined, but
				// may still contain resources as it may have been built with a previous version of Uno.
				|| (!unoHasLocalizationResourcesAttributeDefined && globalStaticResourcesSymbol is not null)
			)
			{
				if (_isWasm)
				{
					var anchorType = globalStaticResourcesSymbol
						?? assembly
							.Modules
							.First()
							.GlobalNamespace
							.GetNamespaceTypes()
							.FirstOrDefault(s => s.IsLocallyPublic(_metadataHelper.Compilation.Assembly.Modules.First()));

					if (anchorType is INamedTypeSymbol namedSymbol)
					{
						// Use a public type to get the assembly to work around a WASM assembly loading issue
						writer.AppendLineIndented(
							$"global::Windows.ApplicationModel.Resources.ResourceLoader" +
							$".AddLookupAssembly(typeof(global::{namedSymbol.GetFullyQualifiedTypeExcludingGlobal()}).Assembly);"
						);
					}
				}
				else
				{
					writer.AppendLineIndented($"global::Windows.ApplicationModel.Resources.ResourceLoader.AddLookupAssembly(global::System.Reflection.Assembly.Load(\"{assembly.Name}\"));");
				}
			}
		}

		/// <summary>
		/// Processes a top-level control definition.
		/// </summary>
		/// <param name="writer">String builder</param>
		/// <param name="topLevelControl">The top-level xaml object</param>
		/// <param name="isDirectUserControlChild">True if the defined control directly inherits from UserControl.</param>
		private void BuildGenericControlInitializerBody(IndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			TryAnnotateWithGeneratorSource(writer);
			// OnInitializeCompleted() seems to be used by some older code as a substitute for the constructor for UserControls, which are optimized out of the visual tree.
			RegisterPartial("void OnInitializeCompleted()");

			var topLevelControlType = GetType(topLevelControl.Type);
			if (!IsWindow(topLevelControlType)) // Window is not a DependencyObject
			{
				writer.AppendLineIndented("NameScope.SetNameScope(this, __nameScope);");
			}
			writer.AppendLineIndented("var __that = this;");
			TrySetParsing(writer, topLevelControlType, isInitializer: false);

			using (TrySetDefaultBindMode(topLevelControl))
			{
				RegisterAndBuildResources(writer, topLevelControl, isInInitializer: false);
				BuildProperties(writer, topLevelControl, isInline: false, useBase: true);

				writer.AppendLineIndented(";");

				writer.AppendLineIndented("");
				writer.AppendLineIndented("this");


				using (var blockWriter = CreateApplyBlock(writer, null, out var closure))
				{
					blockWriter.AppendLineInvariantIndented(
						"// Source {0} (Line {1}:{2})",
						_relativePath,
						topLevelControl.LineNumber,
						topLevelControl.LinePosition
					);

					BuildLiteralProperties(blockWriter, topLevelControl, closure);
				}

				if (IsDependencyObject(topLevelControlType))
				{
					BuildExtendedProperties(writer, topLevelControl, useGenericApply: true);
				}
			}

			writer.AppendLineIndented(";");

			if (IsWindow(topLevelControlType)) // Window is not a DependencyObject
			{
				writer.AppendLineIndented("if (__that.Content != null)");
				using var _ = writer.Block();
				writer.AppendLineIndented("NameScope.SetNameScope(__that.Content, __nameScope);");
			}

			writer.AppendLineIndented("OnInitializeCompleted();");
		}

		private void BuildPartials(IIndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);
			foreach (var partialDefinition in _partials)
			{
				writer.AppendLineIndented($"partial {partialDefinition};");
			}
		}

		private void BuildXBindTryGetDeclarations(IIndentedStringBuilder writer)
		{
			foreach (var xBindMethodDeclaration in CurrentScope.XBindTryGetMethodDeclarations)
			{
				writer.AppendMultiLineIndented(xBindMethodDeclaration);
			}
		}

		private void BuildBackingFields(IIndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);
			foreach (var backingFieldDefinition in CurrentScope.BackingFields.Distinct())
			{
				var sanitizedFieldName = SanitizeResourceName(backingFieldDefinition.Name);

				if (sanitizedFieldName != null)
				{
					CurrentScope.ReferencedElementNames.Remove(sanitizedFieldName);
				}

				writer.AppendLineIndented($"private global::Windows.UI.Xaml.Data.ElementNameSubject _{sanitizedFieldName}Subject = new global::Windows.UI.Xaml.Data.ElementNameSubject();");


				using (writer.BlockInvariant($"{FormatAccessibility(backingFieldDefinition.Accessibility)} {backingFieldDefinition.GlobalizedTypeName} {sanitizedFieldName}"))
				{
					using (writer.BlockInvariant("get"))
					{
						writer.AppendLineIndented($"return ({backingFieldDefinition.GlobalizedTypeName})_{sanitizedFieldName}Subject.ElementInstance;");
					}

					using (writer.BlockInvariant("set"))
					{
						writer.AppendLineIndented($"_{sanitizedFieldName}Subject.ElementInstance = value;");
					}
				}
			}

			foreach (var xBindEventHandler in CurrentScope.xBindEventsHandlers)
			{
				// Create load-time subjects for ElementName references not in local scope
				writer.AppendLineIndented($"{FormatAccessibility(xBindEventHandler.Accessibility)} {xBindEventHandler.GlobalizedTypeName} {xBindEventHandler.Name};");
			}

			foreach (var remainingReference in CurrentScope.ReferencedElementNames)
			{
				// Create load-time subjects for ElementName references not in local scope
				writer.AppendLineIndented($"private global::Windows.UI.Xaml.Data.ElementNameSubject _{remainingReference}Subject = new global::Windows.UI.Xaml.Data.ElementNameSubject(isRuntimeBound: true, name: \"{remainingReference}\");");
			}
		}

		private static readonly char[] ResourceInvalidCharacters = new[] { '.', '-', ':' };

		private static string? SanitizeResourceName(string? name)
		{
			if (name != null)
			{
				foreach (var c in ResourceInvalidCharacters)
				{
					name = name.Replace(c, '_');
				}
			}

			return name;
		}

		private void BuildChildSubclasses(IIndentedStringBuilder writer, bool isTopLevel = false)
		{
			_isInChildSubclass = true;
			TryAnnotateWithGeneratorSource(writer);
			var ns = $"{_defaultNamespace}.__Resources";
			var disposable = isTopLevel ? writer.BlockInvariant($"namespace {ns}") : null;

			using (disposable)
			{
				foreach (var kvp in CurrentScope.Subclasses)
				{
					var className = kvp.Key;
					var contentOwner = kvp.Value.ContentOwner;

					using (TrySetDefaultBindMode(contentOwner.Owner, kvp.Value.DefaultBindMode))
					{
						using (Scope(ns, className))
						{
							var hrInterfaceName = _isHotReloadEnabled ? $"I{className}" : "";
							var hrInterfaceImpl = _isHotReloadEnabled ? $": {hrInterfaceName}" : "";

							if (_isHotReloadEnabled)
							{
								// Build an interface that can be used to hide the actual replaced
								// implementation of a type during hot reload.
								using (writer.BlockInvariant($"internal interface {hrInterfaceName}"))
								{
									writer.AppendLineIndented($"{kvp.Value.ReturnType} Build(object owner);");
								}
							}

							var classAccessibility = isTopLevel ? "" : "private";

							WriteMetadataNewTypeAttribute(writer);
							writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
							using (writer.BlockInvariant($"{classAccessibility} class {className} {hrInterfaceImpl}"))
							{
								BuildBaseUri(writer);

								using (ResourceOwnerScope())
								{
									writer.AppendLineIndented("global::Windows.UI.Xaml.NameScope __nameScope = new global::Windows.UI.Xaml.NameScope();");

									using (writer.BlockInvariant($"public {kvp.Value.ReturnType} Build(object {CurrentResourceOwner})"))
									{
										writer.AppendLineIndented($"{kvp.Value.ReturnType} __rootInstance = null;");
										writer.AppendLineIndented($"var __that = this;");
										writer.AppendLineIndented("__rootInstance = ");

										// Is never considered in Global Resources because class encapsulation
										BuildChild(writer, contentOwner, contentOwner.Objects.First());

										writer.AppendLineIndented(";");

										BuildCompiledBindingsInitializerForTemplate(writer);

										using (writer.BlockInvariant("if (__rootInstance is DependencyObject d)", kvp.Value.ReturnType))
										{
											using (writer.BlockInvariant("if (global::Windows.UI.Xaml.NameScope.GetNameScope(d) == null)", kvp.Value.ReturnType))
											{
												writer.AppendLineIndented("global::Windows.UI.Xaml.NameScope.SetNameScope(d, __nameScope);");
												writer.AppendLineIndented("__nameScope.Owner = d;");
											}

											writer.AppendLineIndented("global::Uno.UI.FrameworkElementHelper.AddObjectReference(d, this);");
										}

										writer.AppendLineIndented("return __rootInstance;");
									}
								}

								BuildComponentFields(writer);

								TryBuildElementStubHolders(writer);

								BuildBackingFields(writer);

								BuildChildSubclasses(writer);

								BuildXBindTryGetDeclarations(writer);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// The "OriginalSourceLocation" can be used like DebugParseContext (set via SetBaseUri) but for elements that aren't FrameworkElements
		/// </summary>
		private void TrySetOriginalSourceLocation(IIndentedStringBuilder writer, string element, int lineNumber, int linePosition)
		{
			if (_isHotReloadEnabled)
			{
				writer.AppendLineIndented($"global::Uno.UI.Helpers.MarkupHelper.SetElementProperty({element}, \"OriginalSourceLocation\", \"file:///{_fileDefinition.FilePath.Replace("\\", "/")}#L{lineNumber}:{linePosition}\");");
			}
		}

		/// <summary>
		/// Builds the element stub holder variables, use for platform having implicit pinning
		/// </summary>
		private void TryBuildElementStubHolders(IIndentedStringBuilder writer)
		{
			if (HasImplicitViewPinning)
			{
				foreach (var elementStubHolder in CurrentScope.ElementStubHolders)
				{
					writer.AppendLineIndented($"private Func<_View> {elementStubHolder};");
				}
			}
		}

		private (string bindingsInterfaceName, string bindingsClassName) GetBindingsTypeNames(string className)
			=> ($"I{className}_Bindings", $"{className}_Bindings");

		private void BuildCompiledBindingsInitializer(IndentedStringBuilder writer, INamedTypeSymbol controlBaseType)
		{
			TryAnnotateWithGeneratorSource(writer);

			EnsureXClassName();

			var hasXBindExpressions = CurrentScope.XBindExpressions.Count != 0;
			var hasResourceExtensions = CurrentScope.Components.Any(c => HasMarkupExtensionNeedingComponent(c.XamlObject));
			var frameworkElementSymbol = Generation.FrameworkElementSymbol.Value;
			var isFrameworkElement =
				IsType(_xClassName.Symbol, frameworkElementSymbol)              // The current type may not have a base type as it is defined in XAML,
				|| IsType(controlBaseType, frameworkElementSymbol);    // so look at the control base type extracted from the XAML.

			var isWindow = IsWindow(controlBaseType);

			if (hasXBindExpressions || hasResourceExtensions)
			{
				var activator = _isHotReloadEnabled
					? $"(({GetBindingsTypeNames(_xClassName.ClassName).bindingsInterfaceName})global::Uno.UI.Helpers.TypeMappings.CreateInstance<{GetBindingsTypeNames(_xClassName.ClassName).bindingsClassName}>(this))"
					: $"new {GetBindingsTypeNames(_xClassName.ClassName).bindingsClassName}(this)";

				writer.AppendLineIndented($"Bindings = {activator};");
			}

			if ((isFrameworkElement || isWindow) && (hasXBindExpressions || hasResourceExtensions))
			{
				if (_isHotReloadEnabled)
				{
					// Attach the current context to itself to avoid having a closure in the lambda
					writer.AppendLineIndented($"global::Uno.UI.Helpers.MarkupHelper.SetElementProperty(__that, \"owner\", __that);");
				}

				var eventSubscription = isFrameworkElement
					? "Loading += (s, e) =>"
					: "Activated += (s, e) =>";

				using (writer.BlockInvariant(eventSubscription))
				{
					if (_isHotReloadEnabled && _xClassName.Symbol != null)
					{
						writer.AppendLineIndented($"var __that = global::Uno.UI.Helpers.MarkupHelper.GetElementProperty<{_xClassName.Symbol?.GetFullyQualifiedTypeIncludingGlobal()}>(s, \"owner\");");
					}

					if (hasXBindExpressions)
					{
						writer.AppendLineIndented("__that.Bindings.Update();");
					}

					writer.AppendLineIndented("__that.Bindings.UpdateResources();");
				}

				writer.AppendLineIndented(";");
			}
		}
		private void BuildCompiledBindingsInitializerForTemplate(IIndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);
			var hasResourceExtensions = CurrentScope.Components.Any(c => HasMarkupExtensionNeedingComponent(c.XamlObject));

			if (hasResourceExtensions)
			{
				using (writer.BlockInvariant($"if (__rootInstance is FrameworkElement __fe) "))
				{
					writer.AppendLineIndented($"var owner = this;");

					using (writer.BlockInvariant($"__fe.Loading += delegate"))
					{
						BuildComponentResouceBindingUpdates(writer);
						BuildxBindEventHandlerInitializers(writer, CurrentScope.xBindEventsHandlers);
					}
					writer.AppendLineIndented(";");
				}
			}
		}

		private void BuildxBindEventHandlerInitializers(IIndentedStringBuilder writer, List<EventHandlerBackingFieldDefinition> xBindEventsHandlers, string prefix = "")
		{
			foreach (var xBindEventHandler in xBindEventsHandlers)
			{
				var ownerParameter = _isHotReloadEnabled ? "owner," : "";

				writer.AppendLineIndented($"{prefix}{xBindEventHandler.Name}?.Invoke({ownerParameter} owner.{xBindEventHandler.ComponentName});");

				// Only needs to happen once per visual tree creation
				writer.AppendLineIndented($"{prefix}{xBindEventHandler.Name} = null;");
			}
		}

		private void BuildComponentResouceBindingUpdates(IIndentedStringBuilder writer)
		{
			for (var i = 0; i < CurrentScope.Components.Count; i++)
			{
				var component = CurrentScope.Components[i];

				if (HasMarkupExtensionNeedingComponent(component.XamlObject) && IsDependencyObject(component.XamlObject))
				{
					writer.AppendLineIndented($"{component.MemberName}.UpdateResourceBindings();");
				}
			}
		}

		private void BuildComponentFields(IIndentedStringBuilder writer)
		{
			for (var i = 0; i < CurrentScope.Components.Count; i++)
			{
				var current = CurrentScope.Components[i];

				var componentName = current.MemberName;
				var typeName = GetType(current.XamlObject.Type).GetFullyQualifiedTypeIncludingGlobal();

				var isWeak = current.IsWeakReference ? "true" : "false";

				// As of C# 7.0, C# Hot Reload does not support the renaming fields.
				var propertySyntax = _isHotReloadEnabled ? "{ get; }" : "";
				writer.AppendLineIndented($"private global::Windows.UI.Xaml.Markup.ComponentHolder {componentName}_Holder {propertySyntax} = new global::Windows.UI.Xaml.Markup.ComponentHolder(isWeak: {isWeak});");

				using (writer.BlockInvariant($"private {typeName} {componentName}"))
				{
					using (writer.BlockInvariant("get"))
					{
						writer.AppendLineIndented($"return ({typeName}){componentName}_Holder.Instance;");
					}

					using (writer.BlockInvariant("set"))
					{
						writer.AppendLineIndented($"{componentName}_Holder.Instance = value;");
					}
				}
			}
		}

		private void BuildCompiledBindings(IndentedStringBuilder writer)
		{
			EnsureXClassName();

			var hasXBindExpressions = CurrentScope.XBindExpressions.Count != 0;
			var hasResourceExtensions = CurrentScope.Components.Any(c => HasMarkupExtensionNeedingComponent(c.XamlObject));

			if (hasXBindExpressions || hasResourceExtensions)
			{
				var (bindingsInterfaceName, bindingsClassName) = GetBindingsTypeNames(_xClassName.ClassName);

				using (writer.BlockInvariant($"private interface {bindingsInterfaceName}"))
				{
					writer.AppendLineIndented("void Initialize();");
					writer.AppendLineIndented("void Update();");
					writer.AppendLineIndented("void UpdateResources();");
					writer.AppendLineIndented("void StopTracking();");
					writer.AppendLineIndented("void NotifyXLoad(string name);");
				}

				writer.AppendLineIndented($"#pragma warning disable 0169 //  Suppress unused field warning in case Bindings is not used.");
				writer.AppendLineIndented($"private {bindingsInterfaceName} Bindings;");
				writer.AppendLineIndented($"#pragma warning restore 0169");

				WriteMetadataNewTypeAttribute(writer);

				writer.AppendLineIndented($"[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]");
				using (writer.BlockInvariant($"private class {bindingsClassName} : {bindingsInterfaceName}"))
				{
					writer.AppendLineIndented("#if UNO_HAS_UIELEMENT_IMPLICIT_PINNING");
					writer.AppendLineInvariantIndented("{0}", $"private global::System.WeakReference _ownerReference;");
					writer.AppendLineInvariantIndented("{0}", $"private {_xClassName} Owner {{ get => ({_xClassName})_ownerReference?.Target; set => _ownerReference = new global::System.WeakReference(value); }}");
					writer.AppendLineIndented("#else");
					writer.AppendLineInvariantIndented("{0}", $"private {_xClassName} Owner {{ get; set; }}");
					writer.AppendLineIndented("#endif");

					using (writer.BlockInvariant($"public {bindingsClassName}({_xClassName} owner)"))
					{
						writer.AppendLineIndented($"Owner = owner;");
					}

					using (writer.BlockInvariant($"void {bindingsInterfaceName}.NotifyXLoad(string name)"))
					{
						var xLoadNames = new HashSet<string>();
						for (var i = 0; i < CurrentScope.Components.Count; i++)
						{
							var component = CurrentScope.Components[i];
							var isXLoad = false;
							string? xName = null;
							foreach (var member in component.XamlObject.Members)
							{
								if (IsXLoadMember(member))
								{
									isXLoad = true;
								}

								if (member.Member.Name == "Name" &&
									member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
								{
									xName = member.Value as string;
								}
							}

							if (isXLoad && xName is not null)
							{
								xLoadNames.Add(xName);
							}
						}

						foreach (var xLoadName in xLoadNames)
						{
							var componentsWithXBindReferencingLazyElement = new HashSet<string>();
							using (writer.BlockInvariant($"if (name == \"{xLoadName}\")"))
							{
								for (var i = 0; i < CurrentScope.Components.Count; i++)
								{
									var component = CurrentScope.Components[i];
									foreach (var member in component.XamlObject.Members)
									{
										foreach (var xamlObject in member.Objects)
										{
											if (xamlObject.Type.Name == "Bind")
											{
												var xbindPath = XBindExpressionParser.RestoreSinglePath(xamlObject.Members?.FirstOrDefault()?.Value as string);
												if (!string.IsNullOrEmpty(xbindPath))
												{
													var firstPart = xbindPath;
													var indexOfDot = xbindPath!.IndexOf('.');
													if (indexOfDot > -1)
													{
														firstPart = xbindPath.Substring(0, indexOfDot);
														if (firstPart == xLoadName && componentsWithXBindReferencingLazyElement.Add(xLoadName))
														{
															writer.AppendLineIndented($"Owner.{component.MemberName}.ApplyXBind();");
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}

					using (writer.BlockInvariant($"void {bindingsInterfaceName}.Initialize()")) { }
					using (writer.BlockInvariant($"void {bindingsInterfaceName}.Update()"))
					{
						writer.AppendLineIndented($"var owner = Owner;");

						for (var i = 0; i < CurrentScope.Components.Count; i++)
						{
							var component = CurrentScope.Components[i];

							if (HasXBindMarkupExtension(component.XamlObject))
							{
								var isDependencyObject = IsDependencyObject(component.XamlObject);

								var wrapInstance = isDependencyObject ? "" : ".GetDependencyObjectForXBind()";

								writer.AppendLineIndented($"owner.{component.MemberName}{wrapInstance}.ApplyXBind();");
							}

							BuildxBindEventHandlerInitializers(writer, CurrentScope.xBindEventsHandlers, "owner.");
						}
					}
					using (writer.BlockInvariant($"void {bindingsInterfaceName}.UpdateResources()"))
					{
						writer.AppendLineIndented($"var owner = Owner;");

						for (var i = 0; i < CurrentScope.Components.Count; i++)
						{
							var component = CurrentScope.Components[i];

							if (HasMarkupExtensionNeedingComponent(component.XamlObject) && IsDependencyObject(component.XamlObject))
							{
								writer.AppendLineIndented($"owner.{component.MemberName}.UpdateResourceBindings();");
							}
						}
					}
					using (writer.BlockInvariant($"void {bindingsInterfaceName}.StopTracking()")) { }
				}
			}
		}

		/// <summary>
		/// Processes a top-level ResourceDictionary declaration.
		/// </summary>
		private void BuildTopLevelResourceDictionary(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			TryAnnotateWithGeneratorSource(writer);

			using (Scope(null, Path.GetFileNameWithoutExtension(_fileDefinition.FilePath).Replace(".", "_") + "RD"))
			{
				using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
				{
					AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);
					using (writer.BlockInvariant("public sealed partial class GlobalStaticResources"))
					{
						BuildBaseUri(writer);

						if (_isHotReloadEnabled)
						{
							// Create a public member to avoid having to remove all unused member warnings
							// The member is a method to avoid this error: error ENC0011: Updating the initializer of const field requires restarting the application.
							writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
							writer.AppendLineIndented($"internal string __{_fileDefinition.UniqueID}_checksum() => \"{_fileDefinition.Checksum}\";");
						}

						IDisposable WrapSingleton()
						{
							writer.AppendLineIndented("// This non-static inner class is a means of reducing size of AOT compilations by avoiding many accesses to static members from a static callsite, which adds costly class initializer checks each time.");

							WriteMetadataNewTypeAttribute(writer);

							var block = writer.BlockInvariant("internal sealed class {0} : {1}", SingletonClassName, DictionaryProviderInterfaceName);
							_isInSingletonInstance = true;
							return new DisposableAction(() =>
							{
								block.Dispose();
								_isInSingletonInstance = false;
							});
						}

						var hasDefaultStyles = false;
						using (WrapSingleton())
						{
							// Build singleton
							writer.AppendLineInvariantIndented("private static global::Windows.UI.Xaml.NameScope __nameScope = new global::Windows.UI.Xaml.NameScope();");
							writer.AppendLineInvariantIndented("private static {0} __that;", DictionaryProviderInterfaceName);
							using (writer.BlockInvariant("internal static {0} Instance", DictionaryProviderInterfaceName))
							{
								using (writer.BlockInvariant("get"))
								{
									using (writer.BlockInvariant("if (__that == null)"))
									{
										var activator = _isHotReloadEnabled
											? $"({DictionaryProviderInterfaceName})global::Uno.UI.Helpers.TypeMappings.CreateInstance<{SingletonClassName}>()"
											: $"new {SingletonClassName}()";

										writer.AppendLineInvariantIndented($"__that = {activator};");
									}
									writer.AppendLine();
									writer.AppendLineIndented("return __that;");
								}
							}
							writer.AppendLine();
							writer.AppendLineInvariantIndented("private readonly {0} {1};", XamlCodeGeneration.ParseContextPropertyType, XamlCodeGeneration.ParseContextPropertyName);
							writer.AppendLineInvariantIndented("internal static {0} {1}() => (({2})Instance).{3};", XamlCodeGeneration.ParseContextPropertyType, XamlCodeGeneration.ParseContextGetterMethod, SingletonClassName, XamlCodeGeneration.ParseContextPropertyName);
							writer.AppendLine();
							using (writer.BlockInvariant("public {0}()", SingletonClassName))
							{
								var outerProperty = "{0}{1}.GlobalStaticResources.{2}".InvariantCultureFormat(
									GlobalPrefix,
									_defaultNamespace,
									XamlCodeGeneration.ParseContextPropertyName
								);
								writer.AppendLineInvariantIndented("{0} = {1};", XamlCodeGeneration.ParseContextPropertyName, outerProperty);
							}
							writer.AppendLine();

							// Build initializer methods for resource retrieval
							BuildTopLevelResourceDictionaryInitializers(writer, topLevelControl);

							var themeDictionaryMember = topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries");

							foreach (var dict in (themeDictionaryMember?.Objects).Safe())
							{
								BuildTopLevelResourceDictionaryInitializers(writer, dict);
							}

							TryAnnotateWithGeneratorSource(writer, suffix: "DictionaryProperty");
							writer.AppendLineInvariantIndented("private global::Windows.UI.Xaml.ResourceDictionary _{0}_ResourceDictionary;", _fileUniqueId);
							writer.AppendLine();
							using (writer.BlockInvariant("internal global::Windows.UI.Xaml.ResourceDictionary {0}_ResourceDictionary", _fileUniqueId))
							{
								using (writer.BlockInvariant("get"))
								{
									using (writer.BlockInvariant("if (_{0}_ResourceDictionary == null)", _fileUniqueId))
									{
										writer.AppendLineInvariantIndented("_{0}_ResourceDictionary = ", _fileUniqueId);
										InitializeAndBuildResourceDictionary(writer, topLevelControl, setIsParsing: true);
										writer.AppendLineIndented(";");
										var url = _globalStaticResourcesMap.GetSourceLink(_fileDefinition);
										writer.AppendLineInvariantIndented("_{0}_ResourceDictionary.Source = new global::System.Uri(\"{1}{2}\");", _fileUniqueId, XamlFilePathHelper.LocalResourcePrefix, url);
										writer.AppendLineInvariantIndented("_{0}_ResourceDictionary.CreationComplete();", _fileUniqueId);
									}

									writer.AppendLineInvariantIndented("return _{0}_ResourceDictionary;", _fileUniqueId);
								}
							}

							hasDefaultStyles = BuildDefaultStylesRegistration(writer, FindImplicitContentMember(topLevelControl));

							writer.AppendLine();
							writer.AppendLineInvariantIndented("global::Windows.UI.Xaml.ResourceDictionary {0}.GetResourceDictionary() => {1}_ResourceDictionary;", DictionaryProviderInterfaceName, _fileUniqueId);
						}
						writer.AppendLine();
						writer.AppendLineInvariantIndented("internal static global::Windows.UI.Xaml.ResourceDictionary {0}_ResourceDictionary => {1}.Instance.GetResourceDictionary();", _fileUniqueId, SingletonClassName);
						if (hasDefaultStyles)
						{
							writer.AppendLineInvariantIndented("static partial void RegisterDefaultStyles_{0}() => {1}.RegisterDefaultStyles_{0}();", _fileUniqueId, SingletonClassName);
						}

						BuildXBindTryGetDeclarations(writer);
					}
				}

				BuildChildSubclasses(writer, isTopLevel: true);
			}
		}

		private void WriteMetadataNewTypeAttribute(IIndentedStringBuilder writer)
		{
			if (_isHotReloadEnabled)
			{
				writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]");
			}
		}

		/// <summary>
		///Build initializers for the current ResourceDictionary.
		/// </summary>
		/// <param name="writer">The writer to use</param>
		/// <param name="dictionaryObject">The <see cref="XamlObjectDefinition"/> associated with the dictionary.</param>
		private void BuildTopLevelResourceDictionaryInitializers(IIndentedStringBuilder writer, XamlObjectDefinition dictionaryObject)
		{
			TryAnnotateWithGeneratorSource(writer);
			var resourcesRoot = FindImplicitContentMember(dictionaryObject);
			var theme = GetDictionaryResourceKey(dictionaryObject);
			var resources = (resourcesRoot?.Objects).Safe();

			// Pre-populate initializer names (though this is probably no longer necessary...?)
			var index = _dictionaryPropertyIndex;
			foreach (var resource in resources)
			{
				var key = GetDictionaryResourceKey(resource);

				if (key == null || IsNativeStyle(resource))
				{
					continue;
				}

				index++;
				var propertyName = GetInitializerNameForResourceKey(index);
				if (_topLevelQualifiedKeys.ContainsKey((theme, key)))
				{
					throw new InvalidOperationException($"Dictionary Item {resource.Type?.Name} has duplicate key `{key}` {(theme != null ? $" in theme {theme}" : "")}.");
				}
				var isStaticResourceAlias = resource.Type.Name == "StaticResource";
				if (!isStaticResourceAlias)
				{
					_topLevelQualifiedKeys[(theme, key)] = propertyName;
				}
			}

			var former = _themeDictionaryCurrentlyBuilding; //Will 99% of the time be null. (Mainly this is half-heartedly trying to support funky usage of recursive merged dictionaries.)
			_themeDictionaryCurrentlyBuilding = theme;
			foreach (var resource in resources)
			{
				var key = GetDictionaryResourceKey(resource);

				if (key == null || IsNativeStyle(resource))
				{
					continue;
				}

				_dictionaryPropertyIndex++;
				var isStaticResourceAlias = resource.Type.Name == "StaticResource";
				if (isStaticResourceAlias)
				{
					writer.AppendLineInvariantIndented("// Skipping initializer {0} for {1} {2} - StaticResource ResourceKey aliases are added directly to dictionary", _dictionaryPropertyIndex, key, theme);
				}
				else if (!ShouldLazyInitializeResource(resource))
				{
					writer.AppendLineInvariantIndented("// Skipping initializer {0} for {1} {2} - Literal declaration, will be eagerly materialized and added to the dictionary", _dictionaryPropertyIndex, key, theme);
				}
				else
				{
					var initializerName = GetInitializerNameForResourceKey(_dictionaryPropertyIndex);
					if (_topLevelQualifiedKeys[(theme, key)] != initializerName)
					{
						throw new InvalidOperationException($"Method name was not created correctly for {key} (theme={theme}).");
					}
					writer.AppendLineInvariantIndented("// Method for resource {0} {1}", key, theme != null ? "in theme {0}".InvariantCultureFormat(theme) : "");
					BuildSingleTimeInitializer(writer, initializerName, () => BuildChild(writer, resourcesRoot, resource));
				}
			}
			_themeDictionaryCurrentlyBuilding = former;
		}

		/// <summary>
		/// Build StaticResource alias inside a ResourceDictionary.
		/// </summary>
		private void BuildStaticResourceResourceKeyReference(IIndentedStringBuilder writer, XamlObjectDefinition resourceDefinition)
		{
			TryAnnotateWithGeneratorSource(writer);
			var targetKey = resourceDefinition.Members.FirstOrDefault(m => m.Member.Name == "ResourceKey")?.Value as string;

			writer.AppendLineInvariantIndented("global::Uno.UI.ResourceResolverSingleton.Instance.ResolveStaticResourceAlias(\"{0}\", {1})", targetKey, ParseContextPropertyAccess);
		}

		/// <summary>
		/// Get name to use for initializer associated with a resource.
		/// </summary>
		/// <param name="index">An index associated with the property.</param>
		/// <remarks>
		/// We don't use the unqualified resource key because there may be multiple dictionary definitions (merged dictionaries, themed
		/// dictionaries) in the same switch block, and it's possible (probable actually, in the case of theme dictionaries) to have
		/// duplicate resource keys.
		/// </remarks>
		private string GetInitializerNameForResourceKey(int index)
		{
			return $"Get_{index}";
		}

		/// <summary>
		/// Get the dictionary key set on a Xaml object, if any. This may be defined by x:Key or alternately x:Name.
		/// </summary>
		private string? GetDictionaryResourceKey(XamlObjectDefinition resource) => GetDictionaryResourceKey(resource, out var _);

		private string? GetDictionaryResourceKey(XamlObjectDefinition resource, out string? name)
			=> GetExplicitDictionaryResourceKey(resource, out name)
			?? GetImplicitDictionaryResourceKey(resource);


		/// <summary>
		/// Get the dictionary key set on a Xaml object, if any. This may be defined by x:Key or alternately x:Name.
		/// </summary>
		/// <param name="resource">The Xaml object.</param>
		/// <param name="name">The x:Name defined on the object, if any, returned as an out parameter.</param>
		/// <returns>The key to use if one is defined, otherwise null.</returns>
		private static string? GetExplicitDictionaryResourceKey(XamlObjectDefinition resource, out string? name)
		{
			var keyDef = resource.Members.FirstOrDefault(m => m.Member.Name == "Key");
			var nameDef = resource.Members.FirstOrDefault(m => m.Member.Name == "Name");
			name = nameDef?.Value?.ToString();
			var key = keyDef?.Value?.ToString() ?? name;
			return key;
		}

		/// <summary>
		/// Get the implicit key for a dictionary resource, if any.
		///
		/// An implicit key is the TargetType of a Style resource.
		/// </summary>
		private string? GetImplicitDictionaryResourceKey(XamlObjectDefinition resource)
		{
			if (resource.Type.Name == "Style"
				&& resource.Members.FirstOrDefault(m => m.Member.Name == "TargetType")?.Value is string targetTypeName)
			{
				var targetType = GetType(targetTypeName);
				return "typeof({0})".InvariantCultureFormat(targetType.GetFullyQualifiedTypeIncludingGlobal());
			}

			return null;
		}

		/// <summary>
		/// Builds registrations for the default styles defined in this ResourceDictionary.
		///
		/// Note: if we're currently building an app, these registrations will never actually be called (the styles will be treated as implicit styles
		/// instead).
		/// </summary>
		private bool BuildDefaultStylesRegistration(IIndentedStringBuilder writer, XamlMemberDefinition? resourcesRoot)
		{
			TryAnnotateWithGeneratorSource(writer);
			var stylesCandidates = resourcesRoot?.Objects.Where(o => o.Type.Name == "Style" && GetExplicitDictionaryResourceKey(o, out var _) == null);
			if (stylesCandidates?.None() ?? true)
			{
				return false;
			}

			writer.AppendLine();

			using (writer.BlockInvariant("public static void RegisterDefaultStyles_{0}()", _fileUniqueId))
			{
				if (_isHotReloadEnabled)
				{
					writer.AppendLineInvariantIndented("var instance = Instance;");
					using (writer.BlockInvariant("if (instance is {0} original)", SingletonClassName))
					{
						writer.AppendLineInvariantIndented("original.RegisterDefaultStyles_{1}_Core();", SingletonClassName, _fileUniqueId);
					}
					using (writer.BlockInvariant("else"))
					{
						writer.AppendLineInvariantIndented("instance?.GetType()?.GetMethod(\"RegisterDefaultStyles_{0}_Core\", global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic)?.Invoke(instance, null);", _fileUniqueId);
					}
				}
				else
				{
					writer.AppendLineInvariantIndented("(({0})Instance).RegisterDefaultStyles_{1}_Core();", SingletonClassName, _fileUniqueId);
				}
			}

			using (writer.BlockInvariant("private void RegisterDefaultStyles_{0}_Core()", _fileUniqueId))
			{
				foreach (var style in stylesCandidates)
				{
					var targetTypeMember = style.Members.FirstOrDefault(m => m.Member.Name == "TargetType");
					if (!(targetTypeMember?.Value is string targetTypeName))
					{
						continue;
					}

					var targetType = GetType(targetTypeName);
					var implicitKey = GetImplicitDictionaryResourceKey(style);
					if (SymbolEqualityComparer.Default.Equals(targetType.ContainingAssembly, _metadataHelper.Compilation.Assembly))
					{
						var isNativeStyle = IsNativeStyle(style);

						using (TrySingleLineIfForLinkerHint(writer, style))
						{
							writer.AppendLineInvariantIndented("global::Windows.UI.Xaml.Style.RegisterDefaultStyleForType({0}, {1}, /*isNativeStyle:*/{2});",
										implicitKey,
										SingletonInstanceAccess,
										isNativeStyle.ToString().ToLowerInvariant()
									);
						}
					}
					else
					{
						writer.AppendLineInvariantIndented("// Skipping style registration for {0} because the type is defined in assembly {1}", implicitKey, targetType.ContainingAssembly.Name);
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Determines if the provided object is setting the "IsNativeStyle" property, used in
		/// conjuction with "FeatureConfiguration.Style.UseUWPDefaultStyles"
		/// </summary>
		private bool IsNativeStyle(XamlObjectDefinition style)
			=> string.Equals(style.Members.FirstOrDefault(m => m.Member.Name == "IsNativeStyle")?.Value as string, "True", StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Initialize a new ResourceDictionary instance and populate its items and properties.
		/// </summary>
		private void InitializeAndBuildResourceDictionary(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl, bool setIsParsing)
		{
			TryAnnotateWithGeneratorSource(writer);

			if (IsResourceDictionarySubclass(topLevelControl.Type))
			{
				BuildTypedResourceDictionary(writer, topLevelControl);
			}
			else
			{
				using (writer.BlockInvariant("new global::Windows.UI.Xaml.ResourceDictionary"))
				{
					if (setIsParsing)
					{
						TrySetParsing(writer, FindType(topLevelControl.Type), isInitializer: true);
					}
					if (_isUnoAssembly)
					{
						writer.AppendLineIndented("IsSystemDictionary = true,");
					}
					BuildMergedDictionaries(writer, topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "MergedDictionaries"), isInInitializer: true);
					BuildThemeDictionaries(writer, topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries"), isInInitializer: true);
					BuildResourceDictionary(writer, FindImplicitContentMember(topLevelControl), isInInitializer: true);
				}
			}
		}

		private void BuildTypedResourceDictionary(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			var type = GetType(topLevelControl.Type);

			if (type.GetFullyQualifiedTypeExcludingGlobal().Equals("Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.XamlControlsResources", StringComparison.Ordinal))
			{
				int GetResourcesVersion()
				{
					// We're in a XAML file which uses the XamlControlsResources type. To ensure that the linker can work
					// properly we're redirecting the type creation to a type containing only the requested version.
					if (topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ControlsResourcesVersion") is { } versionMember)
					{
						if (versionMember.Value?.ToString()?.TrimStart("Version") is { } versionString)
						{
							if (int.TryParse(versionString, out var explicitVersion))
							{
								if (explicitVersion < 1 || explicitVersion > XamlConstants.MaxFluentResourcesVersion)
								{
									throw new Exception($"Unsupported XamlControlsResources version {explicitVersion}. Max version is {XamlConstants.MaxFluentResourcesVersion}");
								}
							}

							return explicitVersion;
						}
					}

					return XamlConstants.MaxFluentResourcesVersion;
				}

				using (writer.BlockInvariant($"new global::Microsoft" + /* UWP don't rename */ $".UI.Xaml.Controls.XamlControlsResourcesV{GetResourcesVersion()}()"))
				{
					BuildLiteralProperties(writer, topLevelControl);
				}
			}
			else
			{
				using (writer.BlockInvariant("new /* typed resource dictionary */ {0}()", type.GetFullyQualifiedTypeIncludingGlobal()))
				{
					BuildLiteralProperties(writer, topLevelControl);
				}
			}
		}

		/// <summary>
		/// Build backing class from a top-level ResourceDictionary. This is only applied if the 'x:Class' attribute is set.
		/// </summary>
		private void BuildResourceDictionaryBackingClass(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			TryAnnotateWithGeneratorSource(writer);
			var className = FindClassName(topLevelControl);

			if (className?.Namespace != null)
			{
				var controlBaseType = GetType(topLevelControl.Type);

				using (writer.BlockInvariant("namespace {0}", className.Namespace))
				{
					AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);

					WriteMetadataNewTypeAttribute(writer);

					using (writer.BlockInvariant("public sealed partial class {0} : {1}", className.ClassName, controlBaseType.GetFullyQualifiedTypeIncludingGlobal()))
					{
						BuildBaseUri(writer);

						using (Scope(className.Namespace, className.ClassName!))
						{
							using (writer.BlockInvariant("public void InitializeComponent()"))
							{
								if (_isHotReloadEnabled)
								{
									TrySetOriginalSourceLocation(writer, "this", topLevelControl.LineNumber, topLevelControl.LinePosition);
								}

								BuildMergedDictionaries(writer, topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "MergedDictionaries"), isInInitializer: false, dictIdentifier: "this");
								BuildThemeDictionaries(writer, topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries"), isInInitializer: false, dictIdentifier: "this");
								BuildResourceDictionary(writer, FindImplicitContentMember(topLevelControl), isInInitializer: false, dictIdentifier: "this");
							}

							writer.AppendLine();

							BuildChildSubclasses(writer);
							BuildXBindTryGetDeclarations(writer);
						}
					}
				}
			}
		}

		/// <summary>
		/// Build single initializer for resource retrieval
		/// </summary>
		private void BuildSingleTimeInitializer(IIndentedStringBuilder writer, string initializerName, Action propertyBodyBuilder)
		{
			TryAnnotateWithGeneratorSource(writer);
			using (ResourceOwnerScope())
			{
				writer.AppendLineIndented($"private object {initializerName}(object {CurrentResourceOwner}) =>");
				using (writer.Indent())
				{
					propertyBodyBuilder();
					writer.AppendLineIndented(";");
				}
				writer.AppendLine();
			}
		}

		private void BuildSourceLineInfo(IIndentedStringBuilder writer, XamlObjectDefinition definition)
		{
			TryAnnotateWithGeneratorSource(writer);
			writer.AppendLineInvariantIndented(
				"// Source {0} (Line {1}:{2})",
				_relativePath,
				definition.LineNumber,
				definition.LinePosition
			);
		}

		private void BuildNamedResources(
			IIndentedStringBuilder writer,
			IEnumerable<KeyValuePair<string, XamlObjectDefinition>> namedResources
		)
		{
			TryAnnotateWithGeneratorSource(writer);
			writer.AppendLine();
			if (namedResources.Any())
			{
				writer.AppendLineIndented($"// Force materialization of x:Name resources, which will assign them to named property field.");
				writer.AppendLineIndented("object _ = null;");
			}
			foreach (var namedResource in namedResources)
			{
				BuildSourceLineInfo(writer, namedResource.Value);

				writer.AppendLineInvariantIndented("Resources.TryGetValue(\"{0}\", out _);", namedResource.Key);
			}

			bool IsGenerateUpdateResourceBindings(KeyValuePair<string, XamlObjectDefinition> nr)
			{
				var type = GetType(nr.Value.Type);

				// Styles are handled differently for now, and there's no variable generated
				// for those entries. Skip the ApplyCompiledBindings for those. See
				// ImportResourceDictionary handling of x:Name for more details.
				if (SymbolEqualityComparer.Default.Equals(type, Generation.StyleSymbol.Value))
				{
					return false;
				}

				if (type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, Generation.DependencyObjectSymbol.Value)))
				{
					return true;
				}

				return false;
			}

			var resourcesTogenerateUpdateBindings = namedResources
				.Where(IsGenerateUpdateResourceBindings)
				.ToArray();

			if (resourcesTogenerateUpdateBindings.Length > 0)
			{
				using (writer.BlockInvariant("Loading += (s, e) =>"))
				{
					foreach (var namedResource in resourcesTogenerateUpdateBindings)
					{
						var type = GetType(namedResource.Value.Type);

						writer.AppendLineIndented($"{namedResource.Key}.UpdateResourceBindings();");
					}
				}
				writer.AppendLineIndented(";");
			}
		}

		private void GenerateError(IIndentedStringBuilder writer, string message)
		{
			GenerateError(writer, message.Replace("{", "{{").Replace("}", "}}"), Array.Empty<object>());
		}

		private void GenerateError(IIndentedStringBuilder writer, string message, params object?[] options)
		{
			TryAnnotateWithGeneratorSource(writer);
			if (ShouldWriteErrorOnInvalidXaml)
			{
				// it's important to add a new line to make sure #error is on its own line.
				writer.AppendLineIndented(string.Empty);
				writer.AppendLineInvariantIndented("#error " + message, options);
			}
			else
			{
				GenerateSilentWarning(writer, message, options);
			}

		}

		private void GenerateSilentWarning(IIndentedStringBuilder writer, string message, params object?[] options)
		{
			TryAnnotateWithGeneratorSource(writer);
			// it's important to add a new line to make sure #error is on its own line.
			writer.AppendLineIndented(string.Empty);
			writer.AppendLineInvariantIndented("// WARNING " + message, options);
		}

		/// <summary>
		/// Is <paramref name="valueNode"/> the kind of thing that involves squiggly brackets?
		/// </summary>
		private bool HasMarkupExtension(XamlMemberDefinition? valueNode)
		{
			// Return false if the Owner is a custom markup extension
			if (valueNode == null || IsCustomMarkupExtensionType(valueNode.Owner?.Type))
			{
				return false;
			}

			return valueNode
				.Objects
				.Any(o =>
					o.Type.Name == "StaticResource"
					|| o.Type.Name == "ThemeResource"
					|| o.Type.Name == "Binding"
					|| o.Type.Name == "Bind"
					|| o.Type.Name == "TemplateBinding"
					|| o.Type.Name == "CustomResource"
				);
		}

		/// <summary>
		/// Determines if the member definition as x:Name property
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		private static bool HasXNameProperty(XamlMemberDefinition m)
			=> m.Member.Name == "Name" && m.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace;

		private bool HasCustomMarkupExtension(XamlMemberDefinition valueNode)
		{
			// Verify if a custom markup extension exists
			return valueNode
				.Objects
				.Any(o => IsCustomMarkupExtensionType(o.Type));
		}

		private bool HasBindingMarkupExtension(XamlMemberDefinition valueNode)
		{
			return valueNode
				.Objects
				.Any(o =>
					o.Type.Name == "Binding"
					|| o.Type.Name == "Bind"
					|| o.Type.Name == "TemplateBinding"
				);
		}

		private bool HasXBindMarkupExtension(XamlObjectDefinition objectDefinition)
			=> objectDefinition
				.Members
				.Any(o => o.Objects.Any(o => o.Type.Name == "Bind"));

		private bool HasMarkupExtensionNeedingComponent(XamlObjectDefinition objectDefinition)
			=> objectDefinition
				.Members
				.Any(o =>
					o.Objects.Any(o =>
						o.Type.Name == "Bind"
						|| o.Type.Name == "StaticResource"
						|| o.Type.Name == "ThemeResource"

						// Bindings with ElementName properties needs to be resolved during Loading.
						|| (o.Type.Name == "Binding" && o.Members.Any(m => m.Member.Name == "ElementName"))
					)
				)
			|| (
				objectDefinition.Type.Name == "ElementStub"
			);

		/// <summary>
		/// Does this node or any nested nodes have markup extensions?
		/// </summary>
		private bool HasDescendantsWithMarkupExtension(XamlObjectDefinition xamlObjectDefinition)
			=> HasDescendantsWith(xamlObjectDefinition, HasMarkupExtension);

		private bool HasDescendantsWithXName(XamlMemberDefinition memberDefinition)
			=> memberDefinition.Objects.Any(o => HasDescendantsWith(o, HasXNameProperty));

		private bool HasDescendantsWith(XamlObjectDefinition xamlObjectDefinition, Func<XamlMemberDefinition, bool> predicate)
		{
			foreach (var member in xamlObjectDefinition.Members)
			{
				if (predicate(member))
				{
					return true;
				}

				foreach (var obj in member.Objects)
				{
					if (HasDescendantsWith(obj, predicate))
					{
						return true;
					}
				}
			}

			foreach (var obj in xamlObjectDefinition.Objects)
			{
				if (HasDescendantsWith(obj, predicate))
				{
					return true;
				}
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private INamedTypeSymbol? GetMarkupExtensionType(XamlType? xamlType)
		{
			if (xamlType == null)
			{
				return null;
			}

			var ns = GetTrimmedNamespace(xamlType.PreferredXamlNamespace); // No MarkupExtensions are defined in the framework, so we expect a user-defined namespace
			INamedTypeSymbol? findType;
			if (ns != xamlType.PreferredXamlNamespace)
			{
				// If GetTrimmedNamespace returned a different string, it's a "using:"-prefixed namespace.
				// In this case, we'll have `baseTypeString` as
				// the fully qualified type name.
				// In this case, we go through this code path as it's much more efficient than FindType.
				var baseTypeString = $"{ns}.{xamlType.Name}";
				findType = _metadataHelper.FindTypeByFullName(baseTypeString) as INamedTypeSymbol;
				findType ??= _metadataHelper.FindTypeByFullName(baseTypeString + "Extension") as INamedTypeSymbol; // Support shortened syntax
			}
			else
			{
				// It looks like FindType always returns null in this code path.
				// So, we avoid the costly call here.
				return null;
			}

			if (findType?.Is(Generation.MarkupExtensionSymbol.Value) ?? false)
			{
				return findType;
			}

			return null;
		}

		private bool IsCustomMarkupExtensionType(XamlType? xamlType) =>
			// Determine if the type is a custom markup extension
			GetMarkupExtensionType(xamlType) != null;

		private bool IsXamlTypeConverter(INamedTypeSymbol? symbol)
		{
			return symbol?.GetAttributes().Any(a => a.AttributeClass?.Equals(Generation.CreateFromStringAttributeSymbol.Value, SymbolEqualityComparer.Default) == true) == true;
		}

		private string BuildXamlTypeConverterLiteralValue(INamedTypeSymbol? symbol, string memberValue, bool includeQuotations)
		{
			var attributeData = symbol.FindAttribute(Generation.CreateFromStringAttributeSymbol.Value);
			var targetMethod = attributeData?.NamedArguments.FirstOrDefault(kvp => kvp.Key == "MethodName").Value.Value?.ToString();

			if (targetMethod == null)
			{
				throw new NotSupportedException($"Unable to find MethodNameproperty on {symbol}");
			}

			// Since the MethodName value can simply be the name of the method
			// without its full path, example: nameof(MyConversionMethod), we must
			// make sure to fully qualify the method name with its namespace
			var fullyQualifiedOwnerType = !targetMethod.Contains('.')
				? $"{symbol?.GetFullyQualifiedTypeIncludingGlobal()}.{targetMethod}"
				: GetGlobalizedTypeName(targetMethod);

			var literalValue = default(string);
			if (includeQuotations)
			{
				// Return a string that contains the code which calls the "conversion" function with the member value
				literalValue = "{0}(\"{1}\")".InvariantCultureFormat(fullyQualifiedOwnerType, memberValue);
			}
			else
			{
				// Return a string that contains the code which calls the "conversion" function with the member value.
				// By not including quotations, this allows us to use static resources instead of string values.
				literalValue = "{0}({1})".InvariantCultureFormat(fullyQualifiedOwnerType, memberValue);
			}

			TryAnnotateWithGeneratorSource(ref literalValue);
			return literalValue;
		}

		private XamlMemberDefinition? FindMember(XamlObjectDefinition xamlObjectDefinition, string memberName)
		{
			foreach (var member in xamlObjectDefinition.Members)
			{
				if (member.Member.Name == memberName)
				{
					return member;
				}
			}

			return null;
		}

		private XamlMemberDefinition? FindMember(XamlObjectDefinition xamlObjectDefinition, string memberName, string ns)
		{
			foreach (var member in xamlObjectDefinition.Members)
			{
				if (member.Member.Name == memberName && member.Member.PreferredXamlNamespace == ns)
				{
					return member;
				}
			}

			return null;
		}

		private XamlMemberDefinition GetMember(XamlObjectDefinition xamlObjectDefinition, string memberName)
		{
			var member = FindMember(xamlObjectDefinition, memberName);

			if (member == null)
			{
				throw new InvalidOperationException($"Unable to find {memberName} on {xamlObjectDefinition}");
			}

			return member;
		}

		private XClassName GetClassName(XamlObjectDefinition control)
		{
			var classMember = FindClassName(control);

			if (classMember == null)
			{
				throw new Exception("Unable to find class name for toplevel control");
			}

			return classMember;
		}

		private XClassName? FindClassName(XamlObjectDefinition control)
		{
			var classMember = control.Members.FirstOrDefault(m => m.Member.Name == "Class");

			if (classMember?.Value != null)
			{
				var fullName = classMember.Value.ToString() ?? "";

				var index = fullName.LastIndexOf('.');

				return new(fullName.Substring(0, index), fullName.Substring(index + 1), _metadataHelper.FindTypeByFullName(fullName) as INamedTypeSymbol);
			}
			else
			{
				return null;
			}
		}

		internal static INamedTypeSymbol? FindClassSymbol(XamlObjectDefinition control, RoslynMetadataHelper metadataHelper)
		{
			var classMember = control.Members.FirstOrDefault(m => m.Member.Name == "Class");

			if (classMember?.Value != null)
			{
				var fullName = classMember.Value.ToString() ?? "";
				return metadataHelper.FindTypeByFullName(fullName) as INamedTypeSymbol;
			}
			else
			{
				return null;
			}
		}

		[MemberNotNull(nameof(_xClassName))]
		private void EnsureXClassName()
		{
			if (_xClassName == null)
			{
				throw new Exception($"Unable to find x:Class on the top level element");
			}
		}

		private bool BuildProperties(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl, bool isInline = true, string? closureName = null, bool useBase = false)
		{
			TryAnnotateWithGeneratorSource(writer);
			try
			{
				BuildSourceLineInfo(writer, topLevelControl);

				if (topLevelControl.Members.Count > 0)
				{
					var setterPrefix = string.IsNullOrWhiteSpace(closureName) ? string.Empty : closureName + ".";

					var implicitContentChild = FindImplicitContentMember(topLevelControl);

					if (implicitContentChild != null)
					{
						var topLevelControlSymbol = FindType(topLevelControl.Type);
						if (IsTextBlock(topLevelControlSymbol))
						{
							if (IsPropertyLocalized(topLevelControl, "Text"))
							{
								// A localized value is available. Ignore this implicit content as localized resources take precedence over XAML.
								return true;
							}

							if (implicitContentChild.Objects.Count != 0)
							{
								using (writer.BlockInvariant("{0}Inlines = ", setterPrefix))
								{
									foreach (var child in implicitContentChild.Objects)
									{
										BuildChild(writer, implicitContentChild, child);
										writer.AppendLineIndented(",");
									}
								}
							}
							else if (implicitContentChild.Value != null)
							{
								var escapedString = DoubleEscape(implicitContentChild.Value.ToString());
								writer.AppendIndented($"{setterPrefix}Text = \"{escapedString}\"");
							}
						}
						else if (IsRun(topLevelControlSymbol))
						{
							if (IsPropertyLocalized(topLevelControl, "Text"))
							{
								// A localized value is available. Ignore this implicit content as localized resources take precedence over XAML.
								return true;
							}

							if (implicitContentChild.Value != null)
							{
								var escapedString = DoubleEscape(implicitContentChild.Value.ToString());
								writer.AppendIndented($"{setterPrefix}Text = \"{escapedString}\"");
							}
						}
						else if (IsSpan(topLevelControlSymbol))
						{
							if (IsPropertyLocalized(topLevelControl, "Text"))
							{
								// A localized value is available. Ignore this implicit content as localized resources take precedence over XAML.
								return true;
							}

							if (implicitContentChild.Objects.Count != 0)
							{
								using (writer.BlockInvariant("{0}Inlines = ", setterPrefix))
								{
									foreach (var child in implicitContentChild.Objects)
									{
										BuildChild(writer, implicitContentChild, child);
										writer.AppendLineIndented(",");
									}
								}
							}
							else if (implicitContentChild.Value != null)
							{
								using (writer.BlockInvariant("{0}Inlines = ", setterPrefix))
								{
									var escapedString = DoubleEscape(implicitContentChild.Value.ToString());
									writer.AppendIndented("new Run { Text = \"" + escapedString + "\" }");
								}
							}
						}
						else if (IsPage(topLevelControlSymbol))
						{
							if (implicitContentChild.Objects.Count > 0)
							{
								writer.AppendLineInvariantIndented("{0}Content = ", setterPrefix);

								BuildChild(writer, implicitContentChild, implicitContentChild.Objects.First());
							}
						}
						else if (IsBorder(topLevelControlSymbol))
						{
							if (implicitContentChild.Objects.Count > 0)
							{
								if (implicitContentChild.Objects.Count > 1)
								{
									throw new InvalidOperationException("The type {0} does not support multiple children.".InvariantCultureFormat(topLevelControl.Type.Name));
								}

								writer.AppendLineInvariantIndented("{0}Child = ", setterPrefix);

								var implicitContent = implicitContentChild.Objects.First();
								using (TryAdaptNative(writer, implicitContent, Generation.UIElementSymbol.Value))
								{
									BuildChild(writer, implicitContentChild, implicitContent);
								}
							}
						}
						else if (IsType(topLevelControlSymbol, Generation.SolidColorBrushSymbol.Value))
						{
							if (implicitContentChild.Value is string content && !content.IsNullOrWhiteSpace())
							{
								writer.AppendLineIndented($"{setterPrefix}Color = {BuildColor(content)}");
							}
						}
						// WinUI assigned ContentProperty syntax
						else if (
							(IsType(topLevelControlSymbol, Generation.RowDefinitionSymbol.Value) ||
							IsType(topLevelControlSymbol, Generation.ColumnDefinitionSymbol.Value)) &&
							implicitContentChild.Value is string content &&
							!content.IsNullOrWhiteSpace())
						{
							var propertyName = topLevelControl.Type.Name == "ColumnDefinition"
								? "Width"
								: "Height";

							writer.AppendLineInvariantIndented("{0} = {1}", propertyName, BuildGridLength(content));
						}
						else if (topLevelControlSymbol is not null && IsInitializableCollection(topLevelControl, topLevelControlSymbol))
						{
							if (IsDictionary(topLevelControlSymbol))
							{
								foreach (var child in implicitContentChild.Objects)
								{
									if (GetMember(child, "Key") is var keyDefinition)
									{
										using (writer.BlockInvariant(""))
										{
											writer.AppendLineIndented($"\"{keyDefinition.Value}\"");
											writer.AppendLineIndented(",");
											BuildChild(writer, implicitContentChild, child);
										}
									}
									else
									{
										GenerateError(writer, "Unable to find the x:Key property");
									}

									writer.AppendLineIndented(",");
								}
							}
							else
							{
								foreach (var child in implicitContentChild.Objects)
								{
									BuildChild(writer, implicitContentChild, child);
									writer.AppendLineIndented($", /* IsInitializableCollection {topLevelControlSymbol.GetFullyQualifiedTypeExcludingGlobal()} */");
								}
							}
						}
						else // General case for implicit content
						{
							if (topLevelControlSymbol != null)
							{
								var contentProperty = FindContentProperty(topLevelControlSymbol);

								if (contentProperty != null)
								{
									if (IsCollectionOrListType(contentProperty.Type as INamedTypeSymbol))
									{
										if (IsInitializableProperty(contentProperty))
										{
											string contentPropertyName = useBase ? $"base.{contentProperty.Name}" : contentProperty.Name;
											if (isInline)
											{
												using (writer.BlockInvariant($"{contentPropertyName} = "))
												{
													foreach (var child in implicitContentChild.Objects)
													{
														BuildChild(writer, implicitContentChild, child);
														writer.AppendLineIndented(",");
													}
												}
											}
											else
											{
												foreach (var inner in implicitContentChild.Objects)
												{
													writer.AppendIndented($"{contentPropertyName}.Add(");

													BuildChild(writer, implicitContentChild, inner);

													writer.AppendLineIndented(");");
												}
											}
										}
										else if (IsNewableProperty(contentProperty, out var newableTypeName))
										{
											if (string.IsNullOrWhiteSpace(newableTypeName))
											{
												throw new InvalidOperationException("Unable to initialize newable collection type.  Type name for property {0} is empty."
													.InvariantCultureFormat(contentProperty.Name));
											}

											// Explicitly instantiate the collection and set it using the content property
											if (implicitContentChild.Objects.Count == 1
												&& SymbolEqualityComparer.Default.Equals(contentProperty.Type, FindType(implicitContentChild.Objects[0].Type)))
											{
												writer.AppendIndented(contentProperty.Name + " = ");
												BuildChild(writer, implicitContentChild, implicitContentChild.Objects[0]);
												writer.AppendLineIndented(",");
											}
											else
											{
												using (writer.BlockInvariant("{0}{1} = new {2} ", setterPrefix, contentProperty.Name, newableTypeName))
												{
													foreach (var child in implicitContentChild.Objects)
													{
														BuildChild(writer, implicitContentChild, child);
														writer.AppendLineIndented(",");
													}
												}
											}
										}
										else if (GetKnownNewableListOrCollectionInterface(contentProperty.Type as INamedTypeSymbol, out newableTypeName))
										{
											if (string.IsNullOrWhiteSpace(newableTypeName))
											{
												throw new InvalidOperationException("Unable to initialize known newable collection or list interface.  Type name for property {0} is empty."
													.InvariantCultureFormat(contentProperty.Name));
											}

											using (writer.BlockInvariant("{0}{1} = new {2} ", setterPrefix, contentProperty.Name, newableTypeName))
											{
												foreach (var child in implicitContentChild.Objects)
												{
													BuildChild(writer, implicitContentChild, child);
													writer.AppendLineIndented(",");
												}
											}
											writer.AppendLineIndented(",");
										}
										else
										{
											throw new InvalidOperationException("There is no known way to initialize collection property {0}."
												.InvariantCultureFormat(contentProperty.Name));
										}
									}
									else if (IsLazyVisualStateManagerProperty(contentProperty) && !HasDescendantsWithXName(implicitContentChild))
									{
										writer.AppendLineIndented($"/* Lazy VisualStateManager property {contentProperty}*/");
									}
									else // Content is not a collection
									{
										var objectUid = GetObjectUid(topLevelControl);
										var isLocalized = objectUid != null &&
											IsLocalizablePropertyType(contentProperty.Type as INamedTypeSymbol) &&
											BuildLocalizedResourceValue(null, contentProperty.Name, objectUid) != null;

										if (implicitContentChild.Objects.Count > 0 &&
											// A localized value is available. Ignore this implicit content as localized resources take precedence over XAML.
											!isLocalized)
										{
											// At the time of writing, if useBase is true, setterPrefix will be empty.
											// This assertion serves as reminder to re-evaluate the logic if setterPrefix becomes a non-null.
											Debug.Assert(!useBase || setterPrefix.Length == 0);
											writer.AppendLineIndented((useBase ? "base." : string.Empty) + setterPrefix + contentProperty.Name + " = ");

											if (implicitContentChild.Objects.Count > 1)
											{
												throw new InvalidOperationException("The type {0} does not support multiple children.".InvariantCultureFormat(topLevelControl.Type.Name));
											}

											var xamlObjectDefinition = implicitContentChild.Objects.First();
											using (TryAdaptNative(writer, xamlObjectDefinition, contentProperty.Type as INamedTypeSymbol))
											{
												BuildChild(writer, implicitContentChild, xamlObjectDefinition);
											}

											if (isInline)
											{
												writer.AppendLineIndented(",");
											}
										}
										else if (
											implicitContentChild.Value is string implicitValue
											&& !implicitValue.IsNullOrWhiteSpace()
										)
										{
											writer.AppendLineIndented(setterPrefix + contentProperty.Name + " = " + SyntaxFactory.Literal(implicitValue).ToString());

											if (isInline)
											{
												writer.AppendLineIndented(",");
											}
										}
									}
								}
							}
						}

						return true;
					}
				}

				return false;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new XamlParsingException(
					$"An error was found in {topLevelControl.Type.Name}"
					, e
					, topLevelControl.LineNumber
					, topLevelControl.LinePosition
					, _fileDefinition.FilePath
				);
			}
		}

		private IPropertySymbol? FindContentProperty(INamedTypeSymbol elementType) => _metadataHelper.FindContentProperty(elementType);

		private INamedTypeSymbol GetAttachedPropertyType(INamedTypeSymbol type, string propertyName) => _metadataHelper.GetAttachedPropertyType(type, propertyName);

		private bool IsTypeImplemented(INamedTypeSymbol type) => _metadataHelper.IsTypeImplemented(type);

		private string GetFullGenericTypeName(INamedTypeSymbol? propertyType)
		{
			if (propertyType == null)
			{
				return string.Empty;
			}

			return "{0}.{1}".InvariantCultureFormat(propertyType.ContainingNamespace, propertyType.Name) +
				(
					propertyType.IsGenericType ?
					"<{0}>".InvariantCultureFormat(propertyType.TypeArguments.Select(ts => GetFullGenericTypeName(ts as INamedTypeSymbol)).JoinBy(",")) :
					string.Empty
				);
		}

		private bool IsPage(INamedTypeSymbol? symbol) => IsType(symbol, Generation.NativePageSymbol.Value);

		private bool IsWindow(INamedTypeSymbol? symbol) => IsType(symbol, Generation.WindowSymbol.Value);

		private bool IsApplication(INamedTypeSymbol symbol) => IsType(symbol, Generation.ApplicationSymbol.Value);

		private bool IsResourceDictionary(XamlType xamlType) => IsType(xamlType, Generation.ResourceDictionarySymbol.Value);

		private bool IsResourceDictionarySubclass(XamlType xamlType) => xamlType.Name != "ResourceDictionary" && IsResourceDictionary(xamlType);

		private XamlMemberDefinition? FindImplicitContentMember(XamlObjectDefinition topLevelControl, string memberName = "_UnknownContent")
		{
			return topLevelControl
				.Members
				.FirstOrDefault(m => m.Member.Name == memberName);
		}

		/// <summary>
		/// Processes a Resources node, creating a ResourceDictionary if needed, and saving static resources for later registration.
		/// </summary>
		/// <param name="isInInitializer">True if inside an object initialization</param>
		private void RegisterAndBuildResources(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl, bool isInInitializer)
		{
			TryAnnotateWithGeneratorSource(writer);
			var resourcesMember = topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "Resources");

			if (resourcesMember != null)
			{
				// To be able to have MergedDictionaries, the first node of the Resource node
				// must be an explicit resource dictionary.
				var isExplicitResDictionary = resourcesMember.Objects.Any(o => o.Type.Name == "ResourceDictionary");
				var resourcesRoot = isExplicitResDictionary
					? FindImplicitContentMember(resourcesMember.Objects.First())
					: resourcesMember;
				var mergedDictionaries = isExplicitResDictionary ?
					resourcesMember.Objects
						.First().Members
							.Where(m => m.Member.Name == "MergedDictionaries")
							.FirstOrDefault()
					: null;
				var themeDictionaries = isExplicitResDictionary ?
					resourcesMember.Objects
						.First().Members
							.Where(m => m.Member.Name == "ThemeDictionaries")
							.FirstOrDefault()
					: null;

				var source = resourcesMember
					.Objects
					.FirstOrDefault(o => o.Type.Name == "ResourceDictionary")?
					.Members
					.FirstOrDefault(m => m.Member.Name == "Source");

				var rdSubclass = resourcesMember.Objects
					.FirstOrDefault(o => IsResourceDictionarySubclass(o.Type));

				if (rdSubclass != null)
				{
					writer.AppendLineIndented("Resources = ");

					BuildTypedResourceDictionary(writer, rdSubclass);

					writer.AppendLineIndented(isInInitializer ? "," : ";");
				}
				else if (resourcesRoot != null || mergedDictionaries != null || themeDictionaries != null)
				{
					if (isInInitializer)
					{
						writer.AppendLineIndented("Resources = {");
					}

					BuildMergedDictionaries(writer, mergedDictionaries, isInInitializer, dictIdentifier: "Resources");
					BuildThemeDictionaries(writer, themeDictionaries, isInInitializer, dictIdentifier: "Resources");
					BuildResourceDictionary(writer, resourcesRoot, isInInitializer, dictIdentifier: "Resources");

					if (isInInitializer)
					{
						writer.AppendLineIndented("},");
					}
				}
				else if (source != null)
				{
					writer.AppendLineIndented("Resources = ");
					BuildDictionaryFromSource(writer, source, dictObject: null);
					writer.AppendLineIndented(isInInitializer ? "," : ";");
				}
			}
		}

		/// <summary>
		/// Build resources declarations inside a resource dictionary.
		/// </summary>
		/// <param name="writer">The StringBuilder</param>
		/// <param name="resourcesRoot">The xaml member within which resources are declared</param>
		/// <param name="isInInitializer">Whether we're within an object initializer</param>
		private void BuildResourceDictionary(IIndentedStringBuilder writer, XamlMemberDefinition? resourcesRoot, bool isInInitializer, string? dictIdentifier = null)
		{
			TryAnnotateWithGeneratorSource(writer);
			var closingPunctuation = isInInitializer ? "," : ";";

			foreach (var resource in (resourcesRoot?.Objects).Safe())
			{
				TryAnnotateWithGeneratorSource(writer, suffix: "PerKey");
				var key = GetDictionaryResourceKey(resource, out var name);

				if (key != null)
				{
					var wrappedKey = key;
					if (!key.StartsWith("typeof(", StringComparison.InvariantCulture))
					{
						wrappedKey = "\"{0}\"".InvariantCultureFormat(key);
					}

					if (isInInitializer)
					{
						writer.AppendLineIndented("[");
						using (TryTernaryForLinkerHint(writer, resource))
						{
							writer.AppendLineIndented(wrappedKey);
						}
						writer.AppendLineIndented("] = ");
					}
					else
					{
						writer.AppendLineInvariantIndented("{0}[", dictIdentifier);
						using (TryTernaryForLinkerHint(writer, resource))
						{
							writer.AppendLineIndented(wrappedKey);
						}
						writer.AppendLineIndented("] = ");
					}

					if (resource.Type.Name == "StaticResource")
					{
						BuildStaticResourceResourceKeyReference(writer, resource);
					}
					else if (!ShouldLazyInitializeResource(resource))
					{
						BuildChild(writer, null, resource);
					}
					else if (_isTopLevelDictionary
						// Note: It's possible to be in a top-level ResourceDictionary file but for initializerName to be null, eg for
						// a FrameworkElement.Resources declaration inside of a template
						//
						// Actually in this case if it's non-null it's worse still, eg if the main dictionary sets an implicit style for
						// Button, and a FrameworkElement.Resources declaration in a template *also* sets an implicit Button style, we
						// don't want to use the former as a backing for the latter
						&& !_isInChildSubclass
						&& GetResourceDictionaryInitializerName(key) is string initializerName)
					{
						using (TryTernaryForLinkerHint(writer, resource))
						{
							writer.AppendLineInvariantIndented("new global::Uno.UI.Xaml.WeakResourceInitializer(this, {0})", initializerName);
						}
					}
					else
					{
						using (TryTernaryForLinkerHint(writer, resource))
						{
							using (BuildLazyResourceInitializer(writer))
							{
								BuildChild(writer, null, resource);
							}
						}
					}

					writer.AppendLineIndented(closingPunctuation);
				}

				if (name != null && !_isTopLevelDictionary)
				{
					if (_namedResources.ContainsKey(name))
					{
						throw new InvalidOperationException($"There is already a resource with name {name}");
					}
					_namedResources.Add(name, resource);
				}
			}
		}

		private IDisposable TryTernaryForLinkerHint(IIndentedStringBuilder writer, XamlObjectDefinition resource)
			=> TryLinkerHint(
				resource,
				s => writer.AppendLineIndented($"{s} ? ("),
				() => writer.AppendLineIndented("): null")
			);

		private IDisposable TrySingleLineIfForLinkerHint(IIndentedStringBuilder writer, XamlObjectDefinition resource)
			=> TryLinkerHint(
				resource,
				s => writer.AppendLineIndented($"if({s})"),
				() => { }
			);

		private IDisposable TryLinkerHint(XamlObjectDefinition resource, Action<string> prefix, Action suffix)
		{
			var hintEnabled = false;

			if (_xamlResourcesTrimming)
			{
				var styleTargetType = (resource.Type.Name == "Style" || resource.Type.Name == "ControlTemplate")
						? resource.Members.FirstOrDefault(m => m.Member.Name == "TargetType")?.Value as string
						: null;

				if (styleTargetType != null)
				{
					var symbol = GetType(styleTargetType);

					if (symbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, Generation.DependencyObjectSymbol.Value)))
					{
						var safeTypeName = LinkerHintsHelpers.GetPropertyAvailableName(symbol.GetFullMetadataName());
						var linkerHintClass = LinkerHintsHelpers.GetLinkerHintsClassName(_defaultNamespace);

						prefix($"global::{linkerHintClass}.{safeTypeName}");

						hintEnabled = true;
					}
				}
			}

			return Disposable.Create(() =>
			{
				if (hintEnabled)
				{
					suffix();
				}
			});
		}

		/// <summary>
		/// Whether this resource should be lazily initialized.
		/// </summary>
		private bool ShouldLazyInitializeResource(XamlObjectDefinition resource)
		{
			var typeName = resource.Type.Name;
			var symbol = GetType(resource.Type);

			if (resource.Members.Any(HasXNameProperty)

				// Disable eager materialization for internal resource dictionaries
				// where x:Name members are not used
				&& !_isTopLevelDictionary && !_isUnoAssembly && !_isUnoFluentAssembly)
			{
				return false;
			}

			if (!IsTypeImplemented(symbol))
			{
				// Lazily initialize not-implemented types, since there's no point creating them except to surface an error if explicitly required
				return true;
			}

			if (
				typeName == "Style"
				|| typeName == "ControlTemplate"
				|| typeName == "DataTemplate"
				|| symbol.Is(Generation.BrushSymbol.Value)
			)
			{
				// Always lazily initialize these types, since they may be large or many being unused (e.g. brushes)
				return true;
			}

			// If value declaration contains no markup, we can safely create it eagerly. Otherwise, we will wrap it in a lazy initializer
			// to be able to handle lexically-forward resource references correctly.
			return HasDescendantsWithMarkupExtension(resource);
		}

		/// <summary>
		/// Wrap ResourceDictionary resource in a lazy-initializer function.
		/// </summary>
		private IDisposable BuildLazyResourceInitializer(IIndentedStringBuilder writer)
		{
			var currentScope = CurrentResourceOwnerName;
			var resourceOwnerScope = ResourceOwnerScope();

			writer.AppendLineIndented($"new global::Uno.UI.Xaml.WeakResourceInitializer({currentScope}, {CurrentResourceOwner} => ");

			var indent = writer.Indent();

			return new DisposableAction(() =>
			{
				resourceOwnerScope.Dispose();
				indent.Dispose();
				writer.AppendLineIndented(")");
			});
		}

		/// <summary>
		/// Populate MergedDictionaries property of a ResourceDictionary.
		/// </summary>
		private void BuildMergedDictionaries(IIndentedStringBuilder writer, XamlMemberDefinition? mergedDictionaries, bool isInInitializer, string? dictIdentifier = null, bool isSetExtensionMethod = false)
		{
			TryAnnotateWithGeneratorSource(writer);
			BuildDictionaryCollection(writer, mergedDictionaries, isInInitializer, propertyName: "MergedDictionaries", isDict: false, dictIdentifier, isSetExtensionMethod);
		}

		/// <summary>
		/// Populate ThemeDictionaries property of a ResourceDictionary.
		/// </summary>
		private void BuildThemeDictionaries(IIndentedStringBuilder writer, XamlMemberDefinition? themeDictionaries, bool isInInitializer, string? dictIdentifier = null, bool isSetExtensionMethod = false)
		{
			TryAnnotateWithGeneratorSource(writer);
			BuildDictionaryCollection(writer, themeDictionaries, isInInitializer, propertyName: "ThemeDictionaries", isDict: true, dictIdentifier, isSetExtensionMethod);
		}

		/// <summary>
		/// Build a collection of ResourceDictionaries.
		/// </summary>
		private void BuildDictionaryCollection(IIndentedStringBuilder writer, XamlMemberDefinition? dictionaries, bool isInInitializer, string propertyName, bool isDict, string? dictIdentifier, bool isSetExtensionMethod)
		{
			TryAnnotateWithGeneratorSource(writer);
			if (dictionaries == null)
			{
				return;
			}

			if (isInInitializer)
			{
				writer.AppendLineInvariantIndented("{0} = {{", propertyName);
			}
			else
			{
				writer.AppendLineInvariantIndented("// {0}", propertyName);
			}

			for (int i = 0; i < dictionaries.Objects.Count; i++)
			{
				var dictObject = dictionaries.Objects[i];
				var source = dictObject.Members.FirstOrDefault(m => m.Member.Name == "Source");
				if (source != null && dictObject.Members.Any(m => m.Member.Name == "_UnknownContent"))
				{
					throw new Exception("Local values are not allowed in resource dictionary with Source set");
				}

				var key = GetDictionaryResourceKey(dictObject);
				if (isDict && key == null)
				{
					throw new Exception("Each dictionary entry must have an associated key.");
				}

				var former = _themeDictionaryCurrentlyBuilding; //Will 99% of the time be null.
				if (isDict)
				{
					_themeDictionaryCurrentlyBuilding = key;
				}

				if (!isSetExtensionMethod)
				{
					if (!isInInitializer && !isDict)
					{
						writer.AppendLineInvariantIndented("{0}.{1}.Add(", dictIdentifier, propertyName);
					}
					else if (!isInInitializer && isDict)
					{
						writer.AppendLineInvariantIndented("{0}.{1}[\"{2}\"] = ", dictIdentifier, propertyName, key);
					}
					else if (isInInitializer && isDict)
					{
						writer.AppendLineInvariantIndented("[\"{0}\"] = ", key);
					}
				}

				using (isDict ? BuildLazyResourceInitializer(writer) : null)
				{
					if (source != null)
					{
						BuildDictionaryFromSource(writer, source, dictObject);
					}
					else
					{
						InitializeAndBuildResourceDictionary(writer, dictObject, setIsParsing: false);
					}
				}

				if (isSetExtensionMethod)
				{
					if (i < dictionaries.Objects.Count - 1)
					{
						writer.AppendLineIndented(",");
					}
				}
				else if (isInInitializer)
				{
					writer.AppendLineIndented(",");
				}
				else if (!isDict)
				{
					writer.AppendLineIndented(");");
				}
				else
				{
					writer.AppendLineIndented(";");
				}

				_themeDictionaryCurrentlyBuilding = former;
			}

			if (isInInitializer && !isSetExtensionMethod)
			{
				writer.AppendLineIndented("},");
			}
		}

		/// <summary>
		/// Try to create a ResourceDictionary assignment from supplied Source property.
		/// </summary>
		private void BuildDictionaryFromSource(IIndentedStringBuilder writer, XamlMemberDefinition sourceDef, XamlObjectDefinition? dictObject)
		{
			TryAnnotateWithGeneratorSource(writer);
			var source = (sourceDef?.Value as string)?.Replace('\\', '/');

			var customResourceResourceId = GetCustomResourceResourceId(sourceDef);

			if (source == null && customResourceResourceId == null)
			{
				return;
			}

			var sourceProp = source != null ?
				_globalStaticResourcesMap.FindTargetPropertyForMergedDictionarySource(_fileDefinition, source) :
				null;

			if (sourceProp != null)
			{
				writer.AppendLineInvariantIndented("global::{0}.GlobalStaticResources.{1}", _defaultNamespace, sourceProp);
			}
			else
			{
				var innerSource = source != null ?
					"\"{0}\"".InvariantCultureFormat(source) :
					GetCustomResourceRetrieval(customResourceResourceId, "string");
				var currentAbsolutePath = _globalStaticResourcesMap.GetSourceLink(_fileDefinition);
				writer.AppendLineIndented("// Source not resolved statically, falling back on external resource retrieval.");
				writer.AppendLineInvariantIndented("global::Uno.UI.ResourceResolver.RetrieveDictionaryForSource({0}, \"{1}\")", innerSource, currentAbsolutePath);
			}

			if (dictObject is not null)
			{
				var mergedDictionaries = dictObject.Members.FirstOrDefault(m => m.Member.Name == "MergedDictionaries");
				if (mergedDictionaries is not null)
				{
					writer.AppendLineInvariantIndented(".AddMergedDictionaries(");
					BuildMergedDictionaries(writer, mergedDictionaries, isInInitializer: false, isSetExtensionMethod: true);
					writer.AppendLineInvariantIndented(")");
				}

				var themeDictionaries = dictObject.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries");
				if (themeDictionaries is not null)
				{
					writer.AppendLineIndented("#error Nested ThemeDictionaries are not yet supported.");
				}
			}
		}

		private bool IsTextBlock(INamedTypeSymbol? symbol)
		{
			return IsType(symbol, Generation.TextBlockSymbol.Value);
		}

		private void TryExtractAutomationId(XamlMemberDefinition member, string[] targetMembers, ref string? uiAutomationId)
		{
			if (!uiAutomationId.IsNullOrEmpty())
			{
				return;
			}

			var fullMemberName = $"{FindType(member.Member.DeclaringType)?.GetFullMetadataName()}.{member.Member.Name}";

			// Try to match each potential candidate by comparing based only on the name of the member first.
			// If that fails, try matching based on the full metadata name of the member
			var hasUiAutomationMapping = targetMembers
				.Any(candidateMember =>
					(!candidateMember.Contains(".") && candidateMember == member.Member.Name) ||
					candidateMember == fullMemberName
				);

			if (!hasUiAutomationMapping)
			{
				return;
			}

			var bindingMembers = member
				.Objects
				.FirstOrDefault(o => o.Type.Name == "Binding" || o.Type.Name == "TemplateBinding")
				?.Members
				.ToArray();

			string? bindingPath;

			// Checks the first binding member, which can be used to implicitlty declare the binding path (i.e. without
			// declaring a "Path=" specifier). Otherwise, the we look for any explicit binding path declaration.
			var firstBindingMember = bindingMembers?.FirstOrDefault();
			if (firstBindingMember != null &&
				(firstBindingMember.Member.Name == "Path" ||
				 firstBindingMember.Member.Name == "_PositionalParameters" ||
				 firstBindingMember.Member.Name.IsNullOrWhiteSpace())
			)
			{
				bindingPath = firstBindingMember.Value?.ToString();
			}
			else
			{
				bindingPath = bindingMembers
					?.FirstOrDefault(bindingMember => bindingMember.Member.Name == "Path")
					?.Value
					?.ToString();
			}

			if (bindingPath.IsNullOrEmpty())
			{
				return;
			}

			// Replace bracket-based notation with dot notation.
			// ex: [FirstMember][SecondMember] --> FirstMember.SecondMember
			uiAutomationId = new string(bindingPath
				.TrimStart("Parent")
				.Replace("][", ".")
				.Where(c => c != '[' && c != ']')
				.ToArray());
		}

		private void BuildExtendedProperties(IIndentedStringBuilder outerwriter, XamlObjectDefinition objectDefinition, bool useGenericApply = false)
		{
			_generatorContext.CancellationToken.ThrowIfCancellationRequested();

			TryAnnotateWithGeneratorSource(outerwriter);
			var objectUid = GetObjectUid(objectDefinition);

			var extendedProperties = GetExtendedProperties(objectDefinition);
			bool hasChildrenWithPhase = HasChildrenWithPhase(objectDefinition);
			var objectDefinitionType = FindType(objectDefinition.Type);
			var isFrameworkElement = IsType(objectDefinitionType, Generation.FrameworkElementSymbol.Value);
			var hasIsParsing = HasIsParsing(objectDefinitionType);

			if (extendedProperties.Any() || hasChildrenWithPhase || isFrameworkElement || hasIsParsing || !objectUid.IsNullOrEmpty())
			{
				string closureName;
				if (!useGenericApply && objectDefinitionType is null)
				{
					throw new InvalidOperationException("The type {0} could not be found".InvariantCultureFormat(objectDefinition.Type));
				}

				using (var writer = CreateApplyBlock(outerwriter, useGenericApply ? null : objectDefinitionType, out closureName))
				{
					XamlMemberDefinition? uidMember = null;
					XamlMemberDefinition? nameMember = null;
					ComponentDefinition? componentDefinition = null;
					string? uiAutomationId = null;
					string[]? extractionTargetMembers = null;

					if (_isUiAutomationMappingEnabled)
					{
						// Look up any potential UI automation member mappings available for the current object definition
						extractionTargetMembers = _uiAutomationMappings
							?.FirstOrDefault(m => _metadataHelper.FindTypeByFullName(m.Key) is INamedTypeSymbol mappingKeySymbol && (IsType(objectDefinitionType, mappingKeySymbol) || IsImplementingInterface(objectDefinitionType, mappingKeySymbol)))
							.Value
							?.ToArray() ?? Array.Empty<string>();
					}

					if (hasChildrenWithPhase)
					{
						writer.AppendIndented(GenerateRootPhases(objectDefinition, closureName) ?? "");
					}

					if (HasXBindMarkupExtension(objectDefinition) || HasMarkupExtensionNeedingComponent(objectDefinition))
					{
						writer.AppendLineIndented($"/* _isTopLevelDictionary:{_isTopLevelDictionary} */");
						var isInsideFrameworkTemplate = IsMemberInsideFrameworkTemplate(objectDefinition).isInside;
						if (!_isTopLevelDictionary || isInsideFrameworkTemplate)
						{
							componentDefinition = AddComponentForCurrentScope(objectDefinition);

							var componentName = componentDefinition.MemberName;

							writer.AppendLineIndented($"__that.{componentName} = {closureName};");

							if (isInsideFrameworkTemplate
								&& HasMarkupExtensionNeedingComponent(objectDefinition)
								&& IsDependencyObject(objectDefinition)
								&& !IsUIElement(objectDefinitionType))
							{
								// Ensure that the namescope is property propagated to instances
								// that are not UIElements in order for ElementName to resolve properly
								writer.AppendLineIndented($"global::Windows.UI.Xaml.NameScope.SetNameScope(__that.{componentName}, __nameScope);");
							}
						}
						else if (isFrameworkElement && HasMarkupExtensionNeedingComponent(objectDefinition))
						{
							// Register directly for binding updates on the resource referencer itself, since we're inside a top-level ResourceDictionary
							using (writer.BlockInvariant("{0}.Loading += (obj, args) =>", closureName))
							{
								writer.AppendLineIndented("((DependencyObject)obj).UpdateResourceBindings();");
							}
							writer.AppendLineIndented(";");
						}
					}

					var lazyProperties = extendedProperties.Where(IsLazyVisualStateManagerProperty).ToArray();

					foreach (var member in extendedProperties.Except(lazyProperties))
					{
						_generatorContext.CancellationToken.ThrowIfCancellationRequested();

						if (extractionTargetMembers != null)
						{
							TryExtractAutomationId(member, extractionTargetMembers, ref uiAutomationId);
						}

						if (HasMarkupExtension(member))
						{
							if (!IsXLoadMember(member))
							{
								TryValidateContentPresenterBinding(writer, objectDefinition, member);

								BuildComplexPropertyValue(writer, member, closureName + ".", closureName, componentDefinition: componentDefinition);
							}
							else
							{
								writer.AppendLineIndented($"/* Skipping x:Load attribute already applied to ElementStub */");
							}
						}
						else if (HasCustomMarkupExtension(member))
						{
							if (IsAttachedProperty(member) && FindPropertyType(member.Member) != null)
							{
								BuildSetAttachedProperty(writer, closureName, member, objectUid ?? "", isCustomMarkupExtension: true);
							}
							else
							{
								BuildCustomMarkupExtensionPropertyValue(writer, member, closureName);
							}
						}
						else if (member.Objects.Count > 0)
						{
							if (member.Member.Name == "_UnknownContent") // So : FindType(member.Owner.Type) is INamedTypeSymbol type && IsCollectionOrListType(type)
							{
								foreach (var item in member.Objects)
								{
									writer.AppendLineIndented($"{closureName}.Add(");
									using (writer.Indent())
									{
										if (objectDefinitionType?.AllInterfaces.Any(i => i.OriginalDefinition.Equals(Generation.IDictionaryOfTKeySymbol.Value, SymbolEqualityComparer.Default)) == true &&
											GetDictionaryResourceKey(item) is string dictionaryKey)
										{
											writer.AppendLineIndented($"\"{dictionaryKey}\",");
										}
										BuildChild(writer, member, item);
									}
									writer.AppendLineIndented(");");
								}
							}
							else if (!IsType(objectDefinition.Type, member.Member.DeclaringType))
							{
								var ownerType = GetType(member.Member.DeclaringType!);

								var propertyType = GetPropertyTypeByOwnerSymbol(ownerType, member.Member.Name, member.LineNumber, member.LinePosition);

								if (member.Objects.Count == 1 && member.Objects[0] is var child && IsAssignableTo(child.Type, propertyType))
								{
									writer.AppendLineInvariantIndented(
										"{0}.Set{1}({2}, ",
										ownerType.GetFullyQualifiedTypeIncludingGlobal(),
										member.Member.Name,
										closureName
									);

									using (writer.Indent())
									{
										BuildChild(writer, member, member.Objects[0]);
									}

									writer.AppendLineIndented(");");
								}
								else if (IsExactlyCollectionOrListType(propertyType))
								{
									// If the property is specifically an IList or an ICollection
									// we can use C#'s collection initializer.
									writer.AppendLineInvariantIndented(
										"{0}.Set{1}({2}, ",
										ownerType.GetFullyQualifiedTypeIncludingGlobal(),
										member.Member.Name,
										closureName
									);

									using (writer.BlockInvariant("new[]"))
									{
										foreach (var inner in member.Objects)
										{
											BuildChild(writer, member, inner);
											writer.AppendIndented(",");
										}
									}

									writer.AppendLineIndented(");");
								}
								else if (IsCollectionOrListType(propertyType))
								{
									// If the property is a concrete type that implements an IList or
									// an ICollection, we must get the property and call add explicitly
									// on it.
									var localCollectionName = $"{closureName}_collection_{_collectionIndex++}";

									var getterMethod = $"Get{member.Member.Name}";

									if (ownerType.GetFirstMethodWithName(getterMethod) is not null)
									{
										// Attached property
										writer.AppendLineIndented(
											$"var {localCollectionName} = {ownerType.GetFullyQualifiedTypeIncludingGlobal()}.{getterMethod}({closureName});"
										);
									}
									else
									{
										// Plain object
										writer.AppendLineIndented(
											$"var {localCollectionName} = {closureName}.{member.Member.Name};"
										);
									}

									foreach (var inner in member.Objects)
									{
										writer.AppendIndented($"{localCollectionName}.Add(");

										BuildChild(writer, member, inner);

										writer.AppendLineIndented(");");
									}
								}
								else if (member.Objects.Count == 1)
								{
									var childType = GetType(member.Objects[0].Type).GetFullyQualifiedTypeExcludingGlobal();
									throw new InvalidOperationException($"Cannot assign child of type '{childType}' to property of type '{propertyType.GetFullyQualifiedTypeExcludingGlobal()}'.'");
								}
								else
								{
									throw new InvalidOperationException($"The property {member.Member.Name} of type {propertyType} does not support adding multiple objects.");
								}
							}
							else
							{
								// GenerateWarning(writer, $"Unknown type {objectDefinition.Type} for property {member.Member.DeclaringType}");
							}
						}
						else
						{
							var isMemberInsideResourceDictionary = IsMemberInsideResourceDictionary(objectDefinition);
							var value = member.Value?.ToString();

							if (
								member.Member.Name == "Name"
								&& member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace
								&& !isMemberInsideResourceDictionary.isInside
							)
							{
								ValidateName(value);

								writer.AppendLineIndented($@"__nameScope.RegisterName(""{value}"", {closureName});");
							}

							if (
								member.Member.Name == "Name"
								&& !IsAttachedProperty(member)
								&& !isMemberInsideResourceDictionary.isInside
							)
							{
								nameMember = member;

								var type = FindType(objectDefinition.Type)?.GetFullyQualifiedTypeIncludingGlobal();

								if (type == null)
								{
									throw new InvalidOperationException($"Unable to find type {objectDefinition.Type}");
								}

								writer.AppendLineInvariantIndented("__that.{0} = {1};", value, closureName);
								// value is validated as non-null in ValidateName call above.
								RegisterBackingField(type, value!, FindObjectFieldAccessibility(objectDefinition));
							}
							else if (member.Member.Name == "Name"
								&& member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
							{
								writer.AppendLineInvariantIndented("// x:Name {0}", member.Value, member.Value);
							}
							else if (member.Member.Name == "Key")
							{
								writer.AppendLineInvariantIndented("// Key {0}", member.Value, member.Value);
							}
							else if (member.Member.Name == "DeferLoadStrategy"
								&& member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
							{
								writer.AppendLineInvariantIndented("// DeferLoadStrategy {0}", member.Value);
							}
							else if (IsXLoadMember(member))
							{
								writer.AppendLineInvariantIndented("// Load {0}", member.Value);
							}
							else if (member.Member.Name == "Uid")
							{
								uidMember = member;
								writer.AppendLineIndented($"{GlobalPrefix}Uno.UI.Helpers.MarkupHelper.SetXUid({closureName}, \"{objectUid}\");");
							}
							else if (member.Member.Name == "FieldModifier")
							{
								writer.AppendLineInvariantIndented("// FieldModifier {0}", member.Value);
							}
							else if (member.Member.Name == "Phase")
							{
								writer.AppendLineIndented($"{GlobalPrefix}Uno.UI.FrameworkElementHelper.SetRenderPhase({closureName}, {member.Value});");
							}
							else if (member.Member.Name == "DefaultBindMode"
								&& member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
							{
								writer.AppendLineInvariantIndented("// DefaultBindMode {0}", member.Value);
							}
							else if (member.Member.Name == "Class" && member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
							{
								writer.AppendLineInvariantIndented("// Class {0}", member.Value, member.Value);
							}
							else if (
								member.Member.Name == "TargetName" &&
								IsAttachedProperty(member) &&
								member.Member.DeclaringType?.Name == "Storyboard"
							)
							{
								if (member.Value == null)
								{
									throw new InvalidOperationException($"The TargetName property cannot be empty");
								}

								var memberGlobalizedType = FindType(member.Member.DeclaringType)?.GetFullyQualifiedTypeIncludingGlobal() ?? GetGlobalizedTypeName(member.Member.DeclaringType.Name);
								writer.AppendLineInvariantIndented(@"{0}.SetTargetName({2}, ""{1}"");",
									memberGlobalizedType,
									this.RewriteAttachedPropertyPath(member.Value.ToString() ?? ""),
									closureName);

								writer.AppendLineInvariantIndented("{0}.SetTarget({2}, _{1}Subject);",
									memberGlobalizedType,
									member.Value,
									closureName);
							}
							else if (
								member.Member.Name == "TargetName" &&
								!IsAttachedProperty(member) &&
								(member.Member.DeclaringType?.Name.EndsWith("ThemeAnimation", StringComparison.Ordinal) ?? false)
							)
							{
								// Those special animations (xxxThemeAnimation) needs to resolve their target at runtime.
								writer.AppendLineInvariantIndented(@"NameScope.SetNameScope({0}, __nameScope);", closureName);
							}
							else if (
								member.Member.Name == "TargetProperty" &&
								IsAttachedProperty(member) &&
								member.Member.DeclaringType?.Name == "Storyboard"
							)
							{
								if (member.Value == null)
								{
									throw new InvalidOperationException($"The TargetProperty property cannot be empty");
								}

								var memberGlobalizedType = FindType(member.Member.DeclaringType)?.GetFullyQualifiedTypeIncludingGlobal() ?? GetGlobalizedTypeName(member.Member.DeclaringType.Name);
								writer.AppendLineInvariantIndented(@"{0}.SetTargetProperty({2}, ""{1}"");",
									memberGlobalizedType,
									this.RewriteAttachedPropertyPath(member.Value.ToString() ?? ""),
									closureName);
							}
							else if (
								member.Member.DeclaringType?.Name == "RelativePanel" &&
								IsAttachedProperty(member) &&
								IsRelativePanelSiblingProperty(member.Member.Name)
							)
							{
								var memberGlobalizedType = FindType(member.Member.DeclaringType)?.GetFullyQualifiedTypeIncludingGlobal() ?? GetGlobalizedTypeName(member.Member.DeclaringType.Name);
								writer.AppendLineInvariantIndented(@"{0}.Set{1}({2}, _{3}Subject);",
									memberGlobalizedType,
									member.Member.Name,
									closureName,
									member.Value);
							}
							else
							{
								IEventSymbol? eventSymbol = null;
								var declaringTypeSymbol = FindType(member.Member.DeclaringType);
								if (
									!IsType(declaringTypeSymbol, objectDefinitionType)
									|| IsAttachedProperty(member)
									|| (eventSymbol = _metadataHelper.FindEventType(declaringTypeSymbol, member.Member.Name)) != null
								)
								{
									if (_metadataHelper.FindPropertyTypeByOwnerSymbol(declaringTypeSymbol, member.Member.Name) != null)
									{
										BuildSetAttachedProperty(writer, closureName, member, objectUid ?? "", isCustomMarkupExtension: false);
									}
									else if (eventSymbol != null)
									{
										GenerateInlineEvent(closureName, writer, member, eventSymbol, componentDefinition);
									}
									else
									{
										GenerateError(
											writer,
											$"Property {member.Member.PreferredXamlNamespace}:{member.Member} is not available on {member.Member.DeclaringType?.Name}, value is {member.Value}"
										);
									}
								}
							}
						}
					}

					var implicitContentChild = FindImplicitContentMember(objectDefinition);
					var lazyContentProperty = FindLazyContentProperty(implicitContentChild, objectDefinitionType);

					if (lazyProperties.Length > 0 || lazyContentProperty != null)
					{
						// This block is used to generate lazy initializations of some
						// inner VisualStateManager properties in VisualState and VisualTransition.
						// This allows for faster materialization of controls, avoiding the construction
						// of inferequently used objects graphs.

						using (writer.BlockInvariant($"global::Uno.UI.Helpers.MarkupHelper.Set{objectDefinition.Type.Name}Lazy({closureName}, () => "))
						{
							BuildLiteralLazyVisualStateManagerProperties(writer, objectDefinition, closureName);

							if (implicitContentChild != null && lazyContentProperty != null)
							{
								writer.AppendLineIndented($"{closureName}.{lazyContentProperty.Name} = ");

								var xamlObjectDefinition = implicitContentChild.Objects.First();
								using (TryAdaptNative(writer, xamlObjectDefinition, lazyContentProperty.Type as INamedTypeSymbol))
								{
									BuildChild(writer, implicitContentChild, xamlObjectDefinition);
								}
								writer.AppendLineIndented($";");
							}
						}
						writer.AppendLineIndented($");");
					}

					if (IsFrameworkElement(objectDefinition.Type))
					{
						if (_isHotReloadEnabled)
						{
							writer.AppendLineIndented(
								$"global::Uno.UI.FrameworkElementHelper.SetBaseUri(" +
								$"{closureName}, " +
								$"__baseUri_{_fileUniqueId}, " +
								$"\"file:///{_fileDefinition.FilePath.Replace("\\", "/")}\", " +
								$"{objectDefinition.LineNumber}, " +
								$"{objectDefinition.LinePosition}" +
								$");");
						}
						else
						{
							writer.AppendLineIndented($"global::Uno.UI.FrameworkElementHelper.SetBaseUri({closureName}, __baseUri_{_fileUniqueId});");
						}
					}

					if (IsNotFrameworkElementButNeedsSourceLocation(objectDefinition) && _isHotReloadEnabled)
					{
						TrySetOriginalSourceLocation(writer, $"{closureName}", objectDefinition.LineNumber, objectDefinition.LinePosition);
					}

					if (_isUiAutomationMappingEnabled)
					{
						// Prefer using the Uid or the Name if their value has been explicitly assigned
						var assignedUid = uidMember?.Value?.ToString();
						var assignedName = nameMember?.Value?.ToString();

						if (!assignedUid.IsNullOrEmpty())
						{
							uiAutomationId = assignedUid;
						}
						else if (!assignedName.IsNullOrEmpty())
						{
							uiAutomationId = assignedName;
						}

						BuildUiAutomationId(writer, closureName, uiAutomationId, objectDefinition);
					}

					BuildStatementLocalizedProperties(writer, objectDefinition, closureName);

					if (hasIsParsing)
					{
						// This should always be the last thing called when an element is parsed.
						writer.AppendLineInvariantIndented("{0}.CreationComplete();", closureName);
					}
				}
			}

			// Local function used to build a property/value for any custom MarkupExtensions
			void BuildCustomMarkupExtensionPropertyValue(IIndentedStringBuilder writer, XamlMemberDefinition member, string closure)
			{
				var propertyValue = GetCustomMarkupExtensionValue(member, closure);
				if (!propertyValue.IsNullOrEmpty())
				{
					writer.AppendIndented($"{closure}.{member.Member.Name} = {propertyValue};\r\n");
				}
			}
		}

		private static bool IsNotFrameworkElementButNeedsSourceLocation(XamlObjectDefinition objectDefinition)
			=> objectDefinition.Type.Name is "VisualState" or "AdaptiveTrigger" or "StateTrigger";

		private void ValidateName(string? value)
		{
			// Nullable suppressions are due to https://github.com/dotnet/roslyn/issues/55507
			if (!SyntaxFacts.IsValidIdentifier(value!))
			{
				throw new InvalidOperationException($"The value '{value}' is an invalid value for 'Name' property.");
			}

			if (!CurrentScope.DeclaredNames.Add(value!))
			{
				throw new InvalidOperationException($"The name '{value}' is already defined in this scope.");
			}
		}

		private IPropertySymbol? FindLazyContentProperty(XamlMemberDefinition? implicitContentChild, INamedTypeSymbol? elementType)
		{
			if (implicitContentChild != null && elementType != null)
			{
				var contentProperty = FindContentProperty(elementType);

				if (contentProperty != null && IsLazyVisualStateManagerProperty(contentProperty) && !HasDescendantsWithXName(implicitContentChild))
				{
					return contentProperty;
				}
			}

			return null;
		}

		private bool IsXLoadMember(XamlMemberDefinition member) =>
			member.Member.Name == "Load"
			&& member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace;

		private void GenerateInlineEvent(string? closureName, IIndentedStringBuilder writer, XamlMemberDefinition member, IEventSymbol eventSymbol, ComponentDefinition? componentDefinition = null)
		{
			// If a binding is inside a DataTemplate, the binding root in the case of an x:Bind is
			// the DataContext, not the control's instance.
			var isInsideDataTemplate = IsMemberInsideFrameworkTemplate(member.Owner);

			void writeEvent(string? ownerPrefix)
			{
				if (eventSymbol.Type is INamedTypeSymbol delegateSymbol)
				{
					var parmsNames = delegateSymbol
						.DelegateInvokeMethod
						?.Parameters
						.Select(p => member.Value + "_" + p.Name)
						.ToArray();

					var parms = parmsNames.JoinBy(",");

					var eventSource = !ownerPrefix.IsNullOrEmpty() ? ownerPrefix : "__that";

					if (member.Objects.FirstOrDefault() is XamlObjectDefinition bind && bind.Type.Name == "Bind")
					{
						CurrentScope.XBindExpressions.Add(bind);

						var eventTarget = XBindExpressionParser.RestoreSinglePath(bind.Members.First().Value?.ToString());

						if (eventTarget == null)
						{
							throw new InvalidOperationException("x:Bind event path cannot by empty");
						}

						var parts = eventTarget.Split('.').ToList();
						var isStaticTarget = parts.FirstOrDefault()?.Contains(":") ?? false;

						eventTarget = RewriteNamespaces(eventTarget);

						// x:Bind to second-level method generates invalid code
						// sanitizing member.Member.Name so that "ViewModel.SearchBreeds" becomes "ViewModel_SearchBreeds"
						var sanitizedEventTarget = SanitizeResourceName(eventTarget);

						(string target, string weakReference, IMethodSymbol targetMethod) buildTargetContext()
						{
							IMethodSymbol FindTargetMethodSymbol(INamedTypeSymbol? sourceType)
							{
								if (eventTarget.Contains("."))
								{
									ITypeSymbol? currentType = sourceType;

									if (isStaticTarget)
									{
										// First part is a type for static method binding and should
										// overide the original source type
										currentType = GetType(RewriteNamespaces(parts[0]));
										parts.RemoveAt(0);
									}

									for (var i = 0; i < parts.Count - 1; i++)
									{
										var next = currentType.GetAllMembersWithName(RewriteNamespaces(parts[i])).FirstOrDefault();

										currentType = next switch
										{
											IFieldSymbol fs => fs.Type,
											IPropertySymbol ps => ps.Type,
											null => throw new InvalidOperationException($"Unable to find member {parts[i]} on type {currentType}"),
											_ => throw new InvalidOperationException($"The field {next.Name} is not supported for x:Bind event binding")
										};
									}

									var method = currentType?.GetFirstMethodWithName(parts.Last(), includeBaseTypes: true)
										?? throw new InvalidOperationException($"Failed to find {parts.Last()} on {currentType}");

									return method;
								}
								else
								{
									return sourceType?.GetFirstMethodWithName(eventTarget, includeBaseTypes: true)
										?? throw new InvalidOperationException($"Failed to find {eventTarget} on {sourceType}");
								}
							}

							if (isInsideDataTemplate.isInside)
							{
								var dataTypeObject = FindMember(isInsideDataTemplate.xamlObject!, "DataType", XamlConstants.XamlXmlNamespace);

								if (dataTypeObject?.Value == null)
								{
									throw new Exception($"Unable to find x:DataType in enclosing DataTemplate for x:Bind event");
								}

								var dataTypeSymbol = GetType(dataTypeObject.Value.ToString() ?? "");

								return (
									$"({member.Member.Name}_{sanitizedEventTarget}_That.Target as {XamlConstants.Types.FrameworkElement})?.DataContext as {dataTypeSymbol.GetFullyQualifiedTypeIncludingGlobal()}",

									// Use of __rootInstance is required to get the top-level DataContext, as it may be changed
									// in the current visual tree by the user.
									$"(__rootInstance as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference",
									FindTargetMethodSymbol(dataTypeSymbol)
								);
							}
							else
							{
								if (_xClassName?.Symbol != null)
								{
									return (
										$"{member.Member.Name}_{sanitizedEventTarget}_That.Target as {_xClassName}",
										$"({eventSource} as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference",
										FindTargetMethodSymbol(_xClassName.Symbol)
									);
								}
								else
								{
									throw new Exception($"Unable to find the type {_xClassName?.Namespace}.{_xClassName?.ClassName}");
								}
							}
						}

						var targetContext = buildTargetContext();

						var targetMethodHasParamters = targetContext.targetMethod?.Parameters.Any() ?? false;
						var xBindParams = targetMethodHasParamters ? parms : "";

						var builderName = $"_{ownerPrefix}_{CurrentScope.xBindEventsHandlers.Count}_{member.Member.Name}_{sanitizedEventTarget}_Builder";

						EnsureXClassName();

						var ownerName = CurrentScope.ClassName;

						AddXBindEventHandlerToScope(builderName, ownerName, eventSymbol.ContainingType, componentDefinition);

						var handlerParameters = _isHotReloadEnabled ? "(__that, __owner)" : "(__owner)";

						using (writer.BlockInvariant($"__that.{builderName} = {handlerParameters} => "))
						{
							//
							// Generate a weak delegate, so the owner is not being held onto by the delegate. We can
							// use the WeakReferenceProvider to get a self reference to avoid adding the cost of the
							// creation of a WeakReference.
							//
							var thatVariableName = $"{member.Member.Name}_{sanitizedEventTarget}_That";
							writer.AppendLineIndented($"var {thatVariableName} = {targetContext.weakReference};");

							if (_isHotReloadEnabled)
							{
								// Attach the current context to the owner to avoid closing on "this" in the
								// handler of the event.
								writer.AppendLineIndented($"global::Uno.UI.Helpers.MarkupHelper.SetElementProperty(__owner, \"{eventTarget}\", {thatVariableName});");
							}

							using (writer.BlockInvariant($"/* first level targetMethod:{targetContext.targetMethod} */ __owner.{member.Member.Name} += ({parms}) => "))
							{
								if (isStaticTarget)
								{
									writer.AppendLineIndented($"{eventTarget}({xBindParams});");
								}
								else
								{
									if (_isHotReloadEnabled && parmsNames is { Length: > 0 })
									{
										writer.AppendLineIndented(
											$"var {thatVariableName} = /* {delegateSymbol.GetFullyQualifiedTypeExcludingGlobal()} */" +
											$"global::Uno.UI.Helpers.MarkupHelper.GetElementProperty<global::Uno.UI.DataBinding.ManagedWeakReference>({parmsNames[0]}, \"{eventTarget}\");");
									}

									writer.AppendLineIndented($"({targetContext.target})?.{eventTarget}({xBindParams});");
								}
							}
							writer.AppendLineIndented($";");
						}

						writer.AppendLineIndented($";");
					}
					else
					{
						EnsureXClassName();

						// x:Bind to second-level method generates invalid code
						// sanitizing member.Value so that "ViewModel.SearchBreeds" becomes "ViewModel_SearchBreeds"
						var sanitizedMemberValue = SanitizeResourceName(member.Value?.ToString());

						//
						// Generate a weak delegate, so the owner is not being held onto by the delegate. We can
						// use the WeakReferenceProvider to get a self reference to avoid adding the cost of the
						// creation of a WeakReference.
						//
						writer.AppendLineIndented($"var {member.Member.Name}_{sanitizedMemberValue}_That = ({eventSource} as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference;");

						writer.AppendLineIndented($"/* second level */ {closureName}.{member.Member.Name} += ({parms}) => ({member.Member.Name}_{sanitizedMemberValue}_That.Target as {_xClassName})?.{member.Value}({parms});");
					}
				}
				else
				{
					GenerateError(writer, $"{eventSymbol.Type} is not a supported event");
				}
			}

			if (!isInsideDataTemplate.isInside)
			{
				writeEvent("");
			}
			else if (_xClassName?.ClassName != null)
			{
				writeEvent(CurrentResourceOwner);
			}
			else
			{
				GenerateError(writer, $"Unable to use event {member.Member.Name} without a backing class (use x:Class)");
			}
		}

		/// <summary>
		/// Build localized properties which have not been set in the xaml.
		/// </summary>
		private void BuildInlineLocalizedProperties(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, INamedTypeSymbol? objectDefinitionType)
		{
			TryAnnotateWithGeneratorSource(writer);
			var objectUid = GetObjectUid(objectDefinition);

			if (objectUid != null)
			{
				var candidateProperties = FindLocalizableProperties(objectDefinitionType)
					.Except(objectDefinition.Members.Select(m => m.Member.Name));
				foreach (var prop in candidateProperties)
				{
					var localizedValue = BuildLocalizedResourceValue(null, prop, objectUid);
					if (localizedValue != null)
					{
						writer.AppendLineInvariantIndented("{0} = {1},", prop, localizedValue);
					}
				}
			}
		}

		/// <summary>
		/// Build localized properties which have not been set in the xaml.
		/// </summary>
		private void BuildStatementLocalizedProperties(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, string closureName)
		{
			TryAnnotateWithGeneratorSource(writer);
			var objectUid = GetObjectUid(objectDefinition);

			if (objectUid != null)
			{
				var candidateAttachedProperties = FindLocalizableAttachedProperties(objectUid);
				foreach (var candidate in candidateAttachedProperties)
				{
					var localizedValue = BuildLocalizedResourceValue(candidate.ownerType, candidate.property, objectUid);
					if (localizedValue != null)
					{
						var propertyType = GetAttachedPropertyType(candidate.ownerType, candidate.property).GetFullyQualifiedTypeIncludingGlobal();
						var convertedLocalizedProperty =
							$"({propertyType})global::Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({propertyType}),{localizedValue})";

						writer.AppendLineInvariantIndented($"{candidate.ownerType.GetFullyQualifiedTypeIncludingGlobal()}.Set{candidate.property}({closureName}, {convertedLocalizedProperty});");
					}
				}
			}
		}

		private void TryValidateContentPresenterBinding(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, XamlMemberDefinition member)
		{
			TryAnnotateWithGeneratorSource(writer);
			if (
				SymbolEqualityComparer.Default.Equals(FindType(objectDefinition.Type), Generation.ContentPresenterSymbol.Value)
				&& member.Member.Name == "Content"
			)
			{
				var binding = member.Objects.FirstOrDefault(o => o.Type.Name == "Binding");

				if (binding != null)
				{
					var hasRelativeSource = binding.Members
						.Any(m =>
							m.Member.Name == "RelativeSource"
						// It can either be TemplatedParent or Self. In either cases, it does not use the inherited
						// DataContext, which falls outside of the scenario we want to avoid.
						);

					if (!hasRelativeSource)
					{
						writer.AppendLine();
						writer.AppendIndented("#error Using a non-template binding expression on Content " +
							"will likely result in an undefined runtime behavior, as ContentPresenter.Content overrides " +
							"the local value of ContentPresenter.DataContent. " +
							"Use ContentControl instead if you really need a normal binding on Content.\n");
					}
				}
			}
		}

		private void BuildUiAutomationId(IIndentedStringBuilder writer, string closureName, string? uiAutomationId, XamlObjectDefinition parent)
		{
			TryAnnotateWithGeneratorSource(writer);
			if (uiAutomationId.IsNullOrEmpty())
			{
				return;
			}

			writer.AppendLineInvariantIndented("// UI automation id: {0}", uiAutomationId);

			// ContentDescription and AccessibilityIdentifier are used by Xamarin.UITest (Test Cloud) to identify visual elements
			if (IsAndroidView(parent.Type))
			{
				writer.AppendLineInvariantIndented("{0}.ContentDescription = \"{1}\";", closureName, uiAutomationId);
			};

			if (IsIOSUIView(parent.Type))
			{
				writer.AppendLineInvariantIndented("{0}.AccessibilityIdentifier = \"{1}\";", closureName, uiAutomationId);
			}
		}

		private bool IsRelativePanelSiblingProperty(string name)
		{
			return name.Equals("Above") ||
				name.Equals("Below") ||
				name.Equals("LeftOf") ||
				name.Equals("RightOf") ||
				name.Equals("AlignBottomWith") ||
				name.Equals("AlignHorizontalCenterWith") ||
				name.Equals("AlignLeftWith") ||
				name.Equals("AlignRightWith") ||
				name.Equals("AlignTopWith") ||
				name.Equals("AlignVerticalCenterWith");
		}

		private void BuildSetAttachedProperty(IIndentedStringBuilder writer, string closureName, XamlMemberDefinition member, string objectUid, bool isCustomMarkupExtension, INamedTypeSymbol? propertyType = null)
		{
			TryAnnotateWithGeneratorSource(writer);
			var literalValue = isCustomMarkupExtension
				? GetCustomMarkupExtensionValue(member, closureName)
				: BuildLiteralValue(member, propertyType: propertyType, owner: member, objectUid: objectUid);

			var memberGlobalizedType = FindType(member.Member.DeclaringType)?.GetFullyQualifiedTypeIncludingGlobal() ?? GetGlobalizedTypeName(member.Member.DeclaringType.Name);
			writer.AppendLineInvariantIndented(
				"{0}.Set{1}({3}, {2});",
				memberGlobalizedType,
				member.Member.Name,
				literalValue,
				closureName
			);
		}

		private XamlLazyApplyBlockIIndentedStringBuilder CreateApplyBlock(IIndentedStringBuilder writer, INamedTypeSymbol? appliedType, out string closureName)
		{
			TryAnnotateWithGeneratorSource(writer);
			closureName = "c" + (_applyIndex++).ToString(CultureInfo.InvariantCulture);

			//
			// Since we're using strings to generate the code, we can't know ahead of time if
			// content will be generated only by looking at the Xaml object model.
			// For now, we only observe if the inner code has generated code, and we create
			// the apply block at that time.
			//
			string? delegateType = null;

			if (appliedType != null)
			{
				var globalizedFullyQualifiedAppliedType = GetGlobalizedTypeName(appliedType.ToString());
				if (!_xamlAppliedTypes.TryGetValue(globalizedFullyQualifiedAppliedType, out var appliedTypeIndex))
				{
					appliedTypeIndex = _xamlAppliedTypes.Count;
					_xamlAppliedTypes.Add(globalizedFullyQualifiedAppliedType, _xamlAppliedTypes.Count);
				}

				if (_isHotReloadEnabled)
				{
					// In C# HotReload mode, and with recent versions of the C# compiler,
					// nested resolution of the delegate type is less costly than it
					// used to.
					// delegateType = $"global::System.Action<global::{appliedType.ToDisplayString()}>";
				}
				else
				{
					delegateType = $"{_fileUniqueId}XamlApplyExtensions.XamlApplyHandler{appliedTypeIndex}";
				}
			}

			return new XamlLazyApplyBlockIIndentedStringBuilder(
				writer,
				closureName,
				appliedType != null && !_isHotReloadEnabled ? _fileUniqueId : null,
				delegateType,
				!_isTopLevelDictionary && _isHotReloadEnabled);
		}

		private void RegisterPartial(string format, params object[] values)
		{
			_partials.Add(format.InvariantCultureFormat(values));
		}

		private void RegisterXBindTryGetDeclaration(string declaration)
		{
			CurrentScope.XBindTryGetMethodDeclarations.Add(declaration);
		}

		private void RegisterBackingField(string globalizedType, string name, Accessibility accessibility)
		{
			CurrentScope.BackingFields.Add(new BackingFieldDefinition(globalizedType, name, accessibility));
		}

		private void RegisterChildSubclass(string name, XamlMemberDefinition owner, string returnType)
		{
			CurrentScope.Subclasses[name] = new Subclass(owner, returnType, GetDefaultBindMode());
		}

		private void BuildComplexPropertyValue(IIndentedStringBuilder writer, XamlMemberDefinition member, string? prefix, string? closureName = null, bool generateAssignation = true, ComponentDefinition? componentDefinition = null)
		{
			TryAnnotateWithGeneratorSource(writer);
			Func<string, string> formatLine = format => prefix + format + (!prefix.IsNullOrEmpty() ? ";\r\n" : "");
			var postfix = !prefix.IsNullOrEmpty() ? ";" : "";

			var bindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Binding");
			var bindNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Bind");
			var templateBindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "TemplateBinding");
			var declaringType = FindType(member.Member.DeclaringType);
			if (_metadataHelper.FindEventType(declaringType, member.Member.Name) is IEventSymbol eventSymbol)
			{
				GenerateInlineEvent(closureName, writer, member, eventSymbol, componentDefinition);
			}
			else
			{
				(IEnumerable<XamlMemberDefinition>?, IEnumerable<string>?) GetBindingOptions()
				{
					if (bindingNode != null)
					{
						return (bindingNode.Members, default);
					}
					if (bindNode != null)
					{
						return (
							bindNode.Members
								.Where(m => m.Member.Name != "_PositionalParameters" && m.Member.Name != "Path" && m.Member.Name != "BindBack"),
							new[] { bindNode.Members.Any(m => m.Member.Name == "Mode") ? "" : ("Mode = BindingMode." + GetDefaultBindMode()) }
						);
					}
					if (templateBindingNode != null)
					{
						return (
							templateBindingNode.Members,
							new[] { "RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)" }
						);
					}

					return default;
				}

				var (bindingOptions, additionalOptions) = GetBindingOptions();
				if (bindingOptions != null)
				{
					TryAnnotateWithGeneratorSource(writer, suffix: "HasBindingOptions");
					var isDependencyProperty = IsDependencyProperty(declaringType, member.Member.Name);
					var isBindingType = SymbolEqualityComparer.Default.Equals(_metadataHelper.FindPropertyTypeByOwnerSymbol(declaringType, member.Member.Name), Generation.DataBindingSymbol.Value);
					var isOwnerDependencyObject = member.Owner != null && GetType(member.Owner.Type) is { } ownerType &&
						(
							(_xamlTypeToXamlTypeBaseMap.TryGetValue(ownerType, out var baseTypeSymbol) && FindType(baseTypeSymbol)?.GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, Generation.DependencyObjectSymbol.Value)) == true) ||
							ownerType.GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, Generation.DependencyObjectSymbol.Value))
						);

					if (isDependencyProperty)
					{
						var propertyOwner = declaringType;

						using (writer.Indent($"{prefix}SetBinding(", $"){postfix}"))
						{
							writer.AppendLineIndented($"{propertyOwner!.GetFullyQualifiedTypeIncludingGlobal()}.{member.Member.Name}Property,");
							WriteBinding(isTemplateBindingAttachedProperty: templateBindingNode is not null && IsAttachedProperty(declaringType, member.Member.Name));
						}
					}
					else if (isBindingType)
					{
						WriteBinding(isTemplateBindingAttachedProperty: false, prefix: $"{prefix}{member.Member.Name} = ");
						writer.AppendLineIndented(postfix);
					}
					else
					{
						var pocoBuilder = isOwnerDependencyObject ? "" : $"GetDependencyObjectForXBind().";

						using (writer.Indent($"{prefix}{pocoBuilder}SetBinding(", $"){postfix}"))
						{
							writer.AppendLineIndented($"\"{member.Member.Name}\",");
							WriteBinding(isTemplateBindingAttachedProperty: false);
						}
					}

					void WriteBinding(bool isTemplateBindingAttachedProperty, string? prefix = null)
					{
						writer.AppendLineIndented($"{prefix}new {XamlConstants.Types.Binding}()");

						var containsCustomMarkup = bindingOptions.Any(x => IsCustomMarkupExtensionType(x.Objects.FirstOrDefault()?.Type));
						var closure = containsCustomMarkup ? "___b" : default;
						var setters = bindingOptions
							.Select(x => BuildMemberPropertyValue(x, isTemplateBindingAttachedProperty, closure))
							.Concat(additionalOptions ?? Array.Empty<string>())
							.Where(x => !string.IsNullOrEmpty(x))
							.ToArray();

						// members initialization
						if (setters.Length > 0)
						{
							if (containsCustomMarkup)
							{
								// for custom MarkupExtension, we need to pass the `Binding` to build its parser context:
								// new Binding().BindingApply(___b => { x = y,... })
								using var _ = writer.Indent();

								writer.AppendLineIndented($".BindingApply({closure} =>");
								using (writer.Indent("{", "})"))
								{
									foreach (var setter in setters)
									{
										writer.AppendLineIndented($"{closure}.{setter};");
									}

									writer.AppendLineIndented($"return {closure};");
								}
							}
							else
							{
								// using object initializers syntax:
								// new Binding() { x = y, ... }
								using (writer.Block())
								{
									foreach (var setter in setters)
									{
										writer.AppendLineIndented($"{setter},");
									}
								}
							}
						}

						foreach (var option in bindingOptions)
						{
							var themeResourceCandidate = option.Objects.FirstOrDefault();
							if (themeResourceCandidate?.Type is { PreferredXamlNamespace: XamlConstants.PresentationXamlXmlNamespace, Name: "ThemeResource" })
							{
								if (option.Member.Name == "TargetNullValue" && themeResourceCandidate.Members.FirstOrDefault().Value is string targetNullValueKey)
								{
									writer.AppendLineIndented($".ApplyTargetNullValueThemeResource(@\"{targetNullValueKey}\", {ParseContextPropertyAccess})");
								}
								else if (option.Member.Name == "FallbackValue" && themeResourceCandidate.Members.FirstOrDefault().Value is string fallbackValueKey)
								{
									writer.AppendLineIndented($".ApplyFallbackValueThemeResource(@\"{fallbackValueKey}\", {ParseContextPropertyAccess})");
								}
							}
						}

						// xbind initialization
						if (bindNode != null && !isBindingType)
						{
							var xBindEvalFunction = BuildXBindEvalFunction(member, bindNode);

							using var _ = writer.Indent();
							writer.AppendLineIndented(xBindEvalFunction);
						}
					}
				}

				(var resourceKey, var isThemeResourceExtension) = GetStaticResourceKey(member);

				if (resourceKey != null)
				{
					TryAnnotateWithGeneratorSource(writer, suffix: "HasResourceKey");

					if (IsDependencyProperty(declaringType, member.Member.Name))
					{
						var propertyOwner = declaringType;
						writer.AppendLineInvariantIndented("global::Uno.UI.ResourceResolverSingleton.Instance.ApplyResource({0}, {1}.{2}Property, \"{3}\", isThemeResourceExtension: {4}, isHotReloadSupported: {5}, context: {6});", closureName, propertyOwner!.GetFullyQualifiedTypeIncludingGlobal(), member.Member.Name, resourceKey, isThemeResourceExtension ? "true" : "false", _isHotReloadEnabled ? "true" : "false", ParseContextPropertyAccess);
					}
					else if (IsAttachedProperty(member))
					{
						if (closureName == null)
						{
							throw new InvalidOperationException("A closure name is required for attached properties");
						}

						BuildSetAttachedProperty(writer, closureName, member, objectUid: "", isCustomMarkupExtension: false, propertyType: GetAttachedPropertyType(member));
					}
					else
					{
						// Load-time resolution isn't feasible for non-DPs, so we just set the Application-level value right away, and that's it.
						var rightSide = GetSimpleStaticResourceRetrieval(member);
						if (generateAssignation)
						{
							writer.AppendLineInvariantIndented(formatLine("{0} = {1}"), member.Member.Name, rightSide);
						}
						else
						{
							writer.AppendLineIndented(rightSide);
						}
					}
				}

				var customResourceResourceId = GetCustomResourceResourceId(member);
				if (customResourceResourceId != null)
				{
					var type = GetPropertyTypeByOwnerSymbol(declaringType!, member.Member.Name, member.LineNumber, member.LinePosition);
					var rightSide = GetCustomResourceRetrieval(customResourceResourceId, type.GetFullyQualifiedTypeIncludingGlobal());
					writer.AppendLineInvariantIndented("{0}{1} = {2};", prefix, member.Member.Name, rightSide);
				}
			}
		}

		/// <summary>
		/// Gets string to retrieve a CustomResource
		/// </summary>
		/// <param name="customResourceResourceId">The id set by the CustomResource markup</param>
		/// <param name="typeStr">The type expected to be returned</param>
		private static string GetCustomResourceRetrieval(object? customResourceResourceId, string typeStr)
		{
			return "global::Uno.UI.ResourceResolver.RetrieveCustomResource<{0}> (\"{1}\", null, null, null)".InvariantCultureFormat(typeStr, customResourceResourceId);
		}

		/// <summary>
		/// Looks for a XamlObjectDefinition from a {CustomResource resourceID} markup and returns resourceId if it exists, null otherwise
		/// </summary>
		private static object? GetCustomResourceResourceId(XamlMemberDefinition? member)
		{
			return member?.Objects.FirstOrDefault(o => o.Type.Name == "CustomResource")?.Members.FirstOrDefault()?.Value;
		}

		private string BuildXBindEvalFunction(XamlMemberDefinition member, XamlObjectDefinition bindNode)
		{
			_xBindCounter++;
			CurrentScope.XBindExpressions.Add(bindNode);

			// If a binding is inside a DataTemplate, the binding root in the case of an x:Bind is
			// the DataContext, not the control's instance.
			var (isInsideDataTemplate, dataTemplateObject) = IsMemberInsideDataTemplate(member.Owner);

			var pathMember = bindNode.Members.FirstOrDefault(m => m.Member.Name == "_PositionalParameters" || m.Member.Name == "Path");

			var rawFunction = XBindExpressionParser.RestoreSinglePath(pathMember?.Value?.ToString() ?? "");
			var propertyType = FindPropertyType(member.Member);

			// Apply replacements to avoid having issues with the XAML parser which does not
			// support quotes in positional markup extensions parameters.
			// Note that the UWP preprocessor does not need to apply those replacements as the x:Bind expressions
			// are being removed during the first phase and replaced by "connections".
			rawFunction = rawFunction
				?.Replace("x:False", "false")
				.Replace("x:True", "true")
				.Replace("{x:Null}", "null")
				.Replace("x:Null", "null")
				?? "";

			rawFunction = RewriteNamespaces(rawFunction);

			var modeMember = bindNode.Members.FirstOrDefault(m => m.Member.Name == "Mode")?.Value?.ToString() ?? GetDefaultBindMode();
			var rawBindBack = bindNode.Members.FirstOrDefault(m => m.Member.Name == "BindBack")?.Value?.ToString();

			var applyBindingParameters = _isHotReloadEnabled
				? "__that, (___b, __that)"
				: "___b";

			if (isInsideDataTemplate)
			{
				var dataTypeObject = FindMember(dataTemplateObject!, "DataType", XamlConstants.XamlXmlNamespace);
				if (dataTypeObject?.Value == null)
				{
					throw new Exception($"Unable to find x:DataType in enclosing DataTemplate");
				}

				var dataType = RewriteNamespaces(dataTypeObject.Value.ToString() ?? "");
				var dataTypeSymbol = GetType(dataType);

				var contextFunction = XBindExpressionParser.Rewrite("___tctx", rawFunction, dataTypeSymbol, _metadataHelper.Compilation.GlobalNamespace, isRValue: true, _xBindCounter, FindType, targetPropertyType: null);
				if (contextFunction.MethodDeclaration is not null)
				{
					RegisterXBindTryGetDeclaration(contextFunction.MethodDeclaration);
				}

				// Populate the property paths only if updateable bindings.
				// TODO: Properties is currently evaluated regardless of the mode. We just throw it out here.
				// Consider optimizing that.
				var propertyPaths = modeMember != "OneTime"
					? contextFunction.Properties
					: ImmutableArray<string>.Empty;

				var formattedPaths = propertyPaths
					.Where(p => !p.StartsWith("global::", StringComparison.Ordinal))  // Don't include paths that start with global:: (e.g. Enums)
					.Select(p => $"\"{p.Replace("\"", "\\\"")}\"");

				var pathsArray = formattedPaths.Any()
					? ", new [] {" + string.Join(", ", formattedPaths) + "}"
					: "";

				string buildBindBack()
				{
					if (modeMember == "TwoWay")
					{
						if (contextFunction.HasFunction)
						{
							if (!string.IsNullOrWhiteSpace(rawBindBack))
							{
								return $"(___ctx, __value) => {{ if(___ctx is {dataType} ___tctx) {{ ___tctx.{rawBindBack}(({propertyType})__value); }} }}";
							}
							else
							{
								throw new NotSupportedException($"Expected BindBack for {rawFunction}");
							}
						}
						else
						{
							if (contextFunction.Properties.Length == 1)
							{
								var targetPropertyType = GetXBindPropertyPathType(contextFunction.Properties[0], dataTypeSymbol).GetFullyQualifiedTypeIncludingGlobal();
								var contextFunctionLValue = XBindExpressionParser.Rewrite("___tctx", rawFunction, dataTypeSymbol, _metadataHelper.Compilation.GlobalNamespace, isRValue: false, _xBindCounter, FindType, targetPropertyType);
								if (contextFunctionLValue.MethodDeclaration is not null)
								{
									RegisterXBindTryGetDeclaration(contextFunctionLValue.MethodDeclaration);
									return $"(___ctx, __value) => {{ if(___ctx is {dataType} ___tctx) {{ {contextFunctionLValue.Expression}; }} }}";
								}
								else
								{
									return $"(___ctx, __value) => {{ if(___ctx is {dataType} ___tctx) {{ {contextFunctionLValue.Expression} = ({targetPropertyType})global::Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({targetPropertyType}), __value); }} }}";
								}
							}
							else
							{
								throw new NotSupportedException($"Invalid x:Bind property path count (This should not happen)");
							}
						}
					}
					else
					{
						return "null";
					}
				}

				return $".BindingApply({applyBindingParameters} => /*defaultBindMode{GetDefaultBindMode()}*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, null, ___ctx => ___ctx is {GetType(dataType)} ___tctx ? ({contextFunction.Expression}) : (false, default), {buildBindBack()} {pathsArray}))";
			}
			else
			{
				EnsureXClassName();

				var rewrittenRValue = string.IsNullOrEmpty(rawFunction)
					? (MethodDeclaration: null, Expression: "(true, ___ctx)", Properties: ImmutableArray<string>.Empty, HasFunction: false)
					: XBindExpressionParser.Rewrite("___tctx", rawFunction, _xClassName.Symbol, _metadataHelper.Compilation.GlobalNamespace, isRValue: true, _xBindCounter, FindType, targetPropertyType: null);

				if (rewrittenRValue.MethodDeclaration is not null)
				{
					RegisterXBindTryGetDeclaration(rewrittenRValue.MethodDeclaration);
				}

				string buildBindBack()
				{
					if (modeMember == "TwoWay")
					{
						if (rewrittenRValue.HasFunction)
						{
							if (!string.IsNullOrWhiteSpace(rawBindBack))
							{
								return $"(___tctx, __value) => {rawBindBack}(({propertyType})__value)";
							}
							else
							{
								throw new NotSupportedException($"Expected BindBack for x:Bind function [{rawFunction}]");
							}
						}
						else
						{
							if (rewrittenRValue.Properties.Length == 1)
							{
								var targetPropertyType = GetXBindPropertyPathType(rewrittenRValue.Properties[0]).GetFullyQualifiedTypeIncludingGlobal();

								if (string.IsNullOrEmpty(rawFunction))
								{
									return $"(___ctx, __value) => {{ " +
										$"if(___ctx is {_xClassName} ___tctx) " +
										$"___ctx = ({targetPropertyType})global::Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({targetPropertyType}), __value);" +
										$" }}";
								}

								var rewrittenLValue = XBindExpressionParser.Rewrite("___tctx", rawFunction, _xClassName.Symbol, _metadataHelper.Compilation.GlobalNamespace, isRValue: false, _xBindCounter, FindType, targetPropertyType);
								if (rewrittenLValue.MethodDeclaration is not null)
								{
									RegisterXBindTryGetDeclaration(rewrittenLValue.MethodDeclaration);
									return $"(___ctx, __value) => {{ if(___ctx is {_xClassName} ___tctx) {rewrittenLValue.Expression}; }}";
								}
								else
								{
									return $"(___ctx, __value) => {{ " +
										$"if(___ctx is {_xClassName} ___tctx) " +
										$"{rewrittenLValue.Expression} = ({targetPropertyType})global::Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({targetPropertyType}), __value);" +
										$" }}";
								}
							}
							else
							{
								throw new NotSupportedException($"Invalid x:Bind property path count (This should not happen)");
							}
						}
					}
					else
					{
						return "null";
					}
				}

				var bindFunction = $"___ctx is {_xClassName} ___tctx ? ({rewrittenRValue.Expression}) : (false, default)";

				// Populate the property paths only if updateable bindings.
				// TODO: Properties is currently evaluated regardless of the mode. We just throw it out here.
				// Consider optimizing that.
				var propertyPaths = modeMember != "OneTime"
					? rewrittenRValue.Properties
					: ImmutableArray<string>.Empty;

				var formattedPaths = propertyPaths
					.Where(p => !p.StartsWith("global::", StringComparison.Ordinal))  // Don't include paths that start with global:: (e.g. Enums)
					.Select(p => $"\"{p.Replace("\"", "\\\"")}\"");

				var pathsArray = formattedPaths.Any()
					? ", new [] {" + string.Join(", ", formattedPaths) + "}"
					: "";

				return $".BindingApply({applyBindingParameters} =>  /*defaultBindMode{GetDefaultBindMode()} {rawFunction}*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, __that, ___ctx => {bindFunction}, {buildBindBack()} {pathsArray}))";
			}
		}

		private ITypeSymbol GetXBindPropertyPathType(string propertyPath, INamedTypeSymbol? rootType = null)
		{
			ITypeSymbol currentType = rootType
				?? _xClassName?.Symbol
				?? throw new InvalidCastException($"Unable to find type {_xClassName}");

			var parts = propertyPath.Split('.');

			for (int i = 0; i < parts.Length; i++)
			{
				var part = parts[i];
				var isIndexer = false;
				if (part.IndexOf('[') is int indexOfIndexer && indexOfIndexer > 0)
				{
					isIndexer = true;
					part = part.Substring(0, indexOfIndexer);
				}

				if (currentType.TryGetPropertyOrFieldType(part) is ITypeSymbol partType)
				{
					currentType = partType;
				}
				else if (i == 0 && FindSubElementByName(_fileDefinition.Objects.First(), part) is XamlObjectDefinition elementByName)
				{
					currentType = GetType(elementByName.Type);

					if (currentType == null)
					{
						throw new InvalidOperationException($"Unable to find member [{part}] on type [{elementByName.Type}]");
					}
				}
				else
				{
					// We can't find the type. It could be something that is source-generated, or it could be a user error.
					// For source-generated members, it's not possible to reliably get this information due to https://github.com/dotnet/roslyn/issues/57239
					// However, we do a best effort to handle the common scenario, which is code generated by CommunityToolkit.Mvvm.
					foreach (var typeProvider in Generation.TypeProviders)
					{
						if (typeProvider.TryGetType(currentType, part) is { } thirdPartyType)
						{
							currentType = thirdPartyType;
							break;
						}
					}

					if (currentType is null)
						throw new InvalidOperationException($"Unable to find member [{part}] on type [{currentType}]");
				}

				if (isIndexer)
				{
					currentType = currentType.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.IsIndexer).Type;
				}
			}

			return currentType;
		}

		private string RewriteNamespaces(string xamlString)
		{
			foreach (var ns in _fileDefinition.Namespaces)
			{
				xamlString = ReplaceNamespace(xamlString, ns);
			}

			return xamlString;

			// This is a performance-sensitive code. It used to be using Regex and was improved to avoid Regex.
			// It might be possible to improve it further, but that seems to do the job for now.
			// We can optimize further if future performance measures shows it being problematic.
			// What this method does is:
			// Given a xamlString like "muxc:SomeControl", converts it to Microsoft .UI.Xaml.Controls.SomeControl.
			// Note that the given xamlString can be a more complex expression involving multiple namespaces.
			static string ReplaceNamespace(string xamlString, NamespaceDeclaration ns)
			{
				// Note: The call xamlString.IndexOf($"{ns.Prefix}:", StringComparison.Ordinal) can be replaced with xamlString.IndexOf(ns.Prefix)
				// followed by a separate check for ":" character. This will save a string allocation.
				// But for now, we keep it as is. We can always revisit if it shows performance issues.
				if (ns.Namespace.StartsWith("using:", StringComparison.Ordinal))
				{
					while (xamlString.Length > ns.Prefix.Length)
					{
						var index = xamlString.IndexOf($"{ns.Prefix}:", StringComparison.Ordinal);
						if (index == 0 || (index > 0 && !char.IsLetterOrDigit(xamlString[index - 1])))
						{
							var nsGlobalized = ns.Namespace.Replace("using:", "global::");

							// following is the equivalent of "string.Concat(xamlString.Substring(0, index), nsGlobalized, ".", xamlString.Substring(index + ns.Prefix.Length + 1))"
							// but in a way that allocates less memory.
							var newLength = xamlString.Length + nsGlobalized.Length - ns.Prefix.Length;
							xamlString = StringExtensions.Create(newLength, (xamlString, index, nsGlobalized, ns.Prefix), static (span, state) =>
							{
								var (xamlString, index, nsGlobalized, nsPrefix) = state;
								var copiedLengthSoFar = 0;

								xamlString.AsSpan().Slice(0, index).CopyTo(span.Slice(copiedLengthSoFar));
								copiedLengthSoFar += index;

								nsGlobalized.AsSpan().CopyTo(span.Slice(copiedLengthSoFar));
								copiedLengthSoFar += nsGlobalized.Length;

								span[copiedLengthSoFar] = '.';
								copiedLengthSoFar++;

								xamlString.AsSpan().Slice(index + nsPrefix.Length + 1).CopyTo(span.Slice(copiedLengthSoFar));
							});

						}
						else
						{
							break;
						}
					}

					return xamlString;
				}
				else if (ns.Namespace == XamlConstants.XamlXmlNamespace)
				{
					while (xamlString.Length > ns.Prefix.Length)
					{
						var index = xamlString.IndexOf($"{ns.Prefix}:", StringComparison.Ordinal);
						if (index == 0 || (index > 0 && !char.IsLetterOrDigit(xamlString[index - 1])))
						{
							const string nsGlobalized = "global::System.";

							// following is the equivalent of "string.Concat(xamlString.Substring(0, index), nsGlobalized, xamlString.Substring(index + ns.Prefix.Length + 1))"
							// but in a way that allocates less memory.
							var newLength = xamlString.Length + nsGlobalized.Length - ns.Prefix.Length - 1;
							xamlString = StringExtensions.Create(newLength, (xamlString, index, ns.Prefix), static (span, state) =>
							{
								var (xamlString, index, nsPrefix) = state;
								var copiedLengthSoFar = 0;

								xamlString.AsSpan().Slice(0, index).CopyTo(span.Slice(copiedLengthSoFar));
								copiedLengthSoFar += index;

								nsGlobalized.AsSpan().CopyTo(span.Slice(copiedLengthSoFar));
								copiedLengthSoFar += nsGlobalized.Length;

								xamlString.AsSpan().Slice(index + nsPrefix.Length + 1).CopyTo(span.Slice(copiedLengthSoFar));
							});
						}
						else
						{
							break;
						}
					}

					return xamlString;
				}
				else
				{
					return xamlString;
				}
			}
		}

		private string GetDefaultBindMode() => _currentDefaultBindMode.Peek();

		private string BuildMemberPropertyValue(XamlMemberDefinition m, bool isTemplateBindingAttachedProperty, string? closure = null)
		{
			if (IsCustomMarkupExtensionType(m.Objects.FirstOrDefault()?.Type))
			{
				// If the member contains a custom markup extension, build the inner part first
				var propertyValue = GetCustomMarkupExtensionValue(m, closure);
				return "{0} = {1}".InvariantCultureFormat(m.Member.Name, propertyValue);
			}
			else
			{
				return "{0} = {1}".InvariantCultureFormat(
					m.Member.Name == "_PositionalParameters" ? "Path" : m.Member.Name,
					BuildBindingOption(m, FindPropertyType(m.Member), isTemplateBindingAttachedProperty));
			}
		}

		private string GetCustomMarkupExtensionValue(XamlMemberDefinition member, string? target = null)
		{
			// Get the type of the custom markup extension
			var markup = member
				.Objects
				.Select(x => new { Value = x, Symbol = GetMarkupExtensionType(x.Type) })
				.FirstOrDefault(x => x.Symbol != null);
			if (markup == null)
			{
				throw new InvalidOperationException($"Unable to find markup extension type: '{member.Objects.FirstOrDefault()?.Type}' on '{member.Member}'");
			}

			var property = FindProperty(member.Member);
			var declaringType = property?.ContainingType;
			var propertyType = property?.FindDependencyPropertyType();

			if (declaringType == null || propertyType == null)
			{
#if DEBUG
				Console.WriteLine($"Unable to determine the target dependency property for the markup extension ('{member.Member}' cannot be found).");
#endif
				return string.Empty;
			}

			// Get the full globalized names
			var globalized = new
			{
				MarkupType = markup.Symbol!.GetFullyQualifiedTypeIncludingGlobal(),
				MarkupHelper = $"global::{XamlConstants.Types.MarkupHelper}",
				IMarkupExtensionOverrides = $"global::{XamlConstants.Types.IMarkupExtensionOverrides}",
				XamlBindingHelper = $"global::{XamlConstants.Types.MarkupXamlBindingHelper}",
				PvtpDeclaringType = declaringType.GetFullyQualifiedTypeIncludingGlobal(),
				PvtpType = propertyType.GetFullyQualifiedTypeIncludingGlobal(),
			};

			// Build a string of all its properties
			var properties = markup.Value
				.Members
				.Select(m =>
				{
					var propertyType = markup.Symbol.GetPropertyWithName(m.Member.Name)?.Type as INamedTypeSymbol;
					var resourceName = GetSimpleStaticResourceRetrieval(m, propertyType);
					var value = resourceName ?? BuildLiteralValue(m, propertyType: propertyType, owner: member);

					return "{0} = {1}".InvariantCultureFormat(m.Member.Name, value);
				})
				.JoinBy(", ");
			var markupInitializer = !properties.IsNullOrEmpty() ? $" {{ {properties} }}" : "()";

			// Build the parser context for ProvideValue(IXamlServiceProvider)
			var providerDetails = new string[]
			{
				// BuildParserContext(object? target, Type propertyDeclaringType, string propertyName, Type propertyType)
				target ?? "null",
				$"typeof({globalized.PvtpDeclaringType})",
				$"\"{member.Member.Name}\"",
				$"typeof({globalized.PvtpType})",
			};
			var provider = $"{globalized.MarkupHelper}.CreateParserContext({providerDetails.JoinBy(", ")})";

			var provideValue = $"(({globalized.IMarkupExtensionOverrides})new {globalized.MarkupType}{markupInitializer}).ProvideValue({provider})";
			if (IsImplementingInterface(propertyType, Generation.IConvertibleSymbol.Value))
			{
				provideValue = $"{globalized.XamlBindingHelper}.ConvertValue(typeof({globalized.PvtpType}), {provideValue})";
			}
			var unboxed = $"({globalized.PvtpType}){provideValue}";

			return unboxed;
		}

		private (bool isInside, XamlObjectDefinition? xamlObject) IsMemberInsideDataTemplate(XamlObjectDefinition? xamlObject)
			=> IsMemberInside(xamlObject, "DataTemplate");

		private (bool isInside, XamlObjectDefinition? xamlObject) IsMemberInsideFrameworkTemplate(XamlObjectDefinition? xamlObject) =>
			FrameworkTemplateTypes
				.Select(n => IsMemberInside(xamlObject, n))
				.FirstOrDefault(n => n.isInside);

		private (bool isInside, XamlObjectDefinition? xamlObject) IsMemberInsideResourceDictionary(XamlObjectDefinition xamlObject, int? maxDepth = 1)
			=> IsMemberInside(xamlObject, "ResourceDictionary", maxDepth: maxDepth);

		private static (bool isInside, XamlObjectDefinition? xamlObject) IsMemberInside(XamlObjectDefinition? xamlObject, string typeName, int? maxDepth = null)
		{
			if (xamlObject == null)
			{
				return (false, null);
			}

			int depth = 0;
			do
			{
				if (xamlObject.Type?.Name == typeName)
				{
					return (true, xamlObject);
				}

				xamlObject = xamlObject.Owner;
			}
			while (xamlObject != null && (maxDepth == null || ++depth <= maxDepth));

			return (false, null);
		}

		/// <summary>
		/// Gets the key referred to by a StaticResourceExtension or ThemeResourceExtension.
		/// </summary>
		/// <param name="member">The StaticResourceExtension or ThemeResourceExtension member</param>
		/// <returns>(Key referred to, true if ThemeResource)</returns>
		private (string? key, bool isThemeResourceExtension) GetStaticResourceKey(XamlMemberDefinition member)
		{
			var staticResourceNode = member.Objects.FirstOrDefault(o => o.Type.Name == "StaticResource");
			var themeResourceNode = member.Objects.FirstOrDefault(o => o.Type.Name == "ThemeResource");

			var staticResourcePath = staticResourceNode?.Members.First().Value?.ToString();
			var themeResourcePath = themeResourceNode?.Members.First().Value?.ToString();

			return (staticResourcePath ?? themeResourcePath, themeResourceNode != null);
		}

		/// <summary>
		/// Returns code for simple initialization-time retrieval for StaticResource/ThemeResource markup.
		/// </summary>
		private string? GetSimpleStaticResourceRetrieval(XamlMemberDefinition member, INamedTypeSymbol? targetPropertyType = null)
		{
			//Both ThemeResource and StaticResource use the same lookup mechanism
			var resourcePath = GetStaticResourceKey(member).key;

			if (resourcePath == null)
			{
				return null;
			}
			else
			{
				targetPropertyType = targetPropertyType ?? FindPropertyType(member.Member);

				// If the property Type is attributed with the CreateFromStringAttribute
				if (IsXamlTypeConverter(targetPropertyType))
				{
					// We expect the resource to be a string if we're applying the CreateFromStringAttribute
					var memberValue = GetSimpleStaticResourceRetrieval(targetPropertyType: Generation.StringSymbol.Value, resourcePath);

					// We must build the member value as a call to a "conversion" function
					var converterValue = BuildXamlTypeConverterLiteralValue(targetPropertyType, memberValue, includeQuotations: false);
					TryAnnotateWithGeneratorSource(ref converterValue);
					return converterValue;
				}

				var retrieval = GetSimpleStaticResourceRetrieval(targetPropertyType, resourcePath);
				TryAnnotateWithGeneratorSource(ref retrieval);
				return retrieval;
			}
		}

		/// <summary>
		/// Returns code for simple initialization-time retrieval for StaticResource/ThemeResource markup.
		/// </summary>
		private string GetSimpleStaticResourceRetrieval(INamedTypeSymbol? targetPropertyType, string? keyStr)
		{
			targetPropertyType = targetPropertyType ?? Generation.ObjectSymbol.Value;

			var targetPropertyFQT = targetPropertyType.GetFullyQualifiedTypeIncludingGlobal();

			var staticRetrieval = $"({targetPropertyFQT})global::Uno.UI.ResourceResolverSingleton.Instance.ResolveResourceStatic(" +
				$"\"{keyStr}\", typeof({targetPropertyFQT}), context: {ParseContextPropertyAccess})";
			TryAnnotateWithGeneratorSource(ref staticRetrieval);
			return staticRetrieval;
		}

		/// <summary>
		/// Get the retrieval key associated with the given resource key, if one exists, otherwise null.
		/// </summary>
		private string? GetResourceDictionaryInitializerName(string keyStr)
		{
			if (_topLevelQualifiedKeys.TryGetValue((_themeDictionaryCurrentlyBuilding, keyStr), out var qualifiedKey))
			{
				return qualifiedKey;
			}

			return null;
		}

		private INamedTypeSymbol FindUnderlyingType(INamedTypeSymbol propertyType)
		{
			return (propertyType.IsNullable(out var underlyingType) && underlyingType is INamedTypeSymbol underlyingNamedType) ? underlyingNamedType : propertyType;
		}

		private string BuildLiteralValue(INamedTypeSymbol propertyType, bool isTemplateBindingAttachedProperty, string? memberValue, XamlMemberDefinition? owner = null, string memberName = "", string objectUid = "")
		{
			var literalValue = Inner();
			TryAnnotateWithGeneratorSource(ref literalValue);
			return literalValue;

			string GetMemberValue()
			{
				if (string.IsNullOrWhiteSpace(memberValue))
				{
					throw new InvalidOperationException($"The property value is invalid");
				}

				return memberValue!;
			}

			string Inner()
			{
				if (IsLocalizedString(propertyType, objectUid))
				{
					var resourceValue = BuildLocalizedResourceValue(FindType(owner?.Member.DeclaringType), memberName, objectUid);

					if (resourceValue != null)
					{
						return resourceValue;
					}
				}

				// If the property Type is attributed with the CreateFromStringAttribute
				if (IsXamlTypeConverter(propertyType))
				{
					// We must build the member value as a call to a "conversion" function
					return BuildXamlTypeConverterLiteralValue(propertyType, GetMemberValue(), includeQuotations: true);
				}

				propertyType = FindUnderlyingType(propertyType);
				switch (propertyType.SpecialType)
				{
					case SpecialType.System_Int32:
						// UWP ignores everything starting from a space. So `5 6 7` is a valid int value and it's "5".
						return IgnoreStartingFromFirstSpaceIgnoreLeading(GetMemberValue());
					case SpecialType.System_Int64:
					case SpecialType.System_Int16:
					case SpecialType.System_Byte:
						// UWP doesn't ignore spaces here.
						return GetMemberValue();
					case SpecialType.System_Single:
					case SpecialType.System_Double:
						return GetFloatingPointLiteral(GetMemberValue(), propertyType, owner);
					case SpecialType.System_String:
						return "\"" + DoubleEscape(memberValue) + "\"";
					case SpecialType.System_Boolean:
						return bool.Parse(GetMemberValue()).ToString().ToLowerInvariant();
				}

				var propertyTypeWithoutGlobal = propertyType.GetFullyQualifiedTypeExcludingGlobal();
				switch (propertyTypeWithoutGlobal)
				{
					case XamlConstants.Types.DependencyProperty:
						return BuildDependencyProperty(GetMemberValue());

					case XamlConstants.Types.Brush:
					case XamlConstants.Types.SolidColorBrush:
						return BuildBrush(GetMemberValue());

					case XamlConstants.Types.Thickness:
						return BuildThickness(GetMemberValue());

					case XamlConstants.Types.CornerRadius:
						return BuildCornerRadius(GetMemberValue());

					case XamlConstants.Types.FontFamily:
						return $@"new global::{propertyTypeWithoutGlobal}(""{memberValue}"")";

					case XamlConstants.Types.FontWeight:
						return BuildFontWeight(GetMemberValue());

					case XamlConstants.Types.GridLength:
						return BuildGridLength(GetMemberValue());

					case "UIKit.UIColor":
						return BuildColor(GetMemberValue());

					case "Windows.UI.Color":
						return BuildColor(GetMemberValue());

					case "Android.Graphics.Color":
						return BuildColor(GetMemberValue());

					case "System.Uri":
						var uriValue = GetMemberValue();
						return $"new System.Uri({RewriteUri(uriValue)}, global::System.UriKind.RelativeOrAbsolute)";

					case "System.Type":
						return $"typeof({GetType(GetMemberValue(), owner?.Owner).GetFullyQualifiedTypeIncludingGlobal()})";

					case XamlConstants.Types.Geometry:
						return $"@\"{memberValue}\"";

					case XamlConstants.Types.KeyTime:
						return ParseTimeSpan(GetMemberValue());

					case XamlConstants.Types.Duration:
						return $"new Duration({ParseTimeSpan(GetMemberValue())})";

					case "System.TimeSpan":
						return ParseTimeSpan(GetMemberValue());

					case "System.Drawing.Point":
						return "new System.Drawing.Point(" + memberValue + ")";

					case "Windows.UI.Xaml.Media.CacheMode":
						return ParseCacheMode(GetMemberValue());

					case "System.Drawing.PointF":
						return "new System.Drawing.PointF(" + AppendFloatSuffix(GetMemberValue()) + ")";

					case "System.Drawing.Size":
						return "new System.Drawing.Size(" + SplitAndJoin(memberValue) + ")";

					case "Windows.Foundation.Size":
						return "new Windows.Foundation.Size(" + SplitAndJoin(memberValue) + ")";

					case "Windows.UI.Xaml.Media.Matrix":
						return "new Windows.UI.Xaml.Media.Matrix(" + SplitAndJoin(memberValue) + ")";

					case "Windows.Foundation.Point":
						return "new Windows.Foundation.Point(" + SplitAndJoin(memberValue) + ")";

					case "System.Numerics.Vector3":
						return "new global::System.Numerics.Vector3(" + memberValue + ")";

					case "Windows.UI.Xaml.Input.InputScope":
						return "new global::Windows.UI.Xaml.Input.InputScope { Names = { new global::Windows.UI.Xaml.Input.InputScopeName { NameValue = global::Windows.UI.Xaml.Input.InputScopeNameValue." + memberValue + "} } }";

					case "UIKit.UIImage":
						var imageValue = GetMemberValue();
						if (imageValue.StartsWith(XamlConstants.BundleResourcePrefix, StringComparison.InvariantCultureIgnoreCase))
						{
							return "UIKit.UIImage.FromBundle(\"" + imageValue.Substring(XamlConstants.BundleResourcePrefix.Length, imageValue.Length - XamlConstants.BundleResourcePrefix.Length) + "\")";
						}
						return imageValue;

					case "Windows.UI.Xaml.Controls.IconElement":
						return "new Windows.UI.Xaml.Controls.SymbolIcon { Symbol = Windows.UI.Xaml.Controls.Symbol." + memberValue + "}";

					case "Windows.Media.Playback.IMediaPlaybackSource":
						return "Windows.Media.Core.MediaSource.CreateFromUri(new Uri(\"" + memberValue + "\"))";

					case "Windows.UI.Xaml.Media.ImageSource":
						return RewriteUri(memberValue);

					case "Windows.UI.Xaml.TargetPropertyPath":
						return BuildTargetPropertyPath(GetMemberValue(), owner);
				}

				var isEnum = propertyType.TypeKind == TypeKind.Enum;
				if (isEnum)
				{
					var value = GetMemberValue();
					var isEnumSigned = propertyType.EnumUnderlyingType!.SpecialType switch
					{
						// CS1008 Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
						SpecialType.System_Byte => false,
						SpecialType.System_SByte => true,
						SpecialType.System_Int16 => true,
						SpecialType.System_UInt16 => false,
						SpecialType.System_Int32 => true,
						SpecialType.System_UInt32 => false,
						SpecialType.System_Int64 => true,
						SpecialType.System_UInt64 => false,

						_ => throw new Exception($"The enum underlying type '{propertyType.EnumUnderlyingType}' is not expected."),
					};

					var definedFlags = propertyType.GetFields().Select(field => field.Name).ToArray();
					var flags = value.Split(',')
						.Select(x => x.Trim())
						.Select(x => new
						{
							Text = x,
							DefinedName = definedFlags.FirstOrDefault(y => y.Equals(x, StringComparison.OrdinalIgnoreCase)),
							IsValidNumeric = x.SkipWhile((c, i) => isEnumSigned && i == 0 && c == '-').All(char.IsNumber),
						})
						.ToArray();

					// FIXME: UWP throws on undefined numerical value for some enum type like <StackPanel Orientation="2" />, but not on some others (eg: user-defined enums)".
					// Setting the same Orientation to 2 in code behind is fine however...
					// Given the logic is not well understood, we are ignoring this behavior here.
					if (flags.Any(x => x.DefinedName == null && !x.IsValidNumeric))
					{
						throw new Exception($"Failed to create a '{propertyTypeWithoutGlobal}' from the text '{value}'.");
					}

					var values = flags.Select(x => x.DefinedName is { }
						? $"global::{propertyTypeWithoutGlobal}.{x.DefinedName}" // x.Text may not be properly cased
						: $"(global::{propertyTypeWithoutGlobal})({x.Text})" // Parentheses are needed to cast negative value (CS0075)
					).ToArray();

					return string.Join("|", values);
				}

				var hasImplictToString = propertyType
					.GetMethodsWithName("op_Implicit")
					.Any(m => m.Parameters.FirstOrDefault().SelectOrDefault(p => p?.Type.SpecialType == SpecialType.System_String)
					);

				if (hasImplictToString

					// Can be an object (e.g. in case of Binding.ConverterParameter).
					|| propertyType.SpecialType == SpecialType.System_Object
				)
				{
					if (isTemplateBindingAttachedProperty)
					{
						return "@\"(" + memberValue + ")\"";
					}
					else
					{
						return "@\"" + memberValue + "\"";
					}
				}

				if (memberValue == null && propertyType.IsReferenceType)
				{
					return "null";
				}

				throw new Exception("Unable to convert {0} for {1} with type {2}".InvariantCultureFormat(memberValue, memberName, propertyType));

				static string? SplitAndJoin(string? value)
					=> value == null ? null : splitRegex.Replace(value, ", ");

				string RewriteUri(string? rawValue)
				{
					if (rawValue is not null
						&& Uri.TryCreate(rawValue, UriKind.RelativeOrAbsolute, out var parsedUri)
						&& !parsedUri.IsAbsoluteUri)
					{
						var declaringType = FindFirstConcreteAncestorType(owner?.Owner);

						if (
							declaringType.Is(Generation.ImageSourceSymbol.Value)
							|| declaringType.Is(Generation.ImageSymbol.Value)
						)
						{
							var uriBase = rawValue.StartsWith("/", StringComparison.Ordinal)
								? "\"ms-appx:///\""
								: $"__baseUri_prefix_{_fileUniqueId}";

							return $"{uriBase} + \"{rawValue.TrimStart('/')}\"";
						}
						else
						{
							// Breaking change, support for ms-resource:// for non framework owners (https://github.com/unoplatform/uno/issues/8339)
						}
					}

					return $"@\"{rawValue}\"";
				}
			}
		}

		/// <summary>
		/// Finds the first ancestor type that is resolving to a known type (excluding _unknownContent members)
		/// </summary>
		private INamedTypeSymbol? FindFirstConcreteAncestorType(XamlObjectDefinition? objectDefinition)
		{
			while (objectDefinition is not null)
			{
				if (FindType(objectDefinition.Type) is { } type)
				{
					return type;
				}

				objectDefinition = objectDefinition.Owner;
			}

			return null;
		}

		private string? BuildLocalizedResourceValue(INamedTypeSymbol? owner, string memberName, string objectUid)
		{
			//windows 10 localization concat the xUid Value with the member value (Text, Content, Header etc...)
			string fullKey;

			if (owner != null && IsAttachedProperty(owner, memberName))
			{
				var declaringType = owner;
				var nsRaw = declaringType.ContainingNamespace.ToDisplayString();
				var type = declaringType.Name;

				fullKey = $"{objectUid}.[using:{nsRaw}]{type}.{memberName}";
			}
			else
			{
				fullKey = objectUid + "." + memberName;
			}

			if (_resourceDetailsCollection.FindByKey(fullKey) is { } resourceDetail)
			{
				var viewName = _isInsideMainAssembly
					? resourceDetail.FileName
					: resourceDetail.Assembly + "/" + resourceDetail.FileName;

				return $"global::Uno.UI.Helpers.MarkupHelper.GetResourceStringForXUid(\"{viewName}\", \"{RewriteResourceKeyName(resourceDetail.Key)}\")";
			}

			return null; // $"null /*{fullKeyWithLibrary}*/";
		}

		private StringBuilder _keyRewriteBuilder = new();
		private string RewriteResourceKeyName(string keyName)
		{
			var firstDotIndex = keyName.IndexOf('.');
			if (firstDotIndex != -1)
			{
				_keyRewriteBuilder.Clear();
				_keyRewriteBuilder.Append(keyName);

				_keyRewriteBuilder[firstDotIndex] = '/';

				return _keyRewriteBuilder.ToString();
			}

			return keyName;
		}

		private bool IsPropertyLocalized(XamlObjectDefinition obj, string propertyName)
		{
			var uid = GetObjectUid(obj);
			var isLocalized = uid != null && BuildLocalizedResourceValue(null, propertyName, uid) != null;

			return isLocalized;
		}

		private string ParseCacheMode(string memberValue)
		{
			if (memberValue.Equals("BitmapCache", StringComparison.OrdinalIgnoreCase))
			{
				return "new global::Windows.UI.Xaml.Media.BitmapCache()";
			}

			throw new Exception($"The [{memberValue}] cache mode is not supported");
		}

		private string BuildTargetPropertyPath(string target, XamlMemberDefinition? owner)
		{
			// https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter.target?view=winrt-22621
			// The Setter.Target property can be used in either a Style or a VisualState, but in different ways.
			// - When used in a Style, the property that needs to be modified can be specified directly.
			// - When used in VisualState, the Target property must be given a TargetPropertyPath(dotted syntax with a target element and property explicitly specified).
			if (CurrentStyleTargetType is not null)
			{
				// Target is used in a style, so it defines the DP directly.
				return BuildDependencyProperty(target);
			}

			var ownerControl = GetControlOwner(owner?.Owner);
			if (ownerControl != null)
			{
				// This builds property setters for specified member setter.
				var separatorIndex = target.IndexOf(".", StringComparison.Ordinal);
				var elementName = target.Substring(0, separatorIndex);
				var targetElement = FindSubElementByName(ownerControl, elementName);
				if (targetElement != null)
				{
					var propertyName = target.Substring(separatorIndex + 1);
					// Attached properties need to be expanded using the namespace, otherwise the resolution will be
					// performed at runtime at a higher cost.
					propertyName = RewriteAttachedPropertyPath(propertyName);
					return $"new global::Windows.UI.Xaml.TargetPropertyPath(this._{elementName}Subject, \"{propertyName}\")";
				}
				else
				{
					return $"/* target element not found {elementName} */ null";
				}
			}

			throw new InvalidOperationException("GetControlOwner returned null.");
		}

		private static string? DoubleEscape(string? thisString)
		{
			//http://stackoverflow.com/questions/366124/inserting-a-tab-character-into-text-using-c-sharp
			return thisString
				?.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\t", "\\t");
		}

		private static string ParseTimeSpan(string memberValue)
		{
			var value = TimeSpan.Parse(memberValue, CultureInfo.InvariantCulture);

			return $"global::System.TimeSpan.FromTicks({value.Ticks} /* {memberValue} */)";
		}

		private static string BuildGridLength(string memberValue)
		{
			var gridLength = Windows.UI.Xaml.GridLength.ParseGridLength(memberValue).FirstOrDefault();

			return $"new global::{XamlConstants.Types.GridLength}({gridLength.Value.ToStringInvariant()}f, global::{XamlConstants.Types.GridUnitType}.{gridLength.GridUnitType})";
		}

		private string BuildLiteralValue(XamlMemberDefinition member, INamedTypeSymbol? propertyType = null, XamlMemberDefinition? owner = null, string objectUid = "", bool isTemplateBindingAttachedProperty = false)
		{
			var literal = Inner();
			TryAnnotateWithGeneratorSource(ref literal);

			return literal;

			string Inner()
			{
				if (member.Objects.None())
				{
					var memberValue = member.Value?.ToString();

					var originalType = propertyType;

					propertyType = propertyType ?? FindPropertyType(member.Member);



					if (propertyType != null)
					{
						var s = BuildLiteralValue(propertyType, isTemplateBindingAttachedProperty, memberValue, owner ?? member, member.Member.Name, objectUid);
						return s;
					}
					else
					{
						throw new Exception($"The property {member.Owner?.Type?.Name}.{member.Member?.Name} is unknown. Line number: {member.LineNumber}, Line position: {member.LinePosition}, File: {_fileDefinition.FilePath}");
					}
				}
				else
				{
					var expression = member.Objects.First();

					if (expression.Type.Name == "StaticResource" || expression.Type.Name == "ThemeResource")
					{
						return GetSimpleStaticResourceRetrieval(propertyType, expression.Members.First().Value?.ToString());
					}
					else if (expression.Type.Name == "NullExtension")
					{
						return "null";
					}
					else
					{
						throw new NotSupportedException("MarkupExtension {0} is not supported.".InvariantCultureFormat(expression.Type.Name));
					}
				}
			}
		}

		private string BuildFontWeight(string memberValue)
		{
			var fontWeights = Generation.FontWeightsSymbol.Value;
			var fontWeight = fontWeights.GetProperties().FirstOrDefault(p => p.Name.Equals(memberValue, StringComparison.OrdinalIgnoreCase))?.Name;
			if (fontWeight is not null)
			{
				return $"global::{XamlConstants.Types.FontWeights}.{fontWeight}";
			}
			else
			{
				return $"global::{XamlConstants.Types.FontWeights}.Normal /* Warning {memberValue} is not supported on this platform */";
			}
		}

		private INamedTypeSymbol? GetDependencyPropertyTypeForSetter(string property)
		{
			property = property.Trim('(', ')');
			// Handle attached properties
			var isAttachedProperty = property.Contains(".");
			if (isAttachedProperty)
			{
				var separatorIndex = property.IndexOf('.');

				var target = property.Remove(separatorIndex);
				property = property.Substring(separatorIndex + 1);
				var type = FindType(target);
				return _metadataHelper.GetAttachedPropertyType(type!, property);
			}

			return _metadataHelper.FindPropertyTypeByOwnerSymbol(CurrentStyleTargetType, property);
		}

		private string BuildDependencyProperty(string property)
		{
			property = property.Trim('(', ')');
			// Handle attached properties
			var isAttachedProperty = property.Contains(".");
			if (isAttachedProperty)
			{
				var separatorIndex = property.IndexOf('.');

				var target = property.Remove(separatorIndex);
				property = property.Substring(separatorIndex + 1);
				var foundFullTargetType = FindType(target)?.GetFullyQualifiedTypeIncludingGlobal();

				return $"{foundFullTargetType}.{property}Property";
			}

			var currentStyleTargetType = CurrentStyleTargetType;
			if (currentStyleTargetType is null)
			{
				throw new InvalidOperationException($"Cannot convert '{property}' to DependencyProperty");
			}

			if (IsDependencyProperty(currentStyleTargetType, property))
			{
				return $"{currentStyleTargetType.GetFullyQualifiedTypeIncludingGlobal()}.{property}Property";
			}

			throw new InvalidOperationException($"{property} is not a DependencyProperty in type {currentStyleTargetType.GetFullyQualifiedTypeExcludingGlobal()}");
		}

		private string BuildBrush(string memberValue)
		{
			var colors = (INamedTypeSymbol)_metadataHelper.GetTypeByFullName(XamlConstants.Types.Colors);

			// This ensures that a memberValue "DarkGoldenRod" gets converted to colorName "DarkGoldenrod" (notice the lowercase 'r')
			var colorName = colors.GetProperties().FirstOrDefault(m => m.Name.Equals(memberValue, StringComparison.OrdinalIgnoreCase))?.Name;
			if (colorName != null)
			{
				return $"new global::{XamlConstants.Types.SolidColorBrush}(global::{XamlConstants.Types.Colors}.{colorName})";
			}
			else
			{
				memberValue = ColorCodeParser.ParseColorCode(memberValue);
				return $"new global::{XamlConstants.Types.SolidColorBrush}(global::{XamlConstants.Types.Color}.FromArgb({{0}}))".InvariantCultureFormat(memberValue);
			}
		}

		private static string BuildThickness(string memberValue)
		{
			// This is until we find an appropriate way to convert strings to Thickness.
			if (!memberValue.Contains(","))
			{
				memberValue = ReplaceWhitespaceByCommas(memberValue);
			}

			if (memberValue.Contains("."))
			{
				// Append 'f' to every decimal value in the thickness
				memberValue = AppendFloatSuffix(memberValue);
			}

			return "new global::Windows.UI.Xaml.Thickness(" + memberValue + ")";
		}

		private static string BuildCornerRadius(string memberValue)
		{
			// values can be separated by commas or whitespace
			// ensure commas are used for the constructor
			if (!memberValue.Contains(","))
			{
				memberValue = ReplaceWhitespaceByCommas(memberValue);
			}

			return $"new {XamlConstants.Types.CornerRadius}({memberValue})";
		}

		private static string ReplaceWhitespaceByCommas(string memberValue)
		{
			// empty delimiter array = whitespace in string.Split
			return string.Join(",", memberValue.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
		}

		private static string AppendFloatSuffix(string memberValue)
		{
			return memberValue.Split(',')
					.Select(s => s.Contains(".") ? s + "f" : s)
					.Aggregate((s1, s2) => "{0},{1}".InvariantCultureFormat(s1, s2));
		}

		private string BuildColor(string memberValue)
		{
			var colorsSymbol = Generation.ColorsSymbol.Value;

			if (colorsSymbol.GetProperties().FirstOrDefault(p =>
				SymbolEqualityComparer.Default.Equals(p.Type, Generation.ColorSymbol.Value) &&
					p.Name.Equals(memberValue, StringComparison.OrdinalIgnoreCase)) is IPropertySymbol property)
			{
				return $"{GlobalPrefix}{XamlConstants.Types.Colors}.{property.Name}";
			}
			else
			{
				memberValue = ColorCodeParser.ParseColorCode(memberValue);

				return $"{GlobalPrefix}{XamlConstants.Types.ColorHelper}.FromARGB({memberValue})";
			}
		}

		private string BuildBindingOption(XamlMemberDefinition m, INamedTypeSymbol? propertyType, bool isTemplateBindingAttachedProperty)
		{
			// The default member is Path
			var isPositionalParameter = m.Member.Name == "_PositionalParameters";
			var memberName = isPositionalParameter ? "Path" : m.Member.Name;

			if (m.Objects.Count > 0)
			{
				var bindingType = m.Objects.First();

				if (
					bindingType.Type.Name == "StaticResource"
					|| bindingType.Type.Name == "ThemeResource"
					)
				{
					var resourceName = bindingType.Members.First().Value?.ToString();
					return GetSimpleStaticResourceRetrieval(propertyType, resourceName);
				}

				if (bindingType.Type.Name == "RelativeSource")
				{
					var firstMember = bindingType.Members.First();
					var resourceName = firstMember.Value?.ToString();
					if (resourceName == null)
					{
						resourceName = firstMember.Objects.SingleOrDefault()?.Members?.SingleOrDefault()?.Value?.ToString();
					}

					return $"new RelativeSource(RelativeSourceMode.{resourceName})";
				}

				if (bindingType.Type.Name == "NullExtension")
				{
					return "null";
				}

				if (IsCustomMarkupExtensionType(bindingType.Type))
				{
					return GetCustomMarkupExtensionValue(m);
				}

				if (memberName == "Converter" &&
					m.Objects.SingleOrDefault() is XamlObjectDefinition { } converterObjectDefinition)
				{
					var fullTypeName = converterObjectDefinition.Type.Name;
					var knownType = FindType(converterObjectDefinition.Type);
					if (knownType == null && converterObjectDefinition.Type.PreferredXamlNamespace.StartsWith("using:", StringComparison.Ordinal))
					{
						fullTypeName = converterObjectDefinition.Type.PreferredXamlNamespace.TrimStart("using:") + "." + converterObjectDefinition.Type.Name;
					}
					if (knownType != null)
					{
						// Override the using with the type that was found in the list of loaded assemblies
						fullTypeName = knownType.GetFullyQualifiedTypeExcludingGlobal();
					}

					return $"new {GetGlobalizedTypeName(fullTypeName)}()";
				}

				if (FindType(bindingType.Type) is INamedTypeSymbol namedTypeSymbol &&
					m.Objects.SingleOrDefault()?.Members?.SingleOrDefault() is XamlMemberDefinition { } innerMember)
				{
					m = innerMember;
				}
				else
				{
					// If type specified in the binding was not found, log and return an error message
					if (!string.IsNullOrEmpty(bindingType.Type.Name))
					{
						var message = $"null\r\n#error Invalid binding: {bindingType.Type.Name} could not be found.\r\n";

						return message;
					}

					return $"null\r\n#error Invalid binding. Location: ({m.LineNumber}, {m.LinePosition})\r\n";
				}
			}

			if (memberName == "Path")
			{
				var value = BuildLiteralValue(m, GetPropertyTypeByOwnerSymbol(Generation.DataBindingSymbol.Value, memberName, m.LineNumber, m.LinePosition), isTemplateBindingAttachedProperty: isTemplateBindingAttachedProperty);
				value = RewriteAttachedPropertyPath(value);
				return value;
			}
			else if (memberName == "ElementName")
			{
				if (m.Value == null)
				{
					throw new InvalidOperationException($"The property ElementName cannot be empty");
				}

				// Skip the literal value, use the elementNameSubject instead
				var elementName = m.Value.ToString() ?? "";
				var value = "_" + elementName + "Subject";

				// Track referenced ElementNames
				CurrentScope.ReferencedElementNames.Add(elementName);

				return value;
			}
			else
			{
				var shouldMatchPropertyType =
					// FallbackValue can match the type of the property being bound.
					memberName == "FallbackValue" ||
					// TargetNullValue possibly doesn't match the type of the property being bound, if there is a converter
					(memberName == "TargetNullValue" &&
						m.Owner != null &&
						m.Owner.Members.None(otherMember => otherMember.Member.Name == "Converter"));
				propertyType = shouldMatchPropertyType
					? propertyType
					: GetPropertyTypeByOwnerSymbol(Generation.DataBindingSymbol.Value, memberName, m.LineNumber, m.LinePosition);
				var targetValueType = propertyType;

				// If value is typed and the property is not, the value type should be preserved.
				var explicitCast = string.Empty;
				if (propertyType?.SpecialType == SpecialType.System_Object &&
					!isPositionalParameter &&
					FindType(m.Owner?.Type) is { } actualValueType)
				{
					targetValueType = actualValueType;

					// These members of Binding will upcast enum to its underlying type.
					if (actualValueType.TypeKind == TypeKind.Enum && (
						memberName == "ConverterParameter" ||
						memberName == "FallbackValue" ||
						memberName == "TargetNullValue"
					))
					{
						explicitCast = actualValueType.EnumUnderlyingType!.SpecialType switch
						{
							// CS1008 Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
							SpecialType.System_Byte => "(byte)",
							SpecialType.System_SByte => "(sbyte)",
							SpecialType.System_Int16 => "(short)",
							SpecialType.System_UInt16 => "(ushort)",
							SpecialType.System_Int32 => "(int)",
							SpecialType.System_UInt32 => "(uint)",
							SpecialType.System_Int64 => "(long)",
							SpecialType.System_UInt64 => "(ulong)",

							_ => throw new Exception($"The enum underlying type '{actualValueType.EnumUnderlyingType}' is not expected."),
						};
					}
				}

				var value = BuildLiteralValue(m, targetValueType);
				value = explicitCast + value;

				return value;
			}
		}

		private string RewriteAttachedPropertyPath(string value)
		{
			if (value.Contains("("))
			{
				foreach (var ns in _fileDefinition.Namespaces)
				{
					if (ns != null)
					{
						var clrNamespace = ns.Namespace.TrimStart("using:");

						value = value.Replace("(" + ns.Prefix + ":", "(" + clrNamespace + ":");
					}
				}

				var match = Regex.Match(value, @"(\(.*?\))");

				if (match.Success)
				{
					do
					{
						if (!match.Value.Contains(":"))
						{
							// if there is no ":" this means that the type is using the default
							// namespace, so try to resolve the best way we can.

							var parts = match.Value.Trim(_parenthesesArray).Split(_dotArray);

							if (parts.Length == 2)
							{
								var targetType = FindType(parts[0]);

								if (targetType != null)
								{
									var newPath = targetType.ContainingNamespace.ToDisplayString() + ":" + targetType.Name + "." + parts[1];

									value = value.Replace(match.Value, '(' + newPath + ')');
								}
							}
						}
					}
					while ((match = match.NextMatch()).Success);
				}
			}

			return value;
		}

		private void BuildLiteralProperties(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, string? closureName = null)
		{
			var extendedProperties = GetExtendedProperties(objectDefinition);

			bool PropertyFilter(XamlMemberDefinition member)
			{
				var type = FindType(member.Member.DeclaringType);
				return IsType(objectDefinition.Type, type)
					&& !IsAttachedProperty(type, member.Member.Name)
					&& !IsLazyVisualStateManagerProperty(member)
					&& _metadataHelper.FindEventType(type, member.Member.Name) == null
					&& member.Member.Name != "_UnknownContent"; // We are defining the elements of a collection explicitly declared in XAML
			}

			BuildLiteralPropertiesWithFilter(writer, objectDefinition, closureName, extendedProperties, PropertyFilter);
		}

		private void BuildLiteralLazyVisualStateManagerProperties(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, string? closureName = null)
		{
			var extendedProperties = GetExtendedProperties(objectDefinition);

			bool PropertyFilter(XamlMemberDefinition member)
				=> IsLazyVisualStateManagerProperty(member);

			BuildLiteralPropertiesWithFilter(writer, objectDefinition, closureName, extendedProperties, PropertyFilter);
		}

		private void BuildLiteralPropertiesWithFilter(
			IIndentedStringBuilder writer,
			XamlObjectDefinition objectDefinition,
			string? closureName,
			IEnumerable<XamlMemberDefinition> extendedProperties,
			Func<XamlMemberDefinition, bool> propertyPredicate
		)
		{
			if (extendedProperties.Any())
			{
				TryAnnotateWithGeneratorSource(writer);

				var objectUid = GetObjectUid(objectDefinition);
				var closingPunctuation = string.IsNullOrWhiteSpace(closureName) ? "," : ";";

				foreach (var member in extendedProperties)
				{
					var fullValueSetter = string.IsNullOrWhiteSpace(closureName) ? member.Member!.Name : "{0}.{1}".InvariantCultureFormat(closureName, member.Member!.Name);

					// Exclude attached properties, must be set in the extended apply section.
					// If there is no type attached, this can be a binding.
					// Exclude lazy initialized properties, those are always in the extended block.
					if (propertyPredicate(member))
					{
						if (member.Objects.None())
						{
							if (IsInitializableCollection(member.Member))
							{
								// WinUI Grid succinct syntax
								if (member.Owner?.Type.Name == "Grid" &&
									member.Owner?.Type.PreferredXamlNamespace == XamlConstants.PresentationXamlXmlNamespace &&
									(member.Member.Name == "ColumnDefinitions" || member.Member.Name == "RowDefinitions") &&
									member.Member.PreferredXamlNamespace == XamlConstants.PresentationXamlXmlNamespace &&
									member.Value is string definitions)
								{
									using (writer.BlockInvariant($"{fullValueSetter} = "))
									{
										var propertyName = member.Member.Name == "ColumnDefinitions"
											? "Width"
											: "Height";

										var values = definitions
											.Split(',')
											.Select(static definition => definition.Trim())
											.ToArray();

										foreach (var value in values)
										{
											using (writer.BlockInvariant($"new global::{(member.Member.Name == "ColumnDefinitions" ? XamlConstants.Types.ColumnDefinition : XamlConstants.Types.RowDefinition)}"))
											{
												writer.AppendLineInvariantIndented("{0} = {1}", propertyName, BuildGridLength(value));
											}

											writer.AppendLineIndented(",");
										}
									}
									writer.AppendLineIndented(",");
								}
								else
								{
									writer.AppendLineIndented("// Empty collection");
								}
							}
							else
							{
								INamedTypeSymbol? setterPropertyType = null;
								if (member.Owner?.Type.Name == "Setter" &&
									member.Member.Name == "Value")
								{
									if (FindMember(member.Owner, "Property") is { Value: string setterPropertyName })
									{
										setterPropertyType = GetDependencyPropertyTypeForSetter(setterPropertyName);
									}
									else if (CurrentStyleTargetType is not null && GetMember(member.Owner, "Target") is { Value: string setterTarget })
									{
										// TODO: Confirm if (or not) we need to find the type even if we are not in a Style.
										setterPropertyType = GetDependencyPropertyTypeForSetter(setterTarget);
									}
								}
								else if (member.Owner?.Type.Name == "Setter" && member.Member.Name == "Target" && CurrentStyleTargetType is not null)
								{
									// A setter's Target inside a style will set Property instead of Target
									fullValueSetter = string.IsNullOrWhiteSpace(closureName) ? "Property" : $"{closureName}.Property";
								}

								if (FindPropertyType(member.Member) != null)
								{
									writer.AppendLineInvariantIndented("{0} = {1}{2}", fullValueSetter, BuildLiteralValue(member, propertyType: setterPropertyType, objectUid: objectUid ?? ""), closingPunctuation);
								}
								else
								{
									if (IsRelevantNamespace(member?.Member?.PreferredXamlNamespace)
										&& IsRelevantProperty(member?.Member, objectDefinition))
									{
										GenerateError(
											writer,
											"Property {0} does not exist on {1}, name is {2}, preferrednamespace {3}",
											fullValueSetter,
											member?.Member?.DeclaringType,
											member?.Member?.Name,
											member?.Member?.PreferredXamlNamespace);
									}
									else
									{
										GenerateSilentWarning(
											writer,
											"Property {0} does not exist on {1}, the namespace is {2}. This error was considered irrelevant by the XamlFileGenerator",
											fullValueSetter,
											member?.Member?.DeclaringType,
											member?.Member?.PreferredXamlNamespace);
									}
								}
							}
						}
						else
						{
							var nonBindingObjects = member
								.Objects
								.Where(m =>
									m.Type.Name != "Binding"
									&& m.Type.Name != "Bind"
									&& m.Type.Name != "StaticResource"
									&& m.Type.Name != "ThemeResource"
									&& m.Type.Name != "CustomResource"
									&& m.Type.Name != "TemplateBinding"
									&& !IsCustomMarkupExtensionType(m.Type)
								);

							if (nonBindingObjects.Any())
							{
								var isInitializableCollection = IsInitializableCollection(member.Member);
								var isInlineCollection = IsInlineCollection(member.Member, nonBindingObjects);

								if (isInlineCollection && closureName != null)
								{
									foreach (var child in nonBindingObjects)
									{
										writer.AppendLineIndented($"{fullValueSetter}.Add(");
										using (writer.Indent())
										{
											BuildChild(writer, member, child);
										}
										writer.AppendLineIndented(");");
									}
								}
								else if (isInitializableCollection || isInlineCollection)
								{
									using (writer.BlockInvariant($"{fullValueSetter} = "))
									{
										foreach (var child in nonBindingObjects)
										{
											BuildChild(writer, member, child);
											writer.AppendLineIndented(",");
										}
									}
								}
								else
								{
									writer.AppendIndented($"{fullValueSetter} = ");
									var nonBindingObject = nonBindingObjects.First();
									using (TryAdaptNative(writer, nonBindingObject, FindPropertyType(member.Member)))
									{
										BuildChild(writer, member, nonBindingObject);
									}
								}

								writer.AppendLineIndented(closingPunctuation);
							}
						}
					}
					else if (member.Member.DeclaringType == null && member.Member.Name == "Name")
					{
						// This is a special case, where the declaring type is from the x: namespace,
						// but is considered of an unknown type. This can happen when providing the
						// name of a control using x:Name instead of Name.
						var hasNameProperty = HasProperty(objectDefinition.Type, "Name");
						if (hasNameProperty)
						{
							writer.AppendLineInvariantIndented("{0} = \"{1}\"{2}", fullValueSetter, member.Value, closingPunctuation);
						}
					}
				}
			}
		}

		private bool IsLazyVisualStateManagerProperty(XamlMemberDefinition member)
			=> _isLazyVisualStateManagerEnabled
				&& member.Owner != null
				&& member.Owner.Type.Name switch
				{
					"VisualState" => (member.Member.Name == "Storyboard"
									|| member.Member.Name == "Setters") && !HasDescendantsWithXName(member),
					"VisualTransition" => member.Member.Name == "Storyboard" && !HasDescendantsWithXName(member),
					_ => false,
				};

		private bool IsLazyVisualStateManagerProperty(IPropertySymbol property)
			=> _isLazyVisualStateManagerEnabled
				&& property.ContainingSymbol.Name switch
				{
					"VisualState" => property.Name == "Storyboard"
									|| property.Name == "Setters",
					"VisualTransition" => property.Name == "Storyboard",
					_ => false,
				};

		/// <summary>
		/// Determines if the member is inline initializable and the first item is not a new collection instance
		/// </summary>
		private bool IsInlineCollection(XamlMember member, IEnumerable<XamlObjectDefinition> elements)
		{
			var declaringType = member.DeclaringType;
			var propertyName = member.Name;
			var property = GetPropertyWithName(declaringType, propertyName);

			if (property?.Type is INamedTypeSymbol collectionType)
			{
				if (IsCollectionOrListType(collectionType) && elements.FirstOrDefault() is XamlObjectDefinition first)
				{
					var firstElementType = GetType(first.Type);

					return !SymbolEqualityComparer.Default.Equals(collectionType, firstElementType);
				}
			}

			return false;
		}

		private static bool IsLocalizedString(INamedTypeSymbol? propertyType, string? objectUid)
		{
			return !objectUid.IsNullOrEmpty() && IsLocalizablePropertyType(propertyType);
		}

		internal static bool IsLocalizablePropertyType(INamedTypeSymbol? propertyType)
		{
			return propertyType is { SpecialType: SpecialType.System_String or SpecialType.System_Object };
		}

		private string? GetObjectUid(XamlObjectDefinition objectDefinition)
		{
			string? objectUid = null;
			var localizedObject = FindMember(objectDefinition, "Uid");
			if (localizedObject?.Value != null)
			{
				objectUid = localizedObject.Value.ToString();
			}

			return objectUid;
		}

		/// <summary>
		/// Gets a basic class that implements a know collection or list interface
		/// </summary>
		private bool GetKnownNewableListOrCollectionInterface(INamedTypeSymbol? type, out string? newableTypeName)
		{
			switch (type?.Name)
			{
				case "ICollection":
				case "IList":
					newableTypeName = type.IsGenericType ?
						"List<{0}>".InvariantCultureFormat(GetFullGenericTypeName(type.TypeArguments.Single() as INamedTypeSymbol)) :
						"List<System.Object>";
					return true;
			};

			newableTypeName = null;
			return false;
		}

		private IEnumerable<XamlMemberDefinition> GetExtendedProperties(XamlObjectDefinition objectDefinition)
		{
			var objectUid = GetObjectUid(objectDefinition);
			var hasUnkownContentOnly = objectDefinition.Members.All(m => m.Member.Name == "_UnknownContent");

			return objectDefinition
				.Members
				.Where(m =>
					(
						m.Member.Name != "_UnknownContent"

						&& m.Member.Name != "Resources"
						&& m.Member.Name != "Key"
					)
					||
					(
						m.Member.Name == "Content" && HasMarkupExtension(m)
					)
					||
					(
						m.Member.Name == "Content" && IsLocalizedString(FindPropertyType(m.Member), objectUid)
					)
					||
					(
						m.Member.Name == "Style" && HasBindingMarkupExtension(m)
					)
					|| IsAttachedProperty(m)
					|| IsLazyVisualStateManagerProperty(m)
					||
					(
						// If the object is a collection and it has both _UnknownContent (i.e. an item) and other properties defined,
						// we cannot use property ADN collection initializer syntax at same time, so we will add items using an ApplyBlock.
						m.Member.Name == "_UnknownContent" && !hasUnkownContentOnly && FindType(m.Owner?.Type) is INamedTypeSymbol type && IsCollectionOrListType(type)
					)
				);
		}

		private void BuildChild(IIndentedStringBuilder writer, XamlMemberDefinition? owner, XamlObjectDefinition xamlObjectDefinition, string? outerClosure = null)
		{
			_generatorContext.CancellationToken.ThrowIfCancellationRequested();

			TryAnnotateWithGeneratorSource(writer);
			var typeName = xamlObjectDefinition.Type.Name;
			var fullTypeName = xamlObjectDefinition.Type.Name;

			var knownType = FindType(xamlObjectDefinition.Type);

			if (knownType == null && xamlObjectDefinition.Type.PreferredXamlNamespace.StartsWith("using:", StringComparison.Ordinal))
			{
				fullTypeName = xamlObjectDefinition.Type.PreferredXamlNamespace.TrimStart("using:") + "." + xamlObjectDefinition.Type.Name;
			}

			if (knownType != null)
			{
				// Override the using with the type that was found in the list of loaded assemblies
				fullTypeName = knownType.GetFullyQualifiedTypeExcludingGlobal();
			}

			using (TrySetDefaultBindMode(xamlObjectDefinition))
			{
				var isItemsPanelTemplate = typeName == "ItemsPanelTemplate";

				if (
					typeName == "DataTemplate"
					|| isItemsPanelTemplate
					|| typeName == "ControlTemplate"

					// This case is specific the custom ListView for iOS. Should be removed
					// when the list rebuilt to be compatible.
					|| typeName == "ListViewBaseLayoutTemplate"
				)
				{
					writer.AppendIndented($"new {GetGlobalizedTypeName(fullTypeName)}(");

					// The unknown content is what's inside the DataTemplate element, but for the location we want the position of the "DataTemplate" element.
					var contentOwner = xamlObjectDefinition.Members.FirstOrDefault(m => m.Member.Name == "_UnknownContent");
					var contentLocation = (IXamlLocation)xamlObjectDefinition.Members.FirstOrDefault(m => m.Member.Name == "Key") ?? xamlObjectDefinition;

					if (contentOwner != null)
					{
						var resourceOwner = CurrentResourceOwnerName;

						writer.Append($"{resourceOwner} , __owner => ");
						// This case is to support the layout switching for the ListViewBaseLayout, which is not
						// a FrameworkTemplate. This will need to be removed when this custom list view is removed.
						var returnType = typeName == "ListViewBaseLayoutTemplate" ? "global::Uno.UI.Controls.Legacy.ListViewBaseLayout" : "_View";

						BuildChildThroughSubclass(writer, contentOwner, returnType);

						writer.AppendIndented(")");
						if (_isHotReloadEnabled)
						{
							using (var applyWriter = CreateApplyBlock(writer, null, out var closureName))
							{
								TrySetOriginalSourceLocation(applyWriter, closureName, contentLocation.LineNumber, contentLocation.LinePosition);
							}
						}

					}
					else
					{
						writer.AppendIndented("/* This template does not have a content for this platform */)");
					}
				}
				else if (typeName == "NullExtension")
				{
					writer.AppendIndented("null");
				}
				else if (
					typeName == "StaticResource"
					|| typeName == "ThemeResource"
					|| typeName == "Binding"
					|| typeName == "Bind"
					|| typeName == "TemplateBinding"
					)
				{
					if (owner == null)
					{
						throw new InvalidOperationException($"An owner is required for {typeName}");
					}

					BuildComplexPropertyValue(writer, owner, null, closureName: outerClosure, generateAssignation: outerClosure != null);
				}
				else if (HasInitializer(xamlObjectDefinition))
				{
					BuildInitializer(writer, xamlObjectDefinition, owner);
					BuildLiteralProperties(writer, xamlObjectDefinition);
				}
				// TODO: Remove this else if in Uno 6 as a breaking change.
				else if (fullTypeName == XamlConstants.Types.Setter && CurrentStyleTargetType is { } currentStyleTargetType && IsLegacySetter(xamlObjectDefinition, out var propertyName))
				{
					var propertyType = GetDependencyPropertyTypeForSetter(propertyName);
					var valueNode = FindMember(xamlObjectDefinition, "Value");
					writer.AppendLineInvariantIndented(
						"new global::Windows.UI.Xaml.Setter<{0}>(\"{1}\", o => o.{1} = {2})",
						currentStyleTargetType.GetFullyQualifiedTypeIncludingGlobal(),
						propertyName,
						BuildLiteralValue(valueNode!, propertyType)
					);

				}
				else if (fullTypeName == XamlConstants.Types.ResourceDictionary)
				{
					InitializeAndBuildResourceDictionary(writer, xamlObjectDefinition, setIsParsing: true);
					BuildExtendedProperties(writer, xamlObjectDefinition);
				}
				else
				{
					var hasCustomInitalizer = HasCustomInitializer(knownType);

					if (hasCustomInitalizer)
					{
						var implicitContent = FindImplicitContentMember(xamlObjectDefinition);
						if (implicitContent == null)
						{
							throw new InvalidOperationException($"Unable to find content value on {xamlObjectDefinition}");
						}

						writer.AppendIndented(BuildLiteralValue(implicitContent, knownType, owner));
					}
					else
					{
						var isStyle = fullTypeName == XamlConstants.Types.Style;
						if (isStyle)
						{
							var targetTypeNode = GetMember(xamlObjectDefinition, "TargetType");

							if (targetTypeNode.Value == null)
							{
								throw new InvalidOperationException("TargetType cannot be empty");
							}

							var targetType = targetTypeNode.Value.ToString();
							_currentStyleTargetTypeStack.Push(FindType(targetType));
						}

						using (TryGenerateDeferedLoadStrategy(writer, knownType, xamlObjectDefinition))
						{
							using (writer.BlockInvariant("new {0}{1}", GetGlobalizedTypeName(fullTypeName), GenerateConstructorParameters(knownType)))
							{
								TrySetParsing(writer, knownType, isInitializer: true);
								RegisterAndBuildResources(writer, xamlObjectDefinition, isInInitializer: true);
								BuildLiteralProperties(writer, xamlObjectDefinition);
								BuildProperties(writer, xamlObjectDefinition);
								BuildInlineLocalizedProperties(writer, xamlObjectDefinition, knownType);
							}

							if (fullTypeName == XamlConstants.Types.Setter && FindMember(xamlObjectDefinition, "Value") is { } valueNode &&
									valueNode.Objects.Count == 1)
							{
								var setterValueObject = valueNode.Objects[0];
								if (setterValueObject.Type.Name == "Bind")
								{
									writer.AppendLineIndented(".MakeSetterMutable()");
								}

								var isThemeResource = setterValueObject.Type.Name == "ThemeResource";
								var isStaticResourceForHotReload = _isHotReloadEnabled && setterValueObject.Type.Name == "StaticResource";
								if (isThemeResource || isStaticResourceForHotReload)
								{
									writer.AppendLineInvariantIndented(".ApplyThemeResourceUpdateValues(\"{0}\", {1}, {2}, {3})", setterValueObject.Members.FirstOrDefault()?.Value, ParseContextPropertyAccess, isThemeResource ? "true" : "false", _isHotReloadEnabled ? "true" : "false");
								}
							}

							if (isStyle)
							{
								if (_isHotReloadEnabled)
								{
									using (var applyWriter = CreateApplyBlock(writer, Generation.StyleSymbol.Value, out string closure))
									{
										TrySetOriginalSourceLocation(applyWriter, $"{closure}", xamlObjectDefinition.LineNumber, xamlObjectDefinition.LinePosition);
									}
								}
							}

							BuildExtendedProperties(writer, xamlObjectDefinition);
						}

						if (isStyle)
						{
							_currentStyleTargetTypeStack.Pop();
						}
					}
				}
			}
		}

		private bool IsLegacySetter(XamlObjectDefinition xamlObjectDefinition, out string propertyName)
		{
			var propertyNode = FindMember(xamlObjectDefinition, "Property");
			propertyName = propertyNode?.Value?.ToString()!;
			if (propertyName is not null && !propertyName.Contains('.'))
			{
				return !IsDependencyProperty(CurrentStyleTargetType, propertyName);
			}

			return false;
		}

		/// <summary>
		/// Set the 'IsParsing' flag. This should be the first property set when an element is parsed.
		/// </summary>
		private void TrySetParsing(IIndentedStringBuilder writer, INamedTypeSymbol? type, bool isInitializer)
		{
			TryAnnotateWithGeneratorSource(writer);
			if (HasIsParsing(type))
			{
				var basePrefix = !isInitializer ? "base." : string.Empty;
				var initializerSuffix = isInitializer ? "," : ";";
				writer.AppendLineIndented($"{basePrefix}IsParsing = true{initializerSuffix}");
			}
		}

		private bool HasChildrenWithPhase(XamlObjectDefinition xamlObjectDefinition)
		{
			if (xamlObjectDefinition.Owner?.Type.Name == "DataTemplate")
			{

				var q = from element in EnumerateSubElements(xamlObjectDefinition.Owner)
						let phase = FindMember(element, "Phase")?.Value
						where phase != null
						select phase;

				return q.Any();
			}

			return false;
		}

		private string? GenerateRootPhases(XamlObjectDefinition xamlObjectDefinition, string ownerVariable)
		{
			if (xamlObjectDefinition.Owner?.Type.Name == "DataTemplate")
			{
				var q = from element in EnumerateSubElements(xamlObjectDefinition.Owner)
						let phase = FindMember(element, "Phase")?.Value
						where phase != null
						select int.Parse(phase.ToString() ?? "", CultureInfo.InvariantCulture);

				var phases = q.Distinct().ToArray();

				if (phases.Length > 0)
				{
					var phasesValue = phases.OrderBy(i => i).Select(s => s.ToString(CultureInfo.InvariantCulture)).JoinBy(",");
					return $"global::Uno.UI.FrameworkElementHelper.SetDataTemplateRenderPhases({ownerVariable}, new []{{{phasesValue}}});";
				}
			}

			return null;
		}

		private XamlObjectDefinition? GetControlOwner(XamlObjectDefinition? owner)
		{
			if (owner != null)
			{
				do
				{
					var type = GetType(owner.Type);

					if (type.Is(Generation.UIElementSymbol.Value))
					{
						return owner;
					}

					owner = owner.Owner;
				}
				while (owner != null);
			}

			return null;
		}

		/// <summary>
		/// Builds the x:Name cache for faster lookups in <see cref="FindSubElementByName(XamlObjectDefinition, string)"/>.
		/// </summary>
		/// <remarks>
		/// The lookup is performed by searching for the x:Name, then determine if one of the ancestors
		/// is known. This avoids doing linear lookups at many levels recusively for the same nodes.
		/// </remarks>
		private void BuildNameCache(XamlObjectDefinition topLevelControl)
		{
			foreach (var element in EnumerateSubElements(topLevelControl))
			{
				var nameMember = FindMember(element, "Name");

				if (nameMember?.Value is string name)
				{
					if (!_nameCache.TryGetValue(name, out var list))
					{
						_nameCache[name] = list = new();
					}

					list.Add(element);
				}
			}
		}
		/// <summary>
		/// Statically finds a element by name, given a xaml element root
		/// </summary>
		/// <param name="xamlObject">The root from which to start the search</param>
		/// <param name="elementName">The x:Name value to search for</param>
		/// <returns></returns>
		private XamlObjectDefinition? FindSubElementByName(XamlObjectDefinition xamlObject, string elementName)
		{
			if (_nameCache.TryGetValue(elementName, out var list))
			{
				// Found a matching x:Name in the document, now find one that
				// is a child of the xamlObject parameter.

				foreach (var namedObject in list)
				{
					var current = namedObject;

					do
					{
						if (ReferenceEquals(xamlObject, current))
						{
							return namedObject;
						}

						current = current.Owner;

					} while (current is not null);
				}
			}

			return null;
		}

		/// <summary>
		/// Statically finds a element by name, given a xaml element root
		/// </summary>
		/// <param name="xamlObject">The root from which to start the search</param>
		/// <param name="elementName">The x:Name value to search for</param>
		/// <returns></returns>
		private IEnumerable<XamlObjectDefinition> EnumerateSubElements(XamlObjectDefinition xamlObject)
		{
			yield return xamlObject;

			foreach (var member in xamlObject.Members)
			{
				foreach (var element in EnumerateSubElements(member.Objects))
				{
					yield return element;
				}
			}

			var objects = xamlObject.Objects;

			foreach (var element in EnumerateSubElements(objects))
			{
				yield return element;
			}
		}

		private IEnumerable<XamlObjectDefinition> EnumerateSubElements(IEnumerable<XamlObjectDefinition> objects)
		{
			foreach (var child in objects.Safe())
			{
				yield return child;

				foreach (var innerElement in EnumerateSubElements(child))
				{
					yield return innerElement;
				}
			}

			yield break;
		}

		private IDisposable? TryGenerateDeferedLoadStrategy(IIndentedStringBuilder writer, INamedTypeSymbol? targetType, XamlObjectDefinition definition)
		{
			TryAnnotateWithGeneratorSource(writer);
			var strategy = FindMember(definition, "DeferLoadStrategy");
			var loadMember = FindMember(definition, "Load");
			var hasLoadMarkup = HasMarkupExtension(loadMember);

			if (strategy?.Value?.ToString()?.ToLowerInvariant() == "lazy"
				|| loadMember?.Value?.ToString()?.ToLowerInvariant() == "false"
				|| hasLoadMarkup)
			{
				var visibilityMember = FindMember(definition, "Visibility");
				var dataContextMember = FindMember(definition, "DataContext");
				var nameMember = FindMember(definition, "Name");

				var elementStubBaseType = Generation.ElementStubSymbol.Value.BaseType;
				if (!(targetType?.Is(elementStubBaseType) ?? false))
				{
					writer.AppendLineIndented($"/* Lazy DeferLoadStrategy was ignored because the target type is not based on {elementStubBaseType?.GetFullyQualifiedTypeExcludingGlobal()} */");
					return null;
				}

				if (visibilityMember != null || loadMember?.Value != null || hasLoadMarkup)
				{
					var hasVisibilityMarkup = HasMarkupExtension(visibilityMember);
					var isLiteralVisible = !hasVisibilityMarkup && (visibilityMember?.Value?.ToString() == "Visible");

					var hasDataContextMarkup = dataContextMember != null && HasMarkupExtension(dataContextMember);

					var currentOwnerName = CurrentResourceOwnerName;

					var elementStubHolderNameStatement = "";

					if (HasImplicitViewPinning)
					{
						// Build the ElemenStub builder holder variable to ensute that the element stub
						// does not keep a hard reference to "this" through the closure needed to keep
						// the namescope and other variables. The ElementStub, in turn keeps a weak
						// reference to the builder's target instance.
						var elementStubHolderName = $"_elementStubHolder_{CurrentScope.ElementStubHolders.Count}";
						elementStubHolderNameStatement = $"{elementStubHolderName} = ";
						CurrentScope.ElementStubHolders.Add(elementStubHolderName);
					}

					writer.AppendLineIndented($"new {XamlConstants.Types.ElementStub}({elementStubHolderNameStatement} () => ");

					var disposable = new DisposableAction(() =>
					{
						writer.AppendIndented(")");
					});

					var xLoadScopeDisposable = XLoadScope();

					return new DisposableAction(() =>
					{
						disposable?.Dispose();

						string closureName;
						using (var innerWriter = CreateApplyBlock(writer, Generation.ElementStubSymbol.Value, out closureName))
						{
							var elementStubType = new XamlType(XamlConstants.BaseXamlNamespace, "ElementStub", new List<XamlType>(), new XamlSchemaContext());

							if (hasDataContextMarkup)
							{
								// We need to generate the datacontext binding, since the Visibility
								// may require it to bind properly.

								GenerateBinding("DataContext", dataContextMember, definition);
							}

							if (nameMember != null)
							{
								innerWriter.AppendLineIndented(
									$"{closureName}.Name = \"{nameMember.Value}\";"
								);

								// Set the element name to the stub, then when the stub will be replaced
								// the actual target control will override it.
								innerWriter.AppendLineIndented(
									$"_{nameMember.Value}Subject.ElementInstance = {closureName};"
								);
							}

							if (hasLoadMarkup || hasVisibilityMarkup)
							{

								var members = new List<XamlMemberDefinition>();

								if (hasLoadMarkup)
								{
									members.Add(GenerateBinding("Load", loadMember, definition));
								}

								if (hasVisibilityMarkup)
								{
									members.Add(GenerateBinding("Visibility", visibilityMember, definition));
								}

								var isInsideFrameworkTemplate = IsMemberInsideFrameworkTemplate(definition).isInside;
								if ((!_isTopLevelDictionary || isInsideFrameworkTemplate)
									&& (HasXBindMarkupExtension(definition) || HasMarkupExtensionNeedingComponent(definition)))
								{
									var xamlObjectDef = new XamlObjectDefinition(elementStubType, 0, 0, definition, namespaces: null);
									xamlObjectDef.Members.AddRange(members);

									AddComponentForParentScope(xamlObjectDef);

									var componentName = CurrentScope.Components.Last().MemberName;
									writer.AppendLineIndented($"__that.{componentName} = {closureName};");

									if (!isInsideFrameworkTemplate)
									{
										EnsureXClassName();

										writer.AppendLineIndented($"var {componentName}_update_That = ({CurrentResourceOwnerName} as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference;");

										if (nameMember != null)
										{
											writer.AppendLineIndented($"var {componentName}_update_subject_capture = _{nameMember.Value}Subject;");
										}

										using (writer.BlockInvariant($"void {componentName}_update(global::Windows.UI.Xaml.ElementStub sender)"))
										{
											using (writer.BlockInvariant($"if ({componentName}_update_That.Target is {_xClassName} that)"))
											{

												using (writer.BlockInvariant($"if (sender.IsMaterialized)"))
												{
													writer.AppendLineIndented($"that.Bindings.UpdateResources();");
													if (nameMember?.Value is string xName)
													{
														writer.AppendLineIndented($"that.Bindings.NotifyXLoad(\"{xName}\");");
													}
												}
											}
										}

										writer.AppendLineIndented($"{closureName}.MaterializationChanged += {componentName}_update;");

										writer.AppendLineIndented($"var owner = this;");

										if (_isHotReloadEnabled)
										{
											// Attach the current context to itself to avoid having a closure in the lambda
											writer.AppendLineIndented($"global::Uno.UI.Helpers.MarkupHelper.SetElementProperty({closureName}, \"{componentName}_owner\", owner);");
										}

										using (writer.BlockInvariant($"void {componentName}_materializing(object sender)"))
										{
											if (_isHotReloadEnabled)
											{
												writer.AppendLineIndented($"var owner = global::Uno.UI.Helpers.MarkupHelper.GetElementProperty<{CurrentScope.ClassName}>(sender, \"{componentName}_owner\");");
											}

											// Refresh the bindings when the ElementStub is unloaded. This assumes that
											// ElementStub will be unloaded **after** the stubbed control has been created
											// in order for the component field to be filled, and Bindings.Update() to do its work.

											using (writer.BlockInvariant($"if ({componentName}_update_That.Target is {_xClassName} that)"))
											{
												if (CurrentXLoadScope != null)
												{
													foreach (var component in CurrentXLoadScope.Components)
													{
														writer.AppendLineIndented($"that.{component.VariableName}.ApplyXBind();");
														writer.AppendLineIndented($"that.{component.VariableName}.UpdateResourceBindings();");
													}

													BuildxBindEventHandlerInitializers(writer, CurrentXLoadScope.xBindEventsHandlers, "that.");
												}
											}
										}

										writer.AppendLineIndented($"{closureName}.Materializing += {componentName}_materializing;");
									}
									else
									{
										// TODO for https://github.com/unoplatform/uno/issues/6700
									}

								}
							}
							else
							{
								if (visibilityMember != null)
								{
									innerWriter.AppendLineInvariantIndented(
										"{0}.Visibility = {1};",
										closureName,
										BuildLiteralValue(visibilityMember)
									);
								}
							}

							XamlMemberDefinition GenerateBinding(string name, XamlMemberDefinition? memberDefinition, XamlObjectDefinition owner)
							{
								var def = new XamlMemberDefinition(
									new XamlMember(name,
										elementStubType,
										false
									), 0, 0,
									owner
								);

								if (memberDefinition != null)
								{
									def.Objects.AddRange(memberDefinition.Objects);
								}

								BuildComplexPropertyValue(innerWriter, def, closureName + ".", closureName);

								return def;
							}
						}

						xLoadScopeDisposable?.Dispose();
					}
					);
				}
			}

			return null;
		}


		/// <summary>
		/// Set the active DefaultBindMode, if x:DefaultBindMode is defined on this <paramref name="xamlObjectDefinition"/>.
		/// </summary>
		private IDisposable? TrySetDefaultBindMode(XamlObjectDefinition? xamlObjectDefinition, string? ambientDefaultBindMode = null)
		{
			var definedMode = xamlObjectDefinition
				?.Members
				.FirstOrDefault(m => m.Member.Name == "DefaultBindMode")?.Value?.ToString()
				?? ambientDefaultBindMode;

			if (definedMode == null)
			{
				return null;
			}
			else if (!IsValid(definedMode))
			{
				throw new InvalidOperationException("Invalid value specified for attribute 'DefaultBindMode'.  Accepted values are 'OneWay', 'OneTime', or 'TwoWay'.");
			}
			else
			{
				_currentDefaultBindMode.Push(definedMode);
				return new DisposableAction(() => _currentDefaultBindMode.Pop());
			}

			bool IsValid(string mode)
			{
				switch (mode)
				{
					case "OneWay":
					case "OneTime":
					case "TwoWay":
						return true;
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// Checks if the element is a native view and, if so, wraps it in a container for addition to the managed visual tree.
		/// </summary>
		private IDisposable? TryAdaptNative(IIndentedStringBuilder writer, XamlObjectDefinition xamlObjectDefinition, INamedTypeSymbol? targetType)
		{
			if (IsManagedViewBaseType(targetType) && !IsFrameworkElement(xamlObjectDefinition.Type) && IsNativeView(xamlObjectDefinition.Type))
			{
				writer.AppendLineIndented("global::Windows.UI.Xaml.Media.VisualTreeHelper.AdaptNative(");
				return new DisposableAction(() => writer.AppendIndented(")"));
			}

			return null;
		}

		private void BuildChildThroughSubclass(IIndentedStringBuilder writer, XamlMemberDefinition contentOwner, string returnType)
		{
			TryAnnotateWithGeneratorSource(writer);
			// To prevent conflicting names whenever we are working with dictionaries, subClass index is a Guid in those cases
			var subClassPrefix = _scopeStack.Aggregate("", (val, scope) => val + scope.Name);

			var namespacePrefix = _scopeStack.Count == 1 && _scopeStack.Last().Name.EndsWith("RD", StringComparison.Ordinal) ? "__Resources." : "";

			var subclassName = $"_{_fileUniqueId}_{subClassPrefix}SC{(_subclassIndex++).ToString(CultureInfo.InvariantCulture)}";

			RegisterChildSubclass(subclassName, contentOwner, returnType);

			var activator = _isHotReloadEnabled
				? $"(({namespacePrefix}I{subclassName})global::Uno.UI.Helpers.TypeMappings.CreateInstance<{namespacePrefix}{subclassName}>())"
				: $"new {namespacePrefix}{subclassName}()";

			writer.AppendLineIndented($"{activator}.Build(__owner)");
		}

		private string GenerateConstructorParameters(INamedTypeSymbol? type)
		{
			if (IsType(type, Generation.AndroidViewSymbol.Value))
			{
				// For android, all native control must take a context as their first parameters
				// To be able to use this control from the Xaml, we need to generate a constructor
				// call that takes the ContextHelper.Current as the first parameter.
				var hasContextConstructor = type.Constructors.Any(c => c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, Generation.AndroidContentContextSymbol.Value));

				if (hasContextConstructor)
				{
					return "(global::Uno.UI.ContextHelper.Current)";
				}
			}

			return "";
		}

		private bool HasCustomInitializer(INamedTypeSymbol? propertyType)
		{
			if (propertyType != null)
			{
				propertyType = FindUnderlyingType(propertyType);
				if (propertyType.TypeKind == TypeKind.Enum)
				{
					return true;
				}

				switch (propertyType.SpecialType)
				{
					case SpecialType.System_Int32:
					case SpecialType.System_Single:
					case SpecialType.System_Int64:
					case SpecialType.System_Int16:
					case SpecialType.System_Byte:
					case SpecialType.System_Double:
					case SpecialType.System_String:
					case SpecialType.System_Boolean:
						return true;
				}

				switch (propertyType.GetFullyQualifiedTypeExcludingGlobal())
				{
					case XamlConstants.Types.Thickness:
					case XamlConstants.Types.FontFamily:
					case XamlConstants.Types.FontWeight:
					case XamlConstants.Types.GridLength:
					case XamlConstants.Types.CornerRadius:
					case XamlConstants.Types.Brush:
					case XamlConstants.Types.Duration:
					case "UIKit.UIColor":
					case "Windows.UI.Color":
					case "Color":
					case "Windows.UI.Xaml.Media.ImageSource":
						return true;
				}
			}

			return false;
		}

		private void BuildInitializer(IIndentedStringBuilder writer, XamlObjectDefinition xamlObjectDefinition, XamlMemberDefinition? owner = null)
		{
			TryAnnotateWithGeneratorSource(writer);
			var initializer = xamlObjectDefinition.Members.First(m => m.Member.Name == "_Initialization");

			if (IsPrimitive(xamlObjectDefinition))
			{
				if (xamlObjectDefinition.Type.Name == "Double" || xamlObjectDefinition.Type.Name == "Single")
				{
					if (initializer.Value == null)
					{
						throw new InvalidOperationException($"Initializer value for {xamlObjectDefinition.Type.Name} cannot be empty");
					}

					writer.AppendLineInvariantIndented("{0}", GetFloatingPointLiteral(initializer.Value.ToString() ?? "", GetType(xamlObjectDefinition.Type), owner));
				}
				else if (xamlObjectDefinition.Type.Name == "Boolean")
				{
					if (initializer.Value == null)
					{
						throw new InvalidOperationException($"Initializer value for {xamlObjectDefinition.Type.Name} cannot be empty");
					}

					writer.AppendLineInvariantIndented("{1}", xamlObjectDefinition.Type.Name, initializer.Value.ToString()?.ToLowerInvariant());
				}
				else
				{
					writer.AppendLineInvariantIndented("{1}", xamlObjectDefinition.Type.Name, initializer.Value);
				}
			}
			else if (IsString(xamlObjectDefinition))
			{
				writer.AppendLineInvariantIndented("\"{1}\"", xamlObjectDefinition.Type.Name, DoubleEscape(initializer.Value?.ToString()));
			}
			else
			{
				writer.AppendLineInvariantIndented("new {0}({1})", GetGlobalizedTypeName(xamlObjectDefinition.Type.Name), initializer.Value);
			}
		}

		private string GetFloatingPointLiteral(string memberValue, INamedTypeSymbol type, XamlMemberDefinition? owner)
		{
			var name = ValidatePropertyType(type, owner);

			var isDouble = IsDouble(name);

			if (memberValue.Equals("Infinity", StringComparison.Ordinal))
			{
				// WinUI is case sensitive here.
				return isDouble ? "double.PositiveInfinity" : "float.PositiveInfinity";
			}
			else if (memberValue.Equals("-Infinity", StringComparison.Ordinal))
			{
				// WinUI is case sensitive here.
				return isDouble ? "double.NegativeInfinity" : "float.NegativeInfinity";
			}
			else if (memberValue.EndsWith("nan", StringComparison.OrdinalIgnoreCase) || memberValue.Equals("auto", StringComparison.OrdinalIgnoreCase))
			{
				return "{0}.NaN".InvariantCultureFormat(isDouble ? "double" : "float");
			}
			else if (memberValue.EndsWith("positiveinfinity", StringComparison.OrdinalIgnoreCase))
			{
				return "{0}.PositiveInfinity".InvariantCultureFormat(isDouble ? "double" : "float");
			}
			else if (memberValue.EndsWith("negativeinfinity", StringComparison.OrdinalIgnoreCase))
			{
				return "{0}.NegativeInfinity".InvariantCultureFormat(isDouble ? "double" : "float");
			}
			else if (memberValue.EndsWith("px", StringComparison.OrdinalIgnoreCase))
			{
#if DEBUG
				Console.WriteLine($"The value [{memberValue}] is specified in pixel and is not yet supported. ({owner?.LineNumber}, {owner?.LinePosition} in {_fileDefinition.FilePath})");
#endif
				return "{0}{1}".InvariantCultureFormat(memberValue.TrimEnd("px"), isDouble ? "d" : "f");
			}

			// UWP's Xaml parsing ignores curly brackets at beginning and end of a double literal
			memberValue = memberValue.Trim('{', '}');

			if (isDouble)
			{
				// UWP does that for double but not for float.
				memberValue = IgnoreStartingFromFirstSpaceIgnoreLeading(memberValue);
			}

			return "{0}{1}".InvariantCultureFormat(memberValue, isDouble ? "d" : "f");
		}

		private static string IgnoreStartingFromFirstSpaceIgnoreLeading(string value)
		{
			var span = value.AsSpan().TrimStart();

			var firstWhitespace = -1;
			for (int i = 0; i < span.Length; i++)
			{
				if (char.IsWhiteSpace(span[i]))
				{
					firstWhitespace = i;
					break;
				}
			}

			return firstWhitespace == -1
				? value
				: span.Slice(0, firstWhitespace).ToString();
		}

		private string ValidatePropertyType(INamedTypeSymbol propertyType, XamlMemberDefinition? owner)
		{
			var displayString = propertyType.GetFullyQualifiedTypeExcludingGlobal();
			if (IsDouble(displayString) &&
				owner != null && (
				owner.Member.Name == "Width" ||
				owner.Member.Name == "Height" ||
				owner.Member.Name == "MinWidth" ||
				owner.Member.Name == "MinHeight" ||
				owner.Member.Name == "MaxWidth" ||
				owner.Member.Name == "MaxHeight"
				))
			{
				return "float";
			}

			return displayString;
		}

		private NameScope CurrentScope
			=> _scopeStack.Peek();

		private IDisposable Scope(string? @namespace, string className)
		{
			_scopeStack.Push(new NameScope(@namespace, className));

			return new DisposableAction(() => _scopeStack.Pop());
		}

		private ComponentDefinition AddComponentForCurrentScope(XamlObjectDefinition objectDefinition)
		{
			// Prefix with the run ID so that we can leverage the removal of fields/properties during HR sessions
			var componentDefinition = new ComponentDefinition(
				objectDefinition,
				true,
				$"_component_{CurrentScope.ComponentCount}");
			CurrentScope.Components.Add(componentDefinition);

			if (CurrentXLoadScope is { } xLoadScope)
			{
				CurrentXLoadScope.Components.Add(new ComponentEntry(componentDefinition.MemberName, objectDefinition));
			}

			return componentDefinition;
		}

		private void AddComponentForParentScope(XamlObjectDefinition objectDefinition)
		{
			// Add the element stub as a strong reference, so that the
			// stub can be brought back if the loaded state changes.
			CurrentScope.Components.Add(new ComponentDefinition(objectDefinition, IsWeakReference: false, $"_component_{CurrentScope.ComponentCount}"));

			if (_xLoadScopeStack.Count > 1)
			{
				var parent = _xLoadScopeStack.Skip(1).First();

				parent.Components.Add(new ComponentEntry(CurrentScope.Components.Last().MemberName, objectDefinition));
			}
		}

		private void AddXBindEventHandlerToScope(string fieldName, string ownerTypeName, INamedTypeSymbol declaringType, ComponentDefinition? componentDefinition)
		{
			if (componentDefinition is null)
			{
				throw new InvalidOperationException("The component definition cannot be null.");
			}

			var thatParameter = _isHotReloadEnabled ? $"{ownerTypeName}, " : "";

			var builderDelegateType = $"global::System.Action<{thatParameter}{declaringType.GetFullyQualifiedTypeIncludingGlobal()}>";
			var definition = new EventHandlerBackingFieldDefinition(builderDelegateType, fieldName, Accessibility.Private, componentDefinition.MemberName);

			CurrentScope.xBindEventsHandlers.Add(definition);

			if (CurrentXLoadScope is { } xLoadScope)
			{
				CurrentXLoadScope.xBindEventsHandlers.Add(definition);
			}
		}

		private XLoadScope? CurrentXLoadScope
			=> _xLoadScopeStack.Count != 0 ? _xLoadScopeStack.Peek() : null;

		private IDisposable XLoadScope()
		{
			_xLoadScopeStack.Push(new XLoadScope());

			return new DisposableAction(() => _xLoadScopeStack.Pop());
		}

		private string? CurrentResourceOwner
			=> _resourceOwner != 0 ? $"__ResourceOwner_{_resourceOwner.ToString(CultureInfo.InvariantCulture)}" : null;

		private string CurrentResourceOwnerName
			=> CurrentResourceOwner ?? "this";

		public bool HasImplicitViewPinning
			=> Generation.IOSViewSymbol.Value is not null || Generation.AppKitViewSymbol.Value is not null;

		/// <summary>
		/// Pushes a ResourceOwner variable name onto the stack
		/// </summary>
		/// <remarks>
		/// The ResourceOwner scope is used to propagate the top-level owner of the resources
		/// in order for FrameworkTemplates contents to access the code-behind context, without
		/// causing circular references and case memory leaks.
		/// </remarks>
		private IDisposable ResourceOwnerScope()
		{
			_resourceOwner++;

			return new DisposableAction(() => _resourceOwner--);
		}

		/// <summary>
		/// If flag is set, decorate the generated code with a marker of the current method. Useful for pinpointing the source of a bug or other undesired behavior.
		/// </summary>
		private void TryAnnotateWithGeneratorSource(IIndentedStringBuilder writer, string? suffix = null, [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
		{
			if (_shouldAnnotateGeneratedXaml)
			{
				writer.Append(GetGeneratorSourceAnnotation(callerName, lineNumber, suffix));
			}
		}

		private void TryAnnotateWithGeneratorSource(ref string str, string? suffix = null, [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
		{
			if (_shouldAnnotateGeneratedXaml && str != null)
			{
				str = GetGeneratorSourceAnnotation(callerName, lineNumber, suffix) + str;
			}
		}

		private static string GetGeneratorSourceAnnotation(string? callerName, int lineNumber, string? suffix)
		{
			if (suffix != null)
			{
				suffix = "-" + suffix;
			}
			return "/*{0} L:{1}{2}*/".InvariantCultureFormat(callerName, lineNumber, suffix);
		}
	}
}
