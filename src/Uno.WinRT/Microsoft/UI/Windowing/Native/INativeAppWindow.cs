using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Microsoft.UI.Dispatching;

namespace Microsoft.UI.Windowing.Native;

internal interface INativeAppWindow
{
	string Title { get; set; }

	bool IsVisible { get; }

	float RasterizationScale { get; }

	PointInt32 Position { get; }

	SizeInt32 Size { get; }

	SizeInt32 ClientSize { get; }

	DispatcherQueue DispatcherQueue { get; }

	void Destroy();

	void Hide();

	void Move(PointInt32 position);

	void Resize(SizeInt32 size);

	void Show(bool activateWindow);

	void SetPresenter(AppWindowPresenter presenter);
	void SetIcon(string iconPath);
}
