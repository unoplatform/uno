using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core;

partial class InputManager
{
	#region Drag and Drop
	private DragRoot _dragRoot;

	internal DragDropManager DragDrop { get; private set; }

	private void InitDragAndDrop()
	{
		var coreManager = CoreDragDropManager.CreateForCurrentView(); // So it's ready to be accessed by ui manager and platform extension
		var uiManager = DragDrop = new DragDropManager(this);

		coreManager.SetUIManager(uiManager);
	}

	internal IDisposable OpenDragAndDrop(DragView dragView)
	{
		var rootElement = ContentRoot.VisualTree.RootElement as Panel;

		if (rootElement is null)
		{
			return Disposable.Empty;
		}

		if (_dragRoot is null)
		{
			_dragRoot = new DragRoot();
			rootElement.Children.Add(_dragRoot);
		}

		_dragRoot.Show(dragView);

		return Disposable.Create(Remove);

		void Remove()
		{
			_dragRoot.Hide(dragView);

			if (_dragRoot.PendingDragCount == 0)
			{
				rootElement.Children.Remove(_dragRoot);
				_dragRoot = null;
			}
		}
	}
	#endregion
}
