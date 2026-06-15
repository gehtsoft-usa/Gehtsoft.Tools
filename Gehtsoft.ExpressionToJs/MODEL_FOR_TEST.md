# MODEL_FOR_TEST.md

The testing model for Gehtsoft.ExpressionToJs — how we prove that a C# `LambdaExpression`
and the JavaScript string it compiles to mean the **same thing**.

## Why this shape of test

The whole point of the library is **one source of truth** for validation: a predicate is
written once in C#, runs server-side as a compiled delegate, and runs client-side as emitted
JS. A test that only checks "the emitted JS string looks right" proves nothing about behavior —
the two sides can drift on integer division, rounding, loose equality, null handling, dates and
time zones, enum/`decimal` coercion, and so on. So the tests are **differential**: every case is
executed on both sides and compared.

The single guiding rule:

> A test compiles a C# lambda, runs the compiled C# delegate, evaluates the emitted JS in an
> in-process JS engine (Jint) with `stub.js` loaded, and asserts the two results agree **and**
> match an independent expected value.

This catches three classes of bug at once:
1. the C# expectation is wrong (anchor check),
2. the emitted JS is wrong (parity check), and
3. both happen to be wrong the same way (still caught by the independent expected anchor).

## The three-way assertion

Every differential case is checked three ways (see `Complete/DifferentialTestBase.cs`):

1. **C# vs expected** — the compiled delegate must produce the expected value. Proves the
   operation itself is correct.
2. **JS vs expected** — the emitted JS, evaluated in Jint with the stub loaded, must produce the
   expected value.
3. **C# vs JS** — the two must agree directly, so a *shared* wrong answer is still caught against
   the expected anchor and any divergence is reported explicitly.

`AssertValue` normalizes across the CLR/JS type gap: numbers compared with a relative tolerance
(binary-double drift), booleans via `Convert.ToBoolean`, everything else by string form, with
`null` handled explicitly.

## Two layers of tests

### Layer 1 — low-level differential suite (`Complete/`)

Primitive-by-primitive coverage of the contract, organized by area: operators, math, strings,
LINQ, enums, XOR, constant folding, the `Methods`/`Constants`/`Members` registries, `DateTimeMode`,
and `Functions` ↔ `jsv_*` parity. `DifferentialTestBase` provides `AssertUnary`/`AssertBinary`,
which bind the lambda parameters into Jint by name and run the three-way check. This layer guards
the **server/client parity that is maintained by hand** — every `Functions.X` must have a
behaviorally identical `jsv_x` in `stub.js`.

### Layer 2 — realistic entity-validation scenario (`Complete/ProfileValidationScenarioTests.cs`)

This layer tests the library the way the **primary consumer** (Gehtsoft.EF.Toolbox) uses it: a
complex entity with meaningful, domain-shaped validation rules, each compiled through the
form-validation compiler and run on both sides. It is the model documented in detail below.

## The realistic-validation model

### 1. A complex entity with every data category

`UserProfile` deliberately spans all the kinds of data a real form carries, including **folded
(nested) entities**:

| Category            | Members                                                        |
|---------------------|----------------------------------------------------------------|
| strings             | `FirstName`, `LastName`, `Email`, `Phone`, `Bio`               |
| dates               | `BirthDate`, `RegisteredAt`, `DateTime? LastLoginAt`           |
| integers / nullable | `int LoginCount`, `int? ReferrerId`                            |
| floating / money    | `double AccountBalance`, `decimal CreditLimit`                 |
| booleans            | `AcceptedTerms`, `IsActive`                                    |
| enum                | `AccountTier { Free, Standard, Premium }`                      |
| collections         | `string[] Roles`, `int[] FavoriteNumbers`                      |
| folded entities     | `Address { Street, City, Country, PostalCode }`, `CreditCard { Number, HolderName, Expiration }` |

A `ValidProfile()` factory returns one instance that satisfies every rule. Each test starts from
it and mutates exactly one thing to drive a rule false — so the "valid" and "invalid" verdicts are
both exercised, and the failing field is obvious.

### 2. Rules are real predicates, not contrived ones

Examples (all are `Expression<Func<UserProfile, bool>>`):

- **Age ≥ 21** from the birthdate: `Functions.YearsSince(DateTime.Today, p.BirthDate) >= 21`
  (uses the dynamic "today", so it is *not* constant-folded — it evaluates at validation time on
  both sides).
- **Email / postal code** regex match; **registration not in the future** (`p.RegisteredAt <= DateTime.Now`).
- **Optional-but-constrained** patterns over nullables: `!p.ReferrerId.HasValue || p.ReferrerId.Value > 0`,
  `!p.LastLoginAt.HasValue || p.LastLoginAt.Value >= p.RegisteredAt`.
