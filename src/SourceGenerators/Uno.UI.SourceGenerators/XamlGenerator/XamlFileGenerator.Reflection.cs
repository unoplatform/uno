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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using System.Threading;
using Uno;
using Uno.Logging;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;
using System.Diagnostics;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlFileGenerator
	{
		private Func<string, INamedTypeSymbol> _findType;
		private Func<XamlType, INamedTypeSymbol> _findTypeByXamlType;
		private Func<string, string, INamedTypeSymbol> _findPropertyTypeByName;
		private Func<XamlMember, INamedTypeSymbol> _findPropertyTypeByXamlMember;
		private Func<XamlMember, IEventSymbol> _findEventType;
		private Func<INamedTypeSymbol, Dictionary<string, IEventSymbol>> _getEventsForType;
		private Func<INamedTypeSymbol, string[]> _findLocalizableDeclaredProperties;
		private (string ns, string className) _className;
		private bool _hasLiteralEventsRegistration = false;
		private string[] _clrNamespaces;
		private readonly static Func<INamedTypeSymbol, IPropertySymbol> _findContentProperty;
		private readonly static Func<INamedTypeSymbol, string, bool> _isAttachedProperty;
		private readonly static Func<INamedTypeSymbol, string, INamedTypeSymbol> _getAttachedPropertyType;

		private void InitCaches()
		{
			_findType = Funcs.Create<string, INamedTypeSymbol>(SourceFindType).AsLockedMemoized();
			_findPropertyTypeByXamlMember = Funcs.Create<XamlMember, INamedTypeSymbol>(SourceFindPropertyType).AsLockedMemoized();
			_findEventType = Funcs.Create<XamlMember, IEventSymbol>(SourceFindEventType).AsLockedMemoized();
			_findPropertyTypeByName = Funcs.Create<string, string, INamedTypeSymbol>(SourceFindPropertyType).AsLockedMemoized();
			_findTypeByXamlType = Funcs.Create<XamlType, INamedTypeSymbol>(SourceFindTypeByXamlType).AsLockedMemoized();
			_getEventsForType = Funcs.Create<INamedTypeSymbol, Dictionary<string, IEventSymbol>>(SourceGetEventsForType).AsLockedMemoized();
			_findLocalizableDeclaredProperties = Funcs.Create<INamedTypeSymbol, string[]>(SourceFindLocalizableDeclaredProperties).AsLockedMemoized();

			var defaultXmlNamespace = _fileDefinition
				.Namespaces
				.Where(n => n.Prefix.None())
				.FirstOrDefault()
				.SelectOrDefault(n => n.Namespace);

			_clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(defaultXmlNamespace, new string[0]);
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

				} while (!Equals(type, _objectSymbol));
			}

			return false;
		}

		private bool IsType(XamlType xamlType, ISymbol typeSymbol)
		{
			var type = FindType(xamlType);

			if (type != null)
			{
				do
				{
					if (Equals(type, typeSymbol))
					{
						return true;
					}

					type = type.BaseType;

					if (type == null)
					{
						break;
					}

				} while (!Equals(type, _objectSymbol));
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

				} while (type.Name != "Object");
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

		private bool IsImplementingInterface(INamedTypeSymbol symbol, INamedTypeSymbol interfaceName)
		{
			bool isSameType(INamedTypeSymbol source, INamedTypeSymbol iface) =>
				Equals(source, iface) || Equals(source.OriginalDefinition, iface);

			if (symbol != null)
			{
				if (isSameType(symbol, interfaceName))
				{
					return true;
				}

				do
				{
					if (symbol.Interfaces.Any(i => isSameType(i, interfaceName)))
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
			return IsType(xamlType, XamlConstants.Types.FrameworkElement);
		}

		private bool IsAndroidView(XamlType xamlType)
		{
			return IsType(xamlType, "Android.Views.View");
		}

		private bool IsIOSUIView(XamlType xamlType)
		{
			return IsType(xamlType, "UIKit.UIView");
		}

		private bool IsMacOSNSView(XamlType xamlType)
		{
			return IsType(xamlType, "AppKit.NSView");
		}

		/// <summary>
		/// Is the type derived from the native view type on a Xamarin platform?
		/// </summary>
		private bool IsNativeView(XamlType xamlType) => IsAndroidView(xamlType) || IsIOSUIView(xamlType) || IsMacOSNSView(xamlType);

		/// <summary>
		/// Is the type one of the base view types in WinUI? (UIElement is most commonly used to mean 'any WinUI view type,' but
		/// FrameworkElement is valid too)
		/// </summary>
		private bool IsManagedViewBaseType(INamedTypeSymbol targetType) => Equals(targetType, _uiElementSymbol) || Equals(targetType, _frameworkElementSymbol);

		private bool IsTransform(XamlType xamlType)
		{
			return IsType(xamlType, XamlConstants.Types.Transform);
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
					ThrowOnErrorSymbol(type);

					var resolvedType = type;

					var property = resolvedType.GetAllPropertiesWithName(propertyName).FirstOrDefault();
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
			=> _findEventType(xamlMember);

		private IEventSymbol SourceFindEventType(XamlMember xamlMember)
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
			=> _getEventsForType(symbol);

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

		private bool IsRelevantNamespace(string xamlNamespace)
		{
			if (XamlConstants.XmlXmlNamespace.Equals(xamlNamespace, StringComparison.OrdinalIgnoreCase))
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
				var property = type.GetAllPropertiesWithName(name).FirstOrDefault();
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
		/// Get the type of the attached property.
		/// </summary>
		private INamedTypeSymbol GetAttachedPropertyType(XamlMemberDefinition member)
		{
			var type = GetType(member.Member.DeclaringType);
			return _getAttachedPropertyType(type, member.Member.Name);
		}

		private static INamedTypeSymbol SourceGetAttachedPropertyType(INamedTypeSymbol type, string name)
		{
			do
			{
				var setMethod = type.GetMethods().FirstOrDefault(p => p.Name == "Set" + name);

				if (setMethod != null && setMethod.IsStatic && setMethod.Parameters.Length == 2)
				{
					return setMethod.Parameters[1].Type as INamedTypeSymbol;
				}

				type = type.BaseType;

				if (type == null || type.Name == "Object")
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
			=> IsImplementingInterface(propertyType, _iCollectionSymbol)
			|| IsImplementingInterface(propertyType, _iCollectionOfTSymbol)
			|| IsImplementingInterface(propertyType, _iListSymbol)
			|| IsImplementingInterface(propertyType, _iListOfTSymbol);

		/// <summary>
		/// Returns true if the type implements <see cref="IDictionary{TKey, TValue}"/>
		/// </summary>
		private bool IsDictionary(INamedTypeSymbol propertyType)
			=> IsImplementingInterface(propertyType, _iDictionaryOfTKeySymbol);

		/// <summary>
		/// Returns true if the type exactly implements either ICollection, IList or one of their generics
		/// </summary>
		private bool IsExactlyCollectionOrListType(INamedTypeSymbol type)
		{
			return Equals(type, _iCollectionSymbol)
				|| Equals(type.OriginalDefinition, _iCollectionOfTSymbol)
				|| Equals(type, _iListSymbol)
				|| Equals(type.OriginalDefinition, _iListOfTSymbol);
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
		private IPropertySymbol GetPropertyWithName(XamlType declaringType, string propertyName)
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

		private bool IsString(XamlObjectDefinition xamlObjectDefinition)
		{
			return xamlObjectDefinition.Type.Name == "String";
		}

		private bool IsPrimitive(XamlObjectDefinition xamlObjectDefinition)
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

		private bool HasInitializer(XamlObjectDefinition objectDefinition)
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

		private INamedTypeSymbol FindType(string name)
			=> _findType(name);

		private INamedTypeSymbol FindType(XamlType type)
			=> type != null ? _findTypeByXamlType(type) : null;

		private INamedTypeSymbol SourceFindTypeByXamlType(XamlType type)
		{
			if (type != null)
			{
				// Search first using the explicit XML namespace in known namespaces

				// Remove the namespace conditionals declaration
				var trimmedNamespace = type.PreferredXamlNamespace.Split('?').First();
				var clrNamespaces = _knownNamespaces.UnoGetValueOrDefault(trimmedNamespace, new string[0]);

				foreach (var clrNamespace in clrNamespaces)
				{
					if(_findType(clrNamespace + "." + type.Name) is { } result)
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
					return _findType(XamlConstants.Namespaces.Data + ".Binding");
				}

				var isKnownNamespace = ns?.Prefix?.HasValue() ?? false;
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

			if (name.StartsWith(GlobalPrefix))
			{
				name = name.TrimStart(GlobalPrefix);
			}
			else if (name.Contains(":"))
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
				// Search first using the default namespace
				foreach (var clrNamespace in _clrNamespaces)
				{
					var type = _metadataHelper.FindTypeByFullName(clrNamespace + "." + name) as INamedTypeSymbol;

					if (type != null)
					{
						return type;
					}
				}
			}

			var resolvers = new Func<INamedTypeSymbol>[] {

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

		private string[] FindLocalizableDeclaredProperties(INamedTypeSymbol type) => _findLocalizableDeclaredProperties(type);

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
	}
}
