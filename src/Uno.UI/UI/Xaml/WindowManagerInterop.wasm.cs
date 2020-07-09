using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Extensions;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// An interop layer to invoke methods found in the WindowManager.ts file.
	/// </summary>
	/// <remarks>Slow methods are present for the WPF hosting mode, for which memory sharing is not available</remarks>
	internal partial class WindowManagerInterop
	{
		private static bool UseJavascriptEval =>
			!WebAssemblyRuntime.IsWebAssembly || FeatureConfiguration.Interop.ForceJavascriptInterop;

		#region Init
		internal static void Init(bool isHostedMode, bool isLoadEventsEnabled)
		{
			if (UseJavascriptEval)
			{
				WebAssemblyRuntime.InvokeJS($"Uno.UI.WindowManager.init({isHostedMode.ToString().ToLowerInvariant()}, {isLoadEventsEnabled.ToString().ToLowerInvariant()});");
			}
			else
			{
				var parms = new WindowManagerInitParams
				{
					IsHostedMode = isHostedMode,
					IsLoadEventsEnabled = isLoadEventsEnabled
				};

				TSInteropMarshaller.InvokeJS<WindowManagerInitParams, bool>("UnoStatic:initNative", parms);
			}
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerInitParams
		{
			public bool IsHostedMode;

			public bool IsLoadEventsEnabled;
		}

		#endregion

		#region CreateContent
		internal static void CreateContent(IntPtr htmlId, string htmlTag, IntPtr handle, int uiElementRegistrationId, bool htmlTagIsSvg, bool isFocusable)
		{
			if (UseJavascriptEval)
			{
				var isSvgStr = htmlTagIsSvg ? "true" : "false";
				var isFocusableStr = isFocusable ? "true" : "false"; // by default all control are not focusable, it has to be change latter by the control itself

				WebAssemblyRuntime.InvokeJS(
					"Uno.UI.WindowManager.current.createContent({" +
					"id:\"" + htmlId + "\"," +
					"tagName:\"" + htmlTag + "\", " +
					"handle:" + handle + ", " +
					"uiElementRegistrationId: " + uiElementRegistrationId + ", " +
					"isSvg:" + isSvgStr + ", " +
					"isFocusable:" + isFocusableStr +
					"});");
			}
			else
			{
				var parms = new WindowManagerCreateContentParams
				{
					HtmlId = htmlId,
					TagName = htmlTag,
					Handle = handle,
					UIElementRegistrationId = uiElementRegistrationId,
					IsSvg = htmlTagIsSvg,
					IsFocusable = isFocusable
				};

				TSInteropMarshaller.InvokeJS<WindowManagerCreateContentParams, bool>("Uno:createContentNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				var isFrameworkElementStr = isFrameworkElement ? "true" : "false";
				var classesParam = classNames.Select(c => $"\"{c}\"").JoinBy(",");

				var ret = WebAssemblyRuntime.InvokeJS(
					"Uno.UI.WindowManager.current.registerUIElement({" +
					"typeName:\"" + typeName + "\"," +
					"isFrameworkElement:" + isFrameworkElementStr + ", " +
					"classes:[" + classesParam + "]" +
					"});");

				return int.Parse(ret);
			}
			else
			{
				var parms = new WindowManagerRegisterUIElementParams
				{
					TypeName = typeName,
					IsFrameworkElement = isFrameworkElement,
					Classes_Length = classNames.Length,
					Classes = classNames,
				};

				var ret = TSInteropMarshaller.InvokeJS<WindowManagerRegisterUIElementParams, WindowManagerRegisterUIElementReturn>("Uno:registerUIElementNative", parms);
				return ret.RegistrationId;
			}
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

		internal static void SetElementTransform(IntPtr htmlId, Matrix3x2 matrix, bool requiresClipping)
		{
			if (UseJavascriptEval)
			{
				FormattableString native = $"matrix({matrix.M11},{matrix.M12},{matrix.M21},{matrix.M22},{matrix.M31},{matrix.M32})";

				SetStyles(
					htmlId,
					new[] { ("transform", native.ToStringInvariant()) }
				);

				SetArrangeProperties(htmlId, requiresClipping);
			}
			else
			{
				var parms = new WindowManagerSetElementTransformParams
				{
					HtmlId = htmlId,
					M11 = matrix.M11,
					M12 = matrix.M12,
					M21 = matrix.M21,
					M22 = matrix.M22,
					M31 = matrix.M31,
					M32 = matrix.M32,
					ClipToBounds = requiresClipping
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetElementTransformParams, bool>("Uno:setElementTransformNative", parms);
			}
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

			public bool ClipToBounds;
		}

		#endregion

		#region SetPointerEvents

		internal static void SetPointerEvents(IntPtr htmlId, bool enabled)
		{
			if (UseJavascriptEval)
			{
				var enabledString = enabled ? "true" : "false";

				var command = "Uno.UI.WindowManager.current.setPointerEvents(" + htmlId + ", " + enabledString + ");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetPointerEventsParams
				{
					HtmlId = htmlId,
					Enabled = enabled
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetPointerEventsParams, bool>("Uno:setPointerEventsNative", parms);
			}
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetPointerEventsParams
		{
			public IntPtr HtmlId;

			public bool Enabled;
		}

		#endregion

		#region MeasureView
		internal static Size MeasureView(IntPtr htmlId, Size availableSize)
		{
			if (UseJavascriptEval)
			{
				var w = double.IsInfinity(availableSize.Width) ? "null" : availableSize.Width.ToStringInvariant();
				var h = double.IsInfinity(availableSize.Height) ? "null" : availableSize.Height.ToStringInvariant();

				var command = "Uno.UI.WindowManager.current.measureView(" + htmlId + ", \"" + w + "\", \"" + h + "\");";
				var result = WebAssemblyRuntime.InvokeJS(command);

				var parts = result.Split(';');

				return new Size(
					double.Parse(parts[0], CultureInfo.InvariantCulture),
					double.Parse(parts[1], CultureInfo.InvariantCulture));
			}
			else
			{
				var parms = new WindowManagerMeasureViewParams
				{
					HtmlId = htmlId,
					AvailableWidth = availableSize.Width,
					AvailableHeight = availableSize.Height
				};

				var ret = TSInteropMarshaller.InvokeJS<WindowManagerMeasureViewParams, WindowManagerMeasureViewReturn>("Uno:measureViewNative", parms);

				return new Size(ret.DesiredWidth, ret.DesiredHeight);
			}
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerMeasureViewParams
		{
			public IntPtr HtmlId;

			public double AvailableWidth;
			public double AvailableHeight;
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
			if (UseJavascriptEval)
			{
				SetStyles(htmlId, new[] { (name, value.ToString(CultureInfo.InvariantCulture)) });
			}
			else
			{
				var parms = new WindowManagerSetStyleDoubleParams
				{
					HtmlId = htmlId,
					Name = name,
					Value = value
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetStyleDoubleParams>("Uno:setStyleDoubleNative", parms);
			}
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

		#region SetRectanglePosition

		internal static void SetSvgElementRect(IntPtr htmlId, Rect rect)
		{
			if (UseJavascriptEval)
			{
				SetAttributes(
					htmlId,
					new[]{
						("x", rect.X.ToStringInvariant()),
						("y", rect.Y.ToStringInvariant()),
						("width", rect.Width.ToStringInvariant()),
						("height", rect.Height.ToStringInvariant())
					}
				);
			}
			else
			{
				var parms = new WindowManagerSetSvgElementRectParams
				{
					HtmlId = htmlId,
					X = rect.X,
					Y = rect.Y,
					Width = rect.Width,
					Height = rect.Height,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetSvgElementRectParams>("Uno:setSvgElementRect", parms);
			}
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
			if (UseJavascriptEval)
			{
				var stylesStr = string.Join(", ", styles.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
				var command = "Uno.UI.WindowManager.current.setStyle(\"" + htmlId + "\", {" + stylesStr + "}); ";

				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var pairs = new string[styles.Length * 2];

				for (int i = 0; i < styles.Length; i++)
				{
					pairs[i * 2 + 0] = styles[i].name;
					pairs[i * 2 + 1] = styles[i].value;
				}

				var parms = new WindowManagerSetStylesParams
				{
					HtmlId = htmlId,
					Pairs_Length = pairs.Length,
					Pairs = pairs,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetStylesParams>("Uno:setStyleNative", parms);
			}
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

		private static void SetArrangeProperties(IntPtr htmlId, bool requiresClipping)
		{
			if (!UseJavascriptEval)
			{
				throw new InvalidOperationException("This should only be called when UseJavascriptEval flag is set");
			}

			var command = "Uno.UI.WindowManager.current.setArrangeProperties(\"" + htmlId + "\", " + (requiresClipping ? "true" : "false") + "); ";

			WebAssemblyRuntime.InvokeJS(command);
		}

		#region SetUnsetCssClasses
		internal static void SetUnsetCssClasses(IntPtr htmlId, string[] cssClassesToSet, string[] cssClassesToUnset)
		{
			if (UseJavascriptEval)
			{
				var setClasses =
					cssClassesToSet == null
						? "null"
						: "[" +
						  string.Join(", ", cssClassesToSet
							  .Select(WebAssemblyRuntime.EscapeJs)
							  .Select(s => "\"" + s + "\""))
						  + "]";
				var unsetClasses =
					cssClassesToUnset == null
						? "null"
						: "[" +
						  string.Join(", ", cssClassesToUnset
							  .Select(WebAssemblyRuntime.EscapeJs)
							  .Select(s => "\"" + s + "\""))
						  + "]";
				var command = "Uno.UI.WindowManager.current.setUnsetClasses(" + htmlId + ", " + setClasses + ", " + unsetClasses + ");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetUnsetClassesParams
				{
					HtmlId = htmlId,
					CssClassesToSet = cssClassesToSet,
					CssClassesToSet_Length = cssClassesToSet?.Length ?? 0,
					CssClassesToUnset = cssClassesToUnset,
					CssClassesToUnset_Length = cssClassesToUnset?.Length ?? 0
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetUnsetClassesParams>("Uno:setUnsetClassesNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				var classes = string.Join(", ", cssClasses.Select(s => "\"" + s + "\""));
				var command = "Uno.UI.WindowManager.current.setClasses(" + htmlId + ", [" + classes + "], " + index + ");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetClassesParams
				{
					HtmlId = htmlId,
					CssClasses = cssClasses,
					CssClasses_Length = cssClasses.Length,
					Index = index
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetClassesParams>("Uno:setClassesNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				if (index != null)
				{
					var command = "Uno.UI.WindowManager.current.addView(" + htmlId + ", " + child + ", " + index.Value + ");";
					WebAssemblyRuntime.InvokeJS(command);
				}
				else
				{
					var command = "Uno.UI.WindowManager.current.addView(" + htmlId + ", " + child + ");";
					WebAssemblyRuntime.InvokeJS(command);
				}
			}
			else
			{
				var parms = new WindowManagerAddViewParams
				{
					HtmlId = htmlId,
					ChildView = child,
					Index = index ?? -1,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerAddViewParams>("Uno:addViewNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				var attributesStr = string.Join(", ", attributes.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
				var command = "Uno.UI.WindowManager.current.setAttributes(\"" + htmlId + "\", {" + attributesStr + "});";

				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var pairs = new string[attributes.Length * 2];

				for (int i = 0; i < attributes.Length; i++)
				{
					pairs[i * 2 + 0] = attributes[i].name;
					pairs[i * 2 + 1] = attributes[i].value;
				}

				var parms = new WindowManagerSetAttributesParams()
				{
					HtmlId = htmlId,
					Pairs_Length = pairs.Length,
					Pairs = pairs,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetAttributesParams>("Uno:setAttributesNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				var attributesStr = "\"" + name + "\": \"" + WebAssemblyRuntime.EscapeJs(value) + "\"";
				var command = "Uno.UI.WindowManager.current.setAttributes(\"" + htmlId + "\", {" + attributesStr + "});";

				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetAttributeParams()
				{
					HtmlId = htmlId,
					Name = name,
					Value = value,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetAttributeParams>("Uno:setAttributeNative", parms);
			}
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

		#region ClearAttribute
		internal static void RemoveAttribute(IntPtr htmlId, string name)
		{
			if (UseJavascriptEval)
			{
				var command = "Uno.UI.WindowManager.current.removeAttribute(\"" + htmlId + "\", \"" + name + "\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerRemoveAttributeParams()
				{
					HtmlId = htmlId,
					Name = name,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerRemoveAttributeParams>("Uno:removeAttributeNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				var command = $"Uno.UI.WindowManager.current.setName(\"{htmlId}\", \"{name}\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetNameParams()
				{
					HtmlId = htmlId,
					Name = name,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetNameParams>("Uno:setNameNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				var command = $"Uno.UI.WindowManager.current.setXUid(\"{htmlId}\", \"{name}\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetXUidParams()
				{
					HtmlId = htmlId,
					Uid = name,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetXUidParams>("Uno:setXUidNative", parms);
			}
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

		#region SetProperty

		internal static void SetProperty(IntPtr htmlId, (string name, string value)[] properties)
		{
			if (UseJavascriptEval)
			{
				var attributesStr = string.Join(", ", properties.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
				var command = "Uno.UI.WindowManager.current.setProperty(\"" + htmlId + "\", {" + attributesStr + "});";

				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var pairs = new string[properties.Length * 2];

				for (int i = 0; i < properties.Length; i++)
				{
					pairs[i * 2 + 0] = properties[i].name;
					pairs[i * 2 + 1] = properties[i].value;
				}

				var parms = new WindowManagerSetAttributesParams()
				{
					HtmlId = htmlId,
					Pairs_Length = pairs.Length,
					Pairs = pairs,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetAttributesParams>("Uno:setPropertyNative", parms);

			}
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

		#region RemoveView
		internal static void RemoveView(IntPtr htmlId, IntPtr childId)
		{
			if (UseJavascriptEval)
			{
				var command = "Uno.UI.WindowManager.current.removeView(" + htmlId + ", " + childId + ");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerRemoveViewParams
				{
					HtmlId = htmlId,
					ChildView = childId
				};

				TSInteropMarshaller.InvokeJS<WindowManagerRemoveViewParams>("Uno:removeViewNative", parms);
			}
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
			if (UseJavascriptEval)
			{
				var command = "Uno.UI.WindowManager.current.destroyView(" + htmlId + ");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerDestroyViewParams
				{
					HtmlId = htmlId
				};

				TSInteropMarshaller.InvokeJS<WindowManagerDestroyViewParams>("Uno:destroyViewNative", parms);
			}
		}

		[TSInteropMessage]
		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerDestroyViewParams
		{
			public IntPtr HtmlId;
		}
		#endregion

		#region ResetStyle
		internal static void ResetStyle(IntPtr htmlId, string[] names)
		{
			if (UseJavascriptEval)
			{
				if (names.Length == 1)
				{
					var command = "Uno.UI.WindowManager.current.resetStyle(\"" + htmlId + "\", [\"" + names[0] + "\"]);";
					WebAssemblyRuntime.InvokeJS(command);
				}
				else
				{
					var namesStr = string.Join(", ", names.Select(n => "\"" + n + "\""));
					var command = "Uno.UI.WindowManager.current.resetStyle(\"" + htmlId + "\", [\"" + namesStr + "\"]);";
					WebAssemblyRuntime.InvokeJS(command);
				}
			}
			else
			{
				var parms = new WindowManagerResetStyleParams()
				{
					HtmlId = htmlId,
					Styles = names,
					Styles_Length = names.Length,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerResetStyleParams>("Uno:resetStyleNative", parms);

			}
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
			if (UseJavascriptEval)
			{
				var onCapturePhaseStr = onCapturePhase ? "true" : "false";
				var cmd = $"Uno.UI.WindowManager.current.registerEventOnView(\"{htmlId}\", \"{eventName}\", {onCapturePhaseStr}, {eventExtractorId});";

				WebAssemblyRuntime.InvokeJS(cmd);
			}
			else
			{
				var parms = new WindowManagerRegisterEventOnViewParams()
				{
					HtmlId = htmlId,
					EventName = eventName,
					OnCapturePhase = onCapturePhase,
					EventExtractorId = eventExtractorId,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerRegisterEventOnViewParams>("Uno:registerEventOnViewNative", parms);
			}
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

			TSInteropMarshaller.InvokeJS<WindowManagerRegisterPointerEventsOnViewParams>("Uno:registerPointerEventsOnView", parms);
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
			if (UseJavascriptEval)
			{
				var sizeString = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.getBBox(" + htmlId + ");");
				var sizeParts = sizeString.Split(';');
				return new Rect(double.Parse(sizeParts[0]), double.Parse(sizeParts[1]), double.Parse(sizeParts[2]), double.Parse(sizeParts[3]));
			}
			else
			{
				var parms = new WindowManagerGetBBoxParams
				{
					HtmlId = htmlId
				};

				var ret = TSInteropMarshaller.InvokeJS<WindowManagerGetBBoxParams, WindowManagerGetBBoxReturn>("Uno:getBBoxNative", parms);

				return new Rect(ret.X, ret.Y, ret.Width, ret.Height);
			}
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
			if (UseJavascriptEval)
			{
				var escapedHtml = WebAssemblyRuntime.EscapeJs(html);

				var command = "Uno.UI.WindowManager.current.setHtmlContent(" + htmlId + ", \"" + escapedHtml + "\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetContentHtmlParams()
				{
					HtmlId = htmlId,
					Html = html,
				};

				TSInteropMarshaller.InvokeJS<WindowManagerSetContentHtmlParams>("Uno:setHtmlContentNative", parms);
			}
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

		internal static void ArrangeElement(IntPtr htmlId, Rect rect, bool clipToBounds, Rect? clipRect)
		{
			if (UseJavascriptEval)
			{
				var clipRect2 = clipRect != null
					? string.Format(
						CultureInfo.InvariantCulture,
						"rect({0}px,{1}px,{2}px,{3}px",
						clipRect.Value.Top,
						clipRect.Value.Right,
						clipRect.Value.Bottom,
						clipRect.Value.Left
					)
					: "";

				SetStyles(
					htmlId,
					new[] {
						("position", "absolute"),
						("top", rect.Top.ToString(CultureInfo.InvariantCulture) + "px"),
						("left", rect.Left.ToString(CultureInfo.InvariantCulture) + "px"),
						("width", rect.Width.ToString(CultureInfo.InvariantCulture) + "px"),
						("height", rect.Height.ToString(CultureInfo.InvariantCulture) + "px"),
						("clip", clipRect2)
					}
				);

				SetArrangeProperties(htmlId, clipToBounds);
			}
			else
			{
				var parms = new WindowManagerArrangeElementParams()
				{
					HtmlId = htmlId,
					Top = rect.Top,
					Left = rect.Left,
					Width = rect.Width,
					Height = rect.Height,
					ClipToBounds = clipToBounds
				};

				if (clipRect != null)
				{
					parms.Clip = true;
					parms.ClipTop = clipRect.Value.Top;
					parms.ClipLeft = clipRect.Value.Left;
					parms.ClipBottom = clipRect.Value.Bottom;
					parms.ClipRight = clipRect.Value.Right;
				}

				TSInteropMarshaller.InvokeJS<WindowManagerArrangeElementParams>("Uno:arrangeElementNative", parms);
			}
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
			public bool ClipToBounds;
		}


		#endregion

		#region GetClientViewSize

		internal static (Size clientSize, Size offsetSize) GetClientViewSize(IntPtr htmlId)
		{
			if (UseJavascriptEval)
			{
				var sizeString = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.getClientViewSize(" + htmlId + ");");
				var sizeParts = sizeString.Split(';');

				return (
					clientSize: new Size(double.Parse(sizeParts[0]), double.Parse(sizeParts[1])),
					offsetSize: new Size(double.Parse(sizeParts[2]), double.Parse(sizeParts[3]))
				);
			}
			else
			{
				var parms = new WindowManagerGetClientViewSizeParams
				{
					HtmlId = htmlId
				};

				var ret = TSInteropMarshaller.InvokeJS<WindowManagerGetClientViewSizeParams, WindowManagerGetClientViewSizeReturn>("Uno:getClientViewSizeNative", parms);

				return (
					clientSize: new Size(ret.ClientWidth, ret.ClientHeight),
					offsetSize: new Size(ret.OffsetWidth, ret.OffsetHeight)
				);
			}
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

		#region ScrollTo
		internal static void ScrollTo(IntPtr htmlId, double? left, double? top, bool disableAnimation)
		{
			var parms = new WindowManagerScrollToOptionsParams
			{
				HtmlId = htmlId,
				HasLeft = left.HasValue,
				Left = left ?? 0,
				HasTop = top.HasValue,
				Top = top ?? 0,
				DisableAnimation = disableAnimation
			};

			TSInteropMarshaller.InvokeJS<WindowManagerScrollToOptionsParams>("Uno:scrollTo", parms);
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
	}
}
