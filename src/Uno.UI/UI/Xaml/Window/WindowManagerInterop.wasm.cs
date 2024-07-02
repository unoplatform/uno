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
using Windows.UI.Xaml;

using System.Runtime.InteropServices.JavaScript;

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

		#region CreateContent
		internal static void CreateContent(IntPtr htmlId, string htmlTag, IntPtr handle, int uiElementRegistrationId, bool htmlTagIsSvg, bool isFocusable)
		{
			NativeMethods.CreateContent(htmlId, htmlTag, uiElementRegistrationId, isFocusable, htmlTagIsSvg);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerCreateContentParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string TagName;

			public IntPtr Handle;

			public int UIElementRegistrationId;

			public bool IsSvg;
			public bool IsFocusable;
		}

		#endregion

		#region CreateContent
		internal static int RegisterUIElement(string typeName, string[] classNames, bool isFrameworkElement)
		{
			var parms = new WindowManagerRegisterUIElementParams
			{
				TypeName = typeName,
				IsFrameworkElement = isFrameworkElement,
				Classes_Length = classNames.Length,
				Classes = classNames,
			};

			var ret = (WindowManagerRegisterUIElementReturn)TSInteropMarshaller.InvokeJS("Uno:registerUIElementNative", parms, typeof(WindowManagerRegisterUIElementReturn));

			return ret.RegistrationId;
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerRegisterUIElementParams
		{
			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string TypeName;

			public bool IsFrameworkElement;

			public int Classes_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = TSInteropMarshaller.LPUTF8Str)]
			public string[] Classes;
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerRegisterUIElementReturn
		{
			public int RegistrationId;
		}

		#endregion

		#region SetElementTransform

		internal static void SetElementTransform(IntPtr htmlId, Matrix3x2 matrix)
		{
			NativeMethods.SetElementTransform(htmlId, matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerSetElementTransformParams
		{
			public IntPtr HtmlId;

			public double M11;
			public double M12;
			public double M21;
			public double M22;
			public double M31;
			public double M32;
		}

		#endregion

		#region SetPointerEvents

		internal static void SetPointerEvents(IntPtr htmlId, bool enabled)
		{
			NativeMethods.SetPointerEvents(htmlId, enabled);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetPointerEventsParams
		{
			public IntPtr HtmlId;

			public bool Enabled;
		}

		#endregion

		internal static void SetPointerCapture(IntPtr htmlId, uint pointerId)
		{
			NativeMethods.SetPointerCapture(htmlId, pointerId);
		}

		internal static void ReleasePointerCapture(IntPtr htmlId, uint pointerId)
		{
			NativeMethods.ReleasePointerCapture(htmlId, pointerId);
		}

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
		private struct WindowManagerMeasureViewParams
		{
			public IntPtr HtmlId;

			public double AvailableWidth;
			public double AvailableHeight;
			public bool MeasureContent;
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

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetStyleStringParams
		{
			public IntPtr HtmlId;

			public string Name;

			public string Value;
		}

		#endregion

		#region SetRectanglePosition

		internal static void SetSvgElementRect(IntPtr htmlId, Rect rect)
		{
			var parms = new WindowManagerSetSvgElementRectParams
			{
				HtmlId = htmlId,
				X = rect.X,
				Y = rect.Y,
				Width = rect.Width,
				Height = rect.Height,
			};

			TSInteropMarshaller.InvokeJS("Uno:setSvgElementRect", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerSetSvgElementRectParams
		{
			public double X;
			public double Y;
			public double Width;
			public double Height;

			public IntPtr HtmlId;
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

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetStylesParams
		{
			public IntPtr HtmlId;

			public int Pairs_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)]
			public string[] Pairs;
		}

		#endregion

		#region IsCssFeatureSupported

		internal static bool IsCssFeatureSupported(string supportCondition)
		{
			return NativeMethods.CssSupports(supportCondition);
		}

		#endregion

		#region SetUnsetCssClasses
		internal static void SetUnsetCssClasses(IntPtr htmlId, string[] cssClassesToSet, string[] cssClassesToUnset)
		{
			var parms = new WindowManagerSetUnsetClassesParams
			{
				HtmlId = htmlId,
				CssClassesToSet = cssClassesToSet,
				CssClassesToSet_Length = cssClassesToSet?.Length ?? 0,
				CssClassesToUnset = cssClassesToUnset,
				CssClassesToUnset_Length = cssClassesToUnset?.Length ?? 0
			};

			TSInteropMarshaller.InvokeJS("Uno:setUnsetClassesNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetUnsetClassesParams
		{
			public IntPtr HtmlId;

			public int CssClassesToSet_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)]
			public string[] CssClassesToSet;

			public int CssClassesToUnset_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)]
			public string[] CssClassesToUnset;
		}
		#endregion

		#region SetClasses

		internal static void SetClasses(IntPtr htmlId, string[] cssClasses, int index)
		{
			var parms = new WindowManagerSetClassesParams
			{
				HtmlId = htmlId,
				CssClasses = cssClasses,
				CssClasses_Length = cssClasses.Length,
				Index = index
			};

			TSInteropMarshaller.InvokeJS("Uno:setClassesNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetClassesParams
		{
			public IntPtr HtmlId;

			public int CssClasses_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)]
			public string[] CssClasses;

			public int Index;
		}

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

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetAttributesParams
		{
			public IntPtr HtmlId;

			public int Pairs_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = TSInteropMarshaller.LPUTF8Str)]
			public string[] Pairs;
		}

		#endregion

		#region SetAttribute
		internal static void SetAttribute(IntPtr htmlId, string name, string value)
		{
			var parms = new WindowManagerSetAttributeParams()
			{
				HtmlId = htmlId,
				Name = name,
				Value = value,
			};

			TSInteropMarshaller.InvokeJS("Uno:setAttributeNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetAttributeParams
		{
			public IntPtr HtmlId;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string Name;

			[MarshalAs(TSInteropMarshaller.LPUTF8Str)]
			public string Value;
		}

		#endregion

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

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetVisibilityParams
		{
			public IntPtr HtmlId;

			public bool Visible;
		}
		#endregion

		internal static string GetProperty(IntPtr htmlId, string name)
		{
			return NativeMethods.GetProperty(htmlId, name);
		}

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
		{
			NativeMethods.SetProperty(htmlId, name, value);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetSinglePropertyParams
		{
			public IntPtr HtmlId;

			public string Name;

			public string Value;
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetPropertyParams
		{
			public IntPtr HtmlId;

			public int Pairs_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = TSInteropMarshaller.LPUTF8Str)]
			public string[] Pairs;
		}

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

		#region SetElementFill

		internal static void SetElementFill(IntPtr htmlId, Color color)
		{
			var colorAsInteger = color.ToCssInteger();

			var parms = new WindowManagerSetElementFillParams()
			{
				HtmlId = htmlId,
				Color = colorAsInteger,
			};

			TSInteropMarshaller.InvokeJS("Uno:setElementFillNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetElementFillParams
		{
			public IntPtr HtmlId;

			public uint Color;
		}
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

			var parms = new WindowManagerResetStyleParams()
			{
				HtmlId = htmlId,
				Styles = names,
				Styles_Length = names.Length,
			};

			TSInteropMarshaller.InvokeJS("Uno:resetStyleNative", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerResetStyleParams
		{
			public IntPtr HtmlId;

			public int Styles_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = TSInteropMarshaller.LPUTF8Str)]
			public string[] Styles;
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

		#region registerPointerEventsOnView
		internal static void RegisterPointerEventsOnView(IntPtr htmlId)
		{
			var parms = new WindowManagerRegisterPointerEventsOnViewParams()
			{
				HtmlId = htmlId,
			};

			TSInteropMarshaller.InvokeJS("Uno:registerPointerEventsOnView", parms);
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerRegisterPointerEventsOnViewParams
		{
			public IntPtr HtmlId;
		}
		#endregion

		#region GetBBox

		internal static Rect GetBBox(IntPtr htmlId)
		{
			var parms = new WindowManagerGetBBoxParams
			{
				HtmlId = htmlId
			};

			var ret = (WindowManagerGetBBoxReturn)TSInteropMarshaller.InvokeJS("Uno:getBBoxNative", parms, typeof(WindowManagerGetBBoxReturn));

			return new Rect(ret.X, ret.Y, ret.Width, ret.Height);
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

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerArrangeElementParams
		{
			public double Top;
			public double Left;
			public double Width;
			public double Height;

			public double ClipTop;
			public double ClipLeft;
			public double ClipBottom;
			public double ClipRight;

			public IntPtr HtmlId;
			public bool Clip;
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

		#region Pointers
		[Flags]
		internal enum HtmlPointerButtonsState
		{
			// https://developer.mozilla.org/en-US/docs/Web/API/Pointer_events#Determining_button_states

			None = 0,
			Left = 1,
			Middle = 4,
			Right = 2,
			X1 = 8,
			X2 = 16,
			Eraser = 32,
		}

		internal enum HtmlPointerButtonUpdate
		{
			None = -1,
			Left = 0,
			Middle = 1,
			Right = 2,
			X1 = 3,
			X2 = 4,
			Eraser = 5
		}
		#endregion

		internal static partial class NativeMethods
		{
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

			[JSImport("globalThis.Uno.UI.WindowManager.current.releasePointerCapture")]
			internal static partial void ReleasePointerCapture(IntPtr htmlId, double pointerId);

			[JSImport("globalThis.Uno.UI.WindowManager.current.selectInputRange")]
			internal static partial void SelectInputRange(IntPtr htmlId, int start, int length);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setAttributesNativeFast")]
			internal static partial void SetAttributes(IntPtr htmlId, string[] pairs);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setElementTransformNativeFast")]
			internal static partial void SetElementTransform(IntPtr htmlId, float m11, float m12, float m21, float m22, float m31, float m32);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setImageAsMonochrome")]
			internal static partial void SetImageAsMonochrome(IntPtr htmlId, string url, string color);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setCornerRadius")]
			internal static partial void SetCornerRadius(IntPtr htmlId, float topLeftX, float topLeftY, float topRightX, float topRightY, float bottomRightX, float bottomRightY, float bottomLeftX, float bottomLeftY);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setPointerCapture")]
			internal static partial void SetPointerCapture(IntPtr htmlId, double pointerId);

			[JSImport("globalThis.Uno.UI.WindowManager.current.setPointerEventsNativeFast")]
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
		}
	}
}
