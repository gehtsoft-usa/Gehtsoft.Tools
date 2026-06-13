# Known bugs and limitations

## Gehtsoft.ExpressionToJs 0.2.1 — client/server semantic divergences (FIXED in 0.2.2)

Both issues below existed in `Gehtsoft.ExpressionToJs` **0.2.1** and are **fixed in 0.2.2**
(sources: `/mnt/d/develop/components/Gehtsoft.Tools/Gehtsoft.ExpressionToJs/`). Consumers must
upgrade to 0.2.2 to get the fixes. Anything still pinned to **≤ 0.2.1** must keep using the
documented workarounds — the JS-converter tests (`TestJsConvertor`) guard the affected rules
with conditions, and validators authored for client-side translation on the old package should
do the same.

Regression tests covering both fixes live in
`Gehtsoft.ExpressionToJs.Tests/CompilerTest.cs` (`TestNullSafetyAndShortCircuit`), executed
against the generated JS through Jint.

### 1. `jsv_match` is not null-safe — FIXED in 0.2.2

`DoesMatchPredicate.Validate(null)` returns `false` on the server (a null value does not
match), and `DoesNotMatchPredicate` correspondingly passes. The generated client script
`jsv_match(/…/, value)` calls `value.match(...)`, which throws a `TypeError` when the value
is null/undefined instead of returning false.

**Impact**: any unguarded `DoesMatch`/`DoesNotMatch`/`EmailAddress`/`NotSQLInjection`/
`NotHTML`/`PhoneNumber` rule on a nullable field crashes client-side validation where the
server politely fails (or passes) the rule.

**Workaround (≤ 0.2.1)**: guard such rules with `.UnlessValue(v => string.IsNullOrEmpty(v))`
(which is usually the intended semantics anyway).

**Fix (0.2.2)**: `jsv_match` in `stub.js` now returns `false` for null/undefined input.

### 2. `jsv_and` / `jsv_or` evaluate both operands eagerly — FIXED in 0.2.2

C# `&&`/`||` short-circuit; the translated `jsv_and(a, b)`/`jsv_or(a, b)` are plain JS
functions, so both arguments are evaluated before the call. An expression that relies on
short-circuiting to protect the second operand, e.g.

```csharp
RuleFor(e => e.Age).EntityMust(e => e.Scores == null || e.Age >= e.Scores[0]);
```

works on the server but throws on the client when `Scores` is null (`null[0]`).

**Workaround (≤ 0.2.1)**: move the guard into a rule condition
(`.WhenEntity(e => e.Scores != null)`), which is evaluated separately on both sides.

**Fix (0.2.2)**: `&&`/`||` are translated to native JS `&&`/`||` operators (which
short-circuit) instead of the `jsv_and`/`jsv_or` helper calls. This covers both `bool` and
nullable `bool` (`bool?`) operands.
