#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlFileGenerator
	{
		private Func<string, INamedTypeSymbol?>? _findType;
		private Func<XamlType, INamedTypeSymbol?>? _findTypeByXamlType;

		private XClassName? _xClassName;
		private string[]? _clrNamespaces;

		record XClassName(string Namespace, string ClassName, INamedTypeSymbol? Symbol)
		{
			public override string ToString()
				=> Symbol?.GetFullyQualifiedTypeIncludingGlobal() ?? Namespace + "." + ClassName;
		}

		private void InitCaches()
		{
			_findType = Funcs.Create<string, INamedTypeSymbol?>(SourceFindType).AsMemoized();
			_findTypeByXamlType = Funcs.Create<XamlType, INamedTypeSymbol?>(SourceFindTypeByXamlType).AsMemoized();

			var defaultXmlNamespace = _fileDefinition
				.Namespaces
				.Where(n => n.Prefix.None())
				.FirstOrDefault()
				?.Namespace ?? "";

			_clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(defaultXmlNamespace, Array.Empty<string>());
		}

		/// <summary>
		/// Gets the full target type, ensuring it is prefixed by "global::"
		/// to avoid namespace conflicts
		/// </summary>
		private string GetGlobalizedTypeName(string? fullTargetType)
		{
			fullTargetType ??= "";

			// Only prefix if it isn't already prefixed and if the type is fully qualified with a namespace
			// as opposed to, for instance, "double" or "Style"
			if (!fullTargetType.StartsWith(GlobalPrefix, StringComparison.Ordinal)
				&& fullTargetType.Contains(QualifiedNamespaceMarker))
			{
				return string.Format(CultureInfo.InvariantCulture, "{0}{1}", GlobalPrefix, fullTargetType);
			}

			return fullTargetType;
		}

		private bool IsType(XamlType xamlType, XamlType? baseType)
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
				return IsType(xamlType, clrBaseType);
			}
			else
			{
				return false;
			}
		}

		private bool IsType(XamlType xamlType, ISymbol? typeSymbol)
		{
			var type = FindType(xamlType);

			return IsType(type, typeSymbol);
		}

		private bool IsAssignableTo(XamlType xamlType, ISymbol? typeSymbol)
			=> IsType(xamlType, typeSymbol) || (xamlType.Name is "NullExtension" && xamlType.PreferredXamlNamespace is XamlConstants.XamlXmlNamespace);

		private static bool IsType([NotNullWhen(true)] INamedTypeSymbol? namedTypeSymbol, ISymbol? typeSymbol)
		{
			if (namedTypeSymbol != null)
			{
				if (typeSymbol is INamedTypeSymbol { SpecialType: SpecialType.System_Object })
				{
					// Everything is an object.
					return true;
				}

				if (typeSymbol is INamedTypeSymbol { TypeKind: TypeKind.Interface })
				{
					return namedTypeSymbol.AllInterfaces.Contains(typeSymbol, SymbolEqualityComparer.Default);
				}

				do
				{
					if (SymbolEqualityComparer.Default.Equals(namedTypeSymbol, typeSymbol))
					{
						return true;
					}

					namedTypeSymbol = namedTypeSymbol.BaseType;

					if (namedTypeSymbol == null)
					{
						break;
					}

				} while (namedTypeSymbol.SpecialType != SpecialType.System_Object);
			}

			return false;
		}

		public bool HasProperty(XamlType xamlType, string propertyName)
		{
			var type = FindType(xamlType);
			return type.GetPropertyWithName(propertyName) is not null;
		}

		private bool IsRun(INamedTypeSymbol? symbol)
		{
			return IsType(symbol, Generation.RunSymbol.Value);
		}

		private bool IsSpan(INamedTypeSymbol? symbol)
		{
			return IsType(symbol, Generation.SpanSymbol.Value);
		}

		private bool IsImplementingInterface(INamedTypeSymbol? symbol, INamedTypeSymbol? interfaceName)
		{
			bool isSameType(INamedTypeSymbol source, INamedTypeSymbol? iface) =>
				SymbolEqualityComparer.Default.Equals(source, iface) || SymbolEqualityComparer.Default.Equals(source.OriginalDefinition, iface);

			if (symbol != null)
			{
				if (isSameType(symbol, interfaceName))
				{
					return true;
				}

				foreach (var @interface in symbol.AllInterfaces)
				{
					if (isSameType(@interface, interfaceName))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool IsBorder(INamedTypeSymbol? symbol)
		{
			return IsType(symbol, Generation.BorderSymbol.Value);
		}

		private bool IsFrameworkElement(XamlType xamlType)
		{
			return IsType(xamlType, Generation.FrameworkElementSymbol.Value);
		}

		private bool IsAndroidView(XamlType xamlType)
		{
			return IsType(xamlType, Generation.AndroidViewSymbol.Value);
		}

		private bool IsIOSUIView(XamlType xamlType)
		{
			return IsType(xamlType, Generation.IOSViewSymbol.Value);
		}

		private bool IsDependencyObject(XamlObjectDefinition component)
			=> GetType(component.Type).GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, Generation.DependencyObjectSymbol.Value));

		private bool IsUIElement(INamedTypeSymbol? symbol)
			=> IsType(symbol, Generation.UIElementSymbol.Value);

		/// <summary>
		/// Is the type derived from the native view type on a Xamarin platform?
		/// </summary>
		private bool IsNativeView(XamlType xamlType) => IsAndroidView(xamlType) || IsIOSUIView(xamlType);

		/// <summary>
		/// Is the type one of the base view types in WinUI? (UIElement is most commonly used to mean 'any WinUI view type,' but
		/// FrameworkElement is valid too)
		/// </summary>
		private bool IsManagedViewBaseType(INamedTypeSymbol? targetType) => SymbolEqualityComparer.Default.Equals(targetType, Generation.UIElementSymbol.Value) || SymbolEqualityComparer.Default.Equals(targetType, Generation.FrameworkElementSymbol.Value);

		private static bool IsDependencyProperty(INamedTypeSymbol? propertyOwner, string name)
		{
			return propertyOwner.GetPropertyWithName(name + "Property") is not null ||
				propertyOwner.GetFieldWithName(name + "Property") is not null;
		}

		private bool HasIsParsing(INamedTypeSymbol? type)
		{
			return IsImplementingInterface(type, Generation.DependencyObjectParseSymbol.Value);
		}

		private Accessibility FindObjectFieldAccessibility(XamlObjectDefinition objectDefinition)
		{
			if (
				FindMember(objectDefinition, "FieldModifier") is XamlMemberDefinition fieldModifierMember
				&& Enum.TryParse<Accessibility>(fieldModifierMember.Value?.ToString(), true, out var modifierValue)
			)
			{
				return modifierValue;
			}

			return Accessibility.Private;
		}

		private string FormatAccessibility(Accessibility accessibility)
		{
			switch (accessibility)
			{
				case Accessibility.Private:
					return "private";

				case Accessibility.Internal:
					return "internal";

				case Accessibility.Public:
					return "public";

				default:
					throw new NotSupportedException($"Field modifier {accessibility} is not supported");
			}
		}

		private INamedTypeSymbol GetPropertyTypeByOwnerSymbol(INamedTypeSymbol ownerType, string propertyName, int lineNumber, int linePosition, [CallerMemberName] string caller = "")
		{
			var definition = _metadataHelper.FindPropertyTypeByOwnerSymbol(ownerType, propertyName);

			if (definition == null)
			{
				throw new Exception($"The property {ownerType}.{propertyName} is unknown. Line number: {lineNumber}, Line position: {linePosition}, Caller: {caller}, File: {_fileDefinition.FilePath}");
			}

			return definition;
		}

		private ISymbol? FindProperty(XamlMember xamlMember)
		{
			var type = FindType(xamlMember.DeclaringType);
			return _metadataHelper.FindPropertyByOwnerSymbol(type, xamlMember.Name);
		}

		private ISymbol? FindProperty(XamlMemberDefinition xamlMemberDefinition)
		{
			var declaringType = xamlMemberDefinition.Member.DeclaringType;

			var type = IsCustomMarkupExtensionType(declaringType)
				? GetMarkupExtensionType(declaringType)
				: FindType(xamlMemberDefinition.Member.DeclaringType);

			return _metadataHelper.FindPropertyByOwnerSymbol(type, xamlMemberDefinition.Member.Name);
		}

		private INamedTypeSymbol? FindPropertyType(XamlMember xamlMember) => FindProperty(xamlMember)?.FindDependencyPropertyType();

		private bool IsAttachedProperty(XamlMemberDefinition member)
		{
			if (member.Member.DeclaringType != null)
			{
				var type = FindType(member.Member.DeclaringType);
				return IsAttachedProperty(type, member.Member.Name);
			}

			return false;
		}

		private bool IsAttachedProperty(INamedTypeSymbol? declaringType, string name) => _metadataHelper.IsAttachedProperty(declaringType, name);

		private static bool IsRelevantNamespace(string? xamlNamespace)
		{
			if (XamlConstants.XmlXmlNamespace.Equals(xamlNamespace, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return true;
		}

		private static bool IsRelevantProperty(XamlMember? member, XamlObjectDefinition objectDefinition)
		{
			if (member?.Name == "Phase") // Phase is not relevant as it's not an actual property
			{
				return false;
			}

			if (member?.Name == "IsNativeStyle" && objectDefinition.Type.Name == "Style")
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get the type of the attached property.
		/// </summary>
		private INamedTypeSymbol GetAttachedPropertyType(XamlMemberDefinition member)
		{
			var type = GetType(member.Member.DeclaringType);
			return GetAttachedPropertyType(type, member.Member.Name);
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
			var property = GetPropertyWithName(declaringType, propertyName);

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
			return !property.SetMethod.SelectOrDefault(m => m!.DeclaredAccessibility == Accessibility.Public, false);
		}

		/// <summary>
		/// Returns true if the property has an accessible public setter and has a parameterless constructor
		/// </summary>
		private bool IsNewableProperty(IPropertySymbol property, [NotNullWhen(true)] out string? newableTypeName)
		{
			if (property is
				{
					SetMethod.DeclaredAccessibility: Accessibility.Public,
					Type: INamedTypeSymbol propertyType
				}
				&& propertyType.Constructors.Any(ms => ms.Parameters.Length == 0))
			{
				newableTypeName = GetFullGenericTypeName(propertyType);
				return true;
			}
			else
			{
				newableTypeName = null;
				return false;
			}
		}

		/// <summary>
		/// Returns true if the type implements either ICollection, IList or one of their generics
		/// </summary>
		private bool IsCollectionOrListType(INamedTypeSymbol? propertyType)
			=> IsImplementingInterface(propertyType, Generation.ICollectionSymbol.Value)
			|| IsImplementingInterface(propertyType, Generation.ICollectionOfTSymbol.Value)
			|| IsImplementingInterface(propertyType, Generation.IListSymbol.Value)
			|| IsImplementingInterface(propertyType, Generation.IListOfTSymbol.Value);

		/// <summary>
		/// Returns true if the type implements <see cref="IDictionary{TKey, TValue}"/>
		/// </summary>
		private bool IsDictionary(INamedTypeSymbol? propertyType)
			=> IsImplementingInterface(propertyType, Generation.IDictionaryOfTKeySymbol.Value);

		/// <summary>
		/// Returns true if the type exactly implements either ICollection, IList or one of their generics
		/// </summary>
		private bool IsExactlyCollectionOrListType(INamedTypeSymbol type)
		{
			return SymbolEqualityComparer.Default.Equals(type, Generation.ICollectionSymbol.Value)
				|| type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_ICollection_T
				|| SymbolEqualityComparer.Default.Equals(type, Generation.IListSymbol.Value)
				|| type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IList_T;
		}

		/// <summary>
		/// Determines if the provided object definition is of a C# initializable list
		/// </summary>
		private bool IsInitializableCollection(XamlObjectDefinition definition, INamedTypeSymbol type)
		{
			if (definition.Members.Any(m => m.Member.Name != "_UnknownContent"))
			{
				return false;
			}

			return IsImplementingInterface(type, Generation.ICollectionSymbol.Value)
				|| IsImplementingInterface(type, Generation.ICollectionOfTSymbol.Value);
		}

		/// <summary>
		/// Gets the
		/// </summary>
		private IPropertySymbol? GetPropertyWithName(XamlType declaringType, string propertyName)
		{
			var type = FindType(declaringType);
			return type.GetPropertyWithName(propertyName);
		}

		private static bool IsDouble(string typeName)
		{
			// handles cases where name is "Java.Lang.Double"
			// TODO: Fix type resolution
			return typeName.Equals("double", StringComparison.InvariantCultureIgnoreCase)
				|| typeName.EndsWith(".double", StringComparison.InvariantCultureIgnoreCase);
		}

		private static bool IsString(XamlObjectDefinition xamlObjectDefinition)
		{
			return xamlObjectDefinition.Type.Name == "String";
		}

		private static bool IsPrimitive(XamlObjectDefinition xamlObjectDefinition)
		{
			switch (xamlObjectDefinition.Type.Name)
			{
				case "Byte":
				case "Int16":
				case "Int32":
				case "Int64":
				case "UInt16":
				case "UInt32":
				case "UInt64":
				case "Single":
				case "Double":
				case "Boolean":
					return true;
			}

			return false;
		}

		private static bool HasInitializer(XamlObjectDefinition objectDefinition)
		{
			return objectDefinition.Members.Any(m => m.Member.Name == "_Initialization");
		}

		private INamedTypeSymbol? FindType(string name)
			=> _findType!(name);

		private INamedTypeSymbol? FindType(XamlType? type)
			=> type != null ? _findTypeByXamlType!(type) : null;

		private INamedTypeSymbol? SourceFindTypeByXamlType(XamlType type)
		{
			if (type != null)
			{
				// Search first using the explicit XML namespace in known namespaces

				// Remove the namespace conditionals declaration
				var trimmedNamespace = type.PreferredXamlNamespace.Split('?').First();
				var clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(trimmedNamespace, Array.Empty<string>());

				foreach (var clrNamespace in clrNamespaces)
				{
					if (_metadataHelper.FindTypeByFullName(clrNamespace + "." + type.Name) is INamedTypeSymbol result)
					{
						return result;
					}
				}

				if (
					type.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace
					&& type.Name == "Bind"
				   )
				{
					return Generation.DataBindingSymbol.Value;
				}

				var nsName = GetTrimmedNamespace(trimmedNamespace);
				if (nsName.IndexOf("#using:", StringComparison.Ordinal) is int indexOfHashUsing && indexOfHashUsing > -1)
				{
					if (SearchClrNamespaces(type.Name) is INamedTypeSymbol symbolFromCLRNamespace)
					{
						return symbolFromCLRNamespace;
					}

					var hashUsingNamespaces = nsName.Substring(indexOfHashUsing + "#using:".Length).Split(';');
					foreach (var hashUsingNamespace in hashUsingNamespaces)
					{
						if (_metadataHelper.FindTypeByFullName(hashUsingNamespace + "." + type.Name) is INamedTypeSymbol namedType)
						{
							return namedType;
						}
					}
				}
				else if (_metadataHelper.FindTypeByFullName(nsName + "." + type.Name) is INamedTypeSymbol namedType)
				{
					return namedType;
				}

				var ns = _fileDefinition
					.Namespaces
					// Ensure that prefixless declaration (generally xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation") is considered first, otherwise PreferredXamlNamespace matching can go awry
					.OrderByDescending(n => n.Prefix.IsNullOrEmpty())
					.FirstOrDefault(n => n.Namespace == type.PreferredXamlNamespace);
				if (ns?.Prefix is { Length: > 0 } nsPrefix)
				{
					return _findType!($"{nsPrefix}:{type.Name}");
				}
			}

			return null;
		}

		private INamedTypeSymbol? SearchNamespaces(string name, string[] namespaces)
		{
			foreach (var @namespace in namespaces)
			{
				if (_metadataHelper.FindTypeByFullName(@namespace + "." + name) is INamedTypeSymbol type)
				{
					return type;
				}
			}

			return null;
		}

		private INamedTypeSymbol? SearchClrNamespaces(string name)
		{
			if (_clrNamespaces != null)
			{
				return SearchNamespaces(name, _clrNamespaces);
			}

			return null;
		}

		private INamedTypeSymbol? ResolveType(string? name, XamlObjectDefinition? xmlnsContextProvider = null)
		{
			if (name == null)
			{
				return null;
			}

			while (xmlnsContextProvider is not null)
			{
				var namespaces = xmlnsContextProvider.Namespaces;
				if (namespaces is { Count: > 0 } && name.IndexOf(':') is int indexOfColon && indexOfColon > 0)
				{
					var ns = name.AsSpan().Slice(0, indexOfColon);
					foreach (var @namespace in namespaces)
					{
						if (ns.Equals(@namespace.Prefix.AsSpan(), StringComparison.Ordinal))
						{
							if (_metadataHelper.FindTypeByFullName($"{GetTrimmedNamespace(@namespace.Namespace)}.{name.Substring(indexOfColon + 1)}") is INamedTypeSymbol namedType)
							{
								return namedType;
							}

							break;
						}
					}
				}

				xmlnsContextProvider = xmlnsContextProvider.Owner;
			}

			return _findType!(name);
		}
		private INamedTypeSymbol GetType(string name, XamlObjectDefinition? objectDefinition = null) =>
			ResolveType(name, objectDefinition) ??
			throw new InvalidOperationException("The type {0} could not be found".InvariantCultureFormat(name));

		private INamedTypeSymbol GetType(XamlType type)
		{
			var clrType = FindType(type);

			if (clrType == null)
			{
				throw new InvalidOperationException("The type {0} could not be found".InvariantCultureFormat(type));
			}

			return clrType;
		}

		private INamedTypeSymbol? SourceFindType(string name)
		{
			if (name.StartsWith(GlobalPrefix, StringComparison.Ordinal))
			{
				return _metadataHelper.FindTypeByFullName(name.Substring(GlobalPrefix.Length)) as INamedTypeSymbol;
			}
			else if (name.Contains(":"))
			{
				// We are passed a `namespace:type_name`
				var fields = name.Split(':');

				var ns = _fileDefinition.Namespaces.FirstOrDefault(n => n.Prefix == fields[0]);
				if (ns is null)
				{
					// The given namespace is not found. We can't resolve a symbol.
					// We should be returning null here, but we fallback to fuzzy matching if enabled.
					return SearchWithFuzzyMatching(fields[1]);
				}

				var indexOfQuestionMark = ns.Namespace.IndexOf('?');
				var namespaceUrl = ns.Namespace;
				if (indexOfQuestionMark > -1)
				{
					namespaceUrl = ns.Namespace.Substring(0, indexOfQuestionMark);
				}

				if (_knownNamespaces.TryGetValue(namespaceUrl, out var knownNamespaces))
				{
					return SearchNamespaces(fields[1], knownNamespaces);
				}

				var nsName = GetTrimmedNamespace(namespaceUrl);
				if (namespaceUrl == nsName && _includeXamlNamespaces.Contains(ns.Prefix))
				{
					// For XAML included namespaces (e.g, android) where we don't have "using:" in the url, assume the default namespace.
					return SearchClrNamespaces(fields[1]) ?? SearchWithFuzzyMatching(fields[1]);
				}

				name = nsName + "." + fields[1];

				if (_metadataHelper.FindTypeByFullName(name) is INamedTypeSymbol namedTypeSymbol1)
				{
					return namedTypeSymbol1;
				}

				// Background on this code path taking the following xaml just as an example:
				// https://github.com/unoplatform/uno/blob/12c3b1c3cdd6bcd856005d181be4057cd3751212/src/Uno.UI.FluentTheme.v2/Resources/Version2/PriorityDefault/CommandBarFlyout.xaml#L5-L6
				// In the above XAML, we have 'local:CommandBarFlyoutCommandBar' which refers to 'using:Microsoft/**/.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar'
				// However, we have 'CommandBarFlyoutCommandBar' in Windows namespace for UWP tree, and in Microsoft namespace for WinUI tree.
				// So, if we couldn't get Microsoft.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar, we try with Microsoft.UI.Xaml.Controls.Primitives.CommandBarFlyoutCommandBar
				// Ideally we would like UWP and WinUI trees to individually have the correct namespace. Until that happens, we have to live with this workaround.
				if (nsName.StartsWith("Microsoft.", StringComparison.Ordinal) &&
					_metadataHelper.FindTypeByFullName("Windows." + nsName.Substring("Microsoft.".Length) + "." + fields[1]) is INamedTypeSymbol namedTypeSymbol2)
				{
					return namedTypeSymbol2;
				}
				else if (nsName.Equals("Uno.UI.Controls.Legacy") &&
					_metadataHelper.FindTypeByFullName(XamlConstants.Namespaces.Controls + "." + fields[1]) is INamedTypeSymbol namedTypeSymbol3)
				{
					// Workaround. There are usages of `legacy:ListView` and `legacy:GridView` in XAML where the referenced control is only in Android and iOS.
					// We fallback to the corresponding non-legacy for this case
					return namedTypeSymbol3;
				}

				return SearchWithFuzzyMatching(fields[1]);
			}

			// In this path, we are dealing with a simple name (not containing colon :)
			if (SearchClrNamespaces(name) is INamedTypeSymbol namedTypeSymbol4)
			{
				return namedTypeSymbol4;
			}

			return SearchWithFuzzyMatching(name);

			INamedTypeSymbol? SearchWithFuzzyMatching(string name)
			{
				if (!_enableFuzzyMatching)
				{
					return null;
				}

				var symbol = _metadataHelper.Compilation.GetSymbolsWithName(name, SymbolFilter.Type).OfType<INamedTypeSymbol>().FirstOrDefault();
				if (symbol is not null)
				{
					return symbol;
				}

				return SearchFromMetadata(name);
			}

			INamedTypeSymbol? SearchFromMetadata(string name)
			{
				var compilation = _metadataHelper.Compilation;
				foreach (var metadataReference in compilation.References)
				{
					if (compilation.GetAssemblyOrModuleSymbol(metadataReference) is IAssemblySymbol assembly &&
						assembly.TypeNames.Contains(name))
					{
						foreach (var candidate in assembly.GlobalNamespace.GetNamespaceTypes())
						{
							if (candidate.Name == name)
							{
								return candidate;
							}
						}
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Trim prefixes from namespace declaration
		/// </summary>
		private static string GetTrimmedNamespace(string nsNamespace)
		{
			var nsName = nsNamespace.TrimStart("using:");
			if (nsName.StartsWith("clr-namespace:", StringComparison.Ordinal))
			{
				nsName = nsName.Split(';')[0].TrimStart("clr-namespace:");
			}

			return nsName;
		}

		private IEnumerable<string> FindLocalizableProperties(INamedTypeSymbol? type)
		{
			while (type != null)
			{
				foreach (var prop in _metadataHelper.FindLocalizableDeclaredProperties(type))
				{
					yield return prop;
				}

				type = type.BaseType;
			}
		}

		private IEnumerable<(INamedTypeSymbol ownerType, string property)> FindLocalizableAttachedProperties(string uid)
		{
			foreach (var resource in _resourceDetailsCollection.FindByUId(uid))
			{
				// fullKey = $"{uidName}.[using:{ns}]{type}.{memberName}";
				//
				// Example:
				// OpenVideosButton.[using:Microsoft.UI.Xaml.Controls]ToolTipService.ToolTip

				var firstDotIndex = resource.Key.IndexOf('.');
				var propertyPath = resource.Key.Substring(firstDotIndex + 1);

				const string usingPattern = "[using:";

				if (propertyPath.StartsWith(usingPattern, StringComparison.Ordinal))
				{
					var lastDotIndex = propertyPath.LastIndexOf('.');

					var propertyName = propertyPath.Substring(lastDotIndex + 1);
					var typeName = propertyPath
						.Substring(usingPattern.Length, lastDotIndex - usingPattern.Length)
						.Replace("]", ".");

					if (_metadataHelper.FindTypeByFullName(typeName) is INamedTypeSymbol typeSymbol)
					{
						yield return (typeSymbol, propertyName);
					}
					else
					{
						throw new Exception($"Unable to find the type {typeName} in key {resource.Key}");
					}
				}
			}
		}
	}
}
