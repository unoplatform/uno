using System;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.Foundation;

namespace Windows.UI.Xaml.Input
{
	public partial class XamlUICommand : DependencyObject, ICommand
	{
		public event EventHandler CanExecuteChanged;
		public event TypedEventHandler<XamlUICommand, CanExecuteRequestedEventArgs> CanExecuteRequested;
		public event TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> ExecuteRequested;

		public string Label
		{
			get => (string)this.GetValue(LabelProperty);
			set => this.SetValue(LabelProperty, value);
		}

		public Controls.IconSource IconSource
		{
			get => (Controls.IconSource)this.GetValue(IconSourceProperty);
			set => this.SetValue(IconSourceProperty, value);
		}

		public string Description
		{
			get => (string)this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		public ICommand Command
		{
			get => (ICommand)this.GetValue(CommandProperty);
			set => this.SetValue(CommandProperty, value);
		}

		public string AccessKey
		{
			get => (string)this.GetValue(AccessKeyProperty);
			set => this.SetValue(AccessKeyProperty, value);
		}

		public IList<KeyboardAccelerator> KeyboardAccelerators => (IList<KeyboardAccelerator>)this.GetValue(KeyboardAcceleratorsProperty);

		public static DependencyProperty AccessKeyProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(AccessKey),
			propertyType: typeof(string),
			ownerType: typeof(XamlUICommand),
			typeMetadata: new FrameworkPropertyMetadata(default(string)));

		public static DependencyProperty CommandProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(Command),
			propertyType: typeof(ICommand),
			ownerType: typeof(XamlUICommand),
			typeMetadata: new FrameworkPropertyMetadata(default(ICommand)));

		public static DependencyProperty DescriptionProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(Description),
			propertyType: typeof(string),
			ownerType: typeof(XamlUICommand),
			typeMetadata: new FrameworkPropertyMetadata(default(string)));

		public static DependencyProperty IconSourceProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(IconSource),
			propertyType: typeof(Controls.IconSource),
			ownerType: typeof(XamlUICommand),
			typeMetadata: new FrameworkPropertyMetadata(default(Controls.IconSource)));

		public static DependencyProperty KeyboardAcceleratorsProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(KeyboardAccelerators),
			propertyType: typeof(IList<KeyboardAccelerator>),
			ownerType: typeof(XamlUICommand),
			typeMetadata: new FrameworkPropertyMetadata(default(IList<KeyboardAccelerator>)));

		public static DependencyProperty LabelProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(Label),
			propertyType: typeof(string),
			ownerType: typeof(XamlUICommand),
			typeMetadata: new FrameworkPropertyMetadata(default(string)));

		public void NotifyCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, null);
		}

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
}
