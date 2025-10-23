﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class ImageIcon
{
	private bool _initialized = false;
	private bool _applyTemplateCalled = false;

	private void EnsureInitialized()
	{
		InitializeVisualTree();

		// Uno workaround: OnApplyTemplate is not called when there is no template.
		if (!_applyTemplateCalled)
		{
			OnApplyTemplate();
			_applyTemplateCalled = true;
		}
	}

	private void InitializeVisualTree()
	{
		if (!_initialized)
		{
			var image = new Image
			{
				Stretch = Stretch.Uniform
			};

			AddIconChild(image);
			_initialized = true;
		}
	}
}
