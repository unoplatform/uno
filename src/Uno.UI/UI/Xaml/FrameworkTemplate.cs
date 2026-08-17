#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml.Markup;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Helpers;

using View = Microsoft.UI.Xaml.UIElement;

namespace Microsoft.UI.Xaml
{
	[ContentProperty(Name = "Template")]
	public partial class FrameworkTemplate : DependencyObject, IFrameworkTemplateInternal
	{
		private readonly int _hashCode;
		private readonly ManagedWeakReference? _ownerRef;

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
		internal IDelegate<FrameworkTemplateBuilder>? ViewFactory { get; private set; }

		protected FrameworkTemplate()
			=> throw new NotSupportedException("Use the factory constructors");

		internal FrameworkTemplate(object? owner, FrameworkTemplateBuilder? factory)
		{
			InitializeBinder();

			_ownerRef = WeakReferencePool.RentWeakReference(this, owner);

			// Compute the hash for this template once, it will be used a lot
			// in the ControlPool's internal dictionary.
			_hashCode = HashCode.Combine(factory?.Target, factory?.Method);
#if DEBUG
			TemplateSource = $"{factory?.Method.DeclaringType}.{factory?.Method.Name}";
			if (factory?.Target is { })
			{
				TemplateSource += $", target={factory.Target.GetType()}";
			}
#endif

			_xamlScope = ResourceResolver.CurrentScope;

			SetViewFactory(factory);
		}

		/// <summary>
		/// Sets the view factory. Internal method to avoid unwanted changes from outside the framework.
		/// </summary>
		/// <param name="factory">The new factory to set</param>
		internal void SetViewFactory(FrameworkTemplateBuilder? factory)
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

				var members = FrameworkTemplatePool.IsPoolingEnabled ? new List<DependencyObject>() : null;
				var settings = new TemplateMaterializationSettings(templatedParent, members);

				var view = ViewFactory?.Delegate?.Invoke(_ownerRef?.Target, settings);

				if (view is { } && members is { })
				{
					// TODO: impl recycling (tp update) for tracked template members. Lazily materialized
					// ones (x:Load, VisualState) do get their templated parent, but they are created
					// after this snapshot is taken, so they are absent from the tracked set.
					FrameworkTemplatePool.Instance.TrackMaterializedTemplate(view, members);
				}

				return view;
			}
			finally
			{
				ResourceResolver.PopScope();
			}
		}

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
		private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<FrameworkTemplate, global::Windows.UI.Core.WeakEventHelper.WeakEventCollection> _templateUpdatedHandlers = new();

		internal IDisposable RegisterTemplateUpdated(Action handler)
		{
			var handlers = _templateUpdatedHandlers.GetOrCreateValue(this);
			return global::Windows.UI.Core.WeakEventHelper.RegisterEvent(
				handlers,
				handler,
				(h, s, a) => (h as Action)?.Invoke()
			);
		}

		internal bool UpdateFactory(Func<FrameworkTemplateBuilder?, FrameworkTemplateBuilder?> update)
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
