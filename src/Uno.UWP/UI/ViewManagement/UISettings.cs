#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Concurrent;
using Uno;
using Uno.Helpers.Theming;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Contains a set of common app user interface settings and operations.
	/// </summary>
	/// <remarks>Events on this class are fired as long as the instance is alive.
	/// To ensure the class does not get garbage collected, keep a strong reference to it.</remarks>
	public partial class UISettings
	{
		private static readonly ConcurrentDictionary<WeakReference<UISettings>, object> _instances = new ConcurrentDictionary<WeakReference<UISettings>, object>();
		private readonly WeakReference<UISettings> _weakReference;

		public UISettings()
		{
			_weakReference = new WeakReference<UISettings>(this);
			_instances.TryAdd(_weakReference, null);
		}

		~UISettings()
		{
			_instances.TryRemove(_weakReference, out var _);
		}

		internal static void OnColorValuesChanged()
		{
			foreach (var instance in _instances)
			{
				var weakReference = instance.Key;
				if (weakReference.TryGetTarget(out var uiSettings))
				{
					uiSettings.ColorValuesChanged?.Invoke(uiSettings, null);
				}
			}
		}

		internal static void OnTextScaleFactorChanged()
		{
			foreach (var instance in _instances)
			{
				var weakReference = instance.Key;
				if (weakReference.TryGetTarget(out var uiSettings))
				{
					uiSettings.TextScaleFactorChanged?.Invoke(uiSettings, null);
				}
			}
		}

		public event TypedEventHandler<UISettings, object> ColorValuesChanged;

		/// <summary>
		/// Starts observing OS text scale factor changes.
		/// On Skia desktop, subscribes to the <see cref="ITextScaleFactorExtension"/>.
		/// No-op on other platforms (they use their own change notification mechanisms).
		/// </summary>
		internal static void ObserveTextScaleFactorChanges()
		{
			ObserveTextScaleFactorChangesPlatform();
		}

		static partial void ObserveTextScaleFactorChangesPlatform();

		/// <summary>Starts observing OS accent color changes; raises <see cref="ColorValuesChanged"/> on change (Skia desktop). No-op elsewhere.</summary>
		internal static void ObserveAccentColorChanges()
		{
			ObserveAccentColorChangesPlatform();
		}

		static partial void ObserveAccentColorChangesPlatform();

		/// <summary>True when the platform provides the OS accent color (e.g. macOS); otherwise the default SystemAccentColor resources are kept.</summary>
		internal static bool HasAccentColorExtension =>
#if __SKIA__
			GetAccentColorExtension() is not null;
#else
			false;
#endif

#pragma warning disable 67 // Event is never used — used by platform-specific partials
		internal static event global::System.EventHandler TextScaleFactorChangedInternal;
#pragma warning restore 67

#if !__ANDROID__
		public bool AnimationsEnabled => true;
#endif

		public Color GetColorValue(UIColorType desiredColor)
		{
			var systemTheme = SystemThemeHelper.SystemTheme;
#if __SKIA__
			if (desiredColor is UIColorType.Accent
				or UIColorType.AccentDark1 or UIColorType.AccentDark2 or UIColorType.AccentDark3
				or UIColorType.AccentLight1 or UIColorType.AccentLight2 or UIColorType.AccentLight3)
			{
				if (GetAccentColorExtension() is { } extension)
				{
					var accent = extension.GetAccentColor();
					return desiredColor switch
					{
						UIColorType.Accent => accent,
						UIColorType.AccentDark1 => AdjustLightness(accent, -0.15),
						UIColorType.AccentDark2 => AdjustLightness(accent, -0.30),
						UIColorType.AccentDark3 => AdjustLightness(accent, -0.45),
						UIColorType.AccentLight1 => AdjustLightness(accent, 0.15),
						UIColorType.AccentLight2 => AdjustLightness(accent, 0.30),
						UIColorType.AccentLight3 => AdjustLightness(accent, 0.45),
						_ => accent,
					};
				}
			}
#endif
			return desiredColor switch
			{
				UIColorType.Background =>
					systemTheme == SystemTheme.Light ? Colors.White : Colors.Black,
				UIColorType.Foreground =>
					systemTheme == SystemTheme.Light ? Colors.Black : Colors.White,
				// The accent color values match SystemResources.xaml in Uno.UI
				// as we can't access Application resources from here directly.
				UIColorType.Accent => Color.FromArgb(255, 51, 153, 255),
				UIColorType.AccentDark1 => Color.FromArgb(255, 0, 90, 158),
				UIColorType.AccentDark2 => Color.FromArgb(255, 0, 66, 117),
				UIColorType.AccentDark3 => Color.FromArgb(255, 0, 38, 66),
				UIColorType.AccentLight1 => Color.FromArgb(255, 66, 156, 227),
				UIColorType.AccentLight2 => Color.FromArgb(255, 118, 185, 237),
				UIColorType.AccentLight3 => Color.FromArgb(255, 166, 216, 255),
				_ => Colors.Transparent
			};
		}

#if __SKIA__
		private static Color AdjustLightness(Color color, double delta)
		{
			// Simple HSL-based shade derivation for the AccentDark/Light variants
			// since macOS only exposes a single accentColor.
			var r = color.R / 255.0;
			var g = color.G / 255.0;
			var b = color.B / 255.0;

			var max = global::System.Math.Max(r, global::System.Math.Max(g, b));
			var min = global::System.Math.Min(r, global::System.Math.Min(g, b));
			var l = (max + min) / 2.0;

			double h = 0, s = 0;
			if (max != min)
			{
				var d = max - min;
				s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
				if (max == r)
				{
					h = (g - b) / d + (g < b ? 6 : 0);
				}
				else if (max == g)
				{
					h = (b - r) / d + 2;
				}
				else
				{
					h = (r - g) / d + 4;
				}
				h /= 6.0;
			}

			l = global::System.Math.Clamp(l + delta, 0.0, 1.0);

			double r1, g1, b1;
			if (s == 0)
			{
				r1 = g1 = b1 = l;
			}
			else
			{
				var q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
				var p = 2.0 * l - q;
				r1 = HueToRgb(p, q, h + 1.0 / 3.0);
				g1 = HueToRgb(p, q, h);
				b1 = HueToRgb(p, q, h - 1.0 / 3.0);
			}

			return Color.FromArgb(color.A,
				(byte)global::System.Math.Round(r1 * 255),
				(byte)global::System.Math.Round(g1 * 255),
				(byte)global::System.Math.Round(b1 * 255));
		}

		private static double HueToRgb(double p, double q, double t)
		{
			if (t < 0) t += 1;
			if (t > 1) t -= 1;
			if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
			if (t < 1.0 / 2.0) return q;
			if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
			return p;
		}
#endif

		[NotImplemented]
		public uint CaretBlinkRate
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "CaretBlinkRate");
				return 1;
			}
		}

		[NotImplemented]
		public bool CaretBrowsingEnabled
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "CaretBrowsingEnabled");
				return false;
			}
		}

		[NotImplemented]
		public uint CaretWidth
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "CaretWidth");
				return 1;
			}
		}

		[NotImplemented]
		public global::Windows.Foundation.Size CursorSize
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "CaretWidth");
				return new Foundation.Size(1, 10);
			}
		}

		[NotImplemented]
		public uint DoubleClickTime
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "DoubleClickTime");
				return 250;
			}
		}

		[NotImplemented]
		public global::Windows.UI.ViewManagement.HandPreference HandPreference
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "HandPreference");
				return global::Windows.UI.ViewManagement.HandPreference.RightHanded;
			}
		}

		[NotImplemented]
		public uint MessageDuration
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "MessageDuration");
				return 0;
			}
		}

		[NotImplemented]
		public uint MouseHoverTime
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "MouseHoverTime");
				return 0;
			}
		}

		[NotImplemented]
		public global::Windows.Foundation.Size ScrollBarArrowSize
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "ScrollBarArrowSize");
				return new Foundation.Size(10, 10);
			}
		}

		[NotImplemented]
		public global::Windows.Foundation.Size ScrollBarSize
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "ScrollBarSize");
				return new Foundation.Size(10, 10);
			}
		}

		[NotImplemented]
		public global::Windows.Foundation.Size ScrollBarThumbBoxSize
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "ScrollBarThumbBoxSize");
				return new Foundation.Size(10, 10);
			}
		}

		// TextScaleFactor: platform-specific implementations in UISettings.Android.cs,
		// UISettings.UIKit.cs, UISettings.skia.cs. Fallback for WASM/reference/unit tests:
#if !__ANDROID__ && !__APPLE_UIKIT__ && !__SKIA__
		public double TextScaleFactor => 1.0;
		internal static double GetTextScaleFactorValue() => 1.0;
#endif

		[NotImplemented]
		public bool AdvancedEffectsEnabled
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "AdvancedEffectsEnabled");
				return true;
			}
		}

		[NotImplemented]
		public bool AutoHideScrollBars { get; set; } = true;

		[NotImplemented]
		public global::Windows.UI.Color UIElementColor(global::Windows.UI.ViewManagement.UIElementType desiredElement)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "UIElementColor");
			return Colors.Black;
		}

		public event TypedEventHandler<UISettings, object> TextScaleFactorChanged;

#pragma warning disable 67
		[global::Uno.NotImplemented]
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.UISettings, object> AdvancedEffectsEnabledChanged;
#pragma warning restore 67
	}
}
