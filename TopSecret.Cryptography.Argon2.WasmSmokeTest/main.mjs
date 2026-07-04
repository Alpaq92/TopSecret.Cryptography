import { dotnet } from './_framework/dotnet.js'

const { runMainAndExit } = await dotnet
    .withDiagnosticTracing(false)
    .create();

await runMainAndExit();
