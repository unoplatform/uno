// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the SharedHelpers.cpp file from WinUI controls.
//

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.System;
using Windows.System.Profile;
using Windows.System.Threading;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Xaml.Controls._Tracing;

#if HAS_UNO_WINUI
using ITextSelection = Microsoft.UI.Text.ITextSelection;
#else
using ITextSelection = Windows.UI.Text.ITextSelection;
#endif

namespace Uno.UI.Helpers.WinUI
{
	internal class SharedHelpers
	{
#if HAS_UNO && !(IS_UNIT_TESTS || __NETSTD_REFERENCE__)
		private static bool s_isOnXboxInitialized;
		private static bool s_isOnXbox;
#endif

		private static bool s_isMouseModeEnabledInitialized;
		private static bool s_isMouseModeEnabled;

		static SharedHelpers()
		{
			isApiContractVxAvailable = new Dictionary<ushort, bool>();
		}

		public static bool IsSystemDll() => false;

		public static bool IsAnimationsEnabled()
		{
			UISettings uiSettings = new UISettings();
			return uiSettings.AnimationsEnabled;
		}

		public static bool IsInDesignMode()
		{
			// Design mode V2 can do many things that V1 could not do. Codepaths that were checking for design mode should generally be
			// let through if V2 is enabled.
			return IsInDesignModeV1();
		}

		static bool? s_isInDesignModeV1;
		public static bool IsInDesignModeV1()
		{
			if (s_isInDesignModeV1 == null)
			{
				s_isInDesignModeV1 = Windows.ApplicationModel.DesignMode.DesignModeEnabled && !IsInDesignModeV2();
			}

			return s_isInDesignModeV1.Value;
		}

		internal static void RaiseAutomationPropertyChangedEvent<T>(UIElement element, T oldValue, T newValue)
		{
			AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(element);
			if (peer != null)
			{
				peer.RaisePropertyChangedEvent(
					ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
					oldValue,
					newValue);
			}
		}

		static bool? s_isInDesignModeV2;
		public static bool IsInDesignModeV2()
		{
			if (s_isInDesignModeV2 == null)
			{
				s_isInDesignModeV2 = IsRS3OrHigher() && Windows.ApplicationModel.DesignMode.DesignMode2Enabled;
			}
			return s_isInDesignModeV2.Value;
		}

		public static bool ShouldUseDynamicScrollbars()
		{
			if (s_dynamicScrollbarsDirty)
			{
				UISettings uiSettings = new UISettings();
				s_dynamicScrollbars = uiSettings.AutoHideScrollBars;
				s_dynamicScrollbarsDirty = false;
			}

			return s_dynamicScrollbars;
		}

		// logical helpers
		public static bool Is21H1OrHigher()
		{
			return IsAPIContractV14Available();
		}

		public static bool IsVanadiumOrHigher()
		{
			return IsAPIContractV9Available();
		}

		public static bool Is19H1OrHigher()
		{
			return IsAPIContractV8Available();
		}

		public static bool IsRS5OrHigher()
		{
			return IsAPIContractV7Available();
		}

		public static bool IsRS4OrHigher()
		{
			return IsAPIContractV6Available();
		}

		public static bool IsRS3OrHigher()
		{
			return IsAPIContractV5Available();
		}

		public static bool IsRS2OrHigher()
		{
			return IsAPIContractV4Available();
		}

		public static bool IsRS1OrHigher()
		{
			return IsAPIContractV3Available();
		}

		public static bool IsRS1()
		{
			return IsAPIContractV3Available() && !IsAPIContractV4Available();
		}

		public static bool IsTH2OrLower()
		{
			return !IsAPIContractV3Available();
		}

		static bool? s_IsXamlCompositionBrushBaseAvailable_isAvailable;

		public static bool IsXamlCompositionBrushBaseAvailable()
		{
			if (s_IsXamlCompositionBrushBaseAvailable_isAvailable == null)
			{
				s_IsXamlCompositionBrushBaseAvailable_isAvailable = IsRS3OrHigher() || ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.XamlCompositionBrushBase, Uno.UI");
			}

			// On RS3 we know XamlCompositionBrushBase was always present, so short circuit the check there.
			return s_IsXamlCompositionBrushBaseAvailable_isAvailable.Value;
		}

