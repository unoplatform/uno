using SampleControl.Presentation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Extensions;
using System.Text;
using System.Collections.Generic;

namespace SamplesApp
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			sampleControl.DataContext = new SampleChooserViewModel(sampleControl);

			this.Loaded += (s, e) =>
			{
				this.SizeChanged += (s2, e2) =>
				{
					var tree = this.TreeGraph();
				};
			};
			this.SizeChanged += (s2, e2) =>
			{
				var tree = this.TreeGraph();
			};
			var tree = this.TreeGraph();
		}
		private static string DescribeVT(object x)
		{
			return new StringBuilder()
				.Append(x.GetType().Name)
				.Append((x as FrameworkElement)?.Name is string xname && xname.Length > 0 ? $"#{xname}" : string.Empty)
				.Append($" // {string.Join(", ", GetDetails())}")
				.ToString();

			IEnumerable<string> GetDetails()
			{
				//if (x is FrameworkElement fe) yield return $"TemplatedParent={DescribeElement(fe.TemplatedParent)}";
				//if (x is ContentControl cc)
				//{
				//	if (cc.GetBindingExpression(ContentControl.ContentProperty) is { ParentBinding.IsTemplateBinding: true } binding)
				//	{
				//		yield return $"ContentTP={DescribeElement(binding.TemplatedParentScopeDebug.TemplatedParent)}";
				//	}
				//}
				//if (x is ContentPresenter cp)
				//{
				//	if (cp.GetBindingExpression(ContentPresenter.ContentProperty) is { ParentBinding.IsTemplateBinding: true } binding)
				//	{
				//		yield return $"ContentTP={DescribeElement(binding.TemplatedParentScopeDebug.TemplatedParent)}";
				//	}
				//}
				//if (x is Border b)
				//{
				//	if (b.GetBindingExpression(Border.ChildProperty) is { ParentBinding.IsTemplateBinding: true } binding)
				//	{
				//		yield return $"ContentTP={DescribeElement(binding.TemplatedParentScopeDebug.TemplatedParent)}";
				//	}
				//}
				yield break;
			}
			//string DescribeElement(object x) => (x as FrameworkElement)?.Name is string xname && xname.Length > 0 ? $"{x.GetType().Name}#{xname}" : x?.GetType().Name;
		}
	}
}
