using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using Windows.Graphics.Display;
using Microsoft.UI.Composition;

namespace Uno.UI
{
	public static class FeatureConfiguration
	{
		/// <summary>
		/// Configuration for collectible AssemblyLoadContext (secondary-app) teardown.
		/// </summary>
		public static class Alc
		{
			/// <summary>
			/// When true, a failure to read an <see cref="System.Runtime.Loader.AssemblyLoadContext"/>'s
			/// unload state (e.g. a future runtime renaming the private state field) throws instead of
			/// silently falling back. The fallback keeps teardown leak-safe, but it would also let a
			/// broken read silently degrade every ALC cleanup scenario — enable this in tests/CI so such
			/// a regression fails loudly rather than going unnoticed. Defaults to false.
			/// </summary>
			public static bool ThrowOnUnloadStateReadFailure { get; set; }
		}

		public static class ApiInformation
		{
			/// <summary>
			/// Determines if runtime use of not implemented members raises an exception, or logs an error message.
			/// </summary>
			public static bool IsFailWhenNotImplemented
			{
				get => Windows.Foundation.Metadata.ApiInformation.IsFailWhenNotImplemented;
				set => Windows.Foundation.Metadata.ApiInformation.IsFailWhenNotImplemented = value;
			}

			/// <summary>
			/// Determines if runtime use of not implemented members is logged only once, or at each use.
			/// </summary>
			public static bool AlwaysLogNotImplementedMessages
			{
				get => Windows.Foundation.Metadata.ApiInformation.AlwaysLogNotImplementedMessages;
				set => Windows.Foundation.Metadata.ApiInformation.AlwaysLogNotImplementedMessages = value;
			}

			/// <summary>
			/// The message log level used when a not implemented member is used at runtime, if <see cref="IsFailWhenNotImplemented"/> is false.
			/// </summary>
			public static LogLevel NotImplementedLogLevel
			{
				get => Windows.Foundation.Metadata.ApiInformation.NotImplementedLogLevel;
				set => Windows.Foundation.Metadata.ApiInformation.NotImplementedLogLevel = value;
			}
		}

		public static class AutomationPeer
		{
			/// <summary>
			/// Enable a mode that simplifies accessibility by automatically grouping accessible elements into top-level accessible elements. The default value is false.
			/// </summary>
			/// <remarks>
			/// When enabled, the accessibility name of top-level accessible elements (elements that return a non-null AutomationPeer in <see cref="UIElement.OnCreateAutomationPeer()"/> and/or have <see cref="AutomationProperties.Name" /> set to a non-empty string)
			/// will be an aggregate of the accessibility name of all child accessible elements.
			///
			/// For example, if you have a <see cref="Button"/> that contains 3 <see cref="TextBlock"/> "A" "B" "C", the accessibility name of the <see cref="Button"/> will be "A, B, C".
			/// These 3 <see cref="TextBlock"/> will also be automatically excluded from accessibility focus.
			///
			/// This greatly facilitates accessibility, as you would need to do this manually on UWP.
			///
			/// A limitation of this strategy is that you can't nest interactive elements, as children of an accessible elements are excluded from accessibility focus.
			/// For example, if you put a <see cref="Button"/> inside another <see cref="Button"/>, only the parent <see cref="Button"/> will be focusable.
			/// This happens to match a limitation of iOS, which does this by default and forces developers to make elements as siblings instead of nesting them.
			///
			/// To prevent a top-level accessible element from being accessible and make its children accessibility focusable, you can set <see cref="AutomationProperties.AccessibilityViewProperty"/> to <see cref="AccessibilityView.Raw"/>.
			///
			/// Note: This is incompatible with the way accessibility works on UWP.
			/// </remarks>
			public static bool UseSimpleAccessibility { get; set; }

			/// <summary>
			/// When set to <c>true</c>, enables the accessibility semantic tree automatically
			/// on WebAssembly without requiring user interaction with the "Enable Accessibility" button.
			/// This is similar to Flutter's <c>SemanticsBinding.instance.ensureSemantics()</c>.
			/// Set this in your application startup before the host is built. The default value is <c>false</c>.
			/// </summary>
			public static bool AutoEnableAccessibility { get; set; }
		}

