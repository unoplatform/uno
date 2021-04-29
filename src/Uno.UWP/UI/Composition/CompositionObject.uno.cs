#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Java.Util;

namespace Windows.UI.Composition
{
	partial class CompositionObject
	{
		private /*CompositionPropertyType*/ int _pendingDirtyState;
		private /*CompositionPropertyType*/ int _commitedDirtyState;
		private List<CompositionObject>? _listeners;

		internal CompositionPropertyType PendingDirtyState => (CompositionPropertyType)_pendingDirtyState;
		internal CompositionPropertyType CommittedDirtyState => (CompositionPropertyType)_commitedDirtyState;

		/// <summary>
		/// Register a dependent <see cref="CompositionObject"/> on which invalidate request has to be propagated.
		/// </summary>
		internal void Subscribe(CompositionObject subscriber)
		{
			if (_listeners is null)
			{
				Interlocked.CompareExchange(ref _listeners, new List<CompositionObject>(), null);
			}

			lock (_listeners)
			{
				_listeners.Add(subscriber);
			}

			ShareDirtyStateWith(subscriber);
		}

		/// <summary>
		/// Remove a dependent <see cref="CompositionObject"/>.
		/// </summary>
		internal void Unsubscribe(CompositionObject subscriber)
		{
			if (_listeners is { } listeners)
			{
				lock (listeners)
				{
					listeners.Remove(subscriber);
				}
			}
		}

		private protected void ShareDirtyStateWith(CompositionObject dependent)
		{
			dependent.Invalidate((CompositionPropertyType)_pendingDirtyState);

			CheckParentsPendingDirtyState();
		}

		/// <summary>
		/// Invalidates pending values (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// This method is not intended to be used externally.
		/// It should be used only by implementers and nested composition objects.
		/// </remarks>
		/// <param name="kind">One or more kind of invalidated property.</param>
		private protected void Invalidate(CompositionPropertyType kind)
		{
			var invalidatedKind = VisualDirtyStateHelper.Invalidate(ref _pendingDirtyState, kind);
			if (invalidatedKind != CompositionPropertyType.None)
			{
				// Note: For Visual.Parent, we prefer to use the virtual OnInvalidated to avoid creation of _listeners list,
				//		 but it's basically the same thing than register the Parent in the _listeners.
				OnInvalidated(invalidatedKind);

				if (_listeners is { } listeners)
				{
					lock (listeners)
					{
						foreach (var listener in listeners)
						{
							listener.Invalidate(invalidatedKind);
						}
					}
				}
			}

			CheckParentsPendingDirtyState();
		}

		private protected virtual void OnInvalidated(CompositionPropertyType kind) { }

		[Conditional("DEBUG")]
		internal void CheckParentsPendingDirtyState()
		{
			var co = this;
			while (co is Visual visual && (co = visual.Parent) is { })
			{
				if ((co._pendingDirtyState & _pendingDirtyState) != _pendingDirtyState)
				{
					global::System.Diagnostics.Debug.Fail(
						$"Invalid dirty state of '{co.Comment}' "
						+ $"(expected to have flags '{(CompositionPropertyType)_pendingDirtyState}' but has only '{(CompositionPropertyType)co._pendingDirtyState}')");
				}
			}
		}

		[Conditional("DEBUG")]
		internal void CheckChildrenPendingDirtyState()
		{
			DoCheck(this, this);

			void DoCheck(CompositionObject parent, CompositionObject element)
			{
				if ((parent._pendingDirtyState & element._pendingDirtyState) != element._pendingDirtyState)
				{
					global::System.Diagnostics.Debug.Fail(
						$"Invalid dirty state of '{parent.Comment}' "
						+ $"(expected to have flags '{(CompositionPropertyType)element._pendingDirtyState}' but has only '{(CompositionPropertyType)parent._pendingDirtyState}')");
				}

				if (element is ContainerVisual container)
				{
					foreach (var child in container.Children)
					{
						DoCheck(element, child);
					}
				}
			}
		}

		/// <summary>
		/// WARNING: This method is most probably NOT what you want!
		/// This is an **internal method for the composition engine**, it's the first step of the composition. <br />
		/// 
		/// Commits the pending changes of this visual and its children into the rendering engine.
		/// </summary>
		/// <remarks>This has to be invoked from the UI thread and is expected to be incredibly fast.</remarks>
		/// <remarks>This method is mutually-excluded with the <see cref="Render"/>.</remarks>
		internal void Commit()
		{
			if (_pendingDirtyState == (int)CompositionPropertyType.None)
			{
				return;
			}

			// Request to implementors to commit their own values
			OnCommit();

			// We copy the dirty state only after having committed fields,
			// so it avoids concurrency issue with property setters that are setting the field before invalidating the object.
			// Note: Fro safety we prefer to append the dirty state instead of overriding it,
			//		 this adds an additional layer of protection against concurrency issue.
			_commitedDirtyState |= Interlocked.Exchange(ref _pendingDirtyState, (int)CompositionPropertyType.None);
		}

		private protected virtual void OnCommit() { }

		/// <summary>
		/// Removes a kind of the committed dirty state
		/// </summary>
		private protected bool Reset(CompositionPropertyType kind)
			=> VisualDirtyStateHelper.Reset(ref _commitedDirtyState, kind);
	}
}
