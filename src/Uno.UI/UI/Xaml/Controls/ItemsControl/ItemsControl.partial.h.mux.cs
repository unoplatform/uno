// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsControl_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

using DirectUI;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemsControl : IContainerRecyclingContext
{
	// special value to workaround the async nature of the loaded event.
	// layoutticks are 16 bits wide, so 32_max will never be reached.
	private const int loadCounterToken = int.MaxValue;

	// TODO Uno: C# fields can't reference `this` in an initializer, so the context is created on first use.
	private DataTemplateSelectorRecyclingContext m_dataSelectorRecyclingContext;

	// Used by ThemeTransitionContext to save previous TransitionContext
	private ThemeTransitionContext m_themeTransitionContext = ThemeTransitionContext.Entrance;

	// Used by ThemeTransitionContext to compute Added elements count
	private uint m_elementCountAddedThisLayoutTick;

	// Used by ThemeTransitionContext to compute Removed elements count
	private uint m_elementCountRemovedThisLayoutTick;

	// Used by ThemeTransitionContext to save previous TickCounter
	private int m_previousTickCounterId;

	// Used by ThemeTransitionContext to save Loaded Tick
	private int m_loadedTick = loadCounterToken;

	// Used by ThemeTransitionContext to find whether context is changed or not
	private bool m_resetItemsThisLayoutTick;

	// Used by ThemeTransitionContext to compute Reordered elements count
	private int m_elementCountReorderedThisLayoutTick;
}