		static bool? s_movesLightFromRSVToRootVisual;

		public static bool DoesXamlMoveRSVLightToRootVisual()
		{
			if (s_movesLightFromRSVToRootVisual == null)
			{
				s_movesLightFromRSVToRootVisual = IsSystemDll() || IsRS3OrHigher();
			}
			// In RS3 we made a change where WUX internally sets lights on the RootScrollViewer on the RootVisual instead. If that
			// happens, then we don't need to attach lights to the other roots.
			return s_movesLightFromRSVToRootVisual.Value;
		}

		static bool? s_DoesListViewItemPresenterVSMWork_isAvailable;

		public static bool DoesListViewItemPresenterVSMWork()
		{
			if (s_DoesListViewItemPresenterVSMWork_isAvailable == null)
			{
				s_DoesListViewItemPresenterVSMWork_isAvailable = IsSystemDll() || IsRS3OrHigher();
			}
			// The fix to make ListViewItemPresenter's VSM work was done at the same time as when the RevealListViewItemPresenter type was *removed* from windows.
			// Also check if RevealBorderBrush was present because RS3 was around for a bit before we added Reveal in.
			return s_DoesListViewItemPresenterVSMWork_isAvailable.Value;
		}

		static bool? s_IsCoreWindowActivationModeAvailable_isAvailable;
		public static bool IsCoreWindowActivationModeAvailable()
		{
			if (s_IsCoreWindowActivationModeAvailable_isAvailable == null)
			{
				s_IsCoreWindowActivationModeAvailable_isAvailable = IsSystemDll() || IsRS3OrHigher();
			}
			// In RS3 we got CoreWindow.ActivationMode API which can be queried for window activation state,
			// and particularly in a Component UI host appto check if the compoenent is active (while host isn't).
			return s_IsCoreWindowActivationModeAvailable_isAvailable.Value;
		}

		static bool? s_isFlyoutShowOptionsAvailable = false;
		public static bool IsFlyoutShowOptionsAvailable()
		{
			if (s_isFlyoutShowOptionsAvailable == null)
			{
				s_isFlyoutShowOptionsAvailable =
					IsSystemDll() ||
					Is19H1OrHigher() ||
					ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Primitives.FlyoutShowOptions, Uno.UI");
			}
			return s_isFlyoutShowOptionsAvailable.Value;
		}

		// VisualInteractionSourceRedirectionMode.PointerWheelOnly and VisualInteractionSourceRedirectionMode.CapableTouchpadAndPointerWheel
		// were introduced late in RS4.
		static bool? s_areInteractionTrackerPointerWheelRedirectionModesAvailable;
		public static bool AreInteractionTrackerPointerWheelRedirectionModesAvailable()
		{
			if (s_areInteractionTrackerPointerWheelRedirectionModesAvailable == null)
			{
				s_areInteractionTrackerPointerWheelRedirectionModesAvailable =
					IsSystemDll() ||
					IsRS5OrHigher() ||
					(IsRS4OrHigher() && ApiInformation.IsEnumNamedValuePresent("Microsoft.UI.Composition.Interactions.VisualInteractionSourceRedirectionMode, Uno.UI.Composition", "PointerWheelOnly"));
			}

			return s_areInteractionTrackerPointerWheelRedirectionModesAvailable.Value;
		}

		static bool? s_isScrollViewerReduceViewportForCoreInputViewOcclusionsAvailable;
		public static bool IsScrollViewerReduceViewportForCoreInputViewOcclusionsAvailable()
		{
			if (s_isScrollViewerReduceViewportForCoreInputViewOcclusionsAvailable == null)
			{
				s_isScrollViewerReduceViewportForCoreInputViewOcclusionsAvailable =
					IsSystemDll() ||
					Is19H1OrHigher() ||
					ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.ScrollViewer, Uno.UI", "ReduceViewportForCoreInputViewOcclusions");
			}
			return s_isScrollViewerReduceViewportForCoreInputViewOcclusionsAvailable.Value;
		}

