# Provisioning the benchmark data repo

A one-time setup performed by an `unoplatform` org admin. After provisioning the
nightly + per-merge benchmark stages start landing data on the dashboard.

The PR that ships the benchmark suite can merge **before** provisioning is done.
The publisher tool detects the missing PAT and exits 0 with a warning, so master
stays green; the first nightly run after provisioning starts publishing data.

## Steps

### 1. Create the GitHub repo

Create `unoplatform/uno.benchmarks` (note the dot, matching the local folder name
this PR's author scaffolded into). Public, no template, no initial commit.

### 2. Push the dashboard scaffolding

The PR author scaffolded `D:\Work\uno.benchmarks\` (or the equivalent on
whichever machine the work was done). It contains:

```
README.md
Schema.md
.gitignore
index.html
js/{main,data,dashboard,compare,regression-badge}.js
vendor/README.md
```

**Vendor uPlot** before first deploy: download `uPlot.min.js` and `uPlot.min.css`
from <https://github.com/leeoniya/uPlot/releases> into `vendor/`. The dashboard
already references them at `vendor/uPlot.min.{js,css}` — no code change needed.

Push `main`:

```bash
cd D:/Work/uno.benchmarks
git init
git add .
git commit -m "feat: Initialize uno.benchmarks dashboard"
git branch -M main
git remote add origin https://github.com/unoplatform/uno.benchmarks.git
git push -u origin main
```

### 3. Create the orphan `data` branch

```bash
git checkout --orphan data
git rm -rf .
mkdir -p data/by-platform data/by-benchmark
echo '{"schemaVersion":1,"updatedAt":null,"entries":[]}' > data/index.json
git add data
git commit -m "chore: Initialize benchmark data branch"
git push origin data
```

### 4. Generate the publish PAT

In GitHub → your account → Settings → Developer settings → Personal access tokens
→ Fine-grained tokens → Generate new token:

- Resource owner: `unoplatform`
- Repository access: **Only select repositories** → `unoplatform/uno.benchmarks`
- Permissions: **Contents → Read and write**. Nothing else.
- Expiration: pick something you'll remember to renew (90d–1y).

Copy the token.

### 5. Add the PAT to Azure DevOps

In the Uno Azure DevOps project → Pipelines → Library → Variable groups → New
variable group:

- Name: `benchmark-publisher`
- Variable: `BENCHMARK_REPO_TOKEN` = (paste the PAT). Mark as secret.
- Pipeline permissions: grant access to the main Uno pipeline.

### 6. Enable GitHub Pages

In `unoplatform/uno.benchmarks` → Settings → Pages:

- Source: Deploy from a branch
- Branch: `main`, folder `/ (root)`
- Save.

The dashboard becomes available at `https://unoplatform.github.io/uno.benchmarks/`
within a minute.

### 7. Configure the nightly schedule (Azure DevOps)

In the Uno Azure DevOps pipeline → Triggers → Scheduled triggers → New:

- Schedule: `0 3 * * *` (03:00 UTC daily)
- Branch: `master`
- Always run: yes

The benchmark stages are gated on
`or(eq(Build.SourceBranch, 'refs/heads/master'), eq(Build.Reason, 'Schedule'))`
so the nightly trigger picks them up automatically.

## Verification

After the next master merge or scheduled run:

1. The pipeline's `Benchmarks - …` stages should run for each live target.
2. The downstream `Benchmarks - Publish trends` stage runs once at the end.
3. A new commit appears on the `data` branch of `unoplatform/uno.benchmarks`.
4. Refreshing <https://unoplatform.github.io/uno.benchmarks/> shows the new
   benchmarks listed on the index.

If the publish job logs `BENCHMARK_REPO_TOKEN not set; skipping push` even
after provisioning, the variable group isn't authorized for the pipeline. Go
back to step 5.

## Bringing up a new target

Four targets are gated off in v1: Skia Wasm, Skia iOS, Skia Android (CoreCLR),
Skia Android (NativeAOT). Each follows the same recipe:

1. **Verify locally** that BDN 0.14 with `InProcessNoEmitToolchain` actually
   loads and produces results on that target. Build a SamplesApp variant for
   the target, run with `UITEST_RUNTIME_BENCHMARKS_ONLY=true`, confirm results
   land in `%TEMP%/uno-bench-…/results.jsonl`.
2. **Diagnose any platform-specific issues** —
   - iOS / Android NativeAOT: linker/trim preserves needed for BDN type
     discovery? Add a `LinkerDefinition.Skia.xml` entry or
     `[DynamicallyAccessedMembers(All)]` on `BenchmarkRunnerHost`.
   - Wasm: are `RuntimeInformation.OSDescription` and
     `RuntimeInformation.ProcessArchitecture` returning sane values? If not,
     the publisher tool will write `null`/blank `host` fields and the dashboard
     will tolerate that, but log it.
   - Android CoreCLR: is `Reflection.Emit` actually working? If not,
     `InProcessNoEmitToolchain` should still work — verify.
3. **Edit the gated yaml** at
   `build/ci/tests/.azure-devops-benchmarks-<target>-skia.yml`: remove the
   `condition: false` placeholder and replace the body with the same shape as
   the live targets. Use the existing live yaml as a template.
4. **Add the target to the publish job's downloads** in
   `build/ci/tests/.azure-devops-benchmarks-publish.yml` (one new
   `DownloadPipelineArtifact@2` block).
5. **Open a PR** scoped to that one target. Reviewers should see ~50 LOC of
   yaml change plus any preserve-attribute fixes. Title format:
   `feat(benchmarks): Enable <target> benchmarks`.

The platform enum slugs (`skia-wasm`, `skia-ios`, `skia-android-coreclr`,
`skia-android-nativeaot`) are already wired into `Schema.md`'s platform list and
the dashboard auto-discovers them from `index.json` — no dashboard-side change
needed.

## Token rotation

Fine-grained PATs expire. Set a calendar reminder ~2 weeks before the token's
expiry. Rotation procedure:

1. Generate a new fine-grained PAT with the same scope (step 4).
2. Update `BENCHMARK_REPO_TOKEN` in the Azure DevOps variable group.
3. Revoke the old PAT.

If a rotation is missed, the publish stage starts logging push failures but
master remains green (graceful skip). The data gap is fillable by re-running the
master pipeline manually after rotation — the JSONL is append-only so a
re-run produces fresh rows for the same commit, which the dashboard aggregates
into one chart point.
