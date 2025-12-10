#nullable enable

using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators.CommunityToolkitMvvm;

internal sealed class MvvmTypeProvider : ITypeProvider, IBindableTypeProvider
{
	private readonly XamlCodeGeneration _generation;

	public MvvmTypeProvider(XamlCodeGeneration generation)
	{
		_generation = generation;
		IRelayCommandSymbol = generation.GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IRelayCommand");
		IRelayCommandTSymbol = generation.GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IRelayCommand`1");
		IAsyncRelayCommandSymbol = generation.GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IAsyncRelayCommand");
		IAsyncRelayCommandTSymbol = generation.GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IAsyncRelayCommand`1");
		ObservableObjectSymbol = generation.GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.ComponentModel.ObservableObject");
		ObservableObjectAttributeSymbol = generation.GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.ComponentModel.ObservableObjectAttribute");
		INotifyPropertyChangedSymbol = generation.GetOptionalSymbolAsLazy("System.ComponentModel.INotifyPropertyChanged");
	}

	internal Lazy<INamedTypeSymbol?> IRelayCommandSymbol { get; }
	internal Lazy<INamedTypeSymbol?> IRelayCommandTSymbol { get; }
	internal Lazy<INamedTypeSymbol?> IAsyncRelayCommandSymbol { get; }
	internal Lazy<INamedTypeSymbol?> IAsyncRelayCommandTSymbol { get; }
	internal Lazy<INamedTypeSymbol?> ObservableObjectSymbol { get; }
	internal Lazy<INamedTypeSymbol?> ObservableObjectAttributeSymbol { get; }
	internal Lazy<INamedTypeSymbol?> INotifyPropertyChangedSymbol { get; }

	public ITypeSymbol? TryGetType(ITypeSymbol symbol, string memberName)
	{
		// Case 1: ObservableProperty attribute.
		// Relevant code: https://github.com/CommunityToolkit/dotnet/blob/b341ef91fe66101444d6b811bc5dff32de029d3c/src/CommunityToolkit.Mvvm.SourceGenerators/ComponentModel/ObservablePropertyGenerator.Execute.cs#L1231-L1244
		// The user defines a field named '_abc', 'abc', 'm_abc', '_Abc', or 'm_Abc'.
		// The generated property will be named 'Abc', which is what the user will be referencing in x:Bind.

		// Case 2: RelayCommand attribute
		// Relevant code: https://github.com/CommunityToolkit/dotnet/blob/b341ef91fe66101444d6b811bc5dff32de029d3c/src/CommunityToolkit.Mvvm.SourceGenerators/Input/RelayCommandGenerator.Execute.cs#L455-L496
		// and also: https://github.com/CommunityToolkit/dotnet/blob/b341ef91fe66101444d6b811bc5dff32de029d3c/src/CommunityToolkit.Mvvm.SourceGenerators/Input/RelayCommandGenerator.Execute.cs#L522-L614
		// The user defines a method named 'OnAbcAsync', 'AbcAsync', 'OnAbc', 'Abc'.
		// The generated command property will be named 'AbcCommand'
		return TryGetMvvmObservablePropertyType(symbol, memberName) ?? TryGetMvvmRelayCommandType(symbol, memberName);
	}

	private static ITypeSymbol? TryGetMvvmObservablePropertyType(ITypeSymbol currentType, string part)
	{
		if (part.Length > 0 && char.IsUpper(part[0]))
		{
			// We could first check if the field has [ObservableAttribute], but it doesn't add enough value.
			var firstCharLower = char.ToLower(part[0], CultureInfo.InvariantCulture);
			var firstCharStripped = part.Substring(1);
			var mvvmType = currentType.TryGetPropertyOrFieldType($"_{firstCharLower}{firstCharStripped}");
			mvvmType ??= currentType.TryGetPropertyOrFieldType($"{firstCharLower}{firstCharStripped}");
			mvvmType ??= currentType.TryGetPropertyOrFieldType($"m_{firstCharLower}{firstCharStripped}");
			mvvmType ??= currentType.TryGetPropertyOrFieldType($"_{part}");
			mvvmType ??= currentType.TryGetPropertyOrFieldType($"m_{part}");
			return mvvmType;
		}

		return null;
	}

	private ITypeSymbol? TryGetMvvmRelayCommandType(ITypeSymbol currentType, string part)
	{
		const string CommandSuffix = "Command";
		if (part.EndsWith(CommandSuffix, StringComparison.Ordinal))
		{
			// We could first check if the method has [RelayCommand], but it doesn't add enough value.
			part = part.Substring(0, part.Length - CommandSuffix.Length);
			var method = currentType.GetAllMembersWithName(part).FirstOrDefault() as IMethodSymbol;
			method ??= currentType.GetAllMembersWithName($"On{part}").FirstOrDefault() as IMethodSymbol;
			method ??= currentType.GetAllMembersWithName($"On{part}Async").FirstOrDefault() as IMethodSymbol;
			method ??= currentType.GetAllMembersWithName($"{part}Async").FirstOrDefault() as IMethodSymbol;

			if (method is not null)
			{
				if (method.ReturnsVoid && method.Parameters.Length == 0)
				{
					return IRelayCommandSymbol.Value;
				}
				else if (method.ReturnsVoid && method.Parameters.Length == 1)
				{
					return IRelayCommandTSymbol.Value?.Construct(method.Parameters[0].Type);
				}
				else if (method.ReturnType is INamedTypeSymbol namedType && namedType.Is(_generation.TaskSymbol.Value))
				{
					if (method.Parameters.Length == 0)
					{
						return IAsyncRelayCommandSymbol.Value;
					}
					else if (method.Parameters.Length is 1 or 2)
					{
						return IAsyncRelayCommandTSymbol.Value?.Construct(method.Parameters[0].Type);
					}
				}
			}
		}

		return null;
	}

	public bool IsBindableType(INamedTypeSymbol type)
	{
		// Get attributes once to avoid multiple enumerations
		var attributes = type.GetAllAttributes();

		// Check if type has [ObservableObject] attribute (CommunityToolkit MVVM)
		if (ObservableObjectAttributeSymbol.Value != null
			&& attributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, ObservableObjectAttributeSymbol.Value)))
		{
			return true;
		}

		// Check if type inherits from ObservableObject (CommunityToolkit MVVM)
		if (ObservableObjectSymbol.Value != null)
		{
			var baseType = type.BaseType;
			while (baseType != null)
			{
				if (SymbolEqualityComparer.Default.Equals(baseType, ObservableObjectSymbol.Value))
				{
					return true;
				}
				baseType = baseType.BaseType;
			}
		}

		// Check if type implements INotifyPropertyChanged (fallback for other MVVM implementations)
		if (INotifyPropertyChangedSymbol.Value != null
			&& type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, INotifyPropertyChangedSymbol.Value)))
		{
			return true;
		}

		return false;
	}
}
