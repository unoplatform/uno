using System;
using System.Drawing;
using System.Windows.Input;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	public partial class MenuFlyoutItem : MenuFlyoutItemBase
	{
		public MenuFlyoutItem()
		{

		}

		#region CommandParameter

		public object CommandParameter
		{
			get { return (object)GetValue(CommandParameterProperty); }
			set { SetValue(CommandParameterProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty CommandParameterProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"CommandParameter", typeof(object),
				typeof(global::Windows.UI.Xaml.Controls.MenuFlyoutItem),
				new FrameworkPropertyMetadata(default(object)));

		#endregion

		#region Command

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty CommandProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"Command", typeof(global::System.Windows.Input.ICommand),
				typeof(global::Windows.UI.Xaml.Controls.MenuFlyoutItem),
				new FrameworkPropertyMetadata(default(global::System.Windows.Input.ICommand)));

		#endregion

		#region Text

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty TextProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"Text", typeof(string),
				typeof(global::Windows.UI.Xaml.Controls.MenuFlyoutItem),
				new FrameworkPropertyMetadata(default(string)));
		
		#endregion
	}
}
