# TopSecret.Cryptography.Blake2

Blake2b hashing for .NET (RFC 7693), implemented as a
`System.Security.Cryptography.HMAC` — a drop-in anywhere that accepts
`HashAlgorithm`/`HMAC`. A maintained fork of
[Konscious.Security.Cryptography.Blake2](https://github.com/kmaragon/Konscious.Security.Cryptography).

```csharp
using TopSecret.Cryptography;

var hmac = new HMACBlake2B(512);        // unkeyed, 512-bit output
hmac.Initialize();
byte[] digest = hmac.ComputeHash(data);

byte[] key = ...;
var keyed = new HMACBlake2B(key, 512);  // keyed, 512-bit output
```

Hash size can be any 8-bit-aligned value from 8 to 512 bits; keys can be 0 to
64 bytes. Targets `netstandard2.0;net462;net6.0;net8.0;net10.0`. No `Task`,
async, or threading code anywhere — Blake2b's compression is inherently
sequential, so it runs the same way on single-threaded runtimes such as
browser WASM as anywhere else.

See the [full README](https://github.com/Alpaq92/TopSecret.Cryptography#readme)
for everything that differs from the upstream Konscious project this forks.

MIT licensed — see
[LICENSE](https://github.com/Alpaq92/TopSecret.Cryptography/blob/master/LICENSE).
All credit for the underlying Blake2 implementation belongs to
[Keef Aragon](https://github.com/kmaragon) and Konscious's contributors.
