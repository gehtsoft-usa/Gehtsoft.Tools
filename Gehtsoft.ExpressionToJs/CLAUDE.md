# CLAUDE.md

Guidance for working in the Gehtsoft.ExpressionToJs repository.

## What this project is

A library that converts a C# `LambdaExpression` into an equivalent **JavaScript expression
string**. The purpose is to have **one source of truth** for form validation: the same C#
predicate runs server-side (compiled/invoked as a normal delegate) and client-side (the emitted
JS, evaluated in the browser). Typical use is boolean validation expressions.

## Layout

- `Gehtsoft.ExpressionToJs/` — the library (`netstandard2.0`, `LangVersion 14.0`).
  - `ExpressionWalker.cs` — `ExpressionCompiler`, the core. Recursively walks the expression
    tree (`WalkExpression`) and emits JS. Operators map to `jsv_*` calls inline; calls, member
    access and constants dispatch through translator pipelines (see below). Most emit methods are
    `protected virtual` so subclasses can override.
  - `MethodTranslators.cs` — the method-call translation pipeline: `IExpressionEmitContext`,
    `IMethodCallTranslator`, the registry interfaces (`IJsMethodRegistry`/`IJsConstantRegistry`/
    `IJsMemberRegistry`), the data-driven `TableMethodTranslator`, and focused translators
    (Regex, IndexOf, StartsWith/EndsWith comparisons, Contains, LINQ, indexer, ToString, DateDiff).
    Also defines `enum DateTimeMode`.
  - `ValueTranslators.cs` — constant and member-access translators: `IConstantTranslator` /
    `IMemberTranslator`, `DateTimeConstantTranslator`/`DateTimeMemberTranslator` (mode-aware),
    `NullableMemberTranslator`, `TableMemberTranslator`, the parameter-access fallback, etc.
  - `Functions.cs` — `static class Functions`. Server-side C# implementations of helpers that
    have no direct BCL equivalent (`DaysSince`, `MonthsSince`, `YearsSince`,
    `IsCreditCardNumberCorrect`, `ToBool`, `ToInt`, `Fractional`, `IsNull`/`IsNotNull` etc.).
    These are what the C# side calls; the JS side calls the matching `jsv_*` function.
  - `stub.js` — the JavaScript runtime. Defines every `jsv_*` helper the emitted expressions
    call. **Embedded as a resource** (`EmbeddedResource` in the csproj); retrieve it at runtime
    via `ExpressionToJsStubAccessor.GetJsIncludesAsString()`. The host page must include this
    stub before evaluating any compiled expression.
  - `StubAccessor.cs` — loads `stub.js` from the assembly's embedded resources.
- `Gehtsoft.ExpressionToJs.Tests/` — xUnit v3 tests (`net8.0`), run on **Jint** (an in-process
  JS engine). The key pattern: compile a C# lambda → evaluate the emitted JS in Jint with the
  stub loaded → assert the JS result equals the C# expected value.
  - `Complete/` — the comprehensive differential suite (the bulk of the tests). `DifferentialTestBase`
    checks each case three ways: compiled C# delegate, emitted JS in Jint, and the two against each
    other. Organized by area: operators, math, strings, LINQ, enums, XOR, constant folding, the
    `Methods`/`Constants`/`Members` registries, `DateTimeMode`, etc.
  - `CompilerTest.cs` — the original round-trip suite (`TestExpression<TA,TR>` helper + `SetupJint`).
  - `ValidationExpressionCompiler.cs` — a sample `ExpressionCompiler` subclass that maps the
    lambda's parameters onto `reference('path')` / `value` for the form-validation use case.
  - `FunctionsTest.cs` — direct tests of `Functions`. `Debug.cs` — scratch/explicit tests.
  - `Complete/*GuideTests.cs` + `ReadmeExamplesTests.cs` + `ParameterRegistryTests.cs` — pin the
    **exact emitted JS** for the examples shown in the docs/README (see the doc-examples rule below).
