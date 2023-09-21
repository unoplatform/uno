// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Based on the implementation in https://github.com/dotnet/aspnetcore/blob/26e3dfc7f3f3a91ba445ec0f8b1598d12542fb9f/src/Components/WebAssembly/WebAssembly/src/HotReload/HotReloadAgent.cs

#nullable enable

using System;

namespace Uno.UI.RemoteControl.HotReload.MetadataUpdater;

internal static class MetadataUpdaterHelper
{
	public static event EventHandler? MetadataUpdated;

	internal static void RaiseMetadataUpdated()
	{
		MetadataUpdated?.Invoke(null, EventArgs.Empty);
	}
}
