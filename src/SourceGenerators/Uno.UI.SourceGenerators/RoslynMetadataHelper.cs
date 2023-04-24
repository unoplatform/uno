#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator;

#if NETFRAMEWORK
using GeneratorExecutionContext = Uno.SourceGeneration.GeneratorExecutionContext;
#endif

namespace Uno.Roslyn
{
	internal class RoslynMetadataHelper
	{
		private readonly Func<string, ITypeSymbol?> _findTypeByFullName;
		private readonly Func<INamedTypeSymbol, IPropertySymbol?> _findContentProperty;
		private readonly Func<INamedTypeSymbol, string, bool> _isAttachedProperty;
		private readonly Func<INamedTypeSymbol, string, INamedTypeSymbol> _getAttachedPropertyType;
		private readonly Func<INamedTypeSymbol, bool> _isTypeImplemented;

		public Compilation Compilation { get; }

		public string AssemblyName => Compilation.AssemblyName!;

		public RoslynMetadataHelper(GeneratorExecutionContext context)
		{
			Compilation = context.Compilation;

			_findTypeByFullName = Funcs.Create<string, ITypeSymbol?>(SourceFindTypeByFullName).AsLockedMemoized();
			_findContentProperty = Funcs.Create<INamedTypeSymbol, IPropertySymbol?>(SourceFindContentProperty).AsLockedMemoized();
			_isAttachedProperty = Funcs.Create<INamedTypeSymbol, string, bool>(SourceIsAttachedProperty).AsLockedMemoized();
			_getAttachedPropertyType = Funcs.Create<INamedTypeSymbol, string, INamedTypeSymbol>(SourceGetAttachedPropertyType).AsLockedMemoized();
			_isTypeImplemented = Funcs.Create<INamedTypeSymbol, bool>(SourceIsTypeImplemented).AsLockedMemoized();
		}

		public ITypeSymbol? FindTypeByFullName(string fullName)
		{
			return _findTypeByFullName(fullName);
		}

		private ITypeSymbol? SourceFindTypeByFullName(string fullName)
		{
			var symbol = Compilation.GetTypeByMetadataName(fullName);

			if (symbol?.Kind == SymbolKind.ErrorType)
			{
				symbol = null;
			}

			return symbol;
		}

		public IPropertySymbol? FindContentProperty(INamedTypeSymbol elementType)
		{
			return _findContentProperty(elementType);
		}

		private static IPropertySymbol? SourceFindContentProperty(INamedTypeSymbol elementType)
		{
			var data = elementType
				.GetAllAttributes()
				.FirstOrDefault(t => t.AttributeClass?.GetFullyQualifiedTypeExcludingGlobal() == XamlConstants.Types.ContentPropertyAttribute);
			if (data != null)
			{
				var nameProperty = data.NamedArguments.Where(f => f.Key == "Name").FirstOrDefault();
				if (nameProperty.Value.Value != null)
				{
					var name = nameProperty.Value.Value.ToString() ?? "";
					return elementType.GetPropertyWithName(name);
				}
			}
			return null;
		}

		public bool IsAttachedProperty(INamedTypeSymbol declaringType, string name)
			=> _isAttachedProperty(declaringType, name);

		private static bool SourceIsAttachedProperty(INamedTypeSymbol? type, string name)
		{
			do
			{
				var property = type.GetPropertyWithName(name);
				if (property?.GetMethod?.IsStatic ?? false)
				{
					return true;
				}

				var setMethod = type?.GetFirstMethodWithName("Set" + name);
				if (setMethod is { IsStatic: true, Parameters.Length: 2 })
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

		public INamedTypeSymbol GetAttachedPropertyType(INamedTypeSymbol type, string propertyName)
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

		public ITypeSymbol GetTypeByFullName(string fullName)
		{
			var symbol = FindTypeByFullName(fullName);

			if (symbol == null)
			{
				throw new InvalidOperationException($"Unable to find type {fullName}");
			}

			return symbol;
		}

		public bool IsTypeImplemented(INamedTypeSymbol type) => _isTypeImplemented(type);

		private static bool SourceIsTypeImplemented(INamedTypeSymbol type)
			=> type.GetAttributes().None(a => a.AttributeClass?.GetFullyQualifiedTypeExcludingGlobal() == XamlConstants.Types.NotImplementedAttribute);
	}
}
