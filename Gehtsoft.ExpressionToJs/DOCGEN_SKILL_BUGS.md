# docgen-skill bugs / improvement backlog

Issues found while using the `docgen-skill` on this project. Each entry is something the skill
*could* have prevented or automated. Fix in the skill later.

---

## Bug 1 — Skill doesn't teach the C# `<summary>` → `@brief` one-line rule

### What happened

We wrote rich, multi-line XML `<summary>` comments on the public API (use-focused prose, several
sentences, inline `<c>` code spans, `<see cref>` links) and moved long-form detail into `<remarks>`.
After running the C# prepare step (`Asm2Xml` + `cs2ds`), the generated `src/raw/*.ds` showed two
problems:

1. **The entire `<summary>` was dumped into `@brief`.** `@brief` is supposed to be a single line
   (it's used in indexes, tooltips, member lists). A 4–5 line summary produced a 4–5 line `@brief`,
   which is noise everywhere `@brief` is shown.

2. **`<remarks>` was silently dropped.** cs2ds does not emit `<remarks>` anywhere in the `.ds`
   output. All the detail we parked in `<remarks>` simply vanished from the docs — no warning.

3. **Inline markup in the first line truncates the brief.** A `<c>...</c>` or `<see cref>` in the
   *first* paragraph of the summary gets pulled onto its own `[c]...[/c]` / `[clink]` line, so the
   `@brief` ends abruptly at the markup (e.g. `@brief=Returns the full ` with the rest spilled
   below). A `<see cref>` to a type outside the doc model (e.g. `System.Linq.Expressions.LambdaExpression`,
   `bool`) renders as **empty**, so the brief loses words with no indication why.

### The actual convention (confirmed empirically on this repo)

- cs2ds maps the **whole `<summary>`** to `@brief`.
- Paragraphs inside `<summary>` are separated by a **blank `///` line**. The **first paragraph**
  becomes the one-line `@brief`; **subsequent paragraphs** become the detailed description body.
- So the correct authoring pattern is:

  ```csharp
  /// <summary>
  /// One short line — becomes @brief.
  ///
  /// First detail paragraph. May use &lt;c&gt;...&lt;/c&gt; and &lt;see cref&gt; freely.
  ///
  /// Second detail paragraph.
  /// </summary>
  ```

- Keep the **first line plain text** — no `<c>`, no `<see cref>` (especially not to external/BCL
  types, which render empty). Put markup and cross-references in the detail paragraphs only.
- **Do not use `<remarks>`** for docgen targets — it is discarded. Fold that content into summary
  paragraphs.

(Reference example from the Gehtsoft.EF package, which follows this convention:)

```csharp
/// <summary>
/// The database query.
///
/// Use <see cref="SqlDbConnection"/> to create a query.
///
/// Do not forget to dispose the query after use. Some DBs require the previous
/// query to be disposed before the next query is executed.
/// </summary>
```

### What the skill should do (automatable)

1. **Document the convention.** Add the `<summary>`-paragraph / first-paragraph-is-`@brief` /
   no-`<remarks>` / plain-first-line rules to the skill (e.g. `references/ds-format.md` or a new
   "Authoring C# XML comments for docgen" section). Right now the skill explains `.ds` tags but
   never tells the author how a C# `<summary>` is transformed, so the author can't predict `@brief`.

2. **Lint after prepare.** After the prepare step regenerates `src/raw/`, scan every generated
   `@brief` and flag any that is **not exactly one line** (i.e. the line after `@brief=...` is
   non-empty and isn't a new `@tag`). Report the type/member so the author can fix the source
   `<summary>`. This is a cheap, deterministic check — see the awk one-liner used here:

   ```bash
   awk '/@brief=/{b=substr($0,index($0,"=")+1); getline n; gsub(/^[ \t]+/,"",n);
        if (b!="" && n!="") print FILENAME": multi-line @brief -> "b}' src/raw/*.ds
   ```

3. **(Optional) Lint the source.** Before/independent of prepare, warn when a C# `<summary>`:
   - has no blank-line paragraph break but spans multiple sentences/lines (whole thing → `@brief`);
   - contains `<c>` or `<see cref>` in its first paragraph;
   - uses `<remarks>` (content will be dropped).

### Status

Worked around by hand in this repo on 2026-06-18: every public summary rewritten to
`one-line brief` + blank line + detail paragraph(s); all `<remarks>` folded into summaries; inline
markup removed from first lines. Verified all generated `@brief` values are single-line.

---

## Bug 2 — Skill should use docgen BBCode in XML comments, not XML formatting tags

### What happened

We used the standard C# XML doc formatting tags (`<c>`, `<i>`, `<b>`) inside `<summary>`/`<param>`.
cs2ds renders them poorly:

- **`<c>...</c>`** is emitted as a `[c]...[/c]` block **on its own indented line**, breaking the
  surrounding sentence across three lines. Example (from `ExpressionToJsStubAccessor`):

  ```
  call into a set
  of
       [c]jsv_*[/c]
              helper functions (for null-safe arithmetic, ...
  ```

- **`<i>...</i>`** (and other emphasis) is **dropped** — the word survives but the formatting is
  lost (`<i>once</i>` rendered as bare `once`, no emphasis).

### The fix

Author docgen **BBCode directly** in the XML comments instead of XML formatting tags:

| Instead of        | Write            |
|-------------------|------------------|
| `<c>code</c>`     | `[c]code[/c]`    |
| `<i>text</i>`     | `[i]text[/i]`    |
| `<b>text</b>`     | `[b]text[/b]`    |

BBCode is passed through verbatim by cs2ds and stays **inline**, and `simplified-text-syntax=yes`
(already set) interprets it correctly in the final output. After the switch the same passage renders:

```
of [c]jsv_*[/c] helper functions (for null-safe arithmetic, ...
... load it into the JS engine) [i]once[/i], before any compiled ...
drop the returned text inside a [c]<script>[/c] block ...
```

Notes / caveats:
- BBCode (`[`...`]`) is plain text to the C# compiler, so it produces **no XML doc warnings** and is
  safe in `///` comments.
- Keep XML entities for angle brackets *inside* BBCode for source XML validity:
  `[c]&lt;script&gt;[/c]`, not `[c]<script>[/c]` (the latter is invalid XML in the comment). cs2ds
  decodes the entities, so the output is `[c]<script>[/c]`.
- **Keep `<see cref="...">` / `<paramref>` as XML** — those are *references*, not formatting, and
  cs2ds turns `<see cref>` into the desirable `[clink=...]...[/clink]` links. Only the formatting
  tags should become BBCode.

### What the skill should do (automatable)

1. **Document it:** in the "authoring C# XML comments for docgen" guidance, instruct authors to use
   `[c]`/`[i]`/`[b]` BBCode for formatting and reserve XML tags for `<summary>`, `<param>`,
   `<returns>`, `<typeparam>`, `<see cref>`, `<paramref>`.
2. **Lint the source:** warn when a `///` comment contains `<c>`, `<i>`, `<b>` (or other
   formatting tags) and suggest the BBCode replacement. A simple regex check over `.cs` files:

   ```bash
   grep -rnoE '</?(c|i|b|u)>' --include=*.cs .   # any hit = should be BBCode
   ```

### Status

Worked around by hand in this repo on 2026-06-18: all `<c>`/`<i>`/`<b>` in the public API XML
comments converted to `[c]`/`[i]`/`[b]`; `<see cref>`/`<paramref>` left as XML. Library builds clean
(doc warnings as errors); regenerated `.ds` confirmed inline rendering.

---

## Bug 3 — A line that starts with `- ` or `* ` silently becomes a list bullet

### What happened

docgen's simplified-text-syntax treats any line whose first non-whitespace character is `-` or `*`
(followed by a space) as a **list-item bullet** — the markdown-style convention. This bit us twice,
in both authoring surfaces:

1. **Hand-written `.ds` (`index.ds`).** An item in a `@list` had a long `[clink]` that pushed the
   description onto the next line, which then began with `- `:

   ```
   @list-item
       [clink=...ExpressionToJsStubAccessor]ExpressionToJsStubAccessor[/clink]
       - the JavaScript runtime the emitted expressions depend on; load it into the page once.
   @end
   ```

   The `- the JavaScript runtime...` line rendered as a **nested sub-bullet** under the item instead
   of as the item's text. The three sibling items kept `- ...` on the same line as `[/clink]`, so
   they rendered correctly — which is exactly why the bug was easy to miss.

2. **C# `///` comment (the `ExpressionCompiler` class summary).** Ordinary prose used a parenthetical
   dash that happened to fall at the start of a `///` line:

   ```csharp
   /// ... and <see cref="Members"/>. For deeper control
   /// - notably how a parameter renders on the client - derive a subclass and override the
   /// [c]protected virtual[/c] emit methods. ...
   ```

   cs2ds **preserves source line breaks**, so the generated `.ds` had a line beginning `- notably`,
   which docgen rendered as a stray bullet list dropped into the middle of the paragraph.

### Root cause

Line-leading `- ` / `* ` = bullet, in **both** hand-written `.ds` and the text cs2ds emits from
`///` comments. Because cs2ds keeps your source line breaks, where a dash lands relative to the line
boundary in the `.cs` file directly determines whether it becomes a bullet. Wrapping prose for an
80/100-column source limit can turn a mid-sentence " - " into a line-leading "- " by accident.

### The fix

Never let a line begin with `- ` or `* `. Keep the dash attached to the **end of the previous line**
(`... For deeper control -` then `notably ...`), or reword. When you genuinely want a list, use a
real `@list`/`@list-item` block.

### What the skill should do (automatable)

1. **Document the gotcha** in the authoring guidance (both for `.ds` and for `///` comments).
2. **Lint** every line docgen will consume and warn on a line-leading bullet marker that isn't an
   intentional list. Check **both** the source comments and the generated `.ds`:

   ```bash
   # generated + hand-written .ds that docgen consumes
   grep -rnE '^[[:space:]]*[-*][[:space:]]' src/index.ds src/ns/*.ds src/raw/*.ds
   # C# doc-comment lines starting with a dash/star
   grep -rnE '^[[:space:]]*///[[:space:]]*[-*][[:space:]]' --include=*.cs .
   ```

3. **General lesson for the skill:** verify against the *rendered* output (or at least the generated
   `.ds`), not just the source — these transforms have surprises (see Bugs 1–3) that are invisible
   in the `///` source.

### Status

Fixed by hand on 2026-06-18: `index.ds` item reflowed so no line starts with `- `; the
`ExpressionCompiler` summary dash moved to the end of the prior line. Rebuilt library → `Scan`,
`Prepare` → `MakeDoc`; confirmed the index list now has exactly four `<li>` (no nested `<ul>`) and the
class summary renders as prose with no stray bullet. Scanned all consumed `.ds` and `///` comments:
no remaining line-leading `- `/`* `.

---

## Bug 4 — `&` in `.ds` text/code is not escaped like `<`/`>`; literal `&` and `&&` corrupt

### What happened

While writing the how-to articles (hand-written `.ds`, including `@example` code blocks with
`@highlight=cs`), C# operators that contain `&` rendered wrong in the HTML output:

- A **single** `[c]&[/c]` (bitwise-and) emitted `<code>&</code>` — a **bare, unescaped** `&` in
  the HTML. Browsers tolerate it and display `&`, but it is invalid markup.
- A **double** `[c]&&[/c]` (logical-and), and `&&` inside an `@example`, emitted `<code>&amp;</code>`
  / `... &amp; ...` — i.e. **one of the two ampersands was swallowed**, so `x > 0 && x < 100`
  rendered as `x > 0 & x < 100`. A genuine content corruption, not just an escaping nit.

This is asymmetric with `<`/`>`: a **raw** `<` in `.ds` is correctly escaped to `&lt;` on output
(so generics like `Expression<Func<int,bool>>` render fine as raw text — see also the convention
that `.ds` is plain text, *not* XML, so you do **not** entity-escape `<`/`>` in the source). Only
`&` is mishandled.

### The fix (confirmed empirically on this repo)

Write `&` as the HTML entity **`&amp;`** in the `.ds` source — in both body text and `@example`
code. docgen passes `&amp;` through verbatim into the HTML, which renders as a literal `&`.

| C# you want to show | Write in `.ds`        | Renders as |
|---------------------|-----------------------|------------|
| `a & b`             | `[c]a &amp; b[/c]`    | `a & b`    |
| `a && b`            | `[c]a &amp;&amp; b[/c]` | `a && b` |
| `x => x > 0 && y`   | `x => x > 0 &amp;&amp; y` (in `@example`) | `x => x > 0 && y` |

So the cross-language rule for hand-written `.ds`: **raw `<` and `>`, but `&amp;` for `&`.**
(Contrast with `///` C# comments, which *are* XML and need `&lt;`/`&gt;`/`&amp;` for all three.)

### What the skill should do (automatable)

1. **Document it** in `references/ds-format.md` (Escaping section): in `.ds` source, `<`/`>` are
   literal but `&` must be written `&amp;` (else single `&` emits unescaped and `&&` loses a
   character). Note the asymmetry with the XML `///`-comment rule.
2. **Lint** `.ds` files (including `@example` bodies) for a raw `&` not already part of an entity:

   ```bash
   grep -rnE '&(?!amp;|lt;|gt;|quot;|#)' src/index.ds src/ns/*.ds src/articles/*.ds
   ```

3. Reinforces Bug 3's lesson: verify against the **rendered HTML**, not the `.ds` source — this
   corruption is invisible in the source.

### Status

Fixed by hand on 2026-06-19: every `&`/`&&` in the new articles (`supported-features.ds` operator
table, `validation-walkthrough.ds` and `unit-testing.ds` examples) written as `&amp;`. Rebuilt
`MakeDoc`; confirmed `&&` renders as `&&` and bitwise `&` as a properly-escaped `&amp;`.

---

## Bug 5 — `@example` strips per-line leading whitespace; indented code collapses to flush-left

### What happened

A multi-line C# `@example` (`@highlight=cs`) with a nested method body rendered with **every line
flush-left** — docgen trims the leading whitespace of *each* line, not just the common indent, so
`{ }` blocks, nested statements, and comment indentation are all lost. The code is still
syntactically there but unreadable as a structured block.

This only bites examples with **relative** indentation (a method body, a loop, an `if`). Flat
single-level examples (a few statements all at the same indent) are unaffected — their common
indent is trimmed and they still read fine.

### The fix (two options, confirmed on this repo)

To preserve indentation, mark the lines as literal. Either:

1. **`!` line-prefix** — begin each code line (after the block's reading-indent) with `!`. Everything
   *after* the `!` is preserved verbatim, including its own leading spaces. Works with
   `@highlight`. Blank lines inside the block need a bare `!` too (renders as a one-space line).

   ```
   @example
       @title=...
       @highlight=cs
      !static void M<T>(T x)
      !{
      !    if (x != null)
      !        Use(x);
      !}
   @end
   ```

2. **Markdown fenced block** — wrap the code in a triple-backtick fence inside the `@example`.

Note: `<`/`>` inside `!` lines are still HTML-escaped correctly (raw is fine); `&` still needs
`&amp;` (Bug 4) — the `!` only controls whitespace, not the `&` handling.

### What the skill should do (automatable)

1. **Document it** in `references/ds-format.md` (the `@example` section): docgen trims per-line
   leading whitespace; use the `!` line-prefix (or a markdown fence) for any example whose lines are
   relatively indented.
2. **Lint**: in any `@example` body, if a later line has more leading whitespace than the first
   non-blank body line and no `!` prefix / fence is present, warn that indentation will be lost.
3. Same meta-lesson as Bugs 3–4: check the **rendered** block, not the `.ds` source.

### Status

Fixed by hand on 2026-06-19: the differential-assertion example in `unit-testing.ds` (the only one
with a nested body) switched to `!`-prefixed lines. Rebuilt `MakeDoc`; confirmed the method body
renders with its indentation intact and the generic `<...>` still escaped.

---

## Bug 6 — `@width` is pixels unless you add `%`; column proportions via `@col @width` on the header

### What happened

A `@table` with `@width=100` rendered **content-width**, not full width — docgen emits the value
verbatim as `<table ... width="100">`, which HTML reads as **100 pixels**. The skill's `ds-format.md`
says "@width is in percent", which is misleading: you must write the `%`.

### The fix (confirmed on this repo)

- Full-width table: `@width=100%` → `<table width="100%">`.
- Column proportions: put `@width=30%` / `@width=70%` on the `@col` cells of the **header row** only
  (the rest of the column follows). Renders as `<td ... width="30%">`.

### What the skill should do

Document that `@table`/`@col` `@width` values are emitted verbatim into HTML, so a bare number is
pixels — always include `%` for proportional sizing. Update the `ds-format.md` table section.

### Status

Done 2026-06-19: index "API at a glance", the namespace entry-points table, and the model-rules
binding table are all `@width=100%` with 30%/70% header columns.

---

## Bug 7 — `@see` renders as an empty-description table; prefer a "See also" list

### What happened

Ending an article with several `@see` blocks (`@key` + `@title`) renders a 2-column **table** whose
second (description) column is **empty** — visually it looks broken (a wide blank column beside each
link).

### The fix

Drop `@see`. End the article with a heading and a list instead:

```
@headline
    @level=2
    See also
@end
@list
    @list-item
        [link=other-key]Other Article Title[/link]
    @end
@end
```

Renders as a clean `<h2>See also</h2>` + `<ul>` of links.

### What the skill should do

Recommend the "See also" `@headline` + `@list` pattern over `@see` for cross-article navigation
(at least in the HTML template), and note the empty-description-column rendering of `@see`.

### Status

Done 2026-06-19: all `@see` blocks across the articles converted to "See also" lists.
