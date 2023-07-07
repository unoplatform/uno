#nullable enable

using System;
using Uno.UI;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ControlTemplate : FrameworkTemplate
	{
		public ControlTemplate() : this(null) { }

		public ControlTemplate(Func<View?>? factory)
			: base(factory)
		{
		}

		/// <summary>
		/// Build a ControlTemplate with an optional <paramref name="owner"/> to be provided during the call of <paramref name="factory"/>
		/// </summary>
		/// <param name="owner">The owner of the ControlTemplate</param>
		/// <param name="factory">The factory to be called to build the template content</param>
		public ControlTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

		public static implicit operator ControlTemplate(Func<View>? obj)
			=> new ControlTemplate(obj);

		public Type? TargetType
		{
			get;
			set;
		}

		internal View? LoadContentCached(Control templatedParent)
		{
			using (TemplateParentResolver.RentScope(this, templatedParent, out var scope))
			{
				var root = base.LoadContentCachedCore();

				return root;
			}
		}
	}
}

