#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Helpers;

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
		private readonly int _hashCode;
		private readonly ManagedWeakReference? _ownerRef;
		private const bool _isLegacyTemplate = true; // Tests fails if set to false, so we keep it true for now.

		/// <summary>
		/// XAML scope captured during template creation, used as context provider for resource resolution when materializing template content.
		/// </summary>
		private readonly XamlScope _xamlScope;

		/// <summary>
		/// The template builder factory. This field is mutable and may be updated at runtime
		/// only when Uno.UI.TemplateManager enables "template materialization override mode".
		/// In this mode, the factory can be replaced to support dynamic template materialization
		/// scenarios (e.g., hot reload, design-time updates). Outside of this mode, the field
		/// should remain unchanged. See Uno.UI.TemplateManager for details.
		/// </summary>
		/// <remarks>
		/// This delegate may be wrapped to align its signature with <see cref="NewFrameworkTemplateBuilder"/>.
		/// The original factory method can always be found in <see cref="ViewFactoryInner"/>.
		/// </remarks>
		internal IDelegate<NewFrameworkTemplateBuilder>? ViewFactory { get; private set; }

		internal IDelegate<Delegate>? ViewFactoryInner { get; private set; }

		protected FrameworkTemplate()
			=> throw new NotSupportedException("Use the factory constructors");

		public FrameworkTemplate(Func<View?>? factory)
			: this(null, factory, true)
		{
			// TODO: to be removed on next major update.
			// This overload simply should not exist, since the materialized members do not have the tp injected.
			// It can lead to issues like template-parent binding not working...
			// Currently, it seems to be only used in unit tests & runtime tests.

			SetViewFactory(factory, AdaptViewFactory);
		}

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		public FrameworkTemplate(object? owner, FrameworkTemplateBuilder? factory)
			: this(owner, factory, true)
		{
			// TODO: to be removed on next major update.

			SetViewFactory(factory, AdaptFrameworkTemplateBuilder);
		}
