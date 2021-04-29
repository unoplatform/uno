using System;
using System.Numerics;
using System.Threading;
using Windows.Foundation;
using Android.OS;

namespace Windows.UI.Composition
{
	public partial class Visual
	{
		//private /*VisualDirtyState*/ int _dirtyState;
		//private /*VisualDirtyState*/ int _renderDirtyState;

		//internal bool IsDirty => DirtyState > VisualDirtyState.None;

		//internal VisualDirtyState DirtyState => (VisualDirtyState)_dirtyState;

		///// <summary>
		///// Invalidates local
		///// </summary>
		///// <param name="kind"></param>
		//private protected void Invalidate(VisualDirtyState kind)
		//{
		//	if (VisualDirtyStateHelper.Invalidate(ref _dirtyState, kind))
		//	{
		//		if (Parent is { } parent)
		//		{
		//			parent.Invalidate(kind);
		//		}
		//		else
		//		{
		//			Compositor.InvalidateRoot(this, kind);
		//		}
		//	}
		//}

		//private bool IsDirty(VisualDirtyState kind)
		//	=> (_dirtyState & (int)kind) != 0;

		//private bool Reset(VisualDirtyState kind)
		//	=> VisualDirtyStateHelper.Reset(ref _renderDirtyState, kind);

		//private void ClearDirtyState()
		//{
		//	_dirtyState = VisualDirtyState.None;
		//}

		///// <inheritdoc />
		//internal override void Commit()
		//{
		//	base.Commit();
		//	VisualDirtyStateHelper.Commit(ref _fields, ref _dirtyState, ref _renderFields, ref _renderDirtyState);
		//}

		/// <inheritdoc />
		private protected override void OnInvalidated(CompositionPropertyType kind)
		{
			base.OnInvalidated(kind);

			if (Parent is { } parent)
			{
				parent.Invalidate(kind);

				global::System.Diagnostics.Debug.Assert((parent.PendingDirtyState & kind) == kind);
			}
			else
			{
				Compositor.InvalidateRoot(this, kind);
			}
		}

		/// <inheritdoc />
		private protected override void OnCommit()
		{
			base.OnCommit();

			_renderFields = _fields;
		}

		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine**, it's the second and final step of the composition.<br />
		/// 
		/// Natively renders this visual and its children.
		/// </summary>
		/// <remarks>If the platform has a compositor thread, this is expected to be invoked on that thread.</remarks>
		/// <remarks>This method is mutually-excluded with the <see cref="CompositionObject.Commit"/>.</remarks>
		internal virtual void Render()
		{
			RenderIndependent();
			RenderDependent();
		}

		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine**.<br />
		/// 
		/// Natively renders independent properties of this visual and its children.
		/// </summary>
		/// <remarks>If the platform has a compositor thread, this is expected to be invoked on that thread.</remarks>
		/// <remarks>This method is mutually-excluded with the <see cref="CompositionObject.Commit"/>.</remarks>
		internal void RenderIndependent()
		{
			if (Reset(CompositionPropertyType.Independent)) { }
			{
				// Give opportunity to implementers to run shared code
				OnRenderIndependent();

				// Effectively render the content to native
				NativeRenderIndependent();
			}
		}

		private protected virtual void OnRenderIndependent() { }
		partial void NativeRenderIndependent();

		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine**, it's the second and final step of the composition. <br />
		/// 
		/// Natively renders dependent properties of this visual and its children.
		/// </summary>
		/// <remarks>If the platform has a compositor thread, this is expected to be invoked on that thread.</remarks>
		/// <remarks>This method is mutually-excluded with the <see cref="CompositionObject.Commit"/>.</remarks>
		internal void RenderDependent()
		{
			if (Reset(CompositionPropertyType.Dependent)) { }
			{
				// Give opportunity to implementers to run shared code
				OnRenderDependent();

				// Effectively render the content to native
				NativeRenderDependent();
			}
		}

		private protected virtual void OnRenderDependent() { }
		partial void NativeRenderDependent();
	}

	[Flags]
	internal enum CompositionPropertyType
	{
		None = 0,

		//LocalIndependent = 1 << 1,
		//ChildIndependent = 1 << 2,
		Independent = 1,

		//LocalDependent = 1 << 4,
		//ChildDependent = 1 << 5,
		Dependent = 2,
	}

	internal static class VisualDirtyStateHelper
	{
		public static CompositionPropertyType Invalidate(ref int dirtyState, CompositionPropertyType kind)
		{
			var k = (int)kind;

			int current, updated;
			do
			{
				current = dirtyState;
				updated = current | k;
				if (current == updated)
				{
					return CompositionPropertyType.None;
				}
			} while (Interlocked.CompareExchange(ref dirtyState, updated, current) != current);

			return (CompositionPropertyType)~current & kind;
		}

		//public static void Commit<TFields>(
		//	ref TFields fields, ref int dirtyState,
		//	ref TFields renderFields, ref int renderDirtyState)
		//{
		//	renderFields = fields;

		//	// We copy the dirty state only after having commit fields,
		//	// so it avoid concurrency issue with property setters that are setting the field before invalidating the visual.
		//	// Note: This method might be invoked more than once per commit, so we have to append the dirty state, not overriding it.
		//	//		 This also had an additional layer of protection against concurrency issue.
		//	renderDirtyState |= Interlocked.Exchange(ref dirtyState, (int)VisualDirtyState.None);
		//}

		public static bool Reset(ref int dirtyState, CompositionPropertyType kind)
		{
			var k = ~(int)kind;

			int current, updated;
			do
			{
				current = dirtyState;
				updated = current & k;
				if (current == updated)
				{
					return false;
				}
			} while (Interlocked.CompareExchange(ref dirtyState, updated, current) != current);

			return true;
		}
	}

	internal enum VisualKind
	{
		/// <summary>
		/// Unknown views (e.g. a Map) are considered as IndependentNativeView (i.e. the worst case),
		/// as we cannot assume on how they are drawing themselves.
		/// </summary>
		UnknownNativeView = 0,

		/// <summary>
		/// ** This is the worst case **
		/// The view is known to have its own drawing logic which is independent to its size.
		/// This might be for a control that runs native animations (like ProgressRing, Button with native style, etc.),
		/// or which has is own drawing logic like a WebView.
		/// </summary>
		NativeIndependent = 1,

		/// <summary>
		/// A native view (which renders itself in the [On]Draw methods) but which is known to require a redraw
		/// only when its view is (re-)arranged (e.g. a TextBlock).
		/// </summary>
		NativeDependent = 2,

		/// <summary>
		/// A view which is backed by a Visual (e.g. Shape)
		/// </summary>
		ManagedVisual = 3,

		///// <summary>
		///// A view backed by a Visual which is currently animated.
		///// </summary>
		//IndependentAnimation
	}

	//internal static class VisualExtensions
	//{
	//	public static void SetBounds(this Visual visual, Rect bounds)
	//	{
	//		visual.Offset = new Vector3((float)bounds.X, (float)bounds.Y, 0);
	//		visual.Size = new Vector2((float)bounds.Width, (float)bounds.Height);
	//	}
	//}
}
