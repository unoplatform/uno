using System;
using System.Collections.Generic;
using Android.Runtime;
using Android.Views;
using Android.Util;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using System.ComponentModel;
using Windows.UI.Xaml.Media;
using Android.Graphics;
using Android.Views.Animations;
using Uno.Collections;
using Uno.Disposables;
using Uno.UI.Media;

namespace Uno.UI.Controls
{
	/// <summary>
	/// A bindable FrameLayout.
	/// </summary>
	public partial class BindableView :
		UnoViewGroup,
		DependencyObject,
		IShadowChildrenProvider
	{
		private readonly MaterializableList<View> _childrenShadow = new MaterializableList<View>();

		static BindableView()
		{
			if ((int)Android.OS.Build.VERSION.SdkInt <= 10)
			{
				throw new Exception("The target SDK version must be higher than 10 (Honeycomb and higher).");
			}
		}

		public BindableView(Android.Content.Context context, int layoutId)
			: base(context)
		{
			Initialize();

			LayoutId = layoutId;
		}

		public BindableView(Android.Content.Context context)
			: base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			NativeInstanceHelper.CreateNativeInstance(base.GetType(), this, context, base.SetHandle);

			Initialize();
		}

		private void Initialize()
		{
			InitializeBinder();
		}

		protected override void OnLayoutCore(bool changed, int l, int t, int r, int b, bool localIsLayoutRequested)
		{

		}

		/// <summary>
		/// Provides the <see cref="View.MeasuredHeight"/> and <see cref="View.MeasuredWidth"/> in a single fast call.
		/// </summary>
		/// <remarks>
		/// This method exists to avoid having to call twice the View
		/// methods to get the measured width and height, and pay for the
		/// interop cost.
		/// </remarks>
		internal static Windows.Foundation.Size GetNativeMeasuredDimensionsFast(View view)
		{
			unchecked
			{
				var d = GetMeasuredDimensions(view);

				return new Windows.Foundation.Size(
					width: d & 0xFFFFFFFF,
					height: (d & (long)0xFFFFFFFF00000000) >> 32
				);
			}
		}

		/// <summary>
		/// Provides a shadowed list of views, used to limit the impact of the marshalling.
		/// </summary>
		List<View> IShadowChildrenProvider.ChildrenShadow => _childrenShadow.Materialized;

		internal List<View>.Enumerator GetChildrenEnumerator() => _childrenShadow.Materialized.GetEnumerator();

		/// <summary>
		/// Gets the view at a specific position.
		/// </summary>
		/// <remarks>This method does not call the actual GetChildAt method,
		/// but an optimized version of it that does not require a hierarchy
		/// observer.</remarks>
		public new virtual View GetChildAt(int position) => _childrenShadow[position];

		/// <summary>
		/// Gets the number of child views.
		/// </summary>
		/// <remarks>This method does not call the actual ChildCount method,
		/// but an optimized version of it that does not require a hierarchy
		/// observer.</remarks>
		public new virtual int ChildCount => _childrenShadow.Count;

		/// <summary>
		/// Adds a view to the current view group.
		/// </summary>
		/// <remarks>This method does not call the actual AddView method,
		/// but an optimized version of it that does not require a hierarchy
		/// observer.</remarks>
		public new virtual void AddView(View view)
		{
			view.TrySetParent(this);

			_childrenShadow.Add(view);
			base.AddViewFast(view);
			OnChildViewAdded(view);
		}

		/// <summary>
		/// Adds a view to the current view group using a position index.
		/// </summary>
		/// <remarks>This method does not call the actual AddView method,
		/// but an optimized version of it that does not require a hierarchy
		/// observer.</remarks>
		public new virtual void AddView(View view, int index)
		{
			view.TrySetParent(this);

			_childrenShadow.Insert(index, view);
			base.AddViewFast(view, index);
			OnChildViewAdded(view);
		}

		/// <summary>
		/// Removes a view from the current view group.
		/// </summary>
		/// <remarks>This method does not call the actual AddView method,
		/// but an optimized version of it that does not require a hierarchy
		/// observer.</remarks>
		public new virtual void RemoveView(View view)
		{
			if (FeatureConfiguration.FrameworkElement.AndroidUseManagedLoadedUnloaded)
			{
				if (view is FrameworkElement fe)
				{
					fe.IsManagedLoaded = false;
					fe.PerformOnUnloaded();
					fe.SetParent(null);
				}
			}

			_childrenShadow.Remove(view);
			base.RemoveViewFast(view);

			ResetDependencyObjectParent(view);
			OnChildViewRemoved(view);
		}

		/// <summary>
		/// Removes a view from the current view group using a position index.
		/// </summary>
		/// <remarks>This method does not call the actual AddView method,
		/// but an optimized version of it that does not require a hierarchy
		/// observer.</remarks>
		public new virtual void RemoveViewAt(int index)
		{
			var removedView = _childrenShadow[index];

			if (FeatureConfiguration.FrameworkElement.AndroidUseManagedLoadedUnloaded)
			{
				if (removedView is FrameworkElement fe)
				{
					fe.IsManagedLoaded = false;
					fe.PerformOnUnloaded();
					fe.SetParent(null);
				}
			}

			_childrenShadow.RemoveAt(index);
			base.RemoveViewAtFast(index);

			ResetDependencyObjectParent(removedView);
			OnChildViewRemoved(removedView);
		}

