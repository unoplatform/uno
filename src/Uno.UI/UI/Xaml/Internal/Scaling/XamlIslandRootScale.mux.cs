// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\scaling\XamlIslandRootScale.cpp, tag winui3/release/1.5.1

#nullable enable

using System;
using System.ComponentModel.DataAnnotations;

namespace Uno.UI.Xaml.Core.Scaling;

internal class XamlIslandRootScale : RootScale
{
	public XamlIslandRootScale(CoreServices coreServices, VisualTree visualTree) :
		base(RootScaleConfig.ParentApply, coreServices, visualTree)
	{
	}

	protected override void ApplyScaleProtected(bool scaleChanged)
	{
		var visualTree = VisualTree;
		var rootElement = visualTree.RootElement;
		if (rootElement is not null && scaleChanged)
		{
			rootElement.SetEntireSubtreeDirty();
		}

		//const auto connectedAnimationRoot = visualTree->GetConnectedAnimationRoot();
		//if (connectedAnimationRoot)
		//{
		//	// Plateau scale has been applied on the content, and we need to cancel it here,
		//	// because the snapshots are created using the pixel size including the plateau scale,
		//	// and we don't want double scaling.
		//	CValue inverseScaleTransform;
		//	const float scale = 1.0f / GetSystemScale();
		//	CREATEPARAMETERS cp(m_pCoreServices);
		//	CValue value;
		//	{
		//		xref_ptr<CDependencyObject> matrix;
		//		IFC_RETURN(CMatrix::Create(matrix.ReleaseAndGetAddressOf(), &cp));
		//		value.SetFloat(scale);
		//		IFC_RETURN(matrix.get()->SetValueByKnownIndex(KnownPropertyIndex::Matrix_M11, value));
		//		IFC_RETURN(matrix.get()->SetValueByKnownIndex(KnownPropertyIndex::Matrix_M22, value));
		//		{
		//			xref_ptr<CDependencyObject> matrixTransform;
		//			IFC_RETURN(CMatrixTransform::Create(matrixTransform.ReleaseAndGetAddressOf(), &cp));
		//			value.WrapObjectNoRef(matrix.get());
		//			IFC_RETURN(matrixTransform.get()->SetValueByKnownIndex(KnownPropertyIndex::MatrixTransform_Matrix, value));
		//			inverseScaleTransform.SetObjectAddRef(matrixTransform.get());
		//		}
		//	}

		//	IFC_RETURN(connectedAnimationRoot->SetValueByKnownIndex(KnownPropertyIndex::UIElement_RenderTransform, inverseScaleTransform));
		//}

		//if (scaleChanged)
		//{
		//	int scalePercentage = Math.Round(GetEffectiveRasterizationScale() * 100.0f, 0);
		//	// Update the scale factor on the resource manager.
		//	xref_ptr<IPALResourceManager> resourceManager;
		//	IFC_RETURN(m_pCoreServices->GetResourceManager(resourceManager.ReleaseAndGetAddressOf()));
		//	IFC_RETURN(resourceManager->SetScaleFactor(scalePercentage));

		//	// Flush the XAML parser cache. If the application to reload XAML after a scale change, we want to
		//	// re-query MRT for the XAML resource, potentially picking up a new resource for the new scale.
		//	std::shared_ptr<XamlNodeStreamCacheManager> spXamlNodeStreamCacheManager;
		//	IFC_RETURN(m_pCoreServices->GetXamlNodeStreamCacheManager(spXamlNodeStreamCacheManager));
		//	spXamlNodeStreamCacheManager->Flush();
		//}
	}
}
