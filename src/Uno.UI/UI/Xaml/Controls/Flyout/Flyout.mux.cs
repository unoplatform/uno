using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class Flyout
{
#if HAS_UNO // Custom implementation: Simulate enter/leave for KAs on all targets
	private ParentVisualTreeListener _parentVisualTreeListener;

	private void InitializeKeyboardAccelerators()
	{
		ListenToParentLifecycle();
	}

	private void ListenToParentLifecycle()
	{
		_parentVisualTreeListener = new ParentVisualTreeListener(
			this,
			() => EnterImpl(null, new EnterParams(true)),
			() => LeaveImpl(null, new LeaveParams(true)));
	}

	private void EnterImpl(DependencyObject pNamescopeOwner, EnterParams parameters)
	{
		// base.EnterImpl(pNamescopeOwner, parameters);

		var content = Content;

		if (content is not null)
		{
			var newParams = new EnterParams { IsForKeyboardAccelerator = true, IsLive = false };

			var uiElements = FindAllUIElements(content);
			foreach (var uiElement in uiElements)
			{
				if (uiElement.KeyboardAccelerators is KeyboardAcceleratorCollection kac)
				{
					kac.Enter(pNamescopeOwner, parameters);
				}
			}
		}
	}

	private void LeaveImpl(DependencyObject pNamescopeOwner, LeaveParams parameters)
	{
		var content = Content;

		if (content is not null)
		{
			var newParams = new EnterParams { IsForKeyboardAccelerator = true, IsLive = false };

			var uiElements = FindAllUIElements(content);
			foreach (var uiElement in uiElements)
			{
				if (uiElement.KeyboardAccelerators is KeyboardAcceleratorCollection kac)
				{
					kac.Leave(pNamescopeOwner, parameters);
				}
			}
		}
	}

	private IEnumerable<UIElement> FindAllUIElements(DependencyObject element)
	{
		if (element is UIElement uiElement)
		{
			yield return uiElement;
		}

		var children = VisualTreeHelper.GetChildren(element);
		foreach (var child in children)
		{
			foreach (var inner in FindAllUIElements(child))
			{
				yield return inner;
			}
		}
	}
#endif
}
