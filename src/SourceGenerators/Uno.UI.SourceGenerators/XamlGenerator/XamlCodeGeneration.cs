extern alias __uno;
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Uno.Roslyn;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.DevTools.Telemetry;
using Uno.UI.Xaml;
using System.Drawing;
using __uno::Uno.Xaml;
using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Uno.UI.SourceGenerators.Helpers;
using Uno.UI.SourceGenerators.Utils;
using Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators;
using Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators.CommunityToolkitMvvm;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record struct XamlSource(Uno.Roslyn.MSBuildItem Item, string Link);

	internal partial class XamlCodeGeneration
	{
		internal const string ParseContextPropertyName = "__ParseContext_";
		internal const string ParseContextPropertyType = "global::Uno.UI.Xaml.XamlParseContext";
		internal const string ParseContextGetterMethod = "GetParseContext";

		private static readonly char[] _commaArray = new[] { ',' };

		private readonly XamlSource[] _xamlSources;
		private readonly string _targetPath;
		private readonly string _defaultLanguage;
		private readonly bool _isWasm;
		private readonly bool _isDesignTimeBuild;
		private readonly string _defaultNamespace;
		private readonly string _excludeXamlNamespaces;
		private readonly string _includeXamlNamespaces;
		private readonly string[] _analyzerSuppressions;
		private readonly Uno.Roslyn.MSBuildItem[] _resourceFiles;
		private readonly Dictionary<string, string[]> _uiAutomationMappings;
		private readonly string _configuration;
		private readonly bool _isDebug;

		/// <summary>
		/// Should hot reload-related calls be generated? By default this is true iff building in debug, but it can be forced to always true or false using the "UnoForceHotReloadCodeGen" project flag.
		/// </summary>
		private readonly bool _isHotReloadEnabled;
		private readonly bool _generateXamlSourcesProvider;
		private readonly string _projectDirectory;
		private readonly string _projectFullPath;
		private readonly bool _xamlResourcesTrimming;
		private readonly bool _isUnoHead;
		private readonly bool _shouldWriteErrorOnInvalidXaml;
		private readonly RoslynMetadataHelper _metadataHelper;

		/// <summary>
		/// If set, code generated from XAML will be annotated with the source method and line # in XamlFileGenerator, for easier debugging.
		/// </summary>
		private readonly bool _shouldAnnotateGeneratedXaml;

		/// <summary>
		/// When set, Visual State Manager children will be initialized lazily for performance
		/// </summary>
		private readonly bool _isLazyVisualStateManagerEnabled = true;

		private readonly bool _isUiAutomationMappingEnabled;

		/// <summary>
		/// Exists for compatibility only. This option should be removed in Uno 6 and fuzzy matching should be disabled.
		/// </summary>
		private readonly bool _enableFuzzyMatching;

		/// <summary>
		/// Disables support for bindable type providers
		/// </summary>
		private readonly bool _disableBindableTypeProvidersGeneration;

		private readonly GeneratorExecutionContext _generatorContext;

		private bool IsUnoAssembly
			=> _defaultNamespace == "Uno.UI";

		private bool IsUnoFluentAssembly
			=> _defaultNamespace == "Uno.UI.FluentTheme" || _defaultNamespace.StartsWith("Uno.UI.FluentTheme.v", StringComparison.Ordinal);

		private const string WinUIThemeResourcePathSuffixFormatString = "themeresources_v{0}.xaml";
		private static string WinUICompactPathSuffix = Path.Combine("DensityStyles", "Compact.xaml");

		internal Lazy<INamedTypeSymbol?> AssemblyMetadataSymbol { get; }
		internal Lazy<INamedTypeSymbol> ElementStubSymbol { get; }
		internal Lazy<INamedTypeSymbol> ContentControlSymbol { get; }
		internal Lazy<INamedTypeSymbol> ContentPresenterSymbol { get; }
		internal Lazy<INamedTypeSymbol> StringSymbol { get; }
		internal Lazy<INamedTypeSymbol> ObjectSymbol { get; }
		internal Lazy<INamedTypeSymbol> FrameworkElementSymbol { get; }
		internal Lazy<INamedTypeSymbol> UIElementSymbol { get; }
		internal Lazy<INamedTypeSymbol> DependencyObjectSymbol { get; }
		internal Lazy<INamedTypeSymbol> MarkupExtensionSymbol { get; }
		internal Lazy<INamedTypeSymbol> BrushSymbol { get; }
		internal Lazy<INamedTypeSymbol> ImageSourceSymbol { get; }
		internal Lazy<INamedTypeSymbol> ImageSymbol { get; }
		internal Lazy<INamedTypeSymbol> DependencyObjectParseSymbol { get; }
		internal Lazy<INamedTypeSymbol?> AndroidContentContextSymbol { get; }
		internal Lazy<INamedTypeSymbol?> AndroidViewSymbol { get; }
		internal Lazy<INamedTypeSymbol?> IOSViewSymbol { get; }
		internal Lazy<INamedTypeSymbol?> AppKitViewSymbol { get; }
		internal Lazy<INamedTypeSymbol> ICollectionSymbol { get; }
		internal Lazy<INamedTypeSymbol> ICollectionOfTSymbol { get; }
		internal Lazy<INamedTypeSymbol> IConvertibleSymbol { get; }
		internal Lazy<INamedTypeSymbol> IListSymbol { get; }
		internal Lazy<INamedTypeSymbol> IListOfTSymbol { get; }
		internal Lazy<INamedTypeSymbol> IDictionaryOfTKeySymbol { get; }
		internal Lazy<INamedTypeSymbol> DataBindingSymbol { get; }
		internal Lazy<INamedTypeSymbol> StyleSymbol { get; }
		internal Lazy<INamedTypeSymbol> SetterSymbol { get; }
		internal Lazy<INamedTypeSymbol> ColorSymbol { get; }
		internal Lazy<INamedTypeSymbol> ColorsSymbol { get; }
		internal Lazy<INamedTypeSymbol> FontWeightsSymbol { get; }
		internal Lazy<INamedTypeSymbol> SolidColorBrushHelperSymbol { get; }
		internal Lazy<INamedTypeSymbol> CreateFromStringAttributeSymbol { get; }
		internal Lazy<INamedTypeSymbol?> NativePageSymbol { get; }
		internal Lazy<INamedTypeSymbol?> WindowSymbol { get; }
		internal Lazy<INamedTypeSymbol> ApplicationSymbol { get; }
		internal Lazy<INamedTypeSymbol> ResourceDictionarySymbol { get; }
		internal Lazy<INamedTypeSymbol> TextBlockSymbol { get; }
		internal Lazy<INamedTypeSymbol> RunSymbol { get; }
		internal Lazy<INamedTypeSymbol> SpanSymbol { get; }
		internal Lazy<INamedTypeSymbol> BorderSymbol { get; }
		internal Lazy<INamedTypeSymbol> SolidColorBrushSymbol { get; }
		internal Lazy<INamedTypeSymbol> RowDefinitionSymbol { get; }
		internal Lazy<INamedTypeSymbol> ColumnDefinitionSymbol { get; }
		internal Lazy<INamedTypeSymbol> TaskSymbol { get; }


		internal ImmutableArray<ITypeProvider> TypeProviders { get; }

		public XamlCodeGeneration(GeneratorExecutionContext context)
		{
			// To easily debug XAML code generation:
			// Add <UnoUISourceGeneratorDebuggerBreak>True</UnoUISourceGeneratorDebuggerBreak> to your project

			if (!Helpers.DesignTimeHelper.IsDesignTime(context)
				&& (context.GetMSBuildPropertyValue("UnoUISourceGeneratorDebuggerBreak")?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false))
			{
				Debugger.Launch();
			}

			_generatorContext = context;
			InitTelemetry(context);

			_metadataHelper = new RoslynMetadataHelper(context);

			_configuration = context.GetMSBuildPropertyValue("Configuration")
				?? throw new InvalidOperationException("The configuration property must be provided");

			_isDebug = string.Equals(_configuration, "Debug", StringComparison.OrdinalIgnoreCase);

			_projectFullPath = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
			_projectDirectory = Path.GetDirectoryName(_projectFullPath)
				?? throw new InvalidOperationException($"MSBuild property MSBuildProjectFullPath value {_projectFullPath} is not valid");

			var pageItems = GetWinUIItems("Page").Concat(GetWinUIItems("UnoPage"));
			var applicationDefinitionItems = GetWinUIItems("ApplicationDefinition").Concat(GetWinUIItems("UnoApplicationDefinition"));

			_xamlSources = pageItems
				.Except(applicationDefinitionItems, MSBuildItem.IdentityComparer)
				.Concat(applicationDefinitionItems)
				.Distinct(MSBuildItem.IdentityComparer)
				.Select(item => new XamlSource(item, GetSourceLink(item)))
				.ToArray();

			_excludeXamlNamespaces = context.GetMSBuildPropertyValue("ExcludeXamlNamespacesProperty");

			_includeXamlNamespaces = context.GetMSBuildPropertyValue("IncludeXamlNamespacesProperty");

			_analyzerSuppressions = context.GetMSBuildPropertyValue("XamlGeneratorAnalyzerSuppressionsProperty").Split(_commaArray, StringSplitOptions.RemoveEmptyEntries);

			_resourceFiles = context.GetMSBuildItemsWithAdditionalFiles("PRIResource").ToArray();

			if (!bool.TryParse(context.GetMSBuildPropertyValue("ShouldWriteErrorOnInvalidXaml"), out _shouldWriteErrorOnInvalidXaml))
			{
				_shouldWriteErrorOnInvalidXaml = true;
			}

			if (!bool.TryParse(context.GetMSBuildPropertyValue("IsUiAutomationMappingEnabled") ?? "", out _isUiAutomationMappingEnabled))
			{
				_isUiAutomationMappingEnabled = false;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("ShouldAnnotateGeneratedXaml"), out var shouldAnnotateGeneratedXaml))
			{
				_shouldAnnotateGeneratedXaml = shouldAnnotateGeneratedXaml;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoXamlLazyVisualStateManagerEnabled"), out var isLazyVisualStateManagerEnabled))
			{
				_isLazyVisualStateManagerEnabled = isLazyVisualStateManagerEnabled;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoXamlResourcesTrimming"), out var xamlResourcesTrimming))
			{
				_xamlResourcesTrimming = xamlResourcesTrimming;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("IsUnoHead"), out var isUnoHead))
			{
				_isUnoHead = isUnoHead;
			}

			if (bool.TryParse(context.GetMSBuildPropertyValue("UnoForceHotReloadCodeGen"), out var isHotReloadEnabled))
			{
				_isHotReloadEnabled = isHotReloadEnabled;
			}
			else
			{
				_isHotReloadEnabled = _isDebug;
			}

			if (!bool.TryParse(context.GetMSBuildPropertyValue("UnoGenerateXamlSourcesProvider"), out _generateXamlSourcesProvider))
			{
				_generateXamlSourcesProvider = _isHotReloadEnabled; // Default to the presence of Hot Reload feature
			}

			if (!bool.TryParse(context.GetMSBuildPropertyValue("UnoEnableXamlFuzzyMatching"), out _enableFuzzyMatching))
			{
				_enableFuzzyMatching = false;
			}

			if (!bool.TryParse(context.GetMSBuildPropertyValue("UnoDisableBindableTypeProvidersGeneration"), out _disableBindableTypeProvidersGeneration))
			{
				_disableBindableTypeProvidersGeneration = false;
			}

			_targetPath = Path.Combine(
				_projectDirectory,
				context.GetMSBuildPropertyValue("IntermediateOutputPath")
			);

			_defaultLanguage = context.GetMSBuildPropertyValue("DefaultLanguage");

			_uiAutomationMappings = context.GetMSBuildItemsWithAdditionalFiles("CustomUiAutomationMemberMappingAdjusted")
				.Select(i => new
				{
					Key = i.Identity,
					Value = i.GetMetadataValue("Mappings")
						?.Split(_commaArray, StringSplitOptions.RemoveEmptyEntries)
						.Select(m => m.Trim())
						.Where(m => !m.IsNullOrWhiteSpace())
				})
				.GroupBy(p => p.Key)
				.ToDictionary(p => p.Key, p => p.SelectMany(x => x.Value.Safe()).ToArray());

			_defaultNamespace = context.GetMSBuildPropertyValue("RootNamespace");

			_isWasm = context.GetMSBuildPropertyValue("DefineConstantsProperty")?.Contains("__WASM__") ?? false;
			_isDesignTimeBuild = Helpers.DesignTimeHelper.IsDesignTime(context);

			StringSymbol = GetSpecialTypeSymbolAsLazy(SpecialType.System_String);
			ObjectSymbol = GetSpecialTypeSymbolAsLazy(SpecialType.System_Object);
			AssemblyMetadataSymbol = GetOptionalSymbolAsLazy("System.Reflection.AssemblyMetadataAttribute");
			ElementStubSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.ElementStub);
			SetterSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Setter);
			ContentControlSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.ContentControl);
			ContentPresenterSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.ContentPresenter);
			FrameworkElementSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.FrameworkElement);
			UIElementSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.UIElement);
			ImageSourceSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.ImageSource);
			ImageSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Image);
			DependencyObjectSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.DependencyObject);
			MarkupExtensionSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.MarkupExtension);
			BrushSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Brush);
			DependencyObjectParseSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.IDependencyObjectParse);
			ICollectionSymbol = GetMandatorySymbolAsLazy("System.Collections.ICollection");
			ICollectionOfTSymbol = GetSpecialTypeSymbolAsLazy(SpecialType.System_Collections_Generic_ICollection_T);
			IConvertibleSymbol = GetMandatorySymbolAsLazy("System.IConvertible");
			IListSymbol = GetMandatorySymbolAsLazy("System.Collections.IList");
			IListOfTSymbol = GetSpecialTypeSymbolAsLazy(SpecialType.System_Collections_Generic_IList_T);
			IDictionaryOfTKeySymbol = GetMandatorySymbolAsLazy("System.Collections.Generic.IDictionary`2");
			DataBindingSymbol = GetMandatorySymbolAsLazy("Microsoft.UI.Xaml.Data.Binding");
			StyleSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Style);
			ColorSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Color);
			ColorsSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Colors);
			FontWeightsSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.FontWeights);
			SolidColorBrushHelperSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.SolidColorBrushHelper);
			AndroidContentContextSymbol = GetOptionalSymbolAsLazy("Android.Content.Context");
			AndroidViewSymbol = GetOptionalSymbolAsLazy("Android.Views.View");
			IOSViewSymbol = GetOptionalSymbolAsLazy("UIKit.UIView");
			AppKitViewSymbol = GetOptionalSymbolAsLazy("AppKit.NSView");
			CreateFromStringAttributeSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.CreateFromStringAttribute);
			NativePageSymbol = GetOptionalSymbolAsLazy(XamlConstants.Types.NativePage);
			WindowSymbol = GetOptionalSymbolAsLazy(XamlConstants.Types.Window);
			ApplicationSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Application);
			ResourceDictionarySymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.ResourceDictionary);
			TextBlockSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.TextBlock);
			RunSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Run);
			SpanSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Span);
			BorderSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.Border);
			SolidColorBrushSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.SolidColorBrush);
			RowDefinitionSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.RowDefinition);
			ColumnDefinitionSymbol = GetMandatorySymbolAsLazy(XamlConstants.Types.ColumnDefinition);
			TaskSymbol = GetMandatorySymbolAsLazy("System.Threading.Tasks");

			TypeProviders = ImmutableArray.Create<ITypeProvider>(
				new MvvmTypeProvider(this)
				);
		}

		private Lazy<INamedTypeSymbol> GetMandatorySymbolAsLazy(string fullyQualifiedName)
			=> new(() => _generatorContext.Compilation.GetTypeByMetadataName(fullyQualifiedName) ?? throw new InvalidOperationException($"Unable to find type {fullyQualifiedName}"));

		internal Lazy<INamedTypeSymbol?> GetOptionalSymbolAsLazy(string fullyQualifiedName)
			=> new(() => _generatorContext.Compilation.GetTypeByMetadataName(fullyQualifiedName));

		private Lazy<INamedTypeSymbol> GetSpecialTypeSymbolAsLazy(SpecialType specialType)
			=> new(() => _generatorContext.Compilation.GetSpecialType(specialType));

		private static bool IsWinUIItem(Uno.Roslyn.MSBuildItem item)
			=> item.GetMetadataValue("XamlRuntime") is { } xamlRuntime
				? xamlRuntime == "WinUI" || string.IsNullOrWhiteSpace(xamlRuntime)
				: true;

		private IEnumerable<Uno.Roslyn.MSBuildItem> GetWinUIItems(string name)
			=> _generatorContext.GetMSBuildItemsWithAdditionalFiles(name).Where(IsWinUIItem);

		/// <summary>
		/// Get the file location as seen in the IDE, used for ResourceDictionary.Source resolution.
		/// </summary>
		private string GetSourceLink(Uno.Roslyn.MSBuildItem projectItemInstance)
		{
			var link = projectItemInstance.GetMetadataValue("Link");
			var definingProjectFullPath = projectItemInstance.GetMetadataValue("DefiningProjectFullPath");
			var fullPath = projectItemInstance.GetMetadataValue("FullPath");

			// Reproduce the logic from https://github.com/dotnet/msbuild/blob/e70a3159d64f9ed6ec3b60253ef863fa883a99b1/src/Tasks/AssignLinkMetadata.cs
			if (link.IsNullOrEmpty())
			{
				if (definingProjectFullPath.IsNullOrEmpty())
				{
					// Both Uno.SourceGeneration uses relative paths and Roslyn Generators provide
					// full paths. Dependents need specific portions so adjust the paths here for now.
					// For the case of Roslyn generators, DefiningProjectFullPath is not populated on purpose
					// so that we can adjust paths properly.
					if (link.IsNullOrEmpty())
					{
						return Path.IsPathRooted(projectItemInstance.Identity)
							? projectItemInstance.Identity.TrimStart(_projectDirectory).TrimStart(Path.DirectorySeparatorChar)
							: projectItemInstance.Identity;
					}
				}
				else
				{
					var definingProjectDirectory = Path.GetDirectoryName(definingProjectFullPath) + Path.DirectorySeparatorChar;

					if (fullPath.StartsWith(definingProjectDirectory, StringComparison.OrdinalIgnoreCase))
					{
						link = fullPath.Substring(definingProjectDirectory.Length);
					}
					else
					{
						link = projectItemInstance.GetMetadataValue("Identity");
					}
				}
			}

			return link;
		}

		public List<KeyValuePair<string, SourceText>> Generate()
		{
			var stopwatch = Stopwatch.StartNew();
			var ct = _generatorContext.CancellationToken;

			try
			{
				var isInsideMainAssembly = _isUnoHead || PlatformHelper.IsAndroid(_generatorContext);

				var resourceDetailsCollection = BuildResourceDetails(ct);
				TryGenerateUnoResourcesKeyAttribute(resourceDetailsCollection);

				var excludeXamlNamespaces = _excludeXamlNamespaces.Split(_commaArray, StringSplitOptions.RemoveEmptyEntries);
				var includeXamlNamespaces = _includeXamlNamespaces.Split(_commaArray, StringSplitOptions.RemoveEmptyEntries);

				// Parse XAML files
				var xamlParser = new XamlFileParser(_excludeXamlNamespaces, _includeXamlNamespaces, excludeXamlNamespaces, includeXamlNamespaces, _metadataHelper);
				var xamlFiles = xamlParser
					.ParseFiles(_xamlSources, _projectDirectory, ct)
					.OrderBy(file => file.UniqueID)
					.ToArray();

				// Build a map of XamlType (from the top-level control) per class symbol
				var xamlTypeToXamlTypeBaseMap = new ConcurrentDictionary<INamedTypeSymbol, XamlRedirection.XamlType>(SymbolEqualityComparer.Default);
				xamlFiles
					.AsParallel()
					.WithCancellation(ct)
					.ForAll(file =>
					{
						var topLevelControl = file.Objects.FirstOrDefault();
						if (topLevelControl is null)
						{
							return;
						}

						var xClassSymbol = XamlFileGenerator.FindClassSymbol(topLevelControl, _metadataHelper);
						if (xClassSymbol is not null)
						{
							xamlTypeToXamlTypeBaseMap.TryAdd(xClassSymbol, topLevelControl.Type);
						}
					});

				// Build global static resources map used to map ResourceDictionary.Source to the proper property
				var globalStaticResourcesMap = new XamlGlobalStaticResourcesMap(xamlFiles);
				var ambientGlobalResources = BuildAmbientResources();

				// Finally start generation
				TrackStartGeneration(xamlFiles);

				var csharpFiles = (Debugger.IsAttached
						? xamlFiles.AsParallel().WithDegreeOfParallelism(1)
						: xamlFiles.AsParallel())
					.WithCancellation(ct)
					.Select<XamlFileDefinition, (XamlFileDefinition definition, SourceText? code, Exception? error)>(file =>
					{
						var generator = new XamlFileGenerator(
							this,
							file: file,
							targetPath: _targetPath,
							defaultNamespace: _defaultNamespace,
							metadataHelper: _metadataHelper,
							fileUniqueId: file.UniqueID,
							analyzerSuppressions: _analyzerSuppressions,
							globalStaticResourcesMap: globalStaticResourcesMap,
							resourceDetailsCollection: resourceDetailsCollection,
							isUiAutomationMappingEnabled: _isUiAutomationMappingEnabled,
							uiAutomationMappings: _uiAutomationMappings,
							defaultLanguage: _defaultLanguage,
							shouldWriteErrorOnInvalidXaml: _shouldWriteErrorOnInvalidXaml,
							isWasm: _isWasm,
							isHotReloadEnabled: _isHotReloadEnabled,
							isInsideMainAssembly: isInsideMainAssembly,
							isDesignTimeBuild: _isDesignTimeBuild,
							shouldAnnotateGeneratedXaml: _shouldAnnotateGeneratedXaml,
							isUnoAssembly: IsUnoAssembly,
							isUnoFluentAssembly: IsUnoFluentAssembly,
							isLazyVisualStateManagerEnabled: _isLazyVisualStateManagerEnabled,
							enableFuzzyMatching: _enableFuzzyMatching,
							disableBindableTypeProvidersGeneration: _disableBindableTypeProvidersGeneration,
							generatorContext: _generatorContext,
							xamlResourcesTrimming: _xamlResourcesTrimming,
							xamlTypeToXamlTypeBaseMap: xamlTypeToXamlTypeBaseMap,
							includeXamlNamespaces: includeXamlNamespaces
						);

						try
						{
							var csharpCode = generator.GenerateFile();
							return new(file, csharpCode, null);
						}
						catch (Exception error)
						{
							return new(file, null, error);
						}
					})
					.ToArray();

				var outputFiles = new List<KeyValuePair<string, SourceText>>();
				foreach (var csharpFile in csharpFiles)
				{
					// Note: We process parsing exception here in order to have it grouped with any other exception thrown during generation for this file.
					if (csharpFile.definition.ParsingError is {} parseError)
					{
						ProcessParsingException(parseError);
					}
					if (csharpFile.error is { } genError)
					{
						ProcessParsingException(genError);
					}
					if (csharpFile.code is { } csharp)
					{
						outputFiles.Add(new(csharpFile.definition.UniqueID, csharp));
					}
				}

				try
				{
					if (_generateXamlSourcesProvider && GenerateEmbeddedXamlSources(xamlFiles) is { } embeddedXamlSources)
					{
						outputFiles.Add(new("EmbeddedXamlSources", embeddedXamlSources));
					}
				}
				catch (Exception error)
				{
					ProcessParsingException(error);
				}

				try
				{
					outputFiles.Add(new("GlobalStaticResources", GenerateGlobalResources(xamlFiles, globalStaticResourcesMap, ambientGlobalResources)));
				}
				catch (Exception error)
				{
					ProcessParsingException(error);
				}

				TrackGenerationDone(stopwatch.Elapsed);

				return outputFiles.ToList();
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				TrackGenerationFailed(e, stopwatch.Elapsed);
				ProcessParsingException(e);

				return [];
			}
			finally
			{
				_telemetry.Flush();
				_telemetry.Dispose();
			}
		}

		private void TryGenerateUnoResourcesKeyAttribute(ResourceDetailsCollection resourceDetailsCollection)
		{
			var hasResources = resourceDetailsCollection.HasLocalResources;

			_generatorContext.AddSource(
				"LocalizationResources",
				$"""
				// <auto-generated />
				[assembly: global::System.Reflection.AssemblyMetadata("UnoHasLocalizationResources", "{hasResources.ToString(CultureInfo.InvariantCulture)}")]
				""");
		}

		private void ProcessParsingException(Exception e)
		{
			IEnumerable<Exception> Flatten(Exception ex)
			{
				if (ex is AggregateException agg)
				{
					foreach (var inner in agg.InnerExceptions)
					{
						foreach (var inner2 in Flatten(inner))
						{
							yield return inner2;
						}
					}
				}
				else
				{
					if (ex.InnerException != null)
					{
						foreach (var inner2 in Flatten(ex.InnerException))
						{
							yield return inner2;
						}
					}

					yield return ex;
				}
			}

			foreach (var exception in Flatten(e))
			{
				var diagnostic = Diagnostic.Create(
					XamlCodeGenerationDiagnostics.GenericXamlErrorRule,
					GetExceptionFileLocation(exception),
					exception.Message);

				_generatorContext.ReportDiagnostic(diagnostic);
			}
		}

		private Location? GetExceptionFileLocation(Exception exception)
		{
			if (exception is XamlParsingException xamlParsingException)
			{
				var xamlFile = _generatorContext.AdditionalFiles.FirstOrDefault(f => f.Path == xamlParsingException.FilePath);

				if (xamlFile != null
					&& xamlFile.GetText() is { } xamlText
					&& xamlParsingException.LineNumber.HasValue
					&& xamlParsingException.LinePosition.HasValue)
				{
					var linePosition = new LinePosition(
						Math.Max(0, xamlParsingException.LineNumber.Value - 1),
						Math.Max(0, xamlParsingException.LinePosition.Value - 1)
					);

					return Location.Create(
						xamlFile.Path,
						xamlText.Lines.ElementAtOrDefault(xamlParsingException.LineNumber.Value - 1).Span,
						new LinePositionSpan(linePosition, linePosition)
					);
				}
			}

			return null;
		}

		private INamedTypeSymbol[] BuildAmbientResources()
		{
			// Lookup for GlobalStaticResources classes in external assembly
			// references only, and in Uno.UI itself for generic.xaml-like resources.

			var assembliesQuery =
				from ext in _metadataHelper.Compilation.ExternalReferences
				let sym = _metadataHelper.Compilation.GetAssemblyOrModuleSymbol(ext) as IAssemblySymbol
				where sym != null
				select sym;

			var query = from sym in assembliesQuery
						from module in sym.Modules

							// Only consider assemblies that reference Uno.UI
						where module.ReferencedAssemblies.Any(r => r.Name == "Uno.UI") || sym.Name == "Uno.UI"

						// Don't consider Uno.UI.FluentTheme assemblies, as they manage their own initialization
						where sym.Name != "Uno.UI.FluentTheme" && !sym.Name.StartsWith("Uno.UI.FluentTheme.v", StringComparison.InvariantCulture)

						from typeName in sym.GlobalNamespace.GetNamespaceTypes()
						where typeName.Name.EndsWith("GlobalStaticResources", StringComparison.Ordinal)
						select typeName;

			return query.Distinct(SymbolEqualityComparer.Default).Cast<INamedTypeSymbol>().ToArray();
		}

		// Get keys of localized strings
		private ResourceDetailsCollection BuildResourceDetails(CancellationToken ct)
		{
			var localResources = BuildLocalResourceDetails(ct);

			var collection = new ResourceDetailsCollection(_metadataHelper.AssemblyName);

			collection.AddRange(localResources);

			return collection;
		}

		private ResourceDetails[] BuildLocalResourceDetails(CancellationToken ct)
		{
			var resourceKeys = _resourceFiles
				.AsParallel()
				.WithCancellation(ct)
				.SelectMany(file =>
				{
					try
					{
						var sourceText = file.File.GetText(ct)!;
						var cachedFileKey = new ResourceCacheKey(file.Identity, sourceText.GetChecksum());
						var resourceFileName = Path.GetFileNameWithoutExtension(file.Identity);

						if (_cachedResources.TryGetValue(cachedFileKey, out var cachedResource))
						{
							_cachedResources[cachedFileKey] = cachedResource.WithUpdatedLastTimeUsed();
							ScavengeCache();
							return cachedResource.ResourceKeys;
						}

						ScavengeCache();

						//load document
						var doc = new XmlDocument();
						doc.LoadXml(sourceText.ToString());

						//extract all localization keys from Win10 resource file
						// https://docs.microsoft.com/en-us/dotnet/standard/data/xml/compiled-xpath-expressions?redirectedfrom=MSDN#higher-performance-xpath-expressions
						// Per this documentation, /root/data should be more performant than //data
						var keys = doc.SelectNodes("/root/data")
							?.Cast<XmlElement>()
							.Select(node => new ResourceDetails(_metadataHelper.AssemblyName, resourceFileName, node.GetAttribute("name")))
							.ToArray() ?? Array.Empty<ResourceDetails>();
						_cachedResources[cachedFileKey] = new CachedResource(DateTimeOffset.Now, keys);
						return keys;
					}
					catch (Exception e)
					{
						var message = $"Unable to parse resource file [{file.Identity}], make sure it is a valid resw file. ({e.Message})";

						var diagnostic = Diagnostic.Create(
							XamlCodeGenerationDiagnostics.ResourceParsingFailureRule,
							null,
							message);

						_generatorContext.ReportDiagnostic(diagnostic);

						return Array.Empty<ResourceDetails>();
					}
				})
				.Distinct()
				.ToArray();

			return resourceKeys;
		}

		private SourceText GenerateGlobalResources(XamlFileDefinition[] xamlFiles, XamlGlobalStaticResourcesMap map, INamedTypeSymbol[] ambientGlobalResources)
		{
			var writer = new IndentedStringBuilder();

			writer.AppendLineIndented("// <autogenerated />");
			AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);

			using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
			{
				writer.AppendLineIndented("/// <summary>");
				writer.AppendLineIndented("/// Contains all the static resources defined for the application");
				writer.AppendLineIndented("/// </summary>");

				if (_isHotReloadEnabled)
				{
					writer.AppendLineIndented("[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]");
				}

				using (writer.BlockInvariant("public sealed partial class GlobalStaticResources"))
				{
					writer.AppendLineIndented("static bool _initialized;");
					writer.AppendLineIndented("private static bool _stylesRegistered;");
					writer.AppendLineIndented("private static bool _dictionariesRegistered;");

					using (writer.BlockInvariant("internal static {0} {1} {{ get; }} = new {0}()", ParseContextPropertyType, ParseContextPropertyName))
					{
						writer.AppendLineIndented($"AssemblyName = \"{_metadataHelper.AssemblyName}\",");
					}

					writer.AppendLineIndented(";");
					writer.AppendLine();

					using (writer.BlockInvariant("static GlobalStaticResources()"))
					{
						writer.AppendLineIndented("Initialize();");
					}

					using (writer.BlockInvariant("public static void Initialize()"))
					{
						using (writer.BlockInvariant("if (!_initialized)"))
						{
							using (IsUnoAssembly ? writer.BlockInvariant("using (ResourceResolver.WriteInitiateGlobalStaticResourcesEventActivity())") : null)
							{
								writer.AppendLineIndented("_initialized = true;");

								foreach (var ambientResource in ambientGlobalResources)
								{
									if (ambientResource.GetFirstMethodWithName("Initialize") is not null)
									{
										writer.AppendLineIndented($"{ambientResource.GetFullyQualifiedTypeIncludingGlobal()}.Initialize();");
									}
								}

								foreach (var ambientResource in ambientGlobalResources)
								{
									// Note: we do *not* call RegisterDefaultStyles for the current assembly, because those styles are treated as implicit styles, not default styles
									if (ambientResource.GetFirstMethodWithName("RegisterDefaultStyles") is not null)
									{
										writer.AppendLineIndented($"{ambientResource.GetFullyQualifiedTypeIncludingGlobal()}.RegisterDefaultStyles();");
									}
								}

								foreach (var ambientResource in ambientGlobalResources)
								{
									if (ambientResource.GetFirstMethodWithName("RegisterResourceDictionariesBySource") is not null)
									{
										writer.AppendLineIndented($"{ambientResource.GetFullyQualifiedTypeIncludingGlobal()}.RegisterResourceDictionariesBySource();");
									}
								}

								if (IsUnoAssembly && xamlFiles.Length > 0)
								{
									// Build master dictionary
									foreach (var dictProperty in map.GetAllDictionaryProperties())
									{
										writer.AppendLineIndented($"MasterDictionary.MergedDictionaries.Add({dictProperty});");
									}
								}
							}
						}
					}

					using (writer.BlockInvariant("public static void RegisterDefaultStyles()"))
					{
						using (writer.BlockInvariant("if(!_stylesRegistered)"))
						{
							writer.AppendLineIndented("_stylesRegistered = true;");
							foreach (var file in xamlFiles.Select(f => f.UniqueID).Distinct())
							{
								writer.AppendLineIndented($"RegisterDefaultStyles_{file}();");
							}
						}
					}

					writer.AppendLineIndented("// Register ResourceDictionaries using ms-appx:/// syntax, this is called for external resources");
					using (writer.BlockInvariant("public static void RegisterResourceDictionariesBySource()"))
					{
						using (writer.BlockInvariant("if(!_dictionariesRegistered)"))
						{
							writer.AppendLineIndented("_dictionariesRegistered = true;");

							if (!IsUnoAssembly && !IsUnoFluentAssembly)
							{
								// For third-party libraries, expose all files using standard uri
								foreach (var file in xamlFiles.Where(IsResourceDictionary))
								{
									var url = "{0}/{1}".InvariantCultureFormat(_metadataHelper.AssemblyName, map.GetSourceLink(file));
									RegisterForXamlFile(file, url);
								}
							}
							else if (xamlFiles.Any() && IsUnoFluentAssembly)
							{
								// For Uno assembly, we expose WinUI resources using same uri as on Windows
								for (int fluentVersion = 1; fluentVersion <= XamlConstants.MaxFluentResourcesVersion; fluentVersion++)
								{
									RegisterForFile(string.Format(CultureInfo.InvariantCulture, WinUIThemeResourcePathSuffixFormatString, fluentVersion), XamlFilePathHelper.GetWinUIThemeResourceUrl(fluentVersion));
								}
								RegisterForFile(WinUICompactPathSuffix, XamlFilePathHelper.WinUICompactURL);
							}

							void RegisterForFile(string baseFilePath, string url)
							{
								var file = xamlFiles.FirstOrDefault(f =>
									f.FilePath.Substring(_projectDirectory.Length + 1).Equals(baseFilePath, StringComparison.OrdinalIgnoreCase));

								if (file != null)
								{
									RegisterForXamlFile(file, url);
								}
							}

							void RegisterForXamlFile(XamlFileDefinition file, string url)
							{
								if (file != null)
								{
									writer.AppendLineIndented($"global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"ms-appx:///{url}\", context: {ParseContextPropertyName}, dictionary: () => {file.UniqueID}_ResourceDictionary);");
								}
							}
						}
					}

					writer.AppendLineIndented("// Register ResourceDictionaries using ms-resource:/// syntax, this is called for local resources");
					using (writer.BlockInvariant("internal static void RegisterResourceDictionariesBySourceLocal()"))
					{
						foreach (var file in xamlFiles.Where(IsResourceDictionary))
						{
							// Make ResourceDictionary retrievable by Hot Reload
							var filePath = _isDebug ? $"\"{file.FilePath.Replace("\\", "/")}\"" : "null";

							// We leave context null because local resources should be found through Application.Resources
							writer.AppendLineIndented($"global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"{XamlFilePathHelper.LocalResourcePrefix}{map.GetSourceLink(file)}\", context: null, dictionary: () => {file.UniqueID}_ResourceDictionary, {filePath});");
							// Local resources can also be found through the ms-appx:/// prefix
							writer.AppendLineIndented($"global::Uno.UI.ResourceResolver.RegisterResourceDictionaryBySource(uri: \"{XamlFilePathHelper.AppXIdentifier}{map.GetSourceLink(file)}\", context: null, dictionary: () => {file.UniqueID}_ResourceDictionary);");
						}
					}

					if (IsUnoAssembly)
					{
						// Declare master dictionary
						writer.AppendLine();
						writer.AppendLineIndented("internal static global::Microsoft.UI.Xaml.ResourceDictionary MasterDictionary { get; } = new global::Microsoft.UI.Xaml.ResourceDictionary();");
					}

					// Generate all the partial methods, even if they don't exist. That avoids
					// having to sync the generation of the files with this global table.
					foreach (var file in xamlFiles.Select(f => f.UniqueID).Distinct())
					{
						writer.AppendLineIndented($"static partial void RegisterDefaultStyles_{file}();");
					}

					writer.AppendLineIndented("");
				}
			}

			return new StringBuilderBasedSourceText(writer.Builder);
		}

		private static bool IsResourceDictionary(XamlFileDefinition fileDefinition) => fileDefinition.Objects.FirstOrDefault()?.Type.Name == "ResourceDictionary";
	}
}