		public static class ComboBox
		{
			/// <summary>
			/// This defines the default value of the <see cref="UI.Xaml.Controls.ComboBox.DropDownPreferredPlacementProperty"/>. (cf. Remarks.)
			/// </summary>
			/// <remarks>
			/// As this value is read only once when initializing the dependency property,
			/// make sure to define it in the early stages of you application initialization,
			/// before any UI related initialization (like generic styles init) and even before
			/// referencing the ** type ** ComboBox in any way.
			/// </remarks>
			public static Uno.UI.Xaml.Controls.DropDownPlacement DefaultDropDownPreferredPlacement { get; set; } = Uno.UI.Xaml.Controls.DropDownPlacement.Auto;
		}

		public static class CompositionTarget
		{
			/// <summary>
			/// Suggested frame rate for <see cref="Microsoft.UI.Xaml.Media.CompositionTarget.Rendering"/> event.
			/// This property is used by desktop skia renderers.
			/// </summary>
			public static float FrameRate { get; set; } = 60;

			/// <summary>
			/// When possible, read the screen refresh rate and use it as the target frame rate instead of
			/// <see cref="FeatureConfiguration.CompositionTarget.FrameRate"/>.
			/// This property is used by desktop skia renderers.
			/// </summary>
			public static bool SetFrameRateAsScreenRefreshRate { get; set; } = true;
		}

		public static class ContentPresenter
		{
			/// <summary>
			/// Enables the implicit binding Content of a ContentPresenter to the one of the TemplatedParent
			/// when this one is a ContentControl.
			/// It means you can put a `<ContentPresenter />` directly in the ControlTemplate and it will
			/// be bound automatically to its TemplatedPatent's Content.
			/// </summary>
			public static bool UseImplicitContentFromTemplatedParent { get; set; }
		}

		public static class Control
		{
			/// <summary>
			/// Make the default value of VerticalContentAlignment and HorizontalContentAlignment be Top/Left instead of Center/Center
			/// </summary>
			public static bool UseLegacyContentAlignment { get; set; }
		}

		public static class DependencyObject
		{
			/// <summary>
			/// When set to true, the <see cref="DependencyObjectStore"/> will create hard references
			/// instead of weak references for some highly used fields, in common cases to improve the
			/// overall performance.
			/// </summary>
			public static bool IsStoreHardReferenceEnabled { get; set; }
				= true;
		}

		public static class ResourceDictionary
		{
			/// <summary>
			/// Determines whether unreferenced ResourceDictionary present in the assembly
			/// are accessible from app resources.
			/// </summary>
			public static bool IncludeUnreferencedDictionaries { get; set; }
		}

		public static class Font
		{
			private static string _symbolsFont =
				"ms-appx:///Uno.Fonts.Fluent/Fonts/uno-fluentui-assets.ttf";

			/// <summary>
			/// Defines the default font to be used when displaying symbols, such as in SymbolIcon. Must be invoked after App.InitializeComponent() to have an effect.
			/// </summary>
			public static string SymbolsFont
			{
				get => _symbolsFont;
				set
				{
					_symbolsFont = value;
					ResourceResolver.SetSymbolsFontFamily();
				}
			}

			/// <summary>
			/// The default font family for text when a font isn't explicitly specified (e.g. for a TextBlock)
			/// </summary>
			/// <remarks>
			/// The default is Segoe UI, which is not available on Mac and Linux as well as browsers running on Mac and Linux.
			/// So, you can change to OpenSans. For more information, see https://aka.platform.uno/feature-opensans
			/// </remarks>
			public static string DefaultTextFontFamily { get; set; } = "Segoe UI";

			/// <summary>
			/// Ignores text scale factor, resulting in a font size as dictated by the control.
			/// </summary>
			public static bool IgnoreTextScaleFactor { get; set; }

			/// <summary>
			/// Allows the user to limit the scale factor without having to ignore it.
			/// </summary>
			public static float? MaximumTextScaleFactor { get; set; }

			/// <summary>
			/// Overrides the font fallback mechanism used to resolve typefaces for codepoints
			/// that the requested font family cannot render. When <c>null</c> (the default),
			/// the platform-registered service is used.
			/// </summary>
			/// <remarks>
			/// Customers wanting to keep the built-in coverage but change how font bytes are obtained
			/// (e.g. to avoid CORS restrictions on WebAssembly) typically supply a
			/// <see cref="Microsoft.UI.Xaml.Documents.TextFormatting.CoverageTableFontFallbackService"/>
			/// constructed with their own coverage table and stream provider.
			/// </remarks>
			public static Microsoft.UI.Xaml.Documents.TextFormatting.IFontFallbackService FallbackService { get; set; }

