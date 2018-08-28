using Uno.UI.Controls;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Uno.Logging;
using System.Drawing;
using System.Linq;
using Windows.UI.Xaml.Input;
using Uno.Disposables;
using Uno.UI;
using View = Windows.UI.Xaml.UIElement;

namespace Windows.UI.Xaml.Controls
{
	public partial class Control
	{
		/// This binary compatibility workaround that can be removed
		public new static DependencyProperty IsEnabledProperty => FrameworkElement.IsEnabledProperty;
		
		public Control(string htmlTag = "div") : base(htmlTag)
		{
			InitializeControl();
		}

		partial void UnregisterSubView()
		{
			var child = this.GetChildren()?.FirstOrDefault();
			if (child != null)
			{
				RemoveChild(child);
			}
		}

		partial void RegisterSubView(View child)
		{
			AddChild(child);
		}

		/// <summary>
		/// Gets the first sub-view of this control or null if there is none
		/// </summary>
		public IFrameworkElement GetTemplateRoot()
		{
			return this.GetChildren()?.FirstOrDefault() as IFrameworkElement;
		}

		partial void OnFocusStateChangedPartial(FocusState oldValue, FocusState newValue)
		{
			//if (newValue == FocusState.Pointer && Focusable)
			//{
			//	//Set native focus to this view
			//	RequestFocus();
			//}
		}

		partial void OnIsFocusableChanged()
		{
			var isFocusable = IsFocusable && !IsDelegatingFocusToTemplateChild();

			SetAttribute("tabindex", isFocusable ? "0" : "-1");
		}

		protected virtual bool IsDelegatingFocusToTemplateChild() => false;

		private FocusState? _pendingFocusRequestState;
		protected virtual bool RequestFocus(FocusState state)
		{
			try
			{
				_pendingFocusRequestState = state;
				return FocusManager.Focus(this);
			}
			finally
			{
				_pendingFocusRequestState = null;
			}
		}

		internal void SetFocused(bool isFocused)
		{
			if (isFocused)
			{
				if (IsFocusable)
				{
					FocusState = _pendingFocusRequestState ?? FocusState.Pointer;
				}
			}
			else
			{
				Unfocus();
			}
		}
	}
}
