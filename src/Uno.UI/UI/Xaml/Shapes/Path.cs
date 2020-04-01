#if !__IOS__ && !__MACOS__
#define LEGACY_SHAPE_MEASURE
#endif

using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Path
#if LEGACY_SHAPE_MEASURE
		: ArbitraryShapeBase
#endif
	{
		#region Data

		public Geometry Data
		{
			get { return (Geometry)this.GetValue(DataProperty); }
			set { this.SetValue(DataProperty, value); }
		}

		public static readonly DependencyProperty DataProperty =
			DependencyProperty.Register(
				"Data",
				typeof(Geometry), 
				typeof(Path),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.ValueInheritsDataContext | FrameworkPropertyMetadataOptions.LogicalChild | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
					propertyChangedCallback: (s, e) => ((Path)s).OnDataChanged()
				)
			);

		partial void OnDataChanged();

		#endregion

#if LEGACY_SHAPE_MEASURE
		protected internal override IEnumerable<object> GetShapeParameters()
		{
			yield return Data;

			foreach (var p in base.GetShapeParameters())
			{
				yield return p;
			}
		}
#endif
	}
}
