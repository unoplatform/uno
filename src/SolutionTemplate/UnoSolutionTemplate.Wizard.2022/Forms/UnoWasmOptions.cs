using System;
using Microsoft.VisualStudio.Shell;

namespace UnoSolutionTemplate.Wizard.Forms
{
	public partial class UnoWasmOptions : UnoOptionsBaseForm
	{
		public bool UseManifestJson => checkManifestJson.Checked;

		public UnoWasmOptions(IServiceProvider serviceProvider)
			: base(serviceProvider)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			InitializeComponent();

			InitializeFont();
		}

		private void UnoWasmOptions_Load(object sender, EventArgs e)
		{
			ResizeLabelDescription(labelDescription);
			CenterToParent();
		}
	}
}
