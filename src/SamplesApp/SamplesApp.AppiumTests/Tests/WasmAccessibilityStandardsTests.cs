#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
[TestCategory(TestCategories.WasmHostRequired)]
public sealed class WasmAccessibilityStandardsTests : AppiumFixtureBase
{
	protected override string SampleQuery => AccessibilityScreenReaderSnapshotDefinition.SampleQuery;

	[TestMethod]
	public void SemanticDom_HasValidRolesNamesRelationshipsAndTabStops()
	{
		if (Session.Options.Platform != AppiumPlatform.Wasm)
		{
			Assert.Inconclusive(
				$"{nameof(WasmAccessibilityStandardsTests)} requires {AppiumTestOptions.EnvVarPlatform}=wasm.");
			return;
		}

		var scriptResult = ((IJavaScriptExecutor)Session.Driver).ExecuteScript(
			"""
			const root = document.getElementById('uno-semantics-root');
			if (!root) {
				return 'semantic root is missing';
			}

			const validRoles = new Set([
				'alert', 'alertdialog', 'application', 'article', 'banner', 'button',
				'cell', 'checkbox', 'columnheader', 'combobox', 'complementary',
				'contentinfo', 'definition', 'dialog', 'directory', 'document', 'feed',
				'figure', 'form', 'grid', 'gridcell', 'group', 'heading', 'img', 'link',
				'list', 'listbox', 'listitem', 'log', 'main', 'marquee', 'math', 'menu',
				'menubar', 'menuitem', 'menuitemcheckbox', 'menuitemradio', 'meter',
				'navigation', 'none', 'note', 'option', 'presentation', 'progressbar',
				'radio', 'radiogroup', 'region', 'row', 'rowgroup', 'rowheader',
				'scrollbar', 'search', 'searchbox', 'separator', 'slider', 'spinbutton',
				'status', 'switch', 'tab', 'table', 'tablist', 'tabpanel', 'term',
				'textbox', 'timer', 'toolbar', 'tooltip', 'tree', 'treegrid', 'treeitem'
			]);
			const relationshipAttributes = [
				'aria-activedescendant',
				'aria-controls',
				'aria-describedby',
				'aria-details',
				'aria-errormessage',
				'aria-flowto',
				'aria-labelledby',
				'aria-owns'
			];
			const violations = [];
			const seenIds = new Set();

			if (!root.querySelector('p, span')) {
				violations.push('standalone body text has no p/span semantic node');
			}

			const referencedText = (element, attribute) =>
				(element.getAttribute(attribute) || '')
					.split(/\s+/)
					.filter(Boolean)
					.map(id => document.getElementById(id)?.textContent?.trim() || '')
					.filter(Boolean)
					.join(' ')
					.trim();
			const hasAccessibleName = element =>
				(element.getAttribute('aria-label') || '').trim().length > 0 ||
				referencedText(element, 'aria-labelledby').length > 0;

			for (const element of root.querySelectorAll('*')) {
				if (element.id) {
					if (seenIds.has(element.id)) {
						violations.push(`duplicate id: ${element.id}`);
					}
					seenIds.add(element.id);
				}

				const role = (element.getAttribute('role') || '').trim();
				if (role && !validRoles.has(role)) {
					violations.push(`invalid role '${role}' on #${element.id || '(no id)'}`);
				}

				if ((role === 'region' || role === 'form') && !hasAccessibleName(element)) {
					violations.push(`unnamed ${role} on #${element.id || '(no id)'}`);
				}

				for (const attribute of relationshipAttributes) {
					for (const id of (element.getAttribute(attribute) || '').split(/\s+/).filter(Boolean)) {
						if (!document.getElementById(id)) {
							violations.push(`dangling ${attribute}='${id}' on #${element.id || '(no id)'}`);
						}
					}
				}

				const tag = element.tagName.toLowerCase();
				const isStaticText = /^h[1-6]$/.test(tag) || tag === 'p' || tag === 'span';
				const isNonInteractiveContainer =
					role === 'region' || role === 'main' || role === 'navigation' ||
					role === 'form' || role === 'group';
				if ((isStaticText || isNonInteractiveContainer) &&
					element.hasAttribute('tabindex') &&
					Number.parseInt(element.getAttribute('tabindex'), 10) >= 0) {
					violations.push(`noninteractive tab stop on #${element.id || '(no id)'}`);
				}
			}

			return violations.sort().join('\n');
			""");

		if (scriptResult is not string violations)
		{
			Assert.Fail(
				$"The semantic DOM standards scan returned {scriptResult?.GetType().FullName ?? "null"} instead of a string " +
				$"({Session.DiagnosticContext}).");
			return;
		}

		violations.Should().BeEmpty(
			$"the external semantic DOM must satisfy its role/name/relationship/tab-order contract ({Session.DiagnosticContext})");
	}
}
