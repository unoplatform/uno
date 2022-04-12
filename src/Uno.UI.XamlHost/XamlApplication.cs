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
	public void Dispose() => IsDisposed = true;

	public bool IsDisposed { get; private set; }
}
