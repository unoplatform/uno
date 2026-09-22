#nullable enable

using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
public class Given_WasmSemanticElements
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Tree_Keyboard_Navigation_Then_Hidden_And_Collapsed_Branches_Are_Skipped()
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();
		Assert.AreEqual("ok", InvokeBrowserJs(TreeNavigationScript));
#endif
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Password_Input_Then_Only_The_Edited_Range_Is_Replaced()
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();
		Assert.AreEqual("ok", InvokeBrowserJs(PasswordInputScript));
#endif
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Virtualized_Option_Is_Queued_Or_Reused_Then_Latest_State_And_Id_Are_Applied()
	{
#if HAS_UNO
		await EnsureAccessibilityEnabledAsync();
		try
		{
			InvokeBrowserJs(VirtualizedOptionScript);
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.getElementById('uno-semantics--2375600')?.dataset.result || 'pending'") != "pending",
				timeoutMS: 5000);
			Assert.AreEqual("ok", InvokeBrowserJs("document.getElementById('uno-semantics--2375600').dataset.result"));
		}
		finally
		{
			InvokeBrowserJs("""
				(function() {
					Uno.UI.Runtime.Skia.SemanticElements.unregisterVirtualizedContainer(-2375600);
					return 'ok';
				})()
				""");
		}
#endif
	}

