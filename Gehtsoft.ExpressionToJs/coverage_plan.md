# Coverage Improvement Plan — Gehtsoft.ExpressionToJs

## Executive summary

| Metric | Value |
|---|---|
| Line coverage | **81.06%** (702 covered / 110 not / 54 partial) |
| Block coverage | 83.48% |
| Functions | 95 total — 49 fully covered, 41 partial, 5 uncovered |
| Target | 90% line coverage |
| Lines to cover to hit target | **~77** |

Coverage was collected against `Gehtsoft.ExpressionToJs.sln` (313 tests, all passing). Only the
production module `Gehtsoft.ExpressionToJs.dll` is analyzed; the test assembly is excluded.

The headline: the **library is solid where the differential suite hits it** (operators, math,
core strings, the registries' `Map*` paths, `DateTimeMode`, `Functions`↔`jsv_*` parity). The gaps
are concentrated in **translator branches that no test expression reaches yet** — and because this
library's whole value proposition is C#/JS *behavioral parity*, an untested emit branch is exactly
where a silent client/server divergence can hide. Every suggestion below is a **differential test**
(run the C# delegate, run the emitted JS in Jint, assert both agree), matching the model in
`MODEL_FOR_TEST.md`.

---

## Functional-area gaps (ranked by risk × size)

### 1. `.ToString()` translation — **0% covered**, the only entirely-untested feature
**Where:** `ToStringTranslator.TryTranslate` (MethodTranslators.cs 433–439).
**What's at risk:** any validation rule that stringifies a value — `p.Id.ToString() == "5"`,
`p.Code.ToString().StartsWith("A")`, numeric-to-string formatting — emits `jsv_tostring(...)` that
has never been checked against C#'s `ToString()`. Number/bool/date stringification differs sharply
between the CLR and JS (e.g. `True` vs `true`, date formats), so this is a real divergence surface.
**Suggested test scenarios** (differential):
1. `p => p.LoginCount.ToString() == "42"` — integer stringification.
2. `p => p.AccountBalance.ToString().Length > 0` — double stringification (document the format
   difference if one exists; the test pins current behavior).
3. `p => p.IsActive.ToString() == "true"` / boolean — verify the C#/JS casing contract.
**Expected gain:** ~7 lines.

### 2. LINQ aggregates / dedup / defaults — **60% covered**
**Where:** `LinqTranslator.TryTranslate` (MethodTranslators.cs 377–409).
**Untested branches:** selector-less `Count()`/`Sum()`/`Min()`/`Max()`, `Distinct()`, `Empty()`,
`First(predicate)`/`Last(predicate)`, and `FirstOrDefault/LastOrDefault(predicate, fallback)`.
**What's at risk:** aggregate validation rules over collections — "sum of line items ≤ limit",
"distinct tags only", "at least one matching element". These map to `jsv_sum/min/max/count/
distinct/first/last`, unproven end-to-end.
**Suggested test scenarios** (over an `int[]` property, since arrays bind to Jint with `.length`):
1. `p => p.FavoriteNumbers.Sum() <= 100` and `... .Count() == 3` (selector-less aggregates).
2. `p => p.FavoriteNumbers.Min() > 0 && p.FavoriteNumbers.Max() < 50`.
3. `p => p.FavoriteNumbers.Distinct().Count() == p.FavoriteNumbers.Length` (no duplicates).
4. `p => p.FavoriteNumbers.First(n => n > 5) == 7` (predicate First/Last).
**Expected gain:** ~14 lines.

### 3. Case-insensitive string ops & instance/option Regex — **partial (57–67%)**
**Where:** `StringStartsWithComparisonTranslator` (250–255), `StringEndsWithComparisonTranslator`
(301–311), `StringIndexOfTranslator.IsIgnoreCase` (233–237), `RegexIsMatchTranslator` (158–184,
the `RegexOptions` overload and the **instance** `new Regex(...).IsMatch(s)` form).
**What's at risk:** case-insensitive validation is extremely common (emails, country/currency
codes, usernames). The `StringComparison.OrdinalIgnoreCase` paths emit `jsv_upper(...)` wrappers and
the regex paths emit `/.../i`; none are verified against C#.
**Suggested test scenarios** (differential):
1. `p => p.Email.EndsWith(".COM", StringComparison.OrdinalIgnoreCase)` and the `StartsWith` twin.
2. `p => p.FirstName.IndexOf("ada", StringComparison.OrdinalIgnoreCase) >= 0`.
3. `p => Regex.IsMatch(p.Address.Country, "gb", RegexOptions.IgnoreCase)` (options overload).
4. A precompiled `private static readonly Regex` field used as `re.IsMatch(p.Email)` (instance form).
**Expected gain:** ~9 lines + several partial lines closed.

### 4. String → primitive conversions (`AddConvert`) — uncovered cast paths
**Where:** `ExpressionCompiler.AddConvert` (764–772): `(bool)`, `(int)`, `(double)`, `(float)` from
a string emit `jsv_string2bool/int/n`.
**What's at risk:** coercion rules like `(int)p.Code > 100` or `Convert.ToInt32(p.Code)`-style
predicates where a string field is parsed before comparison; divergent parse/NaN behavior.
**Suggested scenarios:** rules that cast a string property to `int`/`double`/`bool` and compare,
e.g. add a `string Code` field and test `p => (int)p.Code == 50` over `"50"`.
**Expected gain:** ~5 lines.

### 5. Collection `Contains` (instance form) & array unwrap — **50% covered**
**Where:** `CollectionContainsTranslator.TryTranslate` (329–339) and `UnwrapCollection` (345–350).
**What's at risk:** the instance `List<T>.Contains` form, the static `Enumerable.Contains(source,
value)` form, and array-arrives-as-span unwrapping.
**Suggested scenario:** a `List<int>.Contains(3)` differential test (Jint exposes `.length` on a
bound CLR list, so it runs), plus an `IEnumerable<int>` source to force the static `Enumerable.
Contains` branch through `UnwrapCollection`.
**Expected gain:** ~6 lines. *(Done — see `TranslatorBranchCoverageTests`; the residual two lines
are the array→span `Convert`/`op_Implicit` unwrap, which idiomatic C# does not produce.)*

### 6. String / regex literal escaping — untested escaping branches
**Where:** `EscapeJsString` (337–342: `\t \b \f` and the `< 0x20` `\uXXXX` path) and
`EscapeJsRegex` (361–364: `\n`/`\r` inside patterns).
**What's at risk:** **emitted-JS correctness/safety.** A constant string or regex containing quotes,
backslashes, slashes, or control characters must be escaped so the generated JS is valid (and not
injectable). Untested control-char and regex-slash handling is a latent "generated script breaks /
is exploitable" bug.
**Suggested scenarios:** constant-comparison rules with awkward literals —
`p => p.Bio == "line1\nline2\t\"q\""`, `p => Regex.IsMatch(p.Email, @"a/b\n")` — compile and assert
the emitted JS evaluates without error and matches C#.
**Expected gain:** ~6 lines (and high safety value).

---

## Quick wins (1–7 lines each, mostly mechanical)

- **Numeric/`TimeSpan` constant literals** — `TryEmitBuiltinConstant` arms for `long`, `short`,
  `float`, `TimeSpan`, and the static-shim `DateTime` path (ExpressionWalker 259–285) are unhit.
  Differential rules using `long`/`short`/`float`/`TimeSpan` constants close these. *(~7 lines)*
- **Base-compiler parameter array/indexer access** — `IsExpressionRootsInParameter` (486–492) and
  base `AddParameterAccess` (508–516) cover `arr[i]` / `list[i]` rooted directly in a parameter
  under the **plain** `ExpressionCompiler`. A single `p => p.FavoriteNumbers[0] > 0` differential
  test with the base compiler hits both. *(~12 lines)*
- **`Functions.ToBool` / `ToInt` error paths** — the `ArgumentException` throws (Functions.cs 89,
  105) are unhit; add `Assert.Throws` for a wrong-typed argument. *(2 lines)*
- **Registry `AddTranslator`** — `Constants.AddTranslator` (610–613) and `Members.AddTranslator`
  (630–633) are untested though `MapConstant`/`MapMember` are. Register a custom `IConstantTranslator`
  / `IMemberTranslator` and assert it wins. *(~8 lines)*
- **`DateTimeMemberTranslator` Hour/Minute/Second** (ValueTranslators 88–93) — add a rule reading
  `.Hour`/`.Minute`/`.Second` in both Local and UTC `DateMode`. *(~3 lines)*

---

## Per-module status vs. 90% target

| Module | Line % | Status |
|---|---|---|
| Gehtsoft.ExpressionToJs.dll | 81.06% | **below target** — ~77 lines to reach 90% |

Implementing areas **1–4** plus the quick wins comfortably clears the 90% line and, more
importantly, closes the highest-risk parity branches (`.ToString()`, LINQ aggregates, case-insensitive
ops, literal escaping) where a silent C#/JS divergence would otherwise be invisible.

---

## Notes on what was *intentionally* excluded

Mechanical members (accessors, ctors, `Dispose`/`Equals`/`GetHashCode`, operators) and
compiler-generated closure/state-machine types are excluded by default — they are covered
implicitly when the real scenarios run. The 5 fully-uncovered functions are the genuine
feature gaps catalogued above, not plumbing.
```bash
# reproduce
dotnet-coverage collect "dotnet test Gehtsoft.ExpressionToJs.sln" -f xml -o coverage.xml
python3 <skill>/scripts/coverage_report.py coverage.xml --mode gaps --group-by type --top 15
```
