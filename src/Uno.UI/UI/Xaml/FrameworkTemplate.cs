#nullable enable

using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Markup;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Defines a builder to be used in <see cref="FrameworkTemplate"/>
	/// </summary>
	public delegate View? FrameworkTemplateBuilder(object? owner, DependencyObject? templatedParent);

	[ContentProperty(Name = "Template")]
	public partial class FrameworkTemplate : DependencyObject, IFrameworkTemplateInternal
	{
		internal readonly FrameworkTemplateBuilder? _viewFactory;
		private readonly int _hashCode;
		private readonly ManagedWeakReference? _ownerRef;

		/// <summary>
		/// The scope at the time of the template's creation, which will be used when its contents are materialized.
		/// </summary>
		private readonly XamlScope _xamlScope;

		protected FrameworkTemplate()
			=> throw new NotSupportedException("Use the factory constructors");

		public FrameworkTemplate(Func<View?>? factory)
			: this(null, (o, tp) => factory?.Invoke())
		{
		}

		public FrameworkTemplate(object? owner, FrameworkTemplateBuilder? factory)
		{
			InitializeBinder();

			_viewFactory = factory;
			_ownerRef = WeakReferencePool.RentWeakReference(this, owner);

			// Compute the hash for this template once, it will be used a lot
			// in the ControlPool's internal dictionary.
			_hashCode = (factory?.Target?.GetHashCode() ?? 0) ^ (factory?.Method.GetHashCode() ?? 0);

			_xamlScope = ResourceResolver.CurrentScope;
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
		internal protected View? LoadContentCachedCore(DependencyObject? templatedParent) => FrameworkTemplatePool.Instance.DequeueTemplate(this, templatedParent);

		/// <summary>
		/// Manually return an unused template root created by <see cref="LoadContentCached"/> to the pool.
		/// </summary>
		/// <remarks>
		/// This is only used in specialized contexts. Normally the template reuse will be automatically handled by the pool.
		/// </remarks>
		internal void ReleaseTemplateRoot(View templateRoot) => FrameworkTemplatePool.Instance.ReleaseTemplateRoot(templateRoot, this);

		/// <summary>
		/// Creates a new instance of the current template.
		/// </summary>
		/// <returns>A new instance of the template</returns>
		View? IFrameworkTemplateInternal.LoadContent(DependencyObject? templatedParent)
		{
			try
			{
				ResourceResolver.PushNewScope(_xamlScope);
				var view = _viewFactory?.Invoke(_ownerRef?.Target, templatedParent);

				return view;
			}
			finally
			{
				ResourceResolver.PopScope();
			}
		}

		public override bool Equals(object? obj)
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

			public bool Equals(FrameworkTemplate? left, FrameworkTemplate? right) =>

				// Same instance
				ReferenceEquals(left, right)

				// Same delegate (possible if the delegate was created from a
				// lambda, which are cached automatically by the C# compiler (as of v6.0)
				|| left?._viewFactory == right?._viewFactory

				// Same target method (instance or static) (possible if the delegate was created from a
				// method group, which are *not* cached by the C# compiler (required by
				// the C# spec as of version 6.0)
				|| (
					ReferenceEquals(left?._viewFactory?.Target, right?._viewFactory?.Target)
					&& left?._viewFactory?.Method == right?._viewFactory?.Method
					);

			public int GetHashCode(FrameworkTemplate obj) => obj._hashCode;
		}
	}
}

