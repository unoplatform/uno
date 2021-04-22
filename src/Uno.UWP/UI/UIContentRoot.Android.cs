using System;
using System.Linq;
using Windows.UI.Composition;
using Android.App;
using Android.Graphics;
using Android.Views;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI
{
	partial class UIContentRoot
	{
		internal UIContentRoot(Activity rootActivity)
		{
			
		}

		internal View Content { get; set; }

		internal Visual ContentVisual { get; }
	}

	//internal class RootVisual : Visual
	//{
	//	/// <inheritdoc />
	//	internal RootVisual(Compositor compositor)
	//		: base(compositor)
	//	{
	//		Comment = "ui-root";
	//	}

	//	public void RenderTo(RenderNode rootNode)
	//	{
	//		if (Edit(rootNode) is { } session)
	//		{
	//			try
	//			{
	//				Draw(session.Canvas);
	//			}
	//			catch (Exception)
	//			{
	//				if (this.Log().IsEnabled(LogLevel.Error))
	//				{
	//					this.Log().Error($"Failed to render visual {Comment}");
	//				}
	//			}
	//			finally
	//			{
	//				session.Dispose();
	//			}
	//		}
	//	}
	//}
}
