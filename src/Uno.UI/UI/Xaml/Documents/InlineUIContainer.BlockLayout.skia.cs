// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using Microsoft.UI.Xaml.Documents.RichTextServices;

namespace Microsoft.UI.Xaml.Documents;

partial class InlineUIContainer
{
	// TODO Uno (Stage 4): InlineUIContainer.GetChild — the hosted UIElement child (CInlineUIContainer::GetChild).
	internal UIElement? GetChild()
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineUIContainer.GetChild");

	// TODO Uno (Stage 4): InlineUIContainer.GetCachedHost — the host this container is currently attached to.
	internal IEmbeddedElementHost? GetCachedHost()
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineUIContainer.GetCachedHost");

	// TODO Uno (Stage 4): InlineUIContainer.ClearCachedHost — forgets the cached host on detach.
	internal void ClearCachedHost()
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineUIContainer.ClearCachedHost");

	// TODO Uno (Stage 4): InlineUIContainer.EnsureAttachedToHost — attaches the child to the given host.
	internal void EnsureAttachedToHost(IEmbeddedElementHost host)
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineUIContainer.EnsureAttachedToHost");

	// TODO Uno (Stage 4): InlineUIContainer.EnsureDetachedFromHost — detaches the child from its cached host.
	internal void EnsureDetachedFromHost()
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineUIContainer.EnsureDetachedFromHost");

	// TODO Uno (Stage 4): InlineUIContainer.SetChildLayoutCache — caches the child's measured size/baseline.
	internal void SetChildLayoutCache(double width, double height, double baseline)
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineUIContainer.SetChildLayoutCache");

	// TODO Uno (Stage 4): InlineUIContainer.GetChildLayoutCache — returns the cached child size/baseline.
	internal void GetChildLayoutCache(out double width, out double height, out double baseline)
		=> throw new NotSupportedException("TODO Uno (Stage 4): InlineUIContainer.GetChildLayoutCache");
}
