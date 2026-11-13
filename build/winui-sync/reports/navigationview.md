<!-- winui-sync-run target=winui3/release/2.6.5 build=failed skipped=0 -->
**Sync to WinUI 2.6.5** (`bac7a9c33f` → `c29edd64ae`)

## Upstream changes
Not determined. The upstream diff between `bac7a9c33ffe3696ec19121b16f166227eae4f1f` and `c29edd64ae8b4412d772c5768d835ebfd19b438e` could not be read (see Build errors).

## Ported
None. No files were changed.

## Not applicable
None. No upstream changes were reviewed.

## Skipped
None. No upstream changes were reviewed.

## Build errors
No port was attempted. Every Bash call failed before running with:

`Sandbox is required but failed to initialize: Sandbox dependencies not available: socat not installed. Restart to retry.`

Without Bash I could not run `git -C .winui-upstream diff ...` (the only way to read the old commit), so there was nothing to port. I also could not run either required `dotnet build` command. I did not retry with the sandbox disabled, because the environment marks it as required. Install `socat` on the runner and re-run the task.
