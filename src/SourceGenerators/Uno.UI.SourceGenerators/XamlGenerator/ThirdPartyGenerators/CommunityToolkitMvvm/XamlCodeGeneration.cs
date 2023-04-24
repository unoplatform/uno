#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal partial class XamlCodeGeneration
{
	internal Lazy<INamedTypeSymbol?> IRelayCommandSymbol { get; set; }
	internal Lazy<INamedTypeSymbol?> IRelayCommandTSymbol { get; set; }
	internal Lazy<INamedTypeSymbol?> IAsyncRelayCommandSymbol { get; set; }
	internal Lazy<INamedTypeSymbol?> IAsyncRelayCommandTSymbol { get; set; }

	[MemberNotNull(nameof(IRelayCommandSymbol), nameof(IRelayCommandTSymbol), nameof(IAsyncRelayCommandSymbol), nameof(IAsyncRelayCommandTSymbol))]
	private void InitializeMvvmLazySymbols()
	{
		IRelayCommandSymbol = GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IRelayCommand");
		IRelayCommandTSymbol = GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IRelayCommand`1");
		IAsyncRelayCommandSymbol = GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IAsyncRelayCommand");
		IAsyncRelayCommandTSymbol = GetOptionalSymbolAsLazy("CommunityToolkit.Mvvm.Input.IAsyncRelayCommand`1");
	}
}
