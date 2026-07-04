using System;
using System.Text;
using TopSecret.Cryptography;

// Not a unit test: xunit doesn't run on browser-wasm. This is a standalone
// executable, compiled to browser-wasm and actually run under a JS host
// (Node's V8 in CI; any browser works too), that proves — rather than just
// compiles — the claim this fork exists to make true: DegreeOfParallelism =
// 1 Argon2id hashing completes on a genuinely single-threaded runtime.
// Source-level inspection alone was judged insufficient to trust this
// (see gaps.md); this is the actual run that inspection can't substitute for.

bool ok = true;

void Check(string name, bool condition)
{
    Console.WriteLine((condition ? "PASS: " : "FAIL: ") + name);
    ok &= condition;
}

Console.WriteLine($"OperatingSystem.IsBrowser() = {OperatingSystem.IsBrowser()}");
Console.WriteLine($"Environment.ProcessorCount = {Environment.ProcessorCount}");

var salt = Encoding.UTF8.GetBytes("0123456789abcdef");
// Cross-checked against argon2-cffi (the official phc-winner-argon2 C
// reference implementation), not this library's own historical output —
// same vector as Argon2ExternalKatTests.ExternallyVerified_Argon2id_DegreeOfParallelism1.
const string expectedHex = "94C86F541ABDB3D9AABFEA59AA03963549483E9C0B1A79336E76B54CEE6C917E";

// The case this fork exists for: TopSecret.ProtectedString only ever uses
// DegreeOfParallelism = 1, and only ever calls the synchronous GetBytes.
try
{
    using var argon = new Argon2id(Encoding.UTF8.GetBytes("correct horse battery staple"))
    {
        Salt = salt,
        Iterations = 3,
        MemorySize = 65536,
        DegreeOfParallelism = 1,
    };

    var actualHex = Convert.ToHexString(argon.GetBytes(32));
    Check("GetBytes(32) at DegreeOfParallelism=1 completes without throwing", true);
    Check($"GetBytes(32) at p=1 matches the externally-verified vector ({actualHex})", actualHex == expectedHex);
}
catch (Exception ex)
{
    Check($"GetBytes(32) at p=1 completes without throwing (threw {ex.GetType().Name}: {ex.Message})", false);
}

// GetBytesAsync at p=1 was already known to complete on WASM before this
// fork's fix (it never blocked); re-confirmed here so a regression in the
// shared Hash()/InitializeLanes() path would be caught by this same run.
try
{
    using var argon = new Argon2id(Encoding.UTF8.GetBytes("correct horse battery staple"))
    {
        Salt = salt,
        Iterations = 3,
        MemorySize = 65536,
        DegreeOfParallelism = 1,
    };

    var actualHex = Convert.ToHexString(await argon.GetBytesAsync(32));
    Check($"GetBytesAsync(32) at p=1 matches the externally-verified vector ({actualHex})", actualHex == expectedHex);
}
catch (Exception ex)
{
    Check($"GetBytesAsync(32) at p=1 completes without throwing (threw {ex.GetType().Name}: {ex.Message})", false);
}

// DegreeOfParallelism > 1 on this single-threaded host must fail fast with a
// clear, actionable exception, not hang or surface the runtime's own opaque
// "Cannot wait on monitors on this runtime" from deep inside Task internals.
try
{
    using var argon = new Argon2id(Encoding.UTF8.GetBytes("password"))
    {
        Salt = salt,
        Iterations = 1,
        MemorySize = 32,
        DegreeOfParallelism = 2,
    };

    argon.GetBytes(4);
    Check("GetBytes(4) at p=2 throws PlatformNotSupportedException on this single-threaded host", false);
}
catch (PlatformNotSupportedException ex)
{
    Check($"GetBytes(4) at p=2 throws PlatformNotSupportedException on this single-threaded host ({ex.Message})", true);
}
catch (Exception ex)
{
    Check($"GetBytes(4) at p=2 threw the wrong exception type ({ex.GetType().Name}: {ex.Message})", false);
}

Console.WriteLine(ok ? "ALL PASSED" : "SOME FAILED");
return ok ? 0 : 1;
