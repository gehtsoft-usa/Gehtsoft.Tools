---
name: gehtsoft-expressiontojs
description: |
  Compile a C# LambdaExpression into an equivalent JavaScript expression with Gehtsoft.ExpressionToJs,
  so one validation rule runs on both the server (compiled delegate) and the browser (emitted JS).
  Use when code references the Gehtsoft.ExpressionToJs namespace, ExpressionCompiler,
  ExpressionToJsStubAccessor, the Functions helpers (YearsSince, IsCreditCardNumberCorrect, ToBool,
  ...), DateTimeMode, or the IJsMethodRegistry/IJsConstantRegistry/IJsMemberRegistry/IJsParameterRegistry
  extension interfaces; or when emitting or hosting jsv_* helpers / stub.js.
packages: Gehtsoft.ExpressionToJs
---

# Gehtsoft.ExpressionToJs

Turns a C# `LambdaExpression` (a boolean validation predicate) into a JavaScript expression string,
so the **same rule** runs server-side (as a compiled delegate) and client-side (the emitted JS). One
source of truth, no drift.

## Core flow

```csharp
using Gehtsoft.ExpressionToJs;

Expression<Func<int, bool>> rule = x => x > 0 && x < 100;

bool ok = rule.Compile()(42);                              // server: run the delegate
string js = new ExpressionCompiler(rule).JavaScriptExpression;
// js == "((jsv_greater(x, 0)) && (jsv_less(x, 100)))"

string stub = ExpressionToJsStubAccessor.GetJsIncludesAsString();   // the jsv_* runtime
```

