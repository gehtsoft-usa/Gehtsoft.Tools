# Gehtsoft.ExpressionToJs — Improvement Plan

The library walks a C# `LambdaExpression`, emits a JavaScript string, and ships a
`jsv_*` runtime stub (`stub.js`) so the *same* C# lambda is the single source of
truth for both server-side and client-side form validation. `Functions.cs`
provides server-side equivalents; `ValidationExpressionCompiler` maps parameters
to `reference()/value`. Jint-based round-trip tests compile C# → run JS → compare.

This document captures the assessment and a prioritized plan.

## What the library does well

- Sound core design: walk the expression, emit JS, ship a runtime stub.
- Jint round-trip tests are the right testing instinct.
- Good breadth: arithmetic, comparisons, conditionals, string ops, char
  classification, regex, DateTime/TimeSpan, nullable, LINQ `Any/All/Count/First/Last`,
  constant folding, and the `reference()/value` mapping for form validation.
- `netstandard2.0` keeps reach wide.

---

## Implementation status (Phase 1)

Low-layer differential test suite added under `Gehtsoft.ExpressionToJs.Tests/Complete/`
(189 passing, 1 skipped). The §1 defects below are **fixed** and verified by those tests:

- ✅ Integer division truncates toward zero, and Int32 arithmetic wraps on overflow — both via
  a `| 0` coercion applied to int-typed results (`WrapInteger`).
- ✅ `Math.Round` uses banker's rounding (`jsv_round`), including the `Math.Round(x, digits)` overload.
- ✅ `Power` now emits `jsv_power` (stub function renamed from `jsv_powerof`).
- ✅ decimal/double equality tolerant between two numbers (`jsv_equal` epsilon) so `0.1 + 0.2 == 0.3`.
- ✅ `==`/`!=` are otherwise strict (`===`), matching C#'s typed comparison (no string/number coercion).
- ✅ String and regex constants are escaped (`EscapeJsString`, `EscapeJsRegex`).
- ✅ `get_Item` indexer bug (`Arguments[1]` → `Arguments[0]`).
- ✅ Whitespace detection uses `\s` (tabs/newlines/NBSP), matching `IsNullOrWhiteSpace`.
- ✅ `jsv_string2int` rejects trailing garbage (returns `NaN`), matching `Int32.Parse`.
- ✅ `jsv_yearssince` sign convention now matches `Functions.YearsSince`.
- ✅ Added support: null-coalescing `??` (`jsv_coalesce`) and `Contains` (`jsv_contains`,
  including array→span `MemoryExtensions.Contains`).
