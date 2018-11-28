using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Extensions;
using Uno.Foundation;
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
		private const UnmanagedType LPUTF8Str = (UnmanagedType)48;

		/// <summary>
		/// Prints the actual offsets of the structures present in <see cref="WindowManagerInterop"/> for debugging purposes.
		/// </summary>
		internal static void GenerateTSMarshallingLayouts()
		{
			Console.WriteLine("Generating layouts");

			foreach (var p in typeof(WindowManagerInterop).GetNestedTypes(System.Reflection.BindingFlags.NonPublic).Where(t => t.IsValueType))
			{
				var sb = new StringBuilder();

				Console.WriteLine($"class {p.Name}:");

				foreach (var field in p.GetFields())
				{
					var fieldOffset = Marshal.OffsetOf(p, field.Name);
					Console.WriteLine($"\t{field.Name} : {fieldOffset}");
				}
			}
		}

		private static void InvokeJS<TParam>(string methodName, TParam paramStruct)
		{
			var pParms = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TParam)));

			try
			{
				Marshal.StructureToPtr(paramStruct, pParms, false);

				var ret = WebAssemblyRuntime.InvokeJSUnmarshalled<IntPtr, bool>(methodName, pParms);
			}
			finally
			{
				Marshal.DestroyStructure(pParms, typeof(TParam));
				Marshal.FreeHGlobal(pParms);
			}
		}

		private static TRet InvokeJS<TParam, TRet>(string methodName, TParam paramStruct) where TRet:new()
		{
			var pParms = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TParam)));
			var pReturnValue = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TRet)));

			var returnValue = new TRet();

			try
			{
				Marshal.StructureToPtr(paramStruct, pParms, false);
				Marshal.StructureToPtr(returnValue, pReturnValue, false);

				var ret = WebAssemblyRuntime.InvokeJSUnmarshalled<IntPtr, IntPtr, bool>(methodName, pParms, pReturnValue);

				returnValue = (TRet)Marshal.PtrToStructure(pReturnValue, typeof(TRet));
				return returnValue;
			}
			finally
			{
				Marshal.DestroyStructure(pParms, typeof(TParam));
				Marshal.FreeHGlobal(pParms);

				Marshal.DestroyStructure(pReturnValue, typeof(TRet));
				Marshal.FreeHGlobal(pReturnValue);
			}
		}

		#region CreateContent
		internal static void CreateContent(IntPtr htmlId, string htmlTag, IntPtr handle, string fullName, bool htmlTagIsSvg, bool isFrameworkElement, bool isFocusable, string[] classes)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var isSvgStr = htmlTagIsSvg ? "true" : "false";
				var isFrameworkElementStr = isFrameworkElement ? "true" : "false";
				var isFocusableStr = isFocusable ? "true" : "false"; // by default all control are not focusable, it has to be change latter by the control itself
				var classesParam = classes.Select(c => $"\"{c}\"").JoinBy(",");

				WebAssemblyRuntime.InvokeJS(
					"Uno.UI.WindowManager.current.createContent({" +
					"id:\"" + htmlId + "\"," +
					"tagName:\"" + htmlTag + "\", " +
					"handle:" + handle + ", " +
					"type:\"" + fullName + "\", " +
					"isSvg:" + isSvgStr + ", " +
					"isFrameworkElement:" + isFrameworkElementStr + ", " +
					"isFocusable:" + isFocusableStr + ", " +
					"classes:[" + classesParam + "]" +
					"});");
			}
			else
			{
				var parms = new WindowManagerCreateContentParams
				{
					HtmlId = htmlId,
					TagName = htmlTag,
					Handle = handle,
					Type = fullName,
					IsSvg = htmlTagIsSvg,
					IsFrameworkElement = isFrameworkElement,
					IsFocusable = isFocusable,
					Classes_Length = classes.Length,
					Classes = classes,
				};

				InvokeJS<WindowManagerCreateContentParams, bool>("Uno:createContentNative", parms);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerCreateContentParams
		{
			public IntPtr HtmlId;

			[MarshalAs(UnmanagedType.LPStr)]
			public string TagName;

			public IntPtr Handle;

			[MarshalAs(UnmanagedType.LPStr)]
			public string Type;

			public bool IsSvg;
			public bool IsFrameworkElement;
			public bool IsFocusable;

			public int Classes_Length;

			[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)]
			public string[] Classes;
		}

		#endregion

		#region MeasureView
		internal static Size MeasureView(IntPtr htmlId, Size availableSize)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var w = double.IsInfinity(availableSize.Width) ? "null" : availableSize.Width.ToStringInvariant();
				var h = double.IsInfinity(availableSize.Height) ? "null" : availableSize.Height.ToStringInvariant();

				var command = "Uno.UI.WindowManager.current.measureView(\"" + htmlId + "\", \"" + w + "\", \"" + h + "\");";
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

				var ret = InvokeJS<WindowManagerMeasureViewParams, WindowManagerMeasureViewReturn>("Uno:measureViewNative", parms);

				return new Size(ret.DesiredWidth, ret.DesiredHeight);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerMeasureViewParams
		{
			public IntPtr HtmlId;

			public double AvailableWidth;
			public double AvailableHeight;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct WindowManagerMeasureViewReturn
		{
			public double DesiredWidth;
			public double DesiredHeight;
		}


		#endregion

		#region SetStyles

		internal static void SetStyles(IntPtr htmlId, (string name, string value)[] styles, bool setAsArranged = false)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var setAsArrangeString = setAsArranged ? "true" : "false";
				var stylesStr = string.Join(", ", styles.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
				var command = "Uno.UI.WindowManager.current.setStyle(\"" + htmlId + "\", {" + stylesStr + "}," + setAsArrangeString + "); ";

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
					SetAsArranged = setAsArranged,
					Pairs_Length = pairs.Length,
					Pairs = pairs,
				};

				InvokeJS<WindowManagerSetStylesParams>("Uno:setStyleNative", parms);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetStylesParams
		{
			public IntPtr HtmlId;

			public bool SetAsArranged;

			public int Pairs_Length;

			public string[] Pairs;
		}

		#endregion

		#region AddView

		internal static void AddView(IntPtr htmlId, IntPtr child, int? index = null)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				if (index != null)
				{
					var command = "Uno.UI.WindowManager.current.addView(\"" + htmlId + "\", \"" + child + "\", " + index.Value + ");";
					WebAssemblyRuntime.InvokeJS(command);
				}
				else
				{
					var command = "Uno.UI.WindowManager.current.addView(\"" + htmlId + "\", \"" + child + "\");";
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

				InvokeJS<WindowManagerAddViewParams>("Uno:addViewNative", parms);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerAddViewParams
		{
			public IntPtr HtmlId;
			public IntPtr ChildView;
			public int Index;
		}

		#endregion

		#region SetAttribute

		internal static void SetAttribute(IntPtr htmlId, (string name, string value)[] attributes)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var attributesStr = string.Join(", ", attributes.Select(s => "\"" + s.name + "\": \"" + WebAssemblyRuntime.EscapeJs(s.value) + "\""));
				var command = "Uno.UI.WindowManager.current.setAttribute(\"" + htmlId + "\", {" + attributesStr + "});";

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

				var parms = new WindowManagerSetAttributeParams()
				{
					HtmlId = htmlId,
					Pairs_Length = pairs.Length,
					Pairs = pairs,
				};

				InvokeJS<WindowManagerSetAttributeParams>("Uno:setAttributeNative", parms);
			}
		}


		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetAttributeParams
		{
			public IntPtr HtmlId;

			public int Pairs_Length;

			public string[] Pairs;
		}

		#endregion

		#region SetName

		internal static void SetName(IntPtr htmlId, string name)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
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

				InvokeJS<WindowManagerSetNameParams>("Uno:setNameNative", parms);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetNameParams
		{
			public IntPtr HtmlId;

			public string Name;
		}
		#endregion

		#region SetProperty

		internal static void SetProperty(IntPtr htmlId, (string name, string value)[] properties)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
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

				var parms = new WindowManagerSetAttributeParams()
				{
					HtmlId = htmlId,
					Pairs_Length = pairs.Length,
					Pairs = pairs,
				};

				InvokeJS<WindowManagerSetAttributeParams>("Uno:setPropertyNative", parms);

			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetPropertyParams
		{
			public IntPtr HtmlId;

			public int Pairs_Length;

			public string[] Pairs;
		}

		#endregion

		#region RemoveView
		internal static void RemoveView(IntPtr htmlId, IntPtr childId)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var command = "Uno.UI.WindowManager.current.removeView(\"" + htmlId + "\", \"" + childId + "\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerRemoveViewParams
				{
					HtmlId = htmlId,
					ChildView = childId
				};

				InvokeJS<WindowManagerRemoveViewParams>("Uno:removeViewNative", parms);
			}
		}

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
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var command = "Uno.UI.WindowManager.current.destroyView(\"" + htmlId + "\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerDestroyViewParams
				{
					HtmlId = htmlId
				};

				InvokeJS<WindowManagerDestroyViewParams>("Uno:destroyViewNative", parms);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerDestroyViewParams
		{
			public IntPtr HtmlId;
		}
		#endregion

		#region ResetStyle
		internal static void ResetStyle(IntPtr htmlId, string[] names)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
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

				InvokeJS<WindowManagerResetStyleParams>("Uno:resetStyleNative", parms);

			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerResetStyleParams
		{
			public IntPtr HtmlId;

			public int Styles_Length;

			public string[] Styles;
		}
		#endregion

		#region RegisterEventOnView
		internal static void RegisterEventOnView(IntPtr htmlId, string eventName, bool onCapturePhase, string eventFilterName, string eventExtractorName)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var onCapturePhaseStr = onCapturePhase ? "true" : "false";
				var cmd = $"Uno.UI.WindowManager.current.registerEventOnView(\"{htmlId}\", \"{eventName}\", {onCapturePhaseStr}, \"{eventFilterName}\", \"{eventExtractorName}\");";

				WebAssemblyRuntime.InvokeJS(cmd);
			}
			else
			{
				var parms = new WindowManagerRegisterEventOnViewParams()
				{
					HtmlId = htmlId,
					EventName = eventName,
					OnCapturePhase = onCapturePhase,
					EventFilterName = eventFilterName,
					EventExtractorName = eventExtractorName,
				};

				InvokeJS<WindowManagerRegisterEventOnViewParams>("Uno:registerEventOnViewNative", parms);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerRegisterEventOnViewParams
		{
			public IntPtr HtmlId;

			public string EventName;

			public bool OnCapturePhase;

			public string EventFilterName;

			public string EventExtractorName;
		}
		#endregion

		#region GetBBox

		internal static Rect GetBBox(IntPtr htmlId)
		{
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var sizeString = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.getBBox(\"" + htmlId + "\");");
				var sizeParts = sizeString.Split(';');
				return new Rect(double.Parse(sizeParts[0]), double.Parse(sizeParts[1]), double.Parse(sizeParts[2]), double.Parse(sizeParts[3]));
			}
			else
			{
				var parms = new WindowManagerGetBBoxParams
				{
					HtmlId = htmlId
				};

				var ret = InvokeJS<WindowManagerGetBBoxParams, WindowManagerGetBBoxReturn>("Uno:getBBoxNative", parms);

				return new Rect(ret.X, ret.Y, ret.Width, ret.Height);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerGetBBoxParams
		{
			public IntPtr HtmlId;
		}


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
			if (!WebAssemblyRuntime.IsWebAssembly)
			{
				var escapedHtml = WebAssemblyRuntime.EscapeJs(html);

				var command = "Uno.UI.WindowManager.current.setHtmlContent(\"" + htmlId + "\", \"" + escapedHtml + "\");";
				WebAssemblyRuntime.InvokeJS(command);
			}
			else
			{
				var parms = new WindowManagerSetContentHtmlParams()
				{
					HtmlId = htmlId,
					Html = html,
				};

				InvokeJS<WindowManagerSetContentHtmlParams>("Uno:setHtmlContentNative", parms);
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		private struct WindowManagerSetContentHtmlParams
		{
			public IntPtr HtmlId;
			public string Html;
		}

		#endregion
	}
}
