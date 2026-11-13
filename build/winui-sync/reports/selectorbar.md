<!-- winui-sync-run target=winui3/release/2.6.5 build=failed skipped=0 -->
**Sync to WinUI 2.6.5** (`b91b3ce6f2` → `c29edd64ae`)

## Upstream changes
Not determined. The upstream diff between `b91b3ce6f25c587a9e18c4e122f348f51331f18b` and `c29edd64ae8b4412d772c5768d835ebfd19b438e` could not be read, so no upstream change was identified and nothing was ported.

## Ported
None. No Uno files were modified.

## Not applicable
None

## Skipped
None

## Build errors
This run was blocked before any porting started. No build was run.

- The Bash tool failed on every call with `Sandbox is required but failed to initialize: Sandbox dependencies not available: socat not installed.` Retrying with the sandbox disabled gave the same error. The environment requires the sandbox, so I did not try to work around it.
- Without Bash I could not run `git -C .winui-upstream diff ...` or `git show`.
- `.winui-upstream` has only a `.git` directory (packed objects) and no checked-out files, so the Read, Glob and Grep tools cannot reach either upstream commit.
- Both required build commands also need Bash.

To retry, run this in an environment where `socat` is installed (the bubblewrap sandbox dependency), or where Bash is allowed to run without the sandbox.
