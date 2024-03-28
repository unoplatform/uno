using System;

namespace Windows.UI.Composition
{
	/// <summary>
	/// Batch types for CompositionCommitBatch and CompositionScopedBatch.
	/// </summary>
	[Flags]
	public enum CompositionBatchTypes : uint
	{
		/// <summary>
		/// None.
		/// </summary>
		None = 0U,
		/// <summary>
		/// The batch contains animations.
		/// </summary>
		Animation = 1U,
		/// <summary>
		/// The batch contains effects.
		/// </summary>
		Effect = 2U,
		/// <summary>
		/// The batch contains an infinite animation.
		/// </summary>
		InfiniteAnimation = 4U,
		/// <summary>
		/// The batch contains all animations.
		/// </summary>
		AllAnimations = InfiniteAnimation | Animation
	}
}
