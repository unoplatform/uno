// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RecyclePool.h, commit 4b206bce3

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

partial class RecyclePool
{
	private static DependencyProperty s_PoolInstanceProperty;
	private static DependencyProperty s_reuseKeyProperty;
	private static DependencyProperty s_originTemplateProperty;

	private struct ElementInfo
	{
		public ElementInfo(UIElement element, IPanel owner)
		{
			Element = element;
			Owner = owner;
		}

		public UIElement Element { get; }
		public IPanel Owner { get; }
	}

	// Uno specific: The C++ uses std::map (ordered) but for the access patterns (lookup by key,
	// iterate values), Dictionary provides equivalent semantics without the ordering cost.
	private readonly Dictionary<string /*key*/, List<ElementInfo>> m_elements = new Dictionary<string, List<ElementInfo>>();
}
