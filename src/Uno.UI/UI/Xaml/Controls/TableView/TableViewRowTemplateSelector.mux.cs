// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewRowTemplateSelector.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Markup;

using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Activation factory registration lives in
// controls/dev/Generated/TableViewRowTemplateSelector.properties.cpp, as for the other
// runtimeclasses here.

partial class TableViewRowTemplateSelector
{
	// TODO Uno: C++ uses the defaulted ctor. The container types are only reached through
	// XamlReader.Load markup, so keep their constructors alive under trimming.
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TableViewRow))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(TableViewGroupHeader))]
	public TableViewRowTemplateSelector()
	{
	}

	internal void SetOwningTableViewInternal(TableView owner)
	{
		m_owningTableView = new WeakReference<TableView>(owner);
	}

	private DataTemplate? ResolveTemplateFromMarkup(string markup)
	{
		try
		{
			return XamlReader.Load(markup) as DataTemplate;
		}
		catch (Exception)
		{
			// Markup is constant, so failure means the Tabular metadata provider could not resolve
			// the container type. Assert rather than surface it later as an opaque
			// "Null encountered as data template" from ItemTemplateWrapper.
			MUX_ASSERT(false);
			return null;
		}
	}

	private void EnsureTemplates()
	{
		if (m_templatesResolved)
		{
			return;
		}

		m_rowTemplate = ResolveTemplateFromMarkup(s_rowContainerMarkup);
		m_groupHeaderTemplate = ResolveTemplateFromMarkup(s_groupHeaderContainerMarkup);

		// Latch only once both exist: latching on entry would make one failure permanent and leave
		// the table empty for the life of the control.
		m_templatesResolved = m_rowTemplate != null && m_groupHeaderTemplate != null;
		MUX_ASSERT(m_templatesResolved);
	}

	internal void Detach()
	{
		// Breaks the template -> pool edge; see the header for why this cannot be a destructor.
		if (m_rowTemplate is not null)
		{
			RecyclePool.SetPoolInstance(m_rowTemplate, null);
		}

		if (m_groupHeaderTemplate is not null)
		{
			RecyclePool.SetPoolInstance(m_groupHeaderTemplate, null);
		}

		m_rowTemplate = null;
		m_groupHeaderTemplate = null;
		m_templatesResolved = false;
		m_owningTableView = null;
	}

	protected override DataTemplate? SelectTemplateCore(object item)
	{
		EnsureTemplates();

		var kind = TableViewRowKind.Data;
		if (m_owningTableView is not null && m_owningTableView.TryGetTarget(out var owner))
		{
			kind = owner.GetRowKindForItem(item);
		}

		return kind == TableViewRowKind.GroupHeader ? m_groupHeaderTemplate : m_rowTemplate;
	}

	protected override DataTemplate? SelectTemplateCore(
		object item,
		DependencyObject container)
	{
		// Container identity cannot change the answer.
		return SelectTemplateCore(item);
	}
}
