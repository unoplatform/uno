using System.Collections.Generic;
using System.Globalization;
using Windows.System;

namespace Microsoft.UI.Xaml.Input
{
	public partial class KeyboardAccelerator : DependencyObject
	{
		public DependencyObject ScopeOwner
		{
			get => (DependencyObject)this.GetValue(ScopeOwnerProperty);
			set => this.SetValue(ScopeOwnerProperty, value);
		}

		public VirtualKeyModifiers Modifiers
		{
			get => (VirtualKeyModifiers)this.GetValue(ModifiersProperty);
			set => this.SetValue(ModifiersProperty, value);
		}

		public VirtualKey Key
		{
			get => (VirtualKey)this.GetValue(KeyProperty);
			set => this.SetValue(KeyProperty, value);
		}
		public bool IsEnabled
		{
			get => (bool)this.GetValue(IsEnabledProperty);
			set => this.SetValue(IsEnabledProperty, value);
		}

		public static DependencyProperty ScopeOwnerProperty { get; } =
			DependencyProperty.Register(nameof(ScopeOwner), typeof(DependencyObject), typeof(KeyboardAccelerator), new FrameworkPropertyMetadata(default(DependencyObject), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public static DependencyProperty ModifiersProperty { get; } =
			DependencyProperty.Register(nameof(Modifiers), typeof(VirtualKeyModifiers), typeof(KeyboardAccelerator), new FrameworkPropertyMetadata(default(VirtualKeyModifiers)));

		public static DependencyProperty KeyProperty { get; } =
			DependencyProperty.Register(nameof(Key), typeof(VirtualKey), typeof(KeyboardAccelerator), new FrameworkPropertyMetadata(default(VirtualKey)));

		public static DependencyProperty IsEnabledProperty { get; } =
			DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(KeyboardAccelerator), new FrameworkPropertyMetadata(default(bool)));

		internal static string GetStringRepresentationForUIElement(UIElement uiElement)
		{
			// We don't want to bother doing anything if we've never actually set a keyboard accelerator,
			// so we'll just return null unless we have.
			// UNO TODO if (!uiElement.GetHandle().CheckOnDemandProperty(KnownPropertyIndex.UIElement_KeyboardAccelerators).IsNull())
			if (uiElement.KeyboardAccelerators.Count != 0)
			{
				IList<KeyboardAccelerator> keyboardAccelerators;
				int keyboardAcceleratorCount;

				keyboardAccelerators = uiElement.KeyboardAccelerators;
				keyboardAcceleratorCount = keyboardAccelerators.Count;

				if (keyboardAcceleratorCount > 0)
				{
					var keyboardAcceleratorStringRepresentation = keyboardAccelerators[0];
					return keyboardAcceleratorStringRepresentation.GetStringRepresentation();
				}
			}

			return null;
		}

		string GetStringRepresentation()
		{
			var key = Key;
			var modifiers = Modifiers;
			var stringRepresentationLocal = "";

			if ((modifiers & VirtualKeyModifiers.Control) != 0)
			{
				ConcatVirtualKey(VirtualKey.Control, ref stringRepresentationLocal);
			}

			if ((modifiers & VirtualKeyModifiers.Menu) != 0)
			{
				ConcatVirtualKey(VirtualKey.Menu, ref stringRepresentationLocal);
			}

			if ((modifiers & VirtualKeyModifiers.Windows) != 0)
			{
				ConcatVirtualKey(VirtualKey.LeftWindows, ref stringRepresentationLocal);
			}

			if ((modifiers & VirtualKeyModifiers.Shift) != 0)
			{
				ConcatVirtualKey(VirtualKey.Shift, ref stringRepresentationLocal);
			}

			ConcatVirtualKey(key, ref stringRepresentationLocal);

			return stringRepresentationLocal;
		}

		void ConcatVirtualKey(VirtualKey key, ref string keyboardAcceleratorString)
		{
			string keyName;

			// UNO TODO
			//(DXamlCore.GetCurrent().GetLocalizedResourceString(GetResourceStringIdFromVirtualKey(key), keyName.ReleaseAndGetAddressOf()));
			keyName = key.ToString();


			if (string.IsNullOrEmpty(keyboardAcceleratorString))
			{
				// If this is the first key string we've accounted for, then
				// we can just set the keyboard accelerator string equal to it.
				keyboardAcceleratorString = keyName;
			}
			else
			{
				// Otherwise, if we already had an existing keyboard accelerator string,
				// then we'll use the formatting string to join strings together
				// to combine it with the new key string.
				string joiningFormatString;
				// UNO TODO
				// (DXamlCore.GetCurrent().GetLocalizedResourceString(KEYBOARD_ACCELERATOR_TEXT_JOIN, joiningFormatString.ReleaseAndGetAddressOf()));
				joiningFormatString = "{0} + {1}";

				keyboardAcceleratorString = string.Format(CultureInfo.InvariantCulture, joiningFormatString, keyboardAcceleratorString, keyName);
			}
		}
	}
}
