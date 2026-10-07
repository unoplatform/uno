// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewTemplateColumn.h, tag winui3/main, commit dc28206ea35

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewTemplateColumn
{
	// Resolve MI ambiguity — TemplateColumn's Properties shadows Column's.
	// TODO Uno: Original C++:
	// using TableViewTemplateColumnProperties::EnsureProperties;
	// using TableViewTemplateColumnProperties::ClearProperties;
	// ForwardRefToBaseReferenceTracker(TableViewColumn)
	// C# has no multiple inheritance or reference-tracker plumbing, so there is nothing to resolve.

	// TableViewTemplateColumn();

	// Override
	// winrt::FrameworkElement GenerateElementCore(const winrt::IInspectable& dataItem) override;

	// CellTemplate routes through here rather than being special-cased by the base class, so a
	// third-party column can do the same. CellEditingTemplate is handled by the base.
	// void OnPropertyChanged(const winrt::DependencyPropertyChangedEventArgs& args);
}
