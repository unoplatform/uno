using System;
using System.Collections.Generic;
using Uno.Extensions;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml
{
	public partial class FrameworkTemplate : DependencyObject
	{

		private readonly Func<View> _viewFactory;
		private readonly int _hashCode;

		protected FrameworkTemplate() { }

		public FrameworkTemplate(Func<View> factory)
		{
			InitializeBinder();

			_viewFactory = factory;

			// Compute the hash for this template once, it will be used a lot
			// in the ControlPool's internal dictionary.
			_hashCode = (factory.Target?.GetHashCode() ?? 0) ^ factory.Method.GetHashCode();
		}

		public static implicit operator Func<View>(FrameworkTemplate obj)
		{
			return obj?._viewFactory;
		}

		/// <summary>
		/// Loads a potentially cached template from the current template, see remarks for more details.
		/// </summary>
		/// <returns>A potentially cached instance of the template</returns>
		/// <remarks>
		/// The owner of the template is the system, which means that an 
		/// instance that has been detached from its parent may be reused at any time.
		/// If a control needs to be the owner of a created instance, it needs to use <see cref="LoadContent"/>.
		/// </remarks>
		internal View LoadContentCached()
		{
			return FrameworkTemplatePool.Instance.DequeueTemplate(this);
		}

		/// <summary>
		/// Creates a new instance of the current template.
		/// </summary>
		/// <returns>A new instance of the template</returns>
		public View LoadContent()
		{
			return _viewFactory();
		}

		public override bool Equals(object obj)
		{
			var other = obj as FrameworkTemplate;

			if (other != null)
			{
				if (FrameworkTemplateEqualityComparer.Default.Equals(other, this))
				{
					return true;
				}
			}

			return base.Equals(obj);
		}

		public override int GetHashCode() => _hashCode;

#if DEBUG
		public string TemplateSource => $"{_viewFactory?.Method.DeclaringType}.{_viewFactory?.Method.Name}";
#endif

		internal class FrameworkTemplateEqualityComparer : IEqualityComparer<FrameworkTemplate>
		{
			public static readonly FrameworkTemplateEqualityComparer Default = new FrameworkTemplateEqualityComparer();

			private FrameworkTemplateEqualityComparer() { }

			public bool Equals(FrameworkTemplate left, FrameworkTemplate right) =>

				// Same instance
				ReferenceEquals(left, right)

				// Same delegate (possible if the delegate was created from a 
				// lambda, which are cached automatically by the C# compiler (as of v6.0)
				|| left._viewFactory == right._viewFactory

				// Same target method (instance or static) (possible if the delegate was created from a 
				// method group, which are *not* cached by the C# compiler (required by 
				// the C# spec as of version 6.0)
				|| (
					ReferenceEquals(left._viewFactory.Target, right._viewFactory.Target)
					&& left._viewFactory.Method == right._viewFactory.Method
					);

			public int GetHashCode(FrameworkTemplate obj) => obj._hashCode;
		}
	}
}

