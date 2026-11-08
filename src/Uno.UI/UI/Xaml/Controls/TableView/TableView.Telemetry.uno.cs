// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableView_Telemetry.cpp, tag winui3/main, commit 79209e041

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableView
{
	// TableView_Instance's two sample points, Loaded and Unloaded.
	// TODO Uno: Telemetry and TiP are Microsoft-only, intentionally no-op.
	private void SampleInstanceTelemetry() => _ = (m_templateIncomplete, m_telemetrySourceKinds);
}
