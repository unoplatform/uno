using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using Uno.Extensions;
using Uno.Disposables;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Media
{
    //Android partial for Brush
    public partial class Brush
    {
        /// <summary>
        /// Return a paint with Fill style
        /// </summary>
        /// <param name="destinationRect">RectF that will be drawn into - used by ImageBrush</param>
        /// <returns>A Paint with Fill style</returns>
        internal Paint GetFillPaint(Windows.Foundation.Rect destinationRect)
        {
            var paint = GetPaintInner(destinationRect);
            paint.SetStyle(Paint.Style.Fill);
            return paint;
        }

        /// <summary>
        /// Return a paint with Stroke style
        /// </summary>
        /// <param name="destinationRect">RectF that will be drawn into - used by ImageBrush</param>
        /// <returns>A Paint with Stroke style</returns>
        internal Paint GetStrokePaint(Windows.Foundation.Rect destinationRect)
        {
            var paint = GetPaintInner(destinationRect);
            paint.SetStyle(Paint.Style.Stroke);
            return paint;
        }
        protected virtual Paint GetPaintInner(Windows.Foundation.Rect destinationRect) { throw new NotImplementedException(); }

        internal static IDisposable AssignAndObserveBrush(Brush b, Action<Android.Graphics.Color> colorSetter, Action imageBrushCallback = null)
        {
            var disposables = new CompositeDisposable();

            if (b != null)
            {
                var colorBrush = b as SolidColorBrush;
                var imageBrush = b as ImageBrush;

                if (colorBrush != null)
                {
                    colorSetter(colorBrush.ColorWithOpacity);

                    colorBrush.RegisterDisposablePropertyChangedCallback(
                        SolidColorBrush.ColorProperty,
                        (s, colorArg) => colorSetter((s as SolidColorBrush).ColorWithOpacity)
                    ).DisposeWith(disposables);

                    colorBrush.RegisterDisposablePropertyChangedCallback(
                        SolidColorBrush.OpacityProperty,
                        (s, colorArg) => colorSetter((s as SolidColorBrush).ColorWithOpacity)
                    ).DisposeWith(disposables);
                }
                else if (imageBrush != null && imageBrushCallback != null)
                {
                    imageBrush.RegisterDisposablePropertyChangedCallback(
                        ImageBrush.ImageSourceProperty,
                        (_, __) => imageBrushCallback()
                    ).DisposeWith(disposables);

                    imageBrush.RegisterDisposablePropertyChangedCallback(
                        ImageBrush.StretchProperty,
                        (_, __) => imageBrushCallback()
                    ).DisposeWith(disposables);

                    imageBrush.RegisterDisposablePropertyChangedCallback(
                        ImageBrush.RelativeTransformProperty,
                        (_, __) => imageBrushCallback()
                    ).DisposeWith(disposables);
                }
                else
                {
                    colorSetter(SolidColorBrushHelper.Transparent.Color);
                }
            }
            else
            {
                colorSetter(SolidColorBrushHelper.Transparent.Color);
            }

            return disposables;
        }

    }
}
