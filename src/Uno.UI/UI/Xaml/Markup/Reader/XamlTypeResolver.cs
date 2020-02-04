
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Uno;
using Uno.Extensions;
using Uno.Xaml;

namespace Windows.UI.Xaml.Markup.Reader
{
    internal class XamlTypeResolver
    {
		private readonly static Assembly _frameworkElementAssembly = typeof(FrameworkElement).Assembly;

        private readonly Func<string, Type> _findType;
        private readonly Func<Type, string, bool> _isAttachedProperty;
        private readonly XamlFileDefinition FileDefinition;
        private readonly Func<string, string, Type> _findPropertyTypeByName;
        private readonly Func<XamlMember, Type> _findPropertyTypeByXamlMember;
        private readonly Func<Type, PropertyInfo> _findContentProperty;

		public static ImmutableDictionary<string, string[]> KnownNamespaces { get; } 
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

        public XamlTypeResolver(XamlFileDefinition definition)
        {
            FileDefinition = definition;
            _findType = SourceFindType;
            _findType = _findType.AsMemoized();
            _isAttachedProperty = Funcs.Create<Type, string, bool>(SourceIsAttachedProperty).AsLockedMemoized();
            _findPropertyTypeByXamlMember = Funcs.Create<XamlMember, Type>(SourceFindPropertyType).AsLockedMemoized();
            _findPropertyTypeByName = Funcs.Create<string, string, Type>(SourceFindPropertyType).AsLockedMemoized();
            _findContentProperty = Funcs.Create<Type, PropertyInfo>(SourceFindContentProperty).AsLockedMemoized();
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

        public PropertyInfo GetPropertyByName(XamlType declaringType, string propertyName) 
            => GetPropertyByName(FindType(declaringType), propertyName);

        public PropertyInfo FindContentProperty(Type elementType)
            => _findContentProperty(elementType);

        private static PropertyInfo GetPropertyByName(Type type, string propertyName) 
            => type?.GetProperties().FirstOrDefault(p => p.Name == propertyName);

        private PropertyInfo SourceFindContentProperty(Type elementType)
        {
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

		/// <summary>
		/// Returns true if the property has an accessible public setter and has a parameterless constructor
		/// </summary>
		public bool IsNewableProperty(PropertyInfo property, out Type newableType)
		{
			var namedType = property.PropertyType as Type;

			var isNewable = property.SetMethod.SelectOrDefault(m => m.IsPublic, false) &&
				namedType.SelectOrDefault(nts => nts.GetConstructors().Any(ms => ms.GetParameters().Length == 0), false);

			newableType = isNewable ? namedType : null;

			return isNewable;
		}

		public DependencyProperty FindDependencyProperty(XamlMemberDefinition member)
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

		public DependencyProperty FindDependencyProperty(Type propertyOwner, string propertyName)
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

		private static IEnumerable<PropertyInfo> GetAllProperties(Type type)
		{
			while(type != typeof(object))
			{
				foreach(var property in type.GetProperties())
				{
					yield return property;
				}

				type = type.BaseType;
			}
		}

		private static IEnumerable<FieldInfo> GetAllFields(Type type)
		{
			while (type != typeof(object))
			{
				foreach (var field in type.GetFields())
				{
					yield return field;
				}

				type = type.BaseType;
			}
		}

		private bool IsInitializableProperty(PropertyInfo property) 
            => !property.SetMethod.SelectOrDefault(m => m.IsPublic, false);

        public bool IsCollectionOrListType(Type type)
        {
            return IsImplementingInterface(type, typeof(global::System.Collections.ICollection)) ||
                IsImplementingInterface(type, typeof(ICollection<>)) ||
                IsImplementingInterface(type, typeof(global::System.Collections.IList)) ||
                IsImplementingInterface(type, typeof(global::System.Collections.Generic.IList<>));
        }

        private bool IsImplementingInterface(Type type, Type iface) => 
            type
                .Flatten(t => t.BaseType)
                .Any(t => t
                    .GetInterfaces()
                    .Any(i => 
                        i == iface 
                        || (i.IsGenericType && i.GetGenericTypeDefinition() == iface)
                    )
                );

        public bool IsType(XamlType xamlType, string typeName)
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

        public Type FindType(string name)
        {
			if (name.IsNullOrWhiteSpace())
			{
				return null;
			}

			return _findType(name);
        }

        public Type FindType(XamlType type)
        {
            if (type != null)
            {
                var ns = FileDefinition.Namespaces.FirstOrDefault(n => n.Namespace == type.PreferredXamlNamespace);
                var isKnownNamespace = ns?.Prefix?.HasValue() ?? false;

                if (type.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace)
                {
					if (type.Name == "Bind")
					{
						return _findType(XamlConstants.Namespaces.Data + ".Binding");
					}
					else if (_findType("System." + type.Name) is Type systemType)
					{
						return systemType;
					}
                }

                var fullName = isKnownNamespace ? ns.Prefix + ":" + type.Name : type.Name;

                return _findType(fullName);
            }
            else
            {
                return null;
            }
        }

        private Type SourceFindType(string name)
        {
            var originalName = name;

            if (name.Contains(":"))
            {
                var fields = name.Split(new[] { ':' });

                var ns = FileDefinition.Namespaces.FirstOrDefault(n => n.Prefix == fields[0]);

                if (ns != null)
                {
                    var nsName = ns.Namespace.TrimStart("using:");

                    if (nsName.StartsWith("clr-namespace:"))
                    {
                        nsName = nsName.Split(new[] { ';' })[0].TrimStart("clr-namespace:");
                    }

                    name = nsName + "." + fields[1];
                }
            }
            else
            {
                var defaultXmlNamespace = FileDefinition
                        .Namespaces
                        .Where(n => n.Prefix.None())
                        .FirstOrDefault()
                        .SelectOrDefault(n => n.Namespace);

                var clrNamespaces = KnownNamespaces.UnoGetValueOrDefault(defaultXmlNamespace, new string[0]);

                // Search first using the default namespace
                foreach (var clrNamespace in clrNamespaces)
                {
					// This lookup is performed in the current assembly as it is the
					// original behavior, and the Wasm AOT engine does not yet respect this
					// behavior (because of Wasm missing stack walking feature)
					var type = _frameworkElementAssembly.GetType(clrNamespace + "." + name);

                    if (type != null)
                    {
                        return type;
                    }
                }
            }

            var resolvers = new Func<Type>[] {

				// As a full name
				() => Type.GetType(name),

				// As a partial name using the original type
				() => Type.GetType(originalName),

				// As a partial name using the non-qualified name
				() => Type.GetType(originalName.Split(new[] { ':' }).ElementAtOrDefault(1) ?? ""),

				// Look for the type in all loaded assemblies
				() => AppDomain.CurrentDomain
					.GetAssemblies()
					.Select(a =>
						a.GetType(name)
					)
					.Trim()
					.FirstOrDefault(),
			};

            return resolvers
                .Select(m => m())
                .Trim()
                .FirstOrDefault();
        }
        private static bool SourceIsAttachedProperty(Type type, string name)
        {
            do
            {
                var property = type.GetProperties().FirstOrDefault(p => p.Name == name);
                var setMethod = type.GetMethods().FirstOrDefault(p => p.Name == "Set" + name);

                if (property?.GetMethod?.IsStatic ?? false)
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

        public Type FindPropertyType(XamlMember xamlMember) => _findPropertyTypeByXamlMember(xamlMember);

        private Type SourceFindPropertyType(XamlMember xamlMember)
        {
            // Search for the type the clr namespaces registered with the xml namespace
            if (xamlMember.DeclaringType != null)
            {
                var clrNamespaces = KnownNamespaces.UnoGetValueOrDefault(xamlMember.DeclaringType.PreferredXamlNamespace, new string[0]);

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
            return FindPropertyType(type.SelectOrDefault(t => t.FullName, "$$unknown"), xamlMember.Name);
        }

        public Type FindPropertyType(string ownerType, string propertyName) => _findPropertyTypeByName(ownerType, propertyName);

        private Type SourceFindPropertyType(string ownerType, string propertyName)
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

    }
}
