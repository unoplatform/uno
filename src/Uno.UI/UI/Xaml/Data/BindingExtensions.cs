using System.ComponentModel;
using Windows.UI.Xaml.Data;

#if __ANDROID__
using _NativeObject = Android.Views.View;
#elif __IOS__
using _NativeObject = Foundation.NSObject;
#else
#endif

namespace Uno.UI.Helpers.Xaml
{
	public static class BindingExtensions
	{
		public delegate Binding BindingApplyHandler(Binding binding);

		/// <summary>
		/// Executes the provided apply handler on the binding instance. Used by the XAML code generator.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static Binding BindingApply(this Binding instance, BindingApplyHandler apply)
		{
			apply(instance);
			return instance;
		}
	}
}

