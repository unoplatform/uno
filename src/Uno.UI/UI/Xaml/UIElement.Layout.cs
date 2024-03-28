#if __WASM__ || __SKIA__
// "Managed Measure Dirty Path" means it's the responsibility of the
// managed code (Uno) to walk the tree and do the measure phase.
#define IMPLEMENTS_MANAGED_MEASURE_DIRTY_PATH
#define IMPLEMENTS_MANAGED_ARRANGE_DIRTY_PATH
#endif
using System;
using System.Runtime.CompilerServices;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		/// <summary>
		/// Determines if InvalidateMeasure has been called
		/// </summary>
		internal bool IsMeasureDirty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.MeasureDirty);
		}

#if IMPLEMENTS_MANAGED_MEASURE_DIRTY_PATH
		internal bool IsMeasureDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.MeasureDirtyPath);
		}
#else
		// IsMeasureDirtyPath implemented in platform-specific file to
		// connect to native mechanisms.
#endif

#if IMPLEMENTS_MANAGED_MEASURE_DIRTY_PATH
		internal bool IsMeasureDirtyOrMeasureDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsAnyLayoutFlagsSet(LayoutFlag.MeasureDirty | LayoutFlag.MeasureDirtyPath);
		}
#else
		/// <summary>
		/// This is for compatibility - not implemented yet on this platform
		/// </summary>
		internal bool IsMeasureDirtyOrMeasureDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsMeasureDirty || IsMeasureDirtyPath;
		}
#endif

		/// <summary>
		/// If the first measure has been done since the control
		/// is connected to its parent
		/// </summary>
		internal bool IsFirstMeasureDone
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.FirstMeasureDone);
		}

#if !__ANDROID__
		/// <summary>
		/// Determines if InvalidateArrange has been called
		/// </summary>
		internal bool IsArrangeDirty
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.ArrangeDirty);
		}
#endif

#if IMPLEMENTS_MANAGED_ARRANGE_DIRTY_PATH
		internal bool IsArrangeDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsAnyLayoutFlagsSet(LayoutFlag.ArrangeDirtyPath);
		}

		internal bool IsArrangeDirtyOrArrangeDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsAnyLayoutFlagsSet(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath);
		}

		/// <summary>
		/// If the first arrange has been done since the control
		/// is connected to its parent
		/// </summary>
		internal bool IsFirstArrangeDone
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.FirstArrangeDone);
		}

#else
		/// <summary>
		/// This is for compatibility - not implemented yet on this platform
		/// </summary>
		internal bool IsArrangeDirtyOrArrangeDirtyPath
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsArrangeDirty || IsArrangeDirtyPath;
		}
#endif

		[Flags]
		internal enum LayoutFlag : short
		{
			/// <summary>
			/// Means the Measure is dirty for the current element
			/// </summary>
			MeasureDirty = 0b0000_0001,

#if IMPLEMENTS_MANAGED_MEASURE_DIRTY_PATH
			/// <summary>
			/// Means the Measure is dirty on at least one child of this element
			/// </summary>
			MeasureDirtyPath = 0b0000_0010,

			/// <summary>
			/// Indicates that the element is currently being measured during the Arrange cycle.
			/// </summary>
			MeasureDuringArrange = 0b0000_0001_0000_0000,

			/// <summary>
			/// Indicates that the element is currently being measured.
			/// </summary>
			MeasuringSelf = 0b0000_0010_0000_0000,
#endif

			/// <summary>
			/// Indicates that first measure has been done on the element after been connected to parent
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

#if !__ANDROID__ // On Android, it's directly connected to IsLayoutRequested
			/// <summary>
			/// Means the Arrange is dirty on the current element or one of its child
			/// </summary>
			ArrangeDirty = 0b0001_0000,
#endif

#if IMPLEMENTS_MANAGED_ARRANGE_DIRTY_PATH
			/// <summary>
			/// Means the Arrange is dirty on at least one child of this element
			/// </summary>
			ArrangeDirtyPath = 0b0010_0000,

			/// <summary>
			/// Indicates that first arrange has been done on the element and we can use the
			/// LayoutInformation.GetLayoutSlot() to get previous finalRect
			/// </summary>
			FirstArrangeDone = 0b0100_0000,
#endif

			/// <summary>
			/// Means the MeasureDirtyPath is disabled on this element.
			/// </summary>
			/// <remarks>
			/// This flag is copied to children when they are attached, but can be re-enabled afterwards.
			/// This flag is used during invalidation
			/// </remarks>
			ArrangeDirtyPathDisabled = 0b1000_0000,
		}

		private const LayoutFlag DEFAULT_STARTING_LAYOUTFLAGS = 0;
		private const LayoutFlag LAYOUT_FLAGS_TO_CLEAR_ON_RESET =
			LayoutFlag.MeasureDirty |
#if IMPLEMENTS_MANAGED_MEASURE_DIRTY_PATH
			LayoutFlag.MeasureDirtyPath |
			LayoutFlag.MeasureDuringArrange |
			LayoutFlag.MeasuringSelf |
#endif
#if !__ANDROID__
			LayoutFlag.ArrangeDirty |
#endif
#if IMPLEMENTS_MANAGED_ARRANGE_DIRTY_PATH
			LayoutFlag.ArrangeDirtyPath |
			LayoutFlag.FirstArrangeDone |
#endif
			LayoutFlag.FirstMeasureDone;

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

		internal bool IsArrangeDirtyPathDisabled
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => IsLayoutFlagSet(LayoutFlag.ArrangeDirtyPathDisabled);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => SetLayoutFlags(LayoutFlag.ArrangeDirtyPathDisabled, value);
		}
	}
}
