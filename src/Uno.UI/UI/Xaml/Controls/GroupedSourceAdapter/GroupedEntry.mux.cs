// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\GroupedSourceAdapter\GroupedEntry.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class GroupedEntry
{
	public GroupedEntry(
		object? group,
		int groupItemCount,
		bool isExpanded)
	{
		m_group = group;
		m_groupItemCount = groupItemCount;
		m_isExpanded = isExpanded;
	}

	public partial object? Group() => m_group;

	public partial int GroupItemCount() => m_groupItemCount;

	public partial bool IsExpanded() => m_isExpanded;
}
