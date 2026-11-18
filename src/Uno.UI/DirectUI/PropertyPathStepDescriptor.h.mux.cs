// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml/xcp/dxaml/lib/PropertyPathStepDescriptor.h

#nullable enable

using Microsoft.UI.Xaml;

namespace DirectUI;

internal enum PropertyPathStepDescriptorKind : byte
{
	None = 0,
	SourceAccess,
	PropertyAccess,
	IntIndexer,
	StringIndexer,
	DependencyProperty,
}

readonly partial struct PropertyPathStepDescriptor
{
	// Uno: managed references replace the native owning union; overflow belongs to the parser.
	private readonly object? _payload;
	private readonly int _index;

	public PropertyPathStepDescriptorKind Kind { get; }

	public string? Name => _payload as string;

	public int Index => _index;

	public DependencyProperty? Property => _payload as DependencyProperty;
}
