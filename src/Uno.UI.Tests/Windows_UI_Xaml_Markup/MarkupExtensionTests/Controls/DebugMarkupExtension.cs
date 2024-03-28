using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.MarkupExtensionTests.Controls;

internal partial class DebugMarkupExtension : MarkupExtension
{
	public enum DebugBehavior { ReturnProvider, AssignToTag, AssignToAttachableValue }

	public DebugBehavior Behavior { get; set; } = DebugBehavior.ReturnProvider;

	protected override object ProvideValue(IXamlServiceProvider serviceProvider)
	{
		if (Behavior == DebugBehavior.ReturnProvider)
		{
			return serviceProvider;
		}
		else
		{
			if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget pvt &&
				pvt.TargetObject is FrameworkElement fe &&
				pvt.TargetProperty is ProvideValueTargetProperty pvtp)
			{
				if (Behavior == DebugBehavior.AssignToTag)
				{
					fe.Tag = serviceProvider;
				}
				else
				{
					Attachable.SetValue(fe, serviceProvider);
				}

				return pvtp.Type.IsValueType ? Activator.CreateInstance(pvtp.Type) : null;
			}

			// shouldn't reach here if we generated the correct xaml source
			// but if we do, then there is no recovery.
			throw new InvalidOperationException("IProvideValueTarget is invalid or not provided.");
		}
	}
}
