// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Win32.UI.XamlApplication/XamlApplication.cpp

using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.XamlHost;

public class XamlApplication : Application, IXamlMetadataProvider, IXamlMetadataContainer, IDisposable
{
	public XamlApplication()
	{
	}

	public XamlApplication(List<IXamlMetadataProvider> providers)
	{
		MetadataProviders = providers;
	}

	public IList<IXamlMetadataProvider> MetadataProviders { get; }

	public IXamlType GetXamlType(Type type) => throw new NotImplementedException();

	public IXamlType GetXamlType(string fullName) => throw new NotImplementedException();

	public XmlnsDefinition[] GetXmlnsDefinitions() => throw new NotImplementedException();

	public
#if __IOS__ || __MACOS__
		new
#endif
		void Dispose() => IsDisposed = true;


	public bool IsDisposed { get; private set; }
}
