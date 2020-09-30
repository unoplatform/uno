using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls.Maps.Presenters
{
	public sealed partial class MapPresenter : Control
	{
		private readonly SerialDisposable _ownerSubscription = new SerialDisposable();

		public MapPresenter()
		{
			DefaultStyleKey = typeof(MapPresenter);

			Loading += (_, __) => UpdateOwnerSubscriptions();
			Unloaded += (_, __) => UpdateOwnerSubscriptions();

			InitializePartial();
		}

		partial void InitializePartial();

		private void UpdateOwnerSubscriptions()
		{
			_ownerSubscription.Disposable = null;

			_owner = TemplatedParent as MapControl;

			if (_owner != null)
			{
				_ownerSubscription.Disposable = UpdateOwnerSubscriptions(_owner);
			}
		}
	}
}
