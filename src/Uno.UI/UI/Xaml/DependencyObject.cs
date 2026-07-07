using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
	/// store and binder are carried here, replacing the per-type mixin that the
	/// <c>DependencyObjectGenerator</c> used to emit while <see cref="DependencyObject"/> was an interface.
	/// </remarks>
	[global::Microsoft.UI.Xaml.Data.Bindable]
	public partial class DependencyObject : IDependencyObjectStoreProvider, IDependencyObjectInternal, IWeakReferenceProvider
	{
		private DependencyObjectStore __storeBackingField;
		private BinderReferenceHolder _refHolder;
		private ManagedWeakReference _selfWeakReference;

		public CoreDispatcher Dispatcher => CoreApplication.MainView.Dispatcher;

		public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

		private DependencyObjectStore __Store
		{
			get
			{
				if (__storeBackingField == null)
				{
					__storeBackingField = new DependencyObjectStore(this);
					__InitializeBinder();
				}

				return __storeBackingField;
			}
		}

		public bool IsStoreInitialized => __storeBackingField != null;

		DependencyObjectStore IDependencyObjectStoreProvider.Store => __Store;

		public object GetValue(DependencyProperty dp) => __Store.GetValue(dp);

		public void SetValue(DependencyProperty dp, object value) => __Store.SetValue(dp, value);

		public void ClearValue(DependencyProperty dp) => __Store.ClearValue(dp);

		public object ReadLocalValue(DependencyProperty dp) => __Store.ReadLocalValue(dp);

		public object GetAnimationBaseValue(DependencyProperty dp) => __Store.GetAnimationBaseValue(dp);

		public long RegisterPropertyChangedCallback(DependencyProperty dp, DependencyPropertyChangedCallback callback) => __Store.RegisterPropertyChangedCallback(dp, callback);

		public void UnregisterPropertyChangedCallback(DependencyProperty dp, long token) => __Store.UnregisterPropertyChangedCallback(dp, token);

		void IDependencyObjectInternal.OnPropertyChanged2(DependencyPropertyChangedEventArgs args) => OnPropertyChanged2(args);

		internal virtual void OnPropertyChanged2(DependencyPropertyChangedEventArgs args) { }

		private void __InitializeBinder()
		{
			if (BinderReferenceHolder.IsEnabled)
			{
				_refHolder = new BinderReferenceHolder(this.GetType(), this);
			}
		}

		// Invoked from constructors of derived types; the binder itself is initialized lazily from
		// the __Store getter via __InitializeBinder().
		private protected void InitializeBinder() { }

		/// <summary>
		/// Obsolete method kept for binary compatibility
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearBindings() => __Store.ClearBindings();

		/// <summary>
		/// Obsolete method kept for binary compatibility
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RestoreBindings() => __Store.RestoreBindings();

		ManagedWeakReference IWeakReferenceProvider.WeakReference
			=> _selfWeakReference ??= WeakReferencePool.RentSelfWeakReference(this);

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

		public void SetBinding(object target, string dependencyProperty, BindingBase binding)
			=> __Store.SetBinding(target, dependencyProperty, binding);

		public void SetBinding(string dependencyProperty, BindingBase binding)
			=> __Store.SetBinding(dependencyProperty, binding);

		public void SetBinding(DependencyProperty dependencyProperty, BindingBase binding)
			=> __Store.SetBinding(dependencyProperty, binding);

		public void SetBindingValue(object value, [CallerMemberName] string propertyName = null)
			=> __Store.SetBindingValue(value, propertyName);

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal bool IsAutoPropertyInheritanceEnabled { get => __Store.IsAutoPropertyInheritanceEnabled; set => __Store.IsAutoPropertyInheritanceEnabled = value; }

		public BindingExpression GetBindingExpression(DependencyProperty dependencyProperty)
			=> __Store.GetBindingExpression(dependencyProperty);

		public void ResumeBindings() => __Store.ResumeBindings();

		public void SuspendBindings() => __Store.SuspendBindings();
	}
}
