# TopSecret.Cryptography.ArgonBenchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org/) performance suite for
[TopSecret.Cryptography.Argon2](https://www.nuget.org/packages/TopSecret.Cryptography.Argon2) —
measures Argon2i/Argon2d/Argon2id throughput across degrees of parallelism
and target frameworks (`net462;net8.0;net10.0`).

This is a development tool from the
[TopSecret.Cryptography](https://github.com/Alpaq92/TopSecret.Cryptography)
repository, not a library meant to be referenced from your own code — it's
published so the benchmark results behind that repo's performance claims are
independently reproducible.

```bash
dotnet run --project TopSecret.Cryptography.ArgonBenchmarks -c Release
```

See the [full README](https://github.com/Alpaq92/TopSecret.Cryptography#readme)
and [CONTRIBUTING](https://github.com/Alpaq92/TopSecret.Cryptography/blob/master/CONTRIBUTING.md)
for everything else.

MIT licensed — see
[LICENSE](https://github.com/Alpaq92/TopSecret.Cryptography/blob/master/LICENSE).
