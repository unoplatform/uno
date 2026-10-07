// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\SortMemberPathResolver.h, tag winui3/main, commit dc28206ea35

#nullable enable

using System;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// Evaluates a property path against a row item. A one-time {Binding} on a throwaway
// ContentControl is used rather than reflection because it is the same evaluator the cells use,
// so a path that displays also sorts - including indexers and dotted paths.
//
// Shared by both sort front-ends: TableView turns a column's SortMemberPath into a key selector,
// and TableViewSource turns the path handed to the fluent Sort verb into the same thing. One
// evaluator means the two front-ends cannot disagree about what a path means.
//
// Not thread-safe and UI-thread affine (it drives the binding engine). Reuse one instance per
// path - EnsureBinding rebinds only when the path changes, so repeated Resolve calls on the same
// path cost a DataContext write and a property read.
internal sealed class SortMemberPathResolver
{
	public SortMemberPathResolver(string sortMemberPath)
	{
		SortMemberPath = sortMemberPath;
	}

	public object? Resolve(object? item)
	{
		if (item is null || string.IsNullOrEmpty(SortMemberPath))
		{
			return null;
		}

		try
		{
			EnsureBinding();
			Probe!.DataContext = item;
			try
			{
				return Probe.Content;
			}
			finally
			{
				ClearDataContext();
			}
		}
		catch (Exception)
		{
			ClearDataContext();
			return null;
		}
	}

	private void EnsureBinding()
	{
		if (Probe is null)
		{
			Probe = new ContentControl();
		}

		if (BoundPath == SortMemberPath)
		{
			return;
		}

		ClearDataContext();
		Probe.ClearValue(ContentControl.ContentProperty);

		Binding binding = new();
		binding.Path = new PropertyPath(SortMemberPath);
		// TODO Uno: Uno's OneTime binding currently behaves as OneWay (see uno-issues
		// uno--binding-mode-onetime-behaves-as-oneway.md). The resolver still reads the right value because
		// every Resolve re-sets DataContext before reading Content.
		binding.Mode = BindingMode.OneTime;
		BindingOperations.SetBinding(
			Probe,
			ContentControl.ContentProperty,
			binding);
		BoundPath = SortMemberPath;
	}

	// Never hold the last row item alive through the probe once the key has been read.
	private void ClearDataContext()
	{
		try
		{
			if (Probe is not null)
			{
				Probe.DataContext = null;
			}
		}
		catch (Exception)
		{
		}
	}

	private string SortMemberPath;
	private string BoundPath = "";
	private ContentControl? Probe = null;
}
