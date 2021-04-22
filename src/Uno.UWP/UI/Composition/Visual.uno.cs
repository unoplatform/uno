using System;
using System.Threading;

namespace Windows.UI.Composition
{
	public partial class Visual
	{
		private int _dirtyState;

		//internal bool IsDirty => DirtyState > VisualDirtyState.None;

		internal VisualDirtyState DirtyState => (VisualDirtyState)_dirtyState;

		internal void Invalidate(VisualDirtyState kind)
		{
			if (VisualDirtyStateHelper.Invalidate(ref _dirtyState, kind))
			{

			}

			{
				if (Parent is { } parent)
				{
					parent.Invalidate(kind);
				}
				else
				{
					Compositor.InvalidateRoot(this, kind);
				}
			}
		}

		private bool IsDirty(VisualDirtyState kind)
			=> (_dirtyState & (int)kind) != 0;

		private void Reset(VisualDirtyState kind)
			=> VisualDirtyStateHelper.Reset(ref _dirtyState, kind);

		//private void ClearDirtyState()
		//{
		//	_dirtyState = VisualDirtyState.None;
		//}

		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine**, it's the first step of the composition. <br />
		/// 
		/// Commits the pending changes of this visual and its children into the rendering engine.
		/// </summary>
		/// <remarks>This has to be invoked from the UI thread and is expected to be incredibly fast.</remarks>
		/// <remarks>This method is mutually-excluded with the <see cref="Render"/>.</remarks>
		internal virtual void Commit()
		{
			_render = _ui;
		}

		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine**, it's the second and final step of the composition. <br />
		/// 
		/// Natively renders this visual and its children.
		/// </summary>
		/// <remarks>If the platform has a compositor thread, this is expected to be invoked on that thread.</remarks>
		/// <remarks>This method is mutually-excluded with the <see cref="Commit"/>.</remarks>
		internal virtual void Render()
		{
			RenderPartial();
		}

		partial void RenderPartial();
	}

	[Flags]
	internal enum VisualDirtyState
	{
		None = 0,

		Independent = 1,

		Dependent = 2,

		//Native = -1,
	}

	internal static class VisualDirtyStateHelper
	{
		public static bool Invalidate(ref int dirtyState, VisualDirtyState kind)
		{
			var k = (int)kind;

			int current, updated;
			do
			{
				current = dirtyState;
				updated = current | k;
				if (current == updated)
				{
					return false;
				}
			} while (Interlocked.CompareExchange(ref dirtyState, updated, current) != current);

			return true;
		}

		public static void Reset(ref int dirtyState, VisualDirtyState kind)
		{
			var k = ~(int)kind;

			int current, updated;
			do
			{
				current = dirtyState;
				updated = current & k;
				if (current == updated)
				{
					return;
				}
			} while (Interlocked.CompareExchange(ref dirtyState, updated, current) != current);
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
}
