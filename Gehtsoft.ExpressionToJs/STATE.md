# STATE — Gehtsoft.ExpressionToJs

Current state of the project. (For *how it works / how to work here* see CLAUDE.md; for the full
improvement history see PLAN.md.)

## Release

**3.1 — done and published to MyGet.** Tests green (380, xUnit v3 on Jint). Docs build clean.

## What 3.1 delivered

- **Migration to xUnit v3.** The whole test suite runs on `xunit.v3` (`net8.0`), executing emitted
  JavaScript in-process with Jint.
- **Test coverage.** A comprehensive `Complete/` differential suite — each case checked three ways
  (compiled C# delegate, emitted JS in Jint, and the two against each other), organised by area
  (operators, math, strings, LINQ, enums, XOR, constant folding, dates, the registries, a realistic
  profile scenario). Earlier correctness/parity fixes from PLAN.md §1 are locked in by it.
- **Extensibility refactor.** `AddCall`/`AddMemberAccess`/`EmitConstant` became thin dispatch loops
  over translator pipelines, exposed as flat registries on the compiler:
  - `Methods` (`IJsMethodRegistry`), `Constants` (`IJsConstantRegistry`), `Members` (`IJsMemberRegistry`).
  - **`Parameters` (`IJsParameterRegistry`)** — the new form-binding registry: `MapReference(Func<Type,bool>)`
    turns on the built-in `reference()`/`reference('path')`/`jsv_index` rendering (off by default);
    `Map(...)` emits any custom shape; public `ExpressionCompiler.ParameterAccessPath(Expression)`
    builds the dotted path. This makes the form-validation `reference()/value` binding a built-in
    feature — **no subclassing needed**.
- **Documentation.** A full docgen site under `help/` (build: `dotnet build help/project.proj
  /t:MakeDoc`): landing page, namespace overview, and nine how-to/reference guides in
  `src/articles.ds` — walkthrough, model-object rules, client frameworks, supported features, dates &
  time zones, extending the translator, unit-testing. Every emitted-JS example is pinned by a test.
- **Packaging.** NuGet package (`nuget/`, `nuget pack` from the nuspec) now ships a package `README.md`
  (with docs/source links and an ASP.NET client example) and an **embedded consumer skill**
  (`skills/SKILL.md`, delivered via `nuget-skills`). Metadata carries `projectUrl` (docs), `repository`,
  and `readme`.

## Not done yet

- **Publish the docs** to `https://docs.gehtsoftusa.com/Gehtsoft.Expression.ToJs/` (upload `help/dst/`;
  the URL is already referenced from the nuspec and README, so it 404s until then).
- **Downstream refactor** of Gehtsoft.EF.Toolbox `ValidationExpressionCompiler` onto the `Parameters`
  registry (bump its dependency to 3.1+) — see **REFERENCE_REFACTOR.md**.
- **Feature stragglers** (PLAN.md §4): `Average`, predicate-less `FirstOrDefault`/`LastOrDefault`, bit
  shifts, `int.TryParse`/`double.TryParse`, string `+` concat, `DateTime.AddMonths`/`AddYears`.
- **Optional:** per-`DateTimeKind` constant normalization; global enum name-mode; packaging/CI refresh
  and nullable reference types (PLAN.md §5).
