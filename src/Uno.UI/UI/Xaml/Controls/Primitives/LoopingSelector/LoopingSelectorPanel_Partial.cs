using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class LoopingSelectorPanel
	{
		internal LoopingSelectorPanel()
		{
			_snapPointOffset = 0.0f;
			_snapPointSpacing = 0.0f;

			InitializeImpl();
		}

		//void get_AreHorizontalSnapPointsRegularImpl(out boolean pValue)
		//{
		//	pValue = true;
		//	return S_OK;
		//}
		public bool AreHorizontalSnapPointsRegular => true;

		//void get_AreVerticalSnapPointsRegularImpl(out boolean pValue)
		//{
		//	pValue = true;
		//	return S_OK;
		//}
		public bool AreVerticalSnapPointsRegular => true;

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			// NOTE: This method should never be called, both
			// horizontal and vertical SnapPoints are ALWAYS regular.
			//UNREFERENCED_PARAMETER(orientation);
			//UNREFERENCED_PARAMETER(alignment);
			//UNREFERENCED_PARAMETER(returnValue);
			//return E_NOTIMPL;
			throw new NotImplementedException();
		}


		public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
		{
			// For now the LoopingSelectorPanel will simply return a evenly spaced grid,
			// the vertical and horizontal snap points will be identical.
			//UNREFERENCED_PARAMETER(orientation);
			//UNREFERENCED_PARAMETER(alignment);

			//if (offset == null) throw new ArgumentNullException();
			//if (returnValue == null) throw new ArgumentNullException();

			if (orientation != Orientation.Vertical) throw new InvalidOperationException();

			offset = _snapPointOffset;
			var returnValue = _snapPointSpacing;
			return returnValue;
		}


		void InitializeImpl()
		{
			//wrl.ComPtr<xaml_controls.ICanvasFactory> spInnerFactory;
			//wrl.ComPtr<xaml_controls.ICanvas> spInnerInstance;
			//wrl.ComPtr<DependencyObject> spInnerInspectable;

			//LoopingSelectorPanelGenerated.InitializeImpl();
			//(wf.GetActivationFactory(
			//	wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_Canvas),
			//	&spInnerFactory));

			//(spInnerFactory.CreateInstance(
			//	(DependencyObject)((ILoopingSelectorPanel)(this)),
			//	&spInnerInspectable,
			//	&spInnerInstance));

			//(SetComposableBasePointers(
			//	spInnerInspectable,
			//	spInnerFactory));
		}


		internal void SetOffsetInPixels(float offset)
		{
			if (_snapPointOffset != offset)
			{
				_snapPointOffset = offset;
				RaiseSnapPointsChangedEvents();
			}
		}


		internal void SetSizeInPixels(float size)
		{
			if (_snapPointSpacing != size)
			{
				_snapPointSpacing = size;
				RaiseSnapPointsChangedEvents();
			}
		}


		void RaiseSnapPointsChangedEvents()
		{
			//DependencyObject spThisAsInspectable;

			//(QueryInterface(
			//	__uuidof(DependencyObject),
			//	&spThisAsInspectable));
			//spThisAsInspectable = this;

			//(_snapPointsChangedEventSource.InvokeAll(
			//	spThisAsInspectable,
			//	spThisAsInspectable));
			//_snapPointsChangedEventSource?.Invoke(spThisAsInspectable, spThisAsInspectable);
			VerticalSnapPointsChanged?.Invoke(this, this);
		}
	}
}
