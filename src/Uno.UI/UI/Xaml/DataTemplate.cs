#nullable enable

using System;

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

namespace Windows.UI.Xaml
{
	public partial class DataTemplate : FrameworkTemplate
	{
		public DataTemplate() : base(null) { }

		public DataTemplate(Func<View?>? factory)
			: base(factory)
		{
		}

		/// <summary>
		/// Build a DataTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the DataTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		public DataTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

		public static implicit operator DataTemplate?(Func<View?>? obj)
		{
			if (obj == null)
			{
				return null;
			}

			return new DataTemplate(obj);
		}

		public View? LoadContent()
			=> ((IFrameworkTemplateInternal)this).LoadContent();
	}
}

