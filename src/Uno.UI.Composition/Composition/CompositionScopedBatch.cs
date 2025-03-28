#nullable enable

using System;
using Uno;
using Windows.Foundation;

namespace Windows.UI.Composition
{
	[NotImplemented]
	public partial class CompositionScopedBatch : global::Windows.UI.Composition.CompositionObject
	{
		internal CompositionScopedBatch() => throw new NotSupportedException("Use the ctor with Compositor");

		internal CompositionScopedBatch(Compositor compositor, CompositionBatchTypes batchType) : base(compositor)
		{
			BatchType = batchType;
		}

		[NotImplemented]
		public bool IsActive { get; private set; }

		[NotImplemented]
		public bool IsEnded { get; private set; }

		internal CompositionBatchTypes BatchType { get; }

		[NotImplemented]
		public void End()
		{

		}

		[NotImplemented]
		public void Resume()
		{

		}

		[NotImplemented]
		public void Suspend()
		{

		}

#pragma warning disable 67 // unused member
		[NotImplemented]
		public event TypedEventHandler<object, global::Windows.UI.Composition.CompositionBatchCompletedEventArgs>? Completed;
#pragma warning restore 67 // unused member
	}
}
