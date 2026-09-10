// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml/xcp/dxaml/lib/PropertyPathParser.h, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DirectUI;

partial class PropertyPathParser
{
	// Uno: indexed access projects the native begin/end/size interface over managed storage.
	public int DescriptorCount => m_descriptorCount;

	public PropertyPathStepDescriptor GetDescriptorAt(int index)
	{
		if ((uint)index >= (uint)m_descriptorCount)
		{
			throw new ArgumentOutOfRangeException(nameof(index));
		}

		return index < InlineDescriptorCapacity
			? m_inlineDescriptors[index]
			: m_overflowDescriptors![index - InlineDescriptorCapacity];
	}

	// Uno: managed inline slots and an overflow list replace WinUI's tagged native heap pointer.
	private const int InlineDescriptorCapacity = 2;

	[InlineArray(InlineDescriptorCapacity)]
	private struct InlineDescriptorBuffer
	{
		private PropertyPathStepDescriptor _element0;
	}

	private InlineDescriptorBuffer m_inlineDescriptors;
	private List<PropertyPathStepDescriptor>? m_overflowDescriptors;
	private int m_descriptorCount;
}
