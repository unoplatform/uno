// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference StandardUICommand_Partial.cpp, tag winui3/release/1.4.2

using System;
using DirectUI;
using Uno.Disposables;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Input;

partial class StandardUICommand : IDependencyObjectInternal
{
	private void PrepareState()
	{
		//base.PrepareState();

		StandardUICommandKind kind = Kind;

		PopulateForKind(kind);
	}

	void IDependencyObjectInternal.OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		//base.OnPropertyChanged2(args);

		if (m_settingPropertyInternally)
		{
			return;
		}

		if (args.Property == KindProperty)
		{
			PopulateForKind((StandardUICommandKind)(args.NewValue));
		}
		else if (args.Property == LabelProperty)
		{
			m_ownsLabel = false;
		}
		else if (args.Property == IconSourceProperty)
		{
			m_ownsIconSource = false;
		}
		else if (args.Property == DescriptionProperty)
		{
			m_ownsDescription = false;
		}
	}

	// TODO: Uno - this should be called for every DependencyObject when
	// added to the visual tree
	//private void EnterImpl()
	//{
	//	if (Kind == StandardUICommandKind.None)
	//	{
	//		throw new InvalidOperationException("StandardUICommand Kind must be set");
	//	}
	//}

	private void PopulateForKind(StandardUICommandKind kind)
	{
		switch (kind)
		{
			case StandardUICommandKind.None:
				break;

			case StandardUICommandKind.Cut:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_CUT,
					Symbol.Cut,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_CUT,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_CUT);
				break;

			case StandardUICommandKind.Copy:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_COPY,
					Symbol.Copy,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_COPY,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_COPY);
				break;

			case StandardUICommandKind.Paste:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_PASTE,
					Symbol.Paste,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_PASTE,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_PASTE);
				break;

			case StandardUICommandKind.SelectAll:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_SELECTALL,
					Symbol.SelectAll,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_SELECTALL,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_SELECTALL);
				break;

			case StandardUICommandKind.Delete:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_DELETE,
					Symbol.Delete,
					VirtualKey.Delete,
					VirtualKeyModifiers.None,
					TEXT_COMMAND_DESCRIPTION_DELETE);
				break;

			case StandardUICommandKind.Share:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_SHARE,
					Symbol.Share,
					VirtualKey.None,
					VirtualKeyModifiers.None,
					TEXT_COMMAND_DESCRIPTION_SHARE);
				break;

			case StandardUICommandKind.Save:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_SAVE,
					Symbol.Save,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_SAVE,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_SAVE);
				break;

			case StandardUICommandKind.Open:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_OPEN,
					Symbol.OpenFile,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_OPEN,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_OPEN);
				break;

			case StandardUICommandKind.Close:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_CLOSE,
					Symbol.Cancel,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_CLOSE,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_CLOSE);
				break;

			case StandardUICommandKind.Pause:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_PAUSE,
					Symbol.Pause,
					VirtualKey.None,
					VirtualKeyModifiers.None,
					TEXT_COMMAND_DESCRIPTION_PAUSE);
				break;

			case StandardUICommandKind.Play:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_PLAY,
					Symbol.Play,
					VirtualKey.None,
					VirtualKeyModifiers.None,
					TEXT_COMMAND_DESCRIPTION_PLAY);
				break;

			case StandardUICommandKind.Stop:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_STOP,
					Symbol.Stop,
					VirtualKey.None,
					VirtualKeyModifiers.None,
					TEXT_COMMAND_DESCRIPTION_STOP);
				break;

			case StandardUICommandKind.Forward:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_FORWARD,
					Symbol.Forward,
					VirtualKey.None,
					VirtualKeyModifiers.None,
					TEXT_COMMAND_DESCRIPTION_FORWARD);
				break;

			case StandardUICommandKind.Backward:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_BACKWARD,
					Symbol.Back,
					VirtualKey.None,
					VirtualKeyModifiers.None,
					TEXT_COMMAND_DESCRIPTION_BACKWARD);
				break;

			case StandardUICommandKind.Undo:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_UNDO,
					Symbol.Undo,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_UNDO,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_UNDO);
				break;

			case StandardUICommandKind.Redo:
				PopulateWithProperties(
					TEXT_COMMAND_LABEL_REDO,
					Symbol.Redo,
					TEXT_COMMAND_KEYBOARDACCELERATORKEY_REDO,
					VirtualKeyModifiers.Control,
					TEXT_COMMAND_DESCRIPTION_REDO);
				break;

			default:
				throw new InvalidOperationException("Unknown StandardUICommandKind");
		}
	}

	private void PopulateWithProperties(
	   string labelResourceId,
	   Symbol symbol,
	   string acceleratorKeyResourceId,
	   VirtualKeyModifiers acceleratorModifiers,
	   string descriptionResourceId)
	{
		// If the accelerator key we've been passed is a string,
		// we need to convert it to an enum value.
		var acceleratorKeyString = DXamlCore.Current.GetLocalizedResourceString(acceleratorKeyResourceId);

		char acceleratorKeyChar = acceleratorKeyString.Length >= 1 ? acceleratorKeyString[0] : '\0';
		VirtualKey acceleratorKey = VirtualKey.None;

		switch (acceleratorKeyChar)
		{
			case 'A':
			case 'a':
				acceleratorKey = VirtualKey.A;
				break;
			case 'B':
			case 'b':
				acceleratorKey = VirtualKey.B;
				break;
			case 'C':
			case 'c':
				acceleratorKey = VirtualKey.C;
				break;
			case 'D':
			case 'd':
				acceleratorKey = VirtualKey.D;
				break;
			case 'E':
			case 'e':
				acceleratorKey = VirtualKey.E;
				break;
			case 'F':
			case 'f':
				acceleratorKey = VirtualKey.F;
				break;
			case 'G':
			case 'g':
				acceleratorKey = VirtualKey.G;
				break;
			case 'H':
			case 'h':
				acceleratorKey = VirtualKey.H;
				break;
			case 'I':
			case 'i':
				acceleratorKey = VirtualKey.I;
				break;
			case 'J':
			case 'j':
				acceleratorKey = VirtualKey.J;
				break;
			case 'K':
			case 'k':
				acceleratorKey = VirtualKey.K;
				break;
			case 'L':
			case 'l':
				acceleratorKey = VirtualKey.L;
				break;
			case 'M':
			case 'm':
				acceleratorKey = VirtualKey.M;
				break;
			case 'N':
			case 'n':
				acceleratorKey = VirtualKey.N;
				break;
			case 'O':
			case 'o':
				acceleratorKey = VirtualKey.O;
				break;
			case 'P':
			case 'p':
				acceleratorKey = VirtualKey.P;
				break;
			case 'Q':
			case 'q':
				acceleratorKey = VirtualKey.Q;
				break;
			case 'R':
			case 'r':
				acceleratorKey = VirtualKey.R;
				break;
			case 'S':
			case 's':
				acceleratorKey = VirtualKey.S;
				break;
			case 'T':
			case 't':
				acceleratorKey = VirtualKey.T;
				break;
			case 'U':
			case 'u':
				acceleratorKey = VirtualKey.U;
				break;
			case 'V':
			case 'v':
				acceleratorKey = VirtualKey.V;
				break;
			case 'W':
			case 'w':
				acceleratorKey = VirtualKey.W;
				break;
			case 'X':
			case 'x':
				acceleratorKey = VirtualKey.X;
				break;
			case 'Y':
			case 'y':
				acceleratorKey = VirtualKey.Y;
				break;
			case 'Z':
			case 'z':
				acceleratorKey = VirtualKey.Z;
				break;
		}

		PopulateWithProperties(
			labelResourceId,
			symbol,
			acceleratorKey,
			acceleratorKey != VirtualKey.None ? acceleratorModifiers : VirtualKeyModifiers.None,
			descriptionResourceId);
	}

	private void PopulateWithProperties(
	   string labelResourceId,
	   Symbol symbol,
	   VirtualKey acceleratorKey,
	   VirtualKeyModifiers acceleratorModifiers,
	   string descriptionResourceId)
	{
		SetLabelIfUnset(labelResourceId);
		SetIconSourceIfUnset(symbol);
		SetKeyboardAcceleratorIfUnset(acceleratorKey, acceleratorModifiers);
		SetDescriptionIfUnset(descriptionResourceId);
	}

	private void SetLabelIfUnset(string labelResourceId)
	{
		if (m_ownsLabel)
		{
			using var scopeGuard = Disposable.Create(() => m_settingPropertyInternally = false);
			m_settingPropertyInternally = true;

			string label = DXamlCore.Current.GetLocalizedResourceString(labelResourceId);
			Label = label;
		}
	}

	private void SetIconSourceIfUnset(Symbol symbol)
	{
		if (m_ownsIconSource)
		{
			using var scopeGuard = Disposable.Create(() => m_settingPropertyInternally = false);
			m_settingPropertyInternally = true;

			SymbolIconSource symbolIconSource = new();
			symbolIconSource.Symbol = symbol;

			IconSource = symbolIconSource;
		}
	}

	private void SetKeyboardAcceleratorIfUnset(
	   VirtualKey acceleratorKey,
	   VirtualKeyModifiers acceleratorModifiers)
	{
		if (!m_ownsKeyboardAccelerator)
		{
			return;
		}

		var keyboardAccelerators = KeyboardAccelerators;

		int keyboardAcceleratorCount = keyboardAccelerators.Count;

		// If we ever detect a keyboard accelerator value that we didn't set,
		// we'll cede ownership at that point to the app developer, and won't
		// touch the keyboard accelerators further.
		if (keyboardAcceleratorCount == 0 &&
			m_previousAcceleratorKey != VirtualKey.None)
		{
			// Unless the accelerator key was "None", we should always have an accelerator -
			// so, if we don't, then we know that the app developer has changed things.
			m_ownsKeyboardAccelerator = false;
			return;
		}
		else if (keyboardAcceleratorCount == 1)
		{
			var keyboardAccelerator = keyboardAccelerators[0];

			var currentAcceleratorKey = keyboardAccelerator.Key;
			var currentAcceleratorModifiers = keyboardAccelerator.Modifiers;

			if (currentAcceleratorKey != m_previousAcceleratorKey ||
				currentAcceleratorModifiers != m_previousAcceleratorModifiers)
			{
				m_ownsKeyboardAccelerator = false;
				return;
			}
		}
		else if (keyboardAcceleratorCount > 1)
		{
			// We'll never set more than one keyboard accelerator,
			// so if we have more than one, then we know that the app developer
			// has changed things.
			m_ownsKeyboardAccelerator = false;
			return;
		}

		keyboardAccelerators.Clear();

		if (acceleratorKey != VirtualKey.None)
		{
			var keyboardAccelerator = new KeyboardAccelerator();
			keyboardAccelerator.Key = acceleratorKey;
			keyboardAccelerator.Modifiers = acceleratorModifiers;
			keyboardAccelerators.Add(keyboardAccelerator);
		}

		m_previousAcceleratorKey = acceleratorKey;
		m_previousAcceleratorModifiers = acceleratorModifiers;

		return;
	}

	private void SetDescriptionIfUnset(string descriptionResourceId)
	{
		if (m_ownsDescription)
		{
			using var _ = Disposable.Create(() => m_settingPropertyInternally = false);

			m_settingPropertyInternally = true;

			var description = DXamlCore.Current.GetLocalizedResourceString(descriptionResourceId);
			Description = description;
		}
	}
}
