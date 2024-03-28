// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//  Abstract:
//      LoopingSelectorPanel is designed to be used exclusively within a LoopingSelector control as
//      the content of the ScrollViewer. It allows elements to be abritrarily positioned on a Canvas
//      while

using System;

namespace Windows.UI.Xaml.Controls.Primitives
{
	partial class LoopingSelectorPanel : Canvas, IScrollSnapPointsInfo
	{

		// public
		//LoopingSelectorPanel();

		// void OnPropertyChanged(
		//     xaml.IDependencyPropertyChangedEventArgs pArgs);

		// void SetOffsetInPixels( FLOAT offset);
		// void SetSizeInPixels( FLOAT size);

		// private
		//~LoopingSelectorPanel();

		// void InitializeImpl() override;

		// public
		// Implementation of IScrollSnapPointsInfo
		//void get_AreHorizontalSnapPointsRegularImpl(out boolean pValue) override;
		//void get_AreVerticalSnapPointsRegularImpl(out boolean pValue) override;

		//void GetIrregularSnapPointsImpl(
		//    xaml_controls.Orientation orientation,
		//    xaml_primitives.SnapPointsAlignment alignment,
		//   out  wfc.IVectorView<FLOAT> returnValue);

		//void GetRegularSnapPointsImpl(
		//    xaml_controls.Orientation orientation,
		//    xaml_primitives.SnapPointsAlignment alignment,
		//   out FLOAT offset,
		//   out FLOAT returnValue);

		// private
		private float _snapPointOffset;
		private float _snapPointSpacing;

		//EventHandler<DependencyObject> _snapPointsChangedEventSource;

#pragma warning disable CS0067
		public event EventHandler<object> HorizontalSnapPointsChanged;
		public event EventHandler<object> VerticalSnapPointsChanged;

		//void RaiseSnapPointsChangedEvents();
	}
}
