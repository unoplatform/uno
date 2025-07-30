#nullable enable

using System;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Markup;

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Defines a builder to be used in <see cref="FrameworkTemplate"/>
	/// </summary>
	public delegate View? FrameworkTemplateBuilder(object? owner);

	/// <summary>
	/// Defines a builder to be used in <see cref="FrameworkTemplate"/>
	/// </summary>
	public delegate View? NewFrameworkTemplateBuilder(object? owner, TemplateMaterializationSettings settings);

	[ContentProperty(Name = "Template")]
	public partial class FrameworkTemplate : DependencyObject, IFrameworkTemplateInternal
	{
		internal readonly NewFrameworkTemplateBuilder? _viewFactory;
		private readonly int _hashCode;
		private readonly ManagedWeakReference? _ownerRef;
		private readonly bool _isLegacyTemplate = true; // Tests fails if set to false, so we keep it true for now.

		/// <summary>
		/// The scope at the time of the template's creataion, which will be used when its contents are materialized.
		/// </summary>
		private readonly XamlScope _xamlScope;

		protected FrameworkTemplate()
			=> throw new NotSupportedException("Use the factory constructors");

		public FrameworkTemplate(Func<View?>? factory)
			: this(null, (o, s) => factory?.Invoke(), factory)
		{
			// TODO: to be removed on next major update.
			// This overload simply should not exist, since the materialized members do not have the tp injected.
			// It can lead to issues like template-parent binding not working...
			// Currently, it seems to be only used in unit tests & runtime tests.
			this._isLegacyTemplate = true;
		}

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		public FrameworkTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: this(owner, (o, s) => factory?.Invoke(o), factory)
		{
			// TODO: to be removed on next major update.
			this._isLegacyTemplate = true;
		}
#endif

		public FrameworkTemplate(object? owner, NewFrameworkTemplateBuilder? factory)
			: this(owner, factory, factory)
		{
		}

		private FrameworkTemplate(object? owner, NewFrameworkTemplateBuilder? factory, Delegate? rawFactory)
		{
			InitializeBinder();

			_viewFactory = factory;
			_ownerRef = WeakReferencePool.RentWeakReference(this, owner);

			// Compute the hash for this template once, it will be used a lot
			// in the ControlPool's internal dictionary.
			_hashCode = HashCode.Combine(rawFactory?.Target, rawFactory?.Method);
#if DEBUG
			// `rawFactory` is guarantee to contains the actual factory method,
			// `factory` can sometime be the lambda from the overloads
			TemplateSource = $"{rawFactory?.Method.DeclaringType}.{rawFactory?.Method.Name}";
#endif

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
#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
				TemplatedParentScope.PushScope(templatedParent, _isLegacyTemplate);
#endif

				if (!FrameworkTemplatePool.IsPoolingEnabled || _isLegacyTemplate)
				{
					var settings = new TemplateMaterializationSettings(templatedParent, null);

					var view = _viewFactory?.Invoke(_ownerRef?.Target, settings);
					return view;
				}
				else
				{
					var members = new List<DependencyObject>();
					var settings = new TemplateMaterializationSettings(templatedParent, members.Add);

					var view = _viewFactory?.Invoke(_ownerRef?.Target, settings);

					if (view is { })
					{
						// TODO: impl recycling (tp update) for tracked template members
						FrameworkTemplatePool.Instance.TrackMaterializedTemplate(this, view, members);
					}

					return view;
				}
			}
			finally
			{
#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
				TemplatedParentScope.PopScope();
#endif
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
		public string TemplateSource { get; init; }
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

