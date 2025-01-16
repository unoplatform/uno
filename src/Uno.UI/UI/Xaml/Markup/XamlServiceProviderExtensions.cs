namespace Microsoft.UI.Xaml;

internal static class XamlServiceProviderExtensions
{
	public static T GetService<T>(this IXamlServiceProvider provider) => (T)provider.GetService(typeof(T));
}
