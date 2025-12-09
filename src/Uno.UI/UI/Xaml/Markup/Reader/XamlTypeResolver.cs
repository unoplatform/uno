#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Uno;
using Uno.Extensions;
using Uno.Xaml;
using Windows.UI;
using Windows.Foundation;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace Microsoft.UI.Xaml.Markup.Reader
{
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "normal flow of operation")]
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "normal flow of operation")]
	[UnconditionalSuppressMessage("Trimming", "IL2075znote", Justification = "normal flow of operation")]
	internal class XamlTypeResolver
	{
		private readonly static Assembly[] _lookupAssemblies = new[]{
			typeof(FrameworkElement).Assembly,
			typeof(Color).Assembly,
			typeof(Size).Assembly,
		};

		private readonly Func<string?, Type?> _findType;
		private readonly Func<Type, string, bool> _isAttachedProperty;
		private readonly XamlFileDefinition FileDefinition;
		private readonly Func<string, string, Type?> _findPropertyTypeByName;
		private readonly Func<XamlMember, Type?> _findPropertyTypeByXamlMember;
		private readonly Func<Type?, PropertyInfo?> _findContentProperty;
		private readonly Func<Type, Type?, MethodInfo?> _findCollectionAddItemMethod;

		private static ImmutableDictionary<string, string[]> KnownNamespaces { get; }
			= new Dictionary<string, string[]>
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
			}.ToImmutableDictionary();

		[UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Need to ask @jeromelaban why e.g. `_isAttachedProperty` is a Func<…> instead of a method invocation.  See also 26c5cc5992cae3c8c25adf51eb77ca4b0dd34e93.")]
		public XamlTypeResolver(XamlFileDefinition definition)
		{
			FileDefinition = definition;

			_findType = SourceFindType;
			_findType = _findType.AsMemoized();
			_isAttachedProperty = Funcs.Create<Type, string, bool>(SourceIsAttachedProperty).AsLockedMemoized();
			_findPropertyTypeByXamlMember = Funcs.Create<XamlMember, Type?>(SourceFindPropertyType).AsLockedMemoized();
			_findPropertyTypeByName = Funcs.Create<string, string, Type?>(SourceFindPropertyType).AsLockedMemoized();
			_findContentProperty = Funcs.Create<Type?, PropertyInfo?>(SourceFindContentProperty).AsLockedMemoized();
			_findCollectionAddItemMethod = Funcs.Create<Type, Type?, MethodInfo?>(SourceFindCollectionAddItemMethod).AsLockedMemoized();
		}

		public bool IsAttachedProperty(XamlMemberDefinition member)
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

		public bool IsType(XamlType xamlType, XamlType baseType)
		{
			if (xamlType == baseType)
			{
				return true;
			}

			if (baseType == null || xamlType == null)
			{
				return false;
			}

			var clrBaseType = _findType(baseType.Name);

			if (clrBaseType != null)
			{
				return IsType(xamlType, clrBaseType.FullName);
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Determines if the provided member is an C# initializable list (where the collection already exists, and no set property is present)
		/// </summary>
		public bool IsInitializedCollection(PropertyInfo property)
		{
			if (property != null && IsInitializableProperty(property))
			{
				return IsCollectionOrListType(property.PropertyType);
			}

			return false;
		}

		public PropertyInfo? GetPropertyByName(XamlType declaringType, string propertyName, BindingFlags? flags = null)
			=> GetPropertyByName(FindType(declaringType), propertyName, flags);

		public FieldInfo? GetFieldByName(XamlType declaringType, string propertyName, BindingFlags? flags = null)
			=> GetFieldByName(FindType(declaringType), propertyName, flags);

		public PropertyInfo? FindContentProperty(Type? elementType)
			=> _findContentProperty(elementType);

		public PropertyInfo? GetPropertyByName(Type? type, string propertyName, BindingFlags? flags = null)
			=> (flags != null
				? type?.GetProperties(flags.Value)
				: type?.GetProperties()
			)?.FirstOrDefault(p => p.Name == propertyName);

		public FieldInfo? GetFieldByName(Type? type, string propertyName, BindingFlags? flags = null)
			=> (flags != null
				? type?.GetFields(flags.Value)
				: type?.GetFields()
			)?.FirstOrDefault(p => p.Name == propertyName);

		public EventInfo? GetEventByName(XamlType declaringType, string eventName)
			=> GetEventByName(FindType(declaringType), eventName);

		private static EventInfo? GetEventByName(Type? type, string eventName)
				=> type?.GetEvents().FirstOrDefault(p => p.Name == eventName);

		private PropertyInfo? SourceFindContentProperty(Type? elementType)
		{
			if (elementType == null)
			{
				return null;
			}

			var data = elementType
				.GetCustomAttributes<ContentPropertyAttribute>()
				.FirstOrDefault();

			if (data != null)
			{
				return GetPropertyByName(elementType, data.Name);
			}
			else
			{
				if (elementType.BaseType != typeof(object))
				{
					return FindContentProperty(elementType.BaseType);
				}
				else
				{
					return null;
				}
			}
		}

		public bool IsNewableType(Type type) => type.GetConstructors().Any(x => x.GetParameters().Length == 0);

		public DependencyProperty? FindDependencyProperty(XamlMemberDefinition member)
		{
			var propertyOwner = FindType(member.Member.DeclaringType);

			if (propertyOwner != null)
			{
				var propertyName = member.Member.Name;

				return FindDependencyProperty(propertyOwner, propertyName);
			}
			else
			{
				return null;
			}
		}

		public DependencyProperty? FindDependencyProperty(Type? propertyOwner, string? propertyName)
		{
			var propertyDependencyPropertyQuery = GetAllProperties(propertyOwner)
								.Where(p => p.Name == propertyName + "Property")
								.FirstOrDefault();

			var fieldDependencyPropertyQuery = GetAllFields(propertyOwner)
				.Where(p => p.Name == propertyName + "Property")
				.FirstOrDefault();

			return (
				propertyDependencyPropertyQuery?.GetValue(null)
				?? fieldDependencyPropertyQuery?.GetValue(null)
			) as DependencyProperty;
		}

		private static IEnumerable<PropertyInfo> GetAllProperties(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type? type)
		{
			Type? currentType = type;

			while (currentType != null && currentType != typeof(object))
			{
				foreach (var property in currentType.GetProperties())
				{
					yield return property;
				}

				currentType = currentType.BaseType;
			}
		}

		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "GetField/BaseType may return null, normal flow of operation")]
		[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "GetField/BaseType may return null, normal flow of operation")]
		private static IEnumerable<FieldInfo> GetAllFields(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type? type)
		{
			Type? currentType = type;

			while (currentType != null && currentType != typeof(object))
			{
				foreach (var field in currentType.GetFields())
				{
					yield return field;
				}

				currentType = currentType.BaseType;
			}
		}

		private bool IsInitializableProperty(PropertyInfo property)
			=> !(property.SetMethod?.IsPublic ?? false);

		public bool IsCollectionOrListType(Type? type)
		{
			if (type == null)
			{
				return false;
			}

			return IsImplementingInterface(type, typeof(global::System.Collections.ICollection)) ||
				IsImplementingInterface(type, typeof(ICollection<>)) ||
				IsImplementingInterface(type, typeof(global::System.Collections.IList)) ||
				IsImplementingInterface(type, typeof(global::System.Collections.Generic.IList<>));
		}

		private bool IsImplementingInterface(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
			Type iface)
			=> type
				.Flatten(t => t.BaseType!)
				.Any(t => t
					.GetInterfaces()
					.Any(i =>
						i == iface
						|| (i.IsGenericType && i.GetGenericTypeDefinition() == iface)
					)
				);

		public bool IsType(XamlType xamlType, string? typeName)
		{
			var type = FindType(xamlType);

			if (type != null)
			{
				do
				{
					if (type.FullName == typeName)
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

		public Type? FindType(string? name)
		{
			if (name.IsNullOrWhiteSpace())
			{
				return null;
			}

			return _findType(name);
		}
		public Type? FindType(string xmlns, string name)
		{
			if (xmlns == XamlConstants.Xmlnses.X)
			{
				if (name == "Bind")
				{
					return _findType(XamlConstants.Namespaces.Data + ".Binding");
				}
				else if (_findType("System." + name) is Type systemType)
				{
					return systemType;
				}
			}

			var ns = FileDefinition.Namespaces.FirstOrDefault(n => n.Namespace == xmlns);
			var isKnownNamespace = ns?.Prefix is { Length: > 0 };
			var fullName = (!isKnownNamespace && xmlns.StartsWith("using:", StringComparison.Ordinal))
				? xmlns.TrimStart("using:") + "." + name
				: isKnownNamespace ? ns?.Prefix + ":" + name : name;

			return _findType(fullName);
		}

		public Type? FindType(XamlType? type)
		{
			if (type != null)
			{
				return FindType(type.PreferredXamlNamespace, type.Name);
			}
			else
			{
				return null;
			}
		}

		public Type? FindTypeByXClass(XamlObjectDefinition? definition)
		{
			if (definition is { } &&
				FindXClass(definition) is { } name &&
				!name.IsNullOrWhiteSpace())
			{
				return _findType(name);
			}

			return null;
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types may be removed or not present as part of the normal operations of that method")]
		[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "GetType may return null, normal flow of operation")]
		private Type? SourceFindType(string? name)
		{
			static string? GetFullyQualifiedName(NamespaceDeclaration? ns, string nonQualifiedName)
			{
				if (ns != null)
				{
					var nsName = ns.Namespace.TrimStart("using:");

					if (nsName.StartsWith("clr-namespace:", StringComparison.Ordinal))
					{
						nsName = nsName.Split(';')[0].TrimStart("clr-namespace:");
					}

					return nsName + "." + nonQualifiedName;
				}

				return null;
			}

			if (name == null)
			{
				return null;
			}

			var originalName = name;

			if (name.Contains(':'))
			{
				var fields = name.Split(':');

				var ns = FileDefinition.Namespaces.FirstOrDefault(n => n.Prefix == fields[0]);

				name = GetFullyQualifiedName(ns, fields[1]);
			}
			else
			{
				var defaultXmlNamespaceDeclaration = FileDefinition
						.Namespaces
						.Where(n => n.Prefix.None())
						.FirstOrDefault();

				var defaultXmlNamespace = defaultXmlNamespaceDeclaration?.Namespace ?? "";

				var clrNamespaces = KnownNamespaces.UnoGetValueOrDefault(defaultXmlNamespace, Array.Empty<string>());

				// Search first using the default namespace
				foreach (var clrNamespace in clrNamespaces)
				{
					// This lookup is performed in the current assembly as it is the
					// original behavior, and the Wasm AOT engine does not yet respect this
					// behavior (because of Wasm missing stack walking feature)
					foreach (var assembly in _lookupAssemblies)
					{
						var type = assembly.GetType(clrNamespace + "." + name);

						if (type != null)
						{
							return type;
						}
					}
				}

				// The default namespace for the XAML snippet may be non-standard (starting with using: or clr-namespace:)
				name = GetFullyQualifiedName(defaultXmlNamespaceDeclaration, name);
			}

			var resolvers = new Func<Type?>[] {

				// As a full name
				() => name != null ? Type.GetType(name) : null,

				// As a partial name using the original type
				() => Type.GetType(originalName),

				// As a partial name using the non-qualified name
				() => Type.GetType(originalName.Split(':').ElementAtOrDefault(1) ?? ""),
					
				// Look for the type in all loaded assemblies
				() => AppDomain.CurrentDomain
					.GetAssemblies()
					.Select(

						[UnconditionalSuppressMessage("Trimming","IL2026", Justification = "Types may be removed or not present as part of the normal operations of that method")]
						(a) =>
							(name != null ? a.GetType(name) : null) ??
							a.GetType(originalName)
					)
					.Trim()
					.FirstOrDefault(),
			};

			return resolvers
				.Select(m => m())
				.Trim()
				.FirstOrDefault();
		}
		private static bool SourceIsAttachedProperty(
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
			Type type,
			string name)
		{
			Type? currentType = type;

			do
			{
				if (currentType == null)
				{
					return false;
				}

				// Converted from LINQ to reduce allocations: var property = currentType.GetProperties().FirstOrDefault(p => p.Name == name);
				PropertyInfo? property = null;
				foreach (var p in currentType.GetProperties())
				{
					if (p.Name == name)
					{
						property = p;
						break;
					}
				}

				// Converted from LINQ to reduce allocations: var setMethod = currentType.GetMethods().FirstOrDefault(p => p.Name == "Set" + name);
				MethodInfo? setMethod = null;
				foreach (var p in currentType.GetMethods())
				{
					if (p.Name == "Set" + name)
					{
						setMethod = p;
						break;
					}
				}

				if (property?.GetMethod?.IsStatic ?? false)
				{
					return true;
				}

				if (setMethod != null && setMethod.IsStatic)
				{
					return true;
				}

				currentType = currentType.BaseType;

				if (type == null || type.Name == "Object")
				{
					return false;
				}

			} while (true);
		}

		public Type? FindPropertyType(XamlMember xamlMember) => _findPropertyTypeByXamlMember(xamlMember);

		private Type? SourceFindPropertyType(XamlMember xamlMember)
		{
			// Search for the type the clr namespaces registered with the xml namespace
			if (xamlMember.DeclaringType != null)
			{
				var clrNamespaces = KnownNamespaces.UnoGetValueOrDefault(xamlMember.DeclaringType.PreferredXamlNamespace, Array.Empty<string>());

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
			return FindPropertyType(type?.FullName ?? "$$unknown", xamlMember.Name);
		}

		public Type? FindPropertyType(string ownerType, string propertyName) => _findPropertyTypeByName(ownerType, propertyName);

		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Types manipulated here have been marked earlier")]
		private Type? SourceFindPropertyType(string ownerType, string propertyName)
		{
			var type = FindType(ownerType);

			if (type != null)
			{
				do
				{
					var resolvedType = type;

					var property = resolvedType.GetProperties().FirstOrDefault(p => p.Name == propertyName);
					var setMethod = resolvedType.GetMethods().FirstOrDefault(p => p.Name == "Set" + propertyName);

					if (property != null)
					{
						//if (property.PropertyType.OriginalDefinition != null
						//    && property.PropertyType.OriginalDefinition?.ToDisplayString() == "System.Nullable<T>")
						//{
						//    //TODO
						//    return (property.Type as INamedTypeSymbol).TypeArguments.First() as INamedTypeSymbol;
						//}
						// else
						{
							return property.PropertyType;
						}
					}
					else
					{
						if (setMethod != null)
						{
							return setMethod.GetParameters().ElementAt(1).ParameterType;
						}
						else
						{
							var baseType = type.BaseType;

							if (baseType == null)
							{
								baseType = type.BaseType;
							}

							type = baseType;

							if (type == null || type == typeof(object))
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

		public MethodInfo? FindCollectionAddItemMethod(Type collectionType, Type? itemType) => _findCollectionAddItemMethod(collectionType, itemType);

		private MethodInfo? SourceFindCollectionAddItemMethod(Type collectionType, Type? itemType)
		{
			return collectionType
				.GetMethods(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public)
				.Where(m => m.Name == "Add")
				.FirstOrDefault(m =>
					m.GetParameters() is [var arg0] &&
					(itemType ?? typeof(object)).Is(arg0.ParameterType)
				);
		}

		private static string? FindXClass(XamlObjectDefinition definition) =>
			definition.Members
				.FirstOrDefault(x => x.Member.PreferredXamlNamespace == XamlConstants.Xmlnses.X && x.Member.Name == "Class")
				?.Value?.ToString();
	}
}
