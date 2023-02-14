#if DEBUG
#nullable enable

using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{

	public partial class FrameworkTemplate
	{
		/// <summary>
		/// Debugging aid which returns the resource key associated with this resource, if it came from a <see cref="ResourceDictionary"/>.
		/// </summary>
		/// <remarks>Note: The DEBUG_SET_RESOURCE_SOURCE symbol must be set in <see cref="ResourceDictionary"/> for this to return a value.</remarks>
		public string ResourceNameDebug => this.GetResourceNameDebug();

		public ResourceDictionary? ContainingResourceDictionaryDebug => this.GetContainingResourceDictionaryDebug();
	}
}


#endif