		/// <summary>
		/// Moves a view from one position to another position, without unloading it.
		/// </summary>
		/// <param name="oldIndex">The old index of the item</param>
		/// <param name="newIndex">The new index of the item</param>
		/// <remarks>
		/// The trick for this method is to move the child from one position to the other
		/// without calling RemoveView and AddView. In this context, the only way to do this is
		/// to call BringToFront, which is the only available method on ViewGroup that manipulates
		/// the index of a view, even if it does not allow for specifying an index.
		/// </remarks>
		internal void MoveViewTo(int oldIndex, int newIndex)
		{
			var view = _childrenShadow[oldIndex];

			_childrenShadow.RemoveAt(oldIndex);
			_childrenShadow.Insert(newIndex, view);

			var reorderIndex = Math.Min(oldIndex, newIndex);

			for (int i = reorderIndex; i < _childrenShadow.Count; i++)
			{
				_childrenShadow[i].BringToFront();
			}
		}

		/// <summary>
		/// Registers the native adapter which handles the RenderTransform on a child view
		/// </summary>
		internal void RegisterChildTransform(NativeRenderTransformAdapter transformation)
			=> SetChildRenderTransform(transformation.Owner, transformation.Matrix);

		/// <summary>
		/// Removes the native adapter which handles the RenderTransform on a child view
		/// </summary>
		internal void UnregisterChildTransform(NativeRenderTransformAdapter transformation)
			=> RemoveChildRenderTransform(transformation.Owner);

		/// <summary>
		/// Should not be used directly. Notifies that a view has been added to the ViewGroup using the native AddView methods.
		/// </summary>
		protected override void OnLocalViewAdded(View view, int index)
		{
			_childrenShadow.Insert(index, view);
			OnChildViewAdded(view);
		}

		/// <summary>
		/// Should not be used directly. Notifies that a view has been added to the ViewGroup using the native AddView methods.
		/// </summary>
		protected override void OnLocalViewRemoved(View view)
		{
			_childrenShadow.Remove(view);
		}

		/// <summary>
		/// Invoked when a child view has been added.
		/// </summary>
		/// <param name="view">The view being added</param>
		protected virtual void OnChildViewAdded(View view)
		{
		}

		/// <summary>
		/// Invoked when a child view has been removed.
		/// </summary>
		/// <param name="view">The view being removed</param>
		protected virtual void OnChildViewRemoved(View view)
		{
		}

		/// <summary>
		/// Provides a default implementation for the HitCheck
		/// performed in the UnoViewGroup java class.
		/// </summary>
		/// <returns></returns>
		protected override bool NativeHitCheck()
		{
			return true;
		}

		/// <summary>
		/// Determines if the native call to RequestLayout should call its base.
		/// </summary>
		/// <returns></returns>
		protected override bool NativeRequestLayout()
		{
			return !_shouldPreventRequestLayout;
		}

		private bool _shouldPreventRequestLayout;

		internal IDisposable PreventRequestLayout()
		{
			_shouldPreventRequestLayout = true;

			return Disposable.Create(() => _shouldPreventRequestLayout = false);
		}

		/// <summary>
		/// Called whenever the current view is being removed from its parent. This method is
		/// called only if the parent is a BindableView.
		/// </summary>
		protected override void OnRemovedFromParent()
		{

		}

		/// <summary>
		/// Gest the currently applied Layout ID
		/// </summary>
		public int LayoutId { get; private set; }


		/// <summary>
		/// Resets the dependency object parent for non BindableView views, but that implement IDependencyObject provider.
		/// </summary>
		/// <remarks>
		/// This is required on Android because native <see cref="View"/> instances
		/// can't be notified if their parent changes. <see cref="UnoViewGroup"/> provides this behavior
		/// by intercepting add/remove children and calls <see cref="OnRemovedFromParent"/>.
		/// <see cref="FrameworkTemplatePool"/> relies on knowing that the <see cref="DependencyObject.Parent"/> of a
		/// pooled instances has been reset so for non <see cref="BindableView"/> instances we reset it manually.
		/// </remarks>
		private static void ResetDependencyObjectParent(View view)
		{
			if (!(view is BindableView))
			{
				var provider = view as DependencyObject;

				if (provider != null)
				{
					provider.SetParent(null);
				}
			}
		}

		/// <summary>
		/// Call from non-<see cref="UnoViewGroup"/> parents from <see cref="RemoveView(View)"/> and <see cref="RemoveViewAt(int)"/> in
		/// order to clear the <see cref="DependencyObject.Parent"/> correctly.
		/// </summary>
		internal void NotifyRemovedFromParent()
		{
			OnRemovedFromParent();
		}

		public override float Elevation
		{
			get => base.Elevation;
			set
			{
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
				{
					OutlineProvider = new FrameworkElementOutlineProvider();
				}

				base.Elevation = value;
			}
		}
	}
}
