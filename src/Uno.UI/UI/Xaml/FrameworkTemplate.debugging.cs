#if DEBUG
#nullable enable

using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
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
