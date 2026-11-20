// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference NameScopeRoot.h, commit fc2f82117

#nullable enable

namespace Uno.UI.Xaml.Core.NameScoping;

/// <summary>
/// Selects which of an owner's two possible namescope tables a name lives in.
/// </summary>
/// <remarks>
/// Only <see cref="StandardNameScope"/> has a table implementation so far. The enum is present in
/// every signature from the start because the two kinds have materially different lifecycles —
/// template entries are registered once at expansion and are never touched by the Enter/Leave walk —
/// and retrofitting that asymmetry later would mean a second migration of every call site.
/// </remarks>
internal enum NameScopeType
{
	TemplateNameScope,
	StandardNameScope,
}
