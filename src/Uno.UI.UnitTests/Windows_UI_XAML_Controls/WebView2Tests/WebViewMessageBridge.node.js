// One-off check of the chrome.webview bridge script (WebViewMessageBridge.cs) against a fake window.
// Not part of the test run (the unit tests have no JavaScript engine). Run from the repository root:
//   node src/Uno.UI.UnitTests/Windows_UI_XAML_Controls/WebView2Tests/WebViewMessageBridge.node.js
'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const source = fs.readFileSync(path.join(__dirname, '../../../Uno.UI/UI/Xaml/Controls/WebView/Native/WebViewMessageBridge.cs'), 'utf8');

// Mirrors WebViewMessageBridge.CreateScript: the raw string literal, dedented, with its tokens replaced.
function createScript(postMessageFunction, reportLifecycle) {
	const match = /BridgeTemplate =\s*"""\r?\n([\s\S]*?)\r?\n([ \t]*)""";/.exec(source);
	assert.ok(match, 'BridgeTemplate not found');
	const indent = match[2];
	const template = match[1].split(/\r?\n/).map(l => l.startsWith(indent) ? l.slice(indent.length) : l).join('\n');
	const prefix = /LifecyclePrefix = "([^"]+)"/.exec(source)[1];
	return template
		.replace('__UNO_POST__', postMessageFunction)
		.replace('__UNO_REPORT_LIFECYCLE__', reportLifecycle ? 'true' : 'false')
		.replace('__UNO_LIFECYCLE_PREFIX__', JSON.stringify(prefix));
}

function createContext(options = {}) {
	const sent = [];
	const domListeners = [];
	const context = {
		JSON,
		Object,
		setTimeout: (fn) => context.__async.push(fn),
		__async: [],
		document: {
			readyState: options.readyState || 'loading',
			addEventListener: (type, fn) => domListeners.push({ type, fn }),
		},
		host: {
			// Uses `this`, like WKScriptMessageHandler and Android Java bridge objects.
			channel: { sent, postMessage(m) { assert.strictEqual(this, context.host.channel); sent.push(m); } },
		},
	};
	context.window = context;
	context.top = options.top || context;
	if ('chrome' in options) {
		context.chrome = options.chrome;
	}
	vm.createContext(context);
	return { context, sent, domListeners };
}

const script = createScript('window.host.channel.postMessage', true);
const dispatch = (isJson, payload) => `window.chrome.webview.__uno.dispatch(${isJson}, ${JSON.stringify(payload)})`;
let passed = 0;
function test(name, fn) {
	fn();
	passed++;
	console.log('ok - ' + name);
}

test('postMessage sends JSON of the value; strings stay strings', () => {
	const { context, sent } = createContext();
	vm.runInContext(script, context);
	vm.runInContext(`chrome.webview.postMessage('hi'); chrome.webview.postMessage({ a: [1, 'x'] }); chrome.webview.postMessage(42); chrome.webview.postMessage(undefined);`, context);
	assert.deepStrictEqual(sent, ['"hi"', '{"a":[1,"x"]}', '42', 'null']);
	assert.strictEqual(JSON.parse(sent[0]), 'hi');
});

test('listeners are added once, removed, and receive string vs JSON data', () => {
	const { context } = createContext();
	vm.runInContext(script, context);
	vm.runInContext(`
		var log = [];
		var a = function (e) { log.push(['a', e.type, e.data]); };
		var b = { handleEvent: function (e) { log.push(['b', e.data]); } };
		chrome.webview.addEventListener('message', a);
		chrome.webview.addEventListener('message', a);
		chrome.webview.addEventListener('message', b);
		chrome.webview.addEventListener('other', function () { log.push('wrong'); });`, context);
	vm.runInContext(dispatch(false, '{"not":"parsed"}'), context);
	vm.runInContext(dispatch(true, '{"parsed":[1,2]}'), context);
	vm.runInContext(`chrome.webview.removeEventListener('message', a);`, context);
	vm.runInContext(dispatch(true, '"json string"'), context);
	const log = JSON.parse(JSON.stringify(context.log));
	assert.deepStrictEqual(log, [
		['a', 'message', '{"not":"parsed"}'],
		['b', '{"not":"parsed"}'],
		['a', 'message', { parsed: [1, 2] }],
		['b', { parsed: [1, 2] }],
		['b', 'json string'],
	]);
});