#if HAS_UNO
	private const string TreeNavigationScript = """
		(function() {
			const semantic = Uno.UI.Runtime.Skia.SemanticElements;
			const getCallbacks = semantic.getCallbacks;
			semantic.getCallbacks = () => ({});
			const host = document.createElement('div');
			host.id = 'uno-semantics--2375500';
			document.body.appendChild(host);
			const previousFocus = document.activeElement;
			const get = handle => document.getElementById(`uno-semantics-${handle}`);
			const item = (parent, handle, expanded) => {
				semantic.createTreeItemElement(parent, handle, null, 0, 0, 100, 30,
					'Tree item', 1, expanded, false, 1, 2, true);
				return get(handle);
			};
			const navigate = (source, key, expected, scenario) => {
				source.focus();
				source.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true }));
				if (document.activeElement !== expected) {
					throw new Error(`${scenario}: ${key} focused ${document.activeElement?.id}, expected ${expected.id}`);
				}
			};
			try {
				semantic.createTreeElement(-2375500, -2375501, null, 0, 0, 200, 300, 'Tree', false, true);
				const first = item(-2375501, -2375502, 'none');
				const collapsed = item(-2375501, -2375503, 'false');
				item(-2375503, -2375504, 'true');
				item(-2375504, -2375505, 'none');
				const expanded = item(-2375501, -2375506, 'true');
				const hiddenGroup = document.createElement('div');
				hiddenGroup.id = 'uno-semantics--2375507';
				hiddenGroup.setAttribute('role', 'group');
				expanded.appendChild(hiddenGroup);
				item(-2375507, -2375508, 'none');
				const visibleChild = item(-2375506, -2375509, 'none');
				semantic.createTreeElement(-2375506, -2375510, null, 0, 0, 100, 30, 'Nested tree', false, true);
				item(-2375510, -2375511, 'none');

				for (const mode of ['hidden', 'aria-hidden', 'display', 'visibility']) {
					hiddenGroup.hidden = mode === 'hidden';
					hiddenGroup.setAttribute('aria-hidden', String(mode === 'aria-hidden'));
					hiddenGroup.style.display = mode === 'display' ? 'none' : '';
					hiddenGroup.style.visibility = mode === 'visibility' ? 'hidden' : '';
					navigate(first, 'ArrowDown', collapsed, mode);
					navigate(collapsed, 'ArrowDown', expanded, mode);
					navigate(expanded, 'ArrowUp', collapsed, mode);
					navigate(visibleChild, 'ArrowUp', expanded, `${mode}: bubbling`);
					navigate(expanded, 'ArrowRight', visibleChild, mode);
					navigate(visibleChild, 'Home', first, mode);
					navigate(first, 'End', visibleChild, `${mode}: nested tree`);
				}

				collapsed.hidden = true;
				navigate(first, 'ArrowDown', expanded, 'hidden ancestor');
				navigate(expanded, 'ArrowUp', first, 'hidden ancestor');
				collapsed.hidden = false;
				first.setAttribute('aria-hidden', 'true');
				visibleChild.hidden = true;
				navigate(expanded, 'Home', collapsed, 'hidden first item');
				navigate(collapsed, 'End', expanded, 'hidden last item');
				return 'ok';
			} finally {
				semantic.getCallbacks = getCallbacks;
				host.remove();
				if (previousFocus instanceof HTMLElement && previousFocus.isConnected) {
					previousFocus.focus();
				}
			}
		})()
		""";

	private const string VirtualizedOptionScript = """
		(function() {
			const semantic = Uno.UI.Runtime.Skia.SemanticElements;
			semantic.registerVirtualizedContainer(-2375600, 'listbox', 'Options', false);
			const host = document.getElementById('uno-semantics--2375600');
			host.dataset.result = 'pending';
			const add = (label, selected, automationId) =>
				semantic.addVirtualizedItem(-2375600, -2375601, 0, 1, 0, 0, 100, 30, 'option', label, selected, automationId);
			add('Old option', false, 'old-id');
			add('New option', false, 'new-id');
			semantic.updateSelectionState(-2375601, true);
			requestAnimationFrame(() => {
				try {
					const option = document.getElementById('uno-semantics--2375601');
					if (option?.getAttribute('aria-selected') !== 'true' ||
						option.getAttribute('xamlautomationid') !== 'new-id' ||
						option.getAttribute('aria-label') !== 'New option') {
						throw new Error('Queued realization lost the latest selection, label, or AutomationId.');
					}
					add('', false, '');
					semantic.updateSelectionState(-2375601, true);
					requestAnimationFrame(() => {
						try {
							if (option.getAttribute('aria-selected') !== 'true' ||
								option.hasAttribute('xamlautomationid') ||
								option.hasAttribute('aria-label')) {
								throw new Error('Reused option retained stale attributes or overwrote a newer selection.');
							}
							host.dataset.result = 'ok';
						} catch (error) {
							host.dataset.result = String(error);
						}
					});
				} catch (error) {
					host.dataset.result = String(error);
				}
			});
			return 'queued';
		})()
		""";

	private const string PasswordInputScript = """
		(function() {
			const semantic = Uno.UI.Runtime.Skia.SemanticElements;
			const getCallbacks = semantic.getCallbacks;
			let password = 'secret';
			let calls = 0;
			let regularInput = null;
			semantic.getCallbacks = () => ({
				onTextInput: (handle, value, start, end, replacementStart, replacementLength) => {
					if (handle === -2375701) {
						regularInput = { value, replacementStart, replacementLength };
						return;
					}
					password = replacementStart >= 0
						? password.slice(0, replacementStart) + value + password.slice(replacementStart + replacementLength)
						: value;
					calls++;
					semantic.updateTextBoxValue(handle, '•'.repeat(password.length), start, end);
				}
			});
			try {
				semantic.createTextBoxElement(0, -2375700, null, 0, 0, 100, 30,
					'•'.repeat(password.length), false, true, false, 0, 0, true);
				const input = document.getElementById('uno-semantics--2375700');
				const edit = (start, end, from, to, text, expected) => {
					input.setSelectionRange(start, end);
					input.dispatchEvent(new InputEvent('beforeinput', { inputType: 'insertText' }));
					input.value = input.value.slice(0, from) + text + input.value.slice(to);
					input.setSelectionRange(from + text.length, from + text.length);
					input.dispatchEvent(new InputEvent('input', { inputType: 'insertText' }));
					if (password !== expected || input.value !== '•'.repeat(expected.length)) {
						throw new Error('Password edit replaced unchanged characters or exposed cleartext.');
					}
				};
				edit(6, 6, 6, 6, '!', 'secret!');
				edit(0, 1, 0, 1, 'S', 'Secret!');
				edit(7, 7, 6, 7, '', 'Secret');
				edit(3, 3, 3, 3, '•', 'Sec•ret');
				edit(0, 7, 0, 7, 'reset', 'reset');

				input.setSelectionRange(2, 2);
				input.dispatchEvent(new CompositionEvent('compositionstart'));
				input.dispatchEvent(new InputEvent('beforeinput', { inputType: 'insertCompositionText' }));
				input.value = '••é•••';
				input.setSelectionRange(3, 3);
				const beforeComposition = calls;
				input.dispatchEvent(new InputEvent('input', { inputType: 'insertCompositionText', isComposing: true }));
				if (calls !== beforeComposition) {
					throw new Error('Intermediate password composition was committed.');
				}
				input.dispatchEvent(new CompositionEvent('compositionend', { data: 'é' }));
				input.dispatchEvent(new InputEvent('input', { inputType: 'insertFromComposition' }));
				if (password !== 'reéset' || calls !== beforeComposition + 1 || input.value !== '••••••') {
					throw new Error('Password composition was not committed exactly once.');
				}
				input.dispatchEvent(new CompositionEvent('compositionstart'));
				input.dispatchEvent(new CompositionEvent('compositionend', { data: '' }));
				edit(6, 6, 6, 6, '!', 'reéset!');

				semantic.createTextBoxElement(0, -2375701, null, 0, 0, 100, 30, '', false, false, false, 0, 0, true);
				const textBox = document.getElementById('uno-semantics--2375701');
				textBox.value = 'ordinary text';
				textBox.dispatchEvent(new InputEvent('input'));
				if (regularInput?.value !== 'ordinary text' || regularInput.replacementStart !== -1 ||
					regularInput.replacementLength !== -1) {
					throw new Error('Ordinary TextBox input must retain full-value replacement.');
				}
				return 'ok';
			} finally {
				semantic.getCallbacks = getCallbacks;
				document.getElementById('uno-semantics--2375700')?.remove();
				document.getElementById('uno-semantics--2375701')?.remove();
			}
		})()
		""";
#endif
}