			/// <summary>
			/// Overrides the OS-reported text scale factor with a manual value.
			/// When set, this value takes precedence over the OS-reported scale factor.
			/// Useful for platforms without OS text scaling support (macOS, WASM, Linux FrameBuffer) or for testing.
			/// </summary>
			public static double? TextScaleFactor { get; set; }
		}

		public static class FrameworkElement
		{
			/// <summary>
			/// When false, skips the FrameworkElement Loading/Loaded/Unloaded exception handling. This can be
			/// disabled to improve application performance on WebAssembly. See See #7005 for additional details.
			/// </summary>
			public static bool HandleLoadUnloadExceptions { get; set; } = true;
		}

		public static class FrameworkTemplate
		{
			/// <summary>
			/// Determines if the pooling is enabled. If false, all requested instances are new.
			/// </summary>
			public static bool IsPoolingEnabled { get => FrameworkTemplatePool.IsPoolingEnabled; set => FrameworkTemplatePool.IsPoolingEnabled = value; }

			/// <summary>
			/// Determines the duration for which a pooled template stays alive
			/// </summary>
			public static TimeSpan TimeToLive { get => FrameworkTemplatePool.TimeToLive; set => FrameworkTemplatePool.TimeToLive = value; }

			/// <summary>
			/// Defines the ratio of memory usage at which the pools starts to stop pooling elligible views, between 0 and 1
			/// </summary>
			public static float HighMemoryThreshold { get => FrameworkTemplatePool.HighMemoryThreshold; set => FrameworkTemplatePool.HighMemoryThreshold = value; }
		}

		public static class Image
		{
			/// <summary>
			/// Use the old way to align iOS images, using the "ContentMode".
			/// New way is using the Layer to better position the image according to alignments.
			/// </summary>
			public static bool LegacyIosAlignment { get; set; }

			/// <summary>
			/// On platforms that support caching BitmapImage assets, this sets
			/// the maximum number of entries in the cache. The value must be a
			/// positive integer.
			/// </summary>
			public static int MaxBitmapImageCacheCount { get; set; } = 100;

			/// <summary>
			/// On platforms that support caching BitmapImage assets, this enables caching.
			/// </summary>
			public static bool EnableBitmapImageCache { get; set; } = true;
		}

		public static class Interop
		{
			/// <summary>
			/// [WebAssembly Only] Used to control the behavior of the C#/Javascript interop. Setting this
			/// flag to true forces the use of the Javascript eval mode, instead of binary interop.
			/// This flag has no effect when running in hosted mode.
			/// </summary>
			public static bool ForceJavascriptInterop { get; set; }
		}

		public static class BindingExpression
		{
			/// <summary>
			/// When false, skips the BindingExpression.SetTargetValue exception handling. Can be disabled to
			/// improve application performance on WebAssembly. See See #7005 for additional details.
			/// </summary>
			public static bool HandleSetTargetValueExceptions { get; set; } = true;
		}

		public static class Popup
		{
			/// <summary>
			/// When set to true, light dismiss UI popups will not be dismissed when the window is deactivated.
			/// This is mainly useful for debugging purposes, we do not recommend using this in production code.
			/// </summary>
			public static bool PreventLightDismissOnWindowDeactivated { get; set; }

			/// <summary>
			/// By default, popups are constrained by the visible bounds on native renderer, but unconstrained on Skia renderer.
			/// </summary>
			public static bool ConstrainByVisibleBounds { get; set; }
#if !__SKIA__
				= true;
#endif
		}

		public static class ProgressRing
		{
			public static Uri ProgressRingAsset { get; set; } = new Uri("embedded://Uno.UI/Uno.UI.UI.Xaml.Controls.ProgressRing.ProgressRingIntdeterminate.json");
			public static Uri DeterminateProgressRingAsset { get; set; } = new Uri("embedded://Uno.UI/Uno.UI.UI.Xaml.Controls.ProgressRing.ProgressRingDeterminate.json");
		}

