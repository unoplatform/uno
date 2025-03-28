// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\ContentRoot\FocusAdapter.cpp, tag winui3/release/1.5.1, commit 3d10001ba8

#nullable enable

using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal abstract class FocusAdapter
{
	protected readonly ContentRoot _contentRoot;

	public FocusAdapter(ContentRoot contentRoot)
	{
		_contentRoot = contentRoot ?? throw new System.ArgumentNullException(nameof(contentRoot));
	}

	internal abstract void SetFocus();

	internal virtual bool ShouldDepartFocus(FocusNavigationDirection direction) => false;
}
