using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

public class TextBoxExtensions
{
	public static InputReturnType GetInputReturnType(DependencyObject obj) => (InputReturnType)obj.GetValue(InputReturnTypeProperty);

	public static void SetInputReturnType(DependencyObject obj, InputReturnType value) => obj.SetValue(InputReturnTypeProperty, value);

	[DynamicDependency(nameof(GetInputReturnType))]
	[DynamicDependency(nameof(SetInputReturnType))]
	public static readonly DependencyProperty InputReturnTypeProperty =
		DependencyProperty.RegisterAttached(
			nameof(InputReturnType),
			typeof(InputReturnType),
			typeof(TextBox),
			new FrameworkPropertyMetadata(InputReturnType.Default, OnInputReturnTypeChanged));

	private static void OnInputReturnTypeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		var owner = dependencyObject as TextBox;
		if (owner == null)
		{
			return;
		}

		owner.OnInputReturnTypeChanged((InputReturnType)args.NewValue, initial: false);
	}
}
