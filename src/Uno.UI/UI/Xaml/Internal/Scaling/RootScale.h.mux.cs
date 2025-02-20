// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\scaling\inc\RootScale.h, tag winui3/release/1.5.1

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Content;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Core.Scaling;

internal enum RootScaleConfig
{
	// Parent scale is identity or it expects the root visual tree to apply system DPI scale itself.
	ParentInvert,
	// Parent scale already applies the system DPI scale, so need to apply in the internal root visual tree.
	ParentApply,
}

partial class RootScale
{
	internal bool IsInitialized => _initialized;

	internal static float GetRasterizationScaleForContentRoot(ContentRoot? coreContextRoot)
	{
		if (GetRootScaleForContentRoot(coreContextRoot) is { } rootScale)
		{
			return rootScale.GetEffectiveRasterizationScale();
		}
		return 1.0f;
	}

	internal static float GetRasterizationScaleForElement(DependencyObject pDO)
	{
		if (GetRootScaleForElement(pDO) is { } rootScale)
		{
			return rootScale.GetEffectiveRasterizationScale();
		}
		return 1.0f;
	}

	internal static float GetRasterizationScaleForElementWithFallback(DependencyObject pDO)
	{
		var rootScale = GetRootScaleForElementWithFallback(pDO);
		if (rootScale is not null)
		{
			return rootScale.GetEffectiveRasterizationScale();
		}
		return 1.0f;
	}

	protected abstract void ApplyScaleProtected(bool scaleChanged);

	private protected VisualTree VisualTree => _visualTree;

	internal enum ScaleKind
	{
		System,
		Test,
	}

	private readonly RootScaleConfig _config;
	// The system DPI, this is accumulated scale,
	// tipically this is control by the Display Settings app.
	private float _systemScale = 1.0f;
	// Used only for testing, it replaces the system DPI scale with a value
	// This can only be used when config is RootScaleConfig::ParentInvert
	private float _testOverrideScale;
	//private readonly List<DisplayListener> _displayListeners = new();
	private readonly VisualTree _visualTree;
	private bool _initialized;
	private bool _updating;
	//private ImageReloadManager? _imageReloadManager;
	private ContentIsland? _content;  // IExpCompositionContent

	protected readonly CoreServices _coreServices;
}
