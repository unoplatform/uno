using Uno.Extensions;
using Uno.MsBuildTasks.Utils;
using Uno.MsBuildTasks.Utils.XamlPathParser;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Uno;
using Uno.Logging;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal class XamlFileGenerator
	{
		private const string ImplicitStyleMarker = "__ImplicitStyle_";
		private const string GlobalPrefix = "global::";
		private const string QualifiedNamespaceMarker = ".";

		private readonly Dictionary<string, string[]> _knownNamespaces = new Dictionary<string, string[]>
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

		private Dictionary<string, XamlObjectDefinition> _staticResources = new Dictionary<string, XamlObjectDefinition>();
		private Dictionary<string, XamlObjectDefinition> _namedResources = new Dictionary<string, XamlObjectDefinition>();
		private List<string> _partials = new List<string>();
		private Stack<NameScope> _scopeStack = new Stack<NameScope>();
		private readonly XamlFileDefinition _fileDefinition;
		private readonly string _targetPath;
		private readonly string _defaultNamespace;
		private RoslynMetadataHelper _medataHelper;
		private readonly string _fileUniqueId;
		private readonly DateTime _lastReferenceUpdateTime;
		private readonly string[] _analyzerSuppressions;
		private readonly string[] _resourceKeys;
		private readonly bool _outputSourceComments;
		private int _applyIndex = 0;
		private int _collectionIndex = 0;
		private int _subclassIndex = 0;
		private XamlGlobalStaticResourcesMap _globalStaticResourcesMap;
		private readonly bool _isUiAutomationMappingEnabled;
		private readonly Dictionary<string, string[]> _uiAutomationMappings;
		private readonly string _defaultLanguage;
		private readonly string _relativePath;

		private List<INamedTypeSymbol> _xamlAppliedTypes = new List<INamedTypeSymbol>();

		private readonly INamedTypeSymbol _elementStubSymbol;
		private readonly INamedTypeSymbol _contentPresenterSymbol;
		private readonly INamedTypeSymbol _stringSymbol;
		private readonly bool _isWasm;

		// Caches
		private Func<string, INamedTypeSymbol> _findType;
		private Func<string, string, INamedTypeSymbol> _findPropertyTypeByName;
		private Func<XamlMember, INamedTypeSymbol> _findPropertyTypeByXamlMember;
		private Func<XamlMember, IEventSymbol> _findEventType;
		private (string ns, string className) _className;
		private bool _hasLiteralEventsRegistration = false;
		private readonly static Func<INamedTypeSymbol, IPropertySymbol> _findContentProperty;
		private readonly static Func<INamedTypeSymbol, string, bool> _isAttachedProperty;

		static XamlFileGenerator()
		{
			_findContentProperty = Funcs.Create<INamedTypeSymbol, IPropertySymbol>(SourceFindContentProperty).AsLockedMemoized();
			_isAttachedProperty = Funcs.Create<INamedTypeSymbol, string, bool>(SourceIsAttachedProperty).AsLockedMemoized();
		}

		public XamlFileGenerator(
			XamlFileDefinition file,
			string targetPath,
			string defaultNamespace,
			RoslynMetadataHelper medataHelper,
			string fileUniqueId,
			DateTime lastReferenceUpdateTime,
			string[] analyzerSuppressions,
			string[] resourceKeys,
			bool outputSourceComments,
			XamlGlobalStaticResourcesMap globalStaticResourcesMap,
			bool isUiAutomationMappingEnabled,
			Dictionary<string, string[]> uiAutomationMappings,
			string defaultLanguage,
			bool isWasm
		)
		{
			_fileDefinition = file;
			_targetPath = targetPath;
			_defaultNamespace = defaultNamespace;
			_medataHelper = medataHelper;
			_fileUniqueId = fileUniqueId;
			_lastReferenceUpdateTime = lastReferenceUpdateTime;
			_analyzerSuppressions = analyzerSuppressions;
			_resourceKeys = resourceKeys;
			_outputSourceComments = outputSourceComments;
			_globalStaticResourcesMap = globalStaticResourcesMap;
			_isUiAutomationMappingEnabled = isUiAutomationMappingEnabled;
			_uiAutomationMappings = uiAutomationMappings;
			_defaultLanguage = defaultLanguage.HasValue() ? defaultLanguage : "en";

			_findType = Funcs.Create<string, INamedTypeSymbol>(SourceFindType).AsLockedMemoized();
			_findPropertyTypeByXamlMember = Funcs.Create<XamlMember, INamedTypeSymbol>(SourceFindPropertyType).AsLockedMemoized();
			_findEventType = Funcs.Create<XamlMember, IEventSymbol>(SourceFindEventType).AsLockedMemoized();
			_findPropertyTypeByName = Funcs.Create<string, string, INamedTypeSymbol>(SourceFindPropertyType).AsLockedMemoized();

			_relativePath = PathHelper.GetRelativePath(_targetPath, _fileDefinition.FilePath);
			_stringSymbol = GetType("System.String");
			_elementStubSymbol = GetType(XamlConstants.Types.ElementStub);
			_contentPresenterSymbol = GetType(XamlConstants.Types.ContentPresenter);

			_isWasm = isWasm;
		}

		/// <summary>
		/// Indicates if the code generation should write #error in the generated code (and break at compile time) or write a // Warning, which would be silent.
		/// </summary>
		/// <remarks>Initial behavior is to write // Warning, hence the default value to false, but we suggest setting this to true.</remarks>
		public static bool ShouldWriteErrorOnInvalidXaml { get; set; } = false;

		public string GenerateFile()
		{
			this.Log().Info("Processing file {0}".InvariantCultureFormat(_fileDefinition.FilePath));

			var partialFileName = Path.GetFileNameWithoutExtension(_fileDefinition.FilePath);

			// Check for the Roslyn generator's output location
			var outputFile = Path.Combine(
				_targetPath,
				$@"g\{typeof(XamlCodeGenerator).Name}\{_fileDefinition.UniqueID}.g.cs"
				);

			if (File.Exists(outputFile))
			{
				var outputInfo = new FileInfo(outputFile);
				var inputInfo = new FileInfo(_fileDefinition.FilePath);

				if (
					outputInfo.LastWriteTime >= inputInfo.LastWriteTime

					// Check for the references update time. If the file has been generated before the last write time
					// on the references, regenerate the output.
					&& outputInfo.LastWriteTime > _lastReferenceUpdateTime

					// Empty files should be regenerated
					&& outputInfo.Length != 0
				)
				{
					this.Log().Info("Skipping unmodified file {0}".InvariantCultureFormat(_fileDefinition.FilePath));
					return File.ReadAllText(outputFile);
				}
			}

			try
			{
				return InnerGenerateFile();
			}
			catch (Exception)
			{
				this.Log().Error("Processing failed for file {0}".InvariantCultureFormat(_fileDefinition.FilePath));
				throw;
			}
		}

		private string InnerGenerateFile()
		{
			var writer = new IndentedStringBuilder();

			writer.AppendLineInvariant("// <autogenerated />");
			writer.AppendLineInvariant("// Xaml Source Generation is using the {0} Xaml Parser", XamlRedirection.XamlConfig.IsUnoXaml ? "Uno.UI" : "System");
			writer.AppendLineInvariant("#pragma warning disable 618 // Ignore obsolete members warnings");
			writer.AppendLineInvariant("#pragma warning disable 105 // Ignore duplicate namespaces");
			writer.AppendLineInvariant("using System;");
			writer.AppendLineInvariant("using System.Collections.Generic;");
			writer.AppendLineInvariant("using System.Diagnostics;");
			writer.AppendLineInvariant("using System.Linq;");
			writer.AppendLineInvariant("using Uno.UI;");

			//TODO Determine the list of namespaces to use
			foreach (var ns in XamlConstants.Namespaces.All)
			{
				writer.AppendLineInvariant($"using {ns};");
			}

			writer.AppendLineInvariant("using Uno.Extensions;");
			writer.AppendLineInvariant("using Uno;");

			writer.AppendLineInvariant("using {0};", _defaultNamespace);

			// For Subclass build functionality
			writer.AppendLineInvariant("");
			writer.AppendLineInvariant("#if __ANDROID__");
			writer.AppendLineInvariant("using _View = Android.Views.View;");
			writer.AppendLineInvariant("#elif __IOS__");
			writer.AppendLineInvariant("using _View = UIKit.UIView;");
			writer.AppendLineInvariant("#elif __MACOS__");
			writer.AppendLineInvariant("using _View = AppKit.NSView;");
			writer.AppendLineInvariant("#elif __WASM__");
			writer.AppendLineInvariant("using _View = Windows.UI.Xaml.UIElement;");
			writer.AppendLineInvariant("#elif NET46");
			writer.AppendLineInvariant("using _View = Windows.UI.Xaml.UIElement;");
			writer.AppendLineInvariant("#endif");

			writer.AppendLineInvariant("");

			var topLevelControl = _fileDefinition.Objects.First();

			if (topLevelControl.Type.Name == "ResourceDictionary")
			{
				BuildEmptyBackingClass(writer, topLevelControl);

				BuildResourceDictionary(writer, topLevelControl);
			}
			else
			{
				if (IsApplication(topLevelControl.Type))
				{
					BuildResourceDictionary(writer, topLevelControl);
				}

				_className = GetClassName(topLevelControl);

				using (writer.BlockInvariant("namespace {0}", _className.ns))
				{
					AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);

					var controlBaseType = GetType(topLevelControl.Type);

					using (writer.BlockInvariant("public sealed partial class {0} : {1}", _className.className, controlBaseType.ToDisplayString()))
					{
						var isDirectUserControlChild = IsUserControl(topLevelControl.Type, checkInheritance: false);

						using (Scope("{0}{1}".InvariantCultureFormat(_className.ns.Replace(".", ""), _className.className)))
						{
							using (writer.BlockInvariant(BuildControlInitializerDeclaration(_className.className, topLevelControl)))
							{
								if (IsApplication(topLevelControl.Type))
								{
									BuildApplicationInitializerBody(writer, topLevelControl);
								}
								else
								{
									BuildGenericControlInitializerBody(writer, topLevelControl, isDirectUserControlChild);
								}

								BuildNamedResources(writer, _namedResources);
							}

							if (isDirectUserControlChild)
							{
								using (writer.BlockInvariant("public {0}(bool skipsInitializeComponents)", _className.className))
								{
								}

								using (writer.BlockInvariant("private void InitializeComponent()"))
								{
									writer.AppendLineInvariant("Content = (_View)GetContent();");
								}
							}

							BuildPartials(writer, isStatic: false);

							BuildBackingFields(writer);

							BuildChildSubclasses(writer);

						}

						if (!IsApplication(topLevelControl.Type))
						{
							using (writer.BlockInvariant("class StaticResources"))
							{
								using (Scope("{0}{1}StaticResources".InvariantCultureFormat(_className.ns.Replace(".", ""), _className.className)))
								{
									BuildStaticResources(writer, _staticResources, isGlobalResources: false);

									// Build child subclasses for static resources
									BuildChildSubclasses(writer);
								}
							}
						}

						BuildInitializeXamlOwner(writer);
					}
				}
			}

			BuildXamlApplyBlocks(writer);

			// var formattedCode = ReformatCode(writer.ToString());
			return writer.ToString();
		}

		private void BuildInitializeXamlOwner(IndentedStringBuilder writer)
		{
			using (writer.BlockInvariant("private void InitializeXamlOwner()"))
			{ 
				if (_hasLiteralEventsRegistration)
				{
					// We only propagate the xaml owner in the tree if there are event registrations in the file
					// to avoid the performance impact of having a unused dependency property.
					writer.AppendLineInvariant("global::Uno.UI.Xaml.XamlInfo.SetXamlInfo(this, new global::Uno.UI.Xaml.XamlInfo(this));");
				}
			}
		}

		private void BuildXamlApplyBlocks(IndentedStringBuilder writer)
		{
			using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
			{
				using (writer.BlockInvariant("static class {0}XamlApplyExtensions", _fileUniqueId))
				{
					foreach (var typeInfo in _xamlAppliedTypes.Select((type, i) => new { type, Index = i }))
					{
						writer.AppendLineInvariant($"public delegate void XamlApplyHandler{typeInfo.Index}({GetGlobalizedTypeName(typeInfo.type.ToString())} instance);");

						using (writer.BlockInvariant(
							$"public static {GetGlobalizedTypeName(typeInfo.type.ToString())} {_fileUniqueId}_XamlApply(this {GetGlobalizedTypeName(typeInfo.type.ToString())} instance, XamlApplyHandler{typeInfo.Index} handler)"
						))
						{
							writer.AppendLineInvariant($"handler(instance);");
							writer.AppendLineInvariant($"return instance;");
						}
					}
				}
			}
		}

		private void BuildApplicationInitializerBody(IndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			writer.AppendLineInvariant($"global::Windows.UI.Xaml.GenericStyles.Initialize();");
			writer.AppendLineInvariant($"global::Windows.UI.Xaml.ResourceDictionary.DefaultResolver = global::{_defaultNamespace}.GlobalStaticResources.FindResource;");
			writer.AppendLineInvariant($"global::Windows.ApplicationModel.Resources.ResourceLoader.AddLookupAssembly(GetType().Assembly);");
			writer.AppendLineInvariant($"global::Windows.ApplicationModel.Resources.ResourceLoader.AddLookupAssembly(typeof(Windows.UI.Xaml.GenericStyles).Assembly);");
			writer.AppendLineInvariant($"global::Windows.ApplicationModel.Resources.ResourceLoader.DefaultLanguage = \"{_defaultLanguage}\";");
			writer.AppendLineInvariant($"global::{_defaultNamespace}.GlobalStaticResources.Initialize();");
			writer.AppendLineInvariant($"global::Uno.UI.DataBinding.BindableMetadata.Provider = new global::{_defaultNamespace}.BindableMetadataProvider();");

			writer.AppendLineInvariant($"#if __ANDROID__");
			writer.AppendLineInvariant($"global::Windows.UI.Xaml.Media.ImageSource.Drawables = typeof(global::{_defaultNamespace}.Resource.Drawable);");
			writer.AppendLineInvariant($"#endif");

			BuildProperties(writer, topLevelControl, isInline: false, returnsContent: false);
			writer.AppendLineInvariant(";");
		}

		private void BuildGenericControlInitializerBody(IndentedStringBuilder writer, XamlObjectDefinition topLevelControl, bool isDirectUserControlChild)
		{
			RegisterPartial("void OnInitializeCompleted()");

			writer.AppendLineInvariant("var nameScope = new global::Windows.UI.Xaml.NameScope();");
			writer.AppendLineInvariant("NameScope.SetNameScope(this, nameScope);");

			var hasContent = BuildProperties(writer, topLevelControl, isInline: false, returnsContent: isDirectUserControlChild);

			writer.AppendLineInvariant(";");

			if (hasContent)
			{
				writer.AppendLineInvariant("");
				writer.AppendLineInvariant(isDirectUserControlChild ? "content" : "this");

				string closure;

				using (var blockWriter = CreateApplyBlock(writer, null, out closure))
				{
					blockWriter.AppendLineInvariant(
						"// Source {0} (Line {1}:{2})",
						_fileDefinition.FilePath,
						topLevelControl.LineNumber,
						topLevelControl.LinePosition
					);

					BuildLiteralProperties(blockWriter, topLevelControl, closure);
				}

				if (IsFrameworkElement(topLevelControl.Type))
				{
					BuildExtendedProperties(writer, topLevelControl, isDirectUserControlChild, useGenericApply: true);
				}

				writer.AppendLineInvariant(";");
			}

			writer.AppendLineInvariant("OnInitializeCompleted();");
			writer.AppendLineInvariant("InitializeXamlOwner();");

			if (isDirectUserControlChild)
			{
				writer.AppendLineInvariant("return content;");
			}
		}

		private static string ReformatCode(string generatedCode)
		{
			using (var workspace = MSBuildWorkspace.Create())
			{
				var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);

				var options = workspace.Options
					.WithChangedOption(FormattingOptions.SmartIndent, LanguageNames.CSharp, FormattingOptions.IndentStyle.Block)
					.WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, Environment.NewLine)
					.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true)
					.WithChangedOption(CSharpFormattingOptions.SpaceAfterDot, false)
					.WithChangedOption(CSharpFormattingOptions.SpaceBeforeDot, false)
					.WithChangedOption(CSharpFormattingOptions.NewLineForElse, true)
					.WithChangedOption(CSharpFormattingOptions.NewLineForCatch, true)
					.WithChangedOption(CSharpFormattingOptions.NewLineForClausesInQuery, true)
					.WithChangedOption(CSharpFormattingOptions.NewLineForFinally, true)
					.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, true)
					.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, true)
					.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, true)
					.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, true)
					.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true)
					.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, true)
					.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInTypes, true)
					.WithChangedOption(CSharpFormattingOptions.SpaceAfterCast, false)
					.WithChangedOption(CSharpFormattingOptions.SpaceWithinCastParentheses, false)
					.WithChangedOption(CSharpFormattingOptions.SpacingAroundBinaryOperator, BinaryOperatorSpacingOptions.Single)
					.WithChangedOption(CSharpFormattingOptions.SpacingAfterMethodDeclarationName, true)
					.WithChangedOption(CSharpFormattingOptions.IndentBlock, true)
					.WithChangedOption(CSharpFormattingOptions.IndentBraces, false)
					.WithChangedOption(CSharpFormattingOptions.IndentSwitchCaseSection, true)
					.WithChangedOption(CSharpFormattingOptions.IndentSwitchSection, true)
					.WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true);

				var formatted = Formatter.Format(syntaxTree.GetRoot(), workspace, options);

				return formatted.ToFullString();
			}
		}

		private void BuildPartials(IIndentedStringBuilder writer, bool isStatic)
		{
			foreach (var partialDefinition in _partials)
			{
				writer.AppendLineInvariant("{0}partial " + partialDefinition + ";", isStatic ? "static " : "");
			}
		}

		private void BuildBackingFields(IIndentedStringBuilder writer)
		{
			foreach (var backingFieldDefinition in CurrentScope.BackingFields)
			{
				var sanitizedFieldName = SanitizeResourceName(backingFieldDefinition.Name);
				
				CurrentScope.ReferencedElementNames.Remove(sanitizedFieldName);

				writer.AppendLineInvariant($"private global::Windows.UI.Xaml.Data.ElementNameSubject _{sanitizedFieldName}Subject = new global::Windows.UI.Xaml.Data.ElementNameSubject();");


				using (writer.BlockInvariant($"private {GetGlobalizedTypeName(backingFieldDefinition.Type.ToString())} {sanitizedFieldName}"))
				{
					using (writer.BlockInvariant("get"))
					{
						writer.AppendLineInvariant($"return ({GetGlobalizedTypeName(backingFieldDefinition.Type.ToString())})_{sanitizedFieldName}Subject.ElementInstance;");
					}

					using (writer.BlockInvariant("set"))
					{
						writer.AppendLineInvariant($"_{sanitizedFieldName}Subject.ElementInstance = value;");
					}
				}
			}

			foreach (var remainingReference in CurrentScope.ReferencedElementNames)
			{
				// Create load-time subjects for ElementName references not in local scope
				writer.AppendLineInvariant($"private global::Windows.UI.Xaml.Data.ElementNameSubject _{remainingReference}Subject = new global::Windows.UI.Xaml.Data.ElementNameSubject(isRuntimeBound: true, name: \"{remainingReference}\");");
			}
		}

		private static string SanitizeResourceName(string name) => name.Replace("-", "_");

		private void BuildChildSubclasses(IIndentedStringBuilder writer, bool isTopLevel = false)
		{
			var disposable = isTopLevel ? writer.BlockInvariant("namespace {0}.__Resources", _defaultNamespace) : null;

			using (disposable)
			{
				foreach (var kvp in CurrentScope.Subclasses)
				{
					var className = kvp.Key;
					var contentOwner = kvp.Value.ContentOwner;

					using (Scope(className))
					{
						var classAccessibility = isTopLevel ? "" : "private";

						using (writer.BlockInvariant($"{classAccessibility} class {className}"))
						{
							using (writer.BlockInvariant("public {0} Build()", kvp.Value.ReturnType))
							{
								writer.AppendLineInvariant("var nameScope = new global::Windows.UI.Xaml.NameScope();");
								writer.AppendLineInvariant("var child = ");

								// Is never considered in Global Resources because class encapsulation
								BuildChild(writer, contentOwner, contentOwner.Objects.First());

								writer.AppendLineInvariant(";");
								writer.AppendLineInvariant("if (child is DependencyObject d) Windows.UI.Xaml.NameScope.SetNameScope(d, nameScope);");

								writer.AppendLineInvariant("return child;");
							}

							BuildBackingFields(writer);

							BuildChildSubclasses(writer);
						}
					}
				}
			}
		}

		private string BuildControlInitializerDeclaration(string className, XamlObjectDefinition topLevelControl)
		{
			if (IsPage(topLevelControl.Type))
			{
				return "protected override void InitializeComponent()";
			}
			else if (IsUserControl(topLevelControl.Type, checkInheritance: false))
			{
				string contentTypeDisplayString = GetImplicitChildTypeDisplayString(topLevelControl);

				return "public {0} GetContent()".InvariantCultureFormat(contentTypeDisplayString);
			}
			else
			{
				return "private void InitializeComponent()";
			}
		}

		private string GetImplicitChildTypeDisplayString(XamlObjectDefinition topLevelControl)
		{
			var contentType = FindImplicitContentMember(topLevelControl)?.Objects.FirstOrDefault()?.Type;

			return contentType == null ? XamlConstants.Types.IFrameworkElement : FindType(contentType)?.ToDisplayString() ?? XamlConstants.Types.IFrameworkElement;
		}

		private void BuildResourceDictionary(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			var globalResources = new Dictionary<string, XamlObjectDefinition>();

			using (Scope(Path.GetFileNameWithoutExtension(_fileDefinition.FilePath).Replace(".", "_") + "RD"))
			{
				using (writer.BlockInvariant("namespace {0}", _defaultNamespace))
				{
					AnalyzerSuppressionsGenerator.Generate(writer, _analyzerSuppressions);
					using (writer.BlockInvariant("public sealed partial class GlobalStaticResources"))
					{
						globalResources.Merge(ImportResourceDictionary(writer, topLevelControl));

						var themeResources = FindMember(topLevelControl, "ThemeDictionaries");

						if (themeResources != null)
						{
							// Theme resources are not supported for now, so we take the default key
							// and consider everthing inside as a standard StaticResource.

							var defaultTheme = themeResources
								.Objects
								.FirstOrDefault(o => o
									.Members
									.Any(m =>
										m.Member.Name == "Key"
										&& m.Value.ToString() == "Default"
									)
								);

							if (defaultTheme != null)
							{
								globalResources.Merge(ImportResourceDictionary(writer, defaultTheme));
							}
						}

						BuildStaticResources(writer, globalResources, isGlobalResources: true);

						BuildPartials(writer, isStatic: true);

					}
				}

				BuildChildSubclasses(writer, isTopLevel: true);
			}
		}

		private void BuildEmptyBackingClass(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			var className = FindClassName(topLevelControl);

			if (className.ns != null)
			{
				var controlBaseType = GetType(topLevelControl.Type);

				using (writer.BlockInvariant("namespace {0}", className.ns))
				{
					using (writer.BlockInvariant("public sealed partial class {0} : {1}", className.className, GetGlobalizedTypeName(controlBaseType.ToDisplayString())))
					{
						using (writer.BlockInvariant("public void InitializeComponent()"))
						{
						}
					}
				}
			}
		}

		private Dictionary<string, XamlObjectDefinition> ImportResourceDictionary(
			IIndentedStringBuilder writer,
			XamlObjectDefinition topLevelControl
		)
		{
			var resources = new Dictionary<string, XamlObjectDefinition>();

			XamlMemberDefinition contentNode;

			if (IsApplication(topLevelControl.Type))
			{
				contentNode = topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "Resources");
			}
			else
			{
				contentNode = FindMember(topLevelControl, "_UnknownContent");
			}


			if (contentNode != null)
			{
				foreach (var resource in contentNode.Objects)
				{
					var key = resource.Members.FirstOrDefault(m => m.Member.Name == "Key" || m.Member.Name == "Name");

					if (key != null)
					{
						if (!resources.ContainsKey(key.Value.ToString()))
						{
							resources.Add(key.Value.ToString(), resource);
						}
						else
						{
							throw new InvalidOperationException($"Duplicate resource {key.Value.ToString()}");
						}
					}
					else
					{
						if (resource.Type.Name == "Style")
						{
							var targetType = FindMember(resource, "TargetType")?.Value.ToString();

							if (targetType != null)
							{
								var fullTargetType = FindType(targetType).SelectOrDefault(t => t.ToDisplayString(), targetType);

								var keyName = (ImplicitStyleMarker + fullTargetType).Replace(".", "_");

								if (resources.ContainsKey(keyName))
								{
									throw new InvalidOperationException($"Implicit resource for {keyName} already exists");
								}
								else
								{
									resources.Add(keyName, resource);
								}
							}
							else
							{
								throw new InvalidOperationException("The implicit resource must have a TargetType atttribute");
							}
						}
						else if(resource.Type.Name == "ResourceDictionary")
						{
							// ResourceDictionaries and MergedDictionaries are handled elsewhere
						}
						else
						{
							GenerateError(
								writer,
								"Implicit resource other than Style in inline resources are not supported ({0}, Line {1}:{2})",
								contentNode.Member.Type,
								contentNode.LineNumber,
								contentNode.LinePosition
							);
						}
					}
				}
			}

			return resources;
		}

		private void BuildSingleTimeInitializer(IIndentedStringBuilder writer, string propertyType, string propertyName, Action propertyBodyBuilder, bool isStatic = true)
		{
			var sanitizedPropertyName = SanitizeResourceName(propertyName);

			var propertyInitializedVariable = "_{0}Initialized".InvariantCultureFormat(sanitizedPropertyName);
			var backingFieldVariable = "__{0}BackingField".InvariantCultureFormat(sanitizedPropertyName);
			var staticModifier = isStatic ? "static" : "";

			// The property type may be partially qualified, try resolving it through FindType
			var propertySymbol = FindType(propertyType);
			propertyType = propertySymbol?.GetFullName() ?? propertyType;

			writer.AppendLineInvariant($"private {staticModifier} bool {propertyInitializedVariable} = false;");
			writer.AppendLineInvariant($"private {staticModifier} {GetGlobalizedTypeName(propertyType)} {backingFieldVariable};");

			writer.AppendLine();

			using (writer.BlockInvariant($"public {staticModifier} {GetGlobalizedTypeName(propertyType)} {sanitizedPropertyName}"))
			{
				using (writer.BlockInvariant("get"))
				{
					using (writer.BlockInvariant($"if(!{propertyInitializedVariable})"))
					{
						writer.AppendLineInvariant($"{backingFieldVariable} = ");
						using (writer.Indent())
						{
							propertyBodyBuilder();
						}
						writer.AppendLineInvariant($"{propertyInitializedVariable} = true;");
					}

					writer.AppendLineInvariant($"return {backingFieldVariable};");
				}
			}

			writer.AppendLine();
		}

		private void BuildSourceLineInfo(IIndentedStringBuilder writer, XamlObjectDefinition definition)
		{
			if (_outputSourceComments)
			{
				writer.AppendLineInvariant(
					"// Source {0} (Line {1}:{2})",
						_relativePath,
					definition.LineNumber,
					definition.LinePosition
				);
			}
		}

		private void BuildStaticResources(
			IIndentedStringBuilder writer,
			Dictionary<string, XamlObjectDefinition> resources,
			bool isGlobalResources
		)
		{
			BuildKeyedStaticResources(writer, isGlobalResources, resources);

			if (isGlobalResources)
			{
				BuildImplicitStaticResources(writer, isGlobalResources, resources);
			}
		}

		private void BuildImplicitStaticResources(IIndentedStringBuilder writer, bool isGlobalResources, IEnumerable<KeyValuePair<string, XamlObjectDefinition>> resources)
		{
			var styleResources = resources.Where(r =>
				r.Value.Type.Name == "Style" && r.Key.StartsWith(ImplicitStyleMarker)
			);

			if (styleResources.Any())
			{
				// The index is used to generate the methods per partial file, so that a global
				// file can call them one by one.
				using (writer.BlockInvariant("static partial void RegisterImplicitStylesResources_{0}()", _fileUniqueId.ToString()))
				{
					foreach (var resource in styleResources)
					{
						BuildSourceLineInfo(writer, resource.Value);

						var targetType = GetMember(resource.Value, "TargetType")?.Value.ToString();

						var fullTargetType = GetType(targetType).ToDisplayString();

						writer.AppendLineInvariant(
							$"{XamlConstants.Types.Style}.RegisterDefaultStyleForType(typeof({GetGlobalizedTypeName(fullTargetType)}), () => {resource.Key});"
						);
					}
				}
			}
		}

		/// <summary>
		/// Gets the full target type, ensuring it is prefixed by "global::"
		/// to avoid namespace conflicts
		/// </summary>
		private string GetGlobalizedTypeName(string fullTargetType)
		{
			// Only prefix if it isn't already prefixed and if the type is fully qualified with a namespace
			// as opposed to, for instance, "double" or "Style"
			if (!fullTargetType.StartsWith(GlobalPrefix)
				&& fullTargetType.Contains(QualifiedNamespaceMarker))
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}{1}", GlobalPrefix, fullTargetType);
			}

			return fullTargetType;
		}

		private void BuildNamedResources(
			IIndentedStringBuilder writer,
			IEnumerable<KeyValuePair<string, XamlObjectDefinition>> namedResources
		)
		{
			foreach (var namedResource in namedResources)
			{
				BuildSourceLineInfo(writer, namedResource.Value);

				BuildChild(writer, null, namedResource.Value);
				writer.AppendLineInvariant(0, ";", namedResource.Value.Type);
			}
		}

		private bool IsSingleTimeInitializable(XamlType type)
		{
			var resourceType = GetType(type);

			return resourceType.IsValueType
				|| resourceType.SpecialType != SpecialType.None
				|| type.Name == "Double"
				|| type.Name == "Single"
				|| type.Name == "Integer"
				|| type.Name == "String"
				|| type.Name == "Boolean"
			;
		}

		private static string FormatResourcePropertyName(string key)
		{
			if (key.Contains(":"))
			{
				return key.Replace(":", "_");
			}

			return key;
		}

		private void BuildKeyedStaticResources(
			IIndentedStringBuilder writer,
			bool isGlobalResources,
			IEnumerable<KeyValuePair<string, XamlObjectDefinition>> keyedResources
		)
		{
			foreach (var keyedResource in keyedResources)
			{
				BuildSourceLineInfo(writer, keyedResource.Value);

				var resourceType = FindType(keyedResource.Value.Type);
				var resourcePropertyName = FormatResourcePropertyName(keyedResource.Key);
				var resourceTypeName = GenerateTypeName(keyedResource);

				if (keyedResource.Value.Type.Name == "Style")
				{
					BuildStyle(writer, keyedResource);
				}
				else if (keyedResource.Value.Type.Name == "StaticResource")
				{
					// Skip and add it to the global resolver
				}
				else if (IsSingleTimeInitializable(keyedResource.Value.Type))
				{
					writer.AppendLineInvariant($"public static {GetGlobalizedTypeName(resourceTypeName)} {SanitizeResourceName(resourcePropertyName)} {{{{ get; }}}} = ");
					BuildChild(writer, null, keyedResource.Value);
					writer.AppendLine(";");
					writer.AppendLine();
				}
				else
				{
					BuildSingleTimeInitializer(
						writer,
						GenerateTypeName(keyedResource),
						keyedResource.Key,
						() =>
						{
							BuildChild(writer, null, keyedResource.Value);
							writer.AppendLineInvariant(0, ";", keyedResource.Value.Type);
						}
					);
				}
			}

			if (keyedResources.Any())
			{
				if (isGlobalResources)
				{
					// Generate the lookup table, using the index provided at construction.
					// The index is used to generate the methods per partial file, so that a global
					// file can call them one by one.
					using (writer.BlockInvariant("static partial void RegisterResources_{0}()", _fileUniqueId.ToString()))
					{
						using (writer.BlockInvariant("AddResolver(name =>"))
						{
							BuildGetResources(writer, keyedResources);
						}

						writer.AppendLineInvariant(");");
					}
				}
				else
				{
					// If there is no method suffix, 
					using (writer.BlockInvariant("public object FindResource(string name)"))
					{
						BuildGetResources(writer, keyedResources);
					}
				}
			}
		}


		private void BuildGetResources(IIndentedStringBuilder writer, IEnumerable<KeyValuePair<string, XamlObjectDefinition>> resources)
		{
			using (writer.BlockInvariant("switch(name)"))
			{
				foreach (var resource in resources)
				{
					if (resource.Value.Type.Name == "StaticResource")
					{
						writer.AppendLineInvariant($"case \"{resource.Key}\":");

						if (GetMember(resource.Value, "ResourceKey").Value is string resourceKey)
						{
							writer.AppendLineInvariant($"\treturn {GetGlobalStaticResource(resourceKey)};");
						}
					}
					else
					{
						writer.AppendLineInvariant("case \"{0}\":", resource.Key);
						writer.AppendLineInvariant("\treturn {0};", SanitizeResourceName(resource.Key));
					}
				}
			}

			writer.AppendLineInvariant("return null;");
		}

		private string GenerateTypeName(KeyValuePair<string, XamlObjectDefinition> definition)
		{
			var typeName = definition.Value.Type.Name;

			if (definition.Value.Type.PreferredXamlNamespace.StartsWith("using:"))
			{
				typeName = definition.Value.Type.PreferredXamlNamespace.TrimStart("using:") + "." + typeName;
			}

			// Color is an alias the base color type for the target platform.
			if (typeName == "Color" || typeName == "ListViewBaseLayoutTemplate")
			{

				return GetType(typeName).ToDisplayString();
			}

			return typeName;
		}

		private void BuildStyle(IIndentedStringBuilder writer, KeyValuePair<string, XamlObjectDefinition> resource)
		{
			BuildSingleTimeInitializer(writer, "global::Windows.UI.Xaml.Style", resource.Key, () =>
			{
				BuildInlineStyle(writer, resource.Value);

				var partialOverrideNode = resource.Value.Members.FirstOrDefault(o => o.Member.Name.Equals("PartialOverride", StringComparison.OrdinalIgnoreCase));

				if (partialOverrideNode != null
					&& partialOverrideNode.Value.SelectOrDefault(v => v.ToString()).Equals("true", StringComparison.OrdinalIgnoreCase)
				)
				{
					writer.AppendLineInvariant(".Apply(s => On{0}Override(s))", resource.Key);
					RegisterPartial("void On{0}Override(Style s)", resource.Key);
				}

				writer.AppendLineInvariant(";", resource.Value.Type);
			});
		}

		private void BuildInlineStyle(IIndentedStringBuilder writer, XamlObjectDefinition style)
		{
			var targetTypeNode = GetMember(style, "TargetType");
			var targetType = targetTypeNode.Value.ToString();
			var fullTargetType = FindType(targetType).SelectOrDefault(t => t.ToDisplayString(), targetType);

			using (writer.BlockInvariant("new global::Windows.UI.Xaml.Style(typeof({0}))", GetGlobalizedTypeName(fullTargetType)))
			{
				var basedOnNode = style.Members.FirstOrDefault(o => o.Member.Name == "BasedOn");

				if (basedOnNode != null)
				{
					writer.AppendLineInvariant("BasedOn = (global::Windows.UI.Xaml.Style){0},", BuildBindingOption(basedOnNode, null, prependCastToType: false));
				}

				using (writer.BlockInvariant("Setters = ", fullTargetType))
				{
					var contentNode = FindMember(style, "_UnknownContent");

					if (contentNode != null)
					{
						foreach (var child in contentNode.Objects)
						{
							if (child.Type.Name == "Setter")
							{
								var propertyNode = FindMember(child, "Property");
								var valueNode = FindMember(child, "Value");
								var property = propertyNode?.Value.SelectOrDefault(p => p.ToString());

								if (valueNode != null)
								{
									BuildPropertySetter(writer, fullTargetType, ",", property, valueNode);
								}
								else
								{
									GenerateError(writer, $"No value was specified for {property} on {child.Type.Name}");
								}
							}
							else
							{
								GenerateError(writer, "Support for {0} is not implemented", child.Type.Name);
							}
						}
					}
				}
			}
		}

		private void BuildPropertySetter(IIndentedStringBuilder writer, string fullTargetType, string lineEnding, string property, XamlMemberDefinition valueNode, string targetInstance = null)
		{
			targetInstance = targetInstance ?? "o";

			property = property?.Trim('(', ')');

			// Handle attached properties
			var isAttachedProperty = property?.Contains(".") ?? false;
			if (isAttachedProperty)
			{
				var separatorIndex = property.IndexOf(".");

				var target = property.Remove(separatorIndex);
				property = property.Substring(separatorIndex + 1);
				fullTargetType = FindType(target)?.ToDisplayString();

				if (fullTargetType == null || property == null)
				{
					GenerateError(writer, $"Property {property} on {target} could not be found.");
					return;
				}
			}

			var propertyType = FindPropertyType(fullTargetType, property);

			if (propertyType != null)
			{
				bool isDependencyProperty = IsDependencyProperty(FindType(fullTargetType), property);

				if (valueNode.Objects.None())
				{
					if (isDependencyProperty)
					{
						writer.AppendLineInvariant(
							"new global::Windows.UI.Xaml.Setter({0}.{1}Property, ({2}){3})" + lineEnding,
							GetGlobalizedTypeName(fullTargetType),
							property,
							propertyType,
							BuildLiteralValue(valueNode, propertyType)
						);
					}
					else
					{
						writer.AppendLineInvariant(
							"new global::Windows.UI.Xaml.Setter<{0}>(\"{1}\", o => {3}.{1} = {2})" + lineEnding,
							GetGlobalizedTypeName(fullTargetType),
							property,
							BuildLiteralValue(valueNode, propertyType),
							targetInstance
						);
					}
				}
				else
				{
					if (isDependencyProperty)
					{
						writer.AppendLineInvariant(
							"new global::Windows.UI.Xaml.Setter({0}.{1}Property, () => ({2})",
							GetGlobalizedTypeName(fullTargetType),
							property,
							propertyType
						);
					}
					else
					{
						writer.AppendLineInvariant(
							"new global::Windows.UI.Xaml.Setter<{0}>(\"{1}\", o => {2}.{1} = ",
							GetGlobalizedTypeName(fullTargetType),
							property,
								targetInstance
						);
					}

					if (HasMarkupExtension(valueNode))
					{
						writer.AppendLineInvariant(BuildBindingOption(valueNode, propertyType, prependCastToType: true));
					}
					else
					{
						BuildChild(writer, valueNode, valueNode.Objects.First());
					}

					writer.AppendLineInvariant(")" + lineEnding);
				}
			}
			else
			{
				GenerateError(writer, "Property for {0} cannot be found on {1}", property, fullTargetType);
			}
		}

		private void GenerateError(IIndentedStringBuilder writer, string message, params object[] options)
		{
			if(ShouldWriteErrorOnInvalidXaml)
			{
				// it's important to add a new line to make sure #error is on its own line.
				writer.AppendLineInvariant(string.Empty);
				writer.AppendLineInvariant("#error " + message, options);
			}
			else
			{
				GenerateSilentWarning(writer, message, options);
			}

		}

		private void GenerateSilentWarning(IIndentedStringBuilder writer, string message, params object[] options)
		{
			// it's important to add a new line to make sure #error is on its own line.
			writer.AppendLineInvariant(string.Empty);
			writer.AppendLineInvariant("// WARNING " + message, options);
		}

		private bool HasMarkupExtension(XamlMemberDefinition valueNode)
		{
			return valueNode
				.Objects
				.Any(o =>
					o.Type.Name == "StaticResource"
					|| o.Type.Name == "ThemeResource"
					|| o.Type.Name == "Binding"
					|| o.Type.Name == "Bind"
					|| o.Type.Name == "TemplateBinding"
				);
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

		private XamlMemberDefinition FindMember(XamlObjectDefinition xamlObjectDefinition, string memberName)
		{
			return xamlObjectDefinition.Members.FirstOrDefault(m => m.Member.Name == memberName);
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

		private (string ns, string className) GetClassName(XamlObjectDefinition control)
		{
			var classMember = FindClassName(control);

			if (classMember.ns == null)
			{
				throw new Exception("Unable to find class name for toplevel control");
			}

			return classMember;
		}

		private static (string ns, string className) FindClassName(XamlObjectDefinition control)
		{
			var classMember = control.Members.FirstOrDefault(m => m.Member.Name == "Class");

			if (classMember != null)
			{
				var fullName = classMember.Value.ToString();

				var index = fullName.LastIndexOf('.');

				return (fullName.Substring(0, index), fullName.Substring(index + 1));
			}
			else
			{
				return (null, null);
			}
		}

		private bool BuildProperties(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl, bool isInline = true, bool returnsContent = false, string closureName = null)
		{
			try
			{
				BuildSourceLineInfo(writer, topLevelControl);

				if (topLevelControl.Members.Any())
				{
					var setterPrefix = string.IsNullOrWhiteSpace(closureName) ? string.Empty : closureName + ".";

					RegisterResources(topLevelControl);

					var implicitContentChild = FindImplicitContentMember(topLevelControl);

					if (implicitContentChild != null)
					{
						if (IsTextBlock(topLevelControl.Type))
						{
							if (implicitContentChild.Objects.Count != 0)
							{
								using (writer.BlockInvariant("{0}Inlines = ", setterPrefix))
								{
									foreach (var child in implicitContentChild.Objects)
									{
										BuildChild(writer, implicitContentChild, child);
										writer.AppendLineInvariant(",");
									}
								}
							}
							else if (implicitContentChild.Value != null)
							{
								var escapedString = DoubleEscape(implicitContentChild.Value.ToString());
								writer.AppendFormatInvariant($"{setterPrefix}Text = \"{escapedString}\"");
							}
						}
						else if (IsRun(topLevelControl.Type))
						{
							if (implicitContentChild.Value != null)
							{
								var escapedString = DoubleEscape(implicitContentChild.Value.ToString());
								writer.AppendFormatInvariant($"{setterPrefix}Text = \"{escapedString}\"");
							}
						}
						else if (IsSpan(topLevelControl.Type))
						{
							if (implicitContentChild.Objects.Count != 0)
							{
								using (writer.BlockInvariant("{0}Inlines = ", setterPrefix))
								{
									foreach (var child in implicitContentChild.Objects)
									{
										BuildChild(writer, implicitContentChild, child);
										writer.AppendLineInvariant(",");
									}
								}
							}
							else if (implicitContentChild.Value != null)
							{
								using (writer.BlockInvariant("{0}Inlines = ", setterPrefix))
								{
									var escapedString = DoubleEscape(implicitContentChild.Value.ToString());
									writer.AppendLine("new Run { Text = \"" + escapedString + "\" }");
								}
							}
						}
						else if (IsUserControl(topLevelControl.Type))
						{
							if (implicitContentChild.Objects.Any())
							{
								var firstChild = implicitContentChild.Objects.First();

								var elementType = FindType(topLevelControl.Type);
								var contentProperty = FindContentProperty(elementType);

								writer.AppendFormatInvariant(returnsContent ? "{0} content = " : "{1}{2} = ",
									GetType(firstChild.Type).ToDisplayString(),
									setterPrefix,
									contentProperty != null ? contentProperty.Name : "Content"
								);

								BuildChild(writer, implicitContentChild, firstChild);
							}
						}
						else if (IsPage(topLevelControl.Type))
						{
							if (implicitContentChild.Objects.Any())
							{
								writer.AppendFormatInvariant("{0}Content = ", setterPrefix);

								BuildChild(writer, implicitContentChild, implicitContentChild.Objects.First());
							}
						}
						else if (IsBorder(topLevelControl.Type))
						{
							if (implicitContentChild.Objects.Any())
							{
								if (implicitContentChild.Objects.Count > 1)
								{
									throw new InvalidOperationException("The type {0} does not support multiple children.".InvariantCultureFormat(topLevelControl.Type.Name));
								}

								writer.AppendFormatInvariant("{0}Child = ", setterPrefix);

								BuildChild(writer, implicitContentChild, implicitContentChild.Objects.First());
							}
						}
						else if (IsLinearGradientBrush(topLevelControl.Type))
						{
							BuildCollection(writer, isInline, setterPrefix + "GradientStops", implicitContentChild);
						}
						else if (IsInitializableCollection(topLevelControl))
						{
							foreach (var child in implicitContentChild.Objects)
							{
								BuildChild(writer, implicitContentChild, child);
								writer.AppendLineInvariant(",");
							}
						}
						else
						{
							var elementType = FindType(topLevelControl.Type);

							if (elementType != null)
							{
								var contentProperty = FindContentProperty(elementType);

								if (contentProperty != null)
								{
									if (IsCollectionOrListType(contentProperty.Type as INamedTypeSymbol))
									{
										string newableTypeName;

										if (IsInitializableProperty(contentProperty))
										{
											if (isInline)
											{
												using (writer.BlockInvariant(contentProperty.Name + " = "))
												{
													foreach (var child in implicitContentChild.Objects)
													{
														BuildChild(writer, implicitContentChild, child);
														writer.AppendLineInvariant(",");
													}
												}
											}
											else
											{
												foreach (var inner in implicitContentChild.Objects)
												{
													writer.AppendLine($"{contentProperty.Name}.Add(");

													BuildChild(writer, implicitContentChild, inner);

													writer.AppendLineInvariant(");");
												}
											}
										}
										else if (IsNewableProperty(contentProperty, out newableTypeName))
										{
											if (string.IsNullOrWhiteSpace(newableTypeName))
											{
												throw new InvalidOperationException("Unable to initialize newable collection type.  Type name for property {0} is empty."
													.InvariantCultureFormat(contentProperty.Name));
											}

											// Explicitly instantiate the collection and set it using the content property
											if (implicitContentChild.Objects.Count == 1
												&& contentProperty.Type == FindType(implicitContentChild.Objects[0].Type))
											{
												writer.AppendFormatInvariant(contentProperty.Name + " = ");
												BuildChild(writer, implicitContentChild, implicitContentChild.Objects[0]);
												writer.AppendLineInvariant(",");
											}
											else
											{
												using (writer.BlockInvariant("{0}{1} = new {2} ", setterPrefix, contentProperty.Name, newableTypeName))
												{
													foreach (var child in implicitContentChild.Objects)
													{
														BuildChild(writer, implicitContentChild, child);
														writer.AppendLineInvariant(",");
													}
												}
											}
										}
										else if (GetKnownNewableListOrCollectionInterface(contentProperty as INamedTypeSymbol, out newableTypeName))
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
													writer.AppendLineInvariant(",");
												}
											}
										}
										else
										{
											throw new InvalidOperationException("There is no known way to initialize collection property {0}."
												.InvariantCultureFormat(contentProperty.Name));
										}
									}
									else
									{
										if (implicitContentChild.Objects.Any())
										{
											writer.AppendLineInvariant(setterPrefix + contentProperty.Name + " = ");

											if (implicitContentChild.Objects.Count > 1)
											{
												throw new InvalidOperationException("The type {0} does not support multiple children.".InvariantCultureFormat(topLevelControl.Type.Name));
											}

											BuildChild(writer, implicitContentChild, implicitContentChild.Objects.First());

											if (isInline)
											{
												writer.AppendLineInvariant(",");
											}
										}
										else if(
											implicitContentChild.Value is string implicitValue 
											&& implicitValue.HasValueTrimmed()
										)
										{
											writer.AppendLineInvariant(setterPrefix + contentProperty.Name + " = \"" + implicitValue + "\"");

											if (isInline)
											{
												writer.AppendLineInvariant(",");
											}
										}
									}
								}
							}
						}

						return true;
					}
					else if (returnsContent && IsUserControl(topLevelControl.Type))
					{
						writer.AppendFormatInvariant(XamlConstants.Types.IFrameworkElement + " content = null");
					}
				}

				return false;
			}
			catch (Exception e)
			{
				throw new InvalidOperationException(
					"An error occurred when processing {0} at line {1}:{2} ({3}) : {4}"
					.InvariantCultureFormat(
						topLevelControl.Type.Name,
						topLevelControl.LineNumber,
						topLevelControl.LinePosition,
						_fileDefinition.FilePath,
						e.Message
					)
					, e
				);
			}
		}

		private static IPropertySymbol FindContentProperty(INamedTypeSymbol elementType)
		{
			return _findContentProperty(elementType);
		}

		private static IPropertySymbol SourceFindContentProperty(INamedTypeSymbol elementType)
		{
			var data = elementType
				.GetAllAttributes()
				.FirstOrDefault(t => t.AttributeClass.ToDisplayString() == XamlConstants.Types.ContentPropertyAttribute);

			if (data != null)
			{
				var nameProperty = data.NamedArguments.Where(f => f.Key == "Name").FirstOrDefault();

				if (nameProperty.Value.Value != null)
				{
					var name = nameProperty.Value.Value.ToString();

					return elementType?.GetAllProperties().FirstOrDefault(p => p.Name == name);
				}
			}

			return null;
		}

		private string GetFullGenericTypeName(INamedTypeSymbol propertyType)
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

		private void BuildCollection(IIndentedStringBuilder writer, bool isInline, string name, XamlMemberDefinition implicitContentChild)
		{
			if (isInline)
			{
				using (writer.BlockInvariant(name + " = "))
				{
					foreach (var child in implicitContentChild.Objects)
					{
						BuildChild(writer, implicitContentChild, child);
						writer.AppendLineInvariant(",");
					}
				}
			}
			else
			{
				foreach (var child in implicitContentChild.Objects)
				{
					writer.AppendLineInvariant(name + ".Add(");
					BuildChild(writer, implicitContentChild, child);
					writer.AppendLineInvariant(");");
				}
			}
		}

		private bool IsPage(XamlType xamlType) => IsType(xamlType, XamlConstants.Types.NativePage);

		private bool IsApplication(XamlType xamlType) => IsType(xamlType, XamlConstants.Types.Application);

		private XamlMemberDefinition FindImplicitContentMember(XamlObjectDefinition topLevelControl)
		{
			return topLevelControl
				.Members
				.FirstOrDefault(m => m.Member.Name == "_UnknownContent");
		}

		private void RegisterResources(XamlObjectDefinition topLevelControl)
		{
			var resourcesMember = topLevelControl.Members.FirstOrDefault(m => m.Member.Name == "Resources");

			if (resourcesMember != null)
			{
				var isExplicitResDictionary = resourcesMember.Objects.Any(o => o.Type.Name == "ResourceDictionary");

				if (isExplicitResDictionary)
				{
					// To be able to have MergedDictionaries, the first node of the Resource node 
					// must be an explicit resource dictionary.

					resourcesMember = FindImplicitContentMember(resourcesMember.Objects.First());
				}

				if (resourcesMember != null)
				{
					foreach (var resource in resourcesMember.Objects)
					{
						var key = resource.Members.FirstOrDefault(m => m.Member.Name == "Key");
						var name = resource.Members.FirstOrDefault(m => m.Member.Name == "Name");
						var mergedDictionaries = resource.Members.FirstOrDefault(m => m.Member.Name == "MergedDictionaries");

						if (key != null)
						{
							_staticResources.Add(key.Value.ToString(), resource);
						}
						else if (mergedDictionaries != null)
						{
							// Nothing for now. Since merged dictionaries are parsed separately, they are
							// considered as Global Resources.
						}
						else if (name != null)
						{
							_namedResources.Add(name.Value.ToString(), resource);
						}
						else
						{
							throw new Exception(
								"Implicit styles in inline resources are not supported ({0}, Line {1}:{2})"
								.InvariantCultureFormat(topLevelControl.Type.Name, topLevelControl.LineNumber, topLevelControl.LinePosition)
							);
						}
					}
				}
			}
		}

		private bool IsTextBlock(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.TextBlock);
		}

		private bool IsRun(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Run);
		}

		private bool IsSpan(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Span);
		}

		private bool IsType(XamlType xamlType, XamlType baseType)
		{
			if (xamlType == baseType)
			{
				return true;
			}

			if (baseType == null || xamlType == null)
			{
				return false;
			}

			var clrBaseType = FindType(baseType);

			if (clrBaseType != null)
			{
				return IsType(xamlType, clrBaseType.ToDisplayString());
			}
			else
			{
				return false;
			}
		}

		private bool IsType(XamlType xamlType, string typeName)
		{
			var type = FindType(xamlType);

			if (type != null)
			{
				do
				{
					if (type.ToDisplayString() == typeName)
					{
						return true;
					}

					type = type.BaseType;

					if (type == null)
					{
						break;
					}

				} while (type.Name != "Object");
			}

			return false;
		}

		public bool HasProperty(XamlType xamlType, string propertyName)
		{
			var type = FindType(xamlType);

			if (type != null)
			{
				do
				{
					if (type.GetAllProperties().Any(property => property.Name == propertyName))
					{
						return true;
					}

					type = type.BaseType;

					if (type == null)
					{
						break;
					}

				} while (type.Name != "Object");
			}

			return false;
		}

		private bool IsImplementingInterface(XamlType xamlType, string interfaceName)
		{
			return IsImplementingInterface(FindType(xamlType), interfaceName);
		}

		private bool IsImplementingInterface(INamedTypeSymbol symbol, string interfaceName)
		{
			Func<INamedTypeSymbol, string, bool> _isSameType =
				(sym, name) => sym.ToDisplayString() == name || sym.OriginalDefinition.ToDisplayString() == name;

			if (symbol != null)
			{
				if (_isSameType(symbol, interfaceName))
				{
					return true;
				}

				do
				{
					if (symbol.Interfaces.Any(i => _isSameType(i, interfaceName)))
					{
						return true;
					}

					symbol = symbol.BaseType;

					if (symbol == null)
					{
						break;
					}

				} while (symbol.Name != "Object");
			}

			return false;
		}

		private bool IsBorder(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Border);
		}

		private bool IsUserControl(XamlType xamlType, bool checkInheritance = true)
		{
			return checkInheritance ?
				IsType(xamlType, XamlConstants.Types.UserControl) :
				FindType(xamlType)?.ToDisplayString().Equals(XamlConstants.Types.UserControl) ?? false;
		}

		private bool IsContentControl(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.ContentControl);
		}

		private bool IsPanel(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Panel);
		}

		private bool IsLinearGradientBrush(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.LinearGradientBrush);
		}

		private bool IsFrameworkElement(XamlType xamlType)
		{
			return IsImplementingInterface(FindType(xamlType), XamlConstants.Types.IFrameworkElement);
		}

		private bool IsAndroidView(XamlType xamlType)
		{
			return IsType(xamlType, "Android.Views.View");
		}

		private bool IsIOSUIView(XamlType xamlType)
		{
			return IsType(xamlType, "UIKit.UIView");
		}

		private bool IsTransform(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Transform);
		}

		private void TryExtractAutomationId(XamlMemberDefinition member, string[] targetMembers, ref string uiAutomationId)
		{
			if (uiAutomationId.HasValue())
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

			string bindingPath;

			// Checks the first binding member, which can be used to implicitlty declare the binding path (i.e. without 
			// declaring a "Path=" specifier). Otherwise, the we look for any explicit binding path declaration.
			var firstBindingMember = bindingMembers?.FirstOrDefault();
			if (firstBindingMember != null &&
				(firstBindingMember.Member.Name == "Path" ||
				 firstBindingMember.Member.Name == "_PositionalParameters" ||
				 firstBindingMember.Member.Name.IsNullOrWhiteSpace())
			)
			{
				bindingPath = firstBindingMember.Value.ToString();
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

		private void BuildExtendedProperties(IIndentedStringBuilder outerwriter, XamlObjectDefinition objectDefinition, bool useChildTypeForNamedElement = false, bool useGenericApply = false)
		{
			var objectUid = GetObjectUid(objectDefinition);

			var extendedProperties = GetExtendedProperties(objectDefinition);
			bool hasChildrenWithPhase = HasChildrenWithPhase(objectDefinition);

			if (extendedProperties.Any() || hasChildrenWithPhase)
			{
				string closureName;

				using (var writer = CreateApplyBlock(outerwriter, useGenericApply ? null : GetType(objectDefinition.Type), out closureName))
				{
					XamlMemberDefinition uidMember = null;
					XamlMemberDefinition nameMember = null;
					string uiAutomationId = null;
					string[] extractionTargetMembers = null;

					if (_isUiAutomationMappingEnabled)
					{
						// Look up any potential UI automation member mappings available for the current object definition
						extractionTargetMembers = _uiAutomationMappings
							?.FirstOrDefault(m => IsType(objectDefinition.Type, m.Key) || IsImplementingInterface(objectDefinition.Type, m.Key))
							.Value
							?.ToArray() ?? new string[0];
					}

					if (hasChildrenWithPhase)
					{
						writer.AppendLine(GenerateRootPhases(objectDefinition, closureName));
					}

					foreach (var member in extendedProperties)
					{
						if (extractionTargetMembers != null)
						{
							TryExtractAutomationId(member, extractionTargetMembers, ref uiAutomationId);
						}

						if (HasMarkupExtension(member))
						{
							TryValidateContentPresenterBinding(writer, objectDefinition, member);

							BuildComplexPropertyValue(writer, member, closureName + ".", closureName);
						}
						else if (member.Objects.Any())
						{
							if (member.Member.Name == "_UnknownContent") // So : FindType(member.Owner.Type) is INamedTypeSymbol type && IsCollectionOrListType(type)
							{
								foreach (var item in member.Objects)
								{
									writer.AppendLineInvariant($"{closureName}.Add(");
									using (writer.Indent())
									{
										BuildChild(writer, member, item);
									}
									writer.AppendLineInvariant(");");
								}
							}
							else if (!IsType(objectDefinition.Type, member.Member.DeclaringType))
							{
								var ownerType = GetType(member.Member.DeclaringType);

								var propertyType = GetPropertyType(member.Member);

								if (IsExactlyCollectionOrListType(propertyType))
								{
									// If the property is specifically an IList or an ICollection
									// we can use C#'s collection initializer.
									writer.AppendLineInvariant(
										"{0}.Set{1}({2}, ",
										GetGlobalizedTypeName(ownerType.ToDisplayString()),
										member.Member.Name,
										closureName
									);

									using (writer.BlockInvariant("new[]"))
									{
										foreach (var inner in member.Objects)
										{
											BuildChild(writer, member, inner);
											writer.AppendLine(",");
										}
									}

									writer.AppendLineInvariant(");");
								}
								else if (IsCollectionOrListType(propertyType))
								{
									// If the property is a concrete type that implements an IList or
									// an ICollection, we must get the property and call add explicitly
									// on it.
									var localCollectionName = $"{closureName}_collection_{_collectionIndex++}";

									writer.AppendLineInvariant(
										$"var {localCollectionName} = {ownerType.ToDisplayString()}.Get{member.Member.Name}({closureName});"
									);

									foreach (var inner in member.Objects)
									{
										writer.AppendLine($"{localCollectionName}.Add(");

										BuildChild(writer, member, inner);

										writer.AppendLineInvariant(");");
									}
								}
								else
								{
									// If the property is specifically an IList or an ICollection
									// we can use C#'s collection initializer.
									writer.AppendLineInvariant(
										"{0}.Set{1}({2}, ",
										GetGlobalizedTypeName(ownerType.ToDisplayString()),
										member.Member.Name,
										closureName
									);

									if (member.Objects.Count() == 1)
									{
										BuildChild(writer, member, member.Objects.First());
									}
									else
									{
										throw new InvalidOperationException($"The property {member.Member.Name} of type {propertyType} does not support adding multiple objects.");
									}

									writer.AppendLineInvariant(");");
								}
							}
							else
							{
								// GenerateWarning(writer, $"Unknwon type {objectDefinition.Type} for property {member.Member.DeclaringType}");
							}
						}
						else
						{
							if (
								member.Member.Name == "Name"
								&& !IsMemberInsideResourceDictionary(objectDefinition)
							)
							{
								writer.AppendLineInvariant($@"nameScope.RegisterName(""{member.Value}"", {closureName});");
							}

							if (
								member.Member.Name == "Name"
								&& !IsAttachedProperty(member)
								&& !IsMemberInsideResourceDictionary(objectDefinition)
							)
							{
								nameMember = member;
								var value = member.Value.ToString();
								var type = useChildTypeForNamedElement ?
									GetImplicitChildTypeDisplayString(objectDefinition) :
									FindType(objectDefinition.Type)?.ToDisplayString();

								if (type == null)
								{
									throw new InvalidOperationException($"Unable to find type {objectDefinition.Type}");
								}

								writer.AppendLineInvariant("this.{0} = {1};", value, closureName);
								RegisterBackingField(type, value);
							}
							else if (member.Member.Name == "Name"
								&& member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
							{
								writer.AppendLineInvariant("// x:Name {0}", member.Value, member.Value);
							}
							else if (member.Member.Name == "Key")
							{
								writer.AppendLineInvariant("// Key {0}", member.Value, member.Value);
							}
							else if (member.Member.Name == "DeferLoadStrategy")
							{
								writer.AppendLineInvariant("// DeferLoadStrategy {0}", member.Value, member.Value);
							}
							else if (member.Member.Name == "Uid")
							{
								uidMember = member;
							}
							else if (member.Member.Name == "Phase")
							{
								writer.AppendLineInvariant($"Uno.UI.FrameworkElementHelper.SetRenderPhase({closureName}, {member.Value});");
							}
							else if (member.Member.Name == "Class" && member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
							{
								writer.AppendLineInvariant("// Class {0}", member.Value, member.Value);
							}
							else if (
								member.Member.Name == "TargetName" &&
								IsAttachedProperty(member) &&
								member.Member.DeclaringType?.Name == "Storyboard"
							)
							{
								writer.AppendLineInvariant(@"{0}.SetTargetName({2}, ""{1}"");",
									GetGlobalizedTypeName(FindType(member.Member.DeclaringType).SelectOrDefault(t => t.ToDisplayString(), member.Member.DeclaringType.Name)),
									this.RewriteAttachedPropertyPath(member.Value.ToString()),
									closureName);

								writer.AppendLineInvariant("{0}.SetTarget({2}, this._{1}Subject);",
												GetGlobalizedTypeName(FindType(member.Member.DeclaringType).SelectOrDefault(t => t.ToDisplayString(), member.Member.DeclaringType.Name)),
												member.Value,
												closureName);
							}
							else if (
								member.Member.Name == "TargetProperty" &&
								IsAttachedProperty(member) &&
								member.Member.DeclaringType?.Name == "Storyboard"
							)
							{
								writer.AppendLineInvariant(@"{0}.SetTargetProperty({2}, ""{1}"");",
									GetGlobalizedTypeName(FindType(member.Member.DeclaringType).SelectOrDefault(t => t.ToDisplayString(), member.Member.DeclaringType.Name)),
									this.RewriteAttachedPropertyPath(member.Value.ToString()),
									closureName);
							}
							else if (
								member.Member.DeclaringType?.Name == "RelativePanel" &&
								IsAttachedProperty(member) &&
								IsRelativePanelSiblingProperty(member.Member.Name)
							)
							{
								writer.AppendLineInvariant(@"{0}.Set{1}({2}, _{3}Subject);",
									GetGlobalizedTypeName(FindType(member.Member.DeclaringType).SelectOrDefault(t => t.ToDisplayString(), member.Member.DeclaringType.Name)),
									member.Member.Name,
									closureName,
									member.Value);
							}
							else
							{
								if (
									!IsType(member.Member.DeclaringType, objectDefinition.Type)
									|| IsAttachedProperty(member)
									|| FindEventType(member.Member) != null
								)
								{
									if (FindPropertyType(member.Member) != null)
									{
										BuildSetAttachedProperty(writer, closureName, member, objectUid);
									}
									else if (FindEventType(member.Member) != null)
									{
										// If a binding is inside a DataTemplate, the binding root in the case of an x:Bind is
										// the DataContext, not the control's instance.
										var isInsideDataTemplate = IsMemberInsideFrameworkTemplate(member.Owner);

										if (!isInsideDataTemplate)
										{
											writer.AppendLineInvariant($"{closureName}.{member.Member.Name} += {member.Value};");
										}
										else if(_className.className != null)
										{
											_hasLiteralEventsRegistration = true;
											writer.AppendLineInvariant($"{closureName}.RegisterPropertyChangedCallback(");
											using(writer.BlockInvariant($"global::Uno.UI.Xaml.XamlInfo.XamlInfoProperty, (s, p) =>"))
											{
												using(writer.BlockInvariant($"if (global::Uno.UI.Xaml.XamlInfo.GetXamlInfo({closureName})?.Owner is {_className.className} owner)"))
												{
													writer.AppendLineInvariant($"{closureName}.{member.Member.Name} += owner.{member.Value};");
												}
											}
											writer.AppendLineInvariant($");");
										}
										else
										{
											GenerateError(writer, $"Unable to use event {member.Member.Name} without a backing class (use x:Class)");
										}
									}
									else
									{
										GenerateError(writer, "Property {0} is not available on {1}, value is {2}", member.Member.Name, member.Member.DeclaringType?.Name, member.Value);
									}
								}
							}
						}
					}

					if (_isUiAutomationMappingEnabled)
					{
						// Prefer using the Uid or the Name if their value has been explicitely assigned
						var assignedUid = uidMember?.Value?.ToString();
						var assignedName = nameMember?.Value?.ToString();

						if (assignedUid.HasValue())
						{
							uiAutomationId = assignedUid;
						}
						else if (assignedName.HasValue())
						{
							uiAutomationId = assignedName;
						}

						BuildUiAutomationId(writer, closureName, uiAutomationId, objectDefinition);
					}
				}
			}
		}

		private void TryValidateContentPresenterBinding(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, XamlMemberDefinition member)
		{
			if(
				FindType(objectDefinition.Type) == _contentPresenterSymbol 
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
						writer.AppendFormatInvariant(
							"\n#error Using a non-template binding expression on Content " +
							"will likely result in an undefined runtime behavior, as ContentPresenter.Content overrides " + 
							"the local value of ContentPresenter.DataContent. " + 
							"Use ContentControl instead if you really need a normal binding on Content.\n");
					}
				}
			}
		}

		private void BuildUiAutomationId(IIndentedStringBuilder writer, string closureName, string uiAutomationId, XamlObjectDefinition parent)
		{
			if (uiAutomationId.IsNullOrEmpty())
			{
				return;
			}

			writer.AppendLineInvariant("// UI automation id: {0}", uiAutomationId);

			// ContentDescription and AccessibilityIdentifier are used by Xamarin.UITest (Test Cloud) to identify visual elements
			if (IsAndroidView(parent.Type))
			{
				writer.AppendLineInvariant("{0}.ContentDescription = \"{1}\";", closureName, uiAutomationId);
			};

			if (IsIOSUIView(parent.Type))
			{
				writer.AppendLineInvariant("{0}.AccessibilityIdentifier = \"{1}\";", closureName, uiAutomationId);
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

		private void BuildSetAttachedProperty(IIndentedStringBuilder writer, string closureName, XamlMemberDefinition member, string objectUid)
		{
			writer.AppendLineInvariant(
				"{0}.Set{1}({3}, {2});",
				GetGlobalizedTypeName(FindType(member.Member.DeclaringType).SelectOrDefault(t => t.ToDisplayString(), member.Member.DeclaringType.Name)),
				member.Member.Name,
				BuildLiteralValue(member, owner: member, objectUid: objectUid),
				closureName
			);
		}

		private XamlLazyApplyBlockIIndentedStringBuilder CreateApplyBlock(IIndentedStringBuilder writer, INamedTypeSymbol appliedType, out string closureName)
		{
			closureName = "c" + (_applyIndex++).ToString(CultureInfo.InvariantCulture);

			//
			// Since we're using strings to generate the code, we can't know ahead of time if 
			// content will be generated only by looking at the Xaml object model.
			// For now, we only observe if the inner code has generated code, and we create 
			// the apply block at that time.
			//
			string delegateType = null;

			if (appliedType != null)
			{
				int typeIndex = _xamlAppliedTypes.IndexOf(appliedType);

				if (typeIndex == -1)
				{
					_xamlAppliedTypes.Add(appliedType);
					typeIndex = _xamlAppliedTypes.Count - 1;
				}

				delegateType = $"{_fileUniqueId}XamlApplyExtensions.XamlApplyHandler{typeIndex}";
			}

			return new XamlLazyApplyBlockIIndentedStringBuilder(writer, closureName, appliedType != null ? _fileUniqueId : null, delegateType);
		}

		private void RegisterPartial(string format, params object[] values)
		{
			_partials.Add(format.InvariantCultureFormat(values));
		}

		private void RegisterBackingField(string type, string name)
		{
			CurrentScope.BackingFields.Add(new BackingFieldDefinition(type, name));
		}

		private void RegisterChildSubclass(string name, XamlMemberDefinition owner, string returnType)
		{
			CurrentScope.Subclasses[name] = new Subclass { ContentOwner = owner, ReturnType = returnType };
		}

		private void BuildComplexPropertyValue(IIndentedStringBuilder writer, XamlMemberDefinition member, string prefix, string closureName = null)
		{
			Func<string, string> formatLine = format => prefix + format + (prefix.HasValue() ? ";\r\n" : "");

			var bindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Binding");
			var bindNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Bind");
			var templateBindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "TemplateBinding");

			// If a binding is inside a DataTemplate, the binding root in the case of an x:Bind is
			// the DataContext, not the control's instance.
			var isInsideDataTemplate = IsMemberInsideDataTemplate(member.Owner);

			string GetBindingOptions()
			{
				if (bindingNode != null || bindNode != null)
				{
					return (bindingNode ?? bindNode)
						.Members
						.Select(m => "{0} = {1}".InvariantCultureFormat(m.Member.Name == "_PositionalParameters" ? "Path" : m.Member.Name, BuildBindingOption(m, FindPropertyType(m.Member), prependCastToType: true)))
						.Concat(bindNode != null && !isInsideDataTemplate ? new[] { "CompiledSource = this" } : Enumerable.Empty<string>())
						.JoinBy(", ");

				}
				if (templateBindingNode != null)
				{
					return templateBindingNode
						.Members
						.Select(m => "{0} = {1}".InvariantCultureFormat(m.Member.Name == "_PositionalParameters" ? "Path" : m.Member.Name, BuildBindingOption(m, FindPropertyType(m.Member), prependCastToType: true)))
						.Concat("RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)")
						.JoinBy(", ");
				}

				return null;
			};

			var bindingOptions = GetBindingOptions();

			if (bindingOptions != null)
			{
				var isAttachedProperty = IsDependencyProperty(member.Member);

				if (isAttachedProperty)
				{
					var propertyOwner = GetType(member.Member.DeclaringType);

					writer.AppendLine(formatLine($"SetBinding({GetGlobalizedTypeName(propertyOwner.ToDisplayString())}.{member.Member.Name}Property, new {XamlConstants.Types.Binding}{{ {bindingOptions} }})"));
				}
				else
				{
					writer.AppendLine(formatLine($"SetBinding(\"{member.Member.Name}\", new {XamlConstants.Types.Binding}{{ {bindingOptions} }})"));
				}
			}

			var resourceName = GetStaticResourceName(member);

			if (resourceName != null)
			{
				var isAttachedProperty = IsAttachedProperty(member);

				if (isAttachedProperty)
				{
					var propertyOwner = GetType(member.Member.DeclaringType);

					writer.AppendFormatInvariant("{0}.Set{1}({2},{3});\r\n",
						GetGlobalizedTypeName(propertyOwner.ToDisplayString()),
						member.Member.Name,
						closureName,
						resourceName
					);
				}
				else
				{
					writer.AppendLineInvariant(0, formatLine("{0} = {1}"),
						member.Member.Name,
						resourceName
					);
				}
			}
		}

		private bool IsMemberInsideDataTemplate(XamlObjectDefinition xamlObject)
			=> IsMemberInside(xamlObject, "DataTemplate");

		private bool IsMemberInsideFrameworkTemplate(XamlObjectDefinition xamlObject)
			=> IsMemberInside(xamlObject, "DataTemplate")
			|| IsMemberInside(xamlObject, "ControlTemplate")
			|| IsMemberInside(xamlObject, "ItemsPanelTemplate");

		private bool IsMemberInsideResourceDictionary(XamlObjectDefinition xamlObject)
			=> IsMemberInside(xamlObject, "ResourceDictionary", maxDepth: 1);

		private static bool IsMemberInside(XamlObjectDefinition xamlObject, string typeName, int? maxDepth = null)
		{
			if (xamlObject == null)
			{
				return false;
			}

			int depth = 0;
			do
			{
				if (xamlObject.Type?.Name == typeName)
				{
					return true;
				}

				xamlObject = xamlObject.Owner;
			}
			while (xamlObject != null && (maxDepth == null || ++depth <= maxDepth));

			return false;
		}

		private string GetStaticResourceName(XamlMemberDefinition member, INamedTypeSymbol targetPropertyType = null)
		{
			var staticResourceNode = member.Objects.FirstOrDefault(o => o.Type.Name == "StaticResource");
			var themeResourceNode = member.Objects.FirstOrDefault(o => o.Type.Name == "ThemeResource");

			var staticResourcePath = staticResourceNode?.Members.First().Value.ToString();
			var themeResourcePath = themeResourceNode?.Members.First().Value.ToString();

			//For now, both ThemeResource and StaticResource are considered as StaticResource
			if (staticResourcePath == null && themeResourcePath == null)
			{
				return null;
			}
			else
			{
				var resourcePath = staticResourcePath ?? themeResourcePath;

				targetPropertyType = targetPropertyType ?? FindPropertyType(member.Member);

				if(_staticResources.ContainsKey(resourcePath))
				{
					return $"{GetCastString(targetPropertyType, _staticResources[resourcePath])}StaticResources.{SanitizeResourceName(resourcePath)}";
				}
				else if(targetPropertyType?.Name == "TimeSpan")
				{
					// explicit support for TimeSpan because we can't override the parsing.
					return $"global::System.TimeSpan.Parse({GetGlobalStaticResource(resourcePath)}.ToString())";
				}
				else
				{
					return $"{GetCastString(targetPropertyType, null)}{GetGlobalStaticResource(resourcePath)}";
				}
			}
		}

		/// <summary>
		/// Inserts explicit cast if a resource is being assigned to a property of a different type
		/// </summary>
		/// <param name="targetProperty"></param>
		/// <param name="targettingValue"></param>
		/// <returns></returns>
		private string GetCastString(INamedTypeSymbol targetProperty, XamlObjectDefinition targettingValue)
		{
			if (targetProperty != null
				&& targetProperty.Name != targettingValue?.Type.Name
				&& (targettingValue == null || IsPrimitive(targettingValue))
				)
			{
				return $"({targetProperty.ToDisplayString()})";
			}
			return "";
		}

		private bool IsDependencyProperty(XamlMember member)
		{
			string name = member.Name;
			var propertyOwner = FindType(member.DeclaringType);

			return IsDependencyProperty(propertyOwner, name);
		}

		private static bool IsDependencyProperty(INamedTypeSymbol propertyOwner, string name)
		{
			if (propertyOwner != null)
			{
				var propertyDependencyPropertyQuery = propertyOwner.GetAllProperties().Where(p => p.Name == name + "Property");
				var fieldDependencyPropertyQuery = propertyOwner.GetAllFields().Where(p => p.Name == name + "Property");

				return propertyDependencyPropertyQuery.Any() || fieldDependencyPropertyQuery.Any();
			}
			else
			{
				return false;
			}
		}

		private string BuildLiteralValue(INamedTypeSymbol propertyType, string memberValue, XamlMemberDefinition owner = null, string memberName = "", string objectUid = "")
		{
			if (IsLocalizedString(propertyType, objectUid))
			{
				//windows 10 localization concat the xUid Value with the member value (Text, Content, Header etc...)
				var fullKey = objectUid + "." + memberName;

				if (owner != null && IsAttachedProperty(owner))
				{
					var declaringType = GetType(owner.Member.DeclaringType);
					var ns = declaringType.ContainingNamespace.GetFullName();
					var type = declaringType.Name;
					fullKey = $"{objectUid}.[using:{ns}]{type}.{memberName}";
				}

				if (_resourceKeys.Any(k => k == fullKey))
				{
					return @"global::Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView().GetString(""" + fullKey + @""")";
				}
			}

			switch (propertyType.ToDisplayString())
			{
				case "int":
				case "long":
				case "short":
				case "byte":
					return memberValue;

				case "float":
				case "double":
					return GetFloatingPointLiteral(memberValue, propertyType, owner);

				case "string":
					return "\"" + DoubleEscape(memberValue) + "\"";

				case "bool":
					return Boolean.Parse(memberValue).ToString().ToLowerInvariant();

				case XamlConstants.Types.Brush:
				case XamlConstants.Types.SolidColorBrush:
					return BuildBrush(memberValue);

				case XamlConstants.Types.Thickness:
					return BuildThickness(memberValue);

				case XamlConstants.Types.CornerRadius:
					return $"new {XamlConstants.Types.CornerRadius}({memberValue})";

				case XamlConstants.Types.FontFamily:
					return $@"new {propertyType.ToDisplayString()}(""{memberValue}"")";

				case XamlConstants.Types.FontWeight:
					return BuildFontWeight(memberValue);

				case XamlConstants.Types.GridLength:
					return BuildGridLength(memberValue);

				case "UIKit.UIColor":
					return BuildColor(memberValue);

				case "Windows.UI.Color":
					return BuildColor(memberValue);

				case "Android.Graphics.Color":
					return BuildColor(memberValue);

				case "System.Uri":
					if (memberValue.StartsWith("/"))
					{
						return "new System.Uri(\"ms-appx://" + memberValue + "\")";
					}
					else
					{
						return "new System.Uri(\"" + memberValue + "\")";
					}

				case "System.Type":
					return $"typeof({GetGlobalizedTypeName(GetType(memberValue).ToDisplayString())})";

				case XamlConstants.Types.Geometry:
					if (_isWasm)
					{
						return $"@\"{memberValue}\"";
					}
					var generated = Parsers.ParseGeometry(memberValue, CultureInfo.InvariantCulture);
					return generated;

				case XamlConstants.Types.KeyTime:
					return ParseTimeSpan(memberValue);

				case XamlConstants.Types.Duration:
					return $"new Duration({ ParseTimeSpan(memberValue) })";

				case "System.TimeSpan":
					return ParseTimeSpan(memberValue);

				case "System.Drawing.Point":
					return "new System.Drawing.Point(" + memberValue + ")";

				case "Windows.UI.Xaml.Media.CacheMode":
					return ParseCacheMode(memberValue);

				case "System.Drawing.PointF":
					return "new System.Drawing.PointF(" + AppendFloatSuffix(memberValue) + ")";

				case "System.Drawing.Size":
					return "new System.Drawing.Size(" + memberValue + ")";

				case "Windows.Foundation.Size":
					return "new Windows.Foundation.Size(" + memberValue + ")";

				case "Windows.UI.Xaml.Media.Matrix":
					return "new Windows.UI.Xaml.Media.Matrix(" + memberValue + ")";

				case "Windows.Foundation.Point":
					return "new Windows.Foundation.Point(" + memberValue + ")";

				case "Windows.UI.Xaml.Input.InputScope":
					return "new global::Windows.UI.Xaml.Input.InputScope { Names = { new global::Windows.UI.Xaml.Input.InputScopeName { NameValue = global::Windows.UI.Xaml.Input.InputScopeNameValue." + memberValue + "} } }";

				case "UIKit.UIImage":
					if (memberValue.StartsWith(XamlConstants.BundleResourcePrefix, StringComparison.InvariantCultureIgnoreCase))
					{
						return "UIKit.UIImage.FromBundle(\"" + memberValue.Substring(XamlConstants.BundleResourcePrefix.Length, memberValue.Length - XamlConstants.BundleResourcePrefix.Length) + "\")";
					}
					return memberValue;

				case "Windows.UI.Xaml.Controls.IconElement":
					return "new Windows.UI.Xaml.Controls.SymbolIcon { Symbol = Windows.UI.Xaml.Controls.Symbol." + memberValue + "}";

				case "Windows.Media.Playback.IMediaPlaybackSource":
					return "Windows.Media.Core.MediaSource.CreateFromUri(new Uri(\"" + memberValue + "\"))";
			}

			var isEnum = propertyType
				.TypeKind == TypeKind.Enum;

			if (isEnum)
			{
				var validFlags = propertyType.GetFields().Select(field => field.Name);
				var actualFlags = memberValue.Split(',').Select(part => part.Trim());

				var invalidFlags = actualFlags.Except(validFlags, StringComparer.OrdinalIgnoreCase);
				if (invalidFlags.Any())
				{
					throw new Exception($"The following values are not valid members of the '{propertyType.Name}' enumeration: {string.Join(", ", invalidFlags)}");
				}

				var finalFlags = validFlags.Intersect(actualFlags, StringComparer.OrdinalIgnoreCase);
				return string.Join("|", finalFlags.Select(flag => $"{propertyType.ToDisplayString()}.{flag}"));
			}

			var hasImplictToString = propertyType
				.GetMethods()
				.Any(m =>
					m.Name == "op_Implicit"
					&& m.Parameters.FirstOrDefault().SelectOrDefault(p => p.Type.ToDisplayString() == "string")
				);

			if (hasImplictToString

				// Can be an object (e.g. in case of Binding.ConverterParameter).
				|| propertyType.ToDisplayString() == "object"
			)
			{
				return "@\"" + memberValue.ToString() + "\"";
			}

			if (memberValue == null && propertyType.IsReferenceType)
			{
				return "null";
			}

			throw new Exception("Unable to convert {0} for {1} with type {2}".InvariantCultureFormat(memberValue, memberName, propertyType));
		}

		private string ParseCacheMode(string memberValue)
		{
			if (memberValue.Equals("BitmapCache", StringComparison.OrdinalIgnoreCase))
			{
				return "new global::Windows.UI.Xaml.Media.BitmapCache()";
			}

			throw new Exception($"The [{memberValue}] cache mode is not supported");
		}

		private static string DoubleEscape(string thisString)
		{
			//http://stackoverflow.com/questions/366124/inserting-a-tab-character-into-text-using-c-sharp
			return thisString
				.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\t", "\\t");
		}

		private static string ParseTimeSpan(string memberValue)
		{
			var value = TimeSpan.Parse(memberValue);

			return $"global::System.TimeSpan.FromTicks({value.Ticks} /* {memberValue} */)";
		}

		private static string BuildGridLength(string memberValue)
		{
			var gridLength = Windows.UI.Xaml.GridLength.ParseGridLength(memberValue).FirstOrDefault();

			return $"new {XamlConstants.Types.GridLength}({gridLength.Value.ToStringInvariant()}f, {XamlConstants.Types.GridUnitType}.{gridLength.GridUnitType})";
		}

		private string BuildLiteralValue(XamlMemberDefinition member, INamedTypeSymbol propertyType = null, XamlMemberDefinition owner = null, string objectUid = "")
		{
			if (member.Objects.None())
			{
				var memberValue = member.Value?.ToString();

				propertyType = propertyType ?? FindPropertyType(member.Member);

				if (propertyType != null)
				{
					return BuildLiteralValue(propertyType, memberValue, owner, member.Member.Name, objectUid);
				}
				else
				{
					throw new Exception($"The property {member.Owner?.Type?.Name}.{member.Member?.Name} is unknown".InvariantCultureFormat(member.Member?.Name));
				}
			}
			else
			{
				var expression = member.Objects.First();

				if (expression.Type.Name == "StaticResource" || expression.Type.Name == "ThemeResource")
				{
					return GetGlobalStaticResource(expression.Members.First().Value?.ToString());
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

		private string BuildFontWeight(string memberValue)
		{
			var fontWeights = (INamedTypeSymbol)_medataHelper.GetTypeByFullName(XamlConstants.Types.FontWeights);

			if (fontWeights.GetFields().Any(m => m.Name.Equals(memberValue, StringComparison.OrdinalIgnoreCase)))
			{
				return "FontWeights." + memberValue;
			}
			else
			{
				return "FontWeights.Normal /* Warning {0} is not supported on this platform */".InvariantCultureFormat(memberValue);
			}
		}

		private string BuildBrush(string memberValue)
		{
			var colorHelper = (INamedTypeSymbol)_medataHelper.GetTypeByFullName(XamlConstants.Types.SolidColorBrushHelper);

			// This ensures that a memberValue "DarkGoldenRod" gets converted to colorName "DarkGoldenrod" (notice the lowercase 'r')
			var colorName = colorHelper.GetProperties().FirstOrDefault(m => m.Name.Equals(memberValue, StringComparison.OrdinalIgnoreCase))?.Name;
			if (colorName != null)
			{
				return "SolidColorBrushHelper." + colorName;
			}
			else
			{
				memberValue = string.Join(", ", ColorCodeParser.ParseColorCode(memberValue));

				return "SolidColorBrushHelper.FromARGB({0})".InvariantCultureFormat(memberValue);
			}
		}

		private static string BuildThickness(string memberValue)
		{
			// This is until we find an appropriate way to convert strings to Thickness.
			if (!memberValue.Contains(","))
			{
				memberValue = memberValue.Replace(" ", ",");
			}

			if (memberValue.Contains("."))
			{
				// Append 'f' to every decimal value in the thickness
				memberValue = AppendFloatSuffix(memberValue);
			}

			return "new global::Windows.UI.Xaml.Thickness(" + memberValue + ")";
		}

		private static string AppendFloatSuffix(string memberValue)
		{
			return memberValue.Split(',')
					.Select(s => s.Contains(".") ? s + "f" : s)
					.Aggregate((s1, s2) => "{0},{1}".InvariantCultureFormat(s1, s2));
		}

		private string BuildColor(string memberValue)
		{
			var colorHelper = (INamedTypeSymbol)_medataHelper.GetTypeByFullName(XamlConstants.Types.Colors);

			if (colorHelper.GetFields().Any(m => m.Name.Equals(memberValue, StringComparison.OrdinalIgnoreCase)))
			{
				return $"{XamlConstants.Types.Colors}.{memberValue}";
			}
			else
			{
				memberValue = string.Join(", ", ColorCodeParser.ParseColorCode(memberValue));

				return "Windows.UI.ColorHelper.FromARGB({0})".InvariantCultureFormat(memberValue);
			}
		}

		private INamedTypeSymbol GetPropertyType(XamlMember xamlMember)
		{
			var definition = FindPropertyType(xamlMember);

			if (definition == null)
			{
				throw new Exception($"The property {xamlMember.Type?.Name}.{xamlMember.Name} is unknown");
			}

			return definition;
		}

		private INamedTypeSymbol GetPropertyType(string ownerType, string propertyName)
		{
			var definition = FindPropertyType(ownerType, propertyName);

			if (definition == null)
			{
				throw new Exception("The property {0}.{1} is unknown".InvariantCultureFormat(ownerType, propertyName));
			}

			return definition;
		}

		private INamedTypeSymbol FindPropertyType(XamlMember xamlMember) => _findPropertyTypeByXamlMember(xamlMember);

		private INamedTypeSymbol SourceFindPropertyType(XamlMember xamlMember)
		{
			// Search for the type the clr namespaces registered with the xml namespace
			if (xamlMember.DeclaringType != null)
			{
				var clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(xamlMember.DeclaringType.PreferredXamlNamespace, new string[0]);

				foreach (var clrNamespace in clrNamespaces)
				{
					string declaringTypeName = xamlMember.DeclaringType.Name;

					var propertyType = FindPropertyType(clrNamespace + "." + declaringTypeName, xamlMember.Name);

					if (propertyType != null)
					{
						return propertyType;
					}
				}
			}

			var type = FindType(xamlMember.DeclaringType);

			// If not, try to find the closest match using the name only.
			return FindPropertyType(type.SelectOrDefault(t => t.ToDisplayString(), "$$unknown"), xamlMember.Name);
		}

		private INamedTypeSymbol FindPropertyType(string ownerType, string propertyName) => _findPropertyTypeByName(ownerType, propertyName);

		private INamedTypeSymbol SourceFindPropertyType(string ownerType, string propertyName)
		{
			var type = FindType(ownerType);

			if (type != null)
			{
				do
				{
					if (type.Kind == SymbolKind.ErrorType)
					{
						throw new InvalidOperationException($"Unable to resolve {type} (SymbolKind is ErrorType) {type}");
					}

					var resolvedType = type;

					var property = resolvedType.GetAllProperties().FirstOrDefault(p => p.Name == propertyName);
					var setMethod = resolvedType.GetMethods().FirstOrDefault(p => p.Name == "Set" + propertyName);

					if (property != null)
					{
						if (property.Type.OriginalDefinition != null
							&& property.Type.OriginalDefinition?.ToDisplayString() == "System.Nullable<T>")
						{
							//TODO
							return (property.Type as INamedTypeSymbol).TypeArguments.First() as INamedTypeSymbol;
						}
						else
						{
							var finalType = property.Type as INamedTypeSymbol;

							if (finalType == null)
							{
								return FindType(property.Type.ToDisplayString());
							}

							return finalType;
						}
					}
					else
					{
						if (setMethod != null)
						{
							return setMethod.Parameters.ElementAt(1).Type as INamedTypeSymbol;
						}
						else
						{
							var baseType = type.BaseType;

							if (baseType == null)
							{
								baseType = FindType(type.BaseType.ToDisplayString());
							}

							type = baseType;

							if (type == null || type.Name == "Object")
							{
								return null;
							}
						}
					}
				} while (true);
			}
			else
			{
				return null;
			}
		}

		private IEventSymbol FindEventType(XamlMember xamlMember)
		{
			return _findEventType(xamlMember);
		}

		private IEventSymbol SourceFindEventType(XamlMember xamlMember)
		{
			// Search for the type the clr namespaces registered with the xml namespace
			if (xamlMember.DeclaringType != null)
			{
				var clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(xamlMember.DeclaringType.PreferredXamlNamespace, new string[0]);

				foreach (var clrNamespace in clrNamespaces)
				{
					var propertyType = FindEventType(clrNamespace + "." + xamlMember.DeclaringType.Name, xamlMember.Name);

					if (propertyType != null)
					{
						return propertyType;
					}
				}
			}

			var type = FindType(xamlMember.DeclaringType);

			// If not, try to find the closest match using the name only.
			return FindEventType(type.SelectOrDefault(t => t.ToDisplayString(), "$$unknown"), xamlMember.Name);
		}

		private IEventSymbol FindEventType(string ownerType, string eventName)
		{
			var type = FindType(ownerType);

			if (type != null)
			{
				do
				{
					if (type.Kind == SymbolKind.ErrorType)
					{
						throw new InvalidOperationException($"Unable to resolve {type} (SymbolKind is ErrorType)");
					}

					var resolvedType = type;

					var eventSymbol = resolvedType.GetAllEvents().FirstOrDefault(p => p.Name == eventName);

					if (eventSymbol != null)
					{
						return eventSymbol;
					}
					else
					{
						var baseType = type.BaseType;

						if (baseType == null)
						{
							baseType = FindType(type.BaseType.ToDisplayString());
						}

						type = baseType;

						if (type == null || type.Name == "Object")
						{
							return null;
						}
					}
				}
				while (true);
			}
			else
			{
				return null;
			}
		}

		private string BuildBindingOption(XamlMemberDefinition m, INamedTypeSymbol propertyType, bool prependCastToType)
		{
			// The default member is Path
			var memberName = m.Member.Name == "_PositionalParameters" ? "Path" : m.Member.Name;

			if (m.Objects.Any())
			{
				var bindingType = m.Objects.First();

				if (
					bindingType.Type.Name == "StaticResource"
					|| bindingType.Type.Name == "ThemeResource"
					)
				{
					var resourceName = bindingType.Members.First().Value.ToString();
					if (_staticResources.ContainsKey(resourceName))
					{
						return "{0}StaticResources.{1}".InvariantCultureFormat(
							GetCastString(prependCastToType ? propertyType : null, _staticResources[resourceName]),
							resourceName);
					}
					else if (_namedResources.ContainsKey(resourceName))
					{
						return resourceName;
					}
					else
					{
						return "{0}{1}".InvariantCultureFormat(
							GetCastString(prependCastToType ? propertyType : null, null),
							GetGlobalStaticResource(resourceName)
						);
					}
				}

				if (bindingType.Type.Name == "RelativeSource")
				{
					var resourceName = bindingType.Members.First().Value.ToString();

					return "new RelativeSource(RelativeSourceMode.TemplatedParent)";
				}

				return "Unsupported";
			}
			else
			{

				var value = BuildLiteralValue(m, GetPropertyType("Binding", memberName));

				if (memberName == "Path")
				{
					value = RewriteAttachedPropertyPath(value);
				}
				else if (memberName == "ElementName")
				{
					// Skip the literal value, use the elementNameSubject instead
					string elementName = m.Value.ToString();
					value = "_" + elementName + "Subject";
					// Track referenced ElementNames
					CurrentScope.ReferencedElementNames.Add(elementName);
				}
				else if (memberName == "FallbackValue"
					|| (memberName == "TargetNullValue"
						&& m.Owner.Members.None(otherMember => otherMember.Member.Name == "Converter")))
				{
					// FallbackValue can match the type of the property being bound.
					// TargetNullValue possibly doesn't match the type of the property being bound,
					// if there is a converter
					value = BuildLiteralValue(m, propertyType);
				}


				return value;
			}
		}

		private string GetGlobalStaticResource(string resourceName)
		{
			var resource = _globalStaticResourcesMap.FindResource(resourceName);
			if (resource != null)
			{
				return $"global::{resource.Namespace}.GlobalStaticResources.{SanitizeResourceName(resourceName)}";
			}
			else
			{
				return $"global::Windows.UI.Xaml.Application.Current.Resources[\"{resourceName}\"]"; 
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

							var parts = match.Value.Trim(new[] { '(', ')' }).Split('.');

							if (parts.Length == 2)
							{
								var targetType = SourceFindType(parts[0]);

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

		private void BuildLiteralProperties(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, string closureName = null)
		{
			var closingPunctuation = string.IsNullOrWhiteSpace(closureName) ? "," : ";";

			BuildStyleProperty(writer, objectDefinition, closingPunctuation);

			var extendedProperties = GetExtendedProperties(objectDefinition);

			var objectUid = GetObjectUid(objectDefinition);

			if (extendedProperties.Any())
			{
				foreach (var member in extendedProperties)
				{
					var fullValueSetter = string.IsNullOrWhiteSpace(closureName) ? member.Member.Name : "{0}.{1}".InvariantCultureFormat(closureName, member.Member.Name);

					// Exclude attached properties, must be set in the extended apply section.
					// If there is no type attached, this can be a binding.
					if (IsType(objectDefinition.Type, member.Member.DeclaringType)
						&& !IsAttachedProperty(member)
						&& FindEventType(member.Member) == null
						&& member.Member.Name != "_UnknownContent" // We are defining the elements of a collection explicitly declared in XAML
					)
					{
						if (member.Objects.None())
						{
							if (IsInitializableCollection(member.Member))
							{
								writer.AppendLineInvariant("// Empty collection");
							}
							else
							{
								if (FindPropertyType(member.Member) != null)
								{
									writer.AppendLineInvariant("{0} = {1}{2}", fullValueSetter, BuildLiteralValue(member, objectUid: objectUid), closingPunctuation);
								}
								else
								{
									if(IsRelevantNamespace(member?.Member?.PreferredXamlNamespace) 
										&& IsRelevantProperty(member?.Member))
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
									&& m.Type.Name != "TemplateBinding"
								);

							if (nonBindingObjects.Any())
							{
								var isInitializableCollection = IsInitializableCollection(member.Member);
								var isInlineCollection = IsInlineCollection(member.Member, nonBindingObjects);

								if (isInitializableCollection || isInlineCollection)
								{
									using (writer.BlockInvariant($"{fullValueSetter} = "))
									{
										foreach (var child in nonBindingObjects)
										{
											BuildChild(writer, member, child);											
											writer.AppendLineInvariant(",");
										}
									}
								}
								else
								{
									writer.AppendFormatInvariant($"{fullValueSetter} = ");
									BuildChild(writer, member, nonBindingObjects.First());
								}
								
								writer.AppendLineInvariant(closingPunctuation);
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
							writer.AppendLineInvariant("{0} = \"{1}\"{2}", fullValueSetter, member.Value, closingPunctuation);
						}
					}
				}
			}
		}

		/// <summary>
		/// Determines if the member is inline initializable and the first item is not a new collection instance
		/// </summary>
		private bool IsInlineCollection(XamlMember member, IEnumerable<XamlObjectDefinition> elements)
		{
			var declaringType = member.DeclaringType;
			var propertyName = member.Name;
			var property = GetPropertyByName(declaringType, propertyName);

			if (property?.Type is INamedTypeSymbol collectionType)
			{
				if (IsCollectionOrListType(collectionType) && elements.FirstOrDefault() is XamlObjectDefinition first)
				{
					var firstElementType = GetType(first.Type);

					return collectionType != firstElementType;
				}
			}

			return false;
		}

		private void BuildStyleProperty(IIndentedStringBuilder writer, XamlObjectDefinition objectDefinition, string closingPunctuation)
		{
			var styleMember = FindMember(objectDefinition, "Style");

			if (styleMember != null)
			{
				// Explicitly search for StaticResource, as the style could be set using a binding.
				if (styleMember.Objects.Any(o => o.Type.Name == "StaticResource"))
				{
					BuildComplexPropertyValue(writer, styleMember, "");
					writer.AppendLineInvariant(0, closingPunctuation);
				}
			}
		}

		private bool IsLocalizedString(INamedTypeSymbol propertyType, string objectUid)
		{
			return objectUid.HasValue()
				&& (propertyType.ToDisplayString() == "string"
				|| propertyType.ToDisplayString() == "object");
		}

		private string GetObjectUid(XamlObjectDefinition objectDefinition)
		{
			string objectUid = null;
			var localizedObject = FindMember(objectDefinition, "Uid");
			if (localizedObject != null)
			{
				objectUid = localizedObject.Value.ToString();
			}

			return objectUid;
		}

		private bool IsAttachedProperty(XamlMemberDefinition member)
		{
			if (member.Member.DeclaringType != null)
			{
				var type = FindType(member.Member.DeclaringType);

				if (type != null)
				{
					return _isAttachedProperty(type, member.Member.Name);
				}
			}

			return false;
		}

		private bool IsRelevantNamespace(string xamlNamespace)
		{
			if(XamlConstants.XmlXmlNamespace.Equals(xamlNamespace, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return true;
		}

		private bool IsRelevantProperty(XamlMember member)
		{
			if (member?.Name == "Phase") // Phase is not relevant as it's not an actual property
			{
				return false;
			}

			return true;
		}

		private static bool SourceIsAttachedProperty(INamedTypeSymbol type, string name)
		{
			do
			{
				var property = type.GetAllProperties().FirstOrDefault(p => p.Name == name);
				var setMethod = type.GetMethods().FirstOrDefault(p => p.Name == "Set" + name);

				if (property != null && property.GetMethod.IsStatic)
				{
					return true;
				}

				if (setMethod != null && setMethod.IsStatic)
				{
					return true;
				}

				type = type.BaseType;

				if (type == null || type.Name == "Object")
				{
					return false;
				}

			} while (true);
		}


		/// <summary>
		/// Determines if the provided member is an C# initializable list (where the collection already exists, and no set property is present)
		/// </summary>
		/// <param name="xamlMember"></param>
		/// <returns></returns>
		private bool IsInitializableCollection(XamlMember xamlMember)
		{
			var declaringType = xamlMember.DeclaringType;
			var propertyName = xamlMember.Name;

			return IsInitializableCollection(declaringType, propertyName);
		}

		/// <summary>
		/// Determines if the provided member is an C# initializable list (where the collection already exists, and no set property is present)
		/// </summary>
		private bool IsInitializableCollection(XamlType declaringType, string propertyName)
		{
			var property = GetPropertyByName(declaringType, propertyName);

			if (property != null && IsInitializableProperty(property))
			{
				return IsCollectionOrListType(property.Type as INamedTypeSymbol);
			}

			return false;
		}

		/// <summary>
		/// Returns true if the property does not have a accessible setter
		/// </summary>
		private bool IsInitializableProperty(IPropertySymbol property)
		{
			return !property.SetMethod.SelectOrDefault(m => m.DeclaredAccessibility == Accessibility.Public, false);
		}

		/// <summary>
		/// Returns true if the property has an accessible public setter and has a parameterless constructor
		/// </summary>
		private bool IsNewableProperty(IPropertySymbol property, out string newableTypeName)
		{
			var namedType = property.Type as INamedTypeSymbol;

			var isNewable = property.SetMethod.SelectOrDefault(m => m.DeclaredAccessibility == Accessibility.Public, false) &&
				namedType.SelectOrDefault(nts => nts.Constructors.Any(ms => ms.Parameters.Length == 0), false);

			newableTypeName = isNewable ? GetFullGenericTypeName(namedType) : null;

			return isNewable;
		}

		/// <summary>
		/// Returns true if the type implements either ICollection, IList or one of their generics
		/// </summary>
		private bool IsCollectionOrListType(INamedTypeSymbol propertyType)
		{
			return IsImplementingInterface(propertyType, "System.Collections.ICollection") ||
				IsImplementingInterface(propertyType, "System.Collections.Generic.ICollection<T>") ||
				IsImplementingInterface(propertyType, "System.Collections.IList") ||
				IsImplementingInterface(propertyType, "System.Collections.Generic.IList<T>");
		}

		/// <summary>
		/// Returns true if the type exactly implements either ICollection, IList or one of their generics
		/// </summary>
		private bool IsExactlyCollectionOrListType(INamedTypeSymbol type)
		{
			return type.ToDisplayString() == "System.Collections.ICollection"
				|| type.OriginalDefinition.ToDisplayString() == "System.Collections.ICollection<T>"
				|| type.ToDisplayString() == "System.Collections.IList"
				|| type.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IList<T>";
		}

		/// <summary>
		/// Determines if the provided object definition is of a C# initializable list
		/// </summary>
		private bool IsInitializableCollection(XamlObjectDefinition definition)
		{
			if (definition.Members.Any(m => m.Member.Name != "_UnknownContent"))
			{
				return false;
			}

			var type = FindType(definition.Type);
			if (type == null)
			{
				return false;
			}

			return IsImplementingInterface(type, "System.Collections.ICollection")
				|| IsImplementingInterface(type, "System.Collections.Generic.ICollection<T>");
		}

		/// <summary>
		/// Gets the 
		/// </summary>
		private IPropertySymbol GetPropertyByName(XamlType declaringType, string propertyName)
		{
			var type = FindType(declaringType);

			return type?.GetAllProperties().FirstOrDefault(p => p.Name == propertyName);
		}

		/// <summary>
		/// Gets a basic class that implements a know collection or list interface
		/// </summary>
		private bool GetKnownNewableListOrCollectionInterface(INamedTypeSymbol type, out string newableTypeName)
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
						
						// Style requires a special treatment, as it needs to be set before anything else in Literal Properties
						&& m.Member.Name != "Style"

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
					||
					(
						// If the object is a collection and it has both _UnknownContent (i.e. an item) and other properties defined,
						// we cannot use property ADN collection initializer syntax at same time, so we will add items using an ApplyBlock.
						m.Member.Name == "_UnknownContent" && !hasUnkownContentOnly && FindType(m.Owner.Type) is INamedTypeSymbol type && IsCollectionOrListType(type)
					)
				);
		}

		private void BuildChild(IIndentedStringBuilder writer, XamlMemberDefinition owner, XamlObjectDefinition xamlObjectDefinition)
		{
			var typeName = xamlObjectDefinition.Type.Name;
			var fullTypeName = xamlObjectDefinition.Type.Name;

			var knownType = FindType(xamlObjectDefinition.Type);

			if (knownType == null && xamlObjectDefinition.Type.PreferredXamlNamespace.StartsWith("using:"))
			{
				fullTypeName = xamlObjectDefinition.Type.PreferredXamlNamespace.TrimStart("using:") + "." + xamlObjectDefinition.Type.Name;
			}

			if (knownType != null)
			{
				// Override the using with the type that was found in the list of loaded assemblies
				fullTypeName = knownType.ToDisplayString();
			}

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
				writer.AppendFormatInvariant("new {0}(() => ", GetGlobalizedTypeName(fullTypeName));

				var contentOwner = xamlObjectDefinition.Members.FirstOrDefault(m => m.Member.Name == "_UnknownContent");

				if (contentOwner != null)
				{
					// This case is to support the layout switching for the ListViewBaseLayout, which is not 
					// a FrameworkTemplate. Thsi will need to be removed when this custom list view is removed.
					var returnType = typeName == "ListViewBaseLayoutTemplate" ? "Uno.UI.Controls.Legacy.ListViewBaseLayout" : "_View";

					BuildChildThroughSubclass(writer, contentOwner, returnType);

					writer.AppendFormatInvariant(")");
				}
				else
				{
					writer.AppendFormatInvariant("/* This template does not have a content for this platform */");
				}
			}
			else if (typeName == "NullExtension")
			{
				writer.AppendFormatInvariant("null");
			}
			else if (
				typeName == "StaticResource"
				|| typeName == "ThemeResource"
				|| typeName == "Binding"
				|| typeName == "Bind"
				|| typeName == "TemplateBinding"
				)
			{
				BuildComplexPropertyValue(writer, owner, "c.");
			}
			else if (IsDirectUserControlSubType(xamlObjectDefinition) && HasNoUserControlProperties(xamlObjectDefinition))
			{
				writer.AppendLineInvariant("new {0}(skipsInitializeComponents: true).GetContent()", GetGlobalizedTypeName(fullTypeName));

				using (var innerWriter = CreateApplyBlock(writer, null, out var closureName))
				{
					BuildLiteralProperties(innerWriter, xamlObjectDefinition, closureName);
					BuildProperties(innerWriter, xamlObjectDefinition, closureName: closureName);
				}

				BuildExtendedProperties(writer, xamlObjectDefinition, useGenericApply: true);
			}
			else if (HasInitializer(xamlObjectDefinition))
			{
				BuildInitializer(writer, xamlObjectDefinition, owner);
				BuildLiteralProperties(writer, xamlObjectDefinition);
			}
			else if (fullTypeName == XamlConstants.Types.Style)
			{
				BuildInlineStyle(writer, xamlObjectDefinition);
			}
			else if (fullTypeName == XamlConstants.Types.Setter)
			{
				var propertyNode = FindMember(xamlObjectDefinition, "Property");
				var targetNode = FindMember(xamlObjectDefinition, "Target");
				var valueNode = FindMember(xamlObjectDefinition, "Value");

				var property = propertyNode?.Value.SelectOrDefault(p => p.ToString());
				var target = targetNode?.Value.SelectOrDefault(p => p.ToString());

				if (property != null)
				{
					var value = BuildLiteralValue(valueNode);

					// This builds property setters for the owner of the setter.
					writer.AppendLineInvariant($"new Windows.UI.Xaml.Setter(new Windows.UI.Xaml.TargetPropertyPath(this, \"{property}\"), {value})");
				}
				else if (target != null)
				{
					// This builds property setters for specified member setter.
					var separatorIndex = target.IndexOf(".");
					var elementName = target.Substring(0, separatorIndex);
					var propertyName = target.Substring(separatorIndex + 1);

					var ownerControl = GetControlOwner(owner.Owner);

					string value = BuildLiteralValue(valueNode);

					if (ownerControl != null)
					{
						var targetElement = FindSubElementByName(ownerControl, elementName);

						if (targetElement != null)
						{
							var targetElementType = GetType(targetElement.Type);
							var propertyType = FindPropertyType(targetElementType.ToString(), propertyName);

							if (propertyType != null)
							{
								if (HasMarkupExtension(valueNode))
								{
									value = GetStaticResourceName(valueNode, propertyType);
								}
								else
								{
									value = BuildLiteralValue(valueNode, propertyType);
								}
							}
						}
					}

					// Attached properties need to be expanded using the namespace, otherwise the resolution will be 
					// performed at runtime at a higher cost.
					propertyName = RewriteAttachedPropertyPath(propertyName);

					writer.AppendLineInvariant($"new Windows.UI.Xaml.Setter(new Windows.UI.Xaml.TargetPropertyPath({elementName}, \"{propertyName}\"), {value.Replace("{", "{{").Replace("}", "}}")})");
				}
			}
			else
			{
				var hasCustomInitalizer = HasCustomInitializer(xamlObjectDefinition);

				if (hasCustomInitalizer)
				{
					var propertyType = FindType(xamlObjectDefinition.Type);
					writer.AppendFormatInvariant(BuildLiteralValue(FindImplicitContentMember(xamlObjectDefinition), propertyType, owner));
				}
				else
				{
					using (TryGenerateDeferedLoadStrategy(writer, knownType, xamlObjectDefinition))
					{
						using (writer.BlockInvariant("new {0}{1}", GetGlobalizedTypeName(fullTypeName), GenerateConstructorParameters(xamlObjectDefinition.Type)))
						{
							BuildLiteralProperties(writer, xamlObjectDefinition);
							BuildProperties(writer, xamlObjectDefinition);
						}

						BuildExtendedProperties(writer, xamlObjectDefinition);
					}
				}
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

		private string GenerateRootPhases(XamlObjectDefinition xamlObjectDefinition, string ownerVariable)
		{
			if (xamlObjectDefinition.Owner?.Type.Name == "DataTemplate")
			{
				var q = from element in EnumerateSubElements(xamlObjectDefinition.Owner)
						let phase = FindMember(element, "Phase")?.Value
						where phase != null
						select int.Parse(phase.ToString());

				var phases = q.Distinct().ToArray();

				if (phases.Any())
				{
					var phasesValue = phases.OrderBy(i => i).Select(s => s.ToString()).JoinBy(",");
					return $"Uno.UI.FrameworkElementHelper.SetDataTemplateRenderPhases({ownerVariable}, new []{{{phasesValue}}});";
				}
			}

			return null;
		}

		private bool HasNoUserControlProperties(XamlObjectDefinition objectDefinition)
		{
			return
			objectDefinition
				.Members
				.Where(m =>
					(
						m.Member.Name != "DataContext"
						&& m.Member.Name != "_UnknownContent"
					)
				)
				.None();
		}

		private XamlObjectDefinition GetControlOwner(XamlObjectDefinition owner)
		{
			do
			{
				var type = GetType(owner.Type);

				if (type.Is(XamlConstants.Types.Control))
				{
					return owner;
				}

				owner = owner.Owner;
			}
			while (owner != null);

			return null;
		}


		/// <summary>
		/// Statically finds a element by name, given a xaml element root
		/// </summary>
		/// <param name="xamlObject">The root from which to start the search</param>
		/// <param name="elementName">The x:Name value to search for</param>
		/// <returns></returns>
		private XamlObjectDefinition FindSubElementByName(XamlObjectDefinition xamlObject, string elementName)
		{
			foreach (var element in EnumerateSubElements(xamlObject))
			{
				var nameMember = FindMember(element, "Name");

				if (nameMember?.Value?.ToString() == elementName)
				{
					return element;
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

		private IDisposable TryGenerateDeferedLoadStrategy(IIndentedStringBuilder writer, INamedTypeSymbol targetType, XamlObjectDefinition definition)
		{
			var strategy = FindMember(definition, "DeferLoadStrategy");

			
			if (!_isWasm && strategy?.Value?.ToString() == "Lazy")
			{
				var visibilityMember = FindMember(definition, "Visibility");
				var dataContextMember = FindMember(definition, "DataContext");
				var nameMember = FindMember(definition, "Name");

				if (!targetType.Is(_elementStubSymbol.BaseType))
				{
					writer.AppendLineInvariant($"/* Lazy DeferLoadStrategy was ignored because the target type is not based on {_elementStubSymbol.BaseType} */");
					return null;
				}

				if (visibilityMember != null)
				{
					var hasVisibilityMarkup = HasMarkupExtension(visibilityMember);
					var isLiteralVisible = !hasVisibilityMarkup && (visibilityMember.Value?.ToString() == "Visible");

					var hasDataContextMarkup = dataContextMember != null && HasMarkupExtension(dataContextMember);

					var disposable = writer.BlockInvariant($"new {XamlConstants.Types.ElementStub}()");

					writer.Append("ContentBuilder = () => ");

					return new DisposableAction(() =>
						{
							disposable.Dispose();

							string closureName;
							using (var innerWriter = CreateApplyBlock(writer, GetType(XamlConstants.Types.ElementStub), out closureName))
							{
								if (hasDataContextMarkup)
								{
									// We need to generate the datacontext binding, since the Visibility
									// may require it to bind properly.

									var def = new XamlMemberDefinition(
										new XamlMember("DataContext",
											new XamlType("", "ElementStub", new List<XamlType>(), new XamlSchemaContext()),
											false
										), 0, 0
									);

									def.Objects.AddRange(dataContextMember.Objects);

									BuildComplexPropertyValue(innerWriter, def, closureName + ".", closureName);
								}

								if (hasVisibilityMarkup)
								{
									var def = new XamlMemberDefinition(
										new XamlMember("Visibility",
											new XamlType("", "ElementStub", new List<XamlType>(), new XamlSchemaContext()),
											false
										), 0, 0
									);

									def.Objects.AddRange(visibilityMember.Objects);

									BuildComplexPropertyValue(innerWriter, def, closureName + ".", closureName);
								}
								else
								{
									innerWriter.AppendLineInvariant(
										"{0}.Visibility = {1};",
										closureName,
										BuildLiteralValue(visibilityMember)
									);

									if (nameMember != null)
									{
										innerWriter.AppendLineInvariant(
											$"{closureName}.Name = \"{nameMember.Value}\";"
										);

										// Set the element name to the stub, then when the stub will be replaced
										// the actual target control will override it.
										innerWriter.AppendLineInvariant(
											$"_{nameMember.Value}Subject.ElementInstance = {closureName};"
										);
									}
								}
							}
						}
					);
				}
			}

			return null;
		}

		private void BuildChildThroughSubclass(IIndentedStringBuilder writer, XamlMemberDefinition contentOwner, string returnType)
		{
			// To prevent conflicting names whenever we are working with dictionaries, subClass index is a Guid in those cases
			var subClassPrefix = _scopeStack.Aggregate("", (val, scope) => val + scope.Name);

			var namespacePrefix = _scopeStack.Count == 1 && _scopeStack.Last().Name.EndsWith("RD") ? "__Resources." : "";

			var subclassName = $"_{_fileUniqueId}_{subClassPrefix}SC{(_subclassIndex++).ToString(CultureInfo.InvariantCulture)}";

			RegisterChildSubclass(subclassName, contentOwner, returnType);

			writer.AppendLineInvariant($"new {namespacePrefix}{subclassName}().Build()");
		}

		private string GenerateConstructorParameters(XamlType xamlType)
		{
			if (IsType(xamlType, "Android.Views.View"))
			{
				// For android, all native control must take a context as their first parameters
				// To be able to use this control from the Xaml, we need to generate a constructor
				// call that takes the ContextHelper.Current as the first parameter.

				var type = FindType(xamlType);

				if (type != null)
				{
					var q = from m in type.Constructors
							where m.Parameters.Count() == 1
							where m.Parameters.First().Type.ToDisplayString() == "Android.Content.Context"
							select m;

					var hasContextConstructor = q.Any();

					if (hasContextConstructor)
					{
						return "(Uno.UI.ContextHelper.Current)";
					}
				}
			}

			return "";
		}

		private bool HasCustomInitializer(XamlObjectDefinition definition)
		{
			var propertyType = FindType(definition.Type);

			if (propertyType != null)
			{
				if (propertyType.TypeKind == TypeKind.Enum)
				{
					return true;
				}

				switch (propertyType.ToDisplayString())
				{
					case "int":
					case "float":
					case "long":
					case "short":
					case "byte":
					case "double":
					case "string":
					case "bool":
					case XamlConstants.Types.Thickness:
					case XamlConstants.Types.FontFamily:
					case XamlConstants.Types.FontWeight:
					case XamlConstants.Types.GridLength:
					case XamlConstants.Types.CornerRadius:
					case "UIKit.UIColor":
					case "Windows.UI.Color":
					case "Color":
						return true;
				}
			}

			return false;
		}

		private void BuildInitializer(IIndentedStringBuilder writer, XamlObjectDefinition xamlObjectDefinition, XamlMemberDefinition owner = null)
		{
			var initializer = xamlObjectDefinition.Members.First(m => m.Member.Name == "_Initialization");

			if (IsPrimitive(xamlObjectDefinition))
			{
				if (xamlObjectDefinition.Type.Name == "Double" || xamlObjectDefinition.Type.Name == "Single")
				{
					writer.AppendFormatInvariant("{0}", GetFloatingPointLiteral(initializer.Value.ToString(), GetType(xamlObjectDefinition.Type), owner));
				}
				else if (xamlObjectDefinition.Type.Name == "Boolean")
				{
					writer.AppendFormatInvariant("{1}", xamlObjectDefinition.Type.Name, initializer.Value.ToString().ToLowerInvariant());
				}
				else
				{
					writer.AppendFormatInvariant("{1}", xamlObjectDefinition.Type.Name, initializer.Value);
				}
			}
			else if (IsString(xamlObjectDefinition))
			{
				writer.AppendFormatInvariant("\"{1}\"", xamlObjectDefinition.Type.Name, DoubleEscape(initializer.Value?.ToString()));
			}
			else
			{
				writer.AppendFormatInvariant("new {0}({1})", GetGlobalizedTypeName(xamlObjectDefinition.Type.Name), initializer.Value);
			}
		}

		private string GetFloatingPointLiteral(string memberValue, INamedTypeSymbol type, XamlMemberDefinition owner)
		{
			var name = ValidatePropertyType(type, owner);

			var isDouble = IsDouble(name);

			if (memberValue.EndsWith("nan", StringComparison.OrdinalIgnoreCase) || memberValue.Equals("auto", StringComparison.OrdinalIgnoreCase))
			{
				return "{0}.NaN".InvariantCultureFormat(isDouble ? "double" : "float");
			}
			else if (memberValue.ToLowerInvariant().EndsWith("positiveinfinity"))
			{
				return "{0}.PositiveInfinity".InvariantCultureFormat(isDouble ? "double" : "float");
			}
			else if (memberValue.ToLowerInvariant().EndsWith("negativeinfinity"))
			{
				return "{0}.NegativeInfinity".InvariantCultureFormat(isDouble ? "double" : "float");
			}
			else if (memberValue.ToLowerInvariant().EndsWith("px"))
			{
				this.Log().Warn($"The value [{memberValue}] is specified in pixel and is not yet supported. ({owner?.LineNumber}, {owner?.LinePosition} in {_fileDefinition.FilePath})");
				return "{0}{1}".InvariantCultureFormat(memberValue.TrimEnd("px"), isDouble ? "d" : "f");
			}

			return "{0}{1}".InvariantCultureFormat(memberValue, isDouble ? "d" : "f");
		}

		private string ValidatePropertyType(INamedTypeSymbol propertyType, XamlMemberDefinition owner)
		{
			if (IsDouble(propertyType.ToDisplayString()) &&
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

			return propertyType.ToDisplayString();
		}

		private static bool IsDouble(string typeName)
		{
			// handles cases where name is "Java.Lang.Double"
			// TODO: Fix type resolution
			return typeName.Equals("double", StringComparison.InvariantCultureIgnoreCase)
				|| typeName.EndsWith(".double", StringComparison.InvariantCultureIgnoreCase);
		}

		private bool IsString(XamlObjectDefinition xamlObjectDefinition)
		{
			return xamlObjectDefinition.Type.Name == "String";
		}

		private bool IsPrimitive(XamlObjectDefinition xamlObjectDefinition)
		{
			switch (xamlObjectDefinition.Type.Name)
			{
				case "Double":
				case "Int32":
				case "Int16":
				case "Single":
				case "Boolean":
					return true;
			}

			return false;
		}

		private bool HasInitializer(XamlObjectDefinition objectDefinition)
		{
			return objectDefinition.Members.Any(m => m.Member.Name == "_Initialization");
		}

		private bool IsDirectUserControlSubType(XamlObjectDefinition objectDefinition)
		{
			return string.Equals(
				FindType(objectDefinition.Type)
					?.BaseType
					?.ToDisplayString(),
				XamlConstants.Types.UserControl);
		}

		private INamedTypeSymbol FindType(string name)
		{
			return _findType(name);
		}

		private INamedTypeSymbol FindType(XamlType type)
		{
			if (type != null)
			{
				var ns = _fileDefinition.Namespaces.FirstOrDefault(n => n.Namespace == type.PreferredXamlNamespace);
				var isKnownNamespace = ns?.Prefix?.HasValue() ?? false;

				if (
					type.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace
					&& type.Name == "Bind"
				   )
				{
					return _findType(XamlConstants.Namespaces.Data + ".Binding");
				}

				var fullName = isKnownNamespace ? ns.Prefix + ":" + type.Name : type.Name;

				return _findType(fullName);
			}
			else
			{
				return null;
			}
		}

		private INamedTypeSymbol GetType(string name)
		{
			var type = _findType(name);

			if (type == null)
			{
				throw new InvalidOperationException("The type {0} could not be found".InvariantCultureFormat(name));
			}

			return type;
		}

		private INamedTypeSymbol GetType(XamlType type)
		{
			var clrType = FindType(type);

			if (clrType == null)
			{
				throw new InvalidOperationException("The type {0} could not be found".InvariantCultureFormat(type));
			}

			return clrType;
		}

		private INamedTypeSymbol SourceFindType(string name)
		{
			var originalName = name;

			if (name.Contains(":"))
			{
				var fields = name.Split(':');

				var ns = _fileDefinition.Namespaces.FirstOrDefault(n => n.Prefix == fields[0]);

				if (ns != null)
				{
					var nsName = ns.Namespace.TrimStart("using:");

					if (nsName.StartsWith("clr-namespace:"))
					{
						nsName = nsName.Split(';')[0].TrimStart("clr-namespace:");
					}

					name = nsName + "." + fields[1];
				}
			}
			else
			{
				var defaultXmlNamespace = _fileDefinition
						.Namespaces
						.Where(n => n.Prefix.None())
						.FirstOrDefault()
						.SelectOrDefault(n => n.Namespace);

				var clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(defaultXmlNamespace, new string[0]);

				// Search first using the default namespace
				foreach (var clrNamespace in clrNamespaces)
				{
					var type = _medataHelper.FindTypeByFullName(clrNamespace + "." + name) as INamedTypeSymbol;

					if (type != null)
					{
						return type;
					}
				}
			}

			var resolvers = new Func<INamedTypeSymbol>[] {

				// The sanitized name
				() => _medataHelper.FindTypeByName(name) as INamedTypeSymbol,

				// As a full name
				() => _medataHelper.FindTypeByFullName(name) as INamedTypeSymbol,

				// As a partial name using the original type
				() => _medataHelper.FindTypeByName(originalName) as INamedTypeSymbol,

				// As a partial name using the non-qualified name
				() => _medataHelper.FindTypeByName(originalName.Split(':').ElementAtOrDefault(1)) as INamedTypeSymbol,
			};

			return resolvers
				.Select(m => m())
				.Trim()
				.FirstOrDefault();
		}

		private NameScope CurrentScope
		{
			get
			{
				return _scopeStack.Peek();
			}
		}

		private IDisposable Scope(string name)
		{
			_scopeStack.Push(new NameScope(name));

			return new DisposableAction(() => _scopeStack.Pop());
		}
	}
}
