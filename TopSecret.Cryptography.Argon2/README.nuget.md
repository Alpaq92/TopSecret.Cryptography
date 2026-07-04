# TopSecret.Cryptography.Argon2

Argon2i, Argon2d, and Argon2id (the Password Hashing Competition winner) for
.NET, implemented as a `System.Security.Cryptography.DeriveBytes`. A
maintained fork of
[Konscious.Security.Cryptography.Argon2](https://github.com/kmaragon/Konscious.Security.Cryptography).

```csharp
using TopSecret.Cryptography;

byte[] password = ...;
using var argon2 = new Argon2id(password)
{
    DegreeOfParallelism = 8,   // lanes; tune to your hardware
    MemorySize = 65536,        // KiB (OWASP recommends >= 19 456 for interactive logins)
    Iterations = 3,            // OWASP recommends >= 3 for interactive logins
    Salt = salt,               // unique per secret, >= 8 bytes
};

byte[] hash = argon2.GetBytes(32);              // sync
byte[] hash2 = await argon2.GetBytesAsync(32);  // async — safe everywhere, including browser WASM
```

Targets `netstandard2.0;net462;net6.0;net8.0;net10.0`. `DegreeOfParallelism = 1`
also completes on single-threaded runtimes such as browser WASM — see the
[full README](https://github.com/Alpaq92/TopSecret.Cryptography#readme) for
the Browser/WebAssembly details, breaking changes between versions, and
everything else that differs from the upstream Konscious project this forks.

MIT licensed — see
[LICENSE](https://github.com/Alpaq92/TopSecret.Cryptography/blob/master/LICENSE).
All credit for the underlying Argon2 implementation belongs to
[Keef Aragon](https://github.com/kmaragon) and Konscious's contributors.
