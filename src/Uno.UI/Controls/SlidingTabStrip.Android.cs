#nullable disable

using Android.Graphics;
using Android.Util;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Controls
{
	public partial class SlidingTabStrip : LinearLayout
	{
		private const int DefaultBottomBorderThicknessDips = 0;
		private const byte DefaultBottomBorderColorAlpha = 0x26;
		private const int SelectedIndicatorThicknessDips = 3;
		private static readonly Color DefaultSelectedIndicatorColor = Color.Argb(51, 181, 229, 255);

		private readonly int _bottomBorderThickness;
		private readonly Paint _bottomBorderPaint;

		private readonly int _selectedIndicatorThickness;
		private readonly Paint _selectedIndicatorPaint;

		private readonly Color _defaultBottomBorderColor;

		private int _selectedPosition;
		private float _selectionOffset;

		private SlidingTabLayout.ITabColorizer _customTabColorizer;
		private readonly SimpleTabColorizer _defaultTabColorizer;

		public SlidingTabStrip(Android.Content.Context context) 
			: this(context, null)
		{
		}

		public SlidingTabStrip(Android.Content.Context context, IAttributeSet attrs)
			: base(context, attrs)
		{
			SetWillNotDraw(false);

			var density = Resources.DisplayMetrics.Density;

			var outValue = new TypedValue();
			context.Theme.ResolveAttribute(Android.Resource.Attribute.ColorForeground, outValue, true);
			var themeForegroundColor = outValue.Data;

			_defaultBottomBorderColor = SetColorAlpha(themeForegroundColor,	DefaultBottomBorderColorAlpha);

			_defaultTabColorizer = new SimpleTabColorizer();
			_defaultTabColorizer.SetIndicatorColors(DefaultSelectedIndicatorColor);

			_bottomBorderThickness = (int)(DefaultBottomBorderThicknessDips * density);
			_bottomBorderPaint = new Paint();
			_bottomBorderPaint.Color = _defaultBottomBorderColor;

			_selectedIndicatorThickness = (int)(SelectedIndicatorThicknessDips * density);
			_selectedIndicatorPaint = new Paint();
		}

		internal void SetCustomTabColorizer(SlidingTabLayout.ITabColorizer customTabColorizer)
		{
			_customTabColorizer = customTabColorizer;
			Invalidate();
		}

		internal void SetSelectedIndicatorColors(params Color[] colors)
		{
			// Make sure that the custom colorizer is removed
			_customTabColorizer = null;
			_defaultTabColorizer.SetIndicatorColors(colors);
			Invalidate();
		}

		internal void OnViewPagerPageChanged(int position, float positionOffset)
		{
			_selectedPosition = position;
			_selectionOffset = positionOffset;
			Invalidate();
		}

		protected override void OnDraw(Canvas canvas)
		{
			var height = Height;
			var childCount = ChildCount;
			var tabColorizer = _customTabColorizer ??  _defaultTabColorizer;

			// Thick colored underline below the current selection
			if (childCount > 0)
			{
				var selectedTitle = GetChildAt(_selectedPosition);
				var left = selectedTitle.Left;
				var right = selectedTitle.Right;
				var color = tabColorizer.GetIndicatorColor(_selectedPosition);

				if (_selectionOffset > 0f && _selectedPosition < (childCount - 1))
				{
					int nextColor = tabColorizer.GetIndicatorColor(_selectedPosition + 1);
					if (color != nextColor)
					{
						color = BlendColors(nextColor, color, _selectionOffset);
					}

					// Draw the selection partway between the tabs
					var nextTitle = GetChildAt(_selectedPosition + 1);
					left = (int)(_selectionOffset * nextTitle.Left + (1.0f - _selectionOffset) * left);
					right = (int)(_selectionOffset * nextTitle.Right +(1.0f - _selectionOffset) * right);
				}

				_selectedIndicatorPaint.Color = color;

				canvas.DrawRect(left, height - _selectedIndicatorThickness, right, height, _selectedIndicatorPaint);
			}

			// Thin underline along the entire bottom edge
			canvas.DrawRect(0, height - _bottomBorderThickness, Width, height, _bottomBorderPaint);
		}

		/**
		 * Set the alpha value of the {@code color} to be the given {@code alpha} value.
		 */
		private static Color SetColorAlpha(int color, byte alpha)
		{
			return Color.Argb(alpha, Color.GetRedComponent(color), Color.GetGreenComponent(color), Color.GetBlueComponent(color));
		}

		/**
		 * Blend {@code color1} and {@code color2} using the given ratio.
		 *
		 * @param ratio of which to blend. 1.0 will return {@code color1}, 0.5 will give an even blend,
		 *              0.0 will return {@code color2}.
		*/
		private static Color BlendColors(int color1, int color2, float ratio)
		{
			var inverseRation = 1f - ratio;
			var r = (Color.GetRedComponent(color1) * ratio) + (Color.GetRedComponent(color2) * inverseRation);
			var g = (Color.GetGreenComponent(color1) * ratio) + (Color.GetGreenComponent(color2) * inverseRation);
			var b = (Color.GetBlueComponent(color1) * ratio) + (Color.GetBlueComponent(color2) * inverseRation);

			return Color.Rgb((int)r, (int)g, (int)b);
		}


		private class SimpleTabColorizer : SlidingTabLayout.ITabColorizer
		{
			private Color[] _indicatorColors;  

			public Color GetIndicatorColor(int position)
			{
				return _indicatorColors[position % _indicatorColors.Length];
			} 

			public void SetIndicatorColors(params Color[] colors)
			{
				_indicatorColors = colors;
			}
		}
	}
}
