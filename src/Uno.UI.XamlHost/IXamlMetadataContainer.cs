using System.Collections.Generic;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.XamlHost;

/// <summary>
/// Represents a container for XAML metadata providers.
/// Usually this would be a class derived from Application.
/// </summary>
public interface IXamlMetadataContainer
{
	/// <summary>
	/// Gets XAML metadata providers.
	/// </summary>
	IList<IXamlMetadataProvider> MetadataProviders { get; }
}
