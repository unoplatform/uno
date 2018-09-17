using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;

namespace Windows.UI.Xaml
{
	internal abstract class GestureHandler
	{
		private readonly UIElement _owner;
		private readonly Lazy<UIGestureRecognizer> _recognizer;

		protected GestureHandler(UIElement owner)
		{
			_owner = owner ?? throw new ArgumentNullException(nameof(owner));
			_recognizer = new Lazy<UIGestureRecognizer>(() => CreateRecognizer(owner));
		}

		protected abstract UIGestureRecognizer CreateRecognizer(UIElement owner);

		public bool IsAttached => _owner.GestureRecognizers?.Contains(_recognizer.Value) ?? false;

		public void Attach()
		{
			_owner.AddGestureRecognizer(_recognizer.Value);
		}

		public void Detach()
		{
			_owner.RemoveGestureRecognizer(_recognizer.Value);
		}
	}
}
