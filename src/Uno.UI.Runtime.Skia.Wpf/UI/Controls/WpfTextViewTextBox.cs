using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

[Bindable(true)]
internal class WpfTextViewTextBox : TextBox
{
	// private static readonly Style _style = CreateStyle();
	private static readonly Style _style = (Style)XamlReader.Parse(
		"""
		<Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			   xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			   xmlns:controls="clr-namespace:Uno.UI.Runtime.Skia.Wpf.UI.Controls;assembly=Uno.UI.Runtime.Skia.Wpf"
			   TargetType="{x:Type controls:WpfTextViewTextBox}">
			<Setter Property="SnapsToDevicePixels" Value="True" />
			<Setter Property="KeyboardNavigation.TabNavigation" Value="None" />
			<Setter Property="FocusVisualStyle" Value="{x:Null}" />
			<Setter Property="MinWidth" Value="0" />
			<Setter Property="MinHeight" Value="0" />
			<Setter Property="AllowDrop" Value="true" />
			<Setter Property="Template">
				<Setter.Value>
					<ControlTemplate TargetType="{x:Type controls:WpfTextViewTextBox}">
						<Border
							Name="Border"
							Margin="{TemplateBinding Margin}"
							Padding="{TemplateBinding Padding}"
							BorderThickness="0"
							CornerRadius="0">
							<Border.Background>
								<SolidColorBrush Color="Transparent" />
							</Border.Background>
							<Border.BorderBrush>
								<SolidColorBrush Color="Transparent" />
							</Border.BorderBrush>
							<ScrollViewer x:Name="PART_ContentHost" Margin="0" />
							<VisualStateManager.VisualStateGroups>
								<VisualStateGroup x:Name="CommonStates">
									<VisualState x:Name="Normal" />
									<VisualState x:Name="Disabled" />
									<VisualState x:Name="ReadOnly" />
									<VisualState x:Name="MouseOver" />
								</VisualStateGroup>
							</VisualStateManager.VisualStateGroups>
						</Border>
					</ControlTemplate>
				</Setter.Value>
			</Setter>
		</Style>
		""");

	public WpfTextViewTextBox()
	{
		Style = _style;
	}
}
