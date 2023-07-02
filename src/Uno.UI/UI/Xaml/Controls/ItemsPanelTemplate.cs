#nullable enable

using System;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;

#if __ANDROID__
using View = Android.Views.View;
using ViewGroup = Android.Views.ViewGroup;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using View = AppKit.NSView;
using ViewGroup = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsPanelTemplate : FrameworkTemplate
	{
		public ItemsPanelTemplate() : this(null) { }

		public ItemsPanelTemplate(Func<View?>? factory)
			: base(factory)
		{
		}

		/// <summary>
		/// Build an ItemsPanelTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the ItemsPanelTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		public ItemsPanelTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

		public static implicit operator ItemsPanelTemplate(Func<View?>? obj)
			=> new ItemsPanelTemplate(obj);

		public static implicit operator Func<View?>(ItemsPanelTemplate? obj)
			=> () => (obj as IFrameworkTemplateInternal)?.LoadContent();
	}
}

