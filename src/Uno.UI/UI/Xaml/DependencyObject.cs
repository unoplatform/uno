using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using Uno.UI;
using Uno.UI.Controls;
using Uno.UI.DataBinding;
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
		private readonly static IEventProvider _binderTrace = Tracing.Get(DependencyObjectStore.TraceProvider.Id);

		private DependencyObjectStore __storeBackingField;
		private BinderReferenceHolder _refHolder;
		private ManagedWeakReference _selfWeakReference;

		public global::Windows.UI.Core.CoreDispatcher Dispatcher => global::Windows.ApplicationModel.Core.CoreApplication.MainView.Dispatcher;

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

		void IDependencyObjectInternal.OnPropertyChanged2(global::Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs args) => OnPropertyChanged2(args);

		internal virtual void OnPropertyChanged2(global::Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs args) { }

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
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public void ClearBindings() => __Store.ClearBindings();

		/// <summary>
		/// Obsolete method kept for binary compatibility
		/// </summary>
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public void RestoreBindings() => __Store.RestoreBindings();

		global::Uno.UI.DataBinding.ManagedWeakReference IWeakReferenceProvider.WeakReference
			=> _selfWeakReference ??= global::Uno.UI.DataBinding.WeakReferencePool.RentSelfWeakReference(this);

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
		[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2111")]
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

		public void SetBinding(object target, string dependencyProperty, global::Microsoft.UI.Xaml.Data.BindingBase binding)
			=> __Store.SetBinding(target, dependencyProperty, binding);

		public void SetBinding(string dependencyProperty, global::Microsoft.UI.Xaml.Data.BindingBase binding)
			=> __Store.SetBinding(dependencyProperty, binding);

		public void SetBinding(DependencyProperty dependencyProperty, global::Microsoft.UI.Xaml.Data.BindingBase binding)
			=> __Store.SetBinding(dependencyProperty, binding);

		public void SetBindingValue(object value, [CallerMemberName] string propertyName = null)
			=> __Store.SetBindingValue(value, propertyName);

		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		internal bool IsAutoPropertyInheritanceEnabled { get => __Store.IsAutoPropertyInheritanceEnabled; set => __Store.IsAutoPropertyInheritanceEnabled = value; }

		public global::Microsoft.UI.Xaml.Data.BindingExpression GetBindingExpression(DependencyProperty dependencyProperty)
			=> __Store.GetBindingExpression(dependencyProperty);

		public void ResumeBindings() => __Store.ResumeBindings();

		public void SuspendBindings() => __Store.SuspendBindings();
	}
}
