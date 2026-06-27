# Contributing

Thank you for considering a contribution. This document covers how to set up a dev environment, the conventions the project follows, and the pull-request process.

## Table of contents

- [Getting started](#getting-started)
- [Project structure](#project-structure)
- [Making changes](#making-changes)
- [Adding a new LLM model](#adding-a-new-llm-model)
- [Coding conventions](#coding-conventions)
- [Pull request checklist](#pull-request-checklist)
- [Reporting issues](#reporting-issues)

---

## Getting started

1. Fork the repository and clone your fork.
2. Follow the **Getting started** section in the [README](README.md) to configure AWS credentials and `appsettings.Development.json`.
3. Confirm the project builds cleanly:
   ```bash
   dotnet build BedRock.POC/BedRock.POC.csproj
   ```
4. Create a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```

---

## Project structure

```
BedRock.POC/
├── Domain/          Pure business types — no framework or AWS dependencies allowed here
├── Application/     Use cases and interfaces — depends only on Domain
├── Infrastructure/  AWS adapters, Polly pipelines, prompt builder
│   ├── Aws/
│   │   ├── LLM/     One adapter class per Bedrock model
│   │   └── Models/  Internal Textract document types
│   └── Configuration/  IOptions<T> binding classes
└── API/             Minimal API endpoints and DI wiring
```

**Layer rule:** outer layers may depend on inner layers, never the reverse. AWS SDK types (`Amazon.*`) must not appear in `Domain/` or `Application/`.

---

## Making changes

### Bug fixes

- Open an issue first if the bug is non-trivial so it can be discussed.
- Keep the fix minimal — don't refactor surrounding code in the same PR.

### New features

- Open an issue describing the feature before coding to avoid duplicated effort.
- Write the change against the innermost layer that makes sense; only touch outer layers if required.

### Configuration changes

- All tuneable values belong in `appsettings.json` bound through an `IOptions<T>` class in `Infrastructure/Configuration/`.
- Never hardcode region names, bucket names, model IDs, or credentials in source code.

---

## Adding a new LLM model

The factory is open/closed — adding a model requires zero changes to existing classes:

1. **Add the enum member** in `Enums/BedrockFoundationModel.cs`:
   ```csharp
   public enum BedrockFoundationModel
   {
       Claude35Sonnet,
       CommandLight,
       MyNewModel      // ← add here
   }
   ```

2. **Add the config entry** in `appsettings.json` (and in `appsettings.Development.json` locally):
   ```json
   "Bedrock": {
     "Models": {
       "MyNewModel": {
         "ModelId": "provider.model-name-v1:0",
         "Temperature": 0.5,
         "TopP": 0.9
       }
     }
   }
   ```

3. **Create the adapter** in `Infrastructure/Aws/LLM/`:
   ```csharp
   public class MyNewModelAdapter(
       ILogger<MyNewModelAdapter> logger,
       IAmazonBedrockRuntime client,
       IOptions<BedrockModelOptions> options,
       IPromptBuilder promptBuilder)
       : BedrockModelBase(logger, client, options, promptBuilder)
   {
       public override BedrockFoundationModel SupportedModel => BedrockFoundationModel.MyNewModel;

       protected override string GetModelId(BedrockModelOptions opts) =>
           opts.Models["MyNewModel"].ModelId;

       protected override float GetTemperature(BedrockModelOptions opts) =>
           opts.Models["MyNewModel"].Temperature;

       protected override ConverseRequest BuildConverseRequest(
           string systemPrompt, string userContent, string modelId, float temperature) =>
           new() { /* model-specific request shape */ };
   }
   ```

4. **Register it** in `API/Extensions/ServiceCollectionExtensions.cs` alongside the existing adapters:
   ```csharp
   services.AddScoped<ILanguageModelClient, MyNewModelAdapter>();
   ```

The `BedrockModelFactory` discovers it automatically. No switch statements needed.

---

## Coding conventions

- **No inline secrets.** Use `IOptions<T>` for all configuration.
- **No `Console.WriteLine`.** Use `ILogger<T>` throughout.
- **No `Thread.Sleep`.** Use `await Task.Delay(...)` for async waits.
- **No AWS types in Domain or Application.** Keep the anti-corruption layer intact.
- **No comments explaining what the code does** — use well-named identifiers. A comment is appropriate only when it explains a non-obvious constraint or workaround.
- **No `partial class` without a clear reason.**
- Follow C# naming conventions: `PascalCase` for public members, `_camelCase` for private fields.

---

## Pull request checklist

Before opening a PR, confirm:

- [ ] `dotnet build` passes with 0 errors and 0 warnings
- [ ] No hardcoded secrets, bucket names, regions, or model IDs
- [ ] No `Console.WriteLine` or `Thread.Sleep`
- [ ] No AWS SDK types (`Amazon.*`) referenced from `Domain/` or `Application/`
- [ ] `appsettings.Development.json` is **not** included in the commit
- [ ] The `.http` file is updated if a new endpoint or request shape was added

Quick verification:
```bash
grep -rn "Thread.Sleep\|Console.Write" BedRock.POC/
grep -rn "Amazon\." BedRock.POC/Domain BedRock.POC/Application
```

---

## Reporting issues

Open a GitHub issue with:

- A short description of the problem
- Steps to reproduce (include the request shape if it's an API issue)
- Expected vs actual behaviour
- .NET version (`dotnet --version`) and AWS region

Please **do not** include real S3 bucket names, AWS account IDs, or document contents in issue reports.
