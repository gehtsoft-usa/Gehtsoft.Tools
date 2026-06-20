# Gehtsoft.ExpressionToJs

Compile a C# `LambdaExpression` into an equivalent **JavaScript expression** — so one validation rule
runs on both the server (as a compiled delegate) and in the browser (as the emitted JS).

📖 **Full documentation:** https://docs.gehtsoftusa.com/Gehtsoft.Expression.ToJs/

## Why

Form validation is usually written twice: once in C# for the server, once in JavaScript for the
client. The two copies drift — a rule tightened on one side is forgotten on the other, and users hit
"valid here, invalid there." This package removes the second copy. You write the rule **once** as a
C# predicate; the server runs it directly, and its emitted JavaScript twin runs in the browser. The
translation preserves C# semantics (integer division, banker's rounding, typed `==`, date handling),
so a value that passes (or fails) on one side does the same on the other.

## Quick start

```csharp
using System.Linq.Expressions;
using Gehtsoft.ExpressionToJs;

Expression<Func<int, bool>> rule = x => x > 0 && x < 100;

// Server: run it like any delegate.
bool ok = rule.Compile()(42);                       // true

// Client: compile it to JavaScript.
string js = new ExpressionCompiler(rule).JavaScriptExpression;
// "((jsv_greater(x, 0)) && (jsv_less(x, 100)))"

// The runtime the emitted code calls (serve to the browser once).
string stub = ExpressionToJsStubAccessor.GetJsIncludesAsString();
```

The emitted code calls small `jsv_*` helpers defined in the stub; load the stub on the page once,
bind the variables the rule references, and evaluate the string.

## Use it on the client (ASP.NET)

The server already holds the compiler, so render the rule into the page as a named function and call
it from a validator. Load the stub once (e.g. in your layout); emit the rule where it is used:

```aspx
<%-- the jsv_* runtime, once per page --%>
<script src="/expr-stub.js"></script>

<asp:TextBox id="Age" runat="server" />
<asp:CustomValidator runat="server" ControlToValidate="Age"
     ClientValidationFunction="validateAge" ErrorMessage="Enter a value between 1 and 99." />

<script>
    // RuleJs (from code-behind) = new ExpressionCompiler(AgeRule).JavaScriptExpression
    function ageOk(x) { return <%= RuleJs %>; }
    function validateAge(sender, args) { args.IsValid = ageOk(args.Value); }
</script>
```

The same rule's compiled delegate validates on the server, so both tiers enforce it identically. The
function is just a boolean, so any client stack calls it the same way — jQuery Validate `addMethod`, an
Angular validator, a React/Vue check.

## Validate a whole model against a form

Enable the built-in `reference()` binding so model fields resolve against the page — no subclassing:

```csharp
Expression<Func<ChangePassword, bool>> rule = m => m.Password == m.Confirm;

var compiler = new ExpressionCompiler(rule);
compiler.Parameters.MapReference(_ => true);
string js = compiler.JavaScriptExpression;
// "jsv_equal(reference('Password'), reference('Confirm'))"
```

The page supplies `function reference(path) { ... }` (read the field by path), and the rule runs
unchanged.

## Dates that agree on both ends

```csharp
var compiler = new ExpressionCompiler(rule, DateTimeMode.Utc);   // or DateTimeMode.Local (default)
```

`DateTimeMode` selects how calendar constructs emit (`getUTCFullYear()` vs `getFullYear()`, …); bind
JS-side dates in the same frame. `DateTime.Now`/`Today`/`UtcNow` stay dynamic, evaluated at validation
time.

## Extend for your own types

A rule that uses a type the built-ins don't know throws at compile time — teach it (your registrations
run before the built-ins):

```csharp
compiler.Methods.MapMethod(typeof(Coupon), nameof(Coupon.IsValid), "jsv_coupon_ok($0)");
compiler.Members.MapMember(typeof(DateTime), nameof(DateTime.DayOfYear), "jsv_dayofyear($obj)");
compiler.Constants.MapConstant<Guid>(g => "'" + g + "'");
```

You define the matching `jsv_*` helpers in the JavaScript you ship; the library emits the call.

## Notes

- Configure the compiler (`DateMode`, `Methods`/`Constants`/`Members`/`Parameters`) **before** reading
  `JavaScriptExpression` — it is computed once and cached.
- Targets `netstandard2.0`. Licensed under Apache-2.0.
- Documentation: https://docs.gehtsoftusa.com/Gehtsoft.Expression.ToJs/
- Source: https://github.com/gehtsoft-usa/Gehtsoft.ExpressionToJs
