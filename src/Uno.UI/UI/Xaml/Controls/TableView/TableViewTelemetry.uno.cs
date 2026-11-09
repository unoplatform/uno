// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewTelemetry.h, tag winui3/main, commit 79209e041

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

// TODO Uno: Telemetry and TiP are Microsoft-only, intentionally no-op.
internal static class TableViewTelemetry
{
	// TableView_Instance's EnabledFeatures and UsedFeatures bits.
	internal static class Feature
	{
		public const uint Sorting = 1u << 0;
		public const uint Editing = 1u << 1;
		public const uint Filtering = 1u << 2;
		public const uint Grouping = 1u << 3;
		public const uint Hierarchy = 1u << 4;
		public const uint LiveShaping = 1u << 5;
	}

	// TableView_Instance's ColumnKinds bits. A column whose class is not one of ours is the app's.
	internal static class ColumnKind
	{
		public const uint Text = 1u << 0;
		public const uint Template = 1u << 1;
		public const uint AppDerived = 1u << 2;
	}

	// TableView_Instance's SourceKinds bits: a plain collection TableView projected, or the app's own TableViewSource.
	internal static class SourceKind
	{
		public const uint Projected = 1u << 0;
		public const uint AppSupplied = 1u << 1;
	}

	// What the instance has shown across its loads: a row or group header (Rows, which Empty never replaces),
	// or only a source with no data rows (Empty).
	internal enum ContentShown : byte { None = 0, Rows = 1, Empty = 2 }

	// What one sample point (Loaded, Unloaded) read. A session keeps every sample's bits and the largest counts.
	internal struct Sample
	{
		public uint enabledFeatures;
		public uint columnKinds;
		public uint sourceKinds;
		// TableViewSource's rows before and after the filter, and the containers the rows repeater has created. It
		// creates one only when none is free, so this is the most rows realized at once, plus the most group headers
		// (they pool separately).
		public uint sourceRows;
		public uint dataRows;
		public uint peakRealizedRows;
		public bool rightToLeft;
		public bool templateIncomplete;
	}

	// One loaded session, written once as TableView_Instance at Unloaded or destruction.
	// TODO Uno: A class because C++ only holds it by reference. Nothing writes it, so a session is never active.
	internal sealed class Session
	{
		public ulong generation;
		public bool active;
	}

	// Whether a sample can still be written: a listener collects TableView's telemetry and the process cap
	// is not reached. Otherwise TableView skips sampling.
	internal static bool ShouldSample() => false;
	internal static void BeginSession(Session session) { }
	// Writes TableView_Instance, capped per process, and ends the session.
	internal static void EndSession(Session session) { }
	// Presence: once per process, at the first Loaded.
	internal static void ReportFirstLoad() { }
	internal static void AddSample(Session session, Sample sample) { }
	// A one-shot latch: TableView calls it for every element it realizes, so it is a single compare once Rows is seen.
	internal static void NoteContent(Session session, ContentShown content) { }
	// Use counts only into the session it happened in, and only once the instance has shown rows.
	internal static void MarkUsed(Session session, ulong startedIn, uint feature) { }

	// The feature a measured operation exercises, or 0. Editing is used by a commit, not by opening an editor.
	internal static uint FeatureOf(TableViewTip.Operation operation)
	{
		switch (operation)
		{
			case TableViewTip.Operation.Sort: return Feature.Sorting;
			case TableViewTip.Operation.Filter: return Feature.Filtering;
			case TableViewTip.Operation.CommitEdit: return Feature.Editing;
			default: return 0;
		}
	}

	// startedIn is the operation's session generation, or 0 when the report is not tied to one. faultCount is how many
	// faults the report stands for: more than one only for contained app delegate faults (TableViewTip::ContainedFaults).
	internal static void ReportError(Session session, ulong startedIn, TableViewTip.Operation operation, TableViewTip.Stage stage,
		bool recoverable, TableViewTip.Attribution attribution = TableViewTip.Attribution.Unknown,
		int? error = null, TableViewTip.AppFaultKind kind = TableViewTip.AppFaultKind.None,
		uint faultCount = 1)
	{ }
}
