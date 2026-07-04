# TopSecret.Cryptography

<div align="center">
<a href="https://www.nuget.org/packages/TopSecret.Cryptography.Argon2"><img alt="NuGet: TopSecret.Cryptography.Argon2" src="https://img.shields.io/nuget/v/TopSecret.Cryptography.Argon2.svg?label=TopSecret.Cryptography.Argon2"></a>
<a href="https://www.nuget.org/packages/TopSecret.Cryptography.Blake2"><img alt="NuGet: .Blake2" src="https://img.shields.io/nuget/v/TopSecret.Cryptography.Blake2.svg?label=.Blake2"></a>
<a href="https://github.com/Alpaq92/TopSecret.Cryptography/actions/workflows/ci.yml"><img alt="CI" src="https://img.shields.io/github/actions/workflow/status/Alpaq92/TopSecret.Cryptography/ci.yml?branch=master&label=CI"></a>
<a href="https://github.com/Alpaq92/TopSecret.Cryptography/actions/workflows/release.yml"><img alt="Release" src="https://img.shields.io/github/actions/workflow/status/Alpaq92/TopSecret.Cryptography/release.yml?branch=master&label=Release"></a>
<a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-blue.svg"></a>
</div>

<br>

Argon2 and Blake2 for .NET — a maintained fork of
[Konscious.Security.Cryptography](https://github.com/kmaragon/Konscious.Security.Cryptography).

This is a fork of [kmaragon/Konscious.Security.Cryptography](https://github.com/kmaragon/Konscious.Security.Cryptography),
renamed and re-namespaced to `TopSecret.Cryptography`. The motivation was
singular: **make Argon2 work on WASM** — nothing more, nothing less. That
does *not* mean "expose an async Argon2id API for browser apps to call" —
see [Browser / WebAssembly](#browser--webassembly) below for why that option
was evaluated and rejected as a security downgrade at the consuming layer,
despite the raw async call completing correctly on WASM. Beyond that, this
fork tracks modern .NET target frameworks, carries a strong name, and folds
in a handful of long-standing, low-risk fixes from upstream's open PR/issue
backlog. All credit for the underlying Argon2 and Blake2 implementations
belongs to [Keef Aragon](https://github.com/kmaragon) and Konscious's
contributors — see [LICENSE](LICENSE).

## Packages

| Package | Description |
|---|---|
| `TopSecret.Cryptography.Argon2` | Argon2i / Argon2d / Argon2id, implemented as a `System.Security.Cryptography.DeriveBytes`. |
| `TopSecret.Cryptography.Blake2` | Blake2b per RFC 7693, implemented as a `System.Security.Cryptography.HMAC`. |

Both multi-target `netstandard2.0;net462;net6.0;net8.0;net10.0`.

## What differs from upstream

This isn't a cosmetic rename. The following changes were made against
upstream `master`, chosen from a full triage of its open pull requests and
issues (kept — deliberately — to changes that are low-risk and
non-controversial for a small crypto library):

- **Namespaces and package IDs** — `Konscious.Security.Cryptography` →
  `TopSecret.Cryptography`, `Konscious.Security.Cryptography.Argon2` →
  `TopSecret.Cryptography.Argon2`, etc. Public API shape (types, members,
  parameters) is otherwise unchanged from upstream.
- **`net10.0` target added**, `netstandard1.3`/`net4.6` replaced with
  `netstandard2.0`/`net462`, and the test/benchmark projects' EOL `net5`/`net6.0`
  targets replaced with `net8.0`/`net10.0`. The old `netstandard1.3` target
  pulled in a transitively vulnerable `System.Net.Http`/`System.Text.RegularExpressions`
  (advisory [GHSA-7jgj-8wvc-jh57](https://github.com/advisories/GHSA-7jgj-8wvc-jh57));
  the modern targets don't need those packages at all. `net462` (not `net46`)
  because the current `System.Memory`/`System.Numerics.Vectors` releases
  dropped support for plain `net46` — and `ArgonBenchmarks`/`ComparisonHarness`
  already targeted `net462`, so this also fixes a pre-existing inconsistency
  between the libraries and their own tooling. (Partial credit:
  [upstream PR #66](https://github.com/kmaragon/Konscious.Security.Cryptography/pull/66)
  proposed the `net10.0` addition; only its `TargetFrameworks` edits were
  ported, not its CI/branding changes.)
- **`GetBytes(int)` no longer round-trips through the thread pool.** It now
  calls `GetAwaiter().GetResult()` on the same async implementation
  `GetBytesAsync` uses, instead of `Task.Run(async () => ...).Result`. This
  removes an unnecessary thread-pool hop and surfaces the original exception
  instead of an `AggregateException` wrapper. It does **not** fix the
  underlying single-threaded-runtime limitation — see
  [Browser / WebAssembly](#browser--webassembly) below. (Ports
  [upstream PR #47](https://github.com/kmaragon/Konscious.Security.Cryptography/pull/47),
  closed upstream without being merged; addresses
  [issue #46](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/46)
  and the historical [issue #22](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/22).)
- **`LittleEndianActiveStream.ClearBuffer()` null-checks its buffer** before
  clearing it, instead of assuming a prior `Expose(...)` call always
  allocated one. Not reachable through today's call sites, but a real latent
  `NullReferenceException` for any other caller. (From
  [upstream PR #67](https://github.com/kmaragon/Konscious.Security.Cryptography/pull/67).)
- **Both assemblies are strong-named**, resolving one of the most
  frequently-repeated asks in upstream's issue tracker
  ([#38](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/38),
  [#55](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/55),
  [#57](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/57)) —
  never implemented upstream because it changes assembly identity for
  existing unsigned consumers, but free to adopt in a fresh fork with none.
  Signed via **`PublicSign`**, not a committed private key: `TopSecret.Cryptography.publickey`
  holds only the public half, so there is no private key file anywhere to
  ever leak — the same approach the .NET runtime/SDK's own repos use for
  public/OSS-signed builds. This gives every assembly a stable, real
  `PublicKeyToken` (the thing .NET Framework consumers actually bind on),
  without a cryptographic signature backing it — which matches how strong
  naming already works everywhere on .NET Core/5+: the CLR doesn't verify
  strong-name signatures at load time there, only the identity is used.
  `AssemblyVersion` is pinned at `1.0.0.0` in `Directory.Build.props`,
  independent of the floating package version, so a routine patch release
  can't silently break a strong-named .NET Framework consumer's binding.
- **Every dependency audited and bumped to its latest stable release**
  (checked directly against NuGet.org, not assumed): `Microsoft.NET.Test.Sdk`
  17.11.1 → 18.7.0, `xunit` 2.9.2 → 2.9.3, `System.Memory` 4.5.4 → 4.6.3,
  `System.Numerics.Vectors` 4.5.0 → 4.6.1, `BenchmarkDotNet` /
  `BenchmarkDotNet.Diagnostics.Windows` 0.14.0 → 0.15.8 (0.15.x is the first
  line with a `Net10_0` runtime moniker — confirmed by inspecting the shipped
  assembly — so `ArgonBenchmarks` now actually benchmarks `net10.0` instead
  of just compiling it). `xunit.runner.visualstudio` and `Blake2Core` were
  already at their latest stable versions. This also dropped the 2018-era
  `Microsoft.NET.Test.Sdk`/`xunit` versions that pulled in a vulnerable
  transitive `Newtonsoft.Json` 9.0.1 (advisory
  [GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr)).
  Every remaining dependency (`System.Memory`, `System.Numerics.Vectors`,
  `Blake2Core`) is genuinely required — none are transitively provided —
  and each is scoped with an `ItemGroup` `Condition` to only the TFMs that
  actually lack the type in-box (`net462`/`netstandard2.0`; `net6.0`+ needs
  none of them).

### Not everything was ported — here's the full accounting

Of upstream's 3 open PRs, 2 revivable closed PRs, and roughly 19 open/relevant
issues, only the four items above were actually adopted. Everything else:

- **Already merged into upstream `master`** (nothing to port): PRs #50, #51,
  #58, #60.
- **Deliberately deferred as too risky to port as-is**:
  - **Nullable reference type annotations** ([PR #67](https://github.com/kmaragon/Konscious.Security.Cryptography/pull/67))
    — genuinely useful, but turning on `Nullable` across a codebase not
    written with it in mind, under `TreatWarningsAsErrors`, is a much larger
    and riskier change than the diff suggests.
  - **SIMD/AVX2 intrinsics for Blake2b** ([PR #52](https://github.com/kmaragon/Konscious.Security.Cryptography/pull/52),
    40-55% faster in the author's benchmarks) — the author's own words:
    *"done at 2 AM... no idea if it's safe."* Zero code review, three-plus
    years stale, touches the core compression function.
  - **`SingleThreaded` opt-in hashing mode** ([PR #53](https://github.com/kmaragon/Konscious.Security.Cryptography/pull/53))
    — a reasonable idea (relevant to WASM/Blazor per
    [issue #59](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/59)),
    but the diff bundles it with an inferior `GetBytes` fix and was never
    reviewed.
  - **Removing remaining `unsafe` blocks** ([issue #49](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/49))
    — directionally good, no diff exists to port, needs benchmarking.
- **Duplicates, already-explained, or out of scope, no action taken**:
  strong-naming duplicates (#55, #57 — folded into #38 above), "working as
  intended" memory/GC behavior ([#64](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/64),
  [#56](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/56)),
  stale/EOL reports ([#41](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/41),
  [#44](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/44)),
  a support question ([#65](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/65)),
  and large API redesigns out of scope for a fork this size (`ref struct Argon2`
  per [#54](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/54),
  Blake3 support per [#39](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/39)).

## Browser / WebAssembly

The **synchronous** `Argon2.GetBytes(int)` **cannot complete on a
single-threaded runtime** (browser WASM being the practical case) — sync or
async internally, it still blocks the calling thread waiting on work that
needs that same thread to run. This is inherited from Konscious's internal
implementation ([issue #22](https://github.com/kmaragon/Konscious.Security.Cryptography/issues/22)),
not something this fork introduces or can safely paper over. Verified
empirically in a Blazor WebAssembly app (`net10.0-browser`,
`Environment.ProcessorCount == 1`):

- `GetBytesAsyncImpl` schedules every lane's work via `Task.Run(...)` /
  `Task.WhenAll(...)`, regardless of `DegreeOfParallelism`.
- A single-threaded runtime has no second thread for that scheduled work to
  run on, so anything that blocks waiting for it fails. In practice this
  isn't a silent hang: `GetBytes`'s `GetAwaiter().GetResult()` throws
  `PlatformNotSupportedException: Cannot wait on monitors on this runtime`
  immediately — Mono's WASM runtime refuses the blocking wait outright
  rather than deadlocking the page.
- `GetBytesAsync`, by contrast, **does** run to completion on a
  single-threaded runtime — confirmed at both `DegreeOfParallelism = 1` and
  `= 2` — because awaiting it (all the way up the call stack, with no
  synchronous blocking anywhere) lets the runtime's single thread return to
  the event loop and actually execute the queued work. Output matched the
  identical call on desktop byte-for-byte.

That's where this fork's job ends: confirming the underlying async call
genuinely completes correctly on WASM. **It is not a general endorsement of
"expose an async Argon2id API to browser callers."** Whether that's actually
safe depends entirely on what wraps the call, and for a security-critical
credential type it usually isn't. A bare, stateless call —
`new Argon2id(pw){...}; var h = await argon.GetBytesAsync(n);` — has nothing
to regress. But a type that holds a lock across the whole hash operation (to
protect pinned/locked plaintext scratch buffers, to guarantee `Dispose`
returned ⇒ no library-held plaintext remains, or to serialize verify/mutate
so a rotation can't race a stale check) cannot naively await mid-hash without
releasing that lock — and releasing it changes the type's actual security
properties, not just its threading model.

This is exactly the evaluation [TopSecret.ProtectedString](https://github.com/Alpaq92/TopSecret.ProtectedString)
did for its own `ComputeArgon2idHash`, and why it does **not** expose an async
credential-hashing API on browser despite this fork proving the raw call
works there. Quoting its README's `browser-wasm` section directly, because
it's the authoritative account and shouldn't be paraphrased into something
weaker:

> Argon2id is not supported in the browser — deliberately. Konscious (the
> managed Argon2 underneath) has no truly synchronous path: its sync
> `GetBytes` is `Task.Run(...).Result`, which cannot complete on the
> single-threaded WASM runtime — `.Result` blocks the only thread, and the
> queued lane work needs that very thread (Konscious #22). The library fails
> fast and honestly: `ComputeArgon2idHash` / `VerifyArgon2idHash` throw
> `PlatformNotSupportedException` on `net10.0-browser`, and the live demo's
> Argon2id step reports itself unsupported there. An awaited async wrapper
> (which *would* complete on one thread) was prototyped and adversarially
> reviewed, then **rejected**: releasing the instance lock across the KDF
> `await` regresses the sync path's security invariants — overlapped hashes
> multiply long-lived pinned plaintext scratch copies (page-granular
> `mlock`/`VirtualLock` is not refcounted and the `RLIMIT_MEMLOCK` budget is
> small), `Dispose` stops being a barrier proving no library-held plaintext
> remains, and verify/mutate interleavings can accept a stale credential —
> and the gate-and-guard machinery needed to restore them adds more
> concurrency surface to a security-critical type than one platform's KDF is
> worth. Browser guidance: verify credentials server-side (where they should
> be verified anyway); the alternatives were also evaluated and rejected —
> Isopoh (CC0 license), Soenneker (wraps the same Konscious), NSec (native
> libsodium, no browser-wasm RID). Revisit if .NET's multithreaded WASM lands
> or a permissively-licensed, browser-safe managed Argon2 appears.

If you're building something other than a security-critical, lock-holding
wrapper — a one-shot hash with no shared mutable state to protect —
`await GetBytesAsync(...)` on WASM is exactly as safe as it is anywhere else,
and this fork's verification stands. Don't call `GetBytes(...)` or block on
`GetBytesAsync(...).Result`/`.GetAwaiter().GetResult()` there regardless; that
part hasn't changed. The judgment call is entirely about what you build on
top of it.

**Blake2 doesn't have this problem.** `HMACBlake2B` contains no `Task`,
`Task.Run`, or any async/threading code at all — confirmed by inspecting the
whole `TopSecret.Cryptography.Blake2` source (`grep`-verified, zero matches).
Unlike Argon2's lane-parallel KDF, Blake2b's compression is inherently
sequential, so `ComputeHash`/`Initialize` run exactly the same way on a
single-threaded WASM runtime as anywhere else — no fix was needed.

## Usage

### Argon2

There are Argon2i, Argon2d, and Argon2id implementations, all standard
`System.Security.Cryptography.DeriveBytes` types with the same constructor
and property shape as `Rfc2898DeriveBytes` (PBKDF2), but memory-hard. One
difference from `Rfc2898DeriveBytes` worth knowing: `Reset()` is a no-op and
every `GetBytes`/`GetBytesAsync` call re-derives from scratch — consecutive
calls return identical bytes rather than continuing a stream, so don't use
them the way you'd pull a key and then an IV out of PBKDF2.

```csharp
using TopSecret.Cryptography;

byte[] password = ...;
using var argon2 = new Argon2id(password)
{
    DegreeOfParallelism = 8,   // lanes; tune to your hardware
    MemorySize = 65536,        // KiB (OWASP recommends >= 19 456 for interactive logins)
    Iterations = 3,            // OWASP recommends >= 3 for interactive logins
    Salt = salt,               // unique per secret, >= 8 bytes
    AssociatedData = userId,   // optional
    KnownSecret = pepper,      // optional
};

byte[] syncHash = argon2.GetBytes(32);              // sync — see Browser/WebAssembly above
byte[] asyncHash = await argon2.GetBytesAsync(32);  // async — safe everywhere, including WASM
```

| Property | Type | Required? | Description |
|---|---|---|---|
| `DegreeOfParallelism` | `int` | required | Number of lanes to segment memory into; affects the hash and can be tuned for the target hardware. |
| `MemorySize` | `int` | required | Memory cost in KiB. The main lever for Argon2's memory-hardness. |
| `Iterations` | `int` | required | Time cost. Argon2 needs far fewer iterations than PBKDF2 for equivalent security. |
| `Salt` | `byte[]` | recommended | Standard per-secret salt. |
| `AssociatedData` | `byte[]` | optional | Extra data folded into the hash (not secret, but binds the hash to a context). |
| `KnownSecret` | `byte[]` | optional | An additional secret ("pepper") folded into the hash. |

`GetBytes`/`GetBytesAsync` accept up to 1024 bytes of output.

Argon2d is faster but timing-attack-observable; Argon2i is timing-attack
resistant but slower; Argon2id (recommended by OWASP, and the default choice
in most consumers) hybridizes the two.

### Blake2

Blake2b implements `System.Security.Cryptography.HMAC`, so it's a drop-in
anywhere that accepts `HashAlgorithm`/`HMAC`.

```csharp
using TopSecret.Cryptography;

var hmac = new HMACBlake2B(512);        // unkeyed, 512-bit output
hmac.Initialize();
byte[] digest = hmac.ComputeHash(data);

byte[] key = ...;
var keyed = new HMACBlake2B(key, 512);  // keyed, 512-bit output
```

Hash size can be any 8-bit-aligned value from 8 to 512 bits; keys can be 0
to 64 bytes.

## Building

```
dotnet build TopSecret.Cryptography.sln
dotnet test TopSecret.Cryptography.Argon2.Test
dotnet test TopSecret.Cryptography.Blake2.Test
```

## License

MIT — see [LICENSE](LICENSE). Original work Copyright (c) 2017 Keef Aragon;
fork additions Copyright (c) 2026 Alpaq92.