		public static class ListViewBase
		{
			/// <summary>
			/// Sets the value to use for <see cref="ItemsStackPanel.CacheLength"/> and <see cref="ItemsWrapGrid.CacheLength"/> if not set
			/// explicitly in Xaml or code. Higher values will cache more views either side of the visible window, improving list scroll
			/// performance at the expense of consuming more memory and taking longer to initially load. Setting this to null will leave
			/// the default value at the UWP default of 4.0.
			/// </summary>
			public static double? DefaultCacheLength { get; set; } = 1.0;
		}

		public static class Page
		{
			/// <summary>
			/// Enables reuse of <see cref="Page"/> instances. Enabling can improve performance when using <see cref="Frame"/> navigation.
			/// </summary>
			public static bool IsPoolingEnabled { get; set; }
		}

		public static class Frame
		{
			/// <summary>
			/// On non-Skia targets, Frame pools page instances to improve performance by default.
			/// To follow the WinUI behavior, set this to true. Skia uses WinUI behavior by default.
			/// </summary>
			public static bool UseWinUIBehavior { get; set; }
#if __SKIA__
				= true;
#endif
		}

		public static class SelectorItem
		{
			/// <summary>
			/// <para>
			/// Determines if the visual states "PointerOver", "PointerOverSelected"
			/// are used or not. If disabled, those states will never be activated by the selector items.
			/// </para>
			/// <para>The default value is `true`.</para>
			/// </summary>
			public static bool UseOverStates { get; set; } = true;
		}

		public static class Style
		{
			/// <summary>
			/// Determines if Uno.UI should be using native styles for controls that have
			/// a native counterpart. (e.g. Button, Slider, ComboBox, ...)
			///
			/// By default this is true.
			/// </summary>
			public static bool UseUWPDefaultStyles { get; set; } = true;

			/// <summary>
			/// Override the native styles usage per control type.
			/// </summary>
			/// <remarks>
			/// Usage: 'UseUWPDefaultStylesOverride[typeof(Frame)] = false;' will result in the native style always being the default for Frame, irrespective
			/// of the value of <see cref="UseUWPDefaultStyles"/>. This is useful when an app uses the UWP default look for most controls but the native
			/// appearance/comportment for a few particular controls, or vice versa.
			/// </remarks>
			public static IDictionary<Type, bool> UseUWPDefaultStylesOverride { get; } = new Dictionary<Type, bool>();

			/// <summary>
			/// This enables native frame navigation on Android and iOS by setting related classes (<see cref="Frame"/>, <see cref="CommandBar"/>
			/// and <see cref="Microsoft.UI.Xaml.Controls.AppBarButton"/>) to use their native styles.
			/// </summary>
			public static void ConfigureNativeFrameNavigation()
			{
				if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_Frame_Available)
				{
					SetUWPDefaultStylesOverride<Microsoft.UI.Xaml.Controls.Frame>(useUWPDefaultStyle: false);
				}

				if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_CommandBar_Available)
				{
					SetUWPDefaultStylesOverride<Microsoft.UI.Xaml.Controls.CommandBar>(useUWPDefaultStyle: false);
				}

