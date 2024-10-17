// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\scaling\RootScale.cpp, tag winui3/release/1.5.1

#nullable enable

using System;
using Microsoft.UI.Content;
using Windows.UI.Xaml;
using Uno.Disposables;
using Windows.Graphics.Display;
using static Uno.UI.Xaml.Internal.Inlined;

namespace Uno.UI.Xaml.Core.Scaling;

internal abstract partial class RootScale
{
	public RootScale(RootScaleConfig config, CoreServices coreServices, VisualTree visualTree)
	{
		_config = config;
		_visualTree = visualTree;
		_coreServices = coreServices;
	}

	// TODO Uno: Implement
	//~RootScale()
	//{
	//	// It's OK to still have some displayListeners at this point.  When a test is shutting down XAML, we'll
	//	// destroy this object, but we may still have a CLoadedImageSurface object in this list.  Since we're shutting
	//	// down, there won't be new scale changes anyway.
	//	_displayListeners.Clear();
	//	_imageReloadManager.ClearImages();
	//}

	private float GetSystemScale()
	{
		float rasterizationScale = 1.0f;
		if (_content is not null)
		{
			// For CoreWindow scenarios, the CompositionContent is also listening for the CoreWindow's closed event.
			// CompositionContent will get the notification first and close the entire visual tree, then Xaml will
			// exit its message loop and tear down the tree. Since CompositionContent already closed everything,
			// Xaml will get lots of RO_E_CLOSED errors. These are all safe to ignore. So tolerate RO_E_CLOSED if
			// we're also in the middle of tearing down the tree.
			rasterizationScale = _content.RasterizationScale;
		}
		return rasterizationScale;
	}

	internal void SetContentIsland(ContentIsland? content) => _content = content;

	internal void UpdateSystemScale()
	{
		// Remove SuspendFailFastOnStowedException
		//     Bug 19696972: QueryScaleFactor silently fails at startup
		// SuspendFailFastOnStowedException raiiSuspender;
		var systemScale = GetSystemScale();
		if (systemScale != 0.0f)
		{
			SetSystemScale(systemScale);
		}
	}

	internal float GetEffectiveRasterizationScale()
	{
		if (!IsInitialized && !_updating)
		{
			UpdateSystemScale();
		}

		// A testOverrideScale of 0 means there's no override; just use the systemScale
		float effectiveScale = _testOverrideScale == 0.0f ? _systemScale : _testOverrideScale;
		return effectiveScale;
	}

	protected float GetRootVisualScale()
	{
		// In XamlOneCoreTransforms mode, there is no need to do a RenderTransform on the root, because the scale has already been
		// applied for us by the CompositionIsland. However, due to legacy reasons, our DComp tests has a dependency that, even when the scale is 1,
		// a RenderTransform is still applied on the root (Identity). To support these tests, we will always apply a scale transform on the root
		// in XamlOneCoreTransforms mode. When we've enabled XamlOneCoreTransforms mode by default, we can break this dependency and
		// update the tests to not expect an Identity RenderTransform set on the root.
		// In OneCoreTransforms mode, there's already a scale applied to XAML visuals matching the systemScale, so we factor that scale
		// out on the XAML content.
		float newRootVisualScale = 0.0f;
		float effectiveScale = GetEffectiveRasterizationScale();
		if (_config == RootScaleConfig.ParentApply)
		{
			// This is the case where we're pushing a non-identity scale into the root visual
			newRootVisualScale = effectiveScale / _systemScale;
		}
		else
		{
			newRootVisualScale = effectiveScale;
		}
		return newRootVisualScale;
	}

	internal void SetTestOverride(float scale) => SetScale(scale, ScaleKind.Test);

	private void SetSystemScale(float scale) => SetScale(scale, ScaleKind.System);

	private void SetScale(float scale, RootScale.ScaleKind kind)
	{
		_updating = true;
		using var cleanup = Disposable.Create(() =>
		{
			_updating = false;
		});

		float oldScale = GetEffectiveRasterizationScale();
		bool scaleIsValid = scale != 0.0f;
		switch (kind)
		{
			case RootScale.ScaleKind.System:
				if (scaleIsValid)
				{
					_systemScale = scale;
				}
				break;
			case RootScale.ScaleKind.Test:
				_testOverrideScale = scale;
				break;
		}
		float newScale = GetEffectiveRasterizationScale();
		bool scaleChanged = !IsCloseReal(oldScale, newScale);
		ApplyScale(scaleChanged);
		_initialized = true;
	}

	internal void ApplyScale() => ApplyScale(false);

	private void ApplyScale(bool scaleChanged)
	{
		ApplyScaleProtected(scaleChanged);

		if (scaleChanged)
		{
			// TODO Uno: Reload images on scale change!
			//foreach (var displayListener in _displayListeners)
			//{
			//	displayListener.OnScaleChanged();
			//}

			//if (IsInitialized())
			//{
			//	// Reload images.
			//	m_imageReloadManager.ReloadImages(ResourceInvalidationReason.ScaleChanged);
			//}

			VisualTree.ContentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.RasterizationScale);
		}
	}

	//private void AddDisplayListener(DisplayListener displayListener)
	//{
	//	MUX_ASSERT(!_displayListeners.Contains(displayListener));
	//	_displayListeners.Add(displayListener);
	//}

	//private void RemoveDisplayListener(DisplayListener displayListener)
	//{
	//	MUX_ASSERT(_displayListeners.Count(d => d == displayListener) == 1);
	//	_displayListeners.Remove(displayListener);
	//}

	//CImageReloadManager& RootScale::GetImageReloadManager()
	//{
	//	return m_imageReloadManager;
	//}

	private static RootScale? GetRootScaleForElement(DependencyObject pDO)
	{
		if (VisualTree.GetContentRootForElement(pDO) is { } contentRoot)
		{
			return GetRootScaleForContentRoot(contentRoot);
		}

		return null;
	}

	internal static RootScale? GetRootScaleForContentRoot(ContentRoot? contentRoot)
	{
		if (contentRoot is not null)
		{
			if (contentRoot.VisualTree is { } visualTree)
			{
				return visualTree.RootScale;
			}
		}

		return null;
	}

	private static RootScale? GetRootScaleForElementWithFallback(DependencyObject? pDO)
	{
		RootScale? result = null;
		if (pDO is not null)
		{
			result = GetRootScaleForElement(pDO);
		}

		if (result is null)
		{
			var coreServices = CoreServices.Instance; // TODO Uno: This should be DXamlServices::GetHandle()
			var contentRootCoordinator = coreServices.ContentRootCoordinator;
			if (contentRootCoordinator.CoreWindowContentRoot is { } root)
			{
				result = GetRootScaleForContentRoot(root);
			}
		}

		return result;
	}
}
