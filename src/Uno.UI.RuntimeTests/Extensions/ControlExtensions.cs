using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

internal static class ControlExtensions
{
#if WINAPPSDK
	public static T GetTemplateChild<T>(this Control control, string childName) where T : DependencyObject
	{
		return UnsafeGetTemplateChild(control, childName) as T;
	}

	public static DependencyObject GetTemplateChild(this Control control, string childName)
	{
		return UnsafeGetTemplateChild(control, childName);
	}

	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetTemplateChild")]
	extern static DependencyObject UnsafeGetTemplateChild(Control control, string childName);
#endif
}
