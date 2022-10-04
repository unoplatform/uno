using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace UnoSolutionTemplate.Wizard.Forms
{
	public partial class UnoOptions : UnoOptionsBaseForm
	{
		public bool UseWebAssembly => checkWebAssembly.Checked;
		public bool UseiOS => checkiOS.Checked;
		public bool UseAndroid => checkAndroid.Checked;
		public bool UseCatalyst => checkCatalyst.Checked;
		public bool UseAppKit => checkAppKit.Checked;
		public bool UseGtk => checkGtk.Checked;
		public bool UseFramebuffer => checkLinux.Checked;
		public bool UseWpf => checkWpf.Checked;
		public bool UseWinUI => checkWinUI.Checked;
		public bool UseServer => checkServer.Checked;
		public string UseBaseTargetFramework => BaseTargetFramework.SelectedItem is TargetFrameworkOption option ? option.BaseValue : "invalid";

		public class TargetFrameworkOption
		{
			public string BaseValue { get; set; }
			public string DisplayValue { get; set; }
		}

		public UnoOptions(IServiceProvider serviceProvider)
			: base(serviceProvider)
		{
			InitializeComponent();

			InitializeFont();

			BaseTargetFramework.Items.Add(new TargetFrameworkOption { BaseValue = "net6.0", DisplayValue = ".NET 6.0" });
			BaseTargetFramework.Items.Add(new TargetFrameworkOption { BaseValue = "net7.0", DisplayValue = ".NET 7.0" });
			BaseTargetFramework.SelectedIndex = 0;
		}

		private void UnoOptions_Load(object sender, EventArgs e)
		{
			ResizeLabelDescription(labelDescription);
			CenterToParent();
		}

		private void checkWebAssembly_CheckedChanged(object sender, EventArgs e)
		{
		}
	}
}