The emitted code is an **expression**, not a program. It calls `jsv_*` helpers that reproduce **C#
semantics** (integer division truncates, `int` wraps, `Math.Round` is banker's, `==` is strict/typed).
The host page must load the stub **once** before evaluating any rule.

## Rules that matter

- **Configure before reading `JavaScriptExpression`** — it is computed once and cached. Set
  `DateMode` and register translators first.
- The result is meant to be **boolean** (a validation verdict).
- `ExpressionCompiler.Equals` compares by the underlying lambda — usable as a cache key.

## What you can put in a rule

- **Operators:** `+ - * / %`, unary `-`, comparisons, `&& || !`, `^` (logical XOR on bool, bitwise on
  int), `& |`, `?:`, `??`, array indexing, `.Length`/`.Count`.
- **Math:** `Math.Round` (banker's), `Floor`, `Ceiling`, `Truncate`, `Abs`, `Sign`, `Sqrt`, `Pow`,
  `Log`, `Exp`, `Min`, `Max`, trig, `Math.PI`.
- **Strings/chars:** `Trim*`, `ToUpper/ToLower` (+`Invariant`), `StartsWith`/`EndsWith` (+`StringComparison`),
  `Contains`, `IndexOf`/`LastIndexOf`, `Replace`, `PadLeft/PadRight`, indexer, `ToString`;
  `string.IsNullOrEmpty`/`IsNullOrWhiteSpace`; `char.IsUpper/IsLower/IsDigit/IsLetter/...`; `Regex.IsMatch`.
- **LINQ** (chainable): `Any`, `All`, `Count`, `First`, `Last`, `FirstOrDefault`/`LastOrDefault` (with
  predicate), `Where`, `Select`, `Sum`, `Min`, `Max`, `Distinct`, `Contains`.
- **Dates:** component reads, `AddDays/Hours/Minutes/Seconds`, ±`TimeSpan`, date subtraction,
  `TimeSpan.Total*` — plus the `Functions.*` helpers below.

Anything the built-ins don't know **throws** when you read `JavaScriptExpression` — extend it (below)
rather than getting wrong JS.

## Delivering a rule to the page

The emitted text is a **bare expression**, not callable on its own — wrap it in a named function:
`function ageOk(x) { return <emitted js>; }`. Deliver it by **rendering inline** (a server page, e.g.
`@Html.Raw(new ExpressionCompiler(rule).JavaScriptExpression)`) or by **serving it from an endpoint**
referenced with `<script src>`. Load the stub **once** per page first. Since the function is just a
boolean, any framework calls it from its validation hook — ASP.NET `CustomValidator`, jQuery Validate
`addMethod`, an Angular validator, a React/Vue check.

## Form-model binding (no subclassing)

By default a parameter emits verbatim (`p.Age`). To resolve model fields against the page via a
host-provided `reference()` lookup, enable it on the `Parameters` registry — typically once,
app-wide:

```csharp
var compiler = new ExpressionCompiler(rule);
compiler.Parameters.MapReference(_ => true);   // off by default
// p => p.Address.PostalCode.Length == 5  ->  jsv_equal(jsv_length(reference('Address.PostalCode')), 5)
```

The page supplies `function reference(path) { ... }`. For other shapes use
`compiler.Parameters.Map(matches, parameter, access)` with the public
`ExpressionCompiler.ParameterAccessPath(expr)` helper (e.g. `value.Field`, or
`reference('Type','path')` when several forms share a page).

## Dates (DateTimeMode)

`compiler.DateMode` = `DateTimeMode.Local` (default) or `Utc` selects how calendar constructs emit
(`getFullYear()` vs `getUTCFullYear()`, etc.). **Contract: bind JS-side dates in the same frame.** A
mismatch silently shifts every comparison by the client's offset. `DateTime.Now`/`Today`/`UtcNow` stay
dynamic (`new Date()` / `jsv_today(...)`), evaluated at validation time. Epoch ops (±TimeSpan, AddDays,
date subtraction, `.Total*`, `Functions.DaysSince`) are frame-independent.

## Functions parity helpers

Operations with no BCL⇄JS equivalent ship as matched pairs — call `Functions.X` in the rule and the
emitted JS calls the identical `jsv_x`: `DaysSince`, `MonthsSince`, `YearsSince`,
`IsCreditCardNumberCorrect` (Luhn), `ToBool`, `ToInt`, `Fractional`, `IsNull`, `IsNotNull`,
`IsNullOrEmpty`, `IsNotNullOrEmpty`. Do not invent your own helper names without defining the JS twin.

## Extending for your own types

A rule that uses a type the built-ins don't know **throws at compile time** (read of
`JavaScriptExpression`). Teach it — registrations run before built-ins, so they can also shadow:

```csharp
compiler.Methods.MapMethod(typeof(Coupon), nameof(Coupon.IsValid), "jsv_coupon_ok($0)"); // $0,$1=args; $obj=instance
compiler.Members.MapMember(typeof(DateTime), nameof(DateTime.DayOfYear), "jsv_dayofyear($obj)"); // $obj=target
compiler.Constants.MapConstant<Guid>(g => "'" + g + "'");
```

- **You own the JS side.** Whatever you emit (`jsv_coupon_ok`, `jsv_dayofyear`) you must define on the
  page. The library only emits text + ships its own stub.
- When the output must vary on an argument's value/type/arity, implement `IMethodCallTranslator` /
  `IConstantTranslator` / `IMemberTranslator` and `AddTranslator(...)`; each gets the node + an
  `IExpressionEmitContext` (`Emit` to recurse, escape helpers) and returns whether it handled it.
- **Enums** emit their numeric value (flags combine); client fields must carry the number. For names,
  `MapConstant<TEnum>(e => "'" + e + "'")` — but only where the enum stays enum-typed (e.g. a method
  argument); a direct `==` comparison is lifted to `int`, so the map won't fire there.

## Not supported (throw at compile time)

String `+` concat / `string.Format` / `string.Split`; `DateTime.AddMonths`/`AddYears`/`Parse`,
`TimeSpan.FromX`, `DateTimeOffset`; bit shifts, `int.TryParse`/`double.TryParse`, LINQ `Average`,
predicate-less `FirstOrDefault`/`LastOrDefault`; `long` beyond 2^53 (JS precision); statement-bodied
lambdas. Most gaps are closable with a custom translator.

## Testing for parity (differential, with Jint)

```csharp
static void AssertRule<T>(Expression<Func<T, bool>> rule, T input)
{
    bool expected = rule.Compile()(input);
    string js = new ExpressionCompiler(rule).JavaScriptExpression;
    var engine = new Jint.Engine();
    engine.Execute(ExpressionToJsStubAccessor.GetJsIncludesAsString());
    engine.SetValue(rule.Parameters[0].Name, input);
    Assert.Equal(expected, engine.Evaluate(js).AsBoolean());
}
```

Aim inputs at the divergences that flip a verdict: integer division (`11/2==5`), `Math.Round`
midpoints, `0.1+0.2==0.3`, null/empty, dates near midnight in your `DateTimeMode`. Bind date inputs in
the same frame as `DateMode`.
