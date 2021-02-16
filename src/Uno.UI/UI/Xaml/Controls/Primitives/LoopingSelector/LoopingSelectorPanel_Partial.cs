// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls.Primitives
{

	public partial class LoopingSelectorPanel
	{

		LoopingSelectorPanel()
		{
			_snapPointOffset = 0.0f;
			_snapPointSpacing = 0.0f;
		}


		void InitializeImpl()
		{

			wrl.ComPtr<xaml_controls.ICanvasFactory> spInnerFactory;
			wrl.ComPtr<xaml_controls.ICanvas> spInnerInstance;
			wrl.ComPtr<DependencyObject> spInnerInspectable;

			LoopingSelectorPanelGenerated.InitializeImpl();
			(wf.GetActivationFactory(
				wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_Canvas),
				&spInnerFactory));

			(spInnerFactory.CreateInstance(
				(DependencyObject)((ILoopingSelectorPanel)(this)),
				&spInnerInspectable,
				&spInnerInstance));

			(SetComposableBasePointers(
				spInnerInspectable,
				spInnerFactory));
		}

		//void get_AreHorizontalSnapPointsRegularImpl(out boolean pValue)
		//{
		//	pValue = true;
		//	return S_OK;
		//}
		internal bool AreHorizontalSnapPointsRegularImpl => true;

		//void get_AreVerticalSnapPointsRegularImpl(out boolean pValue)
		//{
		//	pValue = true;
		//	return S_OK;
		//}
		internal bool AreVerticalSnapPointsRegularImp => true;

		void GetIrregularSnapPointsImpl(Orientation orientation, SnapPointsAlignment alignment, out IList<float> returnValue)
		{
			// NOTE: This method should never be called, both
			// horizontal and vertical SnapPoints are ALWAYS regular.
			//UNREFERENCED_PARAMETER(orientation);
			//UNREFERENCED_PARAMETER(alignment);
			//UNREFERENCED_PARAMETER(returnValue);
			//return E_NOTIMPL;
			throw new NotImplementedException();
		}


		void GetRegularSnapPointsImpl(Orientation orientation, SnapPointsAlignment alignment, out float offset, out float returnValue)
		{

			// For now the LoopingSelectorPanel will simply return a evenly spaced grid,
			// the vertical and horizontal snap points will be identical.
			//UNREFERENCED_PARAMETER(orientation);
			//UNREFERENCED_PARAMETER(alignment);

			//if (offset == null) throw new ArgumentNullException();
			//if (returnValue == null) throw new ArgumentNullException();

			offset = _snapPointOffset;
			returnValue = _snapPointSpacing;

			// Cleanup
			// return hr;
		}


		void SetOffsetInPixels(float offset)
		{
			if (_snapPointOffset != offset)
			{
				_snapPointOffset = offset;
				return RaiseSnapPointsChangedEvents();
			}
			return;
		}


		void SetSizeInPixels(float size)
		{
			if (_snapPointSpacing != size)
			{
				_snapPointSpacing = size;
				return RaiseSnapPointsChangedEvents();
			}
			return;
		}


		void RaiseSnapPointsChangedEvents()
		{


			wrl.ComPtr<DependencyObject> spThisAsInspectable;

			(QueryInterface(
				__uuidof(DependencyObject),
				&spThisAsInspectable));

			(_snapPointsChangedEventSource.InvokeAll(
				spThisAsInspectable,
				spThisAsInspectable));

			// Cleanup
			// return hr;
		}

	}
} } } } XAML_ABI_NAMESPACE_END