		static bool? s_isScrollContentPresenterSizesContentToTemplatedParentAvailable;
		public static bool IsScrollContentPresenterSizesContentToTemplatedParentAvailable()
		{
			if (s_isScrollContentPresenterSizesContentToTemplatedParentAvailable == null)
			{
				s_isScrollContentPresenterSizesContentToTemplatedParentAvailable =
					IsSystemDll() ||
					Is19H1OrHigher() ||
					ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.ScrollContentPresenter, Uno.UI", "SizesContentToTemplatedParent");
			}
			return s_isScrollContentPresenterSizesContentToTemplatedParentAvailable.Value;
		}

		static bool? s_isBringIntoViewOptionsVerticalAlignmentRatioAvailable;
		public static bool IsBringIntoViewOptionsVerticalAlignmentRatioAvailable()
		{
			if (s_isBringIntoViewOptionsVerticalAlignmentRatioAvailable == null)
			{
				s_isBringIntoViewOptionsVerticalAlignmentRatioAvailable =
					IsRS4OrHigher() ||
					ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.BringIntoViewOptions, Uno.UI", "VerticalAlignmentRatio");
			}
			return s_isBringIntoViewOptionsVerticalAlignmentRatioAvailable.Value;
		}

		static bool? s_isFrameworkElementInvalidateViewportAvailable;
		public static bool IsFrameworkElementInvalidateViewportAvailable()
		{

			if (s_isFrameworkElementInvalidateViewportAvailable == null)
			{
				s_isFrameworkElementInvalidateViewportAvailable = IsSystemDll() || IsRS5OrHigher();
			}
			return s_isFrameworkElementInvalidateViewportAvailable.Value;
		}

		static bool? s_isDisplayRegionGetForCurrentViewAvailable;
		public static bool IsDisplayRegionGetForCurrentViewAvailable()
		{
			if (s_isDisplayRegionGetForCurrentViewAvailable == null)
			{
				s_isDisplayRegionGetForCurrentViewAvailable =
					Is19H1OrHigher() ||
					ApiInformation.IsMethodPresent("Windows.ApplicationModel.Core.DisplayRegion, Uno", "GetForCurrentView");
			}
			return s_isDisplayRegionGetForCurrentViewAvailable.Value;
		}

		static bool s_areFacadesAvailable;
		public static bool IsTranslationFacadeAvailable(UIElement element)
		{
			// s_areFacadesAvailable = (element.try_as<Microsoft.UI.Xaml.IUIElement9>() != null)
			s_areFacadesAvailable = true; // (element is Microsoft.UI.Xaml.IUIElement9>() != null)
			return s_areFacadesAvailable;
		}

		static bool? s_IsIconSourceElementAvailable_isAvailable;
		public static bool IsIconSourceElementAvailable()
		{
			if (s_IsIconSourceElementAvailable_isAvailable == null)
			{
				s_IsIconSourceElementAvailable_isAvailable =
					IsSystemDll() ||
					Is19H1OrHigher() ||
					ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Controls.IconSourceElement, Uno.UI");
			}
			return s_IsIconSourceElementAvailable_isAvailable.Value;
		}

