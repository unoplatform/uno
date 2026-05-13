// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RecyclingElementFactory.h, commit 4b206bce3

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

partial class RecyclingElementFactory
{
	// Uno specific: WinUI stores templates in a winrt::IMap<hstring, DataTemplate>. We use
	// IDictionary<string, DataTemplate> which provides equivalent set/get semantics.
	private RecyclePool m_recyclePool;
	private IDictionary<string, DataTemplate> m_templates = new Dictionary<string, DataTemplate>();
	private SelectTemplateEventArgs m_args;
}
