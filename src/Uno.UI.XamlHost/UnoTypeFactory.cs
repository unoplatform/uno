// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Win32.UI.XamlHost/UWPTypeFactory.cs

using System;
using WUX = Windows.UI.Xaml;

namespace Uno.UI.XamlHost;

/// <summary>
/// Provides factory methods for type registered with <seealso cref="WUX.Application"/>
/// </summary>
static partial class UnoTypeFactory
{
	/// <summary>
	/// Creates UWP XAML type instance from WinRT type name
	/// UWP XAML type name should be specified as: namespace.class
	/// ex: MyClassLibrary.MyCustomType
	/// ex: WUX.Shapes.Rectangle
	/// ex: WUX.Controls.Button
	/// </summary>
	/// <param name="xamlTypeName">UWP XAML type name</param>
	/// <exception cref="InvalidOperationException">Condition.</exception>
	/// <returns>Instance of UWP XAML type described by xamlTypeName string</returns>
	public static WUX.FrameworkElement CreateXamlContentByType(string xamlTypeName)
	{
		WUX.Markup.IXamlType xamlType = null;
		Type systemType = null;

		// If a root metadata provider has been defined on the application object,
		// use it to probe for custom UWP XAML type metadata.  If the root metadata
		// provider has not been implemented on the current application object, assume
		// the caller wants a built-in UWP XAML type, not a custom UWP XAML type.
		var xamlRootMetadataProvider = WUX.Application.Current as WUX.Markup.IXamlMetadataProvider;
		if (xamlRootMetadataProvider != null)
		{
			//TODO: This is a temporary workaround  https://github.com/unoplatform/uno/issues/8978
			//xamlType = xamlRootMetadataProvider.GetXamlType(xamlTypeName);
		}

		systemType = FindBuiltInType(xamlTypeName);

		if (systemType != null)
		{
			// Create built-in UWP XAML type
			return (WUX.FrameworkElement)Activator.CreateInstance(systemType);
		}

		if (xamlType != null)
		{
			// Create custom UWP XAML type
			return (WUX.FrameworkElement)xamlType.ActivateInstance();
		}

		throw new InvalidOperationException("Microsoft.Windows.Interop.UWPTypeFactory: Could not create type: " + xamlTypeName);
	}

	/// <summary>
	/// Searches for a built-in type by iterating through all types in
	/// all assemblies loaded in the current AppDomain
	/// </summary>
	/// <param name="typeName">Full type name, with namespace, without assembly</param>
	/// <returns>If found, <see cref="Type" />; otherwise, null..</returns>
	private static Type FindBuiltInType(string typeName)
	{
		var currentAppDomain = AppDomain.CurrentDomain;
		var appDomainLoadedAssemblies = currentAppDomain.GetAssemblies();

		foreach (var loadedAssembly in appDomainLoadedAssemblies)
		{
			var currentType = loadedAssembly.GetType(typeName);
			if (currentType != null)
			{
				return currentType;
			}
		}

		return null;
	}
}