		static bool? s_IsStandardUICommandAvailable_isAvailable;
		public static bool IsStandardUICommandAvailable()
		{
			if (s_IsStandardUICommandAvailable_isAvailable == null)
			{
				s_IsStandardUICommandAvailable_isAvailable =
					IsSystemDll() ||
					Is19H1OrHigher() ||
					(ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Input.XamlUICommand, Uno.UI") &&
						ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Input.StandardUICommand, Uno.UI"));
			}
			return s_IsStandardUICommandAvailable_isAvailable.Value;
		}

		static bool? s_IsXamlRootAvailable;
		public static bool IsXamlRootAvailable()
		{
			if (s_IsXamlRootAvailable == null)
			{
				s_IsXamlRootAvailable =
				   IsSystemDll() ||
				   IsVanadiumOrHigher() ||
				   ApiInformation.IsTypePresent("Microsoft.UI.Xaml.XamlRoot, Uno.UI");

			}
			return s_IsXamlRootAvailable.Value;
		}

		static bool? s_isThemeShadowAvailable;
		static public bool IsThemeShadowAvailable()
		{
			if (s_isThemeShadowAvailable == null)
			{
				//TODO: Uno specific - had to change condition, as VanadiumOrHigher is available, but Theme Shadow is not implemented in Uno
				s_isThemeShadowAvailable =
					(IsSystemDll() ||
					IsVanadiumOrHigher()) &&
					ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.ThemeShadow, Uno.UI");
			}
			return s_isThemeShadowAvailable.Value;
		}

		static bool? s_IsIsLoadedAvailable;
		static public bool IsIsLoadedAvailable()
		{
			if (s_IsIsLoadedAvailable == null)
			{
				s_IsIsLoadedAvailable =
					IsSystemDll() ||
					IsRS5OrHigher() ||
					ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.FrameworkElement, Uno.UI", "IsLoaded");
			}
			return s_IsIsLoadedAvailable.Value;
		}

		private static bool s_dynamicScrollbarsDirty = true;
		private static bool s_dynamicScrollbars;
		private static readonly Dictionary<ushort, bool> isApiContractVxAvailable;

		public static bool IsAPIContractVxAvailable(ushort apiVersion)
		{
			// Uno specific: WinUI caches using static variables inside of a template function,
			// which creates a separate cache for eache apiVersion value. Instead, we use a dictionary
			// for the same functionality.
			if (!isApiContractVxAvailable.TryGetValue(apiVersion, out var available))
			{
				available =
					IsSystemDll() ?
					true :
					ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", apiVersion);
				isApiContractVxAvailable[apiVersion] = available;
			}

			return available;
		}

		// base helpers
		public static bool IsAPIContractV14Available()
		{
			return IsAPIContractVxAvailable(14);
		}

		public static bool IsAPIContractV9Available()
		{
			return IsAPIContractVxAvailable(9);
		}

		public static bool IsAPIContractV8Available()
		{
			return IsAPIContractVxAvailable(8);
		}

		public static bool IsAPIContractV7Available()
		{
			return IsAPIContractVxAvailable(7);
		}

		public static bool IsAPIContractV6Available()
		{
			return IsAPIContractVxAvailable(6);
		}

		public static bool IsAPIContractV5Available()
		{
			return IsAPIContractVxAvailable(5);
		}


		public static bool IsAPIContractV4Available()
		{
			return IsAPIContractVxAvailable(4);
		}

		public static bool IsAPIContractV3Available()
		{
			return IsAPIContractVxAvailable(3);
		}

		public static bool IsInFrameworkPackage()
		{
			return false;
		}

		// Platform scale helpers
		public static Rect ConvertDipsToPhysical(UIElement xamlRootReference, Rect dipsRect)
		{
			try
			{
				var scaleFactor = (float)xamlRootReference.XamlRoot.RasterizationScale;

				return new Rect()
				{
					X = dipsRect.X * scaleFactor,
					Y = dipsRect.Y * scaleFactor,
					Width = dipsRect.Width * scaleFactor,
					Height = dipsRect.Height * scaleFactor
				};
			}
			catch (Exception)
			{
				// Calling GetForCurrentView on threads without a CoreWindow throws an error. This comes up in places like LogonUI.
				// In this circumstance, we'll just always expand down, since we can't get bounds information.
			}

			return dipsRect;
		}

		public static Rect ConvertPhysicalToDips(UIElement xamlRootReference, Rect physicalRect)
		{
			try
			{
				var scaleFactor = xamlRootReference.XamlRoot.RasterizationScale;

				return new Rect
				{
					X = physicalRect.X / scaleFactor,
					Y = physicalRect.Y / scaleFactor,
					Width = physicalRect.Width / scaleFactor,
					Height = physicalRect.Height / scaleFactor
				};
			}
			catch (Exception)
			{
				// Calling GetForCurrentView on threads without a CoreWindow throws an error. This comes up in places like LogonUI.
				// In this circumstance, we'll just always expand down, since we can't get bounds information.
			}

			return physicalRect;
		}

		public static bool IsOnXbox()
		{
#if HAS_UNO && !(IS_UNIT_TESTS || __NETSTD_REFERENCE__)
			if (!s_isOnXboxInitialized)
			{
				var deviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily;
				s_isOnXbox = (deviceFamily == "Windows.Xbox")
					|| (deviceFamily == "Windows.XBoxSRA")
					|| (deviceFamily == "Windows.XBoxERA");
				s_isOnXboxInitialized = true;
			}
			return s_isOnXbox;
#else
			return false;
#endif
		}

		// Be careful to use this API.
		// If IsTH2OrLower(), it return false;
		public static bool IsMouseModeEnabled()
		{
			if (!s_isMouseModeEnabledInitialized)
			{
				if (IsRS1OrHigher())
				{
					// RequiresPointerMode is in Windows.Foundation.UniversalApiContract (introduced v3)
					s_isMouseModeEnabled = Application.Current.RequiresPointerMode
						== ApplicationRequiresPointerMode.Auto;
				}
				else
				{
					s_isMouseModeEnabled = false;
				}
				s_isMouseModeEnabledInitialized = true;
			}
			return s_isMouseModeEnabled;
		}

		public static void ScheduleActionAfterWait(
			Action action,
			uint millisecondWait)
		{
			if (DispatcherQueue.GetForCurrentThread() is { } dispatcherQueue)
			{
				// The callback that is given to CreateTimer is called off of the UI thread.
				// In order to make this useful by making it so we can interact with XAML objects,
				// we'll use the dispatcher to first post our work to the UI thread before executing it.
				ThreadPoolTimer.CreateTimer(t =>
				{
					dispatcherQueue.TryEnqueue(new DispatcherQueueHandler(action));
				},
				TimeSpan.FromMilliseconds(millisecondWait));
			}
		}

		// Stream helpers
		//InMemoryRandomAccessStream CreateStreamFromBytes(const array_view<const byte>& bytes)
		//{
		//	InMemoryRandomAccessStream stream;
		//	DataWriter writer(stream);

		//	writer.WriteBytes(array_view<const byte>(bytes));
		//	SyncWait(writer.StoreAsync());
		//	SyncWait(writer.FlushAsync());
		//	writer.DetachStream();
		//	writer.Close();

		//	stream.Seek(0);

		//	return stream;
		//}

		public static void QueueCallbackForCompositionRendering(Action callback)
		{
			//TODO:MZ:Make this work on non-Skia targets!
			try
			{
				void OnRender(object sender, object e)
				{
					// Detach event or Rendering will keep calling us back.
					CompositionTarget.Rendering -= OnRender;
					callback();
				}
				CompositionTarget.Rendering += OnRender;
			}
			catch (Exception)
			{
				// DirectUI.CompositionTarget.add_Rendering can fail with RPC_E_WRONG_THREAD if called while the Xaml Core is being shutdown,
				// and there is evidence from Watson that such calls are made in real apps (see Bug 13554197).
				// Since the core is being shutdown, we no longer care about whatever work we wanted to defer to CT.Rendering, so ignore this error.
				//if (e.to_abi() != RPC_E_WRONG_THREAD) { throw; }
				throw;
			}
		}

		// Rect helpers
		public static bool DoesRectContainPoint(Rect rect, Point point)
			=> RectHelper.Contains(rect, point);

		// Returns TRUE if either rect is empty or the rects
		// have an empty intersection.
		public static bool DoRectsIntersect(
			Rect rect1,
			Rect rect2)
		{
			var doIntersect =
				!(rect1.Width <= 0 || rect1.Height <= 0 || rect2.Width <= 0 || rect2.Height <= 0) &&
				(rect2.X <= rect1.X + rect1.Width) &&
				(rect2.X + rect2.Width >= rect1.X) &&
				(rect2.Y <= rect1.Y + rect1.Height) &&
				(rect2.Y + rect2.Height >= rect1.Y);
			return doIntersect;
		}

		// When checkVisibility is True, IsAncestor additionally checks if any UIElement from the 'child'
		// to the 'parent' chain is Collapsed. It returns False when that is the case.
		public static bool IsAncestor(
			DependencyObject child,
			DependencyObject parent,
			bool checkVisibility = false)
		{
			if (child == null || parent == null || child == parent)
			{
				return false;
			}

			if (checkVisibility)
			{
				UIElement parentAsUIE = parent as UIElement;

				if (parentAsUIE != null && parentAsUIE.Visibility == Visibility.Collapsed)
				{
					return false;
				}

				UIElement childAsUIE = child as UIElement;

				if (childAsUIE != null && childAsUIE.Visibility == Visibility.Collapsed)
				{
					return false;
				}
			}

			DependencyObject parentTemp = VisualTreeHelper.GetParent(child);
			while (parentTemp != null)
			{
				if (checkVisibility)
				{
					UIElement parentTempAsUIE = parentTemp as UIElement;

					if (parentTempAsUIE != null && parentTempAsUIE.Visibility == Visibility.Collapsed)
					{
						return false;
					}
				}

				if (parentTemp == parent)
				{
					return true;
				}

				parentTemp = VisualTreeHelper.GetParent(parentTemp);
			}

			return false;
		}

		public static bool IsFocusableElement(
			UIElement element)
		{
			if (element.Visibility == Visibility.Collapsed)
			{
				return false;
			}

			var control = element as Control;

			return control is not null && (control.IsEnabled || control.AllowFocusWhenDisabled) && control.IsTabStop;
		}

		public static IconElement MakeIconElementFrom(Microsoft.UI.Xaml.Controls.IconSource iconSource)
		{
			if (iconSource is Microsoft.UI.Xaml.Controls.FontIconSource fontIconSource)
			{
				FontIcon fontIcon = new FontIcon();

				fontIcon.Glyph = fontIconSource.Glyph;
				fontIcon.FontSize = fontIconSource.FontSize;

				if (fontIconSource.FontFamily != null)
				{
					fontIcon.FontFamily = fontIconSource.FontFamily;
				}

				if (fontIconSource.Foreground != null)
				{
					fontIcon.Foreground = fontIconSource.Foreground;
				}

				fontIcon.FontWeight = fontIconSource.FontWeight;
				fontIcon.FontStyle = fontIconSource.FontStyle;
				fontIcon.IsTextScaleFactorEnabled = fontIconSource.IsTextScaleFactorEnabled;
				fontIcon.MirroredWhenRightToLeft = fontIconSource.MirroredWhenRightToLeft;

				return fontIcon;
			}
			else if (iconSource is Microsoft.UI.Xaml.Controls.SymbolIconSource symbolIconSource)
			{
				SymbolIcon symbolIcon = new SymbolIcon();
				symbolIcon.Symbol = symbolIconSource.Symbol;

				if (symbolIconSource.Foreground != null)
				{
					symbolIcon.Foreground = symbolIconSource.Foreground;
				}

				return symbolIcon;
			}
			else if (iconSource is Microsoft.UI.Xaml.Controls.BitmapIconSource bitmapIconSource)
			{
				BitmapIcon bitmapIcon = new BitmapIcon();

				if (bitmapIconSource.UriSource != null)
				{
					bitmapIcon.UriSource = bitmapIconSource.UriSource;
				}

				if (bitmapIconSource.Foreground != null)
				{
					bitmapIcon.Foreground = bitmapIconSource.Foreground;
				}

				if (IsSystemDll() || ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Controls.BitmapIcon, Uno.UI", "ShowAsMonochrome"))
				{
					bitmapIcon.ShowAsMonochrome = bitmapIconSource.ShowAsMonochrome;
				}

				return bitmapIcon;
			}
			else if (iconSource is Microsoft.UI.Xaml.Controls.PathIconSource pathIconSource)
			{
				PathIcon pathIcon = new PathIcon();

				if (pathIconSource.Data != null)
				{
					pathIcon.Data = pathIconSource.Data;
				}

				if (pathIconSource.Foreground != null)
				{
					pathIcon.Foreground = pathIconSource.Foreground;
				}

				return pathIcon;
			}

			return null;
		}

		public static void SetBinding(
			string pathString,
			DependencyObject target,
			DependencyProperty targetProperty)
		{
			Binding binding = new Binding();
			RelativeSource relativeSource = new RelativeSource();
			relativeSource.Mode = RelativeSourceMode.TemplatedParent;
			binding.RelativeSource = relativeSource;

			binding.Path = new PropertyPath(pathString);

			BindingOperations.SetBinding(target, targetProperty, binding);
		}

		// Be cautious: this function may introduce memory leak because Source holds strong reference to target too
		// There�s an intermediary object � the BindingExpression when BindingOperations::SetBinding
		// For example, if source is NavigationView and target is content control,
		// and there is strong reference: NavigationView -> ContentControl
		// BindingExpression.Source also make a strong reference to NavigationView
		// and it introduces the cycle: ContentControl -> BindingExpression -> NavigationView -> ContentControl
		// Prefer to use RelativeSource version of SetBinding if possible.
		public static void SetBinding(
			object source,
			string pathString,
			DependencyObject target,
			DependencyProperty targetProperty,
			IValueConverter converter = null,
			BindingMode mode = BindingMode.OneWay)
		{
			Binding binding = new Binding();

			binding.Source = source;
			binding.Path = new PropertyPath(pathString);
			binding.Mode = mode;

			if (converter != null)
			{
				binding.Converter = converter;
			}

			BindingOperations.SetBinding(target, targetProperty, binding);
		}

		public static ITextSelection GetRichTextSelection(RichEditBox richEditBox)
		{
			return richEditBox.Document?.Selection;
		}

		public static VirtualKey GetVirtualKeyFromChar(char c)
		{
			switch (c)
			{
				case 'A':
				case 'a':
					return VirtualKey.A;
				case 'B':
				case 'b':
					return VirtualKey.B;
				case 'C':
				case 'c':
					return VirtualKey.C;
				case 'D':
				case 'd':
					return VirtualKey.D;
				case 'E':
				case 'e':
					return VirtualKey.E;
				case 'F':
				case 'f':
					return VirtualKey.F;
				case 'G':
				case 'g':
					return VirtualKey.G;
				case 'H':
				case 'h':
					return VirtualKey.H;
				case 'I':
				case 'i':
					return VirtualKey.I;
				case 'J':
				case 'j':
					return VirtualKey.J;
				case 'K':
				case 'k':
					return VirtualKey.K;
				case 'L':
				case 'l':
					return VirtualKey.L;
				case 'M':
				case 'm':
					return VirtualKey.M;
				case 'N':
				case 'n':
					return VirtualKey.N;
				case 'O':
				case 'o':
					return VirtualKey.O;
				case 'P':
				case 'p':
					return VirtualKey.P;
				case 'Q':
				case 'q':
					return VirtualKey.Q;
				case 'R':
				case 'r':
					return VirtualKey.R;
				case 'S':
				case 's':
					return VirtualKey.S;
				case 'T':
				case 't':
					return VirtualKey.T;
				case 'U':
				case 'u':
					return VirtualKey.U;
				case 'V':
				case 'v':
					return VirtualKey.V;
				case 'W':
				case 'w':
					return VirtualKey.W;
				case 'X':
				case 'x':
					return VirtualKey.X;
				case 'Y':
				case 'y':
					return VirtualKey.Y;
				case 'Z':
				case 'z':
					return VirtualKey.Z;
				default:
					return VirtualKey.None;
			}
		}

		public static object FindResource(string resource, ResourceDictionary resources, object defaultValue)
		{
			var boxedResource = resource;
			return resources.HasKey(boxedResource) ? resources.Lookup(boxedResource) : defaultValue;
		}

		public static object FindInApplicationResources(string resource) => FindInApplicationResources(resource, default);
		public static object FindInApplicationResources(string resource, object defaultValue)
		{
			return FindResource(resource, Application.Current.Resources, defaultValue);
		}

		public static FrameworkElement FindInVisualTreeByName(FrameworkElement parent, string name)
		{
			return FindInVisualTree(
				parent,
				element => element.Name == name);
		}

		public static T FindInVisualTreeByType<T>(FrameworkElement parent)
			where T : class
		{
			var element = FindInVisualTree(
				parent,
				element => element is T);
			return element as T;
		}

		public static FrameworkElement FindInVisualTree(FrameworkElement parent, Func<FrameworkElement, bool> isMatch)
		{
			// Uno Specific - generalized to work with DependencyObject - needed for NativeScrollContentPresenter
			FrameworkElement FindInVisualTreeInner(DependencyObject parent, Func<FrameworkElement, bool> isMatch)
			{
				int numChildren = VisualTreeHelper.GetChildrenCount(parent);

				FrameworkElement foundElement = parent as FrameworkElement;
				if (foundElement != null && isMatch(foundElement))
				{
					return foundElement;
				}

				for (int i = 0; i < numChildren; i++)
				{
					var dp = VisualTreeHelper.GetChild(parent, i);
					if (dp != null)
					{
						foundElement = FindInVisualTreeInner(dp, isMatch);
						if (foundElement != null)
						{
							return foundElement;
						}
					}
				}

				return null;
			}

			return FindInVisualTreeInner(parent, isMatch);
		}

		public static bool IsTrue(bool? nullableBool)
		{
			if (nullableBool != null)
			{
				return nullableBool.Value;
			}
			return false;
		}

		// Sometimes we want to get a string representation from an arbitrary object. E.g. for constructing a UIA Name
		// from an automation peer. There is no guarantee that an arbitrary object is convertable to a string, so
		// this function may return an empty string.
		public static string TryGetStringRepresentationFromObject(object obj)
		{
			string returnHString = "";

			if (obj != null)
			{
				var stringable = obj as IStringable;
				if (stringable != null)
				{
					returnHString = stringable.ToString();
				}
				if (string.IsNullOrEmpty(returnHString))
				{
					returnHString = obj?.ToString() ?? returnHString;
				}
			}

			return returnHString;
		}

		//
		// Header file
		//

		public static AncestorType GetAncestorOfType<AncestorType>(DependencyObject firstGuess) where AncestorType : class
		{
			var obj = firstGuess;
			AncestorType matchedAncestor = null;
			while (obj != null && matchedAncestor == null)
			{
				matchedAncestor = obj as AncestorType;
				obj = VisualTreeHelper.GetParent(obj);
			}

			if (matchedAncestor != null)
			{
				return matchedAncestor as AncestorType;
			}
			else
			{
				return null;
			}
		}

		public static void CopyVector<T>(
			IObservableVector<T> source,
			IObservableVector<T> destination)
		{
			destination.Clear();

			foreach (var element in source)
			{
				destination.Add(element);
			}
		}

		public static void ForwardVectorChange<T>(
			IObservableVector<T> source,
			IObservableVector<T> destination,
			IVectorChangedEventArgs args)
		{
			var index = (int)args.Index;

			switch (args.CollectionChange)
			{
				case CollectionChange.ItemChanged:
					destination[index] = source[index];
					break;
				case CollectionChange.ItemInserted:
					destination.Insert(index, source[index]);
					break;
				case CollectionChange.ItemRemoved:
					destination.RemoveAt(index);
					break;
				case CollectionChange.Reset:
					CopyVector(source, destination);
					break;
				default:
					MUX_ASSERT(false);
					break;
			}
		}

		public static void EraseIfExists<TKey, TValue>(Dictionary<TKey, TValue> map, TKey key)
		{
			map.Remove(key);
		}
	}
}
