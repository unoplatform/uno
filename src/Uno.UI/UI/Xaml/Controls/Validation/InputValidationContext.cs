// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference dxaml\xcp\dxaml\idl\winrt\main\microsoft.ui.xaml.private.idl, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

// Not ported. IInputValidationControl.ValidationContext is commented out alongside it: nothing reads
// MemberName, and IsInputRequired is the floated IsRequired indicator of specs/060, which needs a
// template column of its own before it means anything.
//
// /// <summary>
// /// Describes the member a control's input is validated against.
// /// </summary>
// public partial class InputValidationContext
// {
//	public InputValidationContext(string memberName, bool isRequired)
//	{
//		MemberName = memberName;
//		IsInputRequired = isRequired;
//	}
//
//	/// <summary>
//	/// Whether the member requires a value.
//	/// </summary>
//	public bool IsInputRequired { get; }
//
//	/// <summary>
//	/// The name of the member being validated.
//	/// </summary>
//	public string MemberName { get; }
// }
