#if HAS_UNO
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// The semantic DOM exposes a peer's subtree the way the peer reports it through
/// <see cref="AutomationPeer.GetChildren"/>, as UIA does.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
public class Given_WasmAriaPeerChildren
{
	[TestCleanup]
	public void Cleanup()
	{
		DisableAccessibility();
		TestServices.WindowHelper.WindowContent = null;
	}

	[TestMethod]
	public async Task When_Peer_Reports_No_Children_Then_Subtree_Is_Hidden_On_Initial_Build()
	{
		var hidden = new HiddenLetters("WAY");
		var visible = new TextBlock { Text = "Visible text" };
		await UITestHelper.Load(new StackPanel { Children = { hidden, visible } });

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(visible), message: "The control's sibling should be exposed.");
		await UITestHelper.WaitForIdle();

		AssertNoLetterExposed(hidden);
	}

	[TestMethod]
	public async Task When_Peer_Reports_No_Children_Then_Subtree_Is_Hidden_On_Dynamic_Add()
	{
		await EnsureAccessibilityEnabledAsync();

		var hidden = new HiddenLetters("WAY");
		var visible = new TextBlock { Text = "Visible text" };
		await UITestHelper.Load(new StackPanel { Children = { hidden, visible } });
		await UITestHelper.WaitFor(() => SemanticElementExists(visible), message: "The control's sibling should be exposed.");
		await UITestHelper.WaitForIdle();

		AssertNoLetterExposed(hidden);
	}

	[TestMethod]
	public async Task When_Child_Is_Added_Under_Hiding_Peer_Then_It_Is_Hidden()
	{
		var hidden = new HiddenLetters("WAY");
		var visible = new TextBlock { Text = "Visible text" };
		await UITestHelper.Load(new StackPanel { Children = { hidden, visible } });

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(visible), message: "The control's sibling should be exposed.");

		var added = new TextBlock { Text = "S" };
		hidden.Panel.Children.Add(added);
		var nested = new TextBlock { Text = "T" };
		hidden.Panel.Children.Add(new Border { Child = new Button { Content = nested } });
		var probe = new TextBlock { Text = "Added visible text" };
		((StackPanel)hidden.Parent).Children.Add(probe);
		await UITestHelper.WaitFor(() => SemanticElementExists(probe), message: "A sibling added at the same time should be exposed.");
		await UITestHelper.WaitForIdle();

		AssertNoLetterExposed(hidden);
		Assert.IsFalse(SemanticElementExists(added), "A child added under a peer that reports no children must stay hidden.");
	}

	[TestMethod]
	public async Task When_Hidden_Child_Is_Moved_Out_Then_It_Is_Exposed()
	{
		var hidden = new HiddenLetters("W");
		var visible = new TextBlock { Text = "Visible text" };
		var root = new StackPanel { Children = { hidden, visible } };
		await UITestHelper.Load(root);

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(visible), message: "The control's sibling should be exposed.");
		await UITestHelper.WaitForIdle();

		var letter = (TextBlock)hidden.Panel.Children[0];
		Assert.IsFalse(SemanticElementExists(letter), "The letter starts under a peer that reports no children.");

		hidden.Panel.Children.Remove(letter);
		root.Children.Add(letter);

		await UITestHelper.WaitFor(() => SemanticElementExists(letter), message: "Once moved out from under the hiding peer, the letter should be exposed.");
	}

	[TestMethod]
	public async Task When_Peer_Reports_Subset_Then_Only_Reported_Children_Are_Exposed()
	{
		var reported = new Button { Content = "Reported" };
		var omitted = new Button { Content = "Omitted" };
		var host = new SubsetHost(reported) { Content = new StackPanel { Children = { reported, new Border { Child = omitted } } } };
		await UITestHelper.Load(host);

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(reported), message: "The reported child should be exposed.");
		await UITestHelper.WaitForIdle();

		Assert.IsTrue(SemanticElementExists(host), "The host itself stays exposed.");
		Assert.IsFalse(SemanticElementExists(omitted), "A peer-bearing descendant the host peer does not report must not be exposed.");
	}

	[TestMethod]
	public async Task When_Default_Peers_Then_ItemsControl_Items_Stay_Exposed()
	{
		var first = new Button { Content = "First" };
		var second = new Button { Content = "Second" };
		var itemsControl = new ItemsControl { ItemsSource = new[] { first, second } };
		await UITestHelper.Load(itemsControl);

		EnableAccessibilityThroughDom();
		await UITestHelper.WaitFor(() => SemanticElementExists(first) && SemanticElementExists(second), message: "Items of a default ItemsControl should stay exposed.");
	}

	private static void AssertNoLetterExposed(HiddenLetters hidden)
	{
		foreach (var letter in hidden.Panel.Children.OfType<TextBlock>())
		{
			Assert.IsFalse(SemanticElementExists(letter), $"Letter '{letter.Text}' is under a peer that reports no children and must not be exposed.");
		}

		var exposedLetters = InvokeBrowserJs(
			"(function(){return Array.from(document.querySelectorAll('#uno-semantics-root p')).map(p => p.textContent).filter(t => t.length === 1).join(',');})()");
		Assert.AreEqual(string.Empty, exposedLetters, "No single-letter paragraph should be exposed.");
	}

	private sealed partial class HiddenLetters : UserControl
	{
		public HiddenLetters(string letters)
		{
			Panel = new StackPanel { Orientation = Orientation.Horizontal };
			foreach (var letter in letters)
			{
				Panel.Children.Add(new TextBlock { Text = letter.ToString() });
			}

			Content = Panel;
		}

		public StackPanel Panel { get; }

		protected override AutomationPeer OnCreateAutomationPeer() => new HiddenLettersPeer(this);

		private sealed class HiddenLettersPeer : FrameworkElementAutomationPeer
		{
			public HiddenLettersPeer(HiddenLetters owner) : base(owner) { }

			protected override bool IsControlElementCore() => false;

			protected override bool IsContentElementCore() => false;

			protected override IList<AutomationPeer> GetChildrenCore() => new List<AutomationPeer>();
		}
	}

	private sealed partial class SubsetHost : ContentControl
	{
		private readonly UIElement _reported;

		public SubsetHost(UIElement reported) => _reported = reported;

		protected override AutomationPeer OnCreateAutomationPeer() => new SubsetHostPeer(this);

		private sealed class SubsetHostPeer : FrameworkElementAutomationPeer
		{
			private readonly SubsetHost _owner;

			public SubsetHostPeer(SubsetHost owner) : base(owner) => _owner = owner;

			protected override IList<AutomationPeer> GetChildrenCore()
				=> new List<AutomationPeer> { _owner._reported.GetOrCreateAutomationPeer() };
		}
	}
}
#endif
