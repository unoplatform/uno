#nullable enable
#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
#elif __ANDROID__
using _View = Android.Views.ViewGroup;
#else
using _View = Windows.UI.Xaml.UIElement;
#endif

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Extensions
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// Get all ancestor views of <paramref name="view"/>, in order from its immediate parent to the root of the visual tree.
		/// </summary>
		public static IEnumerable<_View> GetVisualAncestry(this _View view)
		{
			var ancestor = view.GetVisualTreeParent();
			while (ancestor != null)
			{
				yield return ancestor;
				ancestor = ancestor.GetVisualTreeParent();
			}
		}

		internal static string GetElementSpecificDetails(this UIElement element)
		{
			return element switch
			{
				TextBlock textBlock => $" Text=\"{textBlock.Text}\" Foreground={textBlock.Foreground}",
				ScrollViewer scrollViewer => $" Extent={scrollViewer.ExtentWidth}x{scrollViewer.ExtentHeight}",
				ScrollContentPresenter scrollContentPresenter => $" Extent={scrollContentPresenter.ExtentWidth}x{scrollContentPresenter.ExtentHeight} Offset={scrollContentPresenter.ScrollOffsets}",
				Viewbox viewbox => $" Stretch={viewbox.Stretch}",
				SplitView splitview => $" Mode={splitview.DisplayMode}",
				Grid grid => GetGridDetails(grid),
				_ => ""
			};

			string GetGridDetails(Grid grid)
			{
				string? columns = default;
				if (grid.ColumnDefinitions.Count > 1)
				{
					columns = " Cols=" + grid.ColumnDefinitions
						.Select<ColumnDefinition, string>(x => x.Width.ToString())
						.JoinBy(",");
				}

				string? rows = default;
				if (grid.RowDefinitions.Count > 1)
				{
					rows = " Rows=" + grid.RowDefinitions
						.Select<RowDefinition, string>(x => x.Height.ToString())
						.JoinBy(",");
				}

				return "" + columns + rows;
			}
		}

		internal static string GetElementGridOrCanvasDetails(this UIElement element)
		{
			var sb = new StringBuilder();

			CheckProperty(Grid.ColumnProperty);
			CheckProperty(Grid.RowProperty);
			CheckProperty(Grid.ColumnSpanProperty);
			CheckProperty(Grid.RowSpanProperty);
			CheckProperty(Canvas.TopProperty);
			CheckProperty(Canvas.LeftProperty);
			CheckProperty(Canvas.ZIndexProperty);

			void CheckProperty(DependencyProperty property)
			{
				if (element.ReadLocalValue(property) is int value)
				{
					sb.Append($" {property.OwnerType.Name}.{property.Name}={value}");
				}
			}

			return sb.ToString();
		}

		internal static string GetLayoutDetails(this UIElement uiElement)
		{
			var sb = new StringBuilder();

			sb
				.Append(uiElement.IsMeasureDirtyPathDisabled ? " MEASURE_DIRTY_PATH_DISABLED" : "")
				.Append(uiElement.IsMeasureDirtyPath ? " MEASURE_DIRTY_PATH" : "")
				.Append(uiElement.IsMeasureDirty ? " MEASURE_DIRTY" : "")
#if __WASM__ || __SKIA__ || __IOS__ || __ANDROID__
				.Append(!uiElement.IsFirstMeasureDone ? " NEVER_MEASURED" : "")
#endif
				.Append(uiElement.IsArrangeDirtyPathDisabled ? " ARRANGE_DIRTY_PATH_DISABLED" : "")
				.Append(uiElement.IsArrangeDirtyPath ? " ARRANGE_DIRTY_PATH" : "")
				.Append(uiElement.IsArrangeDirty ? " ARRANGE_DIRTY" : "")
#if __WASM__ || __SKIA__
				.Append(!uiElement.IsFirstArrangeDone ? " NEVER_ARRANGED" : "")
#endif
				;

			return sb.ToString();
		}

		internal static string GetTransformDetails(this Transform transform)
		{
			string GetMatrix(MatrixTransform matrix)
			{
				var m = matrix.Matrix;
				return $" Matrix=[{m.M11}, {m.M21}, {m.OffsetX}, {m.M12}, {m.M22}, {m.OffsetY}]";
			}

			return transform switch
			{
				ScaleTransform scale => scale.ScaleX == 1d && scale.ScaleY == 1d ? " UNSCALED" : $" ScaleX/Y={scale.ScaleX}/{scale.ScaleY}",
				TranslateTransform translate => $" TranslateX/Y={translate.X}/{translate.Y}",
				TransformGroup group => " TransfrmGrp[" + group.Children.Select(GetTransformDetails).JoinBy(", ") + "]",
				RotateTransform rotate => $" Rotate={rotate.Angle}",
				MatrixTransform matrix => GetMatrix(matrix),
				CompositeTransform composite => $" ScaleX/Y={composite.ScaleX}/{composite.ScaleY} TranslateX/Y={composite.TranslateX}/{composite.TranslateY}",
				SkewTransform skew => $" SkewX={skew.AngleX}  SkewY={skew.AngleY}",
				_ => ""
			};
		}
	}
}