- **Cross-field** rules: active accounts must have accepted terms; premium tier requires a card;
  a full composite rule — *a premium account must carry a valid, unexpired card with a named holder*.
- **Folded-entity access**: `Functions.IsCreditCardNumberCorrect(p.Card.Number)`,
  `Regex.IsMatch(p.Address.PostalCode, @"^\d{5}$")`, `p.Card.Expiration >= DateTime.Today`.
- **Collections / LINQ**: `p.Roles.All(r => !string.IsNullOrWhiteSpace(r))`, `p.Roles.Contains("user")`,
  `p.FavoriteNumbers.All(n => n > 0)`.

### 3. Compiled the way the consumer compiles

Rules are compiled with `ValidationExpressionCompiler`, which maps the lambda's parameters onto
the bindings the host page provides:

- the **entity** parameter (and any member chain rooted in it) → `reference('Member.Sub')`;
- the **value** parameter (a single property under test) → the ambient `value`;
- parameters introduced by a LINQ lambda (`r => ...`) → their own names, as the base compiler emits;
- array indexing inside a parameter chain → `jsv_index(...)`.

This sample compiler mirrors the real consumer's
`Gehtsoft.Validator.JSConvertor.ValidationExpressionCompiler` in Gehtsoft.EF.Toolbox.

### 4. Evaluated against the consumer's browser runtime

The JS side is run in Jint with `stub.js` loaded **plus** the small `reference()` / `value` /
`index` runtime that the consumer binds in the browser (mirrored from the Toolbox's
`JsRuleExecutor`). The CLR model is bound as `__model`; `reference('Card.Number')` walks it the
same way the client does.

Two rule shapes, two helpers:

- **Entity rule** — `AssertEntityRule(p => condition(p), model, expected)`: parameter is the whole
  entity; member access becomes `reference('...')`.
- **Value rule** — `AssertValueRule(targetPath, v => condition(v), selector, model, expected)`:
  parameter is one property's value; the harness sets `value = reference('targetPath')` before
  evaluating, exactly as the host does per field.

Both run the same three-way (C# / JS / each-other) assertion as Layer 1.

## CLR ↔ JS interop facts this model relies on

Verified green against Jint + `stub.js`:

- **CLR arrays *and* `List<T>` expose `.length`** to Jint (verified empirically against Jint
  4.9.3), so `jsv_length` / `jsv_all` / `jsv_any` / `jsv_contains` work over either. The realistic
  entity still models client-validated collections as **arrays**, because in the real browser
  use-case the model arrives as JSON arrays and the stub's `jsv_length` keys off `.length` — that
  is the production shape, not a Jint limitation. (Don't infer CLR-to-JS binding from what a
  consumer happened to use — probe it with a throwaway test.)
- **Enums coerce to their underlying numeric value**, so `jsv_equal(reference('Tier'), 2)` matches
  the C# `==`.
- **`decimal` coerces to a JS number**, so range comparisons agree within the numeric tolerance.
- **Dates**: a bound CLR `DateTime` becomes a JS `Date`; calendar reads follow `DateMode`
  (local by default). Choose test dates away from midnight / year boundaries so local/UTC framing
  cannot flip a result. Epoch-based date math (`AddDays`, subtraction, `Total*`, `DaysSince`) is
  frame-independent.

## How to add a new validation test

1. Add or reuse a property on `UserProfile` of the right type (arrays, not `List<T>`, for anything
   validated on the client).
2. Write the rule as a normal C# `Expression<Func<UserProfile, bool>>` (or `Func<TValue, bool>` for
   a value rule) — the way a developer would actually write the validation.
3. Assert the happy path from `ValidProfile()`, then mutate one field per case to drive it false.
4. Call `AssertEntityRule` (or `AssertValueRule`) — never assert only the C# side or only the JS
   string. The three-way check is the test.
5. If the construct is genuinely new (no `jsv_*` equivalent), that is a library change, not just a
   test: add the emit logic, the `jsv_*` helper in `stub.js`, and — if it has no BCL form — a
   `Functions.X` method, then cover it in Layer 1 *and* here.

## Running

```bash
dotnet build Gehtsoft.ExpressionToJs.sln
dotnet test  Gehtsoft.ExpressionToJs.sln
# just the realistic scenario:
dotnet test  Gehtsoft.ExpressionToJs.sln --filter "FullyQualifiedName~ProfileValidationScenarioTests"
```
