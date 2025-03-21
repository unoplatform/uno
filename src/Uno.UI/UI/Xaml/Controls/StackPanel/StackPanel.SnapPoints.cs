using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	partial class StackPanel
	{
		private List<float> _snapPoints;

		public bool AreHorizontalSnapPointsRegular => false;
		public bool AreVerticalSnapPointsRegular => false;

		public static DependencyProperty AreScrollSnapPointsRegularProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				nameof(AreScrollSnapPointsRegular), typeof(bool),
				typeof(StackPanel),
				new FrameworkPropertyMetadata(default(bool)));

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, Primitives.SnapPointsAlignment alignment)
		{
			if (orientation != Orientation || _snapPoints == null)
			{
				return Array.Empty<float>();
			}

			switch (alignment)
			{
				case SnapPointsAlignment.Far:
					return _snapPoints;
				case SnapPointsAlignment.Center:
					var centerResult = new List<float>(_snapPoints.Count);
					for (var i = 0; i < _snapPoints.Count; i++)
					{
						var start = i == 0 ? (float)(orientation == Orientation.Horizontal ? Margin.Left : Margin.Right) : _snapPoints[i - 1];
						var end = _snapPoints[i];

						centerResult.Add((start + end) / 2);
					}

					return centerResult;
				case SnapPointsAlignment.Near:
					var nearResult = new List<float>(_snapPoints.Count);
					if (_snapPoints.Count > 0)
					{
						nearResult.Add(0f);
					}
					for (var i = 1; i < _snapPoints.Count; i++)
					{
						nearResult.Add(_snapPoints[i - 1]);
					}
					return nearResult;
				default:
					throw new ArgumentOutOfRangeException(nameof(alignment), "Alignment should be Near, Center or Far.");
			}
		}
		public float GetRegularSnapPoints(Orientation orientation, Primitives.SnapPointsAlignment alignment, out float offset)
		{
			throw new InvalidOperationException();
		}
		public event EventHandler<object> HorizontalSnapPointsChanged;
		public event EventHandler<object> VerticalSnapPointsChanged;

	}
}
