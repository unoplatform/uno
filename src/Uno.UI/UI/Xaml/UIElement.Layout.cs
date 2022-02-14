#if __WASM__ || __SKIA__
#define SUPPORTS_MANAGED_MEASURE_DIRTY_PATH
#endif
using System;
using System.Runtime.CompilerServices;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		[Flags]
		internal enum LayoutFlag : byte
		{
			/// <summary>
			/// Means the Measure is dirty for the current element
			/// </summary>
			MeasureDirty = 0b0000_0001,

#if SUPPORTS_MANAGED_MEASURE_DIRTY_PATH
			/// <summary>
			/// Means the Measure is dirty on at least one child of this element
			/// </summary>
			MeasureDirtyPath = 0b0000_0010,
#endif

			/// <summary>
			/// Indicated the first measure has been done on the element after been connected to parent
			/// </summary>
			FirstMeasureDone = 0b0000_0100,

			/// <summary>
			/// Means the MeasureDirtyPath is disabled on this element.
			/// </summary>
			/// <remarks>
			/// This flag is copied to children when they are attached, but can be re-enabled afterwards.
			/// This flag is used during invalidation
			/// </remarks>
			MeasureDirtyPathDisabled = 0b0000_1000,

			/// <summary>
			/// Means the Arrange is dirty on the current element or one of its child
			/// </summary>
			ArrangeDirty = 0b0001_0000,

			// ArrangeDirtyPath not implemented yet
		}

		private const LayoutFlag DEFAULT_STARTING_LAYOUTFLAGS = 0;
		private const LayoutFlag LAYOUT_FLAGS_TO_CLEAR_ON_RESET =
			LayoutFlag.MeasureDirty |
#if SUPPORTS_MANAGED_MEASURE_DIRTY_PATH
			LayoutFlag.MeasureDirtyPath |
#endif
			LayoutFlag.FirstMeasureDone |
			LayoutFlag.ArrangeDirty;

		private LayoutFlag _layoutFlags = DEFAULT_STARTING_LAYOUTFLAGS;

		/// <summary>
		/// Check for one specific layout flag
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool IsLayoutFlagSet(LayoutFlag flag) => (_layoutFlags & flag) == flag;

		/// <summary>
		/// Check that at least one of the specified flags is set
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool IsAnyLayoutFlagsSet(LayoutFlag flags) => (_layoutFlags & flags) != 0;

		/// <summary>
		/// Set one or many flags (set to 1)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetLayoutFlags(LayoutFlag flags) => _layoutFlags |= flags;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetLayoutFlags(LayoutFlag flags, bool state)
		{
			if (state)
			{
				SetLayoutFlags(flags);
			}
			else
			{
				ClearLayoutFlags(flags);
			}
		}

		/// <summary>
		/// Reset one or many flags (set flag to zero)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ClearLayoutFlags(LayoutFlag flags) => _layoutFlags &= ~flags;

		/// <summary>
		/// Reset flags to original state
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ResetLayoutFlags() => ClearLayoutFlags(LAYOUT_FLAGS_TO_CLEAR_ON_RESET);

		internal bool IsMeasureDirtyPathDisabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.MeasureDirtyPathDisabled);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => SetLayoutFlags(LayoutFlag.MeasureDirtyPathDisabled, value);
		}


	}
}
