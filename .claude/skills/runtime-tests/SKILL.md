---
description: Build and run Uno Platform runtime tests with proper base64 filter encoding. Use when running specific runtime tests or all runtime tests.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **Run Tests Skill**. This skill builds and runs Uno Platform runtime tests with the correct base64 filter encoding that the test runner requires. It handles the non-obvious encoding requirement and provides correct platform-specific commands.

---

## Execution Workflow

### Phase 0: Parse User Input

Determine what to run from the user's input:
- **All tests**: No filter needed
- **Specific test class**: e.g., `Given_Button` → resolve to fully qualified name
- **Specific test method**: e.g., `Given_Button.When_ContentSet` → resolve to fully qualified name
- **Multiple tests**: Pipe-separated list of fully qualified names

If the user provides partial names, search `src/Uno.UI.RuntimeTests/Tests/` to resolve fully qualified test names (namespace + class + method).

**Determine target platform** from user input. Supported platforms:

| Platform | Keyword(s) | Default |
|----------|-----------|---------|
| Skia Desktop | `desktop`, `skia`, or unspecified | **Yes** |
| Skia WASM | `wasm`, `webassembly`, `browser` | No |

Default: **Skia Desktop** (fastest build and execution).

Choose **Skia WASM** only when:
- The user explicitly asks for WASM/browser testing, OR
- The bug or behavior is WASM-specific (e.g., DOM interaction, browser rendering, JS interop)

### Phase 1: Build the Test App

**CRITICAL**: Set timeout to 15+ minutes. **NEVER cancel builds.**

#### Skia Desktop (default)
```bash
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release -f net10.0
```

#### Skia WASM
```bash
dotnet publish src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/SamplesApp.Skia.WebAssembly.Browser.csproj -c Release -f net10.0
```

Note: WASM requires `publish` (not just `build`) to produce the static web assets needed for hosting.

If the build fails:
1. Read the error output
2. Diagnose the issue (missing restore, compilation error, etc.)
3. Fix or report the issue before attempting to run tests

### Phase 2: Construct the Filter

If running specific tests (not all tests):

1. **Format the filter string**: Fully qualified test names, pipe-separated
   - Single test: `Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Control.When_Scenario`
   - Multiple tests: `Test1|Test2|Test3`

2. **Base64 encode the filter** (the `UITEST_RUNTIME_TESTS_FILTER` env var accepts ONLY base64-encoded values):

   **bash (Windows Git Bash / Linux / macOS):**
   ```bash
   export UITEST_RUNTIME_TESTS_FILTER=$(echo -n "fully.qualified.TestName" | base64)
   ```

   **PowerShell:**
   ```powershell
   $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("fully.qualified.TestName"))
   ```

3. **For multiple tests**, join with `|` before encoding:
   ```bash
   export UITEST_RUNTIME_TESTS_FILTER=$(echo -n "Namespace.Test1|Namespace.Test2" | base64)
   ```

4. **WASM-specific**: When passing the filter via URL query parameter, replace `=` with `!` in the base64 string to avoid URL parsing issues. The app reverses this substitution before decoding:
   ```bash
   FILTER_B64=$(echo -n "fully.qualified.TestName" | base64)
   FILTER_B64_ESCAPED=${FILTER_B64//=/!}
   ```

### Phase 3: Run Tests

#### Skia Desktop (default)

Navigate to the build output and execute:
```bash
cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0
dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

If running filtered tests, set the env var first:
```bash
export UITEST_RUNTIME_TESTS_FILTER=$(echo -n "fully.qualified.TestName" | base64) && cd src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0 && dotnet SamplesApp.Skia.Generic.dll --runtime-tests=test-results.xml
```

Results are output in NUnit XML format to `test-results.xml` (relative to CWD).

#### Skia WASM

WASM tests run in a browser. The published app is served over HTTP, and test parameters are passed as URL query parameters. The app writes results back via a companion HTTP server.

**Step 1: Locate the publish output**
```bash
PUBLISH_DIR="src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/bin/Release/net10.0/publish/wwwroot"
```

**Step 2: Start the HTTP file server and the file-creation companion server**
```bash
# Serve the published app on port 8000
python -m http.server 8000 -d "$PUBLISH_DIR" &
HTTP_PID=$!

# Start the companion server that receives file-write POST requests on port 8001
python build/test-scripts/skia-browserwasm-file-creation-server.py 8001 &
COMPANION_PID=$!

sleep 5
```

**Step 3: Construct the URL with test parameters**

The results file path must be an absolute path (the companion server writes to it):
```bash
RESULTS_FILE="$(pwd)/wasm-test-results.xml"
```

URL-encode the parameters and build the URL:
```bash
# Helper: basic URL encoding
rawurlencode() {
    python -c "import urllib.parse; print(urllib.parse.quote('$1', safe=''))"
}

