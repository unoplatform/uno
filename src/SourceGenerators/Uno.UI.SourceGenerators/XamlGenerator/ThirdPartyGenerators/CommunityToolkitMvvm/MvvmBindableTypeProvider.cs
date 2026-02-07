#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators;

namespace Uno.UI.SourceGenerators.XamlGenerator.ThirdPartyGenerators.CommunityToolkitMvvm;

/// <summary>
/// Provides bindable type information for CommunityToolkit MVVM types.
/// </summary>
internal sealed class MvvmBindableTypeProvider : IBindableTypeProvider
{
	private readonly INamedTypeSymbol? _observableObjectSymbol;
	private readonly INamedTypeSymbol? _observableObjectAttributeSymbol;
	private readonly INamedTypeSymbol? _inotifyPropertyChangedSymbol;

	public MvvmBindableTypeProvider(Compilation compilation)
	{
		_observableObjectSymbol = compilation.GetTypeByMetadataName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject");
		_observableObjectAttributeSymbol = compilation.GetTypeByMetadataName("CommunityToolkit.Mvvm.ComponentModel.ObservableObjectAttribute");
		_inotifyPropertyChangedSymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
	}

	public bool IsBindableType(INamedTypeSymbol type)
	{
		// Get attributes once to avoid multiple enumerations
		var attributes = type.GetAllAttributes();

		// Check if type has [ObservableObject] attribute (CommunityToolkit MVVM)
		if (_observableObjectAttributeSymbol != null
			&& attributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _observableObjectAttributeSymbol)))
		{
			return true;
		}

		// Check if type inherits from ObservableObject (CommunityToolkit MVVM)
		if (_observableObjectSymbol != null)
		{
			var baseType = type.BaseType;
			while (baseType != null)
			{
				if (SymbolEqualityComparer.Default.Equals(baseType, _observableObjectSymbol))
				{
					return true;
				}
				baseType = baseType.BaseType;
			}
		}

		// Check if type implements INotifyPropertyChanged (fallback for other MVVM implementations)
		if (_inotifyPropertyChangedSymbol != null
			&& type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, _inotifyPropertyChangedSymbol)))
		{
			return true;
		}

		return false;
	}
}