				if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_AppBarButton_Available)
				{
					SetUWPDefaultStylesOverride<Microsoft.UI.Xaml.Controls.AppBarButton>(useUWPDefaultStyle: false);
				}
			}

			/// <summary>
			/// Override the native styles useage for control type <typeparamref name="TControl"/>.
			/// </summary>
			/// <typeparam name="TControl"></typeparam>
			/// <param name="useUWPDefaultStyle">
			/// Whether instances of <typeparamref name="TControl"/> should use the UWP default style.
			/// If false, the native default style (if one exists) will be used.
			/// </param>
			public static void SetUWPDefaultStylesOverride<TControl>(bool useUWPDefaultStyle) where TControl : Microsoft.UI.Xaml.Controls.Control
				=> UseUWPDefaultStylesOverride[typeof(TControl)] = useUWPDefaultStyle;
		}

		public static class TextBlock
		{
			/// <summary>
			/// [WebAssembly Only] Determines if the measure cache is enabled.
			/// </summary>
			public static bool IsMeasureCacheEnabled { get; set; } = true;

			/// <summary>
			/// [Android Only] Determines if the Java string-cache is enabled.
			/// This option must be set on application startup before the cache is initialized.
			/// </summary>
			public static bool IsJavaStringCachedEnabled { get; set; } = true;

			/// <summary>
			/// [Android Only] Determines the maximum capacity of the Java string-cache.
			/// This option must be set on application startup before the cache is initialized.
			/// </summary>
			public static int JavaStringCachedCapacity { get; set; } = 1000;

			/// <summary>
			/// On Skia targets, determines if the TextBlock should render whitespace characters.
			/// There's usually no effect between toggling this flag on and off, but it can have
			/// an effect when the font used to draw the TextBlock doesn't have a glyph for the
			/// whitespace characters, in which case disabling this flag will prevent the TextBlock
			/// from rendering the font's "replacement character" symbol (e.g. �) instead of just a white space.
			/// </summary>
			public static bool RenderWhiteSpace { get; set; }
		}

		public static class TextBox
		{

			/// <summary>
			/// Determines if the caret is visible or not.
			/// </summary>
			/// <remarks>This feature is used to avoid screenshot comparisons false positives</remarks>
			public static bool HideCaret { get; set; }

			/// <summary>
			/// Hunspell dictionaries to be used for spell checking in TextBox and RichEditBox controls.
			/// By default, an english dictionary is provided.
			/// This is currently a skia-only feature.
			/// </summary>
			public static List<(Stream dictionary, Stream affixes)> CustomSpellCheckDictionaries { get; set; }

			/// <summary>
			/// When set to <see langword="true"/>, disables the floating number pad popover that iOS 26
			/// introduced for <c>UITextField</c> when a numeric keyboard is used on iPad (by setting
			/// <c>UITextField.allowsNumberPadPopover</c> to <see langword="false"/>).
			/// Defaults to <see langword="false"/> (native iOS 26 behavior is preserved).
			/// </summary>
			/// <remarks>
			/// This is currently an iOS Skia-only feature and has no effect on other platforms or
			/// on iOS versions prior to 26.
			/// </remarks>
			public static bool DisableNumberPadPopover { get; set; }
		}

		public static class ScrollViewer
		{
			/// <summary>
			/// This defines the default value of the <see cref="Uno.UI.Xaml.Controls.ScrollViewer.UpdatesModeProperty"/>.
			/// For backward compatibility, you should set it to Synchronous.
			/// For better compatibility with Windows, you should keep the default value 'AsynchronousIdle'.
			/// </summary>
			/// <remarks>
			/// As this value is read only once when initializing the dependency property,
			/// make sure to define it in the early stages of you application initialization,
			/// before any UI related initialization (like generic styles init) and even before
			/// referencing the ** type ** ScrollViewer in any way.
			/// </remarks>
			public static ScrollViewerUpdatesMode DefaultUpdatesMode { get; set; } = ScrollViewerUpdatesMode.AsynchronousIdle;

			/// <summary>
			/// Defines the delay after which the scrollbars hide themselves when pointer is not over.<br/>
			/// Default is 4 sec.<br/>
			/// Setting this to <see cref="TimeSpan.MaxValue"/> will completely disable the auto hide feature.
			/// </summary>
			/// <remarks>This is effective only for managed scrollbars (WASM, macOS and Skia for now)</remarks>
			public static TimeSpan? DefaultAutoHideDelay { get; set; }

			/// <summary>
			/// Defines the delay of after which the ScrollViewer starts to move to snap points. The default value is 250ms.
			/// </summary>
			public static TimeSpan SnapDelay { get; set; } = TimeSpan.FromMilliseconds(250);
		}

		public static class ThemeAnimation
		{
			/// <summary>
			/// Default duration for xxxThemeAnimation
			/// </summary>
			public static TimeSpan DefaultThemeAnimationDuration { get; set; } = TimeSpan.FromSeconds(0.75);
		}

		public static class ToolTip
		{
			public static bool UseToolTips { get; set; }
#if __SKIA__
				= true;
#endif

			public static int ShowDelay { get; set; } = 1000;

			public static int ShowDuration { get; set; } = 5000;
		}

		public static class UIElement
		{
			/// <summary>
			/// [WebAssembly Only] Enable the assignation of the "xamlname", "xuid" and "xamlautomationid" attributes on DOM elements created
			/// from the XAML visual tree. This enables tools such as Puppeteer to select elements
			/// in the DOM for automation purposes.
			/// </summary>
			public static bool AssignDOMXamlName { get; set; }

			/// <summary>
			/// [WebAssembly Only] Enable UIElement.ToString() to return the element's unique ID
			/// </summary>
			public static bool RenderToStringWithId { get; set; } = true;

			/// <summary>
			/// [WebAssembly Only] Enables the assignation of properties from the XAML visual tree as DOM attributes: Height -> "xamlheight",
			/// HorizontalAlignment -> "xamlhorizontalalignment" etc.
			/// </summary>
			/// <remarks>
			/// This should only be enabled for debug builds, but can greatly aid layout debugging.
			///
			/// Note: for release builds of Uno, if the flag is set, attributes will be set on loading and *not* updated if
			/// the values change subsequently. This restriction doesn't apply to debug Uno builds.
			/// </remarks>
			public static bool AssignDOMXamlProperties { get; set; }

			/// <summary>
			/// Enables failure when <see cref="Foundation.NSObjectExtensions.ValidateDispose"/> is invoked.
			/// </summary>
			public static bool FailOnNSObjectExtensionsValidateDispose { get; set; }
		}

		public static class WebView2
		{
			/// <summary>
			/// Enables the platform-native developer tools for the <see cref="WebView2"/> control.
			/// </summary>
			/// <remarks>
			/// <para>Defaults to <c>true</c> in DEBUG builds and <c>false</c> in RELEASE builds.</para>
			/// <para>Per-platform behavior:</para>
			/// <list type="bullet">
			///   <item><description>Windows / Linux (Skia): toggles Chromium DevTools (right-click "Inspect" / F12).</description></item>
			///   <item><description>iOS / Mac Catalyst / macOS: enables Safari Web Inspector against the <c>WKWebView</c> (requires iOS 16.4+, macOS 13.3+).</description></item>
			///   <item><description>Android: enables Chrome DevTools remote debugging at <c>chrome://inspect</c>.</description></item>
			///   <item><description>WebAssembly: no-op; use the host browser's developer tools.</description></item>
			/// </list>
			/// <para>Set this once during application startup before any <c>WebView2</c> is materialized.</para>
			/// </remarks>
			public static bool EnableDevTools { get; set; }
#if DEBUG
				= true;
#endif

			/// <summary>
			/// Enables single sign-on using the OS primary account (for example the Microsoft Entra ID / Azure AD
			/// account the user is signed into Windows with) when the <see cref="Microsoft.UI.Xaml.Controls.WebView2"/> authenticates against
			/// supporting resources.
			/// </summary>
			/// <remarks>
			/// <para><b>Windows (Skia Desktop) only.</b> The value is passed to the underlying CoreWebView2 environment
			/// options when the environment is first created. It is a no-op on every other target, and on the Windows
			/// App SDK (WinUI) target where SSO is configured through <c>CoreWebView2EnvironmentOptions</c> directly.</para>
			/// <para>Must be set once during application startup, before any <c>WebView2</c> is materialized. The
			/// CoreWebView2 environment is shared process-wide per user-data folder, so changing this after the first
			/// <c>WebView2</c> is created has no effect (and creating a second environment with different options throws).</para>
			/// <para>Defaults to <c>false</c>, matching the WebView2 default.</para>
			/// </remarks>
			public static bool AllowSingleSignOnUsingOSPrimaryAccount { get; set; }

			/// <summary>
			/// Additional command-line switches passed to the browser process backing the <see cref="Microsoft.UI.Xaml.Controls.WebView2"/>
			/// (for example proxy configuration or Chromium feature flags), useful in locked-down or managed environments.
			/// </summary>
			/// <remarks>
			/// <para><b>Windows (Skia Desktop) only.</b> The value is applied to the underlying CoreWebView2 environment
			/// options when the environment is first created. It is a no-op on every other target, and on the Windows
			/// App SDK (WinUI) target where <c>CoreWebView2EnvironmentOptions.AdditionalBrowserArguments</c> is used directly.</para>
			/// <para>Must be set once during application startup, before any <c>WebView2</c> is materialized (see
			/// <see cref="AllowSingleSignOnUsingOSPrimaryAccount"/> for why). Refer to the WebView2 documentation for the
			/// list of supported switches.</para>
			/// </remarks>
			public static string AdditionalBrowserArguments { get; set; }

		}

		public static class Xaml
		{
			/// <summary>
			/// By default, XAML hot reload will be enabled when building in debug. Setting this flag to 'true' will force it to be disabled.
			/// </summary>
			public static bool ForceHotReloadDisabled { get; set; }
		}

		public static class XamlReader
		{
			/// <summary>
			/// When set to true, the XamlReader will throw an exception if it encounters properties in
			/// the XAML that do not map to a property on the target object.
			/// </summary>
			public static bool FailOnUnknownProperties { get; set; }
		}

		public static class Rendering
		{
			/// <summary>
			/// Determines if OpenGL rendering should be enabled on the X11 target. If null, defaults to
			/// OpenGL if available. Otherwise, software rendering will be used.
			/// </summary>
			public static bool? UseOpenGLOnX11 { get; set; }

			/// <summary>
			/// Determines if OpenGL ES + EGL should be used instead of OpenGL + GLX if both are available. This value is only
			/// used if <see cref="UseOpenGLOnX11"/> is true or null. This property only affects the order of attempting
			/// to create a GL/GlES context but even when true, if the preferred API fails, the other will be attempted.
			/// </summary>
			public static bool PreferGLESOverGLOnX11 { get; set; }

			/// <summary>
			/// Determines if OpenGL rendering should be enabled on the Win32 target. If null, defaults to
			/// OpenGL if available. Otherwise, software rendering will be used.
			/// </summary>
			public static bool? UseOpenGLOnWin32 { get; set; }

			/// <summary>
			/// Determines if Vulkan rendering should be enabled on the X11 target.
			/// Defaults to true: Vulkan is used for hardware-accelerated rendering when available, falling back to
			/// OpenGL (or software rendering) if Vulkan is unavailable.
			/// </summary>
			public static bool UseVulkanOnX11 { get; set; } = true;

			/// <summary>
			/// Determines if Vulkan rendering should be enabled on the Win32 target.
			/// Defaults to true: Vulkan is used for hardware-accelerated rendering when available, falling back to
			/// OpenGL (or software rendering) if Vulkan is unavailable.
			/// </summary>
			public static bool UseVulkanOnWin32 { get; set; } = true;

			/// <summary>
			/// Determines if OpenGL rendering should be enabled on the Android target when using the skia renderer.
			/// </summary>
			public static bool UseOpenGLOnSkiaAndroid { get; set; } = true;

			/// <summary>
			/// Determines if Vulkan rendering should be enabled on the Android target when using the skia renderer.
			/// Defaults to true: Vulkan is used for hardware-accelerated rendering when available, falling back to
			/// OpenGL ES (or software rendering) if Vulkan is unavailable.
			/// </summary>
			public static bool UseVulkanOnSkiaAndroid { get; set; } = true;

			/// <summary>
			/// Enables certain optimizations that skip rendering some subtrees
			/// of the visual tree that do not change often and instead caches their
			/// rendering output. This optimization is only for skia targets.
			/// </summary>
			public static bool EnableVisualSubtreeSkippingOptimization
			{
#if __SKIA__
				get => Visual.EnablePictureCollapsingOptimization;
				set => Visual.EnablePictureCollapsingOptimization = value;
#else
				get => false;
				set { }
#endif
			}

			/// <summary>
			/// When <see cref="EnableVisualSubtreeSkippingOptimization"/> is enabled, determines the number
			/// of frames that a visual subtree needs to remain unchanged through, after which the subtree
			/// is considered for the subtree skipping optimization.
			/// </summary>
			public static int VisualSubtreeSkippingOptimizationCleanFramesThreshold
			{
#if __SKIA__
				get => Visual.PictureCollapsingOptimizationFrameThreshold;
				set => Visual.PictureCollapsingOptimizationFrameThreshold = value;
#else
				get => 0;
				set { }
#endif
			}

			/// <summary>
			/// When <see cref="EnableVisualSubtreeSkippingOptimization"/> is enabled, determines the minimum
			/// size of visual subtrees considered for the subtree skipping optimization.
			/// </summary>
			public static int VisualSubtreeSkippingOptimizationVisualCountThreshold
			{
#if __SKIA__
				get => Visual.PictureCollapsingOptimizationVisualCountThreshold;
				set => Visual.PictureCollapsingOptimizationVisualCountThreshold = value;
#else
				get => 0;
				set { }
#endif
			}

			/// <summary>
			/// Force the use of Metal (true) or Software (false) rendering on macOS.
			/// If null (default) use Metal if available, otherwise fallback to Software rendering.
			/// </summary>
			public static bool? UseMetalOnMacOS { get; set; }

			/// <summary>
			/// When true, painting of the visual tree is skipped entirely, producing frames with no
			/// visual output. Frame scheduling, rendering events, composition animations and
			/// <see cref="Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"/> keep working as usual.
			/// Intended for scenarios where the visual output is of no interest (e.g. automated tests)
			/// to save CPU/GPU. This flag is only effective on skia targets.
			/// </summary>
			public static bool SkipVisualTreePainting
			{
#if __SKIA__
				get => Compositor.SkipVisualTreePainting;
				set => Compositor.SkipVisualTreePainting = value;
#else
				get => false;
				set { }
#endif
			}

			/// <summary>
			/// When damage-region rendering is active, visually highlights the regions being
			/// repainted each frame, for tuning and debugging. This is a diagnostic aid only.
			/// </summary>
			public static bool DamageRegionOverlay { get; set; }
		}

		public static class ElementRefHandle
		{
			/// <summary>
			/// Accessing the element ref handle registry must only happen on the UI thread.
			/// By default, attempting to access it from a non-UI thread throws an
			/// <see cref="InvalidOperationException"/>.
			/// <para>
			/// Setting this to <see langword="true"/> suppresses that exception.
			/// This is intended only for unit-test environments where a real UI thread is not
			/// available. Do not set this in production code.
			/// </para>
			/// </summary>
			public static bool DisableThreadingCheck { get; internal set; }
		}

		public static class DependencyProperty
		{
			/// <summary>
			/// Accessing the dependency property system isn't thread safe and should only
			/// happen on the UI thread.
			/// By default, attempting to access it from non UI thread will throw an exception.
			/// Setting this flag to true will prevent the exception from being thrown at the risk
			/// of having an undefined behavior and/or race conditions.
			/// </summary>
			public static bool DisableThreadingCheck { get; set; }

			/// <summary>
			/// Defines how many <see cref="DependencyPropertyChangedEventArgs" /> are pooled.
			/// </summary>
			public static int DependencyPropertyChangedEventArgsPoolSize { get; set; } = 32;

			/// <summary>
			/// Enables checks that make sure that <see cref="DependencyObjectStore.GetValue" /> and
			/// <see cref="DependencyObjectStore.SetValue" /> are only called on the owner of the property being
			/// set/got.
			/// </summary>
			public static bool ValidatePropertyOwnerOnReadWrite { get; set; } =
#if DEBUG
				true;
#else
				global::System.Diagnostics.Debugger.IsAttached;
#endif
		}

		/// <summary>
		/// This is for internal use to facilitate turning on/off certain logic that makes it easier/harder
		/// to debug.
		/// </summary>
		internal static class DebugOptions
		{
			public static bool PreventKeyboardStateTrackerFromResettingOnWindowActivationChange { get; set; }

			public static bool WaitIndefinitelyInEventTester { get; set; }
		}

		public static class Shape
		{
			/// <summary>
			/// [WebAssembly Only] Gets or sets whether native svg attributes assignments can be postponed until the first arrange pass.
			/// </summary>
			/// <remarks>This avoid double assignments(with js interop call) from both OnPropertyChanged and UpdateRender.</remarks>
			public static bool WasmDelayUpdateUntilFirstArrange { get; set; } = true;

			/// <summary>
			/// [WebAssembly Only] Gets or sets whether native getBBox() result will be cached.
			/// </summary>
			public static bool WasmCacheBBoxCalculationResult { get; set; } = true;

			internal const int WasmDefaultBBoxCacheSize = 64;
			/// <summary>
			/// [WebAssembly Only] Gets or sets the size of getBBox cache. The default size is 64.
			/// </summary>
			public static int WasmBBoxCacheSize { get; set; } = WasmDefaultBBoxCacheSize;
		}

	}
}
