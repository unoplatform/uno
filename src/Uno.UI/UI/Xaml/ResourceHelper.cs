using Uno.UI;
using Windows.ApplicationModel.Resources;

namespace Windows.UI.Xaml;
public static class ResourceHelper
{
	static ResourceHelper()
	{
		ResourceLoader.GetStringInternal = key => ResourcesService.Get(key);
	}

	/// <summary>
	/// Provides a global resource service for localization in Android and iOS
	/// </summary>
	public static IResourcesService ResourcesService
	{
		get; set;
	}

	/// <summary>
	/// Use to get resource for XamlFileGenerator in Android and iOS
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public static string FindResourceString(string name)
	{
		return ResourcesService.Get(name);
	}
}
