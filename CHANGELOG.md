# Changelog

## [2.1.0](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v2.0.0...v2.1.0) (2026-07-04)


### Features

* **argon2:** wipe internal working memory after every hash ([#18](https://github.com/Alpaq92/TopSecret.Cryptography/issues/18)) ([a989a25](https://github.com/Alpaq92/TopSecret.Cryptography/commit/a989a2542249cf75c04958ee85d7deeff738c4ac))

## [2.0.0](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v1.4.5...v2.0.0) (2026-07-04)


### ⚠ BREAKING CHANGES

* **argon2:** DegreeOfParallelism > 1 on a detected single-threaded host (OperatingSystem.IsBrowser() or Environment.ProcessorCount == 1) now throws PlatformNotSupportedException from both GetBytes and GetBytesAsync. Previously, GetBytesAsync completed successfully in that configuration (verified and documented behavior of the prior release) - any caller relying on that completing, rather than failing fast, will now see an exception instead. Separately, TopSecret.Cryptography.Argon2 no longer brings in TopSecret.Cryptography.Blake2 as a transitive NuGet dependency: code that referenced Blake2's public HMACBlake2B without an explicit PackageReference to TopSecret.Cryptography.Blake2 will fail to build after upgrading and needs that reference added directly.

### Bug Fixes

* **argon2:** make GetBytes complete on single-threaded runtimes at p=1 ([f053fd4](https://github.com/Alpaq92/TopSecret.Cryptography/commit/f053fd4facb2bafd2cab7097826686a3efb227e9))

## [1.4.5](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v1.4.4...v1.4.5) (2026-07-04)


### Bug Fixes

* **ci:** mark dev-tool projects non-packable; add one-off unlist workflow ([#11](https://github.com/Alpaq92/TopSecret.Cryptography/issues/11)) ([5e9ee06](https://github.com/Alpaq92/TopSecret.Cryptography/commit/5e9ee067d4b6b42167233cf219e504a1820485ea))


### Documentation

* rewrite NuGet package descriptions ([#12](https://github.com/Alpaq92/TopSecret.Cryptography/issues/12)) ([0385227](https://github.com/Alpaq92/TopSecret.Cryptography/commit/0385227f21e4ecaef98bdefda5208e27a246478d))

## [1.4.4](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v1.4.3...v1.4.4) (2026-07-04)


### Documentation

* add badges, trim intro, and align WASM motivation framing ([#8](https://github.com/Alpaq92/TopSecret.Cryptography/issues/8)) ([b65720d](https://github.com/Alpaq92/TopSecret.Cryptography/commit/b65720d10e1016ab3fe9975011b918b954f8b749))

## [1.4.3](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v1.4.2...v1.4.3) (2026-07-04)


### Bug Fixes

* **ci:** unquote the nupkg glob so the shell actually expands it ([#7](https://github.com/Alpaq92/TopSecret.Cryptography/issues/7)) ([f47594a](https://github.com/Alpaq92/TopSecret.Cryptography/commit/f47594ac29eef880e798463c6d7f14dbe6a82f9c))

## [1.4.2](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v1.4.1...v1.4.2) (2026-07-04)


### Bug Fixes

* **ci:** use bash for the NuGet push step on windows-latest ([276d724](https://github.com/Alpaq92/TopSecret.Cryptography/commit/276d72418121afdfbe234b5357be0c77dc152a7c))

## [1.4.1](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v1.4.0...v1.4.1) (2026-07-04)


### Bug Fixes

* **ci:** repair YAML syntax break in dependabot-auto-merge.yml ([b52baf7](https://github.com/Alpaq92/TopSecret.Cryptography/commit/b52baf7dd0023d5cfe9b2e1a147fba0bfdfb1baa))

## [1.4.0](https://github.com/Alpaq92/TopSecret.Cryptography/compare/v1.3.1...v1.4.0) (2026-07-04)


### Features

* Bump xunit.runner.visualstudio from 2.8.2 to 3.1.5 ([#3](https://github.com/Alpaq92/TopSecret.Cryptography/issues/3)) ([ece60bf](https://github.com/Alpaq92/TopSecret.Cryptography/commit/ece60bf001f0b27c379fb01a39c377724a93c500))


### Bug Fixes

* **ci:** approve Dependabot PRs so auto-merge actually completes ([6def91b](https://github.com/Alpaq92/TopSecret.Cryptography/commit/6def91b05fc6c27e781304de872ef004305549b8))
* **ci:** approve release-please PRs so they can auto-merge ([f87e56d](https://github.com/Alpaq92/TopSecret.Cryptography/commit/f87e56d811f609adbfe48bfd345dd8e6e0661638))
* **deps:** bump actions/checkout from 4 to 7 ([#2](https://github.com/Alpaq92/TopSecret.Cryptography/issues/2)) ([aa32761](https://github.com/Alpaq92/TopSecret.Cryptography/commit/aa32761d88e10c707bb5f592ccbc6cef1461f0b5))
* **deps:** bump actions/setup-dotnet from 4 to 5 ([#1](https://github.com/Alpaq92/TopSecret.Cryptography/issues/1)) ([fcf0e0e](https://github.com/Alpaq92/TopSecret.Cryptography/commit/fcf0e0e716b0897feace5030a903b1a0da64b94d))
