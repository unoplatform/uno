#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Concurrent;
using Uno;
using Uno.Helpers.Theming;
using Windows.Foundation;
using Windows.UI.Core;
#if __MACOS__
using AppKit;
#endif

namespace Windows.UI.ViewManagement
{
	/// <summary>
	/// Contains a set of common app user interface settings and operations.
	/// </summary>
	/// <remarks>Events on this class are fired as long as the instance is alive.
	/// To ensure the class does not get garbage collected, keep a strong reference to it.</remarks>
	public partial class UISettings
	{
		private static readonly ConcurrentDictionary<WeakReference<UISettings>, object?> _instances = new ConcurrentDictionary<WeakReference<UISettings>, object?>();
		private readonly WeakReference<UISettings> _weakReference;

		public UISettings()
		{
			_weakReference = new WeakReference<UISettings>(this);
			_instances.TryAdd(_weakReference, null);


#if __MACOS__ && NET6_0_OR_GREATER
			NSColor.Notifications.ObserveSystemColorsChanged((sender, eventArgs) =>
			{
				OnColorValuesChanged();
			});
#endif

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

		public event TypedEventHandler<UISettings, object?>? ColorValuesChanged;

#if !__ANDROID__
		public bool AnimationsEnabled => true;
#endif

		public Color GetColorValue(UIColorType desiredColor)
		{
			var systemTheme = SystemThemeHelper.SystemTheme;
			return desiredColor switch
			{

#if __MACOS__ && NET6_0_OR_GREATER
				UIColorType.Background => NSColor.ControlBackground,
				UIColorType.Foreground => NSColor.ControlText,
				UIColorType.Accent => NSColor.ControlAccent,
#else
				UIColorType.Background =>
					systemTheme == SystemTheme.Light ? Colors.White : Colors.Black,
				UIColorType.Foreground =>
					systemTheme == SystemTheme.Light ? Colors.Black : Colors.White,
				// The accent color values match SystemResources.xaml in Uno.UI
				// as we can't access Application resources from here directly.
				UIColorType.Accent => Color.FromArgb(255, 51, 153, 255),
#endif

				UIColorType.AccentDark1 => Color.FromArgb(255, 0, 90, 158),
				UIColorType.AccentDark2 => Color.FromArgb(255, 0, 66, 117),
				UIColorType.AccentDark3 => Color.FromArgb(255, 0, 38, 66),
				UIColorType.AccentLight1 => Color.FromArgb(255, 66, 156, 227),
				UIColorType.AccentLight2 => Color.FromArgb(255, 118, 185, 237),
				UIColorType.AccentLight3 => Color.FromArgb(255, 166, 216, 255),
				_ => Colors.Transparent
			};
		}

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

		[NotImplemented]
		public double TextScaleFactor
		{
			get
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "TextScaleFactor");
				return 1;
			}
		}

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

#pragma warning disable 67
		[global::Uno.NotImplemented]
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.UISettings, object>? TextScaleFactorChanged;

		[global::Uno.NotImplemented]
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.UISettings, object>? AdvancedEffectsEnabledChanged;
#pragma warning restore 67
	}
}
