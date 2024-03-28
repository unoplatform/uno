#if UNO_ISLANDS
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Uno.UI.XamlHost;

namespace SamplesApp;

partial class App
{
	public App(List<IXamlMetadataProvider> providers)
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
#endif
