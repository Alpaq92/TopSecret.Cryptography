# TopSecret.Cryptography.ComparisonHarness

A cross-language parity fuzzing harness for
[TopSecret.Cryptography.Argon2](https://www.nuget.org/packages/TopSecret.Cryptography.Argon2) —
compares its output against an independent Python
([`argon2-cffi`](https://pypi.org/project/argon2-cffi/), which wraps the
official `phc-winner-argon2` C reference implementation) reference across
randomized inputs.

This is a development tool from the
[TopSecret.Cryptography](https://github.com/Alpaq92/TopSecret.Cryptography)
repository, not a library meant to be referenced from your own code — it's
published so the fuzz-comparison evidence behind that repo's correctness
claims is independently reproducible.

```bash
dotnet run --project TopSecret.Cryptography.ComparisonHarness
```

See the [full README](https://github.com/Alpaq92/TopSecret.Cryptography#readme)
and [CONTRIBUTING](https://github.com/Alpaq92/TopSecret.Cryptography/blob/master/CONTRIBUTING.md)
for everything else.

MIT licensed — see
[LICENSE](https://github.com/Alpaq92/TopSecret.Cryptography/blob/master/LICENSE).
