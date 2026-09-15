import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import vm from "node:vm";

const source = readFileSync(new URL("../../../src/Uno.WinRT/WasmScripts/Uno.AppNotifications.ServiceWorker.js", import.meta.url), "utf8");
const appId = "https://example.test/app/";
const clientUrl = `${appId}index.html`;

function createHarness(windows) {
	const listeners = new Map();
	const timers = new Map();
	const channels = [];
	const opened = [];
	const warnings = [];
	let nextTimer = 0;
	let closeCount = 0;

	class ServiceWorkerGlobalScope { }
	class MessageChannel {
		constructor() {
			const createPort = () => ({
				onmessage: null,
				closed: false,
				close() { this.closed = true; },
			});
			this.port1 = createPort();
			this.port2 = createPort();
			this.port2.postMessage = data => queueMicrotask(() => {
				if (!this.port1.closed) {
					this.port1.onmessage?.({ data });
				}
			});
			channels.push(this);
		}
	}

	const scope = Object.assign(new ServiceWorkerGlobalScope(), {
		location: { href: `https://example.test/worker.js?uno-app-id=${encodeURIComponent(appId)}` },
		addEventListener: (name, callback) => listeners.set(name, callback),
		setTimeout: callback => {
			const id = ++nextTimer;
			timers.set(id, callback);
			return id;
		},
		clearTimeout: id => timers.delete(id),
		clients: {
			matchAll: async () => windows,
			openWindow: async url => {
				opened.push(url);
				return { url };
			},
		},
	});

	vm.runInNewContext(source, {
		self: scope,
		ServiceWorkerGlobalScope,
		MessageChannel,
		DOMException,
		URL,
		console: { warn: (...message) => warnings.push(message) },
	});

	const dispatch = type => {
		let completion;
		listeners.get(type)({
			action: "",
			notification: {
				data: {
					unoAppNotification: {
						id: 42,
						appId,
						clientUrl,
						appBaseUrl: appId,
						activationToken: "activation-token",
					},
				},
				close: () => closeCount++,
			},
			waitUntil: task => { completion = task; },
		});
		assert.ok(completion, "The service-worker event must keep its asynchronous work alive.");
		return completion;
	};

	return {
		channels,
		timers,
		opened,
		warnings,
		dispatch,
		get closeCount() { return closeCount; },
		expireTimers() {
			for (const callback of [...timers.values()]) {
				callback();
			}
		},
	};
}

function assertChannelsReleased(harness) {
	assert.equal(harness.timers.size, 0);
	for (const channel of harness.channels) {
		assert.equal(channel.port1.closed, true);
		assert.equal(channel.port2.closed, true);
	}
}

test("an acknowledged activation focuses its client and releases the channel", async () => {
	let focused = 0;
	const harness = createHarness([{
		url: clientUrl,
		postMessage: (_, [port]) => port.postMessage({ accepted: true }),
		focus: async () => { focused++; },
	}]);

	await harness.dispatch("notificationclick");

	assert.equal(focused, 1);
	assert.equal(harness.closeCount, 1);
	assert.equal(harness.opened.length, 0);
	assertChannelsReleased(harness);
});

test("a closing client does not prevent another client from accepting activation", async () => {
	let focused = 0;
	const harness = createHarness([
		{
			url: `${appId}closing.html`,
			postMessage: () => { throw new DOMException("The client is closing.", "InvalidStateError"); },
		},
		{
			url: clientUrl,
			postMessage: (_, [port]) => port.postMessage({ accepted: true }),
			focus: async () => { focused++; },
		},
	]);

	await harness.dispatch("notificationclick");

	assert.equal(focused, 1);
	assert.equal(harness.closeCount, 1);
	assert.equal(harness.warnings.length, 1);
	assertChannelsReleased(harness);
});

test("a closing exact-match client falls back to a newly opened window", async () => {
	const harness = createHarness([{
		url: clientUrl,
		postMessage: () => { throw new DOMException("The client is closing.", "InvalidStateError"); },
		navigate: async () => { throw new DOMException("The client has closed.", "InvalidStateError"); },
	}]);

	await harness.dispatch("notificationclick");

	assert.equal(harness.opened.length, 1);
	assert.equal(new URL(harness.opened[0]).searchParams.get("uno-app-notification"), "activation-token");
	assert.equal(harness.closeCount, 1);
	assertChannelsReleased(harness);
});

test("a rejected activation can navigate the existing window", async () => {
	let navigated;
	let focused = 0;
	const harness = createHarness([{
		url: clientUrl,
		postMessage: (_, [port]) => port.postMessage({ accepted: false }),
		navigate: async url => {
			navigated = url;
			return { focus: async () => { focused++; } };
		},
	}]);

	await harness.dispatch("notificationclick");

	assert.equal(new URL(navigated).searchParams.get("uno-app-notification"), "activation-token");
	assert.equal(focused, 1);
	assert.equal(harness.opened.length, 0);
	assertChannelsReleased(harness);
});

test("an acknowledgement timeout releases its channel and opens a window", async () => {
	let signalPosted;
	const posted = new Promise(resolve => { signalPosted = resolve; });
	const harness = createHarness([{
		url: `${appId}unresponsive.html`,
		postMessage: () => signalPosted(),
	}]);
	const completion = harness.dispatch("notificationclick");
	await posted;
	assert.equal(harness.timers.size, 1);

	harness.expireTimers();
	await completion;

	assert.equal(harness.opened.length, 1);
	assert.equal(harness.closeCount, 1);
	assertChannelsReleased(harness);
});

test("unexpected postMessage errors are reported without leaking a channel", async () => {
	const harness = createHarness([{
		url: clientUrl,
		postMessage: () => { throw new TypeError("Invalid message transport."); },
	}]);

	await harness.dispatch("notificationclick");

	assert.equal(harness.warnings.length, 1);
	assert.equal(harness.opened.length, 0);
	assert.equal(harness.closeCount, 0);
	assertChannelsReleased(harness);
});

test("a closed client does not prevent other clients from receiving dismissal", async () => {
	const messages = [];
	const harness = createHarness([
		{ postMessage: () => { throw new DOMException("The client has closed.", "InvalidStateError"); } },
		{ postMessage: message => messages.push(message) },
	]);

	await harness.dispatch("notificationclose");

	assert.equal(messages.length, 1);
	assert.equal(messages[0].type, "uno-app-notification-closed");
	assert.equal(messages[0].id, 42);
	assert.equal(harness.warnings.length, 1);
});
