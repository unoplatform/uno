// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Result.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

// A value used to describe the outcome of a function call. Used as a return value
// whenever a RichTextServices API might fail. (The C++ IFC/Cleanup macros are not
// ported; callers branch on the value directly.)
internal enum Result
{
	// The call succeeded.
	Success = 0,

	// Failure, due to lack of memory.
	OutOfMemory = -1,

	// Failure, unspecified reason.
	Unexpected = -2,

	// Failure, due to a bad input parameter.
	InvalidParameter = -3,

	// Failure, while formatting.
	FormattingError = -4,

	// Failure, method not implemented.
	NotImplemented = -5,

	// Failure, call is invalid given object's current state.
	InvalidOperation = -6,

	// Error that XAML should keep internal and not pass back to application.
	InternalError = -7,

	// C++ macro alias (RichTextServices::INTERNAL_ERROR).
	INTERNAL_ERROR = InternalError,
}