RESULTS_ENCODED=$(rawurlencode "$RESULTS_FILE")
```

Without filter:
```
http://localhost:8000/?--runtime-tests=$RESULTS_ENCODED
```

With filter (note the `=` → `!` replacement):
```bash
FILTER_B64=$(echo -n "fully.qualified.TestName" | base64)
FILTER_B64_ESCAPED=${FILTER_B64//=/!}
FILTER_ENCODED=$(rawurlencode "$FILTER_B64_ESCAPED")
URL="http://localhost:8000/?--runtime-tests=${RESULTS_ENCODED}&--runtime-test-filter=${FILTER_ENCODED}"
```

**Step 4: Open the URL in a browser**

**If the Playwright MCP tool is available** (`mcp__playwright__browser_navigate`), use it — no manual browser needed:
```
mcp__playwright__browser_navigate(url: "<the constructed URL>")
```
Keep the tab open until tests complete. If the results file does not appear within 10 minutes, use `mcp__playwright__browser_take_screenshot` to inspect the state.

**If Playwright is not available**, open the URL with the OS default browser and instruct the user to keep it open:

On Windows (Git Bash):
```bash
start "" "$URL"
```
On Linux:
```bash
google-chrome --no-sandbox "$URL" &
```
On macOS:
```bash
open "$URL"
```

**Step 5: Wait for results**

The test runner writes a canary file (`$RESULTS_FILE.canary`) when it starts and the actual results file when complete. Poll from bash:
```bash
# Wait for the results file (poll every 10 seconds, timeout 10 minutes)
for i in $(seq 1 60); do [ -f "$RESULTS_FILE" ] && break; sleep 10; done
```

**Step 6: Cleanup**
```bash
kill $HTTP_PID $COMPANION_PID 2>/dev/null || true
```

### Phase 4: Parse and Report Results

1. Read the test results XML file (NUnit format)
2. Report summary: total tests, passed, failed, skipped
3. For failures: extract failure messages and stack traces
4. If tests failed, suggest investigation steps:
   - Check if the test is platform-specific
   - Check if the failure is a known issue
   - Suggest using `/bug-investigate` for complex failures

---

## Platform Quick Reference

| | Skia Desktop | Skia WASM |
|-|-------------|-----------|
| **Project** | `SamplesApp.Skia.Generic` | `SamplesApp.Skia.WebAssembly.Browser` |
| **Build command** | `dotnet build ... -c Release -f net10.0` | `dotnet publish ... -c Release -f net10.0` |
| **Run method** | `dotnet SamplesApp.Skia.Generic.dll --runtime-tests=...` | Browser navigates to URL with query params |
| **Filter delivery** | `UITEST_RUNTIME_TESTS_FILTER` env var | `--runtime-test-filter` URL query param |
| **Base64 `=` handling** | Standard base64 | Replace `=` with `!` before URL-encoding |
| **Results delivery** | Written directly to disk | Written via companion HTTP server on port+1 |
| **Dependencies** | None | Python (http.server), browser |
| **Speed** | Fast | Slower (publish + browser startup) |
| **Recommended for** | Default / local dev | WASM-specific bugs, platform divergence |

---

## Technical Reference

### Environment Variable
- **Name**: `UITEST_RUNTIME_TESTS_FILTER`
- **Format**: Base64-encoded, UTF-8 string
- **Separator**: `|` (pipe) between multiple test names
- **Scope**: Must be set in the same shell session that runs the test app (desktop), or passed as URL param (WASM)

### Code Path
`App.Tests.cs:HandleRuntimeTests()` → `SampleChooserViewModel.RunRuntimeTests()` → `Convert.FromBase64String()` → `Split("|")` → `UnitTestEngineConfig.Filters[]`

### Key Files
- Entry point: `src/SamplesApp/SamplesApp.Shared/App.Tests.cs`
- Filter decoding: `src/SamplesApp/SamplesApp.UnitTests.Shared/Controls/UITests/Presentation/SampleChooserViewModel.cs`
- Config: `src/SamplesApp/SamplesApp.UnitTests.Shared/Controls/UnitTest/UnitTestEngineConfig.cs`
- Test location: `src/Uno.UI.RuntimeTests/Tests/`
- WASM CI script: `build/test-scripts/wasm-run-skia-runtime-tests.sh`
- WASM companion server: `build/test-scripts/skia-browserwasm-file-creation-server.py`
- Agent guide: `.github/agents/runtime-tests-agent.md`

### CI-Only Options
- `--runtime-tests-group` / `--runtime-tests-group-count`: For CI test sharding
