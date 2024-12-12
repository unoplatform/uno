using System;
using System.Collections.Generic;
using Uno;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial interface IScrollSnapPointsInfo
	{
		bool AreHorizontalSnapPointsRegular
		{
			get;
		}
		bool AreVerticalSnapPointsRegular
		{
			get;
		}
		IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment);
		float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset);

		event EventHandler<object> HorizontalSnapPointsChanged;
		event EventHandler<object> VerticalSnapPointsChanged;
	}
}
