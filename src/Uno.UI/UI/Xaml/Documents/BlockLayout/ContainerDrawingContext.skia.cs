// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ContainerDrawingContext.h, ContainerDrawingContext.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.BlockLayout;

// Proxy drawing context for a ContainerNode. On Uno it is a thin transform holder
// (see DrawingContext) since rendering is performed by the Paint walk over the
// children's ParsedText, not by recorded render commands.
internal sealed class ContainerDrawingContext : DrawingContext
{
	private readonly ContainerNode m_pNode;

	public ContainerDrawingContext(ContainerNode pNode)
	{
		m_pNode = pNode;
	}

	public override void InvalidateRenderCache()
	{
	}

	public override void CleanupRealizations()
	{
	}
}