- `help/` — the documentation (docgen; `Gehtsoft.Build.DocGen`). Hand-written `src/index.ds` (landing),
  `src/ns/` (namespace overview), `src/articles.ds` (nine how-to/reference guides, in reading order,
  `@sortarticles=no`); auto-generated API in `src/raw/` (regenerated from the built DLL's XML doc by the
  `Scan`+`Prepare` targets). Build the site with `dotnet build help/project.proj /t:MakeDoc` → `help/dst/`.
- `nuget/` — packaging (NuGet via `nuget pack` from `Gehtsoft.ExpressionToJs.nuspec`, **not** `dotnet
  pack`): `README.md` (the package readme), `pack.bat`/`push.bat`. The nuspec sets `projectUrl` (docs),
  `repository`, `readme`, and ships the embedded skill + readme as `<file>` entries.
- `skills/SKILL.md` — a consumer skill bundled in the package (via the nuspec) for the
  `diegofrata/nuget-skills` tool: it teaches a consuming agent how to use the library. One file, no
  subdirs (the tool concatenates top-level `.md` only).

## How the compiler works (mental model)

`WalkExpression(expr)`:
1. If `expr.CanReduce`, reduce it.
2. **Constant folding:** `GetExpressionValue` first runs a cheap `ContainsNonConstant` check
   (`NonConstantFinder`); only subtrees with no *free* parameter and no ambient-now accessor
   (`DateTime.Now/Today/UtcNow`) are compiled + invoked and emitted as a literal (via the constant
   pipeline). Parameter-referencing and ambient-now subtrees skip the compile and stay dynamic
   (`new Date()` / `jsv_today(...)`).
3. Otherwise switch on `expr.NodeType`: binary/unary operators → `jsv_*` calls (e.g.
   `jsv_plus`, `jsv_less`); `Call` → `AddCall`; member access → `AddMemberAccess`; constants →
   `EmitConstant`; etc.
4. **Translator pipelines.** `AddCall`, `AddMemberAccess`, and `EmitConstant` are thin dispatch loops:
   *user registrations → fixed built-ins → throw*. Built-ins live in `MethodTranslators.cs` /
   `ValueTranslators.cs` as `IMethodCallTranslator` / `IMemberTranslator` / `IConstantTranslator`
   (a data-driven `Table*Translator` for the 1:1 maps plus focused classes for conditional cases).
   Translators are keyed by type + name and are **disjoint**, so order isn't load-bearing (the one
   exception: the terminal parameter-access member fallback).

**Extending without subclassing:** `compiler.Methods` / `compiler.Constants` / `compiler.Members`
(`MapMethod`/`MapConstant<T>`/`MapMember` + `AddTranslator`), plus `compiler.Parameters`
(`IJsParameterRegistry`) for *parameter rendering* — `MapReference(Func<Type,bool>)` turns on the
built-in form-validation `reference()`/`reference('path')`/`jsv_index` rendering (off by default);
`Map(Func<Type,bool>, Func<ParameterExpression,string>, Func<Expression,ParameterExpression,string>)`
emits any custom shape (`value.Property`, `reference('Type','path')`). Public helper
`ExpressionCompiler.ParameterAccessPath(Expression)` builds the dotted path. Bindings apply only to
the rule's root parameters (nested LINQ params keep their names). User registrations are consulted
before built-ins. Subclassing `AddParameter`/`AddParameterAccess` still works (the older
`ValidationExpressionCompiler` sample) but is no longer needed for the `reference()/value` binding.

