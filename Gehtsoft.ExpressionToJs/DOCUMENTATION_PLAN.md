# Documentation Plan — Gehtsoft.ExpressionToJs

Living plan for the docgen documentation effort (PLAN.md §6). Page-by-page; the user validates
**each page** before moving on. Last updated 2026-06-19 (end of session: index, namespace,
walkthrough, model-rules done; core parameter-registry refactor shipped).

## Where things live / how to build

- Landing / root group: `help/src/index.ds` — `@group @key=main`, `@sortarticles=no`.
- Namespace overview: `help/src/ns/Gehtsoft.ExpressionToJs.ds` (merges with auto-generated group).
- **All articles in one file**: `help/src/articles.ds` — each an `@article @ingroup=main`, in
  reading order (sorting disabled, so file order = TOC order). Referenced once from `project.xml`
  as `<dg:file name="src/articles.ds">`.
- Auto-generated API: `help/src/raw/*.ds` (from `prepare`).
- Build: `cd help && dotnet build project.proj /t:MakeDoc`. Rendered HTML: `dst/<key>.html`;
  landing page `dst/main.html`; TOC `dst/web-hhc.html`. Refresh API from code: `prepare` (Scan+Prepare).

## docgen authoring conventions (full detail in DOCGEN_SKILL_BUGS.md, Bugs 1–5)

- `@brief` one line. Use BBCode `[c]`/`[i]`/`[b]`, not XML tags. Never start a line with `- ` / `* `.
- Write `&` as `&amp;` in `.ds` (a raw `&`/`&&` corrupts); but `<` and `>` stay **raw**.
- `@example` trims each line's leading whitespace → for indented code use the `!` line-prefix
  (`       !<code>`); everything after `!` is literal. `@show=yes` expands the example by default.
- Mermaid: `@highlight=diagram` + `@show=yes` (arrows render even though the HTML shows `--&gt;`,
  because mermaid reads decoded `textContent`).
- Full-width table = `@width=100%` (with the `%`; bare `100` = 100 pixels). Column proportions:
  `@col` + `@width=30%` / `@width=70%` on the **header row** once.
- Cross-references: `[clink=<Type key>]` to **class** keys; member-level keys carry unstable CRC
  suffixes, so don't link them — link the class and name the member in the link text.
- **Don't use `@see`** — it renders as a 2-column table with empty descriptions (ugly). Instead end
  an article with a `@headline ... See also` + a `@list` of `[link=key]Title[/link]` items.

## Page-by-page queue (reading order) and status

