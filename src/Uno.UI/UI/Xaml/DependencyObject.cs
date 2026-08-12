using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.DataBinding;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Represents an object that participates in the dependency property system.
	/// </summary>
	/// <remarks>
	/// This is the root of the Uno visual-tree and property hierarchy. The dependency-property
	/// store and binder are carried directly on this type; the machinery that used to live in a
	/// separate <c>DependencyObjectStore</c> now spans the <c>DependencyObject.*.cs</c> partials.
	/// </remarks>
	[global::Microsoft.UI.Xaml.Data.Bindable]
	public partial class DependencyObject : IDependencyObjectInternal, IWeakReferenceProvider
	{
		private BinderReferenceHolder _refHolder;
		private ManagedWeakReference _selfWeakReference;

		public CoreDispatcher Dispatcher => CoreApplication.MainView.Dispatcher;

		public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

		// Public property-system and binder API. These stay in this nullable-oblivious file so their signatures
		// match the historical (interface-era) public surface exactly; the implementations live on the
		// nullable-annotated DependencyObject.*.cs partials.
		public object GetValue(DependencyProperty dp) => GetValueInternal(dp);

		public void SetValue(DependencyProperty dp, object value) => SetValueInternal(dp, value);

		public void ClearValue(DependencyProperty dp) => ClearValueInternal(dp);

		public object ReadLocalValue(DependencyProperty dp) => ReadLocalValueInternal(dp);

		public object GetAnimationBaseValue(DependencyProperty dp) => GetAnimationBaseValueInternal(dp);

		public long RegisterPropertyChangedCallback(DependencyProperty dp, DependencyPropertyChangedCallback callback) => RegisterPropertyChangedCallbackInternal(dp, callback);

		public void UnregisterPropertyChangedCallback(DependencyProperty dp, long token) => UnregisterPropertyChangedCallbackInternal(dp, token);

		// Uno-only, but public because the XAML source generator emits calls to these into the
		// consuming assembly (XamlFileGenerator emits the DependencyProperty overload for DPs and
		// the string overload for the x:Bind/POCO path).
		/// <summary>
		/// Set a binding using a regular or attached DependencyProperty
		/// </summary>
		/// <param name="dependencyProperty">The dependency property to bind</param>
		/// <param name="binding">The binding expression</param>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void SetBinding(DependencyProperty dependencyProperty, BindingBase binding) => SetBindingInternal(dependencyProperty, binding);

		[EditorBrowsable(EditorBrowsableState.Never)]
		public void SetBinding(string dependencyProperty, BindingBase binding) => SetBindingInternal(dependencyProperty, binding);

		internal ManagedWeakReference SelfWeakReference
		{
			get
			{
				if (_selfWeakReference is null)
				{
					// Published atomically: consumers compare handle identity, and a losing racer's
					// handle cannot be pooled back (it is a self reference), so it must not escape.
					Interlocked.CompareExchange(ref _selfWeakReference, WeakReferencePool.RentSelfWeakReference(this), null);
				}

				return _selfWeakReference;
			}
		}

		ManagedWeakReference IWeakReferenceProvider.WeakReference => SelfWeakReference;

		void IDependencyObjectInternal.OnPropertyChanged2(DependencyPropertyChangedEventArgs args) => OnPropertyChanged2(args);

		internal virtual void OnPropertyChanged2(DependencyPropertyChangedEventArgs args) { }

		private void __InitializeBinder()
		{
			if (BinderReferenceHolder.IsEnabled)
			{
				_refHolder = new BinderReferenceHolder(this.GetType(), this);
			}
		}

		// Invoked from constructors of derived types. The binder is initialized from the instance
		// constructor now, so this is a no-op kept to avoid churning every derived constructor.
		private protected void InitializeBinder() { }

		public override string ToString() => GetType().FullName;

		#region TemplatedParent DependencyProperty // legacy api, should no longer to be used.

		[EditorBrowsable(EditorBrowsableState.Never)]
		public DependencyObject TemplatedParent
		{
			get => (DependencyObject)GetValue(TemplatedParentProperty);
			set => SetValue(TemplatedParentProperty, value);
		}

		// Using a DependencyProperty as the backing store for TemplatedParent.  This enables animation, styling, binding, etc...
		[EditorBrowsable(EditorBrowsableState.Never)]
		[UnconditionalSuppressMessage("Trimming", "IL2111")]
		public static DependencyProperty TemplatedParentProperty { get; } =
			DependencyProperty.Register(
				name: nameof(TemplatedParent),
				propertyType: typeof(DependencyObject),
				ownerType: typeof(DependencyObject),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: /*FrameworkPropertyMetadataOptions.Inherits | */FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.WeakStorage,
					propertyChangedCallback: (s, e) => ((DependencyObject)s).OnTemplatedParentChanged(e)
				)
			);

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal protected virtual void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion
	}
}
