#nullable enable

using System;
using Uno;
using Windows.Foundation;

namespace Microsoft.UI.Composition
{
	public partial class CompositionScopedBatch : global::Microsoft.UI.Composition.CompositionObject
	{
		internal CompositionScopedBatch() => throw new NotSupportedException("Use the ctor with Compositor");

		internal CompositionScopedBatch(Compositor compositor, CompositionBatchTypes batchType) : base(compositor)
		{
			BatchType = batchType;
			IsActive = true;
		}

		public bool IsActive { get; private set; }

		public bool IsEnded { get; private set; }

		internal CompositionBatchTypes BatchType { get; }

		// Uno doesn't currently track individual composition animations grouped into a scoped batch,
		// so we approximate the WinUI semantics by raising Completed synchronously when End() is called
		// for non-infinite batches. This is sufficient for AnimatedVisualPlayer's PlayAsync flow which
		// uses the batch as a fence to know when an animation has finished.
		public void End()
		{
			if (IsEnded)
			{
				return;
			}

			IsEnded = true;

			if ((BatchType & CompositionBatchTypes.InfiniteAnimation) == 0)
			{
				Completed?.Invoke(this, new CompositionBatchCompletedEventArgs());
			}
		}

		public void Resume() => IsActive = true;

		public void Suspend() => IsActive = false;

		internal void CompleteByObserver()
		{
			if (IsEnded)
			{
				return;
			}

			IsEnded = true;
			Completed?.Invoke(this, new CompositionBatchCompletedEventArgs());
		}

		public event TypedEventHandler<object, global::Microsoft.UI.Composition.CompositionBatchCompletedEventArgs>? Completed;
	}
}
