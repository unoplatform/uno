// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Tip.cpp, tag winui3/main, commit 79209e041

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Tabular;

// TODO Uno: Telemetry and TiP are Microsoft-only, intentionally no-op.
partial class TableView
{
	// A synchronous action owns its own operation state (and TiP test, when the operation has one) and shares the
	// control's session counters and error mask. withTest false tracks it for TableView_Error without a TiP test.
	// TODO Uno: Scope it with `using ActionTip tip = new(this, ...);`. ActionTip* and ActionTip& both become ActionTip?.
	private sealed class ActionTip : IDisposable
	{
		public ActionTip(TableView owner, TableViewTip.Operation operation, TableViewTip.Stage stage, bool admitted = true, bool withTest = true)
		{
		}

		public void Dispose()
		{
		}

		public void Complete() { }
		public void Ignore(TableViewTip.IgnoreReason reason) { }
		public void Fail(int? error) { }
		// In a catch, before rethrowing: the exception leaving the action, so the app is blamed only when app code
		// threw it.
		public void Escaping(TableViewTip.Fault fault) { }
		// App code stopped the operation: its write did not land.
		public void ApplicationFault(TableViewTip.AppFaultKind kind, int? error = null) { }
		// App code failed (threw, or its write did not land) and TableView carried on: reported, and the
		// outcome stays TableView's.
		public void RecoveredAppFault(TableViewTip.AppFaultKind kind, int? error = null) { }
		public ulong ControlGenerationAtAdmission() => 0;
	}

	private void AttachSourceTip(TableViewSource source) { }
	private ulong BeginTipOperation(TableViewTip.Operation operation, bool admitted = true) => 0;
	private void EndTipOperation() { }
	private void SupersedeTipOperation() { }
	// App code failed (threw, or its write did not land) and TableView carried on without rethrowing.
	private void NoteRecoveredAppFault(TableViewTip.Operation operation, TableViewTip.Stage stage, TableViewTip.AppFaultKind kind, int? error) { }
	// A column's editing hook threw and TableView carried on. The hooks are TableView's code, so this fails the action
	// (or, without one, is reported as TableView's recovered failure), unless app code it ran threw (appFault): then it
	// is recovered as the app's.
	private void NoteColumnFault(ActionTip? tip, TableViewTip.Operation operation, int error, TableViewTip.AppFaultKind? appFault = null) { }
	// An app fault ignores the operation; any other failure fails it.
	private void FailTipOperation(ulong generation, TableViewTip.Stage stage, TableViewTip.Fault? fault) { }
	private void CompleteTipOperation(ulong generation) { }
	private void IgnoreTip(TableViewTip.IgnoreReason reason) { }
	// Initialize: starts once the template is applied and a source is attached, and ends at the layout pass of the
	// first realized row or group header, or at once for an empty view.
	private void StartTipInitialization() { }
	private void CompleteTipInitializationIfEmpty() { }
	// After a source is attached or reclaimed: starts Initialize once the template is applied, and completes it for
	// an empty view. Before Initialize has its verdict, a source swap is part of it rather than a ReplaceSource
	// operation.
	private void NoteTipInitializationSource() => _ = m_templateApplied;
	// Setup (Stage::Template) or the source attach (Stage::Source) threw: Initialize's failure, or ignored when app
	// code threw, whether or not the test was running yet.
	private void FailTipInitialization(TableViewTip.Stage stage, TableViewTip.Fault fault) { }
	private void ArmTipInitializationLayout() { }
	private void RevokeTipInitializationLayout() { }
	// At unload: closes a waiting Initialize as UnloadedBeforeRender when the table had rows to lay out and a viewport,
	// yet laid none out.
	private void IgnoreTipInitializationUnloadedBeforeRender() { }
}
