// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewTipTypes.h, tag winui3/main, commit 79209e041

#nullable enable

using System.Numerics;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// TiP and telemetry vocabulary without the TiP library, for headers that only need the names.
internal static partial class TableViewTip
{
	internal enum Operation : uint
	{
		Setup = 0, ReplaceSource = 1, Sort = 2, HeaderRefresh = 3,
		Filter = 5, GroupConfiguration = 6, GroupExpansion = 7, Selection = 8,
		BeginEdit = 9, CommitEdit = 10, CancelEdit = 11, HierarchyExpansion = 12,
		// One past the last operation: add operations above it. TableViewTelemetry.cpp checks every value below it.
		Max
	}

	internal enum IgnoreReason : ushort
	{
		Cancelled = 100, Vetoed = 101, ValidationRejected = 102, Superseded = 103,
		Unloaded = 104, Retemplated = 105, Stale = 106,
		DeferredOrUnproven = 107, ApplicationFault = 108, AppTemplateIncomplete = 109,
		// Unloaded while Initialize waited on a table that had rows and a viewport: a realization fault, or a
		// navigation faster than the first layout.
		UnloadedBeforeRender = 110,
		// One past the last reason: add reasons above it. TableViewTip.cpp checks that each one has a name.
		Max
	}

	internal enum AppFaultKind : uint { None = 0, Callback = 1, Validation = 3, Write = 4, Factory = 5, Data = 6 }

	// App delegates that threw during a verb and were contained by design (a filter predicate excludes its row, a sort
	// or group key selector yields a null key, a comparer orders the pair as equal): how many, and the first one's
	// HRESULT. TableView reports them as a recovered app fault; the verb's outcome stands.
	internal struct ContainedFaults
	{
		public uint count;
		public int firstError;
	}

	// Where a fault happened and whose it was; recorded on the TableView_Error diagnostic.
	internal enum Stage : uint
	{
		None = 0, Template = 1, Source = 3, Sort = 4, HeaderRefresh = 5,
		Selection = 6, Editing = 7, Filter = 8, Grouping = 9, Expansion = 10
	}

	internal enum Attribution : uint
	{
		Unknown = 0, Product = 1, Application = 2,
		// One past the last attribution: add attributions above it.
		Max
	}

	// Operations TableView tracks for supersession and TableView_Error. The ones with a TiP test are listed by
	// OperationTestIdentity (TableViewTipTests.h).
	internal static bool IsTrackedOperation(Operation operation)
	{
		switch (operation)
		{
			case Operation.ReplaceSource:
			case Operation.Sort:
			case Operation.Filter:
			case Operation.GroupConfiguration:
			case Operation.Selection:
			case Operation.BeginEdit:
			case Operation.CommitEdit:
			case Operation.CancelEdit:
			case Operation.GroupExpansion:
			case Operation.HierarchyExpansion:
				return true;
			default:
				return false;
		}
	}

	// Every TiP test's metrics bucket, from inputs read when the test starts: 0 LTR-small, 1 LTR-large, 2 RTL-small,
	// 3 RTL-large, where large is a source of at least c_largeDataRows rows before the filter. Each result also
	// reports the rows before and after the filter.
	internal struct MetricsInputs
	{
		public bool rightToLeft;
		public uint sourceRows;
		public uint dataRows;
	}

	internal const uint c_largeDataRows = 1000;

	internal static byte MetricsBucket(bool rightToLeft, bool largeData) =>
		(byte)((rightToLeft ? 2 : 0) + (largeData ? 1 : 0));

	// 0 for none; otherwise k for a count in [2^(k-1), 2^k). TiP results and TableView_Instance report row counts so.
	internal static byte Log2Bucket(uint count) =>
		(byte)(32 - BitOperations.LeadingZeroCount(count));
}
