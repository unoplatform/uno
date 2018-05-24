#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET46 || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class UISettings 
	{
		#if __ANDROID__ || __IOS__ || NET46 || false
		[global::Uno.NotImplemented]
		public  bool AnimationsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UISettings.AnimationsEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  uint CaretBlinkRate
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UISettings.CaretBlinkRate is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool CaretBrowsingEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UISettings.CaretBrowsingEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  uint CaretWidth
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UISettings.CaretWidth is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size CursorSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size UISettings.CursorSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  uint DoubleClickTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UISettings.DoubleClickTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.ViewManagement.HandPreference HandPreference
		{
			get
			{
				throw new global::System.NotImplementedException("The member HandPreference UISettings.HandPreference is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  uint MessageDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UISettings.MessageDuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  uint MouseHoverTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UISettings.MouseHoverTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size ScrollBarArrowSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size UISettings.ScrollBarArrowSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size ScrollBarSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size UISettings.ScrollBarSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Size ScrollBarThumbBoxSize
		{
			get
			{
				throw new global::System.NotImplementedException("The member Size UISettings.ScrollBarThumbBoxSize is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double TextScaleFactor
		{
			get
			{
				throw new global::System.NotImplementedException("The member double UISettings.TextScaleFactor is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool AdvancedEffectsEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool UISettings.AdvancedEffectsEnabled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || false
		[global::Uno.NotImplemented]
		public UISettings() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "UISettings.UISettings()");
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.UISettings()
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.HandPreference.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.CursorSize.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.ScrollBarSize.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.ScrollBarArrowSize.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.ScrollBarThumbBoxSize.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.MessageDuration.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.AnimationsEnabled.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.CaretBrowsingEnabled.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.CaretBlinkRate.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.CaretWidth.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.DoubleClickTime.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.MouseHoverTime.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color UIElementColor( global::Windows.UI.ViewManagement.UIElementType desiredElement)
		{
			throw new global::System.NotImplementedException("The member Color UISettings.UIElementColor(UIElementType desiredElement) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.TextScaleFactor.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.TextScaleFactorChanged.add
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.TextScaleFactorChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Color GetColorValue( global::Windows.UI.ViewManagement.UIColorType desiredColor)
		{
			throw new global::System.NotImplementedException("The member Color UISettings.GetColorValue(UIColorType desiredColor) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.ColorValuesChanged.add
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.ColorValuesChanged.remove
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.AdvancedEffectsEnabled.get
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.AdvancedEffectsEnabledChanged.add
		// Forced skipping of method Windows.UI.ViewManagement.UISettings.AdvancedEffectsEnabledChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.UISettings, object> TextScaleFactorChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "event TypedEventHandler<UISettings, object> UISettings.TextScaleFactorChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "event TypedEventHandler<UISettings, object> UISettings.TextScaleFactorChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.UISettings, object> ColorValuesChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "event TypedEventHandler<UISettings, object> UISettings.ColorValuesChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "event TypedEventHandler<UISettings, object> UISettings.ColorValuesChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.ViewManagement.UISettings, object> AdvancedEffectsEnabledChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "event TypedEventHandler<UISettings, object> UISettings.AdvancedEffectsEnabledChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.ViewManagement.UISettings", "event TypedEventHandler<UISettings, object> UISettings.AdvancedEffectsEnabledChanged");
			}
		}
		#endif
	}
}
