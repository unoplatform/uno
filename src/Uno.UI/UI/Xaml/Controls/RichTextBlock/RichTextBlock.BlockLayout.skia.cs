// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class RichTextBlock
{
	// TODO Uno (Stage 4): RichTextBlock.GetBaselineOffset — baseline offset of the formatted content
	// used to align embedded elements (CRichTextBlock::GetBaselineOffset).
	internal void GetBaselineOffset(out float pBaseline)
		=> throw new NotSupportedException("TODO Uno (Stage 4): RichTextBlock.GetBaselineOffset");
}