1. **index.ds** — DONE. "API at a glance" → 30/70 table (#1). Guides table removed — docgen
   auto-lists the articles below (#4). One-line lead-in kept.
2. **Namespace page** — DONE. Brief = "common namespace for all the library's classes" (#2);
   entry points → 30/70 table (#1); extension described by a pointer to the Extending article (#3).
3. **Validating on all tiers from one source of truth** (`guides.validation-walkthrough`) — DONE.
   Title carries both ideas (#6a). Two-runtimes mermaid flowchart. Ship-the-stub: inline Razor
   (#6b1) + static-script/REST endpoint (#6b2). Step 5: rule wrapped as a named function from an
   endpoint + the JS call (#6b/the "weird endpoint" fix). Vague model-binding section replaced by a
   pointer (#6d).
4. **Validating against a model object** (`guides.model-rules`) — DONE (a bit bumpy; could use one
   more polish pass on prose flow). Motivated by cross-field rules (change-password: every check is
   single-field except "password must equal confirmation"). Uses the shipped registry, **no
   subclassing**: enable the built-in `reference()` convention once in an app-level `CompileRule`
   factory via `Parameters.MapReference(_ => true)` (it's off by default → verbatim). Host side:
   endpoint serves `function name(){...}`, page implements `reference(path)` and calls it.
   "Referring to the model another way" covers the two-accessor rationale (custom whole-entity
   method → `reference()` for the whole model, `reference('path')` for a field), multi-form
   (`reference('Type','path')` using `p.Type.Name`), and object access (`value.Password`), all via
   `Parameters.Map(...)` + `ExpressionCompiler.ParameterAccessPath`. Every emitted-JS example is
   backed by a test in `ParameterRegistryTests`.
5. **Wiring into client frameworks** (`guides.client-frameworks`) — STUB. TODO (#6c, dedicated
   article by decision): attach an emitted rule on ASP.NET (forms), jQuery UI, Angular, React, Vue.
6. **Supported expression features** (`guides.supported-features`) — PARTIAL. #8a fixed (tables now
   `@width=100%`). TODO: #8b make the layout **consistent** (stop mixing `@table` and `@list`;
   settle on tables) so "what's supported" is easy to scan; #8c demote the extension `@note
   type=warning` to plain text.
7. **Dates and time zones** (`guides.dates`) — TODO. Light layout-consistency pass; **keep** the
   genuine frame-mismatch `@note type=warning` (real footgun).
8. **Extending the translator for your own types** (`guides.extending`) — TODO, full redesign (#9).
   Organize around three concrete scenarios, each a complete worked example **including the
   JavaScript side** where needed: (a) add **method** support for a type, (b) add **property-read**
   support for a type, (c) add a **constant** for a type. Lead each with *when/why*, then *how*; do
   not front-load the template-token reference. Add the extension-contracts **mermaid class
   diagram** (ExpressionCompiler → Methods/Constants/Members registries → IMethodCallTranslator /
   IConstantTranslator / IMemberTranslator → IExpressionEmitContext). Demote the "your mapping, your
   runtime" warning panel to plain text (#8c). After the core refactor, also mention the new
   parameter-type registry here (or cross-link from model-rules).
9. **Unit-testing your expressions** (`guides.unit-testing`) — TODO (#7). Refocus entirely on
   **boolean validation** rules (target type is always `bool`). Replace the generic
   `AssertParity<TArg,TResult>` helper with a boolean-rule helper. Other return types are out of
   scope (discuss separately later).

## Core parameter-registry refactor — DONE (shipped 2026-06-19)

Made the model→`reference()` binding a **built-in, registerable** feature so consumers don't
subclass. Shipped API (in `ValueTranslators.cs` + `ExpressionWalker.cs`), tests in
`Complete/ParameterRegistryTests.cs` (full suite 368 green):

- `compiler.Parameters` → `IJsParameterRegistry` (parallel to `Methods`/`Constants`/`Members`):
  - `MapReference(Func<Type,bool> matches)` — turns on the built-in `reference()` rendering (bare →
    `reference()`, member chain → `reference('Path')`, array index → `jsv_index(...)`).
  - `Map(Func<Type,bool> matches, Func<ParameterExpression,string> parameter, Func<Expression,ParameterExpression,string> parameterAccess)`
    — full control. `parameter` renders the whole model; `parameterAccess` gets the access
    expression + the root `ParameterExpression` (use it for `.Type.Name`, etc.).
- `public static string ExpressionCompiler.ParameterAccessPath(Expression)` — turns a member chain
  into a dotted path (`m.Address.PostalCode` → `Address.PostalCode`); reuse it inside custom `Map`
  functions so you don't walk the expression yourself.
- **Matches by `Func<Type,bool>`**, not exact type — `_ => true` is the norm (a validation rule's
  one parameter is the model). Bindings apply **only to the rule's root parameters**, so nested LINQ
  lambda params keep their names automatically (no `InLambdaParameter` flag needed).
- **Off by default** — with nothing registered the compiler emits verbatim (`m.Password`), unchanged
  behavior. Built-in member translators (DateTime.Year) and registered ones (MapMember) still run
  *before* the parameter-access fallback, so they compose on top of `reference(...)`.
- Decisions settled with the user: predicate not exact-type; `ParameterExpression` (richer than
  `Type`) in the funcs; the path helper is `public static` (not protected); `value` is
  object-access (`value.Property`) done via `Map`, not a separate "single-field" mode; native `&&`
  is correct (`jsv_and` is back-compat only).

**Downstream (not done):** refactor Gehtsoft.EF.Toolbox `ValidationExpressionCompiler` onto this —
see `REFERENCE_REFACTOR.md`. Optional: a short mention of `Parameters` on the namespace page / in
the Extending article (page 8).

## Decisions already made

- Single `articles.ds` (not a folder); articles in `main` (no `guides` group); `@sortarticles=no`.
- `client-frameworks` is a **dedicated** article; `model-rules` is a **separate** article.
- Deep-dive/internals content folded into the guides (no separate internals section unless asked).

## Review feedback → status map

- #1 list-like content as tables (docgen style) — applied to index + namespace + model-rules; apply to others as we go.
- #2 namespace brief not echoing library brief — DONE. #3 namespace refers extension to article — DONE.
- #4 articles in `main`, no guides group — DONE. #5 ordering via single file + `@sortarticles=no` — DONE.
- #6a title — DONE. #6b ship examples (Razor + REST/script tag) — DONE. #6c platform article — page 5.
  #6d model-rules split + rewrite — DONE (page 4).
- #7 unit-testing boolean-only — page 9. #8a table width — DONE. #8b consistent layout — page 6.
  #8c warning→text — pages 6 & 8. #9 Extending redesign + scenarios + JS side — page 8.
- Late asks (DONE): `@see` → "See also" `@list` in **every** article; 30/70 columns via `@col @width`;
  model-rules reworked several times (subclass → registry → "enable default" framing) per live feedback.

## Standing rule

- **Every emitted-JS example in an article must be backed by a test** asserting that exact output
  (see the memory note). Caught a real bug this session (`&&` is native, not `jsv_and`).

## Diagrams

- Two-runtimes flowchart — DONE (walkthrough).
- Extension-contracts class diagram — TODO (Extending, page 8).
