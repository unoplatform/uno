#if __ANDROID__ || __IOS__ || __MACOS__
using Uno.UI.Services;
using Windows.ApplicationModel.Resources;

namespace Windows.UI.Xaml;

public static class ResourceHelper
{
	static ResourceHelper()
	{
		ResourceLoader.GetStringInternal = key => ResourcesService.Get(key);
	}

	/// <summary>
	/// Provides a global resource service for localization in Android, iOS, and macOS
	/// </summary>
	public static ResourcesService ResourcesService { get; set; }

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

#endif
