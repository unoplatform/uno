using System.ComponentModel;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Helpers.Xaml;

public static class BindingExtensions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Binding ApplyTargetNullValueThemeResource(this Binding binding, string themeResourceName, object parseContext)
	{
		binding.TargetNullValueThemeResource = themeResourceName;
		binding.ParseContext = parseContext;
		return binding;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Binding ApplyFallbackValueThemeResource(this Binding binding, string themeResourceName, object parseContext)
	{
		binding.FallbackValueThemeResource = themeResourceName;
		binding.ParseContext = parseContext;
		return binding;
	}
}
