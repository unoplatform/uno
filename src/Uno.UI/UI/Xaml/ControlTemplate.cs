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
		private readonly bool _shouldInjectTemplatedParent;

		public ControlTemplate() : base(null) { }

		public ControlTemplate(Func<View?>? factory)
			: base(factory)
		{
			_shouldInjectTemplatedParent = true;
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

		public Type? TargetType { get; set; }

		internal View? LoadContentCached(Control templatedParent)
		{
			var root = base.LoadContentCachedCore(templatedParent);
			if (_shouldInjectTemplatedParent)
			{
				// here we are in an alternative path,
				// where custom factory method is provided possibly without TemplatedParent propagation
				// so we have to correct here.
				// todo@xy: ^
			}

			return root;
		}
	}
}

