using System;
using System.Text;
using System.Threading.Tasks;
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

// Cross-checked against argon2-cffi (the official phc-winner-argon2 C
// reference implementation), not this library's own historical output —
// same vector as Argon2ExternalKatTests.ExternallyVerified_Argon2id_DegreeOfParallelism1.
const string expectedHex = "94C86F541ABDB3D9AABFEA59AA03963549483E9C0B1A79336E76B54CEE6C917E";

async Task CheckHashAsync(string label, Func<Task<byte[]>> compute)
{
    try
    {
        var actualHex = Convert.ToHexString(await compute());
        Check($"{label} matches the externally-verified vector ({actualHex})", actualHex == expectedHex);
    }
    catch (Exception ex)
    {
        Check($"{label} completes without throwing (threw {ex.GetType().Name}: {ex.Message})", false);
    }
}

void CheckThrows<TException>(string label, Action action) where TException : Exception
{
    try
    {
        action();
        Check($"{label} throws {typeof(TException).Name}", false);
    }
    catch (TException ex)
    {
        Check($"{label} throws {typeof(TException).Name} ({ex.Message})", true);
    }
    catch (Exception ex)
    {
        Check($"{label} threw the wrong exception type ({ex.GetType().Name}: {ex.Message})", false);
    }
}

Console.WriteLine($"OperatingSystem.IsBrowser() = {OperatingSystem.IsBrowser()}");
Console.WriteLine($"Environment.ProcessorCount = {Environment.ProcessorCount}");

var salt = Encoding.UTF8.GetBytes("0123456789abcdef");

// The case this fork exists for: TopSecret.ProtectedString only ever uses
// DegreeOfParallelism = 1, and only ever calls the synchronous GetBytes.
using (var argon = new Argon2id(Encoding.UTF8.GetBytes("correct horse battery staple"))
{
    Salt = salt,
    Iterations = 3,
    MemorySize = 65536,
    DegreeOfParallelism = 1,
})
{
    await CheckHashAsync("GetBytes(32) at DegreeOfParallelism=1", () => Task.FromResult(argon.GetBytes(32)));
}

// GetBytesAsync at p=1 was already known to complete on WASM before this
// fork's fix (it never blocked); re-confirmed here so a regression in the
// shared Hash()/InitializeLanes() path would be caught by this same run.
using (var argon = new Argon2id(Encoding.UTF8.GetBytes("correct horse battery staple"))
{
    Salt = salt,
    Iterations = 3,
    MemorySize = 65536,
    DegreeOfParallelism = 1,
})
{
    await CheckHashAsync("GetBytesAsync(32) at DegreeOfParallelism=1", () => argon.GetBytesAsync(32));
}

// DegreeOfParallelism > 1 on this single-threaded host must fail fast with a
// clear, actionable exception, not hang or surface the runtime's own opaque
// "Cannot wait on monitors on this runtime" from deep inside Task internals.
using (var argon = new Argon2id(Encoding.UTF8.GetBytes("password"))
{
    Salt = salt,
    Iterations = 1,
    MemorySize = 32,
    DegreeOfParallelism = 2,
})
{
    CheckThrows<PlatformNotSupportedException>("GetBytes(4) at p=2 on this single-threaded host", () => argon.GetBytes(4));
}

Console.WriteLine(ok ? "ALL PASSED" : "SOME FAILED");
return ok ? 0 : 1;
