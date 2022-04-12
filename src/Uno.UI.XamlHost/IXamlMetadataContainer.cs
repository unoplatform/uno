using System.Collections.Generic;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.XamlHost;

public interface IXamlMetadataContainer
{
	IList<IXamlMetadataProvider> MetadataProviders { get; }
}
