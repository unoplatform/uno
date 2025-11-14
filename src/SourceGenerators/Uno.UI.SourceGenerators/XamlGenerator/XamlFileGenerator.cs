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

		private static readonly XamlType _elementStubXamlType = new XamlType(
			XamlConstants.BaseXamlNamespace,
			"ElementStub",
			new List<XamlType>(),
			new XamlSchemaContext()
		);

		private readonly Dictionary<string, XamlObjectDefinition> _namedResources = new Dictionary<string, XamlObjectDefinition>();
		private readonly List<string> _partials = new List<string>();

		/// <summary>
		/// Names of disambiguated keys associated with resource definitions. These are created for top-level ResourceDictionary declarations only.
		/// </summary>
		private readonly Dictionary<(string? Theme, string ResourceKey), string> _topLevelQualifiedKeys = new Dictionary<(string?, string), string>();
		private readonly Stack<NameScope> _scopeStack = new Stack<NameScope>();
		private readonly Stack<LogicalScope> _logicalScopeStack = new Stack<LogicalScope>();
		private readonly Stack<XLoadScope> _xLoadScopeStack = new Stack<XLoadScope>();
		private readonly List<XamlGenerationException> _errors = new List<XamlGenerationException>();
		private int _resourceOwner;
		private readonly XamlFileDefinition _fileDefinition;
		private readonly string _defaultNamespace;
		private readonly RoslynMetadataHelper _metadataHelper;
		private readonly string _fileUniqueId;
		private readonly string[] _analyzerSuppressions;
		private readonly ResourceDetailsCollection _resourceDetailsCollection;
		private int _collectionIndex;
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

		private string Field(string type, string name)
			=> $"private {type} {name} {(_isHotReloadEnabled ? "{ get; set; }" : ";")}";

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

		public (SourceText? code, IEnumerable<XamlGenerationException> errors) GenerateFile()
		{
			try
			{
				var writer = new IndentedStringBuilder();
				_errors.Clear();

				writer.AppendLineIndented("// <autogenerated />");

				AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);

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
				writer.AppendLineIndented("#if HAS_UNO_SKIA");
				writer.AppendLineIndented("using _View = Microsoft.UI.Xaml.UIElement;");
				writer.AppendLineIndented("#elif __ANDROID__");
				writer.AppendLineIndented("using _View = Android.Views.View;");
				writer.AppendLineIndented("#elif __APPLE_UIKIT__ || __IOS__ || __TVOS__");
				writer.AppendLineIndented("using _View = UIKit.UIView;");
				writer.AppendLineIndented("#else");
				writer.AppendLineIndented("using _View = Microsoft.UI.Xaml.UIElement;");
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
						TryGenerateWarningForInconsistentBaseType(writer, topLevelControl);

						var controlBaseType = GetType(topLevelControl.Type);

						using (writer.BlockInvariant("partial class {0} : {1}", _xClassName.ClassName, controlBaseType.GetFullyQualifiedTypeIncludingGlobal()))
						{
							BuildBaseUri(writer);

							using (Scope(_xClassName.Namespace, _xClassName.ClassName))
							{
								using var scopeAutoDisposable = LogicalScope(topLevelControl);

								BuildInitializeComponent(writer, topLevelControl, controlBaseType);

								SafeBuild(TryBuildElementStubHolders, writer);

								SafeBuild(BuildPartials, writer);

								SafeBuild(BuildMethods, writer);

								SafeBuild(BuildBackingFields, writer);

								SafeBuild(w => BuildChildSubclasses(w), writer, "BuildChildSubclasses");

								SafeBuild(BuildComponentFields, writer);

								SafeBuild(BuildCompiledBindings, writer);

								SafeBuild(BuildXBindTryGetDeclarations, writer);
							}
						}

						if (_isHotReloadEnabled && Generation.IOSViewSymbol.Value is not null)
						{
							// Workaround for HR behaving incorrectly on iOS
							// https://github.com/xamarin/xamarin-macios/issues/22102

							using (writer.BlockInvariant($"namespace __internal"))
							{
								writer.AppendLineIndented("/// <remarks>Internal Use for iOS only.</remarks>");
								writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
								writer.AppendLineIndented("[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]");
								using (writer.BlockInvariant($"static partial class __{_xClassName.ClassName}_Dummy"))
								{
									using (writer.BlockInvariant("private static class Dummy_Bindings"))
									{
										writer.AppendLineIndented("private static object Owner { get; set; }");
									}
								}
							}
						}
					}
				}

				SafeBuild(BuildXamlApplyBlocks, writer);

				// var formattedCode = ReformatCode(writer.ToString());
				return (new StringBuilderBasedSourceText(writer.Builder), _errors.ToList());
			}
			catch (Exception globalError) when (globalError is not OperationCanceledException)
			{
				return (null, [new XamlGenerationException($"Processing failed for file {_fileDefinition.FilePath}", globalError, _fileDefinition)]);
			}
		}

		private void SafeBuild<TArg>(Action<IIndentedStringBuilder, TArg> buildAction, IIndentedStringBuilder writer, TArg arg, [CallerArgumentExpression(nameof(buildAction))] string name = "")
			=> SafeBuild(w => buildAction(w, arg), writer, name);

		private void SafeBuild<TArg1, TArg2>(Action<IIndentedStringBuilder, TArg1, TArg2> buildAction, IIndentedStringBuilder writer, TArg1 arg1, TArg2 arg2, [CallerArgumentExpression(nameof(buildAction))] string name = "")
			=> SafeBuild(w => buildAction(w, arg1, arg2), writer, name);

		private void SafeBuild(Action<IIndentedStringBuilder> buildAction, IIndentedStringBuilder writer, [CallerArgumentExpression(nameof(buildAction))] string name = "")
		{
			try
			{
				buildAction(writer);
			}
			catch (XamlGenerationException xamlGenError)
			{
				_errors.Add(xamlGenError);
			}
			catch (Exception error) when (error is not OperationCanceledException)
			{
				_errors.Add(new XamlGenerationException($"Processing failed for an unknown reason ({name})", error, _fileDefinition));
			}
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
			writer.AppendLineIndented("private global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();");

			AnalyzerSuppressionsGenerator.GenerateTrimExclusions(writer);
			using (writer.BlockInvariant($"private void InitializeComponent()"))
			{
				if (IsApplication(controlBaseType))
				{
					SafeBuild(BuildApplicationInitializerBody, writer, topLevelControl);
				}
				else
				{
					SafeBuild(BuildGenericControlInitializerBody, writer, topLevelControl);
					SafeBuild(BuildNamedResources, writer, _namedResources);
				}

				EnsureXClassName();
				SafeBuild(BuildCompiledBindingsInitializer, writer, controlBaseType);
			}
		}

		private void BuildXamlApplyBlocks(IIndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);

			if (!_isHotReloadEnabled)
			{
				using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
				{
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
		private void BuildApplicationInitializerBody(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
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

			if (_isWasm
				// Only applicable when building for Wasm DOM support
				&& _metadataHelper.FindTypeByFullName("Uno.UI.Runtime.WebAssembly.HtmlElementAttribute") is not null)
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

			AttachUnhandledExceptionHandler();

			void ApplyLiteralProperties()
			{
				writer.AppendLineIndented("this");

				using (var blockWriter = CreateApplyBlock(writer, topLevelControl))
				{
					blockWriter.AppendLineInvariantIndented(
						"// Source {0} (Line {1}:{2})",
						_relativePath,
						topLevelControl.LineNumber,
						topLevelControl.LinePosition
					);
					BuildLiteralProperties(blockWriter, topLevelControl, blockWriter.AppliedParameterName);
				}

				writer.AppendLineIndented(";");
			}

			void AttachUnhandledExceptionHandler()
			{
				writer.Append($"#if DEBUG && !DISABLE_GENERATED_UNHANDLED_EXCEPTION_HANDLER");
				writer.AppendLine();
				writer.AppendLineIndented($"UnhandledException += (s, e) =>");
				writer.AppendLineIndented("{");
				writer.Indent();
				using (writer.BlockInvariant("if (global::System.Diagnostics.Debugger.IsAttached)"))
				{
					writer.AppendLineIndented("global::System.Diagnostics.Debug.WriteLine(e.Exception);");
					writer.Append("#if !DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION");
					writer.AppendLine();
					writer.AppendLineIndented("global::System.Diagnostics.Debugger.Break();");
					writer.Append("#endif");
					writer.AppendLine();
				}
				writer.Indent(-1);
				writer.AppendLineIndented("};");
				writer.Append($"#endif");
				writer.AppendLine();
			}
		}

		/// <summary>
		/// Override font properties at the end of the app.xaml ctor.
		/// </summary>
		/// <param name="writer"></param>
		private void ApplyFontsOverride(IIndentedStringBuilder writer)
		{
			if (_generatorContext.GetMSBuildPropertyValue("UnoPlatformDefaultSymbolsFontFamily") is { Length: > 0 } fontOverride)
			{
				writer.AppendLineInvariantIndented($"global::Uno.UI.FeatureConfiguration.Font.SymbolsFont = \"{fontOverride}\";");
			}
		}

		private void GenerateApiExtensionRegistrations(IIndentedStringBuilder writer)
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
				if (registration.Length == 2)
				{
					writer.AppendLineIndented($"global::Uno.Foundation.Extensibility.ApiExtensibility.Register(typeof(global::{registration.ElementAt(0).Value}), o => new global::{registration.ElementAt(1).Value}(o));");
				}
				else if (registration.Length == 3)
				{
					writer.AppendLineIndented($"global::Uno.Foundation.Extensibility.ApiExtensibility.Register<global::{registration.ElementAt(2).Value}>(typeof(global::{registration.ElementAt(0).Value}), o => new global::{registration.ElementAt(1).Value}(o));");
				}
				else if (registration.Length == 4)
				{
					writer.AppendLineIndented($"if (OperatingSystem.IsOSPlatform(\"{registration.ElementAt(3).Value}\"))");
					writer.AppendLineIndented("{");
					using (writer.Indent())
					{
						if (registration.ElementAt(3).Value is not null)
						{
							writer.AppendLineIndented($"global::Uno.Foundation.Extensibility.ApiExtensibility.Register<global::{registration.ElementAt(2).Value}>(typeof(global::{registration.ElementAt(0).Value}), o => new global::{registration.ElementAt(1).Value}(o));");
						}
						else
						{
							writer.AppendLineIndented($"global::Uno.Foundation.Extensibility.ApiExtensibility.Register(typeof(global::{registration.ElementAt(0).Value}), o => new global::{registration.ElementAt(1).Value}(o));");
						}
					}
					writer.AppendLineIndented("}");
				}
				else
				{
					throw new InvalidOperationException($"ApiExtensionAttribute should takes 2 to 4 arguments.");
				}
			}
		}

		private void InitializeRemoteControlClient(IIndentedStringBuilder writer)
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

		private void GenerateResourceLoader(IIndentedStringBuilder writer)
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

		private void BuildResourceLoaderFromAssembly(IIndentedStringBuilder writer, IAssemblySymbol assembly)
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
		private void BuildGenericControlInitializerBody(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
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

				if (BuildInlineLocalizedProperties(writer, topLevelControl, topLevelControlType,
						isInInitializer: false))
				{
					writer.AppendLineIndented("");
				}

				writer.AppendLineIndented("this");

				using (var blockWriter = CreateApplyBlock(writer, topLevelControl))
				{
					blockWriter.AppendLineInvariantIndented(
						"// Source {0} (Line {1}:{2})",
						_relativePath,
						topLevelControl.LineNumber,
						topLevelControl.LinePosition
					);

					BuildLiteralProperties(blockWriter, topLevelControl, blockWriter.AppliedParameterName);
				}

				BuildExtendedProperties(writer, topLevelControl, useGenericApply: true);
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

		private void BuildMethods(IIndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);
			foreach (var callback in CurrentScope.Methods)
			{
				callback(writer);
				writer.AppendLine();
			}
		}

		private void BuildBackingFields(IIndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);
			foreach (var backingFieldDefinition in CurrentScope.BackingFields.Distinct().OrderBy(f => f.Name, StringComparer.Ordinal))
			{
				var fieldName = SanitizeResourceName(backingFieldDefinition.Name);
				if (fieldName != null)
				{
					CurrentScope.ReferencedElementNames.Remove(fieldName);
				}

				if (_isHotReloadEnabled)
				{
					// We use a property for HR so we can remove them without causing rude edit.
					// We also avoid property initializer as they are known to cause issue with HR: https://github.com/dotnet/roslyn/issues/79320
					writer.AppendMultiLineIndented($$"""
						private global::Microsoft.UI.Xaml.Data.ElementNameSubject _{{fieldName}}SubjectBackingPseudoField { get; set; }
						private global::Microsoft.UI.Xaml.Data.ElementNameSubject _{{fieldName}}Subject
						{
							get => _{{fieldName}}SubjectBackingPseudoField ??= new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
						}
						{{FormatAccessibility(backingFieldDefinition.Accessibility)}} {{backingFieldDefinition.GlobalizedTypeName}} {{fieldName}}
						{
							get => ({{backingFieldDefinition.GlobalizedTypeName}})_{{fieldName}}Subject.ElementInstance;
							set => _{{fieldName}}Subject.ElementInstance = value;
						}
						""");
				}
				else
				{
					writer.AppendMultiLineIndented($$"""
						private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _{{fieldName}}Subject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
						{{FormatAccessibility(backingFieldDefinition.Accessibility)}} {{backingFieldDefinition.GlobalizedTypeName}} {{fieldName}}
						{
							get => ({{backingFieldDefinition.GlobalizedTypeName}})_{{fieldName}}Subject.ElementInstance;
							set => _{{fieldName}}Subject.ElementInstance = value;
						}
						""");
				}
			}

			foreach (var remainingReference in CurrentScope.ReferencedElementNames.OrderBy(f => f, StringComparer.Ordinal))
			{
				// Create load-time subjects for ElementName references not in local scope
				if (_isHotReloadEnabled)
				{
					// We use a property for HR so we can remove them without causing rude edit.
					// We also avoid property initializer as they are known to cause issue with HR: https://github.com/dotnet/roslyn/issues/79320
					writer.AppendMultiLineIndented($$"""
						private global::Microsoft.UI.Xaml.Data.ElementNameSubject _{{remainingReference}}SubjectBackingPseudoField { get; set; }
						private global::Microsoft.UI.Xaml.Data.ElementNameSubject _{{remainingReference}}Subject
						{
							get => _{{remainingReference}}SubjectBackingPseudoField ??= new global::Microsoft.UI.Xaml.Data.ElementNameSubject(isRuntimeBound: true, name: "{{remainingReference}}");
						}
						""");
				}
				else
				{
					writer.AppendLineIndented($"private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _{remainingReference}Subject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject(isRuntimeBound: true, name: \"{remainingReference}\");");
				}
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

		private void BuildChildSubclasses(IIndentedStringBuilder writer, bool isTopLevel = false, bool isSubSub = false)
		{
			_isInChildSubclass = true;
			TryAnnotateWithGeneratorSource(writer);
			var ns = $"{_defaultNamespace}.__Resources";
			using var _ = isTopLevel ? writer.BlockInvariant($"namespace {ns}") : null;

			// If _isHotReloadEnabled we generate it anyway so we can remove sub-classes without causing rude edit.
			if (!_isHotReloadEnabled && CurrentScope.Subclasses is not { Count: > 0 } && CurrentScope.SubclassBuilders is not { Count: > 0 })
			{
				return;
			}

			if (!isSubSub)
			{
				writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
				writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]");
			}
			using (isSubSub ? null : writer.BlockInvariant($"{(isTopLevel ? "internal" : "private")} class {CurrentScope.SubClassesRoot}"))
			{
				foreach (var kvp in CurrentScope.Subclasses)
				{
					var className = kvp.Key;
					var contentOwner = kvp.Value.ContentOwner;

					using (TrySetDefaultBindMode(contentOwner.Owner, kvp.Value.DefaultBindMode))
					{
						using (Scope(ns, className))
						{
							AnalyzerSuppressionsGenerator.GenerateTrimExclusions(writer);
							writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
							writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]");
							using (writer.BlockInvariant($"public class {className}"))
							{
								BuildBaseUri(writer);

								using (ResourceOwnerScope())
								{
									writer.AppendLineIndented("global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();");
									writer.AppendLineIndented($"global::System.Object {CurrentResourceOwner};");
									writer.AppendLineIndented($"{kvp.Value.ReturnType} __rootInstance = null;");

									using (writer.BlockInvariant($"public {kvp.Value.ReturnType} Build(object {CurrentResourceOwner}, global::Microsoft.UI.Xaml.TemplateMaterializationSettings __settings)"))
									{
										writer.AppendLineIndented($"var __that = this;");
										writer.AppendLineIndented($"this.{CurrentResourceOwner} = {CurrentResourceOwner};");
										writer.AppendLineIndented("this.__rootInstance = ");

										// Is never considered in Global Resources because class encapsulation
										BuildChild(writer, contentOwner, contentOwner.Objects.First());

										writer.AppendLineIndented(";");

										BuildCompiledBindingsInitializerForTemplate(writer);

										using (writer.BlockInvariant("if (__rootInstance is DependencyObject d)"))
										{
											using (writer.BlockInvariant("if (global::Microsoft.UI.Xaml.NameScope.GetNameScope(d) == null)"))
											{
												writer.AppendLineIndented("global::Microsoft.UI.Xaml.NameScope.SetNameScope(d, __nameScope);");
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

								BuildMethods(writer);

								BuildXBindTryGetDeclarations(writer);
							}

							// We generate the sub-sub-class registered by the sub-class of the current scope, but we generate them directly on the root-sub-class!
							// This is required for HR as roslyn tends to detect class move between sub-classes (even if unrelated at all),
							// having all of them at the root (with class name matching logical tree path) prevents such problem.
							BuildChildSubclasses(writer, isSubSub: true);
						}
					}
				}

				foreach (var subClass in CurrentScope.SubclassBuilders)
				{
					subClass(writer);
					writer.AppendLine();
				}
			}
		}

		/// <summary>
		/// The "OriginalSourceLocation" can be used like DebugParseContext (set via SetBaseUri) but for elements that aren't FrameworkElements
		/// </summary>
		private void TrySetOriginalSourceLocation(IIndentedStringBuilder writer, string element, IXamlLocation location)
		{
			if (_isHotReloadEnabled)
			{
				writer.AppendLineIndented($"global::Uno.UI.Helpers.MarkupHelper.SetElementProperty({element}, \"OriginalSourceLocation\", \"file:///{_fileDefinition.FilePath.Replace("\\", "/")}#L{location.LineNumber}:{location.LinePosition}\");");
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

		private void BuildCompiledBindingsInitializer(IIndentedStringBuilder writer, INamedTypeSymbol controlBaseType)
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

			if (hasXBindExpressions || hasResourceExtensions || _isHotReloadEnabled)
			{
				var activator = $"new {GetBindingsTypeNames(_xClassName.ClassName).bindingsClassName}(this)";

				writer.AppendLineIndented($"Bindings = {activator};");
			}

			if ((isFrameworkElement || isWindow) && (hasXBindExpressions || hasResourceExtensions || _isHotReloadEnabled))
			{
				// Casting to FrameworkElement or Window is important to avoid issues when there
				// is a member named Loading or Activated that shadows the ones from FrameworkElement/Window
				string setupCallbackSignature;
				string? teardownCallbackSignature = null;
				if (isFrameworkElement)
				{
					setupCallbackSignature = "private void __UpdateBindingsAndResources(global::Microsoft.UI.Xaml.FrameworkElement s, object e)";
					writer.AppendLineIndented("((global::Microsoft.UI.Xaml.FrameworkElement)this).Loading += __UpdateBindingsAndResources;");

					if (hasXBindExpressions || _isHotReloadEnabled)
					{
						teardownCallbackSignature = "private void __StopTracking(object s, global::Microsoft.UI.Xaml.RoutedEventArgs e)";
						writer.AppendLineIndented("((global::Microsoft.UI.Xaml.FrameworkElement)this).Unloaded += __StopTracking;");
					}
				}
				else
				{
					setupCallbackSignature = "private void __UpdateBindingsAndResources(object s, global::Microsoft.UI.Xaml.WindowActivatedEventArgs e)";
					writer.AppendLineIndented("((global::Microsoft.UI.Xaml.Window)this).Activated += __UpdateBindingsAndResources;");
				}

				CurrentScope.RegisterMethod("__UpdateBindingsAndResources", (_, eventWriter) =>
				{
					using (eventWriter.BlockInvariant(setupCallbackSignature))
					{
						if (hasXBindExpressions)
						{
							eventWriter.AppendLineIndented("this.Bindings.Update();");
						}

						eventWriter.AppendLineIndented("this.Bindings.UpdateResources();");
					}
				});
				if (!string.IsNullOrEmpty(teardownCallbackSignature))
				{
					CurrentScope.RegisterMethod("__StopTracking", (_, eventWriter) =>
					{
						using (eventWriter.BlockInvariant(teardownCallbackSignature))
						{
							eventWriter.AppendLineIndented("this.Bindings.StopTracking();");
						}
					});
				}
			}
		}

		private void BuildCompiledBindingsInitializerForTemplate(IIndentedStringBuilder writer)
		{
			TryAnnotateWithGeneratorSource(writer);
			var hasResourceExtensions = CurrentScope.Components.Any(c => HasMarkupExtensionNeedingComponent(c.XamlObject));

			if (hasResourceExtensions)
			{
				using (writer.BlockInvariant($"if (__rootInstance is FrameworkElement __fe)"))
				{
					writer.AppendLineIndented("__fe.Loading += __UpdateBindingsAndResources;");
					writer.AppendLineIndented("__fe.Unloaded += __StopTracking;");

					CurrentScope.RegisterMethod("__UpdateBindingsAndResources", (_, eventWriter) =>
					{
						using (writer.BlockInvariant("private void __UpdateBindingsAndResources(global::Microsoft.UI.Xaml.FrameworkElement s, object e)"))
						{
							BuildComponentResourceBindingUpdates(eventWriter);
							BuildXBindApply(eventWriter);
							BuildxBindEventHandlerInitializers(eventWriter, CurrentScope.xBindEventsHandlers);
						}
					});
					CurrentScope.RegisterMethod("__StopTracking", (_, eventWriter) =>
					{
						using (writer.BlockInvariant("private void __StopTracking(object s, global::Microsoft.UI.Xaml.RoutedEventArgs e)"))
						{
							BuildXBindSuspend(eventWriter);
						}
					});
				}
			}
		}

		private void BuildxBindEventHandlerInitializers(IIndentedStringBuilder writer, List<XBindEventInitializerDefinition> xBindEventsHandlers, string prefix = "")
		{
			foreach (var xBindEventHandler in xBindEventsHandlers)
			{
				writer.AppendLineIndented($"{prefix}{xBindEventHandler.MethodName}(true);");
			}
		}

		private void BuildxBindEventHandlerUnInitializers(IIndentedStringBuilder writer, List<XBindEventInitializerDefinition> xBindEventsHandlers, string prefix = "")
		{
			foreach (var xBindEventHandler in xBindEventsHandlers)
			{
				writer.AppendLineIndented($"{prefix}{xBindEventHandler.MethodName}(false);");
			}
		}

		private void BuildComponentResourceBindingUpdates(IIndentedStringBuilder writer, string prefix = "")
		{
			for (var i = 0; i < CurrentScope.Components.Count; i++)
			{
				var component = CurrentScope.Components[i];

				if (HasMarkupExtensionNeedingComponent(component.XamlObject) && IsDependencyObject(component.XamlObject))
				{
					var contextProvider = component.ResourceContext is { } context
						? $"resourceContextProvider: {prefix}{context.MemberName}"
						: "";

					writer.AppendLineIndented($"{prefix}{component.MemberName}.UpdateResourceBindings({contextProvider});");
				}
			}
		}

		private void BuildXBindApply(IIndentedStringBuilder writer, string prefix = "")
		{
			for (var i = 0; i < CurrentScope.Components.Count; i++)
			{
				var component = CurrentScope.Components[i];

				if (HasXBindMarkupExtension(component.XamlObject))
				{
					var isDO = IsDependencyObject(component.XamlObject);
					var wrap = !isDO ? ".GetDependencyObjectForXBind()" : "";

					writer.AppendLineIndented($"{prefix}{component.MemberName}{wrap}.ApplyXBind();");
				}
			}
		}

		private void BuildXBindSuspend(IIndentedStringBuilder writer, string prefix = "")
		{
			for (var i = 0; i < CurrentScope.Components.Count; i++)
			{
				var component = CurrentScope.Components[i];

				if (HasXBindMarkupExtension(component.XamlObject))
				{
					var isDO = IsDependencyObject(component.XamlObject);
					var wrap = !isDO ? ".GetDependencyObjectForXBind()" : "";

					writer.AppendLineIndented($"{prefix}{component.MemberName}{wrap}.SuspendXBind();");
				}
			}
		}

		private void BuildComponentFields(IIndentedStringBuilder writer)
		{
			foreach (var current in CurrentScope.Components.OrderBy(c => c.MemberName, StringComparer.Ordinal))
			{
				var componentName = current.MemberName;
				var typeName = GetType(current.XamlObject.Type).GetFullyQualifiedTypeIncludingGlobal();
				var isWeak = current.IsWeakReference ? "true" : "false";

				if (_isHotReloadEnabled)
				{
					// We use a property for HR so we can remove them without causing rude edit.
					// We also avoid property initializer as they are known to cause issue with HR: https://github.com/dotnet/roslyn/issues/79320
					writer.AppendMultiLineIndented($$"""
						private global::Microsoft.UI.Xaml.Markup.ComponentHolder {{componentName}}_HolderBackingPseudoField { get; set; }
						private global::Microsoft.UI.Xaml.Markup.ComponentHolder {{componentName}}_Holder
						{
							get => {{componentName}}_HolderBackingPseudoField ??= new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: {{isWeak}});
						}
						private {{typeName}} {{componentName}}
						{
							get => ({{typeName}}){{componentName}}_Holder.Instance;
							set => {{componentName}}_Holder.Instance = value;
						}
						
					""");
				}
				else
				{
					writer.AppendMultiLineIndented($$"""
						private global::Microsoft.UI.Xaml.Markup.ComponentHolder {{componentName}}_Holder = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: {{isWeak}});
						private {{typeName}} {{componentName}}
						{
							get => ({{typeName}}){{componentName}}_Holder.Instance;
							set => {{componentName}}_Holder.Instance = value;
						}
						
						""");
				}
			}
		}

		private void BuildCompiledBindings(IIndentedStringBuilder writer)
		{
			EnsureXClassName();

			var hasXBindExpressions = CurrentScope.XBindExpressions.Count != 0;
			var hasResourceExtensions = CurrentScope.Components.Any(c => HasMarkupExtensionNeedingComponent(c.XamlObject));

			if (hasXBindExpressions || hasResourceExtensions || _isHotReloadEnabled) // We forcefully generate the bindings class for HR to avoids rude edit
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

				AnalyzerSuppressionsGenerator.GenerateTrimExclusions(writer);
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

						BuildXBindApply(writer, "owner.");
						BuildxBindEventHandlerInitializers(writer, CurrentScope.xBindEventsHandlers, "owner.");
					}
					using (writer.BlockInvariant($"void {bindingsInterfaceName}.UpdateResources()"))
					{
						writer.AppendLineIndented($"var owner = Owner;");

						BuildComponentResourceBindingUpdates(writer, "owner.");
					}
					using (writer.BlockInvariant($"void {bindingsInterfaceName}.StopTracking()"))
					{
						writer.AppendLineIndented($"var owner = Owner;");

						BuildXBindSuspend(writer, "owner.");
					}
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
					AnalyzerSuppressionsGenerator.GenerateTrimExclusions(writer);

					using (writer.BlockInvariant("public sealed partial class GlobalStaticResources"))
					{
						BuildBaseUri(writer);

						IDisposable WrapSingleton()
						{
							writer.AppendLineIndented("// This non-static inner class is a means of reducing size of AOT compilations by avoiding many accesses to static members from a static callsite, which adds costly class initializer checks each time.");

							if (_isHotReloadEnabled)
							{
								// Create a public member to avoid having to remove all unused member warnings
								// The member is a method to avoid this error: error ENC0011: Updating the initializer of const field requires restarting the application.
								writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
								writer.AppendLineIndented($"internal string __{_fileDefinition.UniqueID}_checksum() => \"{_fileDefinition.Checksum}\";");

								writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]");
							}

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
							writer.AppendLineInvariantIndented("private static global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();");
							writer.AppendLineInvariantIndented("private static {0} __that;", DictionaryProviderInterfaceName);
							using (writer.BlockInvariant("internal static {0} Instance", DictionaryProviderInterfaceName))
							{
								using (writer.BlockInvariant("get"))
								{
									using (writer.BlockInvariant("if (__that == null)"))
									{
										var activator = $"new {SingletonClassName}()";

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

							// Constructor
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
							var initializers = BuildTopLevelResourceDictionaryInitializers(writer, topLevelControl);

							var themeDictionaryMember = topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries");
							foreach (var dict in (themeDictionaryMember?.Objects).Safe())
							{
								initializers.AddRange(BuildTopLevelResourceDictionaryInitializers(writer, dict));
							}

							TryAnnotateWithGeneratorSource(writer, suffix: "DictionaryProperty");
							writer.AppendLineInvariantIndented("private global::Microsoft.UI.Xaml.ResourceDictionary _{0}_ResourceDictionary;", _fileUniqueId);
							writer.AppendLine();
							using (writer.BlockInvariant("internal global::Microsoft.UI.Xaml.ResourceDictionary {0}_ResourceDictionary", _fileUniqueId))
							{
								using (writer.BlockInvariant("get"))
								{
									using (writer.BlockInvariant("if (_{0}_ResourceDictionary == null)", _fileUniqueId))
									{
										writer.AppendLineInvariantIndented("_{0}_ResourceDictionary = ", _fileUniqueId);
										InitializeAndBuildResourceDictionary(writer, topLevelControl, setIsParsing: true, initializers);
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
							writer.AppendLineInvariantIndented("global::Microsoft.UI.Xaml.ResourceDictionary {0}.GetResourceDictionary() => {1}_ResourceDictionary;", DictionaryProviderInterfaceName, _fileUniqueId);

							BuildMethods(writer);
						}
						writer.AppendLine();
						writer.AppendLineInvariantIndented("internal static global::Microsoft.UI.Xaml.ResourceDictionary {0}_ResourceDictionary => {1}.Instance.GetResourceDictionary();", _fileUniqueId, SingletonClassName);
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

		/// <summary>
		///Build initializers for the current ResourceDictionary.
		/// </summary>
		/// <param name="writer">The writer to use</param>
		/// <param name="dictionaryObject">The <see cref="XamlObjectDefinition"/> associated with the dictionary.</param>
		private IDictionary<XamlObjectDefinition, string> BuildTopLevelResourceDictionaryInitializers(IIndentedStringBuilder writer, XamlObjectDefinition dictionaryObject)
		{
			TryAnnotateWithGeneratorSource(writer);
			var resourcesRoot = FindImplicitContentMember(dictionaryObject);
			var theme = GetDictionaryResourceKey(dictionaryObject);
			var resources = (resourcesRoot?.Objects).Safe();
			var initializers = new Dictionary<XamlObjectDefinition, string>();

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
					throw new XamlGenerationException($"Dictionary item '{resource.Type?.Name}' has duplicate key '{key}' {(theme != null ? $"in theme '{theme}'" : "")}", resource);
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
						throw new XamlGenerationException($"Method name was not created correctly for '{key}' (theme='{theme}')", resource);
					}
					writer.AppendLineInvariantIndented("// Method for resource {0} {1}", key, theme != null ? "in theme {0}".InvariantCultureFormat(theme) : "");
					var singleTimeInitializer = BuildSingleTimeInitializer(writer, initializerName, () => BuildChild(writer, resourcesRoot, resource));
					initializers[resource] = singleTimeInitializer;
				}
			}
			_themeDictionaryCurrentlyBuilding = former;

			return initializers;
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

			writer.AppendLineIndented("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2075\")]");
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
							writer.AppendLineInvariantIndented("global::Microsoft.UI.Xaml.Style.RegisterDefaultStyleForType({0}, {1}, /*isNativeStyle:*/{2});",
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
		private void InitializeAndBuildResourceDictionary(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl, bool setIsParsing, IDictionary<XamlObjectDefinition, string>? initializers = default)
		{
			TryAnnotateWithGeneratorSource(writer);

			if (IsResourceDictionarySubclass(topLevelControl.Type))
			{
				BuildTypedResourceDictionary(writer, topLevelControl);
			}
			else
			{
				using (writer.BlockInvariant("new global::Microsoft.UI.Xaml.ResourceDictionary"))
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
					BuildResourceDictionary(writer, FindImplicitContentMember(topLevelControl), isInInitializer: true, initializers: initializers);
				}
			}
		}

		private void BuildTypedResourceDictionary(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			var type = GetType(topLevelControl.Type);

			if (type.GetFullyQualifiedTypeExcludingGlobal().Equals("Microsoft.UI.Xaml.Controls.XamlControlsResources", StringComparison.Ordinal))
			{
				using (writer.BlockInvariant($"new global::Microsoft.UI.Xaml.Controls.XamlControlsResourcesV2()"))
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
					if (_isHotReloadEnabled)
					{
						writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]");
					}

					using (writer.BlockInvariant("public sealed partial class {0} : {1}", className.ClassName, controlBaseType.GetFullyQualifiedTypeIncludingGlobal()))
					{
						BuildBaseUri(writer);

						using (Scope(className.Namespace, className.ClassName!))
						{
							using (writer.BlockInvariant("public void InitializeComponent()"))
							{
								if (_isHotReloadEnabled)
								{
									TrySetOriginalSourceLocation(writer, "this", topLevelControl);
								}

								BuildMergedDictionaries(writer, topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "MergedDictionaries"), isInInitializer: false, dictIdentifier: "this");
								BuildThemeDictionaries(writer, topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "ThemeDictionaries"), isInInitializer: false, dictIdentifier: "this");
								BuildResourceDictionary(writer, FindImplicitContentMember(topLevelControl), isInInitializer: false, dictIdentifier: "this");
							}

							writer.AppendLine();

							BuildMethods(writer);
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
		private string BuildSingleTimeInitializer(IIndentedStringBuilder writer, string initializerName, Action propertyBodyBuilder)
		{
			TryAnnotateWithGeneratorSource(writer);
			using (ResourceOwnerScope())
			{
				AnalyzerSuppressionsGenerator.GenerateTrimExclusions(writer);
				writer.AppendLineIndented($"private object {initializerName}(object {CurrentResourceOwner}) =>");
				using (writer.Indent())
				{
					propertyBodyBuilder();
					writer.AppendLineIndented(";");
				}
				writer.AppendLine();
			}

			return initializerName;
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
				writer.AppendLineIndented("Loading += __UpdateNamedResources;");

				CurrentScope.RegisterMethod("__UpdateNamedResources", (_, eventWriter) =>
				{
					using (eventWriter.BlockInvariant("private void __UpdateNamedResources(global::Microsoft.UI.Xaml.FrameworkElement s, object e)"))
					{
						foreach (var namedResource in resourcesTogenerateUpdateBindings)
						{
							var type = GetType(namedResource.Value.Type);

							eventWriter.AppendLineIndented($"{namedResource.Key}.UpdateResourceBindings();");
						}
					}
				});
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
			if (ns == xamlType.PreferredXamlNamespace)
			{
				// It looks like FindType always returns null in this code path.
				// So, we avoid the costly call here.
				return null;
			}

			// If GetTrimmedNamespace returned a different string, it's a "using:"-prefixed namespace.
			// In this case, we'll have `baseTypeString` as
			// the fully qualified type name.
			// In this case, we go through this code path as it's much more efficient than FindType.
			var baseTypeString = $"{ns}.{xamlType.Name}";

			// Try finding the type with "Extension" suffix first, then without
			return FindMarkupExtensionType(baseTypeString + "Extension") ?? FindMarkupExtensionType(baseTypeString);
		}

		private INamedTypeSymbol? FindMarkupExtensionType(string fullTypeName)
		{
			return _metadataHelper.FindTypeByFullName(fullTypeName) is INamedTypeSymbol type && type.Is(Generation.MarkupExtensionSymbol.Value) ? type : null;
		}

		private bool IsCustomMarkupExtensionType(XamlType? xamlType) =>
			// Determine if the type is a custom markup extension
			GetMarkupExtensionType(xamlType) != null;

		private bool IsXamlTypeConverter(INamedTypeSymbol? symbol)
		{
			return symbol?.GetAttributes().Any(a => a.AttributeClass?.Equals(Generation.CreateFromStringAttributeSymbol.Value, SymbolEqualityComparer.Default) == true) == true;
		}

		private string BuildXamlTypeConverterLiteralValue(INamedTypeSymbol? symbol, string memberValue, bool includeQuotations, IXamlLocation location)
		{
			var attributeData = symbol.FindAttribute(Generation.CreateFromStringAttributeSymbol.Value);
			var targetMethod = attributeData?.NamedArguments.FirstOrDefault(kvp => kvp.Key == "MethodName").Value.Value?.ToString();

			if (targetMethod == null)
			{
				throw new XamlGenerationException($"Unable to find 'MethodName' property on '{symbol}'", location);
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
				throw new XamlGenerationException($"'{memberName}' is not defined on '{xamlObjectDefinition.Type.Name}'", xamlObjectDefinition);
			}

			return member;
		}

		private XClassName GetClassName(XamlObjectDefinition control)
		{
			var classMember = FindClassName(control);

			if (classMember == null)
			{
				throw new XamlGenerationException("Unable to find class name for toplevel control", control);
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
				throw new Exception("Unable to find x:Class on the top level element");
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
									throw new XamlGenerationException($"The type '{topLevelControl.Type.Name}' does not support multiple children", implicitContentChild.Objects[1]);
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
											throw new XamlGenerationException($"Unable to implicitly initialize collection for '{contentProperty.Name}'", topLevelControl);
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
												throw new XamlGenerationException($"The type '{topLevelControl.Type.Name}' does not support multiple children", topLevelControl);
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
			catch (Exception e) when (e is not OperationCanceledException and not XamlGenerationException)
			{
				throw new XamlGenerationException($"An error was found in '{topLevelControl.Type.Name}'", e, topLevelControl);
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
		/// <param name="initializers"></param>
		/// <param name="dictIdentifier"></param>
		private void BuildResourceDictionary(IIndentedStringBuilder writer, XamlMemberDefinition? resourcesRoot, bool isInInitializer, string? dictIdentifier = null, IDictionary<XamlObjectDefinition, string>? initializers = default)
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
						writer.AppendIndented("[");
						using (TryTernaryForLinkerHint(writer, resource))
						{
							writer.Append(wrappedKey);
						}
						writer.Append("] = ");
					}
					else
					{
						writer.AppendIndented($"{dictIdentifier}[");
						using (TryTernaryForLinkerHint(writer, resource))
						{
							writer.Append(wrappedKey);
						}
						writer.Append("] = ");
					}
					writer.AppendLine();

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
							writer.AppendLineInvariantIndented($"new global::Uno.UI.Xaml.WeakResourceInitializer(this, {initializerName})");
						}
					}
					else if (initializers?.TryGetValue(resource, out var initializerMethod) is true)
					{
						using (TryTernaryForLinkerHint(writer, resource))
						{
							writer.AppendLineInvariantIndented($"new global::Uno.UI.Xaml.WeakResourceInitializer(this, {initializerMethod})");
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
						throw new XamlGenerationException($"There is already a resource with name '{name}'", resource);
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

			var bodyDisposable = writer.BlockInvariant($"new global::Uno.UI.Xaml.WeakResourceInitializer({currentScope}, {CurrentResourceOwner} => ");

			writer.AppendLineInvariantIndented($"return ");

			var indent = writer.Indent();

			return new DisposableAction(() =>
			{
				resourceOwnerScope.Dispose();
				indent.Dispose();
				writer.AppendLineInvariantIndented(";");
				bodyDisposable.Dispose();
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
					throw new XamlGenerationException("Local values are not allowed in resource dictionary with Source set", dictObject);
				}

				var key = GetDictionaryResourceKey(dictObject);
				if (isDict && key == null)
				{
					throw new XamlGenerationException("Each dictionary entry must have a 'Key'", dictObject);
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
			var hasChildrenWithPhase = HasChildrenWithPhase(objectDefinition);
			var objectDefinitionType = FindType(objectDefinition.Type);
			var isFrameworkElement = IsType(objectDefinitionType, Generation.FrameworkElementSymbol.Value);
			var hasIsParsing = HasIsParsing(objectDefinitionType);

			if (extendedProperties.Any() || hasChildrenWithPhase || isFrameworkElement || hasIsParsing || !objectUid.IsNullOrEmpty())
			{
				if (!useGenericApply && objectDefinitionType is null)
				{
					throw new XamlGenerationException($"The type '{objectDefinition.Type}' could not be found", objectDefinition);
				}

				using (var writer = CreateApplyBlock(outerwriter, objectDefinition))
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
							?.ToArray() ?? [];
					}

					if (hasChildrenWithPhase)
					{
						writer.AppendIndented(GenerateRootPhases(objectDefinition, writer.AppliedParameterName) ?? "");
					}

					var isInsideFrameworkTemplate = IsMemberInsideFrameworkTemplate(objectDefinition).isInside;
#if USE_NEW_TP_CODEGEN
					var isDependencyObject = IsType(objectDefinitionType, Generation.DependencyObjectSymbol.Value);
					if (isInsideFrameworkTemplate && isDependencyObject)
					{
						writer.AppendLineIndented($"{closureName}.SetTemplatedParent(__settings?.TemplatedParent);");
						writer.AppendLineIndented($"__settings?.TemplateMemberCreatedCallback?.Invoke({closureName});");
					}
#endif

					componentDefinition = CurrentScope.Components.FirstOrDefault(x => x.XamlObject == objectDefinition);
					if (componentDefinition is { } || // element can also be register for component by a descendant DO as its resource provider
						HasXBindMarkupExtension(objectDefinition) ||
						HasMarkupExtensionNeedingComponent(objectDefinition))
					{
						writer.AppendLineIndented($"/* _isTopLevelDictionary:{_isTopLevelDictionary} */");
						if (!_isTopLevelDictionary || isInsideFrameworkTemplate)
						{
							componentDefinition ??= AddComponentForCurrentScope(objectDefinition);

							var componentName = componentDefinition.MemberName;

							writer.AppendLineIndented($"__that.{componentName} = {writer.AppliedParameterName};");

							if (isInsideFrameworkTemplate
								&& HasMarkupExtensionNeedingComponent(objectDefinition)
								&& IsDependencyObject(objectDefinition)
								&& !IsUIElement(objectDefinitionType))
							{
								// Ensure that the namescope is property propagated to instances
								// that are not UIElements in order for ElementName to resolve properly
								writer.AppendLineIndented($"global::Microsoft.UI.Xaml.NameScope.SetNameScope(__that.{componentName}, __nameScope);");
							}
						}
						else if (isFrameworkElement && HasMarkupExtensionNeedingComponent(objectDefinition))
						{
							// Register directly for binding updates on the resource referencer itself, since we're inside a top-level ResourceDictionary
							using (writer.BlockInvariant("{0}.Loading += (obj, args) =>", writer.AppliedParameterName))
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
								TryAnnotateWithGeneratorSource(writer);
								BuildComplexPropertyValue(writer, member, writer.AppliedParameterName + ".", writer.AppliedParameterName, componentDefinition: componentDefinition);
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
								BuildSetAttachedProperty(writer, writer.AppliedParameterName, member, objectUid ?? "", isCustomMarkupExtension: true);
							}
							else
							{
								BuildCustomMarkupExtensionPropertyValue(writer, member, writer.AppliedParameterName, _isTopLevelDictionary ? null : $"(({CurrentScope.ClassName})__that)");
							}
						}
						else if (member.Objects.Count > 0)
						{
							if (member.Member.Name == "_UnknownContent") // So : FindType(member.Owner.Type) is INamedTypeSymbol type && IsCollectionOrListType(type)
							{
								foreach (var item in member.Objects)
								{
									writer.AppendLineIndented($"{writer.AppliedParameterName}.Add(");
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
										writer.AppliedParameterName
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
										writer.AppliedParameterName
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
									var localCollectionName = $"{writer.AppliedParameterName}_collection_{_collectionIndex++}";

									var getterMethod = $"Get{member.Member.Name}";

									if (ownerType.GetFirstMethodWithName(getterMethod) is not null)
									{
										// Attached property
										writer.AppendLineIndented(
											$"var {localCollectionName} = {ownerType.GetFullyQualifiedTypeIncludingGlobal()}.{getterMethod}({writer.AppliedParameterName});"
										);
									}
									else
									{
										// Plain object
										writer.AppendLineIndented(
											$"var {localCollectionName} = {writer.AppliedParameterName}.{member.Member.Name};"
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
									throw new XamlGenerationException($"Cannot assign child of type '{childType}' to property of type '{propertyType.GetFullyQualifiedTypeExcludingGlobal()}'", member.Objects[0]);
								}
								else
								{
									throw new XamlGenerationException($"The property '{member.Member.Name}' of type '{propertyType}' does not support adding multiple objects", member);
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
								ValidateName(value, member);

								writer.AppendLineIndented($@"__nameScope.RegisterName(""{value}"", {writer.AppliedParameterName});");
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
									throw new XamlGenerationException($"Unable to find type '{objectDefinition.Type}'", objectDefinition);
								}

								writer.AppendLineInvariantIndented("__that.{0} = {1};", value, writer.AppliedParameterName);
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
								writer.AppendLineIndented($"{GlobalPrefix}Uno.UI.Helpers.MarkupHelper.SetXUid({writer.AppliedParameterName}, \"{objectUid}\");");
							}
							else if (member.Member.Name == "FieldModifier")
							{
								writer.AppendLineInvariantIndented("// FieldModifier {0}", member.Value);
							}
							else if (member.Member.Name == "Phase")
							{
								writer.AppendLineIndented($"{GlobalPrefix}Uno.UI.FrameworkElementHelper.SetRenderPhase({writer.AppliedParameterName}, {member.Value});");
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
									throw new XamlGenerationException("The TargetName property cannot be empty", member);
								}

								var memberGlobalizedType = FindType(member.Member.DeclaringType)?.GetFullyQualifiedTypeIncludingGlobal() ?? GetGlobalizedTypeName(member.Member.DeclaringType.Name);
								writer.AppendLineInvariantIndented(@"{0}.SetTargetName({2}, ""{1}"");",
									memberGlobalizedType,
									this.RewriteAttachedPropertyPath(member.Value.ToString() ?? ""),
									writer.AppliedParameterName);

								writer.AppendLineInvariantIndented("{0}.SetTarget({2}, _{1}Subject);",
									memberGlobalizedType,
									member.Value,
									writer.AppliedParameterName);
							}
							else if (
								member.Member.Name == "TargetName" &&
								!IsAttachedProperty(member) &&
								(member.Member.DeclaringType?.Name.EndsWith("ThemeAnimation", StringComparison.Ordinal) ?? false)
							)
							{
								// Those special animations (xxxThemeAnimation) needs to resolve their target at runtime.
								writer.AppendLineInvariantIndented(@"NameScope.SetNameScope({0}, __nameScope);", writer.AppliedParameterName);
							}
							else if (
								member.Member.Name == "TargetProperty" &&
								IsAttachedProperty(member) &&
								member.Member.DeclaringType?.Name == "Storyboard"
							)
							{
								if (member.Value == null)
								{
									throw new XamlGenerationException("The TargetProperty property cannot be empty", member);
								}

								var memberGlobalizedType = FindType(member.Member.DeclaringType)?.GetFullyQualifiedTypeIncludingGlobal() ?? GetGlobalizedTypeName(member.Member.DeclaringType.Name);
								writer.AppendLineInvariantIndented(@"{0}.SetTargetProperty({2}, ""{1}"");",
									memberGlobalizedType,
									this.RewriteAttachedPropertyPath(member.Value.ToString() ?? ""),
									writer.AppliedParameterName);
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
									writer.AppliedParameterName,
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
										BuildSetAttachedProperty(writer, writer.AppliedParameterName, member, objectUid ?? "", isCustomMarkupExtension: false);
									}
									else if (eventSymbol != null)
									{
										GenerateInlineEvent(writer.AppliedParameterName, writer, member, eventSymbol, componentDefinition);
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

						using (writer.BlockInvariant($"global::Uno.UI.Helpers.MarkupHelper.Set{objectDefinition.Type.Name}Lazy({writer.AppliedParameterName}, () => "))
						{
							BuildLiteralLazyVisualStateManagerProperties(writer, objectDefinition, writer.AppliedParameterName);

							if (implicitContentChild != null && lazyContentProperty != null)
							{
								writer.AppendLineIndented($"{writer.AppliedParameterName}.{lazyContentProperty.Name} = ");

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
								$"{writer.AppliedParameterName}, " +
								$"__baseUri_{_fileUniqueId}, " +
								$"\"file:///{_fileDefinition.FilePath.Replace("\\", "/")}\", " +
								$"{objectDefinition.LineNumber}, " +
								$"{objectDefinition.LinePosition}" +
								$");");
						}
						else
						{
							writer.AppendLineIndented($"global::Uno.UI.FrameworkElementHelper.SetBaseUri({writer.AppliedParameterName}, __baseUri_{_fileUniqueId});");
						}
					}

					if (IsNotFrameworkElementButNeedsSourceLocation(objectDefinition) && _isHotReloadEnabled)
					{
						TrySetOriginalSourceLocation(writer, $"{writer.AppliedParameterName}", objectDefinition);
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

						BuildUiAutomationId(writer, writer.AppliedParameterName, uiAutomationId, objectDefinition);
					}

					BuildStatementLocalizedProperties(writer, objectDefinition, writer.AppliedParameterName);

					if (hasIsParsing)
					{
						// This should always be the last thing called when an element is parsed.
						writer.AppendLineInvariantIndented("{0}.CreationComplete();", writer.AppliedParameterName);
					}
				}
			}

			// Local function used to build a property/value for any custom MarkupExtensions
			void BuildCustomMarkupExtensionPropertyValue(IIndentedStringBuilder writer, XamlMemberDefinition member, string closure, string? resourceOwner)
			{
				var propertyValue = GetCustomMarkupExtensionValue(member, closure, resourceOwner);
				if (!propertyValue.IsNullOrEmpty())
				{
					writer.AppendIndented($"{closure}.{member.Member.Name} = {propertyValue};\r\n");
				}
			}
		}

		private static bool IsNotFrameworkElementButNeedsSourceLocation(XamlObjectDefinition objectDefinition)
			=> objectDefinition.Type.Name is "VisualState" or "AdaptiveTrigger" or "StateTrigger";

		private void ValidateName(string? value, IXamlLocation location)
		{
			// Nullable suppressions are due to https://github.com/dotnet/roslyn/issues/55507
			if (!SyntaxFacts.IsValidIdentifier(value!))
			{
				throw new XamlGenerationException($"The value '{value}' is an invalid value for 'Name' property", location);
			}

			if (!CurrentScope.DeclaredNames.Add(value!))
			{
				throw new XamlGenerationException($"The name '{value}' is already defined in this scope", location);
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
			if (eventSymbol.Type is not INamedTypeSymbol delegateSymbol)
			{
				GenerateError(writer, $"'{eventSymbol.Type}' is not a supported event");
				return;
			}

			// If a binding is inside a DataTemplate, the binding root in the case of an x:Bind is
			// the DataContext, not the control's instance.
			var template = IsMemberInsideFrameworkTemplate(member.Owner);
			var targetInstance = (template.isInside, _xClassName) switch
			{
				(false, _) => "this",
				(_, not null) => CurrentResourceOwnerName,
				_ => null
			};
			if (targetInstance is null)
			{
				GenerateError(writer, $"Unable to use event '{member.Member.Name}' without a backing class (use x:Class)");
				return;
			}
			EnsureXClassName();

			var parametersWithType = delegateSymbol
				.DelegateInvokeMethod
				?.Parameters
				.Select(p => $"{p.Type.GetFullyQualifiedTypeIncludingGlobal()} {p.Name}")
				.ToArray();
			var parameters = delegateSymbol
				.DelegateInvokeMethod
				?.Parameters
				.Select(p => p.Name)
				.ToArray();

			if (member.Objects.FirstOrDefault() is XamlObjectDefinition bind && bind.Type.Name == "Bind")
			{
				if (componentDefinition is null)
				{
					// 500 - Internal XAML Code gen error
					throw new XamlGenerationException("The component definition cannot be null", bind);
				}

				CurrentScope.XBindExpressions.Add(bind);

				var path = XBindExpressionParser.RestoreSinglePath(bind.Members.First().Value?.ToString());
				if (path is null)
				{
					throw new XamlGenerationException("x:Bind event path cannot be empty", bind);
				}

				INamedTypeSymbol GetTargetType()
				{
					if (template.isInside)
					{
						var dataTypeObject = FindMember(template.xamlObject!, "DataType", XamlConstants.XamlXmlNamespace);
						if (dataTypeObject?.Value == null)
						{
							throw new XamlGenerationException("Unable to find x:DataType in enclosing DataTemplate for x:Bind event", bind);
						}

						return GetType(dataTypeObject.Value.ToString() ?? "");
					}
					else if (_xClassName?.Symbol is not null)
					{
						return _xClassName.Symbol;
					}
					else
					{
						throw new XamlGenerationException($"Unable to find the type '{_xClassName?.Namespace}.{_xClassName?.ClassName}'", bind);
					}
				}

				var targetType = GetTargetType(); // The type of the target object onto which the x:Bind path should be resolved
				var targetInstanceWeakRef = template.isInside
					// Use of __rootInstance is required to get the top-level DataContext, as it may be changed in the current visual tree by the user.
					? "(__rootInstance as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference"
					: $"({targetInstance} as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference";

				var method = ResolveXBindMethod(targetType, path, bind);
				var invokeTarget = (method.isStatic, template.isInside) switch
				{
					(true, _) => method.declaringType.GetFullyQualifiedTypeIncludingGlobal(), // If the method is static, the target onto which the method should be invoked is the declaringType itself
					(_, true) => $"((target.Target as {XamlConstants.Types.FrameworkElement})?.DataContext as {targetType.GetFullyQualifiedTypeIncludingGlobal()})?",
					_ => $"(target.Target as {targetType.GetFullyQualifiedTypeIncludingGlobal()})?"
				};

				var handler = RegisterChildSubclass(
					$"{member.Key}_{member.Member.Name}_Handler",
					(name, subWriter) => subWriter.AppendMultiLineIndented($$"""
							public class {{name}}(global::Uno.UI.DataBinding.ManagedWeakReference target)
							{
								public void Invoke({{parametersWithType.JoinBy(", ")}})
								{
									{{invokeTarget}}.{{method.path}}({{(method.symbol.Parameters.Any() ? parameters.JoinBy(", ") : "")}});
								}
							}
						"""));

				RegisterXBindEventInitializer(
					$"{member.Key}_{member.Member.Name}_Initialize",
					(name, subWriter) => subWriter.AppendMultiLineIndented($$"""
							[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
							{{Field("bool", $"__is{name}d")}}

							[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
							private void {{name}}(bool init)
							{
								if (__is{{name}}d || {{componentDefinition.MemberName}} is null)
								{
									if (!init)
									{
										__is{{name}}d = false;
										// Note: {{componentDefinition.MemberName}} will be collected, no needs to unsubscribe
									}
									
									return;
								}

								{{componentDefinition.MemberName}}.{{member.Member.Name}} += new {{handler}}({{targetInstanceWeakRef}}).Invoke;
								__is{{name}}d = true;
							}
						"""));
			}
			else
			{
				//
				// Generate a sub-class that uses a weak ref, so the owner is not being held onto by the delegate.
				// We can use the WeakReferenceProvider to get a self reference to avoid adding the cost of the
				// creation of a WeakReference.
				// The sub-class prevents fuzzy matching of delegates by HR.
				//
				var subClass = RegisterChildSubclass(
					$"{member.Key}_{member.Member.Name}_Handler",
					(name, subWriter) => subWriter.AppendMultiLineIndented($$"""
							public class {{name}}(global::Uno.UI.DataBinding.ManagedWeakReference target)
							{
								public void Invoke({{parametersWithType.JoinBy(", ")}})
								{
									(target.Target as {{_xClassName}})?.{{member.Value}}({{parameters.JoinBy(", ")}});
								}
							}
						"""));
				writer.AppendLineIndented($"var {member.Member.Name}_Handler = new {subClass}(({targetInstance} as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference);");
				writer.AppendLineIndented($"/* second level */ {closureName}.{member.Member.Name} += {member.Member.Name}_Handler.Invoke;");
			}
		}

		private (string path, ITypeSymbol declaringType, IMethodSymbol symbol, bool isStatic) ResolveXBindMethod(INamedTypeSymbol contextType, string path, IXamlLocation location)
		{
			if (path.Contains("."))
			{
				var rewrittenPath = new StringBuilder();
				ITypeSymbol currentType = contextType;

				var parts = path.Split('.').ToList();
				var isStatic = parts.FirstOrDefault()?.Contains(":") ?? false;
				if (isStatic)
				{
					// First part is a type for static method binding and should override the original source type
					currentType = contextType = GetType(RewriteNamespaces(parts[0]));
					parts.RemoveAt(0);
				}

				for (var i = 0; i < parts.Count - 1; i++)
				{
					var memberName = RewriteNamespaces(parts[i]);
					var next = currentType.GetAllMembersWithName(memberName).FirstOrDefault();

					currentType = next switch
					{
						IFieldSymbol fs => fs.Type,
						IPropertySymbol ps => ps.Type,
						null => throw new XamlGenerationException($"Unable to find member {parts[i]} on type {currentType}", location),
						_ => throw new XamlGenerationException($"The field {next.Name} is not supported for x:Bind event binding", location)
					};

					rewrittenPath.Append(memberName);
					rewrittenPath.Append("?.");
				}

				var method = currentType.GetFirstMethodWithName(parts.Last(), includeBaseTypes: true)
					?? throw new XamlGenerationException($"Failed to find {parts.Last()} on {currentType}", location);

				rewrittenPath.Append(method.Name);

				return (rewrittenPath.ToString(), contextType, method, isStatic);
			}
			else
			{
				var method = contextType.GetFirstMethodWithName(path, includeBaseTypes: true)
					?? throw new XamlGenerationException($"Failed to find {path} on {contextType}", location);

				return (method.Name, contextType, method, false);
			}
		}

		/// <summary>
		/// Build localized properties which have not been set in the xaml.
		/// </summary>
		private bool BuildInlineLocalizedProperties(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, INamedTypeSymbol? objectDefinitionType, bool isInInitializer = true)
		{
			TryAnnotateWithGeneratorSource(writer);
			var objectUid = GetObjectUid(objectDefinition);

			var ret = false;
			if (objectUid != null)
			{
				var candidateProperties = FindLocalizableProperties(objectDefinitionType)
					.Except(objectDefinition.Members.Select(m => m.Member.Name));
				foreach (var prop in candidateProperties)
				{
					var localizedValue = BuildLocalizedResourceValue(null, prop, objectUid);
					if (localizedValue != null)
					{
						ret = true;
						if (isInInitializer)
						{
							writer.AppendLineInvariantIndented("{0} = {1},", prop, localizedValue);
						}
						else
						{
							writer.AppendLineInvariantIndented("{0} = {1};", prop, localizedValue);
						}
					}
				}
			}

			return ret;
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
							$"({propertyType})global::Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({propertyType}),{localizedValue})";

						writer.AppendLineInvariantIndented($"{candidate.ownerType.GetFullyQualifiedTypeIncludingGlobal()}.Set{candidate.property}({closureName}, {convertedLocalizedProperty});");
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
			}

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

		private XamlLazyApplyBlockIIndentedStringBuilder CreateApplyBlock(IIndentedStringBuilder writer, XamlObjectDefinition appliedObject)
			=> CreateApplyBlock(writer, appliedObject, FindType(appliedObject.Type));

		private XamlLazyApplyBlockIIndentedStringBuilder CreateApplyBlock(IIndentedStringBuilder writer, XamlObjectDefinition declaringObject, INamedTypeSymbol? appliedType)
		{
			TryAnnotateWithGeneratorSource(writer);

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
				CurrentScope,
				declaringObject,
				appliedType,
				xamlApplyPrefix: appliedType != null && !_isHotReloadEnabled ? _fileUniqueId : null,
				delegateType,
				!_isTopLevelDictionary);
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

		private string RegisterChildSubclass(string name, XamlMemberDefinition owner, string returnType)
		{
			return NamingHelper.AddUnique(CurrentScope.Subclasses, name, new Subclass(owner, returnType, GetDefaultBindMode()));
		}

		/// <summary>
		/// Register a sub-class to be generated for the current scope.
		/// </summary>
		/// <param name="name">
		/// The **suggested** name of the class.
		/// Be aware this class might not be directly accessible (might be generated nested into another class).
		/// **Don't use this name directly**, prefer to use the returned fullname instead.
		/// </param>
		/// <param name="build">Callback to write the content of the class.</param>
		/// <returns>The fullname of the generated class.</returns>
		private string RegisterChildSubclass(string name, Action<string, IIndentedStringBuilder> build)
		{
			var fullName = $"{CurrentScope.SubClassesRoot}.{name}";
			CurrentScope.SubclassBuilders.Add(writer => build(name, writer));

			return fullName;
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
						if (member.Owner is { } owner && !IsFrameworkElement(owner.Type))
						{
							if (_logicalScopeStack.FirstOrDefault(x => IsFrameworkElement(x.Object.Type)) is { } nearestFE)
							{
								var thisComponent = GetOrAddComponentForCurrentScope(owner);
								thisComponent.ResourceContext = GetOrAddComponentForCurrentScope(nearestFE.Object);
							}
						}

						var args = string.Join(", ", new string[]
						{
							closureName!,
							$"{propertyOwner!.GetFullyQualifiedTypeIncludingGlobal()}.{member.Member.Name}Property",
							$"\"{resourceKey}\"",
							$"isThemeResourceExtension: {(isThemeResourceExtension ? "true" : "false")}",
							$"isHotReloadSupported: {(_isHotReloadEnabled ? "true" : "false")}",
							$"context: {ParseContextPropertyAccess}",
						});
						writer.AppendLineInvariantIndented($"global::Uno.UI.ResourceResolverSingleton.Instance.ApplyResource({args});");
					}
					else if (IsAttachedProperty(member))
					{
						if (closureName == null)
						{
							throw new XamlGenerationException("A closure name is required for attached properties", member);
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

			var sourceInstance = CurrentResourceOwner switch
			{
				null => "__that",
				_ when _scopeStack is { Count: 1 } => "__that",
				var owner => "__that." + owner
			};

			if (isInsideDataTemplate)
			{
				var dataTypeObject = FindMember(dataTemplateObject!, "DataType", XamlConstants.XamlXmlNamespace);
				if (dataTypeObject?.Value == null)
				{
					throw new XamlGenerationException("Unable to find x:DataType in enclosing DataTemplate", bindNode);
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
								throw new XamlGenerationException($"Expected BindBack for '{rawFunction}'", bindNode);
							}
						}
						else
						{
							if (contextFunction.Properties.Length == 1)
							{
								var targetPropertyType = GetXBindPropertyPathType(contextFunction.Properties[0], dataTypeSymbol, bindNode).GetFullyQualifiedTypeIncludingGlobal();
								var contextFunctionLValue = XBindExpressionParser.Rewrite("___tctx", rawFunction, dataTypeSymbol, _metadataHelper.Compilation.GlobalNamespace, isRValue: false, _xBindCounter, FindType, targetPropertyType);
								if (contextFunctionLValue.MethodDeclaration is not null)
								{
									RegisterXBindTryGetDeclaration(contextFunctionLValue.MethodDeclaration);
									return $"(___ctx, __value) => {{ if(___ctx is {dataType} ___tctx) {{ {contextFunctionLValue.Expression}; }} }}";
								}
								else
								{
									return $"(___ctx, __value) => {{ if(___ctx is {dataType} ___tctx) {{ {contextFunctionLValue.Expression} = ({targetPropertyType})global::Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({targetPropertyType}), __value); }} }}";
								}
							}
							else
							{
								throw new XamlGenerationException("Invalid x:Bind property path count (This should not happen)", bindNode);
							}
						}
					}
					else
					{
						return "null";
					}
				}

				return $".BindingApply(___b => /*defaultBindMode{GetDefaultBindMode()}*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, null, ___ctx => ___ctx is {GetType(dataType).GetFullyQualifiedTypeIncludingGlobal()} ___tctx ? ({contextFunction.Expression}) : (false, default), {buildBindBack()} {pathsArray}))";
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
								throw new XamlGenerationException($"Expected BindBack for x:Bind function '{rawFunction}'", bindNode);
							}
						}
						else
						{
							if (rewrittenRValue.Properties.Length == 1)
							{
								var targetPropertyType = GetXBindPropertyPathType(rewrittenRValue.Properties[0], rootType: null, bindNode).GetFullyQualifiedTypeIncludingGlobal();

								if (string.IsNullOrEmpty(rawFunction))
								{
									return $"(___ctx, __value) => {{ " +
										$"if(___ctx is {_xClassName} ___tctx) " +
										$"___ctx = ({targetPropertyType})global::Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({targetPropertyType}), __value);" +
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
										$"{rewrittenLValue.Expression} = ({targetPropertyType})global::Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof({targetPropertyType}), __value);" +
										$" }}";
								}
							}
							else
							{
								throw new XamlGenerationException("Invalid x:Bind property path count (This should not happen)", bindNode);
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

				return $".BindingApply({sourceInstance}, (___b, ___t) =>  /*defaultBindMode{GetDefaultBindMode()} {rawFunction}*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, ___t, ___ctx => {bindFunction}, {buildBindBack()} {pathsArray}))";
			}
		}

		private ITypeSymbol GetXBindPropertyPathType(string propertyPath, INamedTypeSymbol? rootType, IXamlLocation location)
		{
			ITypeSymbol currentType = rootType
				?? _xClassName?.Symbol
				?? throw new XamlGenerationException($"Unable to find type {_xClassName}", location);

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
						throw new XamlGenerationException($"Unable to find member '{part}' on type '{elementByName.Type}'", location);
					}
				}
				else
				{
					// We can't find the type. It could be something that is source-generated, or it could be a user error.
					// For source-generated members, it's not possible to reliably get this information due to https://github.com/dotnet/roslyn/issues/57239
					// However, we do a best effort to handle the common scenario, which is code generated by CommunityToolkit.Mvvm.
					if (!TryFindThirdPartyType(currentType, part, out var thirdPartyType))
					{
						throw new XamlGenerationException($"Unable to find member '{part}' on type '{currentType}'", location);
					}
					else
					{
						currentType = thirdPartyType;
					}
				}

				if (isIndexer)
				{
					currentType = currentType.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.IsIndexer).Type;
				}
			}

			return currentType;
		}

		private bool TryFindThirdPartyType(
			ITypeSymbol type,
			string memberName,
			[NotNullWhen(true)] out ITypeSymbol? thirdPartyType)
		{
			foreach (var typeProvider in Generation.TypeProviders)
			{
				if (typeProvider.TryGetType(type, memberName) is { } foundType)
				{
					thirdPartyType = foundType;
					return true;
				}
			}

			thirdPartyType = null;
			return false;
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
			// Given a xamlString like "muxc:SomeControl", converts it to Microsoft.UI.Xaml.Controls.SomeControl.
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

		private string GetCustomMarkupExtensionValue(XamlMemberDefinition member, string? target = null, string? resourceOwner = null)
		{
			// Get the type of the custom markup extension
			var markup = member
				.Objects
				.Select(x => new { Value = x, Symbol = GetMarkupExtensionType(x.Type) })
				.FirstOrDefault(x => x.Symbol != null);
			if (markup == null)
			{
				throw new XamlGenerationException($"Unable to find markup extension type '{member.Objects.FirstOrDefault()?.Type}'", member);
			}

			var property = FindProperty(member);
			var declaringType = property?.ContainingType;
			var propertyType = property?.FindDependencyPropertyType(unwrapNullable: false);

			if (declaringType == null || propertyType == null)
			{
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
					var value = resourceName ??
						(HasCustomMarkupExtension(m)
							? GetCustomMarkupExtensionValue(m, target, resourceOwner)
							: BuildLiteralValue(m, propertyType: propertyType, owner: member));

					return "{0} = {1}".InvariantCultureFormat(m.Member.Name, value);
				})
				.JoinBy(", ");
			var markupInitializer = !properties.IsNullOrEmpty() ? $" {{ {properties} }}" : "()";

			var thatCurrentResourceOwnerName = resourceOwner switch
			{
				not null => resourceOwner,
				null when CurrentResourceOwner is { } owner => owner,
				_ => "this"
			};

			// Build the parser context for ProvideValue(IXamlServiceProvider)
			var providerDetails = new string[]
			{
				// CreateParserContext(object? target, Type propertyDeclaringType, string propertyName, Type propertyType, object rootObject)
				target ?? "null",
				$"typeof({globalized.PvtpDeclaringType})",
				$"\"{member.Member.Name}\"",
				$"typeof({globalized.PvtpType})",
				// the ResourceOwner for an ResDict is the RD's singleton instance, not the RD itself
				$"({thatCurrentResourceOwnerName} as object as {DictionaryProviderInterfaceName})?.GetResourceDictionary() ?? (object){thatCurrentResourceOwnerName}",
			};
			var provider = $"{globalized.MarkupHelper}.CreateParserContext({providerDetails.JoinBy(", ")})";

			var provideValue = $"(({globalized.IMarkupExtensionOverrides})new {globalized.MarkupType}{markupInitializer}).ProvideValue({provider})";
			var unwrappedPropertyType = propertyType.IsNullable(out var nullableInnerType) ? nullableInnerType as INamedTypeSymbol : propertyType;
			if (IsImplementingInterface(unwrappedPropertyType, Generation.IConvertibleSymbol.Value))
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
					var converterValue = BuildXamlTypeConverterLiteralValue(targetPropertyType, memberValue, includeQuotations: false, member);
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

		private string BuildLiteralValue(INamedTypeSymbol propertyType, bool isTemplateBindingAttachedProperty, string? memberValue, XamlMemberDefinition owner, string memberName = "", string objectUid = "")
		{
			var literalValue = Inner();
			TryAnnotateWithGeneratorSource(ref literalValue);
			return literalValue;

			string GetMemberValue()
			{
				if (string.IsNullOrWhiteSpace(memberValue))
				{
					throw new XamlGenerationException("The property value is invalid", owner);
				}

				return memberValue!;
			}

			string Inner()
			{
				if (IsLocalizedString(propertyType, objectUid))
				{
					var resourceValue = BuildLocalizedResourceValue(FindType(owner.Member.DeclaringType), memberName, objectUid);

					if (resourceValue != null)
					{
						return resourceValue;
					}
				}

				// If the property Type is attributed with the CreateFromStringAttribute
				if (IsXamlTypeConverter(propertyType))
				{
					// We must build the member value as a call to a "conversion" function
					return BuildXamlTypeConverterLiteralValue(propertyType, GetMemberValue(), includeQuotations: true, owner);
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
						return BuildDependencyProperty(GetMemberValue(), owner);

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

					case "Microsoft.UI.Xaml.Media.CacheMode":
						return ParseCacheMode(GetMemberValue(), owner);

					case "System.Drawing.PointF":
						return "new System.Drawing.PointF(" + AppendFloatSuffix(GetMemberValue()) + ")";

					case "System.Drawing.Size":
						return "new System.Drawing.Size(" + SplitAndJoin(memberValue) + ")";

					case "Windows.Foundation.Size":
						return "new Windows.Foundation.Size(" + SplitAndJoin(memberValue) + ")";

					case "Microsoft.UI.Xaml.Media.Matrix":
						return "new Microsoft.UI.Xaml.Media.Matrix(" + SplitAndJoin(memberValue) + ")";

					case "Windows.Foundation.Point":
						return "new Windows.Foundation.Point(" + SplitAndJoin(memberValue) + ")";

					case "System.Numerics.Vector2":
						return "new global::System.Numerics.Vector2(" + SplitAndJoin(memberValue) + ")";

					case "System.Numerics.Vector3":
						return "new global::System.Numerics.Vector3(" + SplitAndJoin(memberValue) + ")";

					case "Microsoft.UI.Xaml.Input.InputScope":
						return "new global::Microsoft.UI.Xaml.Input.InputScope { Names = { new global::Microsoft.UI.Xaml.Input.InputScopeName { NameValue = global::Microsoft.UI.Xaml.Input.InputScopeNameValue." + memberValue + "} } }";

					case "UIKit.UIImage":
						var imageValue = GetMemberValue();
						if (imageValue.StartsWith(XamlConstants.BundleResourcePrefix, StringComparison.InvariantCultureIgnoreCase))
						{
							return "UIKit.UIImage.FromBundle(\"" + imageValue.Substring(XamlConstants.BundleResourcePrefix.Length, imageValue.Length - XamlConstants.BundleResourcePrefix.Length) + "\")";
						}
						return imageValue;

					case "Microsoft.UI.Xaml.Controls.IconElement":
						return "new Microsoft.UI.Xaml.Controls.SymbolIcon { Symbol = Microsoft.UI.Xaml.Controls.Symbol." + memberValue + "}";

					case "Windows.Media.Playback.IMediaPlaybackSource":
						return "Windows.Media.Core.MediaSource.CreateFromUri(new Uri(\"" + memberValue + "\"))";

					case "Microsoft.UI.Xaml.Media.ImageSource":
						return RewriteUri(memberValue);

					case "Microsoft.UI.Xaml.TargetPropertyPath":
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

						_ => throw new XamlGenerationException($"The enum underlying type '{propertyType.EnumUnderlyingType}' is not expected", owner),
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
						throw new XamlGenerationException($"Failed to create a '{propertyTypeWithoutGlobal}' from the text '{value}'", owner);
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

				throw new XamlGenerationException($"Unable to convert '{memberValue}' to '{propertyType}'", owner);

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

		private string ParseCacheMode(string memberValue, IXamlLocation location)
		{
			if (memberValue.Equals("BitmapCache", StringComparison.OrdinalIgnoreCase))
			{
				return "new global::Microsoft.UI.Xaml.Media.BitmapCache()";
			}

			throw new XamlGenerationException($"The '{memberValue}' cache mode is not supported", location);
		}

		private string BuildTargetPropertyPath(string target, XamlMemberDefinition owner)
		{
			// https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.setter.target?view=winrt-22621
			// The Setter.Target property can be used in either a Style or a VisualState, but in different ways.
			// - When used in a Style, the property that needs to be modified can be specified directly.
			// - When used in VisualState, the Target property must be given a TargetPropertyPath(dotted syntax with a target element and property explicitly specified).
			if (CurrentStyleTargetType is not null)
			{
				// Target is used in a style, so it defines the DP directly.
				return BuildDependencyProperty(target, owner);
			}

			var ownerControl = GetControlOwner(owner.Owner);
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
					return $"new global::Microsoft.UI.Xaml.TargetPropertyPath(this._{elementName}Subject, \"{propertyName}\")";
				}
				else
				{
					return $"/* target element not found {elementName} */ null";
				}
			}

			// 500 - Internal XAML Code gen error
			throw new XamlGenerationException("GetControlOwner returned null", owner);
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
			var gridLength = Microsoft.UI.Xaml.GridLength.ParseGridLength(memberValue).FirstOrDefault();

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
						throw new XamlGenerationException($"The property '{member.Owner?.Type?.Name}.{member.Member?.Name}' is unknown", member);
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
						throw new XamlGenerationException($"MarkupExtension '{expression.Type.Name}' is not supported", member);
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
			else if (ushort.TryParse(memberValue, out var numericWeightValue))
			{
				return $"new global::{XamlConstants.Types.FontWeight}() {{ Weight = {numericWeightValue.ToStringInvariant()} }}";
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

		private string BuildDependencyProperty(string property, IXamlLocation location)
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
				throw new XamlGenerationException($"Cannot convert '{property}' to DependencyProperty", location);
			}

			if (IsDependencyProperty(currentStyleTargetType, property))
			{
				return $"{currentStyleTargetType.GetFullyQualifiedTypeIncludingGlobal()}.{property}Property";
			}

			throw new XamlGenerationException($"'{property}' is not a DependencyProperty in type '{currentStyleTargetType.GetFullyQualifiedTypeExcludingGlobal()}'", location);
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

			return "new global::Microsoft.UI.Xaml.Thickness(" + memberValue + ")";
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
					throw new XamlGenerationException("The property ElementName cannot be empty", m);
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

							_ => throw new XamlGenerationException($"The enum underlying type '{actualValueType.EnumUnderlyingType}' is not expected", m),
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
		private bool GetKnownNewableListOrCollectionInterface(INamedTypeSymbol? type, [NotNullWhen(true)] out string? newableTypeName)
		{
			switch (type?.Name)
			{
				case "ICollection":
				case "IList":
					newableTypeName = type.IsGenericType ?
						"List<{0}>".InvariantCultureFormat(GetFullGenericTypeName(type.TypeArguments.Single() as INamedTypeSymbol)) :
						"List<System.Object>";
					return true;
			}

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

		private static bool IsNewScope(XamlObjectDefinition xamlObjectDefinition)
		{
			return xamlObjectDefinition.Type.Name
				is "DataTemplate"
				or "ItemsPanelTemplate"
				or "ControlTemplate"

				// This case is specific the custom ListView for iOS. Should be removed
				// when the list rebuilt to be compatible.
				or "ListViewBaseLayoutTemplate";
		}

		private void BuildChild(IIndentedStringBuilder writer, XamlMemberDefinition? owner, XamlObjectDefinition xamlObjectDefinition, string? outerClosure = null)
		{
			_generatorContext.CancellationToken.ThrowIfCancellationRequested();

			using var scopeAutoDisposable = LogicalScope(xamlObjectDefinition);

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
				try
				{
					if (IsNewScope(xamlObjectDefinition)) // a.k.a. IsTemplate()
					{
						// The unknown content is what's inside the DataTemplate element, but for the location we want the position of the "DataTemplate" element.
						var contentDefinition = xamlObjectDefinition.Members.FirstOrDefault(m => m.Member.Name == XamlConstants.UnknownContent);
						var contentLocation = (IXamlLocation)xamlObjectDefinition.Members.FirstOrDefault(m => m.Member.Name == "Key") ?? xamlObjectDefinition;

						// This case is to support the layout switching for the ListViewBaseLayout, which is not
						// a FrameworkTemplate. This will need to be removed when this custom list view is removed.
						var contentType = typeName == "ListViewBaseLayoutTemplate"
							? "global::Uno.UI.Controls.Legacy.ListViewBaseLayout"
							: "_View";

						if (_isHotReloadEnabled)
						{
							// If hot reload is enabled, we want to pre-generate a sub-type and a factory method even if the template is empty,
							// so variation of code is smaller compared to not provide any method to the ctor of the template.
							// For this we inject a fake _UnknownMember with a single `null` content object.
							contentDefinition ??= new XamlMemberDefinition(
								new XamlMember(
									XamlConstants.UnknownContent,
									new XamlType(XamlConstants.BaseXamlNamespace, "UIElement", new List<XamlType>(), new XamlSchemaContext()),
									false),
								xamlObjectDefinition.LineNumber,
								xamlObjectDefinition.LinePosition,
								xamlObjectDefinition)
							{
								Objects =
							{
								new XamlObjectDefinition(
									new XamlType(XamlConstants.BaseXamlNamespace, "NullExtension", [], new()),
									xamlObjectDefinition)
							}
							};
						}

						string GetCacheBrokerForHotReload()
							=> _isHotReloadEnabled
								? $"\"{_fileDefinition.Checksum}\".ToString(); // Forces this method to be updated (and use updated sub class type) when the file is being updated through Hot Reload"
								: string.Empty;

						if (contentDefinition is null)
						{
							// Note: When HR enabled, we still generate a factory method so we will be able to add content later in it.
							writer.AppendIndented($"new {GetGlobalizedTypeName(fullTypeName)}(/* This template does not have a content for this platform */)");
						}
						else
						{
							// To prevent conflicting names whenever we are working with dictionaries, subClass index is a Guid in those cases
							var isTopLevel = _scopeStack.Count == 1 && _scopeStack.Last().Name.EndsWith("RD", StringComparison.Ordinal);
							var namespacePrefix = isTopLevel ? "__Resources." : "";
							var subclassName = RegisterChildSubclass(contentDefinition.Key, contentDefinition, contentType);
							var buildMethod = CurrentScope.RegisterMethod(
								$"Build_{subclassName.TrimStart('_')}",
								(name, sb) => TryAnnotateWithGeneratorSource(sb).AppendMultiLineIndented($$"""
								private static {{contentType}} {{name}}(object __owner, global::Microsoft.UI.Xaml.TemplateMaterializationSettings __settings)
								{
									{{GetCacheBrokerForHotReload()}}
									return new {{namespacePrefix}}{{CurrentScope.SubClassesRoot}}.{{subclassName}}().Build(__owner, __settings);
								}
								"""));

							writer.AppendIndented($"new {GetGlobalizedTypeName(fullTypeName)}({CurrentResourceOwnerName}, {buildMethod})");
						}

						writer.AppendLine();

						if (_isHotReloadEnabled)
						{
							// Sets the location for the <Data|Control|...>Template
							using var applyWriter = CreateApplyBlock(writer, xamlObjectDefinition);
							TrySetOriginalSourceLocation(applyWriter, applyWriter.AppliedParameterName, contentLocation);
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
							// 500 - Internal XAML Code gen error
							throw new XamlGenerationException($"An owner is required for '{typeName}'", xamlObjectDefinition);
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
							"new global::Microsoft.UI.Xaml.Setter<{0}>(\"{1}\", o => o.{1} = {2})",
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
								throw new XamlGenerationException($"Unable to find content value on '{xamlObjectDefinition}'", xamlObjectDefinition);
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
									throw new XamlGenerationException("TargetType cannot be empty", xamlObjectDefinition);
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
										using (var applyWriter = CreateApplyBlock(writer, xamlObjectDefinition, Generation.StyleSymbol.Value)) // TODO: Validate if the FindType(xamlObjectDefinition) resolves properly and remove the redundant parameter Generation.StyleSymbol.Value
										{
											TrySetOriginalSourceLocation(applyWriter, applyWriter.AppliedParameterName, xamlObjectDefinition);
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
				catch (XamlGenerationException xamlGenError)
				{
					_errors.Add(xamlGenError);
					writer.AppendLineIndented("default!");
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

		private List<string> FindNamesIn(XamlObjectDefinition xamlObjectDefinition)
		{
			var list = new List<string>();
			foreach (var element in EnumerateSubElements(xamlObjectDefinition, stoppingCondition: IsNewScope))
			{
				var nameMember = FindMember(element, "Name");

				if (nameMember?.Value is string name)
				{
					list.Add(name);
				}
			}

			return list;
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
		private IEnumerable<XamlObjectDefinition> EnumerateSubElements(XamlObjectDefinition xamlObject, Func<XamlObjectDefinition, bool>? stoppingCondition = null)
		{
			yield return xamlObject;

			foreach (var member in xamlObject.Members)
			{
				foreach (var element in EnumerateSubElements(member.Objects, stoppingCondition))
				{
					yield return element;
				}
			}

			var objects = xamlObject.Objects;

			foreach (var element in EnumerateSubElements(objects, stoppingCondition))
			{
				yield return element;
			}
		}

		private IEnumerable<XamlObjectDefinition> EnumerateSubElements(IEnumerable<XamlObjectDefinition> objects, Func<XamlObjectDefinition, bool>? stoppingCondition)
		{
			foreach (var child in objects.Safe())
			{
				if (stoppingCondition != null && stoppingCondition(child))
				{
					continue;
				}

				yield return child;

				foreach (var innerElement in EnumerateSubElements(child, stoppingCondition))
				{
					if (stoppingCondition != null && stoppingCondition(innerElement))
					{
						continue;
					}

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
				var nameMember = FindMember(definition, "Name");
				var nameField = nameMember is { Value: string name } ? SanitizeResourceName(name) : null;

				var elementStubBaseType = Generation.ElementStubSymbol.Value.BaseType;
				if (!(targetType?.Is(elementStubBaseType) ?? false))
				{
					writer.AppendLineIndented($"/* Lazy DeferLoadStrategy was ignored because the target type is not based on {elementStubBaseType?.GetFullyQualifiedTypeExcludingGlobal()} */");
					return null;
				}

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

					using (var innerWriter = CreateApplyBlock(writer, definition, Generation.ElementStubSymbol.Value))
					{
						if (nameMember != null)
						{
							innerWriter.AppendLineIndented(
								$"{innerWriter.AppliedParameterName}.Name = \"{nameMember.Value}\";"
							);

							// Set the element name to the stub, then when the stub will be replaced
							// the actual target control will override it.
							innerWriter.AppendLineIndented(
								$"_{nameField}Subject.ElementInstance = {innerWriter.AppliedParameterName};"
							);
						}

						var members = new List<XamlMemberDefinition>();

						if (hasLoadMarkup)
						{
							members.Add(GenerateBinding("Load", loadMember, definition));
						}

						var isInsideFrameworkTemplate = IsMemberInsideFrameworkTemplate(definition).isInside;

						var needsBindingUpdates = HasXBindMarkupExtension(definition) || HasMarkupExtensionNeedingComponent(definition);
						var childrenNeedBindingUpdates = CurrentXLoadScope?.Components.Any(c => HasXBindMarkupExtension(c.ObjectDefinition) || HasMarkupExtensionNeedingComponent(c.ObjectDefinition)) ?? false;
						if ((!_isTopLevelDictionary || isInsideFrameworkTemplate) && (needsBindingUpdates || childrenNeedBindingUpdates))
						{
							var xamlObjectDef = new XamlObjectDefinition(_elementStubXamlType, definition);
							xamlObjectDef.Members.AddRange(members);

							var componentName = AddComponentForParentScope(xamlObjectDef).MemberName;
							innerWriter.AppendLineIndented($"__that.{componentName} = {innerWriter.AppliedParameterName};");

							if (!isInsideFrameworkTemplate)
							{
								EnsureXClassName();

								innerWriter.AppendLineIndented($"var {componentName}_update_That = ({CurrentResourceOwnerName} as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference;");

								using (innerWriter.BlockInvariant($"void {componentName}_update(global::Microsoft.UI.Xaml.ElementStub sender)"))
								{
									using (innerWriter.BlockInvariant($"if ({componentName}_update_That.Target is {_xClassName} that)"))
									{

										using (innerWriter.BlockInvariant($"if (sender.IsMaterialized)"))
										{
											innerWriter.AppendLineIndented($"that.Bindings.UpdateResources();");
											if (nameMember?.Value is string xName)
											{
												innerWriter.AppendLineIndented($"that.Bindings.NotifyXLoad(\"{xName}\");");
											}
										}

										using (innerWriter.BlockInvariant("else"))
										{
											var elementNames = FindNamesIn(definition);
											foreach (var elementName in elementNames)
											{
												innerWriter.AppendLineIndented($"that._{elementName}Subject.ElementInstance = null;");
											}

											if (CurrentXLoadScope is not null)
											{
												BuildxBindEventHandlerUnInitializers(innerWriter, CurrentXLoadScope.xBindEventsHandlers, "that.");
											}
										}
									}
								}

								innerWriter.AppendLineIndented($"{innerWriter.AppliedParameterName}.MaterializationChanged += {componentName}_update;");

								innerWriter.AppendLineIndented($"var owner = this;");

								if (_isHotReloadEnabled)
								{
									// Attach the current context to itself to avoid having a closure in the lambda
									innerWriter.AppendLineIndented($"global::Uno.UI.Helpers.MarkupHelper.SetElementProperty({innerWriter.AppliedParameterName}, \"{componentName}_owner\", owner);");
								}

								using (innerWriter.BlockInvariant($"void {componentName}_materializing(object sender)"))
								{
									if (_isHotReloadEnabled)
									{
										innerWriter.AppendLineIndented($"var owner = global::Uno.UI.Helpers.MarkupHelper.GetElementProperty<{CurrentScope.ClassName}>(sender, \"{componentName}_owner\");");
									}

									// Refresh the bindings when the ElementStub is unloaded. This assumes that
									// ElementStub will be unloaded **after** the stubbed control has been created
									// in order for the component field to be filled, and Bindings.Update() to do its work.

									using (innerWriter.BlockInvariant($"if ({componentName}_update_That.Target is {_xClassName} that)"))
									{
										if (CurrentXLoadScope != null)
										{
											foreach (var component in CurrentXLoadScope.Components)
											{
												innerWriter.AppendLineIndented($"that.{component.VariableName}.ApplyXBind();");
												innerWriter.AppendLineIndented($"that.{component.VariableName}.UpdateResourceBindings();");
											}

											BuildxBindEventHandlerInitializers(innerWriter, CurrentXLoadScope.xBindEventsHandlers, "that.");
										}
									}
								}

								innerWriter.AppendLineIndented($"{innerWriter.AppliedParameterName}.Materializing += {componentName}_materializing;");
							}
							else
							{
								// TODO for https://github.com/unoplatform/uno/issues/6700
							}
						}

						XamlMemberDefinition GenerateBinding(string name, XamlMemberDefinition? memberDefinition, XamlObjectDefinition owner)
						{
							var def = new XamlMemberDefinition(
								new XamlMember(name,
									_elementStubXamlType,
									false
								), 0, 0,
								owner
							);

							if (memberDefinition != null)
							{
								def.Objects.AddRange(memberDefinition.Objects);
							}

							BuildComplexPropertyValue(innerWriter, def, innerWriter.AppliedParameterName + ".", innerWriter.AppliedParameterName);

							return def;
						}
					}

					xLoadScopeDisposable?.Dispose();
				});
			}

			return null;
		}


		/// <summary>
		/// Set the active DefaultBindMode, if x:DefaultBindMode is defined on this <paramref name="xamlObjectDefinition"/>.
		/// </summary>
		private IDisposable? TrySetDefaultBindMode(XamlObjectDefinition xamlObjectDefinition, string? ambientDefaultBindMode = null)
		{
			var definedMode = xamlObjectDefinition
				.Members
				.FirstOrDefault(m => m.Member.Name == "DefaultBindMode")?.Value?.ToString()
				?? ambientDefaultBindMode;

			if (definedMode == null)
			{
				return null;
			}
			else if (!IsValid(definedMode))
			{
				throw new XamlGenerationException("Invalid value specified for attribute 'DefaultBindMode', accepted values are 'OneWay', 'OneTime', or 'TwoWay'", xamlObjectDefinition);
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
				writer.AppendLineIndented("global::Microsoft.UI.Xaml.Media.VisualTreeHelper.AdaptNative(");
				return new DisposableAction(() => writer.AppendIndented(")"));
			}

			return null;
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
					case "Microsoft.UI.Xaml.Media.ImageSource":
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
						throw new XamlGenerationException($"Initializer value for '{xamlObjectDefinition.Type.Name}' cannot be empty", xamlObjectDefinition);
					}

					writer.AppendLineInvariantIndented("{0}", GetFloatingPointLiteral(initializer.Value.ToString() ?? "", GetType(xamlObjectDefinition.Type), owner));
				}
				else if (xamlObjectDefinition.Type.Name == "Boolean")
				{
					if (initializer.Value == null)
					{
						throw new XamlGenerationException($"Initializer value for '{xamlObjectDefinition.Type.Name}' cannot be empty", xamlObjectDefinition);
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
			_scopeStack.Push(new NameScope(ImmutableStack.Create(_scopeStack.ToArray()), _fileUniqueId, @namespace ?? string.Empty, className));

			return new DisposableAction(() => _scopeStack.Pop());
		}

		private ComponentDefinition GetOrAddComponentForCurrentScope(XamlObjectDefinition objectDefinition)
		{
			if (CurrentScope.Components.FirstOrDefault(x => x.XamlObject == objectDefinition) is not { } component)
			{
				component = AddComponentForCurrentScope(objectDefinition);
			}

			return component;
		}

		private ComponentDefinition AddComponentForCurrentScope(XamlObjectDefinition objectDefinition)
		{
			// Prefix with the run ID so that we can leverage the removal of fields/properties during HR sessions
			var componentDefinition = new ComponentDefinition(
				objectDefinition,
				IsWeakReference: true,
				$"_component_{CurrentScope.ComponentCount}");
			CurrentScope.Components.Add(componentDefinition);

			if (CurrentXLoadScope is { } xLoadScope)
			{
				CurrentXLoadScope.Components.Add(new ComponentEntry(componentDefinition.MemberName, objectDefinition));
			}

			return componentDefinition;
		}

		private ComponentDefinition AddComponentForParentScope(XamlObjectDefinition objectDefinition)
		{
			// Add the element stub as a strong reference, so that the
			// stub can be brought back if the loaded state changes.
			var component = new ComponentDefinition(
				objectDefinition,
				IsWeakReference: false,
				$"_component_{CurrentScope.ComponentCount}");
			CurrentScope.Components.Add(component);

			if (_xLoadScopeStack.Count > 1)
			{
				var parent = _xLoadScopeStack.Skip(1).First();

				parent.Components.Add(new ComponentEntry(CurrentScope.Components.Last().MemberName, objectDefinition));
			}

			return component;
		}

		private void RegisterXBindEventInitializer(string methodName, Action<string, IIndentedStringBuilder> build)
		{
			var definition = new XBindEventInitializerDefinition(methodName, build);

			CurrentScope.xBindEventsHandlers.Add(definition);
			CurrentScope.RegisterMethod(methodName, build);

			if (CurrentXLoadScope is { } xLoadScope)
			{
				xLoadScope.xBindEventsHandlers.Add(definition);
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
		private IIndentedStringBuilder TryAnnotateWithGeneratorSource(IIndentedStringBuilder writer, string? suffix = null, [CallerMemberName] string? callerName = null, [CallerLineNumber] int lineNumber = 0)
		{
			if (_shouldAnnotateGeneratedXaml)
			{
				writer.Append(GetGeneratorSourceAnnotation(callerName, lineNumber, suffix));
			}

			return writer;
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

		private IDisposable LogicalScope(XamlObjectDefinition o)
		{
			_logicalScopeStack.Push(new(o));

			return new DisposableAction(() => _logicalScopeStack.Pop());
		}
	}
}