#endif

		public FrameworkTemplate(object? owner, NewFrameworkTemplateBuilder? factory)
			: this(owner, factory, false)
		{
			SetViewFactory(factory);
		}

		private FrameworkTemplate(object? owner, Delegate? rawFactory, bool legacy)
		{
			InitializeBinder();

			// TODO: to restore; but, see note about _isLegacyTemplate first
			//_isLegacyTemplate = legacy;

			_ownerRef = WeakReferencePool.RentWeakReference(this, owner);

			// Compute the hash for this template once, it will be used a lot
			// in the ControlPool's internal dictionary.
			_hashCode = HashCode.Combine(rawFactory?.Target, rawFactory?.Method);
#if DEBUG
			TemplateSource = $"{rawFactory?.Method.DeclaringType}.{rawFactory?.Method.Name}";
			if (rawFactory?.Target is { })
			{
				TemplateSource += $", target={rawFactory.Target.GetType()}";
			}
#endif

			_xamlScope = ResourceResolver.CurrentScope;
		}

		/// <summary>
		/// Sets the view factory. Internal method to avoid unwanted changes from outside the framework.
		/// </summary>
		/// <param name="factory">The new factory to set</param>
		internal void SetViewFactory(NewFrameworkTemplateBuilder? factory)
		{
			// When the factory target is a top-level XAML class (e.g. Page, ResourceDictionary, ...) which commonly implements IWeakReferenceProvider,
			// we wrap the delegate in a weak reference so that the template does not keep that object alive and cause memory leaks.
			// When the target does not implement IWeakReferenceProvider (typically a compiler-generated closure class), we keep a strong
			// reference via LiteralDelegate so the closure stays alive. If the factory doesn't have a target (no capture), we also use the literal
			// delegate without additional overhead.
			ViewFactory = factory switch
			{
				{ Target: IWeakReferenceProvider } => DelegateHelper.CreateWeak(factory),
				{ } => DelegateHelper.CreateLiteral(factory),
				null => null,
			};
			ViewFactoryInner = ViewFactory;
		}

		internal void SetViewFactory<TDelegate>(TDelegate? factory, Func<object?, TemplateMaterializationSettings, IDelegate<TDelegate>, View?> adapter)
			where TDelegate : Delegate
		{
			if (factory is { })
			{
				// When the factory target is a top-level XAML class (e.g. Page, ResourceDictionary, ...) which commonly implements IWeakReferenceProvider,
				// we wrap the delegate in a weak reference so that the template does not keep that object alive and cause memory leaks.
				// When the target does not implement IWeakReferenceProvider (typically a compiler-generated closure class), we keep a strong
				// reference via LiteralDelegate so the closure stays alive. If the factory doesn't have a target (no capture), we also use the literal
				// delegate without additional overhead.
				var inner = factory.Target is IWeakReferenceProvider
					? DelegateHelper.CreateWeak(factory)
					: DelegateHelper.CreateLiteral(factory) as IDelegate<TDelegate>;
				// the adapted factory must not be a weak delegate, as we would lose the captures otherwise.
				var adapted = DelegateHelper.CreateLiteral<NewFrameworkTemplateBuilder>(
					(o, s) => adapter(o, s, inner)
				);

				ViewFactory = adapted;
				ViewFactoryInner = inner;
			}
			else
			{
				ViewFactory = null;
				ViewFactoryInner = null;
			}
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
		internal protected View? LoadContentCachedCore(DependencyObject? templatedParent) =>
			FrameworkTemplatePool.Instance.DequeueTemplate(this, templatedParent);

		/// <summary>
		/// Manually return an unused template root created by <see cref="LoadContentCached"/> to the pool.
		/// </summary>
		/// <remarks>
		/// This is only used in specialized contexts. Normally the template reuse will be automatically handled by the pool.
		/// </remarks>
		internal void ReleaseTemplateRoot(View templateRoot) =>
			FrameworkTemplatePool.Instance.ReleaseTemplateRoot(templateRoot, this);

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

					var view = ViewFactory?.Delegate?.Invoke(_ownerRef?.Target, settings);

					return view;
				}
				else
				{
					var members = new List<DependencyObject>();
					var settings = new TemplateMaterializationSettings(templatedParent, members.Add);

					var view = ViewFactory?.Delegate?.Invoke(_ownerRef?.Target, settings);

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

		internal virtual View? LoadContent(FrameworkElement templatedParent)
			=> ((IFrameworkTemplateInternal)this).LoadContent(templatedParent);

		public override bool Equals(object? obj)
		{
			if (obj is FrameworkTemplate other)
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

		private static View? AdaptFrameworkTemplateBuilder(object? owner, TemplateMaterializationSettings settings, IDelegate<FrameworkTemplateBuilder> del)
		{
			return del.Delegate?.Invoke(owner);
		}

		private static View? AdaptViewFactory(object? owner, TemplateMaterializationSettings settings, IDelegate<Func<View?>> del)
		{
			return del.Delegate?.Invoke();
		}

		internal class FrameworkTemplateEqualityComparer : IEqualityComparer<FrameworkTemplate>
		{
			public static readonly FrameworkTemplateEqualityComparer Default = new FrameworkTemplateEqualityComparer();

			private FrameworkTemplateEqualityComparer() { }

			public bool Equals(FrameworkTemplate? left, FrameworkTemplate? right) =>

				// Same instance
				ReferenceEquals(left, right)

				// Same delegate (possible if the delegate was created from a
				// lambda, which are cached automatically by the C# compiler (as of v6.0)
				|| left?.ViewFactory == right?.ViewFactory

				// Same target method (instance or static) (possible if the delegate was created from a
				// method group, which are *not* cached by the C# compiler (required by
				// the C# spec as of version 6.0)
				|| (left?._hashCode == right?._hashCode)
				;

			public int GetHashCode(FrameworkTemplate obj) => obj._hashCode;
		}

		// --- Uno extension points for template factory injection and update notifications ---

		// Use weak attached field to avoid adding a field to every FrameworkTemplate instance
		// when the dynamic template update feature is not used
		private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<FrameworkTemplate, global::Windows.UI.Core.WeakEventHelper.WeakEventCollection> _templateUpdatedHandlers = new();

		internal IDisposable RegisterTemplateUpdated(Action handler)
		{
			var handlers = _templateUpdatedHandlers.GetOrCreateValue(this);
			return global::Windows.UI.Core.WeakEventHelper.RegisterEvent(
				handlers,
				handler,
				(h, s, a) => (h as Action)?.Invoke()
			);
		}

		internal bool UpdateFactory(Func<View?> value)
		{
			if (value?.Method != ViewFactoryInner?.Method)
			{
				SetViewFactory(value, AdaptViewFactory);

				// Only invoke handlers if they exist for this instance
				if (_templateUpdatedHandlers.TryGetValue(this, out var handlers))
				{
					handlers.Invoke(this, null);
				}

				return true;
			}

			return false;
		}

		internal bool UpdateFactory(Func<NewFrameworkTemplateBuilder?, NewFrameworkTemplateBuilder?> update)
		{
			// Special case to update the factory without creating a new instance.
			// A special mode is required for it to work and is activated directly in the Uno.UI.TemplateManager.

			var previous = ViewFactory?.Delegate;
			var newFactory = update?.Invoke(previous);
			if (newFactory != previous)
			{
				SetViewFactory(newFactory);

				// Only invoke handlers if they exist for this instance
				if (_templateUpdatedHandlers.TryGetValue(this, out var handlers))
				{
					handlers.Invoke(this, null);
				}

				return true;
			}

			return false;
		}
	}
}
