import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import test from "node:test";
import { runInNewContext } from "node:vm";

// Use the MSBuild-produced bundle, or compile this project's tsconfig.json with
// --outFile bin/bridge-tests/Uno.Runtime.Wasm.js before running node --test.
const runtimePath = process.env.UNO_WASM_RUNTIME_JS
	?? new URL("../bin/bridge-tests/Uno.Runtime.Wasm.js", import.meta.url);
const runtime = readFileSync(runtimePath, "utf8");

function loadWindowWrapper() {
	const calls = [];
	const window = {
		resizeTo(...args) {
			assert.equal(this, window);
			calls.push({ method: "resizeTo", args });
		},
		moveTo(...args) {
			assert.equal(this, window);
			calls.push({ method: "moveTo", args });
		}
	};
	const context = { window, navigator: { platform: "" }, URL, Blob };
	runInNewContext(runtime, context, { filename: String(runtimePath) });
	return { wrapper: context.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper, calls };
}

for (const [method, browserMethod, argumentsToForward] of [
	["resizeWindow", "resizeTo", [[853, 479], [0, 0]]],
	["moveWindow", "moveTo", [[173, 291], [-47, 0]]]
]) {
	test(`${method} is exported on the class at its JavaScript import path`, () => {
		const { wrapper } = loadWindowWrapper();
		assert.equal(typeof wrapper[method], "function");
		assert.equal(Object.hasOwn(wrapper, method), true);
	});

	test(`${method} forwards both arguments to window.${browserMethod}`, () => {
		const { wrapper, calls } = loadWindowWrapper();
		const importedMethod = wrapper[method];
		for (const args of argumentsToForward) {
			importedMethod(...args);
		}
		assert.deepEqual(calls, argumentsToForward.map(args => ({ method: browserMethod, args })));
	});
}
