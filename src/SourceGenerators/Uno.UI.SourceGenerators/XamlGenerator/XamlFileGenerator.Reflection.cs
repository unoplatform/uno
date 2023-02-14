#nullable enable

using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlFileGenerator
	{
		private Func<string, INamedTypeSymbol?>? _findType;
		private Func<XamlType, bool, INamedTypeSymbol?>? _findTypeByXamlType;
		private Func<string, string, INamedTypeSymbol?>? _findPropertyTypeByFullName;
		private Func<INamedTypeSymbol?, string, INamedTypeSymbol?>? _findPropertyTypeByOwnerSymbol;
		private Func<XamlMember, INamedTypeSymbol?>? _findPropertyTypeByXamlMember;
		private Func<XamlMember, IEventSymbol?>? _findEventType;
		private Func<INamedTypeSymbol, Dictionary<string, IEventSymbol>>? _getEventsForType;
		private Func<INamedTypeSymbol, string[]>? _findLocalizableDeclaredProperties;
		private XClassName? _xClassName;
		private string[]? _clrNamespaces;
		private readonly static Func<INamedTypeSymbol, IPropertySymbol?> _findContentProperty;
		private readonly static Func<INamedTypeSymbol, string, bool> _isAttachedProperty;
		private readonly static Func<INamedTypeSymbol, string, INamedTypeSymbol> _getAttachedPropertyType;
		private readonly static Func<INamedTypeSymbol, bool> _isTypeImplemented;

		record XClassName(string Namespace, string ClassName, INamedTypeSymbol? Symbol)
		{
			public override string ToString()
				=> Symbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included))
				?? Namespace + "." + ClassName;
		}

		private void InitCaches()
		{
			_findType = Funcs.Create<string, INamedTypeSymbol?>(SourceFindType).AsLockedMemoized();
			_findPropertyTypeByXamlMember = Funcs.Create<XamlMember, INamedTypeSymbol?>(SourceFindPropertyType).AsLockedMemoized();
			_findEventType = Funcs.Create<XamlMember, IEventSymbol?>(SourceFindEventType).AsLockedMemoized();
			_findPropertyTypeByFullName = Funcs.Create<string, string, INamedTypeSymbol?>(SourceFindPropertyTypeByFullName).AsLockedMemoized();
			_findPropertyTypeByOwnerSymbol = Funcs.Create<INamedTypeSymbol?, string, INamedTypeSymbol?>(SourceFindPropertyTypeByOwnerSymbol).AsLockedMemoized();
			_findTypeByXamlType = Funcs.Create<XamlType, bool, INamedTypeSymbol?>(SourceFindTypeByXamlType).AsLockedMemoized();
			_getEventsForType = Funcs.Create<INamedTypeSymbol, Dictionary<string, IEventSymbol>>(SourceGetEventsForType).AsLockedMemoized();
			_findLocalizableDeclaredProperties = Funcs.Create<INamedTypeSymbol, string[]>(SourceFindLocalizableDeclaredProperties).AsLockedMemoized();

			var defaultXmlNamespace = _fileDefinition
				.Namespaces
				.Where(n => n.Prefix.None())
				.FirstOrDefault()
				?.Namespace ?? "";

			_clrNamespaces = _knownNamespaces?.UnoGetValueOrDefault(defaultXmlNamespace, Array.Empty<string>());
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

		private string GetGlobalizedTypeName(XamlType type)
		{
			var fullTypeName = type.Name;
			var knownType = FindType(type);
			if (knownType == null && type.PreferredXamlNamespace.StartsWith("using:", StringComparison.Ordinal))
			{
				fullTypeName = type.PreferredXamlNamespace.TrimStart("using:") + "." + type.Name;
			}
			if (knownType != null)
			{
				// Override the using with the type that was found in the list of loaded assemblies
				fullTypeName = knownType.ToDisplayString();
			}

			return GetGlobalizedTypeName(fullTypeName);
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

				} while (type.SpecialType != SpecialType.System_Object);
			}

			return false;
		}

		private bool IsType(XamlType xamlType, ISymbol? typeSymbol)
		{
			var type = FindType(xamlType);

			return IsType(type, typeSymbol);
		}

		private bool IsType(INamedTypeSymbol? namedTypeSymbol, ISymbol? typeSymbol)
		{
			if (namedTypeSymbol != null)
			{
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

			if (type != null)
			{
				do
				{
					if (type.GetAllPropertiesWithName(propertyName).Any())
					{
						return true;
					}

					type = type.BaseType;

					if (type == null)
					{
						break;
					}

				} while (type.SpecialType != SpecialType.System_Object);
			}

			return false;
		}

		private bool IsRun(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Run);
		}

		private bool IsSpan(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Span);
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

				if (symbol.AllInterfaces.Any(i => isSameType(i, interfaceName)))
				{
					return true;
				}
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

		private bool IsFrameworkElement(XamlType xamlType)
		{
			return IsType(xamlType, _frameworkElementSymbol);
		}

		private bool IsAndroidView(XamlType xamlType)
		{
			return IsType(xamlType, _androidViewSymbol);
		}

		private bool IsIOSUIView(XamlType xamlType)
		{
			return IsType(xamlType, _iOSViewSymbol);
		}

		private bool IsMacOSNSView(XamlType xamlType)
		{
			return IsType(xamlType, _appKitViewSymbol);
		}

		private bool IsDependencyObject(XamlObjectDefinition component)
			=> GetType(component.Type).GetAllInterfaces().Any(i => SymbolEqualityComparer.Default.Equals(i, _dependencyObjectSymbol));

		private bool IsUIElement(XamlObjectDefinition component)
			=> IsType(component.Type, _uiElementSymbol);

		/// <summary>
		/// Is the type derived from the native view type on a Xamarin platform?
		/// </summary>
		private bool IsNativeView(XamlType xamlType) => IsAndroidView(xamlType) || IsIOSUIView(xamlType) || IsMacOSNSView(xamlType);

		/// <summary>
		/// Is the type one of the base view types in WinUI? (UIElement is most commonly used to mean 'any WinUI view type,' but
		/// FrameworkElement is valid too)
		/// </summary>
		private bool IsManagedViewBaseType(INamedTypeSymbol? targetType) => SymbolEqualityComparer.Default.Equals(targetType, _uiElementSymbol) || SymbolEqualityComparer.Default.Equals(targetType, _frameworkElementSymbol);

		private bool IsDependencyProperty(XamlMember member)
		{
			string name = member.Name;
			var propertyOwner = FindType(member.DeclaringType);

			return IsDependencyProperty(propertyOwner, name);
		}

		private static bool IsDependencyProperty(INamedTypeSymbol? propertyOwner, string name)
		{
			if (propertyOwner != null)
			{
				var propertyDependencyPropertyQuery = propertyOwner.GetAllPropertiesWithName(name + "Property");
				var fieldDependencyPropertyQuery = propertyOwner.GetAllFieldsWithName(name + "Property");

				return propertyDependencyPropertyQuery.Any() || fieldDependencyPropertyQuery.Any();
			}
			else
			{
				return false;
			}
		}

		private bool HasIsParsing(XamlType xamlType)
		{
			return IsImplementingInterface(FindType(xamlType), _dependencyObjectParseSymbol);
		}

		private bool HasIsParsing(INamedTypeSymbol? type)
		{
			return IsImplementingInterface(type, _dependencyObjectParseSymbol);
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

		private INamedTypeSymbol GetPropertyType(XamlMember xamlMember)
		{
			var definition = FindPropertyType(xamlMember);

			if (definition == null)
			{
				throw new Exception($"The property {xamlMember.Type?.Name}.{xamlMember.Name} is unknown");
			}

			return definition;
		}

		private INamedTypeSymbol GetPropertyTypeByOwnerSymbol(INamedTypeSymbol ownerType, string propertyName)
		{
			var definition = FindPropertyTypeByOwnerSymbol(ownerType, propertyName);

			if (definition == null)
			{
				throw new Exception("The property {0}.{1} is unknown".InvariantCultureFormat(ownerType, propertyName));
			}

			return definition;
		}

		private INamedTypeSymbol? FindPropertyType(XamlMember xamlMember) => _findPropertyTypeByXamlMember!(xamlMember);

		private INamedTypeSymbol? SourceFindPropertyType(XamlMember xamlMember)
		{
			// Search for the type the clr namespaces registered with the xml namespace
			if (xamlMember.DeclaringType != null)
			{
				var clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(xamlMember.DeclaringType.PreferredXamlNamespace, Array.Empty<string>());

				foreach (var clrNamespace in clrNamespaces)
				{
					string declaringTypeName = xamlMember.DeclaringType.Name;

					var propertyType = FindPropertyTypeByFullName(clrNamespace + "." + declaringTypeName, xamlMember.Name);

					if (propertyType != null)
					{
						return propertyType;
					}
				}
			}

			var type = FindType(xamlMember.DeclaringType);

			// If not, try to find the closest match using the name only.
			return FindPropertyTypeByOwnerSymbol(type, xamlMember.Name);
		}

		private INamedTypeSymbol? FindPropertyTypeByFullName(string ownerType, string propertyName) => _findPropertyTypeByFullName!(ownerType, propertyName);

		private INamedTypeSymbol? SourceFindPropertyTypeByFullName(string ownerType, string propertyName)
		{
			var type = _metadataHelper.FindTypeByFullName(ownerType) as INamedTypeSymbol;
			return FindPropertyTypeByOwnerSymbol(type, propertyName);
		}

		private INamedTypeSymbol? FindPropertyTypeByOwnerSymbol(INamedTypeSymbol? type, string propertyName) => _findPropertyTypeByOwnerSymbol!(type, propertyName);

		private INamedTypeSymbol? SourceFindPropertyTypeByOwnerSymbol(INamedTypeSymbol? type, string propertyName)
		{
			if (type != null && !string.IsNullOrEmpty(propertyName))
			{
				do
				{
					ThrowOnErrorSymbol(type);

					var resolvedType = type;

					var property = resolvedType.GetAllPropertiesWithName(propertyName).FirstOrDefault();
					var setMethod = resolvedType.GetFirstMethodWithName("Set" + propertyName);

					if (property != null)
					{
						if (property.Type.OriginalDefinition is { SpecialType: SpecialType.System_Nullable_T })
						{
							//TODO
							return (property.Type as INamedTypeSymbol)?.TypeArguments[0] as INamedTypeSymbol;
						}
						else
						{
							return property.Type as INamedTypeSymbol;
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

							if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
							{
								return null;
							}

							type = baseType;
						}
					}
				} while (true);
			}
			else
			{
				return null;
			}
		}

		private IEventSymbol? FindEventType(XamlMember xamlMember)
			=> _findEventType!(xamlMember);

		private IEventSymbol? SourceFindEventType(XamlMember xamlMember)
		{
			var ownerType = FindType(xamlMember.DeclaringType);

			if (ownerType != null)
			{
				ThrowOnErrorSymbol(ownerType);

				if (GetEventsForType(ownerType).TryGetValue(xamlMember.Name, out var eventSymbol))
				{
					return eventSymbol;
				}
			}

			return null;
		}

		private Dictionary<string, IEventSymbol> GetEventsForType(INamedTypeSymbol symbol)
			=> _getEventsForType!(symbol);

		private Dictionary<string, IEventSymbol> SourceGetEventsForType(INamedTypeSymbol symbol)
		{
			var output = new Dictionary<string, IEventSymbol>();

			foreach (var evt in symbol.GetAllEvents())
			{
				if (!output.ContainsKey(evt.Name))
				{
					output.Add(evt.Name, evt);
				}
			}

			return output;
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

		private static bool IsRelevantNamespace(string? xamlNamespace)
		{
			if (XamlConstants.XmlXmlNamespace.Equals(xamlNamespace, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return true;
		}

		private static bool IsRelevantProperty(XamlMember? member)
		{
			if (member?.Name == "Phase") // Phase is not relevant as it's not an actual property
			{
				return false;
			}

			return true;
		}

		private static bool SourceIsAttachedProperty(INamedTypeSymbol? type, string name)
		{
			do
			{
				var property = type.GetAllPropertiesWithName(name).FirstOrDefault();
				if (property?.GetMethod?.IsStatic ?? false)
				{
					return true;
				}

				var setMethod = type?.GetFirstMethodWithName("Set" + name);
				if (setMethod is { IsStatic: true, Parameters: { Length: 2 } })
				{
					return true;
				}

				type = type?.BaseType;
				if (type == null || type.SpecialType == SpecialType.System_Object)
				{
					return false;
				}

			} while (true);
		}

		/// <summary>
		/// Get the type of the attached property.
		/// </summary>
		private INamedTypeSymbol GetAttachedPropertyType(XamlMemberDefinition member)
		{
			var type = GetType(member.Member.DeclaringType);
			return _getAttachedPropertyType(type, member.Member.Name);
		}

		/// <summary>
		/// Get the type of the attached property.
		/// </summary>
		private INamedTypeSymbol GetAttachedPropertyType(INamedTypeSymbol type, string propertyName)
			=> _getAttachedPropertyType(type, propertyName);

		private static INamedTypeSymbol SourceGetAttachedPropertyType(INamedTypeSymbol? type, string name)
		{
			do
			{
				var setMethod = type?.GetFirstMethodWithName("Set" + name);

				if (setMethod != null && setMethod.IsStatic && setMethod.Parameters.Length == 2)
				{
					return (setMethod.Parameters[1].Type as INamedTypeSymbol)!;
				}

				type = type?.BaseType;

				if (type == null || type.SpecialType == SpecialType.System_Object)
				{
					throw new InvalidOperationException($"No valid setter found for attached property {name}");
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
		private bool IsNewableProperty(IPropertySymbol property, out string? newableTypeName)
		{
			var namedType = property.Type as INamedTypeSymbol;

			var isNewable = property.SetMethod.SelectOrDefault(m => m!.DeclaredAccessibility == Accessibility.Public, false) &&
				namedType.SelectOrDefault(nts => nts?.Constructors.Any(ms => ms.Parameters.Length == 0) ?? false, false);

			newableTypeName = isNewable && namedType != null ? GetFullGenericTypeName(namedType) : null;

			return isNewable;
		}

		/// <summary>
		/// Returns true if the type implements either ICollection, IList or one of their generics
		/// </summary>
		private bool IsCollectionOrListType(INamedTypeSymbol? propertyType)
			=> IsImplementingInterface(propertyType, _iCollectionSymbol)
			|| IsImplementingInterface(propertyType, _iCollectionOfTSymbol)
			|| IsImplementingInterface(propertyType, _iListSymbol)
			|| IsImplementingInterface(propertyType, _iListOfTSymbol);

		/// <summary>
		/// Returns true if the type implements <see cref="IDictionary{TKey, TValue}"/>
		/// </summary>
		private bool IsDictionary(INamedTypeSymbol? propertyType)
			=> IsImplementingInterface(propertyType, _iDictionaryOfTKeySymbol);

		/// <summary>
		/// Returns true if the type exactly implements either ICollection, IList or one of their generics
		/// </summary>
		private bool IsExactlyCollectionOrListType(INamedTypeSymbol type)
		{
			return SymbolEqualityComparer.Default.Equals(type, _iCollectionSymbol)
				|| type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_ICollection_T
				|| SymbolEqualityComparer.Default.Equals(type, _iListSymbol)
				|| type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IList_T;
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

			return IsImplementingInterface(type, _iCollectionSymbol)
				|| IsImplementingInterface(type, _iCollectionOfTSymbol);
		}

		/// <summary>
		/// Gets the 
		/// </summary>
		private IPropertySymbol? GetPropertyWithName(XamlType declaringType, string propertyName)
		{
			var type = FindType(declaringType);
			return type?.GetAllPropertiesWithName(propertyName).FirstOrDefault();
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

		private static void ThrowOnErrorSymbol(ISymbol symbol)
		{
			if (symbol is IErrorTypeSymbol errorTypeSymbol)
			{
				var candidates = string.Join(";", errorTypeSymbol.CandidateSymbols);
				var location = symbol.Locations.FirstOrDefault()?.ToString() ?? "Unknown";

				throw new InvalidOperationException(
					$"Unable to resolve {symbol} (Reason: {errorTypeSymbol.CandidateReason}, Location:{location}, Candidates: {candidates})"
				);
			}
		}

		private INamedTypeSymbol? FindType(string name)
			=> _findType!(name);

		private INamedTypeSymbol? FindType(XamlType? type, bool strictSearch = false)
			=> type != null ? _findTypeByXamlType!(type, strictSearch) : null;

		private INamedTypeSymbol? SourceFindTypeByXamlType(XamlType type, bool strictSearch)
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

				// Then use fuzzy lookup
				var ns = _fileDefinition
					.Namespaces
					// Ensure that prefixless declaration (generally xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation") is considered first, otherwise PreferredXamlNamespace matching can go awry
					.OrderByDescending(n => n.Prefix.IsNullOrEmpty())
					.FirstOrDefault(n => n.Namespace == type.PreferredXamlNamespace);

				if (
					type.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace
					&& type.Name == "Bind"
				   )
				{
					return _findType!(XamlConstants.Namespaces.Data + ".Binding");
				}

				var isKnownNamespace = ns?.Prefix is { Length: > 0 };

				if (strictSearch)
				{
					if (isKnownNamespace && ns != null)
					{
						var nsName = GetTrimmedNamespace(ns.Namespace);
						return _metadataHelper.FindTypeByFullName(nsName + "." + type.Name) as INamedTypeSymbol;
					}
				}
				else
				{
					var fullName = isKnownNamespace && ns != null ? ns.Prefix + ":" + type.Name : type.Name;

					return _findType!(fullName);
				}
			}

			return null;
		}

		private INamedTypeSymbol GetType(string name)
		{
			var type = _findType!(name);

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

		private INamedTypeSymbol? SourceFindType(string name)
		{
			var originalName = name;

			if (name.StartsWith(GlobalPrefix, StringComparison.Ordinal))
			{
				name = name.TrimStart(GlobalPrefix);
			}
			else if (name.Contains(":"))
			{
				var fields = name.Split(':');

				var ns = _fileDefinition.Namespaces.FirstOrDefault(n => n.Prefix == fields[0]);

				if (ns != null)
				{
					var nsName = GetTrimmedNamespace(ns.Namespace);

					name = nsName + "." + fields[1];
				}
			}
			else
			{
				if (_clrNamespaces != null)
				{
					// Search first using the default namespace
					foreach (var clrNamespace in _clrNamespaces)
					{
						if (_metadataHelper.FindTypeByFullName(clrNamespace + "." + name) is INamedTypeSymbol type)
						{
							return type;
						}
					}
				}
			}

			var resolvers = new Func<INamedTypeSymbol?>[] {

				// The sanitized name
				() => _metadataHelper.FindTypeByName(name) as INamedTypeSymbol,

				// As a full name
				() => _metadataHelper.FindTypeByFullName(name) as INamedTypeSymbol,

				// As a partial name using the original type
				() => _metadataHelper.FindTypeByName(originalName) as INamedTypeSymbol,

				// As a partial name using the non-qualified name
				() => _metadataHelper.FindTypeByName(originalName.Split(':').ElementAtOrDefault(1)) as INamedTypeSymbol,
			};

			return resolvers
				.Select(m => m())
				.Trim()
				.FirstOrDefault();
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

		private IEnumerable<string> FindLocalizableProperties(XamlType xamlType)
		{
			var type = GetType(xamlType);

			while (type != null)
			{
				foreach (var prop in FindLocalizableDeclaredProperties(type))
				{
					yield return prop;
				}

				type = type.BaseType;
			}
		}
		private bool IsAttachedProperty(INamedTypeSymbol declaringType, string name)
			=> _isAttachedProperty(declaringType, name);

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

					if (GetType(typeName) is { } typeSymbol)
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

		private string[] FindLocalizableDeclaredProperties(INamedTypeSymbol type) => _findLocalizableDeclaredProperties!(type);

		private string[] SourceFindLocalizableDeclaredProperties(INamedTypeSymbol type)
		{
			return type.GetProperties()
				.Where(p => !p.IsReadOnly &&
					p.DeclaredAccessibility == Accessibility.Public &&
					IsLocalizablePropertyType(p.Type as INamedTypeSymbol)
				)
				.Select(p => p.Name)
				.ToArray();
		}

		private bool IsTypeImplemented(INamedTypeSymbol type) => _isTypeImplemented(type);

		private static bool SourceIsTypeImplemented(INamedTypeSymbol type)
			=> type.GetAttributes().None(a => a.AttributeClass?.ToDisplayString() == XamlConstants.Types.NotImplementedAttribute);
	}
}
