using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;

using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;
using System.Xml.Linq;
using Microsoft.UI.Composition.Interactions;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// An interop layer to invoke methods found in the WindowManager.ts file.
	/// </summary>
	internal partial class WindowManagerInterop
	{
		//When users set double.MaxValue to scroll to the end of the page Javascript doesn't scroll.
		private const double MAX_SCROLLING_OFFSET = 1_000_000_000_000_000_000;

		internal static Task InitAsync()
			=> NativeMethods.InitAsync();

		internal static void SetBodyCursor(string value)
			=> NativeMethods.SetBodyCursor(value);

		internal static void SetSingleLine(TextBoxView textBoxView)
			=> NativeMethods.SetSingleLine(textBoxView.HtmlId);

		/// <summary>
		/// This method has two purposes:
		/// - Initializes the window size before launch
		/// - Returns the app arguments
		/// The reason for having two concerns in one method 
		/// is to avoid unnecessary roundtrip between JS and C#.
		/// </summary>
		/// <returns>App launch arguments.</returns>
		internal static string BeforeLaunch()
			=> NativeMethods.BeforeLaunch();

		internal static double GetBootTime()
			=> NativeMethods.GetBootTime();

		internal static bool ContainsPoint(IntPtr htmlId, double x, double y, bool considerFill, bool considerStroke)
			=> NativeMethods.ContainsPoint(htmlId, x, y, considerFill, considerStroke);

		#region CreateContent
		internal static void CreateContent(IntPtr htmlId, string htmlTag, IntPtr handle, int uiElementRegistrationId, bool htmlTagIsSvg, bool isFocusable)
		{
			NativeMethods.CreateContent(htmlId, htmlTag, uiElementRegistrationId, isFocusable, htmlTagIsSvg);
		}

		#endregion

		internal static int RegisterUIElement(string typeName, string[] classNames, bool isFrameworkElement)
			=> NativeMethods.RegisterUIElement(typeName, isFrameworkElement, classNames);

		#region SetElementTransform

		internal static void SetElementTransform(IntPtr htmlId, Matrix3x2 matrix)
		{
			NativeMethods.SetElementTransform(htmlId, matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
		}

		#endregion

		#region SetPointerEvents

		internal static void SetPointerEvents(IntPtr htmlId, bool enabled)
		{
			NativeMethods.SetPointerEvents(htmlId, enabled);
		}

		#endregion

		#region MeasureView
		internal static Size MeasureView(IntPtr htmlId, Size availableSize, bool measureContent)
		{
			using var pReturn = TSInteropMarshaller.AllocateBlittableStructure(typeof(WindowManagerMeasureViewReturn));

			NativeMethods.MeasureView(htmlId, availableSize.Width, availableSize.Height, measureContent, pReturn);

			var result = TSInteropMarshaller.UnmarshalStructure<WindowManagerMeasureViewReturn>(pReturn);

			return new Size(result.DesiredWidth, result.DesiredHeight);
		}



		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerMeasureViewReturn
		{
			public double DesiredWidth;
			public double DesiredHeight;
		}
		#endregion

		#region SetStyleDouble
		internal static void SetStyleDouble(IntPtr htmlId, string name, double value)
		{
			var parms = new WindowManagerSetStyleDoubleParams
			{
				HtmlId = htmlId,
				Name = name,
				Value = value
			};

			TSInteropMarshaller.InvokeJS("Uno:setStyleDoubleNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetStyleDoubleParams
		{
			public IntPtr HtmlId;

			public string Name;

			public double Value;
		}

		#endregion

		#region SetStyleString

		internal static void SetStyleString(IntPtr htmlId, string name, string value)
		{
			NativeMethods.SetStyleString(htmlId, name, value);
		}

		#endregion

		#region SetStyles

		internal static void SetStyles(IntPtr htmlId, (string name, string value)[] styles)
		{
			var pairs = new string[styles.Length * 2];

			for (int i = 0; i < styles.Length; i++)
			{
				pairs[i * 2 + 0] = styles[i].name;
				pairs[i * 2 + 1] = styles[i].value;
			}

			NativeMethods.SetStyles(htmlId, pairs);
		}

		#endregion

		#region IsCssFeatureSupported

		internal static bool IsCssFeatureSupported(string supportCondition)
		{
			return NativeMethods.CssSupports(supportCondition);
		}

		#endregion

		internal static void SetUnsetCssClasses(IntPtr htmlId, string[] cssClassesToSet, string[] cssClassesToUnset)
			=> NativeMethods.SetUnsetCssClasses(htmlId, cssClassesToSet, cssClassesToUnset);

		#region SetClasses

		internal static void SetClasses(IntPtr htmlId, string[] cssClasses, int index)
			=> NativeMethods.SetClasses(htmlId, cssClasses, index);

		#endregion

		#region AddView

		internal static void AddView(IntPtr htmlId, IntPtr child, int? index = null)
		{
			var parms = new WindowManagerAddViewParams
			{
				HtmlId = htmlId,
				ChildView = child,
				Index = index ?? -1,
			};

			TSInteropMarshaller.InvokeJS("Uno:addViewNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerAddViewParams
		{
			public IntPtr HtmlId;
			public IntPtr ChildView;
			public int Index;
		}

		#endregion

		#region SetAttributes

		internal static void SetAttributes(IntPtr htmlId, (string name, string value)[] attributes)
		{
			var pairs = new string[attributes.Length * 2];

			for (int i = 0; i < attributes.Length; i++)
			{
				pairs[i * 2 + 0] = attributes[i].name;
				pairs[i * 2 + 1] = attributes[i].value;
			}

			NativeMethods.SetAttributes(htmlId, pairs);
		}

		#endregion

		internal static void SetAttribute(IntPtr htmlId, string name, string value)
			=> NativeMethods.SetAttribute(htmlId, name, value);


		#region GetAttribute
		internal static string GetAttribute(IntPtr htmlId, string name)
		{
			return NativeMethods.GetAttribute(htmlId, name);
		}
		#endregion

		#region ClearAttribute
		internal static void RemoveAttribute(IntPtr htmlId, string name)
		{
			var parms = new WindowManagerRemoveAttributeParams()
			{
				HtmlId = htmlId,
				Name = name,
			};

			TSInteropMarshaller.InvokeJS("Uno:removeAttributeNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerRemoveAttributeParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string Name;
		}

		#endregion

		#region SetName

		internal static void SetName(IntPtr htmlId, string name)
		{
			var parms = new WindowManagerSetNameParams()
			{
				HtmlId = htmlId,
				Name = name,
			};

			TSInteropMarshaller.InvokeJS("Uno:setNameNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetNameParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string Name;
		}
		#endregion

		#region SetXUid

		internal static void SetXUid(IntPtr htmlId, string name)
		{
			var parms = new WindowManagerSetXUidParams()
			{
				HtmlId = htmlId,
				Uid = name,
			};

			TSInteropMarshaller.InvokeJS("Uno:setXUidNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetXUidParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string Uid;
		}
		#endregion

		#region SetVisibility

		internal static void SetVisibility(IntPtr htmlId, bool visible)
		{
			NativeMethods.SetVisibility(htmlId, visible);
		}

		#endregion

		internal static string GetProperty(IntPtr htmlId, string name)
			=> NativeMethods.GetProperty(htmlId, name);

		#region SetProperty

		internal static void SetProperty(IntPtr htmlId, (string name, string value)[] properties)
		{
			var pairs = new string[properties.Length * 2];

			for (int i = 0; i < properties.Length; i++)
			{
				pairs[i * 2 + 0] = properties[i].name;
				pairs[i * 2 + 1] = properties[i].value;
			}

			NativeMethods.SetProperties(htmlId, pairs);
		}

		internal static void SetProperty(IntPtr htmlId, string name, string value)
			=> NativeMethods.SetProperty(htmlId, name, value);

		#endregion

		#region SetElementColor

		internal static void SetElementColor(IntPtr htmlId, Color color)
		{
			var colorAsInteger = color.ToCssInteger();

			var parms = new WindowManagerSetElementColorParams()
			{
				HtmlId = htmlId,
				Color = colorAsInteger,
			};

			TSInteropMarshaller.InvokeJS("Uno:setElementColorNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetElementColorParams
		{
			public IntPtr HtmlId;

			public uint Color;
		}
		#endregion

		#region SetSelectionHighlight

		internal static void SetSelectionHighlight(IntPtr htmlId, Color backgroundColor, Color foregroundColor)
		{
			var backgroundAsInteger = backgroundColor.ToCssInteger();
			var foregroundAsInteger = foregroundColor.ToCssInteger();

			var parms = new WindowManagerSetSelectionHighlightParams()
			{
				HtmlId = htmlId,
				BackgroundColor = backgroundAsInteger,
				ForegroundColor = foregroundAsInteger
			};

			TSInteropMarshaller.InvokeJS("Uno:setSelectionHighlightNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetSelectionHighlightParams
		{
			public IntPtr HtmlId;

			public uint BackgroundColor;

			public uint ForegroundColor;
		}

		#endregion

		#region SetShapeStyles...

		// error SYSLIB1072: Type uint is not supported by source-generated JavaScript interop.
		// ^ we can't use uint here for color, so int will do.

		internal static void SetShapeFillStyle(IntPtr htmlId, int? color, IntPtr? paintRef) => NativeMethods.SetShapeFillStyle(htmlId, color, paintRef);

		internal static void SetShapeStrokeStyle(IntPtr htmlId, int? color, IntPtr? paintRef) => NativeMethods.SetShapeStrokeStyle(htmlId, color, paintRef);

		internal static void SetShapeStrokeWidthStyle(IntPtr htmlId, double strokeWidth) => NativeMethods.SetShapeStrokeWidthStyle(htmlId, strokeWidth);

		internal static void SetShapeStrokeDashArrayStyle(IntPtr htmlId, double[] strokeDashArray) => NativeMethods.SetShapeStrokeDashArrayStyle(htmlId, strokeDashArray);

		internal static void SetShapeStylesFast1(IntPtr htmlId, int? fillColor, IntPtr? fillPaintRef, int? strokeColor, IntPtr? strokePaintRef) =>
			NativeMethods.SetShapeStylesFast1(htmlId, fillColor, fillPaintRef, strokeColor, strokePaintRef);

		internal static void SetShapeStylesFast2(
			IntPtr htmlId,
			int? fillColor, IntPtr? fillPaintRef,
			int? strokeColor, IntPtr? strokePaintRef, double strokeWidth, double[] strokeDashArray) =>
			NativeMethods.SetShapeStylesFast2(htmlId, fillColor, fillPaintRef, strokeColor, strokePaintRef, strokeWidth, strokeDashArray);

		#endregion

		#region SetSvgProperties...
		internal static void SetSvgFillRule(IntPtr htmlId, bool nonzero) =>
			NativeMethods.SetSvgFillRule(htmlId, nonzero);

		internal static void SetSvgEllipseAttributes(IntPtr htmlId, double cx, double cy, double rx, double ry) =>
			NativeMethods.SetSvgEllipseAttributes(htmlId, cx, cy, rx, ry);

		internal static void SetSvgLineAttributes(IntPtr htmlId, double x1, double x2, double y1, double y2) =>
			NativeMethods.SetSvgLineAttributes(htmlId, x1, x2, y1, y2);

		internal static void SetSvgPathAttributes(IntPtr htmlId, bool nonzero, string data) =>
			NativeMethods.SetSvgPathAttributes(htmlId, nonzero, data);

		internal static void SetSvgPolyPoints(IntPtr htmlId, double[] points) =>
			NativeMethods.SetSvgPolyPoints(htmlId, points);

		internal static void SetSvgRectangleAttributes(IntPtr htmlId, double x, double y, double width, double height, double rx, double ry) =>
			NativeMethods.SetSvgRectangleAttributes(htmlId, x, y, width, height, rx, ry);

		#endregion

		#region RemoveView
		internal static void RemoveView(IntPtr htmlId, IntPtr childId)
		{
			var parms = new WindowManagerRemoveViewParams
			{
				HtmlId = htmlId,
				ChildView = childId
			};

			TSInteropMarshaller.InvokeJS("Uno:removeViewNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerRemoveViewParams
		{
			public IntPtr HtmlId;
			public IntPtr ChildView;
		}

		#endregion

		#region DestroyView
		internal static void DestroyView(IntPtr htmlId)
		{
			NativeMethods.DestroyView(htmlId);
		}
		#endregion

		#region ResetStyle
		internal static void ResetStyle(IntPtr htmlId, string[] names)
		{
			if (names == null || names.Length == 0)
			{
				// nothing to do
				return;
			}

			NativeMethods.ResetStyle(htmlId, names);
		}
		#endregion

		#region RegisterEventOnView
		internal static void RegisterEventOnView(IntPtr htmlId, string eventName, bool onCapturePhase, int eventExtractorId)
		{
			var parms = new WindowManagerRegisterEventOnViewParams()
			{
				HtmlId = htmlId,
				EventName = eventName,
				OnCapturePhase = onCapturePhase,
				EventExtractorId = eventExtractorId,
			};

			TSInteropMarshaller.InvokeJS("Uno:registerEventOnViewNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerRegisterEventOnViewParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string EventName;

			public bool OnCapturePhase;

			public int EventExtractorId;
		}
		#endregion

		#region GetBBox

		internal static Rect GetBBox(IntPtr htmlId)
		{
			var ret = NativeMethods.GetBBox(htmlId);
			return new Rect(ret[0], ret[1], ret[2], ret[3]);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerGetBBoxParams
		{
			public IntPtr HtmlId;
		}


		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerGetBBoxReturn
		{
			public double X;
			public double Y;
			public double Width;
			public double Height;
		}

		#endregion

		#region SetContentHtml

		internal static void SetContentHtml(IntPtr htmlId, string html)
		{
			var parms = new WindowManagerSetContentHtmlParams()
			{
				HtmlId = htmlId,
				Html = html,
			};

			TSInteropMarshaller.InvokeJS("Uno:setHtmlContentNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetContentHtmlParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string Html;
		}

		#endregion

		#region ArrangeElement

		internal static void ArrangeElement(IntPtr htmlId, Rect rect, Rect? clipRect)
		{
			var clipRectValue = clipRect ?? default;

			NativeMethods.ArrangeElement(
				htmlId,
				rect.Top,
				rect.Left,
				rect.Width,
				rect.Height,
				clipRect.HasValue,
				clipRectValue.Top,
				clipRectValue.Left,
				clipRectValue.Bottom,
				clipRectValue.Right);
		}


		#endregion

		#region GetClientViewSize

		internal static (Size clientSize, Size offsetSize) GetClientViewSize(IntPtr htmlId)
		{
			var parms = new WindowManagerGetClientViewSizeParams
			{
				HtmlId = htmlId
			};

			var ret = (WindowManagerGetClientViewSizeReturn)TSInteropMarshaller.InvokeJS("Uno:getClientViewSizeNative", parms, typeof(WindowManagerGetClientViewSizeReturn));

			return (
				clientSize: new Size(ret.ClientWidth, ret.ClientHeight),
				offsetSize: new Size(ret.OffsetWidth, ret.OffsetHeight)
			);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerGetClientViewSizeParams
		{
			public IntPtr HtmlId;
		}


		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerGetClientViewSizeReturn
		{
			public double OffsetWidth;
			public double OffsetHeight;
			public double ClientWidth;
			public double ClientHeight;
		}

		#endregion

		internal static void FocusView(IntPtr htmlId)
			=> NativeMethods.FocusView(htmlId);

		#region ScrollTo
		internal static void ScrollTo(IntPtr htmlId, double? left, double? top, bool disableAnimation)
		{
			var sanitizedTop = top.HasValue && top == double.MaxValue ? MAX_SCROLLING_OFFSET : top;
			var sanitizedLeft = left.HasValue && left == double.MaxValue ? MAX_SCROLLING_OFFSET : left;

			var parms = new WindowManagerScrollToOptionsParams
			{
				HtmlId = htmlId,
				HasLeft = sanitizedLeft.HasValue,
				Left = sanitizedLeft ?? 0,
				HasTop = sanitizedTop.HasValue,
				Top = sanitizedTop ?? 0,
				DisableAnimation = disableAnimation
			};

			TSInteropMarshaller.InvokeJS("Uno:scrollTo", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerScrollToOptionsParams
		{
			public double Left;
			public double Top;
			public bool HasLeft;
			public bool HasTop;
			public bool DisableAnimation;

			public IntPtr HtmlId;
		}
		#endregion

		#region SetElementBackgroundColor
		internal static void SetElementBackgroundColor(IntPtr htmlId, Color color)
		{
			var parms = new WindowManagerSetElementBackgroundColorParams
			{
				HtmlId = htmlId,
				Color = color.ToCssInteger()
			};

			TSInteropMarshaller.InvokeJS("Uno:setElementBackgroundColor", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetElementBackgroundColorParams
		{
			public IntPtr HtmlId;

			public uint Color;
		}
		#endregion

		#region SetElementBackgroundColor
		internal static void SetElementBackgroundGradient(IntPtr htmlId, string cssGradient)
		{
			var parms = new WindowManagerSetElementBackgroundGradientParams
			{
				HtmlId = htmlId,
				CssGradient = cssGradient
			};

			TSInteropMarshaller.InvokeJS("Uno:setElementBackgroundGradient", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetElementBackgroundGradientParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string CssGradient;
		}
		#endregion

		#region SetElementBackgroundColor

		internal static void ResetElementBackground(IntPtr htmlId)
		{
			var parms = new WindowManagerResetElementBackgroundParams
			{
				HtmlId = htmlId,
			};

			TSInteropMarshaller.InvokeJS("Uno:resetElementBackground", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerResetElementBackgroundParams
		{
			public IntPtr HtmlId;
		}
		#endregion

		internal static Task<string> GetNaturalImageSizeAsync(string imageUri)
			=> NativeMethods.GetNaturalImageSizeAsync(imageUri);

		internal static string RawPixelsToBase64EncodeImage(IntPtr data, int width, int height)
			=> NativeMethods.RawPixelsToBase64EncodeImage(data, width, height);

		internal static void SelectInputRange(IntPtr htmlId, int start, int length)
			=> NativeMethods.SelectInputRange(htmlId, start, length);

		internal static void SetImageAsMonochrome(IntPtr htmlId, string url, string color)
			=> NativeMethods.SetImageAsMonochrome(htmlId, url, color);

		internal static void SetCornerRadius(IntPtr htmlId, float topLeftX, float topLeftY, float topRightX, float topRightY, float bottomRightX, float bottomRightY, float bottomLeftX, float bottomLeftY)
			=> NativeMethods.SetCornerRadius(htmlId, topLeftX, topLeftY, topRightX, topRightY, bottomRightX, bottomRightY, bottomLeftX, bottomLeftY);

		internal static void SetRootElement(IntPtr htmlId)
		{
			NativeMethods.SetRootElement(htmlId);
		}

		internal static void WindowActivate()
		{
			NativeMethods.WindowActivate();
		}

		internal static bool GetIsOverflowing(IntPtr htmlId)
			=> NativeMethods.GetIsOverflowing(htmlId);
		internal static void SetIsFocusable(IntPtr htmlId, bool isFocusable)
			=> NativeMethods.SetIsFocusable(htmlId, isFocusable);

		internal static UIElement TryGetElementInCoordinate(Point point)
		{
			var htmlId = NativeMethods.GetElementInCoordinate(point.X, point.Y);
			return UIElement.GetElementFromHandle(htmlId);
		}
	}

	partial class WindowManagerInterop
	{
		internal static partial class NativeMethods
		{
			private const string InstancedThis = "globalThis.Uno.UI.WindowManager.current";

			[JSImport("globalThis.Uno.UI.WindowManager.current.arrangeElementNativeFast")]
			internal static partial void ArrangeElement(
				IntPtr htmlId,
				double top,
				double left,
				double width,
				double height,
				bool clip,
				double clipTop,
				double clipLeft,
				double clipBottom,
				double clipRight);

			[JSImport("globalThis.Uno.UI.WindowManager.current.createContentNativeFast")]
			internal static partial void CreateContent(IntPtr htmlId, string tagName, int uiElementRegistrationId, bool isFocusable, bool isSvg);

			[JSImport("globalThis.CSS.supports")]
			internal static partial bool CssSupports(string condition);

			[JSImport("globalThis.Uno.UI.WindowManager.current.destroyViewNativeFast")]
			internal static partial void DestroyView(IntPtr htmlId);

			[JSImport("globalThis.Uno.UI.WindowManager.setBodyCursor")]
			internal static partial void SetBodyCursor(string value);

			[JSImport("globalThis.Uno.UI.WindowManager.setSingleLine")]
			internal static partial void SetSingleLine(IntPtr htmlId);

			[JSImport("globalThis.Uno.UI.WindowManager.beforeLaunch")]
			internal static partial string BeforeLaunch();

			[JSImport("globalThis.Uno.UI.WindowManager.getBootTime")]
			internal static partial double GetBootTime();

			[JSImport("globalThis.Uno.UI.WindowManager.current.getNaturalImageSize")]
			internal static partial Task<string> GetNaturalImageSizeAsync(string imageUri);

			[JSImport("globalThis.Uno.UI.WindowManager.current.focusView")]
			internal static partial void FocusView(IntPtr htmlId);

			[JSImport("globalThis.Uno.UI.WindowManager.current.getAttribute")]
			internal static partial string GetAttribute(IntPtr htmlId, string name);

			[JSImport("globalThis.Uno.UI.WindowManager.current.getProperty")]
			internal static partial string GetProperty(IntPtr htmlId, string name);

			[JSImport("globalThis.Uno.UI.WindowManager.init")]
			internal static partial Task InitAsync();

			[JSImport("globalThis.Uno.UI.WindowManager.current.measureViewNativeFast")]
			internal static partial void MeasureView(IntPtr htmlId, double availableWidth, double availableHeight, bool measureContent, IntPtr pReturn);

			[JSImport("globalThis.Uno.UI.WindowManager.current.rawPixelsToBase64EncodeImage")]
			internal static partial string RawPixelsToBase64EncodeImage(IntPtr data, int width, int height);

			[JSImport("globalThis.Uno.UI.WindowManager.current.selectInputRange")]
			internal static partial void SelectInputRange(IntPtr htmlId, int start, int length);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setAttributesNativeFast")]
			internal static partial void SetAttributes(IntPtr htmlId, string[] pairs);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setAttribute")]
			internal static partial void SetAttribute(IntPtr htmlId, string name, string value);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setElementTransformNativeFast")]
			internal static partial void SetElementTransform(IntPtr htmlId, float m11, float m12, float m21, float m22, float m31, float m32);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setImageAsMonochrome")]
			internal static partial void SetImageAsMonochrome(IntPtr htmlId, string url, string color);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setCornerRadius")]
			internal static partial void SetCornerRadius(IntPtr htmlId, float topLeftX, float topLeftY, float topRightX, float topRightY, float bottomRightX, float bottomRightY, float bottomLeftX, float bottomLeftY);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setPointerEvents")]
			internal static partial void SetPointerEvents(IntPtr htmlId, bool enabled);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setPropertyNativeFast")]
			internal static partial void SetProperties(IntPtr htmlId, string[] pairs);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setSinglePropertyNativeFast")]
			internal static partial void SetProperty(IntPtr htmlId, string name, string value);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setRootElement")]
			internal static partial void SetRootElement(IntPtr htmlId);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setStyleStringNativeFast")]
			internal static partial void SetStyleString(IntPtr htmlId, string name, string value);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setStyleNativeFast")]
			internal static partial void SetStyles(IntPtr htmlId, string[] pairs);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setVisibilityNativeFast")]
			internal static partial void SetVisibility(IntPtr htmlId, bool visible);

			[JSImport("globalThis.Uno.UI.WindowManager.current.activate")]
			internal static partial void WindowActivate();

			[JSImport("globalThis.Uno.UI.WindowManager.current.getIsOverflowing")]
			internal static partial bool GetIsOverflowing(IntPtr htmlId);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setIsFocusable")]
			internal static partial void SetIsFocusable(nint htmlId, bool isFocusable);

			[JSImport("globalThis.Uno.UI.WindowManager.current.getElementInCoordinate")]
			internal static partial IntPtr GetElementInCoordinate(double x, double y);

			[JSImport("globalThis.Uno.UI.WindowManager.current.containsPoint")]
			internal static partial bool ContainsPoint(IntPtr htmlId, double x, double y, bool considerFill, bool considerStroke);

			[JSImport("globalThis.Uno.UI.WindowManager.current.registerUIElement")]
			internal static partial int RegisterUIElement(string typeName, bool isFrameworkElement, string[] classNames);

			[JSImport("globalThis.Uno.UI.WindowManager.current.resetStyle")]
			internal static partial void ResetStyle(IntPtr htmlId, string[] names);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setClasses")]
			internal static partial void SetClasses(IntPtr htmlId, string[] cssClasses, int index);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setUnsetCssClasses")]
			internal static partial void SetUnsetCssClasses(IntPtr htmlId, string[] cssClassesToSet, string[] cssClassesToUnset);

			[JSImport("globalThis.Uno.UI.WindowManager.current.getBBox")]
			internal static partial double[] GetBBox(IntPtr htmlId);

			[JSImport($"{InstancedThis}.setShapeFillStyle")]
			internal static partial void SetShapeFillStyle(IntPtr htmlId, int? color, IntPtr? paintRef);

			[JSImport($"{InstancedThis}.setShapeStrokeStyle")]
			internal static partial void SetShapeStrokeStyle(IntPtr htmlId, int? color, IntPtr? paintRef);

			[JSImport($"{InstancedThis}.setShapeStrokeWidthStyle")]
			internal static partial void SetShapeStrokeWidthStyle(IntPtr htmlId, double strokeWidth);

			[JSImport($"{InstancedThis}.setShapeStrokeDashArrayStyle")]
			internal static partial void SetShapeStrokeDashArrayStyle(IntPtr htmlId, double[] strokeDashArray);

			[JSImport($"{InstancedThis}.setShapeStylesFast1")]
			internal static partial void SetShapeStylesFast1(IntPtr htmlId, int? fillColor, IntPtr? fillPaintRef, int? strokeColor, IntPtr? strokePaintRef);

			[JSImport($"{InstancedThis}.setShapeStylesFast2")]
			internal static partial void SetShapeStylesFast2(
				IntPtr htmlId,
				int? fillColor, IntPtr? fillPaintRef,
				int? strokeColor, IntPtr? strokePaintRef, double strokeWidth, double[] strokeDashArray);

			[JSImport($"{InstancedThis}.setSvgFillRule")]
			internal static partial void SetSvgFillRule(IntPtr htmlId, bool nonzero);

			[JSImport($"{InstancedThis}.setSvgEllipseAttributes")]
			internal static partial void SetSvgEllipseAttributes(IntPtr htmlId, double cx, double cy, double rx, double ry);

			[JSImport($"{InstancedThis}.setSvgLineAttributes")]
			internal static partial void SetSvgLineAttributes(IntPtr htmlId, double x1, double x2, double y1, double y2);

			[JSImport($"{InstancedThis}.setSvgPathAttributes")]
			internal static partial void SetSvgPathAttributes(IntPtr htmlId, bool nonzero, System.String data);

			[JSImport($"{InstancedThis}.setSvgPolyPoints")]
			internal static partial void SetSvgPolyPoints(IntPtr htmlId, double[] points);

			[JSImport($"{InstancedThis}.setSvgRectangleAttributes")]
			internal static partial void SetSvgRectangleAttributes(IntPtr htmlId, double x, double y, double width, double height, double rx, double ry);
		}
	}
}