**Date/time framing:** `compiler.DateMode` (`DateTimeMode.Local` default, or `Utc`; also a
`new ExpressionCompiler(lambda, mode)` ctor) selects how calendar constructs are emitted — `new Date(...)`
+ `getFullYear()` etc. for Local, `new Date(Date.UTC(...))` + `getUTCFullYear()` etc. for Utc, and a `utc`
flag into `jsv_monthssince`/`jsv_yearssince`. Epoch-based ops (`±TimeSpan`, subtraction, `AddDays`,
`.Total*`, `DaysSince`) are frame-independent. **Contract:** the host must bind JS-side dates in the same
frame as `DateMode`. Set it before reading `JavaScriptExpression`. `DateTime.Now`/`Today`/`UtcNow` are
emitted dynamically (`new Date()` / `jsv_today(...)`), so they evaluate at validation time, not compile time.

Server/client parity is maintained **by hand**: every `Functions.X` must have a behaviorally
identical `jsv_x` in `stub.js` — historically the main correctness risk. The `Complete/` differential
suite now guards it by running both sides and comparing (see PLAN.md).

## Build & test

```bash
dotnet build Gehtsoft.ExpressionToJs.sln
dotnet test Gehtsoft.ExpressionToJs.sln        # runs the Jint round-trip tests
```

Tests depend on `Jint` (4.x), `xunit.v3` (3.x), `Microsoft.NET.Test.Sdk`.

## Conventions & gotchas

- The library targets `netstandard2.0` for reach; do **not** add newer-TFM-only APIs there.
- Tests are xUnit **v3** (`xunit.v3` package, `using Xunit;`), not v2.
- When adding support for a new C# construct you typically change **three** places in lockstep:
  (1) emit logic — usually a `Table*Translator` entry (or a focused translator) in
  `MethodTranslators.cs`/`ValueTranslators.cs`, occasionally a `WalkExpression` switch case;
  (2) the `jsv_*` helper in `stub.js`; and (3) — if it has no BCL form — a server-side method in
  `Functions.cs`. Then add a differential test under `Complete/`.
- Numeric/date/null semantics differ between C# and JS; new features need a round-trip test that
  actually exercises the divergent cases (integer division, rounding mode, timezones, loose
  equality). See PLAN.md §1 for known divergences.
- **Every emitted-JS example in the docs/README must be backed by a test** asserting that exact
  output (this caught a real `&&`-vs-`jsv_and` error). When you write or change a `// -> jsv_...`
  example, add/point to an exact-string `Assert.Equal` in a `*GuideTests`/`ReadmeExamplesTests`, run
  it, and paste the verified output.
- **Trunk-based:** commit directly to `master`; do not create feature branches for this repo.
- **docgen authoring (`.ds`) gotchas:** write `&` as `&amp;` but keep `<`/`>` raw; use BBCode
  `[c]`/`[i]`/`[b]` not XML tags; never start a line with `- `/`* `; for indented code in `@example`
  use the `!` line-prefix; `@show=yes` expands an example; mermaid via `@highlight=diagram`+`@show=yes`;
  full-width table is `@width=100%` (with the `%`), columns via `@col @width=30%`/`70%` on the header;
  link `[clink=]` to **class** keys (member keys carry unstable CRC suffixes); don't use `@see`
  (renders an empty-description table) — end an article with a "See also" `@list`.

## Strategic context

Standalone library. Source: `git@github.com:gehtsoft-usa/Gehtsoft.ExpressionToJs.git`. Published to
NuGet/MyGet (current **3.1**). Docs: `https://docs.gehtsoftusa.com/Gehtsoft.Expression.ToJs/` (the
generated `help/dst/` output). It formerly lived under the `Gehtsoft.Tools` tree and is being extracted
into its own repo; treat it as independent (no Tools2 coupling).

## Status & backlog

- **Current state** of the project (what's released, what was done) → **STATE.md**.
- **Full improvement history** and the remaining feature backlog → **PLAN.md**.
- **Downstream follow-up** (refactor Gehtsoft.EF.Toolbox `ValidationExpressionCompiler` onto the
  `Parameters` registry) → **REFERENCE_REFACTOR.md**.
