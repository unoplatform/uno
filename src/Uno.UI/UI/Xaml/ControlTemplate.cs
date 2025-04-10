﻿#nullable enable

using System;
using Microsoft.UI.Xaml.Media;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls
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
		public ControlTemplate(object? owner, NewFrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		public ControlTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: base(owner, factory)
		{
		}
#endif

		public Type? TargetType { get; set; }

		internal View? LoadContentCached(Control templatedParent)
		{
			var root = base.LoadContentCachedCore(templatedParent);

			return root;
		}
	}
}
