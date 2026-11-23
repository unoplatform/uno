// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlBinaryMetadata.h, commit fee1e8acc

#nullable enable

namespace Uno.UI.Xaml.Xbf;

internal enum PersistedConstantType : byte
{
	None = 0,
	IsBoolFalse,
	IsBoolTrue,
	IsFloat,
	IsSigned,
	IsSharedString,
	IsThickness,
	IsGridLength,
	IsColor,
	IsUniqueString,
	IsNullString,
	IsEnum,

	// Uno specific: WinUI's core stores doubles as floats and resolves types and
	// properties through its metadata tables, so it has no need for these.

	// 8 bytes, for the values a float cannot hold
	UnoIsDouble = 64,
	// 4 floats
	UnoIsCornerRadius = 65,
	// [TypeId]
	UnoIsType = 66,
	// [ConstantId] : value computed by generated code
	UnoIsGenerated = 67,
	UnoIsNull = 68,
	// [PropertyId]
	UnoIsDependencyProperty = 69,
	// 8 bytes
	UnoIsInt64 = 70,
	// 4 bytes, boxed as a float
	UnoIsSingle = 71,
}

internal static class XbfConstants
{
	// "XBF\0", as in XamlBinaryFormatCommon.h
	internal const uint Magic = 0x00464258;

	internal const int MajorFileVersion = 2;
	internal const int MinorFileVersion = 1;

	internal const int HeaderSize = 24;

	internal const byte TemplateKindControl = 0;
	internal const byte TemplateKindData = 1;
	internal const byte TemplateKindItemsPanel = 2;
}
