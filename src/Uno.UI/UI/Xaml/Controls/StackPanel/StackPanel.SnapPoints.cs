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
			if (orientation == Orientation && _snapPoints != null)
			{
				switch (alignment)
				{
					case SnapPointsAlignment.Far:
						return _snapPoints;
					case SnapPointsAlignment.Center:
					{
						float previous = 0;
						var result = new List<float>(_snapPoints.Select(sp => (previous + sp) / 2));

						return result;
					}
					case SnapPointsAlignment.Near:
					{
						var result = new List<float>(_snapPoints.Count);
						result.Add(0f);
						for (var i = 1; i < _snapPoints.Count; i++)
						{
							result.Add(_snapPoints[i-1]);
						}
						return result;
					}
				}
			}
			return new float[0];

		}
		public float GetRegularSnapPoints(Orientation orientation, Primitives.SnapPointsAlignment alignment, out float offset)
		{
			throw new InvalidOperationException();
		}
		public event EventHandler<object> HorizontalSnapPointsChanged;
		public event EventHandler<object> VerticalSnapPointsChanged;

	}
}