- Corrected one old test (`CompilerTest.cs`): `intArr.Length / 2` expected `2.0` (C# int division),
  not the previous `2.5` which had enshrined the bug.

- ✅ **DONE — Date/time mode (Local/UTC).** A customer-set `DateTimeMode` (default `Local`; opt into
  `Utc` via the property or the `ExpressionCompiler(lambda, mode)` ctor) governs every calendar-component
  construct consistently: constants (`new Date(...)` vs `new Date(Date.UTC(...))`), member reads
  (`getX` vs `getUTCX`), and `MonthsSince`/`YearsSince` (a `utc` flag passed to the stub). Epoch-based
  ops (±TimeSpan, subtraction, `AddDays`, `.Total*`, `DaysSince`) are unchanged. The mode rides on
  `IExpressionEmitContext.DateMode` so the shared translators stay stateless. **Contract:** the host must
  bind JS-side dates in the same frame as the mode. The static `AddConstant` shim stays Local-only.
  Tests in `DateTimeModeTests`.
- ✅ **DONE — `DateTime.Now`/`Today`/`UtcNow` are dynamic.** The fold check (`ContainsNonConstant`,
  formerly `ReferencesFreeParameter`) now treats these ambient-now accessors as non-constant, so they
  emit live JS instead of a frozen compile-time literal: `Now`/`UtcNow` → `new Date()`, `Today` →
  `jsv_today(false|true)` (mode-aware midnight). Chains like `DateTime.Now.AddDays(30)` stay dynamic;
  component reads follow `DateMode`. Tests in `DateTimeModeTests`. (`DateTimeOffset.Now/UtcNow` remain
  out of scope.) Follow-up still open: optional per-`DateTimeKind` constant normalization.

**Deferred (design-scope, not quick fixes):**

- ⏸️ **64-bit `long` precision** loss beyond 2^53 — unfixable without a bigint shim, and the
  tolerant double comparison can't catch it; no test added (would falsely pass).

## 1. Correctness bugs (highest priority — these undermine "one source of truth")

The value proposition is that server and client agree. Several places silently disagree:

- **Integer division diverges.** `jsv_divide(a,b)` emits `a / b`. C# `11 / 2` is `5`;
  JS gives `5.5`. The compiler ignores operand types. Detect integer operand types and
  emit `Math.trunc(a / b)` (or `(a/b)|0`). Tests pass only because sampled values divide evenly.
- **`Math.Round` rounding mode diverges.** C# `Math.Round` is banker's rounding (ToEven):
  `Math.Round(2.5) == 2`; JS `Math.round(2.5) == 3`. Emit a `jsv_round` replicating ToEven,
  or honor the `MidpointRounding` overload.
- **`get_Item` indexer bug.** `ExpressionWalker.cs:562` reads `expression.Arguments[1]` for a
  single-argument indexer (`Arguments.Count == 1`) → `IndexOutOfRangeException`. Should be `Arguments[0]`.
- **`Power` references a non-existent stub function.** `ExpressionType.Power` → `"jsv_power"`
  (line 64), but the stub defines `jsv_powerof` (stub.js:27). Power expressions produce a runtime ReferenceError.
- **No string/regex escaping in `AddConstant`.** `$"'{constantValue}'"` and `/{pattern}/`
  break on any `'`, `\`, newline, or `/` in the value, and are a JS-injection vector if a constant
  ever derives from user/config data. Needs proper JS string escaping (and regex-source escaping).
- **`decimal` constants lose precision** — emitted as a JS double. Real divergence for money
  validation; at minimum document it, ideally emit a fixed-point comparison helper.
- **`jsv_equal` uses loose `==`.** `null == undefined` is true, `"10" == 10` is true. C# `==`
  does neither. Use `===`/`!==` (with deliberate null/undefined normalization).
- **`jsv_isemptyorwhitespace` only matches spaces** (`/^ *$/`) — tabs/newlines/Unicode
  whitespace pass as non-empty, unlike C# `IsNullOrWhiteSpace`. Use `/^\s*$/`.
- **`MonthsSince`/`YearsSince`: server and client are structurally different algorithms.**
  Tests only check `Math.Floor(Math.Abs(...))` loosely, so off-by-one and sign disagreements are
  uncovered. Share one algorithm or pin with a differential test.
- ✅ **DONE — DateTime timezone handling.** Resolved via the customer-set `DateTimeMode` (Local/UTC) —
  see the Phase-1 status entry above for details.
- **`jsv_string2int` = `parseInt(s)`** (no radix) is lenient: `parseInt("12abc")` is `12`,
  C# `Int32.Parse` throws. Behavior should match the chosen contract.

## 2. The biggest gap: no differential test harness

Given the goal, the single highest-value addition is **property-based differential testing**:
generate random inputs, run the C# lambda *and* the Jint-evaluated JS, and assert equality across a
large sample. This is the only thing that reliably catches the class of divergences above (rounding,
integer division, date math, null semantics) and keeps `stub.js` in sync with `Functions.cs` — which
today must be hand-synchronized and has already drifted. Pair it with golden-file tests on the emitted
JS string for stability.

## 3. Architecture / maintainability

- ✅ **DONE — `AddCall` god method refactored.** The ~300-line method is now a thin dispatch loop over
  an ordered translator pipeline: a data-driven `TableMethodTranslator` for the trivial 1:1 maps plus
  focused `IMethodCallTranslator` classes for the conditional cases (Regex, IndexOf, StartsWith,
  Contains, LINQ, indexer, ToString) — see `MethodTranslators.cs`. Built-ins are a fixed, immutable,
  correctly-ordered list; consumers extend via the `IJsMethodRegistry` (`compiler.Methods`) which adds
  leaf-level translations consulted *before* built-ins (so they can shadow a method) but cannot reorder
  or remove built-ins, preserving precedence invariants. Behavior unchanged (full suite green; complex
  precedence tests added). The symmetric `AddMemberAccess`/property side (`IMemberTranslator`) is the
  natural follow-up.
- ✅ **DONE — `GetExpressionValue` no longer compiles every node.** It now first runs a cheap
  `FreeParameterFinder` (an `ExpressionVisitor`) to detect a reference to a *free* parameter (one not
  bound by a lambda inside the subtree); only closed subtrees are compiled+invoked, still inside a
  try/catch so a constant that throws at eval falls back to structural translation exactly as before.
  This removes the failed JIT compile + swallowed exception that previously fired at every
  parameter-referencing node. Behavior preserved (full suite green; `ConstantFoldingTests` lock the
  free-vs-lambda-bound distinction). Possible later optimization: memoize the finder to avoid O(n²) on
  pathologically large trees (negligible for validation-sized predicates).
- ✅ **DONE — Extensibility model.** Three flat, symmetric registries on the compiler, each backed by
  the same interfaces the built-ins use (dogfooded):
  - **Methods** — `compiler.Methods` (`IJsMethodRegistry`): `AddTranslator(IMethodCallTranslator)`,
    `MapMethod(Type,name)`, `MapMethod(MethodInfo)`.
  - **Constants** — `compiler.Constants` (`IJsConstantRegistry`): `AddTranslator(IConstantTranslator)`,
    `MapConstant<T>(Func<T,string>)`. Unblocks Guid/enum/value-struct constants. The `public static
    AddConstant` stays as a back-compat shim sharing the built-in path; emission now routes through an
    instance pipeline so custom + built-in run uniformly.
  - **Members** — `compiler.Members` (`IJsMemberRegistry`): `AddTranslator(IMemberTranslator)`,
    `MapMember(Type,name,template)`. `AddMemberAccess` is now a dispatch loop over a data-driven
    `TableMemberTranslator` (DateTime.*, TimeSpan.*, Nullable, Length/Count) plus the terminal
    parameter-access fallback.
  - **Parameters** — `compiler.Parameters` (`IJsParameterRegistry`), added 2026-06-19:
    `MapReference(Func<Type,bool>)` turns on the built-in form-validation `reference()` rendering
    (bare param → `reference()`, member chain → `reference('Path')`, array index → `jsv_index`);
    `Map(Func<Type,bool>, Func<ParameterExpression,string> parameter, Func<Expression,ParameterExpression,string> access)`
    emits any custom shape (e.g. `value.Property`, or `reference('Type','path')`). Off by default
    (unchanged verbatim emission); matches by type predicate but applies only to the rule's **root**
    parameters (nested LINQ-lambda params keep their names). Public helper
    `ExpressionCompiler.ParameterAccessPath(Expression)` turns a member chain into a dotted path for
    reuse in custom `access` funcs. This makes the form-validation `reference()/value` binding a
    built-in feature — consumers no longer subclass `ExpressionCompiler` for it. Tests:
    `ParameterRegistryTests` (17). See `REFERENCE_REFACTOR.md` for migrating Gehtsoft.EF.Toolbox's
    `ValidationExpressionCompiler` onto it.

  **Matching model (decided):** translators are keyed by type + name (+ arity/shape); built-ins are
  **disjoint**, so iteration order is not load-bearing and two matches at one tier is a caller logic
  error. The single deliberate ordering is the member parameter-access *fallback*, which is terminal
  (a typed member like `DateTime.Year` wins over it). User registrations run before built-ins purely as
  a deliberate-override escape hatch. New tests: `MethodRegistryTests`, `ValueRegistryTests`.

  **Boundary note (decided):** the library's contract is just (1) C# expression → JS *text* and (2) the
  canonical stub source via `ExpressionToJsStubAccessor.GetJsIncludesAsString()`. How the consumer loads,
  bundles, or minifies JS — and any companion helpers for their custom mappings — is entirely theirs. So
  there is deliberately **no** companion-JS registration API.

- **Stub tree-shaking — WON'T DO (decided).** The stub is loaded once (long-lived JS engine, or a
  site-wide script), so "emit only the helpers this expression used" buys nothing and adds real
  complexity (per-expression usage tracking, dedup across expressions on a page). Not worth it.
- **No "give me a complete function" API.** Consumers must concatenate stub + expression themselves.
  A `CompileToFunction(name, paramNames)` returning a ready `function(...){ return ...; }` (optionally
  bundled with the needed stub) would be much friendlier.

## 4. Expression features worth adding

Rough priority for form-validation use cases:

- **Null-coalescing `??`** (`ExpressionType.Coalesce`) — unsupported today, very common.
- **String concatenation** — C# `"a" + b` on strings compiles to a `string.Concat` *call*, not
  `Add`; not handled today.
- ✅ **DONE (partial) — more string methods**: `EndsWith` (+ `StringComparison`), `Replace`,
  `PadLeft`/`PadRight` (with optional pad char), `TrimStart`/`TrimEnd`, `ToUpperInvariant`/`ToLowerInvariant`,
  `LastIndexOf`, plus char-literal args (`Replace('a','b')`, `Contains('a')`, `PadLeft(5,'0')`) via new
  char-constant support. Deliberately **not** done: `string.Format` and `Split` (locale/format-correctness
  on the JS side is a nightmare — out of scope by decision).
- ✅ **DONE (most) — more LINQ**: `Where`/`Select` (return JS arrays, so they chain),
  `Sum`/`Min`/`Max` (with or without selector), `Distinct`, on top of the existing
  `Any/All/Count/First/Last`. `Contains` was already done. Tests in `LinqTests` (incl. chained
  `Where→Select→Sum`). Still TODO: `Average`, and `FirstOrDefault`/`LastOrDefault` *without* a predicate.
- ✅ **DONE — Enums generally.** Enum constants now emit their underlying numeric value (via
  `TryEmitBuiltinConstant` → `value is Enum`), so any enum works, not just `DayOfWeek` — consistent with
  the `Convert(enum→int)` path comparisons already use. Flags enums emit the combined value. Per-type
  *name* emission is available via `compiler.Constants.MapConstant<TEnum>(e => "'" + e + "'")` (for bare
  constants). Contract: client-side enum fields must carry the numeric underlying value. Tests in
  `EnumTests`. Global name-mode (with `Convert` interception so comparisons stay consistent) remains an
  opt-in follow-up — see §3 enum discussion.
- ✅ **DONE — `ExclusiveOr` (`^`)**: logical XOR on bool (`jsv_boolxor`, returns a real boolean) and
  bitwise XOR on integers (`jsv_xor`, `| 0`-wrapped). Still TODO: bit shifts, `int.TryParse`/`double.TryParse` patterns.
- **`DateTime.Today`, `DateTime.UtcNow`, `DateTimeOffset`, `TimeSpan.FromX`, `DateTime.Parse`**, and
  `AddMonths/AddYears` (only Add days/hours/minutes/seconds exist).
- **Clear, actionable error messages** for unsupported nodes (include the offending member and a hint).

## 5. Project hygiene

- **Packaging / CI / nullable** — still open: nupkg version is stale, no visible CI, nullable reference
  types off. Worth a packaging refresh if this stays in service.
- ✅ **DONE — README** for the library (intent, examples, project layout).
- ✅ **DONE — XML doc comments** on the public API — see the Documentation section below.
- **Tests** are good in spirit but are giant multi-assert `[Fact]`s — convert to `[Theory]`/`InlineData`
  for failure isolation, and add **negative tests** (unsupported expression → expected exception/message).

## 6. Documentation (Phase 2 — in progress)

Goal: a docgen-built reference + guides, with the public API documented from XML comments so the
reference stays in sync with the code. Project lives in `help/` (docgen; `Gehtsoft.Build.DocGen`).

**Done:**

- ✅ **Public API XML doc comments**, written *use-first* (why/when/how, not behavior; parameters
  framed by how their values change the outcome). Covers `ExpressionCompiler` (class, ctors,
  `JavaScriptExpression`, `DateMode`, `Methods`/`Constants`/`Members`, the statics, `Equals`),
  `Functions.*`, `ExpressionToJsStubAccessor`, `DateTimeMode`, and the extension contracts
  (`IMethodCallTranslator`/`IConstantTranslator`/`IMemberTranslator` + the registry interfaces).
- ✅ **Public surface minimized.** All concrete translators (focused built-ins + `Table*`/`Delegate*`)
  are now `internal`; only the entry points and extension *interfaces* are public. Test access via
  `InternalsVisibleTo`. Confirmed: internals no longer appear in generated docs.
- ✅ **docgen wired up and building.** `help/` produces the API reference from the built DLL
  (`Asm2Xml` → `cs2ds` → `src/raw/*.ds`) merged with hand-written `.ds`; `MakeDoc` is green.
- ✅ **`index.ds`** landing page written — the *why* (one rule, two runtimes, no drift) with a short
  API-at-a-glance; *how* deferred to articles.
- ✅ **docgen authoring conventions learned & captured** in `DOCGEN_SKILL_BUGS.md` (3 issues, with
  fixes + automatable lints): (1) the whole `<summary>` becomes the one-line `@brief` and `<remarks>`
  is dropped — so write `one-line brief` + blank line + detail paragraphs, no `<remarks>`;
  (2) use `[c]`/`[i]`/`[b]` BBCode, not XML `<c>`/`<i>`/`<b>` (which cs2ds mangles); (3) a line starting
  with `- `/`* ` becomes a stray list bullet — never begin a line (or a wrapped `///` line) with one.

- ✅ **DONE — Namespace overview** in `src/ns/Gehtsoft.ExpressionToJs.ds` (hand-written `@group`):
  entry points + extension contracts, links to the API classes/interfaces. Merges with the empty
  auto-generated group (hand-written loads first, wins). Guides navigation lives in `index.ds`, not here.
- 🔄 **How-to articles — in progress, tracked page-by-page in `DOCUMENTATION_PLAN.md`** (the live
  doc plan; read it first next session). Structure: a single `src/articles.ds` with seven
  `@article`s in `main` (no `guides` group), `@sortarticles=no`, wired into `project.xml` as one
  `<dg:file>`. The user validates **each page** before the next.
  - **Done:** `index.ds` (API-at-a-glance 30/70 table; guides auto-listed), namespace overview,
    **Validating on all tiers from one source of truth** (walkthrough; two-runtimes mermaid; ship
    via Razor inline + REST endpoint; rule-as-function + JS call), **Validating against a model
    object** (uses the new `Parameters` registry — `MapReference(_ => true)` in an app-level factory;
    host side; the two-accessor / multi-form / object-access customizations).
  - **Remaining:** *Wiring into client frameworks* (stub — ASP.NET/jQuery UI/Angular/React/Vue);
    *Supported expression features* (consistent table layout #8b, demote extension warning #8c);
    *Dates and time zones* (light layout pass); *Extending* (full redesign #9: per-scenario
    method/property/constant with JS side + extension-contracts mermaid class diagram); *Unit-testing*
    (boolean-validation focus #7). Plus a final extension-contracts class diagram.
  - Docgen authoring gotchas captured in `DOCGEN_SKILL_BUGS.md` (Bugs 1–7): one-line `@brief`; BBCode
    not XML; no line-leading `- `/`* `; `&`→`&amp;` but raw `<`/`>`; `!`-prefix for indented
    `@example`; `@show=yes` to expand; mermaid via `@highlight=diagram`+`@show=yes`; `@width=100%`
    (with `%`) + `@col @width` 30/70; `[clink]` to **class** keys; **no `@see`** → use a "See also"
    `@list`. Standing rule: every emitted-JS example must be backed by a test.

**Still to do (after the articles):**

- Publish/host the generated docs; fold into the packaging refresh above.
- (Optional) Deep-dive articles — integration internals and the translator-pipeline model — were
  folded into the walkthrough/extending guides rather than written separately; revisit if a
  dedicated internals section is wanted.

---

## Strategic note

This library lives under the `Gehtsoft.Tools` tree, which is being frozen on net8.0 and superseded by
**Gehtsoft.Tools2**. Before investing heavily, decide whether ExpressionToJs is migrating into Tools2 —
if so, the architecture rework (dispatch registry, extensibility API, differential tests) is best done
as part of that move rather than twice.

## Recommended order

1. ✅ Fix the correctness bugs (§1).
2. ✅ Stand up the differential test harness (§2).
3. ✅ Refactor `AddCall` into a registry (§3).
4. Expand features (§4) — most done; §4 stragglers remain (`Average`, predicate-less
   `FirstOrDefault`/`LastOrDefault`, bit shifts, `TryParse`, string concat, `AddMonths`/`AddYears`).
5. Documentation (§6) — in progress: API XML comments + docgen + `index.ds` done; namespace overview,
   how-to and deep-dive articles still to write.
