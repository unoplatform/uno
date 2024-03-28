// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference XamlUICommand_Partial.cpp, tag winui3/release/1.4.2

using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Input;

/// <summary>
/// Provides a base class for defining the command behavior of an interactive
/// UI element that performs an action when invoked (such as sending an email,
/// deleting an item, or submitting a form).
/// </summary>
public partial class XamlUICommand : DependencyObject, ICommand
{
	public XamlUICommand()
	{
		// TODO: Uno specific - we need to initialize KeyboardAccelerators here
		// WinUI does this automatically on demand via IsValueCreatedOnDemand.
		SetValue(KeyboardAcceleratorsProperty, new List<KeyboardAccelerator>());
	}

	/// <summary>
	/// Occurs whenever something happens that affects whether the command can execute.
	/// </summary>
	public event EventHandler CanExecuteChanged;

	/// <summary>
	/// Occurs when a CanExecute call is made.
	/// </summary>
	public event TypedEventHandler<XamlUICommand, CanExecuteRequestedEventArgs> CanExecuteRequested;

	/// <summary>
	/// Occurs when an Execute call is made.
	/// </summary>
	public event TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> ExecuteRequested;

	/// <summary>
	/// Gets or sets the label for this element.
	/// </summary>
	public string Label
	{
		get => (string)GetValue(LabelProperty) ?? "";
		set => SetValue(LabelProperty, value);
	}

	/// <summary>
	/// Gets or sets a glyph from the Segoe MDL2 Assets font for this element.
	/// </summary>
	public IconSource IconSource
	{
		get => (IconSource)GetValue(IconSourceProperty);
		set => SetValue(IconSourceProperty, value);
	}

	/// <summary>
	/// Gets or sets a description for this element.
	/// </summary>
	public string Description
	{
		get => (string)GetValue(DescriptionProperty) ?? "";
		set => SetValue(DescriptionProperty, value);
	}

	/// <summary>
	/// Gets or sets the command behavior of an interactive UI element
	/// that performs an action when invoked, such as sending an email,
	/// deleting an item, or submitting a form.
	/// </summary>
	public ICommand Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	/// <summary>
	/// Gets or sets the access key (mnemonic) for this element.
	/// </summary>
	public string AccessKey
	{
		get => (string)GetValue(AccessKeyProperty) ?? "";
		set => SetValue(AccessKeyProperty, value);
	}

	/// <summary>
	/// Gets or sets the collection of key combinations for this element that invoke an action using the keyboard.
	/// </summary>
	public IList<KeyboardAccelerator> KeyboardAccelerators => (IList<KeyboardAccelerator>)GetValue(KeyboardAcceleratorsProperty);

	/// <summary>
	/// Identifies the AccessKey dependency property.
	/// </summary>
	public static DependencyProperty AccessKeyProperty { get; } =
		DependencyProperty.Register(
			nameof(AccessKey),
			typeof(string),
			typeof(XamlUICommand),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the Command dependency property.
	/// </summary>
	public static DependencyProperty CommandProperty { get; } =
		DependencyProperty.Register(
			nameof(Command),
			typeof(ICommand),
			typeof(XamlUICommand),
			new FrameworkPropertyMetadata(default(ICommand)));

	/// <summary>
	/// Identifies the Description dependency property.
	/// </summary>
	public static DependencyProperty DescriptionProperty { get; } =
		DependencyProperty.Register(
			nameof(Description),
			typeof(string),
			typeof(XamlUICommand),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Identifies the IconSource dependency property.
	/// </summary>
	public static DependencyProperty IconSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(IconSource),
			typeof(IconSource),
			typeof(XamlUICommand),
			new FrameworkPropertyMetadata(default(IconSource)));

	/// <summary>
	/// Identifies the KeyboardAccelerators dependency property.
	/// </summary>
	public static DependencyProperty KeyboardAcceleratorsProperty { get; } =
		DependencyProperty.Register(
			nameof(KeyboardAccelerators),
			typeof(IList<KeyboardAccelerator>),
			typeof(XamlUICommand),
			new FrameworkPropertyMetadata(default(IList<KeyboardAccelerator>)));

	/// <summary>
	/// Identifies the Label dependency property.
	/// </summary>
	public static DependencyProperty LabelProperty { get; } =
		DependencyProperty.Register(
			nameof(Label),
			typeof(string),
			typeof(XamlUICommand),
			new FrameworkPropertyMetadata(default(string)));

	/// <summary>
	/// Notifies the system that the command state has changed.
	/// </summary>
	public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, null);

	/// <summary>
	/// Retrieves whether the command can execute in its current state.
	/// </summary>
	/// <param name="parameter">Data used by the command. If the command
	/// does not require data, this object can be set to null.</param>
	/// <returns>true if this command can be executed; otherwise, false.</returns>
	public bool CanExecute(object parameter)
	{
		bool canExecute = false;

		CanExecuteRequestedEventArgs args = new CanExecuteRequestedEventArgs();

		args.Parameter = parameter;
		args.CanExecute = true;

#if false // WI_IS_FEATURE_PRESENT(Feature_CommandingImprovements)
		ctl::ComPtr<CommandingContainer> focusedCommandingContainer;
		IFC_RETURN(GetFocusedCommandingContainer(&focusedCommandingContainer));

		if (focusedCommandingContainer)
		{
			IFC_RETURN(args->put_CommandTarget(focusedCommandingContainer->GetCommandTargetNoRef()));
			IFC_RETURN(args->put_ListCommandTarget(focusedCommandingContainer->GetListCommandTargetNoRef()));
		}
#endif

		CanExecuteRequested?.Invoke(this, args);

		canExecute = args.CanExecute;

		ICommand childCommand = Command;

		if (childCommand != null)
		{
			bool childCommandCanExecute = childCommand.CanExecute(parameter);
			canExecute = canExecute && childCommandCanExecute;
		}

		return canExecute;
	}

	/// <summary>
	/// Invokes the command.
	/// </summary>
	/// <param name="parameter">Data used by the command. If the command does not
	/// require data, this object can be set to null.</param>
	public void Execute(object parameter)
	{
		ExecuteRequestedEventArgs args = new ExecuteRequestedEventArgs();

		args.Parameter = parameter;

#if false // WI_IS_FEATURE_PRESENT(Feature_CommandingImprovements)
		ctl::ComPtr<CommandingContainer> focusedCommandingContainer;
		IFC_RETURN(GetFocusedCommandingContainer(&focusedCommandingContainer));

		if (focusedCommandingContainer)
		{
			IFC_RETURN(args->put_CommandTarget(focusedCommandingContainer->GetCommandTargetNoRef()));
			IFC_RETURN(args->put_ListCommandTarget(focusedCommandingContainer->GetListCommandTargetNoRef()));
		}
#endif

		ExecuteRequested?.Invoke(this, args);

		ICommand childCommand = Command;

		if (childCommand != null)
		{
			childCommand.Execute(parameter);
		}
	}
}
