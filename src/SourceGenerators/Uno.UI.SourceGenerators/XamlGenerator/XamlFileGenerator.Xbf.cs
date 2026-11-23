#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.Extensions;
using Uno.Roslyn;
using Uno.UI.SourceGenerators.Helpers;
using Uno.UI.SourceGenerators.XamlGenerator.Xbf;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;
using Uno.UI.Xaml.Xbf;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	// Builds XAML objects as XBF node streams rather than as C#.
	//
	// WinUI's XAML compiler produces the node stream by running the XAML parser against an
	// encoding ObjectWriter (ObjectWriterRuntimeEncoder). Uno's parser produces a tree of
	// XamlObjectDefinition instead, so the nodes are written while walking that tree, the same
	// way the C# is: the methods below mirror BuildChild, BuildLiteralProperties, BuildProperties,
	// BuildExtendedProperties and BuildComplexPropertyValue, and must stay aligned with them.
	//
	// An object that uses a construct the node stream cannot express yet raises
	// XbfNotSupportedException, and is built in C# instead. BuildChild then gets to try
	// again for each of its children, so the fallback is as narrow as it can be.
	internal partial class XamlFileGenerator
	{
		private const string XbfLoaderType = "global::Uno.UI.Xaml.Xbf.XbfLoader";

		private XbfFileBuilder? _xbf;
		private bool _isBuildingXbf;
		private readonly HashSet<XamlObjectDefinition> _xbfUnsupportedObjects = new();
		private readonly Dictionary<string, int> _xbfFallbackReasons = new(StringComparer.Ordinal);
		private int _xbfObjectCount;

		/// <summary>
		/// What an object is built into: the scope a generated class holds in its fields.
		/// </summary>
		private sealed class XbfScope
		{
			public static readonly XbfScope Root = new(isTemplate: false, isComponent: false, declaredNames: new());

			public XbfScope(bool isTemplate, bool isComponent, HashSet<string> declaredNames)
			{
				IsTemplate = isTemplate;
				IsComponent = isComponent;
				DeclaredNames = declaredNames;
			}

			/// <summary>
			/// The content of a template.
			/// </summary>
			public bool IsTemplate { get; }

			/// <summary>
			/// The content of an x:Class, whose named elements are fields of the class.
			/// </summary>
			public bool IsComponent { get; }

			/// <summary>
			/// Whether the elements can be named, and referred to by their name.
			/// </summary>
			public bool HasNames => IsTemplate || IsComponent;

			public HashSet<string> DeclaredNames { get; }
		}

		/// <summary>
		/// What the Connect method of the class does for an object of the node stream.
		/// </summary>
		private sealed class XbfConnection
		{
			public int Id;
			public string Type = "";
			public List<string> Statements = new();
		}

		private Dictionary<XamlObjectDefinition, XbfConnection> _xbfConnections = new();
		private List<BackingFieldDefinition> _xbfBackingFields = new();

		private void InitializeXbf()
		{
			// Hot Reload updates the generated C#, which the XBF takes the place of.
			if (Generation.IsXbfEnabled && !_isHotReloadEnabled)
			{
				_xbf = new XbfFileBuilder($"__Xbf_{_fileUniqueId}");
			}
		}

		private const string XbfInterfacesToken = "/*__xbf_interfaces__*/";

		/// <summary>
		/// The placeholder of the interfaces the XBF adds to the class of the file, once known.
		/// </summary>
		private string XbfClassInterfaces => _xbf is null ? "" : XbfInterfacesToken;

		private void XbfResolveClassInterfaces(StringBuilder builder)
		{
			if (_xbf is not null)
			{
				builder.Replace(XbfInterfacesToken, _xbfConnections.Count != 0 || _xbfIsComponent ? ", global::Uno.UI.Xaml.Xbf.IXbfComponentConnector" : "");
			}
		}

		private bool _xbfIsComponent;

		/// <summary>
		/// Builds the content of an x:Class as an XBF node stream, and writes the body of InitializeComponent that loads it.
		/// </summary>
		/// <returns>False when the file has to be built in C#.</returns>
		private bool TryBuildComponentAsXbf(IIndentedStringBuilder writer, XamlObjectDefinition topLevelControl)
		{
			if (_xbf is null || _isTopLevelDictionary || FindType(topLevelControl.Type) is not { } topLevelControlType)
			{
				return false;
			}

			var checkpoint = _xbf.CreateCheckpoint();
			var errorCount = _errors.Count;

			_isBuildingXbf = true;
			try
			{
				if (FindMember(topLevelControl, "Load") is not null)
				{
					throw XbfNotSupported("XAML error: invalid x:Load");
				}

				if (GetObjectUid(topLevelControl) is not null)
				{
					throw XbfNotSupported("x:Uid");
				}

				var nodes = new XbfNodeWriter();
				var declaredNames = new HashSet<string>(FindNamesIn(topLevelControl), StringComparer.Ordinal);
				var scope = new XbfScope(isTemplate: false, isComponent: true, declaredNames);

				using (Scope(null, "__xbf"))
				using (TrySetDefaultBindMode(topLevelControl))
				{
					// Mirrors BuildGenericControlInitializerBody
					XbfEmitResources(nodes, topLevelControl, scope);
					XbfEmitProperties(nodes, topLevelControl, topLevelControlType, scope);
					XbfEmitLiteralProperties(nodes, topLevelControl, scope, lazyProperties: false);
					XbfEmitExtendedProperties(nodes, topLevelControl, topLevelControlType, scope);
				}

				if (_errors.Count != errorCount)
				{
					throw new XbfNotSupportedException("XAML error: " + _errors[errorCount].Message);
				}

				nodes.WriteNodeType(ObjectWriterNodeType.EndOfStream);

				var offset = _xbf.AppendRoot(nodes);
				_xbfObjectCount++;
				_xbfIsComponent = true;

				// OnInitializeCompleted() seems to be used by some older code as a substitute for the constructor for UserControls, which are optimized out of the visual tree.
				RegisterPartial("void OnInitializeCompleted()");

				foreach (var backingField in _xbfBackingFields)
				{
					CurrentScope.BackingFields.Add(backingField);
				}

				writer.AppendLineIndented(
					$"{XbfLoaderType}.LoadComponent(this, {_xbf.ClassName}.Instance, {offset.ToString(CultureInfo.InvariantCulture)}, __nameScope, {ParseContextPropertyAccess}, __baseUri_{_fileUniqueId});");
				writer.AppendLineIndented("OnInitializeCompleted();");

				CurrentScope.RegisterMethod("__XbfConnect", (_, connectWriter) => BuildXbfConnect(connectWriter));

				return true;
			}
			catch (Exception e) when (e is XbfNotSupportedException or XamlGenerationException)
			{
				var reason = e is XbfNotSupportedException notSupported ? notSupported.Reason : "XAML error: " + e.Message;

				_xbf.Rollback(checkpoint);
				_errors.RemoveRange(errorCount, _errors.Count - errorCount);
				_xbfConnections = new();
				_xbfBackingFields = new();
				_currentStyleTargetTypeStack.Clear();
				_xbfFallbackReasons[reason] = _xbfFallbackReasons.TryGetValue(reason, out var count) ? count + 1 : 1;

				return false;
			}
			finally
			{
				_isBuildingXbf = false;
			}
		}

		/// <summary>
		/// Builds the method the node stream calls for the objects the class has a field or an event handler for.
		/// </summary>
		private void BuildXbfConnect(IIndentedStringBuilder writer)
		{
			writer.AppendLineIndented("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
			using (writer.BlockInvariant("void global::Uno.UI.Xaml.Xbf.IXbfComponentConnector.Connect(int connectionId, object target)"))
			{
				if (_xbfConnections.Count != 0)
				{
					using (writer.BlockInvariant("switch (connectionId)"))
					{
						foreach (var connection in _xbfConnections.Values.OrderBy(c => c.Id))
						{
							writer.AppendLineIndented($"case {connection.Id.ToString(CultureInfo.InvariantCulture)}:");
							using (writer.Block())
							{
								writer.AppendLineIndented($"var e = ({connection.Type})target;");
								foreach (var statement in connection.Statements)
								{
									writer.AppendLineIndented(statement);
								}
							}
							writer.AppendLineIndented("break;");
						}
					}
				}
			}
		}

		/// <summary>
		/// Has the class of the file do something with the object, which the nodes being written are creating.
		/// </summary>
		/// <param name="statement">A C# statement of the class, the object being <c>e</c>.</param>
		private void XbfConnect(XbfNodeWriter nodes, XamlObjectDefinition objectDefinition, INamedTypeSymbol objectType, string statement)
		{
			if (_isTopLevelDictionary)
			{
				throw XbfNotSupported("event handler or field without an x:Class");
			}

			if (!_xbfConnections.TryGetValue(objectDefinition, out var connection))
			{
				_xbfConnections[objectDefinition] = connection = new XbfConnection
				{
					Id = _xbfConnections.Count + 1,
					Type = objectType.GetFullyQualifiedTypeIncludingGlobal(),
				};

				nodes.WriteNodeType(ObjectWriterNodeType.SetConnectionId);
				nodes.WriteUInt(connection.Id);
			}

			connection.Statements.Add(statement);
		}

		/// <summary>
		/// Builds the object as an XBF node stream, and writes the expression that loads it.
		/// </summary>
		/// <returns>False when the object has to be built in C#.</returns>
		private bool TryBuildChildAsXbf(IIndentedStringBuilder writer, XamlMemberDefinition? owner, XamlObjectDefinition xamlObjectDefinition)
		{
			if (_xbf is null
				|| _isBuildingXbf
				// Only the objects of a top-level dictionary are self-contained for now: the ones of a page
				// or of a C# template are tied to the fields of their class.
				|| !_isTopLevelDictionary
				|| _isInChildSubclass
				|| _xbfUnsupportedObjects.Contains(xamlObjectDefinition))
			{
				return false;
			}

			if (xamlObjectDefinition.Type.Name is "NullExtension" or "StaticResource" or "ThemeResource" or "Binding" or "Bind" or "TemplateBinding" or "CustomResource"
				|| HasInitializer(xamlObjectDefinition))
			{
				return false;
			}

			var knownType = FindType(xamlObjectDefinition.Type);
			if (knownType is null
				|| HasCustomInitializer(knownType)
				|| IsType(knownType, Generation.ResourceDictionarySymbol.Value))
			{
				return false;
			}

			var checkpoint = _xbf.CreateCheckpoint();
			var errorCount = _errors.Count;
			var styleTargetTypes = _currentStyleTargetTypeStack.ToArray();
			var isInSingletonInstance = _isInSingletonInstance;

			_isBuildingXbf = true;
			try
			{
				var nodes = new XbfNodeWriter();

				// The helpers shared with the C# generation register fields and components on the
				// current scope, which this object has no use for.
				using (Scope(null, "__xbf"))
				{
					XbfEmitObject(nodes, owner, xamlObjectDefinition, XbfScope.Root);
				}

				if (_errors.Count != errorCount)
				{
					throw new XbfNotSupportedException("XAML error: " + _errors[errorCount].Message);
				}

				nodes.WriteNodeType(ObjectWriterNodeType.EndOfStream);

				var offset = _xbf.AppendRoot(nodes);
				_xbfObjectCount++;

				writer.AppendLineIndented(
					$"(({knownType.GetFullyQualifiedTypeIncludingGlobal()}){XbfLoaderType}.Load(" +
					$"{_xbf.ClassName}.Instance, {offset.ToString(CultureInfo.InvariantCulture)}, {CurrentResourceOwnerName}, {ParseContextPropertyAccess}, __baseUri_{_fileUniqueId}))");

				return true;
			}
			catch (Exception e) when (e is XbfNotSupportedException or XamlGenerationException)
			{
				// A XAML error is left for the C# generation to report.
				var reason = e is XbfNotSupportedException notSupported ? notSupported.Reason : "XAML error: " + e.Message;

				_xbf.Rollback(checkpoint);
				_errors.RemoveRange(errorCount, _errors.Count - errorCount);
				_xbfUnsupportedObjects.Add(xamlObjectDefinition);
				_xbfFallbackReasons[reason] = _xbfFallbackReasons.TryGetValue(reason, out var count) ? count + 1 : 1;

				_currentStyleTargetTypeStack.Clear();
				foreach (var styleTargetType in styleTargetTypes.Reverse())
				{
					_currentStyleTargetTypeStack.Push(styleTargetType);
				}

				return false;
			}
			finally
			{
				_isBuildingXbf = false;
				_isInSingletonInstance = isInSingletonInstance;
			}
		}

		private void BuildXbfProvider(IIndentedStringBuilder writer)
		{
			if (_xbf is null)
			{
				return;
			}

			Generation.ReportXbf(_fileDefinition.FilePath, _xbfObjectCount, _xbfFallbackReasons);

			if (!_xbf.IsEmpty)
			{
				_xbf.WriteProviderClass(writer);
			}
		}

		private static XbfNotSupportedException XbfNotSupported(string reason) => new(reason);

		// Mirrors BuildChild
		private void XbfEmitObject(XbfNodeWriter nodes, XamlMemberDefinition? owner, XamlObjectDefinition xamlObjectDefinition, XbfScope scope)
		{
			_generatorContext.CancellationToken.ThrowIfCancellationRequested();

			using var scopeAutoDisposable = LogicalScope(xamlObjectDefinition);

			var typeName = xamlObjectDefinition.Type.Name;
			var knownType = FindType(xamlObjectDefinition.Type);

			if (IsNewScope(xamlObjectDefinition))
			{
				XbfEmitTemplate(nodes, xamlObjectDefinition);
			}
			else if (typeName == "NullExtension")
			{
				nodes.WriteNodeType(ObjectWriterNodeType.PushConstant);
				nodes.WriteByte((byte)PersistedConstantType.UnoIsNull);
			}
			else if (typeName is "StaticResource" or "ThemeResource")
			{
				if (owner is null)
				{
					throw XbfNotSupported("resource reference without an owner");
				}

				var targetPropertyType = FindPropertyType(owner.Member) ?? Generation.ObjectSymbol.Value;
				if (IsXamlTypeConverter(targetPropertyType)
					|| xamlObjectDefinition.Members.FirstOrDefault()?.Value?.ToString() is not { } resourceKey)
				{
					throw XbfNotSupported("converted resource reference");
				}

				nodes.WriteNodeType(typeName == "StaticResource" ? ObjectWriterNodeType.ProvideStaticResourceValue : ObjectWriterNodeType.ProvideThemeResourceValue);
				nodes.WriteUInt(_xbf!.GetStringId(resourceKey));
				nodes.WriteUInt(XbfGetTypeId(targetPropertyType, needsActivator: false));
			}
			else if (typeName is "Binding" or "Bind" or "TemplateBinding" or "CustomResource")
			{
				throw XbfNotSupported($"{typeName} as an object");
			}
			else if (knownType is null)
			{
				throw XbfNotSupported("unknown type");
			}
			else if (HasInitializer(xamlObjectDefinition))
			{
				if (xamlObjectDefinition.Members.Any(m => m.Member.Name != "_Initialization" && !IsXbfIgnoredDirective(m)))
				{
					throw XbfNotSupported("initializer with properties");
				}

				var initializer = new IndentedStringBuilder();
				BuildInitializer(initializer, xamlObjectDefinition, owner);

				nodes.WriteNodeType(ObjectWriterNodeType.PushConstant);
				XbfWriteConstant(nodes, initializer.ToString().Trim(), knownType, knownType);
			}
			else if (IsType(knownType, Generation.ResourceDictionarySymbol.Value))
			{
				throw XbfNotSupported("nested ResourceDictionary");
			}
			else if (HasCustomInitializer(knownType))
			{
				var implicitContent = FindImplicitContentMember(xamlObjectDefinition);
				if (implicitContent is null)
				{
					throw XbfNotSupported("custom initializer without content");
				}

				nodes.WriteNodeType(ObjectWriterNodeType.PushConstant);
				XbfWriteConstant(nodes, XbfStaticExpression(() => BuildLiteralValue(implicitContent, knownType, owner)), knownType, knownType);
			}
			else
			{
				var isStyle = knownType.GetFullyQualifiedTypeExcludingGlobal() == XamlConstants.Types.Style;
				if (isStyle)
				{
					var targetTypeNode = GetMember(xamlObjectDefinition, "TargetType");
					if (targetTypeNode.Value == null)
					{
						throw new XamlGenerationException("TargetType cannot be empty", xamlObjectDefinition);
					}

					_currentStyleTargetTypeStack.Push(FindType(targetTypeNode.Value.ToString()));
				}

				var elementNodes = nodes;
				var isDeferred = XbfIsDeferredLoad(xamlObjectDefinition, knownType, scope);
				if (isDeferred)
				{
					// The element is created by its stub, when it gets loaded.
					nodes = new XbfNodeWriter();
				}

				if (GetObjectUid(xamlObjectDefinition) is not null)
				{
					throw XbfNotSupported("x:Uid");
				}

				if (GenerateConstructorParameters(knownType).Length != 0
					|| knownType.IsAbstract
					|| (knownType.TypeKind != TypeKind.Struct && !knownType.InstanceConstructors.Any(c => c.Parameters.Length == 0 && IsXbfAccessible(c))))
				{
					throw XbfNotSupported("type without a default constructor");
				}

				nodes.WriteNodeType(ObjectWriterNodeType.PushScopeCreateTypeBeginInit);
				nodes.WriteUInt(XbfGetTypeId(knownType, needsActivator: true));

				XbfEmitResources(nodes, xamlObjectDefinition, scope);
				XbfEmitLiteralProperties(nodes, xamlObjectDefinition, scope, lazyProperties: false);
				XbfEmitProperties(nodes, xamlObjectDefinition, knownType, scope);

				if (knownType.GetFullyQualifiedTypeExcludingGlobal() == XamlConstants.Types.Setter
					&& FindMember(xamlObjectDefinition, "Value") is { Objects.Count: 1 } valueNode)
				{
					var setterValueObject = valueNode.Objects[0];
					if (setterValueObject.Type.Name == "Bind")
					{
						throw XbfNotSupported("x:Bind");
					}

					if (setterValueObject.Type.Name == "ThemeResource")
					{
						nodes.WriteNodeType(ObjectWriterNodeType.UnoSetterThemeResource);
						nodes.WriteUInt(_xbf!.GetStringId(setterValueObject.Members.FirstOrDefault()?.Value?.ToString() ?? ""));
						nodes.WriteBool(true);
					}
				}

				XbfEmitExtendedProperties(nodes, xamlObjectDefinition, knownType, scope);

				nodes.WriteNodeType(ObjectWriterNodeType.EndInitPopScope);

				if (isDeferred)
				{
					XbfEmitElementStub(elementNodes, nodes, xamlObjectDefinition, scope);
				}

				if (isStyle)
				{
					_currentStyleTargetTypeStack.Pop();
				}
			}
		}

		// Mirrors the condition of TryGenerateDeferedLoadStrategy
		private bool XbfIsDeferredLoad(XamlObjectDefinition definition, INamedTypeSymbol targetType, XbfScope scope)
		{
			var strategy = FindMember(definition, "DeferLoadStrategy");
			var loadMember = FindMember(definition, "Load");

			if (strategy?.Value?.ToString()?.ToLowerInvariant() != "lazy"
				&& loadMember?.Value?.ToString()?.ToLowerInvariant() != "false"
				&& !HasMarkupExtension(loadMember))
			{
				return false;
			}

			if (loadMember != null
				&& (FindNameMember(definition) == null || definition.Owner?.Type?.Name is "DataTemplate" || IsDirectChildOfResourceDictionary(definition)))
			{
				throw XbfNotSupported("XAML error: invalid x:Load");
			}

			if (!targetType.Is(Generation.ElementStubSymbol.Value.BaseType))
			{
				// Lazy DeferLoadStrategy is ignored because the target type cannot be stubbed
				return false;
			}

			if (!scope.IsTemplate)
			{
				// The class of a page tracks the elements it loads on demand, to update their bindings.
				throw XbfNotSupported("x:Load outside of a template");
			}

			return true;
		}

		// Mirrors what TryGenerateDeferedLoadStrategy applies to the ElementStub
		private void XbfEmitElementStub(XbfNodeWriter nodes, XbfNodeWriter element, XamlObjectDefinition definition, XbfScope scope)
		{
			var elementStubType = Generation.ElementStubSymbol.Value;
			var loadMember = FindMember(definition, "Load");

			nodes.WriteNodeType(ObjectWriterNodeType.UnoPushScopeCreateElementStub);
			nodes.WriteBlock(element);

			if (FindNameMember(definition) is { Value: string name })
			{
				nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
				nodes.WriteUInt(XbfGetSetterId(elementStubType, elementStubType.GetPropertyWithName("Name")!));
				nodes.WriteByte((byte)PersistedConstantType.IsSharedString);
				nodes.WriteUInt(_xbf!.GetStringId(name));

				// Set the element name to the stub, then when the stub will be replaced
				// the actual target control will override it.
				nodes.WriteNodeType(ObjectWriterNodeType.UnoSetElementNameSubjectInstance);
				nodes.WriteUInt(_xbf.GetStringId(name));
			}

			if (HasMarkupExtension(loadMember))
			{
				var load = new XamlMemberDefinition(new XamlMember("Load", _elementStubXamlType, false), 0, 0, definition);
				load.Objects.AddRange(loadMember!.Objects);

				XbfEmitComplexPropertyValue(nodes, load, elementStubType, scope);
			}

			if (HasXBindMarkupExtension(definition))
			{
				throw XbfNotSupported("x:Bind");
			}

			if (EnumerateSubElements(definition).Any(HasMarkupExtensionNeedingComponent))
			{
				nodes.WriteNodeType(ObjectWriterNodeType.UnoTrackResourceBindings);
				nodes.WriteBool(false);
			}

			// The stub is not initialized as the elements are
			nodes.WriteNodeType(ObjectWriterNodeType.PopScope);
		}

		private void XbfEmitTemplate(XbfNodeWriter nodes, XamlObjectDefinition xamlObjectDefinition)
		{
			var templateKind = xamlObjectDefinition.Type.Name switch
			{
				"ControlTemplate" => XbfConstants.TemplateKindControl,
				"DataTemplate" => XbfConstants.TemplateKindData,
				"ItemsPanelTemplate" => XbfConstants.TemplateKindItemsPanel,
				_ => throw XbfNotSupported("custom template type"),
			};

			if (FindType(xamlObjectDefinition.Type)?.GetFullyQualifiedTypeExcludingGlobal() is not (
				"Microsoft.UI.Xaml.Controls.ControlTemplate" or "Microsoft.UI.Xaml.DataTemplate" or "Microsoft.UI.Xaml.Controls.ItemsPanelTemplate"))
			{
				throw XbfNotSupported("custom template type");
			}

			var content = new XbfNodeWriter();
			var contentDefinition = xamlObjectDefinition.Members.FirstOrDefault(m => m.Member.Name == XamlConstants.UnknownContent);

			if (contentDefinition?.Objects.FirstOrDefault() is { } contentRoot)
			{
				// The content is built by the template, outside of what its declaration is nested in.
				var styleTargetTypes = _currentStyleTargetTypeStack.ToArray();
				_currentStyleTargetTypeStack.Clear();

				var declaredNames = new HashSet<string>(FindNamesIn(contentRoot), StringComparer.Ordinal);
				if (FindNameMember(contentRoot)?.Value is string rootName)
				{
					declaredNames.Add(rootName);
				}

				// The names of a template are its own, as they are for the class the C# generation builds for it.
				using (Scope(null, "__xbf"))
				using (TrySetDefaultBindMode(contentDefinition.Owner))
				{
					XbfEmitObject(content, contentDefinition, contentRoot, new XbfScope(isTemplate: true, isComponent: false, declaredNames));
				}

				foreach (var styleTargetType in styleTargetTypes.Reverse())
				{
					_currentStyleTargetTypeStack.Push(styleTargetType);
				}
			}

			nodes.WriteNodeType(ObjectWriterNodeType.UnoPushScopeCreateTemplateBeginInit);
			nodes.WriteByte(templateKind);
			nodes.WriteBlock(content);
			nodes.WriteNodeType(ObjectWriterNodeType.EndInitPopScope);
		}

		// Mirrors RegisterAndBuildResources and BuildResourceDictionary
		private void XbfEmitResources(XbfNodeWriter nodes, XamlObjectDefinition xamlObjectDefinition, XbfScope scope)
		{
			if (FindResourcesMember(xamlObjectDefinition) is not { } resources)
			{
				return;
			}

			if (!resources.PopulatesOwnDictionary
				|| resources.MergedDictionaries is not null
				|| resources.ThemeDictionaries is not null
				|| resources.Root is null)
			{
				throw XbfNotSupported("Resources with dictionaries");
			}

			var resourcesProperty = FindType(xamlObjectDefinition.Type).GetPropertyWithName("Resources")
				?? throw XbfNotSupported("Resources property not found");

			nodes.WriteNodeType(ObjectWriterNodeType.PushScopeGetValue);
			nodes.WriteUInt(XbfGetGetterId(resourcesProperty));

			foreach (var resource in resources.Root.Objects)
			{
				var key = GetDictionaryResourceKey(resource, out _);
				if (key is null)
				{
					continue;
				}

				if (resource.Type.Name == "StaticResource")
				{
					throw XbfNotSupported("StaticResource alias");
				}

				if (ShouldLazyInitializeResource(resource))
				{
					var content = new XbfNodeWriter();
					XbfEmitObject(content, null, resource, scope);

					nodes.WriteNodeType(ObjectWriterNodeType.UnoAddDeferredToDictionary);
					XbfWriteResourceKey(nodes, key);
					nodes.WriteBlock(content);
				}
				else
				{
					XbfEmitObject(nodes, null, resource, scope);

					nodes.WriteNodeType(ObjectWriterNodeType.AddToDictionaryWithKey);
					XbfWriteResourceKey(nodes, key);
				}
			}

			nodes.WriteNodeType(ObjectWriterNodeType.PopScope);
		}

		private void XbfWriteResourceKey(XbfNodeWriter nodes, string key)
		{
			const string TypeofPrefix = "typeof(";

			if (key.StartsWith(TypeofPrefix, StringComparison.Ordinal))
			{
				// The implicit key of a Style
				nodes.WriteByte((byte)PersistedConstantType.UnoIsType);
				nodes.WriteUInt(_xbf!.GetTypeId(key.Substring(TypeofPrefix.Length, key.Length - TypeofPrefix.Length - 1), needsActivator: false));
			}
			else
			{
				nodes.WriteByte((byte)PersistedConstantType.IsSharedString);
				nodes.WriteUInt(_xbf!.GetStringId(key));
			}
		}

		// Mirrors BuildLiteralProperties, BuildLiteralLazyVisualStateManagerProperties and BuildLiteralPropertiesWithFilter
		private void XbfEmitLiteralProperties(XbfNodeWriter nodes, XamlObjectDefinition objectDefinition, XbfScope scope, bool lazyProperties)
		{
			var extendedProperties = GetExtendedProperties(objectDefinition);
			var objectUid = GetObjectUid(objectDefinition);

			bool PropertyFilter(XamlMemberDefinition member)
			{
				if (lazyProperties)
				{
					return IsLazyVisualStateManagerProperty(member);
				}

				var type = FindType(member.Member.DeclaringType);
				return IsType(objectDefinition.Type, type)
					&& !IsAttachedProperty(type, member.Member.Name)
					&& !IsLazyVisualStateManagerProperty(member)
					&& _metadataHelper.FindEventType(type, member.Member.Name) == null
					&& !IsXNameMember(member, objectDefinition.Type)
					&& member.Member.Name != "_UnknownContent";
			}

			foreach (var member in extendedProperties)
			{
				if (PropertyFilter(member))
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
								var isColumn = member.Member.Name == "ColumnDefinitions";
								var definitionType = isColumn ? Generation.ColumnDefinitionSymbol.Value : Generation.RowDefinitionSymbol.Value;
								var lengthProperty = definitionType.GetPropertyWithName(isColumn ? "Width" : "Height")!;
								var collectionProperty = FindType(member.Member.DeclaringType)!.GetPropertyWithName(member.Member.Name)!;
								var adderId = _xbf!.GetAdderId(
									$"(({collectionProperty.Type.GetFullyQualifiedTypeIncludingGlobal()})c).Add(({definitionType.GetFullyQualifiedTypeIncludingGlobal()})v)");

								nodes.WriteNodeType(ObjectWriterNodeType.PushScopeGetValue);
								nodes.WriteUInt(XbfGetGetterId(collectionProperty));

								foreach (var definition in definitions.Split(',').Select(static definition => definition.Trim()))
								{
									nodes.WriteNodeType(ObjectWriterNodeType.PushScopeCreateTypeBeginInit);
									nodes.WriteUInt(XbfGetTypeId(definitionType, needsActivator: true));
									nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
									nodes.WriteUInt(XbfGetSetterId(definitionType, lengthProperty));
									XbfWriteConstant(nodes, BuildGridLength(definition), lengthProperty.Type, lengthProperty.Type as INamedTypeSymbol);
									nodes.WriteNodeType(ObjectWriterNodeType.EndInitPopScope);
									nodes.WriteNodeType(ObjectWriterNodeType.UnoAddToCollectionTyped);
									nodes.WriteUInt(adderId);
								}

								nodes.WriteNodeType(ObjectWriterNodeType.PopScope);
							}

							// Otherwise, an empty collection
						}
						else
						{
							INamedTypeSymbol? setterPropertyType = null;
							var memberName = member.Member.Name;

							if (member.Owner?.Type.Name == "Setter" && memberName == "Value")
							{
								if (FindMember(member.Owner, "Property") is { Value: string setterPropertyName })
								{
									setterPropertyType = GetDependencyPropertyTypeForSetter(setterPropertyName);
								}
								else if (CurrentStyleTargetType is not null && GetMember(member.Owner, "Target") is { Value: string setterTarget })
								{
									setterPropertyType = GetDependencyPropertyTypeForSetter(setterTarget);
								}
							}
							else if (member.Owner?.Type.Name == "Setter" && memberName == "Target")
							{
								if (CurrentStyleTargetType is not null)
								{
									// A setter's Target inside a style will set Property instead of Target
									memberName = "Property";
								}
								else
								{
									XbfEmitTargetPropertyPath(nodes, member, scope);
									continue;
								}
							}

							if (FindPropertyType(member.Member) != null)
							{
								var declaringType = FindType(member.Member.DeclaringType)!;
								var property = declaringType.GetPropertyWithName(memberName)
									?? throw XbfNotSupported("property not found");

								var value = XbfStaticExpression(() => BuildLiteralValue(member, propertyType: setterPropertyType, objectUid: objectUid ?? ""));

								nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
								nodes.WriteUInt(XbfGetSetterId(declaringType, property));
								XbfWriteConstant(nodes, value, property.Type, setterPropertyType ?? FindPropertyType(member.Member));
							}
							else if (IsRelevantNamespace(member.Member.PreferredXamlNamespace) && IsRelevantProperty(member.Member))
							{
								throw XbfNotSupported($"XAML error: unknown property {member.Member.DeclaringType?.Name}.{member.Member.Name}");
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
							)
							.ToArray();

						if (nonBindingObjects.Length != 0)
						{
							var declaringType = FindType(member.Member.DeclaringType)!;
							var property = declaringType.GetPropertyWithName(member.Member.Name)
								?? throw XbfNotSupported("property not found");

							if (IsInitializableCollection(member.Member) || IsInlineCollection(member.Member, nonBindingObjects))
							{
								nodes.WriteNodeType(ObjectWriterNodeType.PushScopeGetValue);
								nodes.WriteUInt(XbfGetGetterId(property));

								foreach (var child in nonBindingObjects)
								{
									XbfEmitObject(nodes, member, child, scope);
									XbfEmitAddToCollection(nodes, property.Type, child);
								}

								nodes.WriteNodeType(ObjectWriterNodeType.PopScope);
							}
							else
							{
								var child = nonBindingObjects[0];

								XbfEmitObject(nodes, member, child, scope);
								XbfEnsureAssignable(child, property.Type);

								nodes.WriteNodeType(ObjectWriterNodeType.SetValue);
								nodes.WriteUInt(XbfGetSetterId(declaringType, property));
							}
						}
					}
				}
				else if (!lazyProperties && member.Member.DeclaringType == null && member.Member.Name == "Name")
				{
					// This is a special case, where the declaring type is from the x: namespace,
					// but is considered of an unknown type. This can happen when providing the
					// name of a control using x:Name instead of Name.
					if (HasProperty(objectDefinition.Type, "Name") && !IsXNameProvider(objectDefinition.Type))
					{
						var declaringType = FindType(objectDefinition.Type)!;
						var property = declaringType.GetPropertyWithName("Name")!;

						nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
						nodes.WriteUInt(XbfGetSetterId(declaringType, property));
						nodes.WriteByte((byte)PersistedConstantType.IsSharedString);
						nodes.WriteUInt(_xbf!.GetStringId(member.Value?.ToString() ?? ""));
					}
				}
			}
		}

		// Mirrors BuildTargetPropertyPath, for a Setter of a VisualState
		private void XbfEmitTargetPropertyPath(XbfNodeWriter nodes, XamlMemberDefinition member, XbfScope scope)
		{
			if (member.Value is not string target || !scope.HasNames)
			{
				throw XbfNotSupported("Setter.Target outside of a name scope");
			}

			var ownerControl = GetControlOwner(member.Owner);
			var separatorIndex = target.IndexOf(".", StringComparison.Ordinal);
			if (ownerControl is null || separatorIndex < 0)
			{
				throw XbfNotSupported("Setter.Target without an element");
			}

			var elementName = target.Substring(0, separatorIndex);
			if (FindSubElementByName(ownerControl, elementName) is null || !scope.DeclaredNames.Contains(elementName))
			{
				throw XbfNotSupported("Setter.Target element not found");
			}

			var propertyName = RewriteAttachedPropertyPath(ParenthesizeBareAttachedProperty(target.Substring(separatorIndex + 1)));

			nodes.WriteNodeType(ObjectWriterNodeType.UnoSetTargetPropertyPath);
			nodes.WriteUInt(_xbf!.GetStringId(elementName));
			nodes.WriteUInt(_xbf.GetStringId(propertyName));
		}

		// Mirrors BuildProperties
		private void XbfEmitProperties(XbfNodeWriter nodes, XamlObjectDefinition topLevelControl, INamedTypeSymbol topLevelControlSymbol, XbfScope scope)
		{
			var implicitContentChild = FindImplicitContentMember(topLevelControl);
			if (implicitContentChild is null)
			{
				return;
			}

			void SetStringProperty(string propertyName, string value)
			{
				var property = topLevelControlSymbol.GetPropertyWithName(propertyName)
					?? throw XbfNotSupported("property not found");

				nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
				nodes.WriteUInt(XbfGetSetterId(topLevelControlSymbol, property));
				nodes.WriteByte((byte)PersistedConstantType.IsSharedString);
				nodes.WriteUInt(_xbf!.GetStringId(value));
			}

			void SetConstantProperty(string propertyName, string expression)
			{
				var property = topLevelControlSymbol.GetPropertyWithName(propertyName)
					?? throw XbfNotSupported("property not found");

				nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
				nodes.WriteUInt(XbfGetSetterId(topLevelControlSymbol, property));
				XbfWriteConstant(nodes, expression, property.Type, property.Type as INamedTypeSymbol);
			}

			void AddToCollectionProperty(IPropertySymbol property)
			{
				nodes.WriteNodeType(ObjectWriterNodeType.PushScopeGetValue);
				nodes.WriteUInt(XbfGetGetterId(property));

				foreach (var child in implicitContentChild.Objects)
				{
					XbfEmitObject(nodes, implicitContentChild, child, scope);
					XbfEmitAddToCollection(nodes, property.Type, child);
				}

				nodes.WriteNodeType(ObjectWriterNodeType.PopScope);
			}

			void SetObjectProperty(IPropertySymbol property, XamlObjectDefinition child)
			{
				XbfEmitObject(nodes, implicitContentChild, child, scope);
				XbfEnsureAssignable(child, property.Type);

				nodes.WriteNodeType(ObjectWriterNodeType.SetValue);
				nodes.WriteUInt(XbfGetSetterId(topLevelControlSymbol, property));
			}

			if (IsTextBlock(topLevelControlSymbol))
			{
				if (implicitContentChild.Objects.Count != 0)
				{
					AddToCollectionProperty(topLevelControlSymbol.GetPropertyWithName("Inlines")!);
				}
				else if (implicitContentChild.Value != null)
				{
					SetStringProperty("Text", implicitContentChild.Value.ToString() ?? "");
				}
			}
			else if (IsRun(topLevelControlSymbol))
			{
				if (implicitContentChild.Value != null)
				{
					SetStringProperty("Text", implicitContentChild.Value.ToString() ?? "");
				}
			}
			else if (IsSpan(topLevelControlSymbol))
			{
				if (implicitContentChild.Objects.Count != 0)
				{
					AddToCollectionProperty(topLevelControlSymbol.GetPropertyWithName("Inlines")!);
				}
				else if (implicitContentChild.Value != null)
				{
					throw XbfNotSupported("Span text content");
				}
			}
			else if (IsNativePage(topLevelControlSymbol))
			{
				throw XbfNotSupported("NativePage");
			}
			else if (IsBorder(topLevelControlSymbol))
			{
				if (implicitContentChild.Objects.Count > 0)
				{
					if (implicitContentChild.Objects.Count > 1)
					{
						throw new XamlGenerationException($"The type '{topLevelControl.Type.Name}' does not support multiple children", implicitContentChild.Objects[1]);
					}

					SetObjectProperty(topLevelControlSymbol.GetPropertyWithName("Child")!, implicitContentChild.Objects[0]);
				}
			}
			else if (IsType(topLevelControlSymbol, Generation.SolidColorBrushSymbol.Value))
			{
				if (implicitContentChild.Value is string content && !content.IsNullOrWhiteSpace())
				{
					SetConstantProperty("Color", BuildColor(content));
				}
			}
			else if (
				(IsType(topLevelControlSymbol, Generation.RowDefinitionSymbol.Value) ||
					IsType(topLevelControlSymbol, Generation.ColumnDefinitionSymbol.Value)) &&
				implicitContentChild.Value is string definitionContent &&
				!definitionContent.IsNullOrWhiteSpace())
			{
				SetConstantProperty(topLevelControl.Type.Name == "ColumnDefinition" ? "Width" : "Height", BuildGridLength(definitionContent));
			}
			else if (IsInitializableCollection(topLevelControl, topLevelControlSymbol))
			{
				if (IsDictionary(topLevelControlSymbol))
				{
					throw XbfNotSupported("dictionary content");
				}

				foreach (var child in implicitContentChild.Objects)
				{
					XbfEmitObject(nodes, implicitContentChild, child, scope);
					XbfEmitAddToCollection(nodes, topLevelControlSymbol, child);
				}
			}
			else // General case for implicit content
			{
				var contentProperty = FindContentProperty(topLevelControlSymbol);

				if (contentProperty != null)
				{
					if (IsCollectionOrListType(contentProperty.Type as INamedTypeSymbol))
					{
						if (IsInitializableProperty(contentProperty))
						{
							AddToCollectionProperty(contentProperty);
						}
						else if (IsNewableProperty(contentProperty, out _))
						{
							// Explicitly instantiate the collection and set it using the content property
							if (implicitContentChild.Objects.Count == 1
								&& SymbolEqualityComparer.Default.Equals(contentProperty.Type, FindType(implicitContentChild.Objects[0].Type)))
							{
								SetObjectProperty(contentProperty, implicitContentChild.Objects[0]);
							}
							else
							{
								nodes.WriteNodeType(ObjectWriterNodeType.PushScopeCreateTypeBeginInit);
								nodes.WriteUInt(XbfGetTypeId((INamedTypeSymbol)contentProperty.Type, needsActivator: true));

								foreach (var child in implicitContentChild.Objects)
								{
									XbfEmitObject(nodes, implicitContentChild, child, scope);
									XbfEmitAddToCollection(nodes, contentProperty.Type, child);
								}

								nodes.WriteNodeType(ObjectWriterNodeType.EndInitPopScope);
								nodes.WriteNodeType(ObjectWriterNodeType.SetValue);
								nodes.WriteUInt(XbfGetSetterId(topLevelControlSymbol, contentProperty));
							}
						}
						else
						{
							throw XbfNotSupported("collection content of an interface type");
						}
					}
					else if (IsLazyVisualStateManagerProperty(contentProperty) && !HasDescendantsWithXName(implicitContentChild))
					{
						// Lazy VisualStateManager property
					}
					else // Content is not a collection
					{
						if (implicitContentChild.Objects.Count > 0)
						{
							if (implicitContentChild.Objects.Count > 1)
							{
								throw new XamlGenerationException($"The type '{topLevelControl.Type.Name}' does not support multiple children", topLevelControl);
							}

							SetObjectProperty(contentProperty, implicitContentChild.Objects[0]);
						}
						else if (implicitContentChild.Value is string implicitValue && !implicitValue.IsNullOrWhiteSpace())
						{
							if (contentProperty.Type.SpecialType is not (SpecialType.System_String or SpecialType.System_Object))
							{
								throw XbfNotSupported("converted text content");
							}

							SetStringProperty(contentProperty.Name, implicitValue);
						}
					}
				}
			}
		}

		// Mirrors BuildExtendedProperties
		private void XbfEmitExtendedProperties(XbfNodeWriter nodes, XamlObjectDefinition objectDefinition, INamedTypeSymbol objectDefinitionType, XbfScope scope)
		{
			_generatorContext.CancellationToken.ThrowIfCancellationRequested();

			var extendedProperties = GetExtendedProperties(objectDefinition).ToArray();
			var isFrameworkElement = IsType(objectDefinitionType, Generation.FrameworkElementSymbol.Value);

			if (HasChildrenWithPhase(objectDefinition))
			{
				throw XbfNotSupported("x:Phase");
			}

			if (HasXBindMarkupExtension(objectDefinition))
			{
				throw XbfNotSupported("x:Bind");
			}

			if (HasMarkupExtensionNeedingComponent(objectDefinition) && IsDependencyObject(objectDefinition))
			{
				if (scope.HasNames)
				{
					nodes.WriteNodeType(ObjectWriterNodeType.UnoTrackResourceBindings);
					// Ensure that the namescope is property propagated to instances
					// that are not UIElements in order for ElementName to resolve properly
					nodes.WriteBool(scope.IsTemplate && !IsUIElement(objectDefinitionType));
				}
				else if (isFrameworkElement)
				{
					nodes.WriteNodeType(ObjectWriterNodeType.UnoTrackResourceBindings);
					nodes.WriteBool(false);
				}
			}

			var lazyProperties = extendedProperties.Where(IsLazyVisualStateManagerProperty).ToArray();

			foreach (var member in extendedProperties.Except(lazyProperties))
			{
				_generatorContext.CancellationToken.ThrowIfCancellationRequested();

				if (HasMarkupExtension(member))
				{
					if (!IsXLoadMember(member))
					{
						XbfEmitComplexPropertyValue(nodes, member, objectDefinitionType, scope);
					}

					// Otherwise, the x:Load attribute is already applied to the ElementStub
				}
				else if (HasCustomMarkupExtension(member))
				{
					throw XbfNotSupported("custom markup extension");
				}
				else if (member.Objects.Count > 0)
				{
					if (member.Member.Name == "_UnknownContent")
					{
						if (IsDictionary(objectDefinitionType))
						{
							throw XbfNotSupported("dictionary content");
						}

						foreach (var item in member.Objects)
						{
							XbfEmitObject(nodes, member, item, scope);
							XbfEmitAddToCollection(nodes, objectDefinitionType, item);
						}
					}
					else if (!IsType(objectDefinition.Type, member.Member.DeclaringType))
					{
						var ownerType = GetType(member.Member.DeclaringType!);
						var propertyType = GetPropertyTypeByOwnerSymbol(ownerType, member.Member.Name, member.LineNumber, member.LinePosition);

						if (member.Objects.Count == 1 && member.Objects[0] is var child && IsAssignableTo(child.Type, propertyType))
						{
							XbfEmitObject(nodes, member, child, scope);
							XbfEnsureAssignable(child, propertyType);

							nodes.WriteNodeType(ObjectWriterNodeType.SetValue);
							nodes.WriteUInt(XbfGetAttachedSetterId(ownerType, member.Member.Name));
						}
						else if (IsExactlyCollectionOrListType(propertyType))
						{
							// If the property is specifically an IList or an ICollection, the C# generation
							// provides an array. The node stream has no array to offer, so a list it is.
							var elementType = propertyType.IsGenericType ? propertyType.TypeArguments[0] : Generation.ObjectSymbol.Value;
							var elementTypeName = elementType.GetFullyQualifiedTypeIncludingGlobal();
							var listTypeName = $"global::System.Collections.Generic.List<{elementTypeName}>";
							var adderId = _xbf!.GetAdderId($"(({listTypeName})c).Add(({elementTypeName})v)");

							nodes.WriteNodeType(ObjectWriterNodeType.PushScopeCreateTypeBeginInit);
							nodes.WriteUInt(_xbf.GetTypeId(listTypeName, needsActivator: true));

							foreach (var inner in member.Objects)
							{
								XbfEmitObject(nodes, member, inner, scope);
								XbfEnsureAssignable(inner, elementType);

								nodes.WriteNodeType(ObjectWriterNodeType.UnoAddToCollectionTyped);
								nodes.WriteUInt(adderId);
							}

							nodes.WriteNodeType(ObjectWriterNodeType.EndInitPopScope);
							nodes.WriteNodeType(ObjectWriterNodeType.SetValue);
							nodes.WriteUInt(XbfGetAttachedSetterId(ownerType, member.Member.Name));
						}
						else if (IsCollectionOrListType(propertyType))
						{
							// If the property is a concrete type that implements an IList or
							// an ICollection, we must get the property and call add explicitly
							// on it.
							nodes.WriteNodeType(ObjectWriterNodeType.PushScopeGetValue);
							nodes.WriteUInt(XbfGetAttachedGetterId(ownerType, member.Member.Name, objectDefinitionType));

							foreach (var inner in member.Objects)
							{
								XbfEmitObject(nodes, member, inner, scope);
								XbfEmitAddToCollection(nodes, propertyType, inner);
							}

							nodes.WriteNodeType(ObjectWriterNodeType.PopScope);
						}
						else
						{
							throw XbfNotSupported($"XAML error: unassignable attached value {member.Member.DeclaringType?.Name}.{member.Member.Name}");
						}
					}
				}
				else
				{
					var isMemberInsideResourceDictionary = IsMemberInsideResourceDictionary(objectDefinition);
					var value = member.Value?.ToString();

					if (IsXNameMember(member, objectDefinition.Type) || (member.Member.Name == "Name" && !IsAttachedProperty(member)))
					{
						if (!isMemberInsideResourceDictionary.isInside)
						{
							if (!scope.HasNames)
							{
								throw XbfNotSupported("x:Name outside of a name scope");
							}

							ValidateName(value, member);

							nodes.WriteNodeType(ObjectWriterNodeType.SetName);
							nodes.WriteUInt(_xbf!.GetStringId(value!));

							if (scope.IsComponent && member.Member.Name == "Name" && !IsAttachedProperty(member))
							{
								// The element is a field of the class
								XbfConnect(nodes, objectDefinition, objectDefinitionType, $"this.{value} = e;");
								_xbfBackingFields.Add(new BackingFieldDefinition(objectDefinitionType.GetFullyQualifiedTypeIncludingGlobal(), value!, FindObjectFieldAccessibility(objectDefinition)));
							}
						}
						else if (IsXNameMember(member, objectDefinition.Type) && IsXNameProvider(objectDefinition.Type))
						{
							throw XbfNotSupported("x:Name of a resource");
						}
					}
					else if (member.Member.Name is "Key" or "FieldModifier" or "Class"
						|| IsXLoadMember(member)
						|| (member.Member.Name is "DefaultBindMode" or "DeferLoadStrategy" && member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace))
					{
						// Nothing to do
					}
					else if (member.Member.Name is "Uid" or "Phase")
					{
						throw XbfNotSupported($"x:{member.Member.Name}");
					}
					else if (
						member.Member.Name == "TargetName" &&
						IsAttachedProperty(member) &&
						member.Member.DeclaringType?.Name == "Storyboard"
					)
					{
						if (value is null || !scope.HasNames)
						{
							throw XbfNotSupported("Storyboard.TargetName outside of a name scope");
						}

						nodes.WriteNodeType(ObjectWriterNodeType.UnoSetStoryboardTargetName);
						nodes.WriteUInt(_xbf!.GetStringId(RewriteAttachedPropertyPath(value)));
						nodes.WriteUInt(_xbf.GetStringId(value));
						nodes.WriteBool(!scope.DeclaredNames.Contains(value));
					}
					else if (
						member.Member.Name == "TargetName" &&
						!IsAttachedProperty(member) &&
						(member.Member.DeclaringType?.Name.EndsWith("ThemeAnimation", StringComparison.Ordinal) ?? false)
					)
					{
						if (!scope.HasNames)
						{
							throw XbfNotSupported("ThemeAnimation outside of a name scope");
						}

						// Those special animations (xxxThemeAnimation) needs to resolve their target at runtime.
						nodes.WriteNodeType(ObjectWriterNodeType.UnoSetNameScope);
					}
					else if (
						member.Member.Name == "TargetProperty" &&
						IsAttachedProperty(member) &&
						member.Member.DeclaringType?.Name == "Storyboard"
					)
					{
						if (value is null)
						{
							throw new XamlGenerationException("The TargetProperty property cannot be empty", member);
						}

						nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
						nodes.WriteUInt(XbfGetAttachedSetterId(GetType(member.Member.DeclaringType), "TargetProperty"));
						nodes.WriteByte((byte)PersistedConstantType.IsSharedString);
						nodes.WriteUInt(_xbf!.GetStringId(RewriteAttachedPropertyPath(value)));
					}
					else if (
						member.Member.DeclaringType?.Name == "RelativePanel" &&
						IsAttachedProperty(member) &&
						IsRelativePanelSiblingProperty(member.Member.Name)
					)
					{
						if (value is null || !scope.HasNames)
						{
							throw XbfNotSupported("RelativePanel sibling outside of a name scope");
						}

						nodes.WriteNodeType(ObjectWriterNodeType.UnoSetValueElementNameSubject);
						nodes.WriteUInt(XbfGetAttachedSetterId(GetType(member.Member.DeclaringType), member.Member.Name));
						nodes.WriteUInt(_xbf!.GetStringId(value));
						nodes.WriteBool(!scope.DeclaredNames.Contains(value));
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
							if (_metadataHelper.FindPropertyTypeByOwnerSymbol(declaringTypeSymbol, member.Member.Name) is { } attachedPropertyType)
							{
								// BuildSetAttachedProperty
								var literalValue = XbfStaticExpression(() => BuildLiteralValue(member, owner: member, objectUid: ""));

								nodes.WriteNodeType(ObjectWriterNodeType.SetValueConstant);
								nodes.WriteUInt(XbfGetAttachedSetterId(declaringTypeSymbol!, member.Member.Name));
								XbfWriteConstant(nodes, literalValue, XbfGetAttachedPropertyDeclaredType(declaringTypeSymbol!, member.Member.Name), attachedPropertyType);
							}
							else if (eventSymbol != null)
							{
								if (member.Value is not string { Length: > 0 } handler || !SyntaxFacts.IsValidIdentifier(handler))
								{
									throw XbfNotSupported("event handler that is not a method name");
								}

								// WinUI connects the handlers the same way, from the Connect method of the class.
								XbfConnect(nodes, objectDefinition, objectDefinitionType, $"e.{eventSymbol.Name} += this.{handler};");
							}
							else
							{
								throw XbfNotSupported($"XAML error: unknown member {member.Member.DeclaringType?.Name}.{member.Member.Name}");
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
				var content = new XbfNodeWriter();

				XbfEmitLiteralProperties(content, objectDefinition, scope, lazyProperties: true);

				if (implicitContentChild != null && lazyContentProperty != null)
				{
					var child = implicitContentChild.Objects.First();

					XbfEmitObject(content, implicitContentChild, child, scope);
					XbfEnsureAssignable(child, lazyContentProperty.Type);

					content.WriteNodeType(ObjectWriterNodeType.SetValue);
					content.WriteUInt(XbfGetSetterId(objectDefinitionType, lazyContentProperty));
				}

				nodes.WriteNodeType(ObjectWriterNodeType.UnoSetDeferredBuilder);
				nodes.WriteBlock(content);
			}
		}

		// Mirrors BuildComplexPropertyValue
		private void XbfEmitComplexPropertyValue(XbfNodeWriter nodes, XamlMemberDefinition member, INamedTypeSymbol objectDefinitionType, XbfScope scope)
		{
			var bindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Binding");
			var bindNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Bind");
			var templateBindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "TemplateBinding");
			var declaringType = FindType(member.Member.DeclaringType);

			if (bindNode is not null)
			{
				throw XbfNotSupported("x:Bind");
			}

			if (_metadataHelper.FindEventType(declaringType, member.Member.Name) is not null)
			{
				throw XbfNotSupported("event handler");
			}

			if (GetCustomResourceResourceId(member) is not null)
			{
				throw XbfNotSupported("CustomResource");
			}

			if (declaringType is null)
			{
				throw XbfNotSupported($"XAML error: unknown type {member.Member.DeclaringType?.Name}");
			}

			var isDependencyProperty = IsDependencyProperty(declaringType, member.Member.Name);
			var bindingOptions = (bindingNode ?? templateBindingNode)?.Members;

			if (bindingOptions is not null)
			{
				var isBindingType = SymbolEqualityComparer.Default.Equals(_metadataHelper.FindPropertyTypeByOwnerSymbol(declaringType, member.Member.Name), Generation.DataBindingSymbol.Value);
				int propertyId;

				if (isDependencyProperty)
				{
					propertyId = XbfGetDependencyPropertyId(declaringType, member.Member.Name);
				}
				else if (isBindingType && declaringType.GetPropertyWithName(member.Member.Name) is { } bindingProperty)
				{
					propertyId = XbfGetSetterId(declaringType, bindingProperty, allowDependencyProperty: false);
				}
				else
				{
					throw XbfNotSupported($"binding on a property that is not a DependencyProperty: {declaringType.Name}.{member.Member.Name}");
				}

				var isTemplateBindingAttachedProperty = templateBindingNode is not null && IsAttachedProperty(declaringType, member.Member.Name);

				if (isDependencyProperty
					&& templateBindingNode is { Members.Count: 1 }
					&& templateBindingNode.Members[0] is { Member.Name: XamlConstants.PositionalParameters or "Path", Objects.Count: 0, Value: string templateBindingPath })
				{
					var path = isTemplateBindingAttachedProperty ? $"({templateBindingPath})" : templateBindingPath;

					nodes.WriteNodeType(ObjectWriterNodeType.SetValueFromTemplateBinding);
					nodes.WriteUInt(propertyId);
					nodes.WriteUInt(_xbf!.GetStringId(RewriteAttachedPropertyPath(path)));
				}
				else
				{
					if (bindingOptions.Any(x => IsCustomMarkupExtensionType(x.Objects.FirstOrDefault()?.Type)))
					{
						throw XbfNotSupported("custom markup extension in a binding");
					}

					string? elementName = null;
					var setters = new List<string>();

					foreach (var option in bindingOptions)
					{
						if (option.Member.Name == "ElementName")
						{
							if (!scope.HasNames)
							{
								throw XbfNotSupported("ElementName outside of a name scope");
							}

							// The subject is provided by the scope the node stream is run in.
							elementName = option.Value?.ToString() ?? throw new XamlGenerationException("The property ElementName cannot be empty", option);
							setters.Add($"ElementName = \"{elementName}\"");
						}
						else
						{
							setters.AddRange(XbfStaticExpressions(() => BuildMemberPropertySetters(option, isTemplateBindingAttachedProperty).ToArray()));
						}
					}

					if (templateBindingNode is not null)
					{
						setters.Add("RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)");
					}

					var binding = new StringBuilder($"new {XamlConstants.Types.Binding}() {{ ");
					binding.Append(string.Join(", ", setters.Where(s => !string.IsNullOrEmpty(s))));
					binding.Append(" }");

					foreach (var option in bindingOptions)
					{
						var themeResourceCandidate = option.Objects.FirstOrDefault();
						if (themeResourceCandidate?.Type is { PreferredXamlNamespace: XamlConstants.PresentationXamlXmlNamespace, Name: "ThemeResource" })
						{
							if (option.Member.Name == "TargetNullValue" && themeResourceCandidate.Members.FirstOrDefault()?.Value is string targetNullValueKey)
							{
								binding.Append(XbfStaticExpression(() => $".ApplyTargetNullValueThemeResource(@\"{targetNullValueKey}\", {ParseContextPropertyAccess})"));
							}
							else if (option.Member.Name == "FallbackValue" && themeResourceCandidate.Members.FirstOrDefault()?.Value is string fallbackValueKey)
							{
								binding.Append(XbfStaticExpression(() => $".ApplyFallbackValueThemeResource(@\"{fallbackValueKey}\", {ParseContextPropertyAccess})"));
							}
						}
					}

					nodes.WriteNodeType(ObjectWriterNodeType.PushConstant);
					nodes.WriteByte((byte)PersistedConstantType.UnoIsGenerated);
					nodes.WriteUInt(_xbf!.GetConstantId(binding.ToString()));

					if (elementName is not null)
					{
						nodes.WriteNodeType(ObjectWriterNodeType.UnoSetBindingElementName);
						nodes.WriteUInt(_xbf.GetStringId(elementName));
						nodes.WriteBool(!scope.DeclaredNames.Contains(elementName));
					}

					nodes.WriteNodeType(ObjectWriterNodeType.SetValueFromMarkupExtension);
					nodes.WriteUInt(propertyId);
				}
			}

			var (resourceKey, isThemeResourceExtension) = GetStaticResourceKey(member);

			if (resourceKey != null)
			{
				if (isDependencyProperty)
				{
					nodes.WriteNodeType(isThemeResourceExtension ? ObjectWriterNodeType.SetValueFromThemeResource : ObjectWriterNodeType.SetValueFromStaticResource);
					nodes.WriteUInt(XbfGetDependencyPropertyId(declaringType, member.Member.Name));
					nodes.WriteUInt(_xbf!.GetStringId(resourceKey));
				}
				else
				{
					var targetPropertyType = (IsAttachedProperty(member) ? GetAttachedPropertyType(member) : FindPropertyType(member.Member))
						?? Generation.ObjectSymbol.Value;

					// If the property Type is attributed with the CreateFromStringAttribute
					if (IsXamlTypeConverter(targetPropertyType))
					{
						throw XbfNotSupported("converted resource reference");
					}

					var propertyId = IsAttachedProperty(member)
						? XbfGetAttachedSetterId(declaringType, member.Member.Name)
						: XbfGetSetterId(declaringType, declaringType.GetPropertyWithName(member.Member.Name) ?? throw XbfNotSupported("property not found"), allowDependencyProperty: false);

					// Load-time resolution isn't feasible for non-DPs, so we just set the Application-level value right away, and that's it.
					nodes.WriteNodeType(ObjectWriterNodeType.UnoSetValueFromResourceStatic);
					nodes.WriteUInt(propertyId);
					nodes.WriteUInt(_xbf!.GetStringId(resourceKey));
					nodes.WriteUInt(XbfGetTypeId(targetPropertyType, needsActivator: false));
				}
			}
		}

		private static bool IsXbfIgnoredDirective(XamlMemberDefinition member)
			=> member.Member.Name is "Key" or "Name" && member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace;

		/// <summary>
		/// Builds an expression that is evaluated by the metadata provider rather than by the class the XAML file generates.
		/// </summary>
		private string XbfStaticExpression(Func<string> builder)
		{
			var isInSingletonInstance = _isInSingletonInstance;
			_isInSingletonInstance = false;
			try
			{
				return builder();
			}
			finally
			{
				_isInSingletonInstance = isInSingletonInstance;
			}
		}

		private string[] XbfStaticExpressions(Func<string[]> builder)
		{
			var isInSingletonInstance = _isInSingletonInstance;
			_isInSingletonInstance = false;
			try
			{
				return builder();
			}
			finally
			{
				_isInSingletonInstance = isInSingletonInstance;
			}
		}

		private bool IsXbfAccessible(ISymbol symbol)
			=> symbol.DeclaredAccessibility == Accessibility.Public
				|| (symbol.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedOrInternal
					&& SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, _metadataHelper.Compilation.Assembly));

		private int XbfGetTypeId(ITypeSymbol type, bool needsActivator)
			=> _xbf!.GetTypeId(type.GetFullyQualifiedTypeIncludingGlobal(), needsActivator);

		private int XbfGetDependencyPropertyId(INamedTypeSymbol declaringType, string name)
			=> _xbf!.GetDependencyPropertyId($"{declaringType.GetFullyQualifiedTypeIncludingGlobal()}.{name}Property");

		/// <summary>
		/// Gets the id of a property to set, either through its DependencyProperty or through its setter.
		/// </summary>
		private int XbfGetSetterId(INamedTypeSymbol declaringType, IPropertySymbol property, bool allowDependencyProperty = true)
		{
			if (allowDependencyProperty
				&& ((ISymbol?)declaringType.GetPropertyWithName(property.Name + "Property") ?? declaringType.GetFieldWithName(property.Name + "Property")) is { IsStatic: true } dependencyProperty
				&& IsXbfAccessible(dependencyProperty)
				&& IsType(declaringType, Generation.DependencyObjectSymbol.Value))
			{
				return XbfGetDependencyPropertyId(declaringType, property.Name);
			}

			if (property.SetMethod is not { } setMethod || !IsXbfAccessible(setMethod) || property.IsStatic || property.ContainingType.TypeKind == TypeKind.Struct)
			{
				throw XbfNotSupported("property without a setter");
			}

			var owner = property.ContainingType.GetFullyQualifiedTypeIncludingGlobal();

			return _xbf!.GetSetterId(
				$"{owner}.{property.Name}",
				$"(({owner})t).{property.Name} = ({property.Type.GetFullyQualifiedTypeIncludingGlobal()})v");
		}

		private int XbfGetGetterId(IPropertySymbol property)
		{
			if (property.GetMethod is not { } getMethod || !IsXbfAccessible(getMethod) || property.IsStatic)
			{
				throw XbfNotSupported("property without a getter");
			}

			var owner = property.ContainingType.GetFullyQualifiedTypeIncludingGlobal();

			return _xbf!.GetGetterId($"{owner}.{property.Name}", $"(({owner})t).{property.Name}");
		}

		private IMethodSymbol XbfGetAttachedAccessor(INamedTypeSymbol ownerType, string prefix, string name, int parameterCount)
		{
			for (var type = ownerType; type is not null && type.SpecialType != SpecialType.System_Object; type = type.BaseType)
			{
				if (type.GetMethodsWithName(prefix + name).FirstOrDefault(m => m.IsStatic && m.Parameters.Length == parameterCount) is { } accessor)
				{
					if (!IsXbfAccessible(accessor))
					{
						break;
					}

					return accessor;
				}
			}

			throw XbfNotSupported("attached property without an accessor");
		}

		private ITypeSymbol XbfGetAttachedPropertyDeclaredType(INamedTypeSymbol ownerType, string name)
			=> XbfGetAttachedAccessor(ownerType, "Set", name, 2).Parameters[1].Type;

		private int XbfGetAttachedSetterId(INamedTypeSymbol ownerType, string name)
		{
			if (((ISymbol?)ownerType.GetPropertyWithName(name + "Property") ?? ownerType.GetFieldWithName(name + "Property")) is { IsStatic: true } dependencyProperty
				&& IsXbfAccessible(dependencyProperty))
			{
				return XbfGetDependencyPropertyId(ownerType, name);
			}

			var setter = XbfGetAttachedAccessor(ownerType, "Set", name, 2);
			var owner = setter.ContainingType.GetFullyQualifiedTypeIncludingGlobal();

			return _xbf!.GetSetterId(
				$"{owner}.{name}",
				$"{owner}.Set{name}(({setter.Parameters[0].Type.GetFullyQualifiedTypeIncludingGlobal()})t, ({setter.Parameters[1].Type.GetFullyQualifiedTypeIncludingGlobal()})v)");
		}

		private int XbfGetAttachedGetterId(INamedTypeSymbol ownerType, string name, INamedTypeSymbol targetType)
		{
			if (ownerType.GetFirstMethodWithName("Get" + name) is null)
			{
				// Plain object
				return XbfGetGetterId(targetType.GetPropertyWithName(name) ?? throw XbfNotSupported("property not found"));
			}

			var getter = XbfGetAttachedAccessor(ownerType, "Get", name, 1);
			var owner = getter.ContainingType.GetFullyQualifiedTypeIncludingGlobal();

			return _xbf!.GetGetterId(
				$"{owner}.{name}",
				$"{owner}.Get{name}(({getter.Parameters[0].Type.GetFullyQualifiedTypeIncludingGlobal()})t)");
		}

		/// <summary>
		/// Ensures that the object can be given to a member of the given type as is: the node stream
		/// carries instances, which no user-defined conversion gets applied to.
		/// </summary>
		private void XbfEnsureAssignable(XamlObjectDefinition value, ITypeSymbol targetType)
		{
			if (value.Type.Name is "NullExtension" or "StaticResource" or "ThemeResource")
			{
				return;
			}

			var valueType = FindType(value.Type);
			if (valueType is null)
			{
				throw XbfNotSupported("unknown type");
			}

			if (IsNewScope(value))
			{
				return;
			}

			var conversion = ((CSharpCompilation)_metadataHelper.Compilation).ClassifyConversion(valueType, targetType);
			if (!(conversion.IsIdentity || (conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing || conversion.IsNullable))))
			{
				throw XbfNotSupported("converted object value");
			}
		}

		private void XbfEmitAddToCollection(XbfNodeWriter nodes, ITypeSymbol collectionType, XamlObjectDefinition item)
		{
			var itemType = item.Type.Name is "NullExtension" or "StaticResource" or "ThemeResource" ? null : FindType(item.Type);

			var candidates = collectionType
				.GetAllMembersWithName("Add")
				.OfType<IMethodSymbol>()
				.Where(m => !m.IsStatic && m.Parameters.Length == 1 && m.DeclaredAccessibility == Accessibility.Public)
				.ToArray();

			IMethodSymbol? adder = null;
			if (itemType is not null)
			{
				adder = candidates.FirstOrDefault(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, itemType))
					?? candidates.FirstOrDefault(m =>
					{
						var conversion = ((CSharpCompilation)_metadataHelper.Compilation).ClassifyConversion(itemType, m.Parameters[0].Type);
						return conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing);
					});
			}
			else if (candidates.Select(m => m.Parameters[0].Type).Distinct(SymbolEqualityComparer.Default).Count() == 1)
			{
				adder = candidates[0];
			}

			if (adder is null)
			{
				throw XbfNotSupported("collection without a matching Add");
			}

			var receiver = adder.ContainingType.TypeKind == TypeKind.Interface ? adder.ContainingType : collectionType;

			nodes.WriteNodeType(ObjectWriterNodeType.UnoAddToCollectionTyped);
			nodes.WriteUInt(_xbf!.GetAdderId(
				$"(({receiver.GetFullyQualifiedTypeIncludingGlobal()})c).Add(({adder.Parameters[0].Type.GetFullyQualifiedTypeIncludingGlobal()})v)"));
		}

		private static readonly Regex _xbfNumber = new(@"^-?[0-9]+(\.[0-9]+)?$", RegexOptions.Compiled);
		private static readonly Regex _xbfDouble = new(@"^(-?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)d$", RegexOptions.Compiled);
		private static readonly Regex _xbfEnumMember = new(@"^global::([A-Za-z_][A-Za-z0-9_.]*)\.([A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
		private static readonly Regex _xbfTypeof = new(@"^typeof\((global::[A-Za-z_][A-Za-z0-9_.<>,: ]*)\)$", RegexOptions.Compiled);
		private static readonly Regex _xbfThickness = new(@"^new global::Microsoft\.UI\.Xaml\.Thickness\(([^()]*)\)$", RegexOptions.Compiled);
		private static readonly Regex _xbfCornerRadius = new(@"^new (?:global::)?Microsoft\.UI\.Xaml\.CornerRadius\(([^()]*)\)$", RegexOptions.Compiled);
		private static readonly Regex _xbfGridLength = new(@"^new global::Microsoft\.UI\.Xaml\.GridLength\((-?[0-9.]+)f, global::Microsoft\.UI\.Xaml\.GridUnitType\.(Auto|Pixel|Star)\)$", RegexOptions.Compiled);
		private static readonly Regex _xbfColor = new(@"^global::Microsoft\.UI\.ColorHelper\.FromARGB\(([^()]*)\)$", RegexOptions.Compiled);

		/// <summary>
		/// Writes a value, given the C# expression the generator would have assigned.
		/// </summary>
		/// <remarks>
		/// The common literals are written into the node stream. Any other value is left to the generated
		/// metadata provider to compute, so that it is exactly the one the C# generation would have produced.
		/// </remarks>
		/// <param name="declaredType">The type of the member the value is assigned to.</param>
		/// <param name="valueType">The type the literal was built for, which a Setter knows better than its Value property.</param>
		private void XbfWriteConstant(XbfNodeWriter nodes, string expression, ITypeSymbol declaredType, INamedTypeSymbol? valueType)
		{
			expression = expression.Trim();

			var underlyingType = declaredType is INamedTypeSymbol namedDeclaredType ? FindUnderlyingType(namedDeclaredType) : declaredType;
			var isObject = underlyingType.SpecialType == SpecialType.System_Object;
			var literalType = isObject && valueType is not null ? FindUnderlyingType(valueType) : underlyingType;

			if (expression == "null")
			{
				nodes.WriteByte((byte)PersistedConstantType.UnoIsNull);
				return;
			}

			switch (literalType.SpecialType)
			{
				case SpecialType.System_Boolean when expression is "true" or "false":
					nodes.WriteByte((byte)(expression == "true" ? PersistedConstantType.IsBoolTrue : PersistedConstantType.IsBoolFalse));
					return;

				case SpecialType.System_Double when XbfTryParseDouble(expression, out var doubleValue):
					XbfWriteDouble(nodes, doubleValue);
					return;

				case SpecialType.System_Int32 when int.TryParse(expression, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var intValue):
					nodes.WriteByte((byte)PersistedConstantType.IsSigned);
					nodes.WriteInt32(intValue);
					return;

				case SpecialType.System_String or SpecialType.System_Object when XbfTryParseString(expression, out var stringValue):
					XbfWriteString(nodes, stringValue);
					return;
			}

			if (literalType.TypeKind == TypeKind.Enum
				&& literalType is INamedTypeSymbol { EnumUnderlyingType.SpecialType: SpecialType.System_Int32 or SpecialType.System_Byte or SpecialType.System_UInt32 } enumType
				&& _xbfEnumMember.Match(expression) is { Success: true } enumMatch
				&& enumMatch.Groups[1].Value == enumType.GetFullyQualifiedTypeExcludingGlobal()
				&& enumType.GetMembers(enumMatch.Groups[2].Value).OfType<IFieldSymbol>().FirstOrDefault() is { HasConstantValue: true } enumField)
			{
				nodes.WriteByte((byte)PersistedConstantType.IsEnum);
				nodes.WriteUInt(XbfGetTypeId(enumType, needsActivator: false));
				nodes.WriteInt32(Convert.ToInt32(enumField.ConstantValue, CultureInfo.InvariantCulture));
				return;
			}

			switch (literalType.GetFullyQualifiedTypeExcludingGlobal())
			{
				case "System.Type" when _xbfTypeof.Match(expression) is { Success: true } typeofMatch:
					nodes.WriteByte((byte)PersistedConstantType.UnoIsType);
					nodes.WriteUInt(_xbf!.GetTypeId(typeofMatch.Groups[1].Value, needsActivator: false));
					return;

				case XamlConstants.Types.Thickness when _xbfThickness.Match(expression) is { Success: true } thicknessMatch
					&& XbfTryParseFloats(thicknessMatch.Groups[1].Value, out var thickness):
					if (thickness.Length is 1 or 4)
					{
						nodes.WriteByte((byte)PersistedConstantType.IsThickness);
						for (var i = 0; i < 4; i++)
						{
							nodes.WriteSingle(thickness[thickness.Length == 1 ? 0 : i]);
						}
						return;
					}
					break;

				case XamlConstants.Types.CornerRadius when _xbfCornerRadius.Match(expression) is { Success: true } cornerRadiusMatch
					&& XbfTryParseFloats(cornerRadiusMatch.Groups[1].Value, out var cornerRadius):
					if (cornerRadius.Length is 1 or 4)
					{
						nodes.WriteByte((byte)PersistedConstantType.UnoIsCornerRadius);
						for (var i = 0; i < 4; i++)
						{
							nodes.WriteSingle(cornerRadius[cornerRadius.Length == 1 ? 0 : i]);
						}
						return;
					}
					break;

				case XamlConstants.Types.GridLength when _xbfGridLength.Match(expression) is { Success: true } gridLengthMatch
					&& float.TryParse(gridLengthMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gridLengthValue):
					nodes.WriteByte((byte)PersistedConstantType.IsGridLength);
					nodes.WriteInt32(gridLengthMatch.Groups[2].Value switch { "Auto" => 0, "Pixel" => 1, _ => 2 });
					nodes.WriteSingle(gridLengthValue);
					return;

				case "Windows.UI.Color" when _xbfColor.Match(expression) is { Success: true } colorMatch
					&& XbfTryParseColor(colorMatch.Groups[1].Value, out var color):
					nodes.WriteByte((byte)PersistedConstantType.IsColor);
					nodes.WriteInt32(unchecked((int)color));
					return;
			}

			// The cast applies the conversion the assignment would have, as the node stream cannot.
			var cast = isObject ? "" : $"({declaredType.GetFullyQualifiedTypeIncludingGlobal()})";

			nodes.WriteByte((byte)PersistedConstantType.UnoIsGenerated);
			nodes.WriteUInt(_xbf!.GetConstantId($"{cast}({expression})"));
		}

		private void XbfWriteString(XbfNodeWriter nodes, string value)
		{
			nodes.WriteByte((byte)PersistedConstantType.IsSharedString);
			nodes.WriteUInt(_xbf!.GetStringId(value));
		}

		private static void XbfWriteDouble(XbfNodeWriter nodes, double value)
		{
			// WinUI stores every floating point value as a float; a double that does not fit is kept whole.
			if ((double)(float)value == value || double.IsNaN(value))
			{
				nodes.WriteByte((byte)PersistedConstantType.IsFloat);
				nodes.WriteSingle((float)value);
			}
			else
			{
				nodes.WriteByte((byte)PersistedConstantType.UnoIsDouble);
				nodes.WriteDouble(value);
			}
		}

		private static bool XbfTryParseDouble(string expression, out double value)
		{
			switch (expression)
			{
				case "double.NaN":
					value = double.NaN;
					return true;
				case "double.PositiveInfinity":
					value = double.PositiveInfinity;
					return true;
				case "double.NegativeInfinity":
					value = double.NegativeInfinity;
					return true;
			}

			if (_xbfDouble.Match(expression) is { Success: true } match)
			{
				return double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
			}

			value = 0;
			return false;
		}

		/// <summary>
		/// Reads back a string literal written through DoubleEscape.
		/// </summary>
		private static bool XbfTryParseString(string expression, out string value)
		{
			value = "";

			if (expression.Length < 2 || expression[0] != '"' || expression[expression.Length - 1] != '"')
			{
				return false;
			}

			var builder = new StringBuilder(expression.Length);
			for (var i = 1; i < expression.Length - 1; i++)
			{
				var c = expression[i];
				if (c == '"')
				{
					// Not a single literal
					return false;
				}

				if (c == '\\')
				{
					if (++i >= expression.Length - 1)
					{
						return false;
					}

					switch (expression[i])
					{
						case '\\': builder.Append('\\'); break;
						case '"': builder.Append('"'); break;
						case 'n': builder.Append('\n'); break;
						case 'r': builder.Append('\r'); break;
						case 't': builder.Append('\t'); break;
						case 'u' when i + 4 < expression.Length - 1
							&& ushort.TryParse(expression.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code):
							builder.Append((char)code);
							i += 4;
							break;
						default:
							return false;
					}
				}
				else
				{
					builder.Append(c);
				}
			}

			value = builder.ToString();
			return true;
		}

		private static bool XbfTryParseFloats(string arguments, out float[] values)
		{
			var parts = arguments.Split(',');
			values = new float[parts.Length];

			for (var i = 0; i < parts.Length; i++)
			{
				var part = parts[i].Trim().TrimEnd('f');
				if (!_xbfNumber.IsMatch(part)
					|| !double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
					|| (double)(float)value != value)
				{
					return false;
				}

				values[i] = (float)value;
			}

			return true;
		}

		private static bool XbfTryParseColor(string arguments, out uint color)
		{
			color = 0;

			var parts = arguments.Split(',');
			if (parts.Length != 4)
			{
				return false;
			}

			foreach (var part in parts)
			{
				var text = part.Trim();
				byte channel;

				if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				{
					if (!byte.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out channel))
					{
						return false;
					}
				}
				else if (!byte.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out channel))
				{
					return false;
				}

				color = (color << 8) | channel;
			}

			return true;
		}
	}
}
