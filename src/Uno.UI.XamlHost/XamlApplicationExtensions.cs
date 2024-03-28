// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Win32.UI.XamlHost/XamlApplicationExtensions.cs

using System;
using System.Linq;
using Uno.UI.Xaml.Core;
using WUX = Windows.UI.Xaml;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using Windows.ApplicationModel.Core;

namespace Uno.UI.XamlHost;

/// <summary>
/// XamlApplication is a custom <see cref="WUX.Application" /> that implements <see cref="WUX.Markup.IXamlMetadataProvider" />. The
/// metadata provider implemented on the application is known as the 'root metadata provider'.  This provider
/// has the responsibility of loading all other metadata for custom UWP XAML types.  In this implementation,
/// reflection is used at runtime to probe for metadata providers in the working directory, allowing any
/// type that includes metadata (compiled in to a .NET framework assembly) to be used without explicit
/// metadata handling by the developer.
/// </summary>
public static partial class XamlApplicationExtensions
{
	private static IXamlMetadataContainer _metadataContainer;
	private static bool _initialized;

	private static IXamlMetadataContainer GetCurrentProvider()
	{
		try
		{
			return WUX.Application.Current as IXamlMetadataContainer;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Gets and returns the current UWP XAML Application instance in a reference parameter.
	/// If the current XAML Application instance has not been created for the process (is null),
	/// a new <see cref="Uno.UI.XamlHost.XamlApplication" /> instance is created and returned.
	/// </summary>
	/// <returns>The instance of <seealso cref="XamlApplication"/></returns>
	public static IXamlMetadataContainer GetOrCreateXamlMetadataContainer()
	{
		WinUICoreServices.Instance.InitializationType = InitializationType.IslandsOnly;
		CoreApplication.IsFullFledgedApp = false;

		// Instantiation of the application object must occur before creating the DesktopWindowXamlSource instance.
		// DesktopWindowXamlSource will create a generic Application object unable to load custom UWP XAML metadata.
		if (_metadataContainer == null && !_initialized)
		{
			_initialized = true;

			// Create a custom UWP XAML Application object that implements reflection-based XAML metadata probing.
			try
			{
				_metadataContainer = GetCurrentProvider();
				if (_metadataContainer == null)
				{
					var providers = MetadataProviderDiscovery.DiscoverMetadataProviders().ToList();
					_metadataContainer = GetCurrentProvider();
					if (_metadataContainer == null)
					{
						_metadataContainer = new XamlApplication(providers);
						return _metadataContainer;
					}
				}
				else
				{
					return _metadataContainer;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
				_metadataContainer = GetCurrentProvider();
			}
		}

		var xamlApplication = _metadataContainer as XamlApplication;
		if (xamlApplication != null && xamlApplication.IsDisposed)
		{
			throw new ObjectDisposedException(typeof(XamlApplication).FullName);
		}

		return _metadataContainer;
	}
}
