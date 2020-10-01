using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample]
	public sealed partial class DragDrop_Basics : UserControl
	{
		public DragDrop_Basics()
		{
			this.InitializeComponent();

			SubscribeDropEvents(_theTarget);
			SubscribeDropEvents(_theNestedTarget);
		}

		private void SubscribeDropEvents(FrameworkElement elt)
		{
			elt.DragEnter += GetHandler("ENTER");
			elt.DragOver += GetHandler("OVER");
			elt.DragLeave += GetHandler("LEAVE");
			elt.Drop += GetHandler("DROP");

			DragEventHandler GetHandler(string evt) => (snd, args) =>
			{
				if (snd == args.OriginalSource)
				{
					_output.Text = $"[{evt}] {GetName(snd)}";
					args.AcceptedOperation = DataPackageOperation.Copy;
				}
			};
		}

		private static string GetName(object uiElt) => uiElt is null ? "--null--" : (uiElt as FrameworkElement)?.Name ?? uiElt.GetType().Name;
	}
}
