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
		private readonly UIGestureRecognizer _recognizer;

		public GestureHandler(UIElement owner)
		{
			if(owner == null)
			{
				throw new ArgumentNullException(nameof(owner));
			}

			_owner = owner;
			_recognizer = CreateRecognizer(owner);
		}

		protected abstract UIGestureRecognizer CreateRecognizer(UIElement owner);

		public bool IsAttached
		{
			get { return _owner.GestureRecognizers?.Contains(_recognizer) ?? false; }
		}

		public void Attach()
		{
			_owner.AddGestureRecognizer(_recognizer);
		}

		public void Detach()
		{
			_owner.RemoveGestureRecognizer(_recognizer);
		}
	}
}
