# Contributing to TopSecret.Cryptography

Thanks for your interest! This document covers what you need to build, test, and land a change, plus how this repository's review/merge/release automation works. For *why* this fork exists and what it changed, included, or deliberately skipped relative to upstream Konscious.Security.Cryptography, read the README's [What differs from upstream](README.md#what-differs-from-upstream) and [Not everything was ported](README.md#not-everything-was-ported--heres-the-full-accounting) sections first — this document won't repeat that reasoning, only reference it.

## Prerequisites

- **.NET SDK 8.0 or newer**, with the 10.0 SDK available for the `net10.0` target (`global.json` doesn't pin a floor beyond that — this repo has no analyzer-package version dependency forcing a higher one).
- `net462` (a plain .NET Framework target, not `net46` — see the README for why) only builds on Windows. Linux/macOS contributors: build individual projects excluding that TFM (`dotnet build TopSecret.Cryptography.Argon2 -f net8.0`), or rely on CI's Windows leg to catch `net462`-specific issues.
- No mobile/browser workloads are needed to build or test any of the **published packages** — unlike some sibling `TopSecret.*` packages, none of `TopSecret.Cryptography.Argon2`/`.Blake2`/`.ArgonBenchmarks`/`.ComparisonHarness` have `net10.0-browser`/`-android`/`-ios` targets; they're plain managed IL. The one exception is `TopSecret.Cryptography.Argon2.WasmSmokeTest` (see below, and not itself published to NuGet) — it targets `browser-wasm` and needs `dotnet workload install wasm-tools` plus Node.js to build and run; you only need that if you're touching `Argon2Core.cs`'s lane-dispatch code.

## Build, test, run

```bash
dotnet build TopSecret.Cryptography.sln
dotnet test  TopSecret.Cryptography.Argon2.Test/TopSecret.Cryptography.Argon2.Test.csproj
dotnet test  TopSecret.Cryptography.Blake2.Test/TopSecret.Cryptography.Blake2.Test.csproj
dotnet run --project TopSecret.Cryptography.ArgonBenchmarks     # BenchmarkDotNet perf suite
dotnet run --project TopSecret.Cryptography.ComparisonHarness   # cross-validates against a Python reference implementation

# WASM smoke test — not part of the .sln above (needs wasm-tools, which the
# main build doesn't install); only relevant if you're touching lane dispatch:
dotnet workload install wasm-tools
dotnet build TopSecret.Cryptography.Argon2.WasmSmokeTest/TopSecret.Cryptography.Argon2.WasmSmokeTest.csproj -c Release
node TopSecret.Cryptography.Argon2.WasmSmokeTest/bin/Release/net10.0/browser-wasm/AppBundle/main.mjs
```

## Project map

| Project | What it is |
| --- | --- |
| `TopSecret.Cryptography.Argon2` | Argon2i/Argon2d/Argon2id (`System.Security.Cryptography.DeriveBytes`). Self-contained — compiles its own private, `internal`-only Blake2b-512 HMAC rather than depending on the `Blake2` package. Most of that code is MSBuild-linked source shared with `TopSecret.Cryptography.Blake2` (one copy on disk, not a duplicate) — see `TopSecret.Cryptography.Argon2.csproj`'s linked `Compile` items. |
| `TopSecret.Cryptography.Blake2` | Blake2b, RFC 7693 (`System.Security.Cryptography.HMAC`), published standalone for consumers who want Blake2b directly. |
| `TopSecret.Cryptography.Argon2.WasmSmokeTest` | Not a unit test (xunit doesn't run on `browser-wasm`) — a standalone `browser-wasm` console app, run under Node's V8 in CI, that actually executes `GetBytes`/`GetBytesAsync` on a genuinely single-threaded host and checks the output against an externally-verified vector. Excluded from `TopSecret.Cryptography.sln` because it needs the `wasm-tools` workload the main build doesn't install; see its own CI job in `ci.yml`. |
| `*.Test` | xunit suites, one per package. |
| `TopSecret.Cryptography.ArgonBenchmarks` | BenchmarkDotNet suite (`net462`/`net8.0`/`net10.0` jobs). Published to NuGet.org (same icon/readme treatment as the two library packages) so the benchmark results behind this repo's performance claims are independently reproducible — not meant to be referenced as a dependency from your own code. |
| `TopSecret.Cryptography.ComparisonHarness` / `TopSecret.Cryptography.PythonHarness` | Cross-language parity fuzzing against a Python Argon2 implementation. Same NuGet publishing rationale as `TopSecret.Cryptography.ArgonBenchmarks` above — reproducibility, not a dependency. `TopSecret.Cryptography.PythonHarness` itself is a plain Python script, not a .NET project, so it isn't published. |

## Rules that will fail your build or review

1. **Warnings are errors** (`TreatWarningsAsErrors`) on both library projects.
2. **Byte-for-byte output stability.** Argon2/Blake2 are spec implementations — any change that alters `GetBytes`/`GetBytesAsync`/`ComputeHash` output for the same inputs is a correctness regression, not a style choice, regardless of how it's phrased in the PR description. `Argon2ExternalKatTests.cs` checks this against RFC 9106's official vector and independent `argon2-cffi` cross-checks, not just this repo's own historical output — a change that passes the self-referential tests but fails those is still a regression.
3. **`GetBytes(int)` stays a thin bridge to `GetBytesAsync`** (`GetBytesAsync(bc).GetAwaiter().GetResult()`). Don't reintroduce a `Task.Run(...).Result` round-trip — that was a bug this fork already fixed once (see README).
4. **`Hash()`/`InitializeLanes()`'s lane 0 always runs inline on the calling thread; never wrap it in `Task.Run` too.** Only lanes `1..N-1` go through `Task.Run`. Reintroducing `Task.Run` for lane 0 — even "just to be consistent with the other lanes" — silently reopens the exact single-threaded-runtime (WASM) deadlock this fork's `Task.Run` elimination fixed, and the WASM smoke test above is what would catch it (the existing desktop-only unit tests can't, since they all have a real thread pool available).
5. **Blake2 stays synchronous, all the way down.** No `Task`, `Task.Run`, or `async` anywhere in `TopSecret.Cryptography.Blake2` (or in the Blake2b compression code Argon2 links into its own assembly) — that's *why* Blake2b's compression has no single-threaded-runtime issue to begin with. A change that introduces threading there reintroduces that bug class.
6. **Don't copy-paste `Blake2Constants.cs`/`Blake2bBase.cs`/`Blake2bNormal.cs`/`Blake2bSimd.cs` if you need to touch them from the Argon2 side.** They're linked into `TopSecret.Cryptography.Argon2.csproj` via `<Compile Include=".." Link=".." />` — edit the file under `TopSecret.Cryptography.Blake2/`, and both projects pick it up. Adding a second physical copy reintroduces the exact drift risk the linking was meant to close.
7. **TFM `Condition` strings must match an actual `TargetFrameworks` entry in the same file.** This repo has shipped that exact bug twice already (`net46` vs `net462`, `net4.6` vs `net46`) — CodeRabbit is configured to check this, but it's still a review-blocking finding if it slips through.
8. **Strong-name identity**: this repo signs via `PublicSign` (`Directory.Build.props`) — there is no private key anywhere, ever. Don't add an `AssemblyOriginatorKeyFile` pointing at a private `.snk`, and don't remove `PublicSign` without discussing it first (that would require generating and committing a real private key, which is exactly what this fork moved away from).

## Review and merge policy

- **Every PR is reviewed automatically by [CodeRabbit](https://coderabbitai.com/)** (`.coderabbit.yaml`) as soon as it's opened. CodeRabbit comments and suggests but never blocks with a `CHANGES_REQUESTED` review — an approval from CodeRabbit satisfies the required-approval gate the same as a human's.
- **Human PRs auto-merge one week after their latest qualifying approval** (`auto-merge-approved.yml`), provided the approval still stands (no later `CHANGES_REQUESTED`) and required checks are green — a soak window for second thoughts and late review comments. The clock starts at *approval time*, not PR-creation time. A daily scheduled job checks and merges what has soaked.
  - Opt out entirely by labeling the PR `no-auto-merge`.
  - GitHub doesn't natively support "merge N days after approval" — branch protection still gates the actual merge attempt, so a regressed approval or a newly-red check simply makes that day's merge attempt fail harmlessly; the workflow retries daily.
- **Dependabot PRs (NuGet and GitHub Actions) auto-merge immediately** once required checks pass — no soak period (`dependabot-auto-merge.yml`). They're low-risk, narrow-scope, and reviewed by CI the same as anything else.
- **Admins and maintainers can always merge manually**, bypassing the soak period and even a pending approval, via the ruleset's bypass list. Use this for hotfixes or anything time-sensitive — the automation is a default, not a lock.

## CI and releases

- **`ci.yml`** — every push/PR: build + test on `windows-latest` (covers `net462`) and `ubuntu-latest` (covers the rest, via the `CI_TFMS` MSBuild property — see the comment in the csproj files).
- **`release.yml`** — on every push to `master`, [release-please](https://github.com/googleapis/release-please) reads Conventional Commit messages and maintains a Release PR that bumps `.release-please-manifest.json` and `CHANGELOG.md`. That PR auto-merges once checks pass; merging it tags a release and packs + publishes both NuGet packages together, at the same version.
- **Monthly dependency refresh, without a bespoke job**: `dependabot.yml` runs monthly against both the `nuget` and `github-actions` ecosystems. Its PRs use a `feat(deps): …`/`fix(deps): …` commit prefix, auto-merge immediately per the policy above, and — because `feat`/`fix` are release-worthy Conventional Commit types — the merge to `master` triggers `release.yml` and a new version ships automatically. Dependabot *is* the monthly-upgrade-and-deploy job; there's no separate one to maintain.

## Commit messages (they drive releases)

Use **[Conventional Commits](https://www.conventionalcommits.org/)**:

- `feat: …` → minor bump, **Features**
- `fix: …` → patch bump, **Bug Fixes**
- `deps: …` → **Dependencies** (Dependabot uses `feat(deps)`/`fix(deps)` automatically)
- `docs:` / `refactor:` / `perf:` → respective sections, patch-level
- `feat!: …` or a `BREAKING CHANGE:` footer → major bump
- `test:` / `chore:` → no release impact

Assume your PR title becomes the squash commit subject — a non-Conventional subject won't break anything, but won't appear in the changelog or trigger a release either.

## Changelog

`CHANGELOG.md` is generated by release-please — don't hand-edit released sections; write good Conventional Commit messages instead, and correct a wrong entry with a follow-up commit.

## Reporting security issues

Please don't open public issues for suspected cryptographic weaknesses or output-correctness bugs that could affect already-derived hashes in the wild — use GitHub's private vulnerability reporting on this repository instead.