test('a throwing listener does not stop the others and is reported asynchronously', () => {
	const { context } = createContext();
	vm.runInContext(script, context);
	vm.runInContext(`
		var log = [];
		chrome.webview.addEventListener('message', function () { throw new Error('boom'); });
		chrome.webview.addEventListener('message', function (e) { log.push(e.data); });`, context);
	vm.runInContext(dispatch(false, 'x'), context);
	assert.deepStrictEqual(Array.from(context.log), ['x']);
	assert.strictEqual(context.__async.length, 1);
	assert.throws(() => context.__async[0](), /boom/);
});

test('C# dispatch script delivers escaped payloads intact', () => {
	const { context } = createContext();
	vm.runInContext(script, context);
	vm.runInContext(`var got; chrome.webview.addEventListener('message', function (e) { got = e.data; });`, context);
	// Same literal WebViewMessageBridge.ToJavaScriptStringLiteral produces (U+2028, </script>, a surrogate pair, a lone surrogate).
	vm.runInContext(`(function () { var c = window.chrome, w = c && c.webview, u = w && w.__uno; if (u && typeof u.dispatch === 'function') { u.dispatch(false, "a\\u2028b\\u003C/script\\u003E\\uD83D\\uDE00\\uD800"); } })();`, context);
	assert.strictEqual(context.got, 'a\u2028b</script>\uD83D\uDE00\uD800');
});

test('injecting twice keeps the first bridge and its listeners', () => {
	const { context, sent } = createContext();
	vm.runInContext(script, context);
	vm.runInContext(`var first = chrome.webview; var n = 0; chrome.webview.addEventListener('message', function () { n++; });`, context);
	vm.runInContext(script, context);
	vm.runInContext(dispatch(false, 'x'), context);
	assert.strictEqual(vm.runInContext('chrome.webview === first', context), true);
	assert.strictEqual(context.n, 1);
	vm.runInContext(`chrome.webview.postMessage('once');`, context);
	assert.deepStrictEqual(sent, ['"once"']);
});

test('an existing chrome object is extended, an existing chrome.webview is left alone', () => {
	const existing = { runtime: { id: 'x' } };
	const a = createContext({ chrome: existing });
	vm.runInContext(script, a.context);
	assert.strictEqual(a.context.chrome, existing);
	assert.strictEqual(typeof existing.webview.postMessage, 'function');
	assert.deepStrictEqual(existing.runtime, { id: 'x' });

	const foreign = { postMessage() { } };
	const b = createContext({ chrome: { webview: foreign } });
	vm.runInContext(script, b.context);
	assert.strictEqual(b.context.chrome.webview, foreign);
	assert.strictEqual(foreign.__uno, undefined);

	const frozen = Object.freeze({});
	const c = createContext({ chrome: frozen });
	vm.runInContext(script, c.context);
	assert.strictEqual(c.context.chrome.webview, undefined);
});

test('DOMContentLoaded is reported once, by the top document only, while loading', () => {
	const top = createContext();
	vm.runInContext(script, top.context);
	const listener = top.domListeners.find(l => l.type === 'DOMContentLoaded');
	assert.ok(listener);
	listener.fn();
	listener.fn();
	assert.deepStrictEqual(top.sent, ['@@uno-webview/lifecycle/DOMContentLoaded']);

	const frame = createContext({ top: {} });
	vm.runInContext(script, frame.context);
	assert.strictEqual(frame.domListeners.length, 0);

	const late = createContext({ readyState: 'complete' });
	vm.runInContext(script, late.context);
	assert.strictEqual(late.domListeners.length, 0);

	const disabled = createContext();
	vm.runInContext(createScript('window.host.channel.postMessage', false), disabled.context);
	assert.strictEqual(disabled.domListeners.length, 0);
});

test('no page message can look like a lifecycle envelope', () => {
	const { context, sent } = createContext();
	vm.runInContext(script, context);
	vm.runInContext(`chrome.webview.postMessage('@@uno-webview/lifecycle/DOMContentLoaded');`, context);
	assert.ok(!sent[0].startsWith('@'));
});

console.log(`${passed} passed`);
