using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Gehtsoft.ExpressionToJs
{
    /// <summary>How DateTime values are framed in the emitted JavaScript.</summary>
    public enum DateTimeMode
    {
        /// <summary>Local time: new Date(y, m, d, ...) and getFullYear()/getMonth()/...</summary>
        Local,

        /// <summary>UTC: new Date(Date.UTC(y, m, d, ...)) and getUTCFullYear()/getUTCMonth()/...</summary>
        Utc,
    }

    /// <summary>
    /// The building blocks a method-call translator needs to emit JavaScript.
    ///
    /// Implemented by <see cref="ExpressionCompiler"/>; delegates to its (overridable) walk/escape
    /// helpers so custom translators compose with subclass customizations such as parameter
    /// rendering.
    /// </summary>
    public interface IExpressionEmitContext
    {
        /// <summary>Emit the JavaScript for a sub-expression (walks the tree).</summary>
        string Emit(Expression expression);

        /// <summary>Try to fold a (parameter-free) sub-expression to a constant value.</summary>
        bool TryEvaluate(Expression expression, out object value);

        /// <summary>Emit a lambda as a JavaScript function-expression predicate (function (...) { return ...; }).</summary>
        string EmitPredicate(LambdaExpression lambda);

        /// <summary>Emit access to a parameter-rooted member/indexer chain.</summary>
        string EmitParameterAccess(Expression expression);

        /// <summary>True if the expression is (or roots in) one of the lambda's parameters.</summary>
        bool RootsInParameter(Expression expression);

        /// <summary>Render a CLR string as a JavaScript string literal.</summary>
        string EscapeString(string value);

        /// <summary>Escape a regex pattern for use inside a JavaScript /.../ literal.</summary>
        string EscapeRegex(string value);

        /// <summary>The configured DateTime framing (Local or Utc).</summary>
        DateTimeMode DateMode { get; }
    }

    /// <summary>
    /// Translates a specific family of method calls into JavaScript.
    ///
    /// Translators are tried in order; the first whose <see cref="TryTranslate"/> returns true wins.
    /// Register custom ones through <see cref="ExpressionCompiler.Methods"/>; registrations are
    /// consulted before the built-ins, so registering one is how you override a built-in translation.
    /// </summary>
    public interface IMethodCallTranslator
    {
        /// <summary>
        /// Implement this to claim a method call and produce its JavaScript.
        ///
        /// The compiler offers each call to the registered translators in turn, so return
        /// [c]false[/c] (and leave <paramref name="js"/> null) for calls you don't handle, and only
        /// consume the ones you recognize - keeping translators disjoint is what lets them be
        /// combined freely.
        /// </summary>
        /// <param name="call">The call to inspect; check its method, declaring type, and arguments to decide whether it is yours.</param>
        /// <param name="context">Use it to emit sub-expressions, fold constants, escape strings, and read the date mode - so your output composes with the rest of the compiler.</param>
        /// <param name="js">Receives the emitted JavaScript when you return [c]true[/c]; otherwise leave it null.</param>
        /// <returns>[c]true[/c] if this translator handled the call; [c]false[/c] to let another try.</returns>
        bool TryTranslate(MethodCallExpression call, IExpressionEmitContext context, out string js);
    }

    /// <summary>
    /// Additive extension surface for mapping method calls to JavaScript.
    ///
    /// Registrations are consulted [b]before[/b] the (fixed, correctly-ordered) built-ins, so a
    /// consumer can shadow a specific method without being able to reorder or remove built-ins - the
    /// built-in precedence invariants are preserved. Registrations are tried in the order added.
    /// </summary>
    public interface IJsMethodRegistry
    {
        /// <summary>Register a custom translator (consulted before built-ins).</summary>
        IJsMethodRegistry AddTranslator(IMethodCallTranslator translator);

        /// <summary>Map a method (by type + name) to a JavaScript template ($obj, $0, $1, ...).</summary>
        IJsMethodRegistry MapMethod(Type declaringType, string name, string template, int? argCount = null);

        /// <summary>Map a specific method to a JavaScript template ($obj, $0, $1, ...).</summary>
        IJsMethodRegistry MapMethod(System.Reflection.MethodInfo method, string template);
    }

    /// <summary>
    /// Data-driven translator for the many 1:1 method mappings. A template uses the tokens
    /// [c]$obj[/c] (the call target) and [c]$0[/c], [c]$1[/c], ... (the emitted arguments).
    /// </summary>
    internal sealed class TableMethodTranslator : IMethodCallTranslator
    {
        private sealed class Entry
        {
            public Type DeclaringType;
            public string Name;
            public int? ArgCount;
            public bool? Instance;
            public string Template;
            public Func<MethodCallExpression, bool> Where;
        }

        private readonly List<Entry> mEntries = new List<Entry>();

        /// <summary>
        /// Adds one method-to-template mapping and returns the same translator so calls chain.
        /// Reach for this (rather than a hand-written <see cref="IMethodCallTranslator"/>) whenever
        /// a method maps straight onto a JavaScript expression with the arguments dropped into
        /// fixed places - the common case.
        /// </summary>
        /// <param name="declaringType">The type that declares the method; pass [c]null[/c] to match by name regardless of declaring type (use sparingly, as it widens the match).</param>
        /// <param name="name">The method name to match, exactly.</param>
        /// <param name="argCount">The required argument count, or [c]null[/c] to match any arity. Give a value to disambiguate overloads that need different templates.</param>
        /// <param name="instance">[c]true[/c] matches only instance calls, [c]false[/c] only static calls, [c]null[/c] either. Set it to keep an instance overload from capturing a static one of the same name.</param>
        /// <param name="template">The JavaScript template. [c]$obj[/c] is replaced by the call target and [c]$0[/c], [c]$1[/c], ... by the emitted arguments in order.</param>
        /// <param name="where">An optional extra predicate over the call; the mapping applies only when it returns [c]true[/c]. Use it for conditions arity can't express, e.g. requiring a particular argument type.</param>
        /// <returns>This translator, for fluent chaining of further <see cref="Map"/> calls.</returns>
        public TableMethodTranslator Map(Type declaringType, string name, int? argCount, bool? instance, string template, Func<MethodCallExpression, bool> where = null)
        {
            mEntries.Add(new Entry
            {
                DeclaringType = declaringType,
                Name = name,
                ArgCount = argCount,
                Instance = instance,
                Template = template,
                Where = where,
            });
            return this;
        }

        public bool TryTranslate(MethodCallExpression call, IExpressionEmitContext context, out string js)
        {
            foreach (Entry e in mEntries)
            {
                if (e.DeclaringType != null && call.Method.DeclaringType != e.DeclaringType)
                    continue;
                if (e.Name != call.Method.Name)
                    continue;
                if (e.ArgCount.HasValue && call.Arguments.Count != e.ArgCount.Value)
                    continue;
                if (e.Instance.HasValue && (call.Object != null) != e.Instance.Value)
                    continue;
                if (e.Where != null && !e.Where(call))
                    continue;

                js = Fill(e.Template, call, context);
                return true;
            }
            js = null;
            return false;
        }

        private static string Fill(string template, MethodCallExpression call, IExpressionEmitContext context)
        {
            string result = template;
            if (call.Object != null && result.Contains("$obj"))
                result = result.Replace("$obj", context.Emit(call.Object));
            for (int i = call.Arguments.Count - 1; i >= 0; i--)
                result = result.Replace("$" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), context.Emit(call.Arguments[i]));
            return result;
        }
    }

    /// <summary>Regex.IsMatch in its static (pattern[, options]) and instance forms.</summary>
    internal sealed class RegexIsMatchTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.DeclaringType != typeof(Regex) || e.Method.Name != nameof(Regex.IsMatch))
                return false;

            if (e.Object == null && e.Arguments.Count == 2)
            {
                string pattern;
                if (ctx.TryEvaluate(e.Arguments[1], out object r) && r is string s)
                    pattern = $"/{ctx.EscapeRegex(s)}/";
                else
                    pattern = ctx.Emit(e.Arguments[1]);
                js = $"jsv_match({pattern}, {ctx.Emit(e.Arguments[0])})";
                return true;
            }

            if (e.Object == null && e.Arguments.Count == 3)
            {
                if (!ctx.TryEvaluate(e.Arguments[1], out object r) || !(r is string s))
                    throw new InvalidOperationException("The string object is expected here");
                if (!ctx.TryEvaluate(e.Arguments[2], out object r1) || !(r1 is RegexOptions ro))
                    throw new InvalidOperationException("The regex options are expected here");
                js = $"jsv_match(/{ctx.EscapeRegex(s)}/{OptionSuffix(ro)}, {ctx.Emit(e.Arguments[0])})";
                return true;
            }

            if (e.Object != null && e.Arguments.Count == 1)
            {
                if (!ctx.TryEvaluate(e.Object, out object r) || !(r is Regex re))
                    throw new InvalidOperationException("The regex object is expected here");
                js = $"jsv_match(/{ctx.EscapeRegex(re.ToString())}/{OptionSuffix(re.Options)}, {ctx.Emit(e.Arguments[0])})";
                return true;
            }

            return false;
        }

        private static string OptionSuffix(RegexOptions options)
        {
            string suffix = "";
            if ((options & RegexOptions.IgnoreCase) != 0) suffix += "i";
            if ((options & RegexOptions.Multiline) != 0) suffix += "m";
            return suffix;
        }
    }

    /// <summary>string.IndexOf with optional start index and/or StringComparison.</summary>
    internal sealed class StringIndexOfTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.DeclaringType != typeof(string) || e.Method.Name != nameof(string.IndexOf) || e.Object == null)
                return false;

            string startIndex = "0";
            bool ignoreCase = false;
            string obj = ctx.Emit(e.Object);
            string argument = ctx.Emit(e.Arguments[0]);

            if (e.Arguments.Count > 1)
            {
                if (e.Arguments[1].Type == typeof(StringComparison))
                    ignoreCase = IsIgnoreCase(ctx, e.Arguments[1]);
                else
                    startIndex = ctx.Emit(e.Arguments[1]);
            }

            if (e.Arguments.Count == 3)
                ignoreCase = IsIgnoreCase(ctx, e.Arguments[2]);

            if (ignoreCase)
            {
                obj = $"jsv_upper({obj})";
                argument = $"jsv_upper({argument})";
            }

            js = $"(({obj}).indexOf({argument}, {startIndex}))";
            return true;
        }

        private static bool IsIgnoreCase(IExpressionEmitContext ctx, Expression argument)
        {
            if (!ctx.TryEvaluate(argument, out object value) || !(value is StringComparison comparison))
                throw new ArgumentException("Only StringComparison argument is supported for IndexOf function");
            return comparison == StringComparison.CurrentCultureIgnoreCase
                || comparison == StringComparison.InvariantCultureIgnoreCase
                || comparison == StringComparison.OrdinalIgnoreCase;
        }
    }

    /// <summary>string.StartsWith(value, StringComparison).</summary>
    internal sealed class StringStartsWithComparisonTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.DeclaringType != typeof(string) || e.Method.Name != nameof(string.StartsWith) || e.Object == null || e.Arguments.Count != 2)
                return false;

            if (!ctx.TryEvaluate(e.Arguments[1], out object value) || !(value is StringComparison comparison))
                throw new ArgumentException("Only StringComparison second argument is supported for StartsWith function");

            bool ignoreCase = comparison == StringComparison.CurrentCultureIgnoreCase
                || comparison == StringComparison.InvariantCultureIgnoreCase
                || comparison == StringComparison.OrdinalIgnoreCase;

            if (ignoreCase)
                js = $"(jsv_upper({ctx.Emit(e.Object)}).indexOf(jsv_upper({ctx.Emit(e.Arguments[0])})) == 0)";
            else
                js = $"(({ctx.Emit(e.Object)}).indexOf({ctx.Emit(e.Arguments[0])}) == 0)";
            return true;
        }
    }

    /// <summary>
    /// Functions.MonthsSince / Functions.YearsSince - calendar-based, so mode-aware: passes a UTC flag
    /// to the stub which then reads getUTC*/setUTC* (Utc) or getX/setX (Local) components.
    /// (Functions.DaysSince is epoch-based and stays a plain table mapping.)
    /// </summary>
    internal sealed class DateDiffTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.DeclaringType != typeof(Functions) || e.Arguments.Count != 2)
                return false;

            string fn;
            switch (e.Method.Name)
            {
                case nameof(Functions.MonthsSince): fn = "jsv_monthssince"; break;
                case nameof(Functions.YearsSince): fn = "jsv_yearssince"; break;
                default: return false;
            }

            string utc = ctx.DateMode == DateTimeMode.Utc ? "true" : "false";
            js = $"{fn}({ctx.Emit(e.Arguments[0])}, {ctx.Emit(e.Arguments[1])}, {utc})";
            return true;
        }
    }

    /// <summary>string.EndsWith(value, StringComparison).</summary>
    internal sealed class StringEndsWithComparisonTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.DeclaringType != typeof(string) || e.Method.Name != nameof(string.EndsWith) || e.Object == null || e.Arguments.Count != 2)
                return false;

            if (!ctx.TryEvaluate(e.Arguments[1], out object value) || !(value is StringComparison comparison))
                throw new ArgumentException("Only StringComparison second argument is supported for EndsWith function");

            bool ignoreCase = comparison == StringComparison.CurrentCultureIgnoreCase
                || comparison == StringComparison.InvariantCultureIgnoreCase
                || comparison == StringComparison.OrdinalIgnoreCase;

            if (ignoreCase)
                js = $"jsv_endswith(jsv_upper({ctx.Emit(e.Object)}), jsv_upper({ctx.Emit(e.Arguments[0])}))";
            else
                js = $"jsv_endswith({ctx.Emit(e.Object)}, {ctx.Emit(e.Arguments[0])})";
            return true;
        }
    }

    /// <summary>
    /// Collection Contains(item): instance form (List&lt;T&gt;.Contains, ICollection) or static form
    /// (Enumerable.Contains, or MemoryExtensions.Contains for arrays where the source arrives as a
    /// span conversion). String.Contains is handled by the table.
    /// </summary>
    internal sealed class CollectionContainsTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.Name != nameof(Enumerable.Contains) || e.Method.DeclaringType == typeof(string))
                return false;

            if (e.Object != null && e.Arguments.Count == 1)
            {
                js = $"jsv_contains({ctx.Emit(e.Object)}, {ctx.Emit(e.Arguments[0])})";
                return true;
            }
            if (e.Object == null && e.Arguments.Count == 2)
            {
                js = $"jsv_contains({UnwrapCollection(e.Arguments[0], ctx)}, {ctx.Emit(e.Arguments[1])})";
                return true;
            }
            return false;
        }

        // Unwrap an array->span conversion (Convert node or op_Implicit call) to the underlying collection.
        private static string UnwrapCollection(Expression expression, IExpressionEmitContext ctx)
        {
            if (expression.NodeType == ExpressionType.Convert && expression is UnaryExpression u)
                return ctx.Emit(u.Operand);
            if (expression.NodeType == ExpressionType.Call && expression is MethodCallExpression c
                && c.Method.Name == "op_Implicit" && c.Arguments.Count == 1)
                return ctx.Emit(c.Arguments[0]);
            return ctx.Emit(expression);
        }
    }

    /// <summary>
    /// LINQ methods over an IEnumerable source: Any/All/Count/First/Last/FirstOrDefault/LastOrDefault/
    /// Empty, the projecting/filtering Where/Select (return arrays, so they chain), the aggregates
    /// Sum/Min/Max (with or without selector), and Distinct.
    /// </summary>
    internal sealed class LinqTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (!(e.Arguments?.Count >= 1) || !typeof(IEnumerable).IsAssignableFrom(e.Arguments[0].Type))
                return false;

            string source = ctx.Emit(e.Arguments[0]);

            if (e.Arguments.Count == 2 && e.Arguments[1].NodeType == ExpressionType.Lambda)
            {
                string lambda = ctx.EmitPredicate((LambdaExpression)e.Arguments[1]);
                switch (e.Method.Name)
                {
                    case nameof(Enumerable.Any): js = $"jsv_any({source}, {lambda})"; return true;
                    case nameof(Enumerable.All): js = $"jsv_all({source}, {lambda})"; return true;
                    case nameof(Enumerable.Count): js = $"jsv_count({source}, {lambda})"; return true;
                    case nameof(Enumerable.First): js = $"jsv_first({source}, {lambda})"; return true;
                    case nameof(Enumerable.Last): js = $"jsv_last({source}, {lambda})"; return true;
                    case nameof(Enumerable.Where): js = $"jsv_where({source}, {lambda})"; return true;
                    case nameof(Enumerable.Select): js = $"jsv_select({source}, {lambda})"; return true;
                    case nameof(Enumerable.Sum): js = $"jsv_sum({source}, {lambda})"; return true;
                    case nameof(Enumerable.Min): js = $"jsv_min({source}, {lambda})"; return true;
                    case nameof(Enumerable.Max): js = $"jsv_max({source}, {lambda})"; return true;
                }
            }
            else if (e.Arguments.Count == 3 && e.Arguments[1].NodeType == ExpressionType.Lambda)
            {
                string predicate = ctx.EmitPredicate((LambdaExpression)e.Arguments[1]);
                string fallback = ctx.Emit(e.Arguments[2]);
                switch (e.Method.Name)
                {
                    case nameof(Enumerable.FirstOrDefault): js = $"jsv_first({source}, {predicate}, {fallback})"; return true;
                    case nameof(Enumerable.LastOrDefault): js = $"jsv_last({source}, {predicate}, {fallback})"; return true;
                }
            }
            else if (e.Arguments.Count == 1)
            {
                switch (e.Method.Name)
                {
                    case nameof(Enumerable.Empty): js = $"jsv_isempty({source})"; return true;
                    case nameof(Enumerable.Count): js = $"jsv_count({source}, undefined)"; return true;
                    case nameof(Enumerable.Sum): js = $"jsv_sum({source}, undefined)"; return true;
                    case nameof(Enumerable.Min): js = $"jsv_min({source}, undefined)"; return true;
                    case nameof(Enumerable.Max): js = $"jsv_max({source}, undefined)"; return true;
                    case nameof(Enumerable.Distinct): js = $"jsv_distinct({source})"; return true;
                }
            }

            return false;
        }
    }

    /// <summary>Indexer access (get_Item); parameter-rooted indexers defer to parameter access.</summary>
    internal sealed class IndexerTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.Name != "get_Item" || e.Arguments.Count != 1)
                return false;

            js = e.Object?.NodeType == ExpressionType.Parameter
                ? ctx.EmitParameterAccess(e)
                : $"jsv_index({ctx.Emit(e.Object)}, {ctx.Emit(e.Arguments[0])})";
            return true;
        }
    }

    /// <summary>Parameterless ToString() on any type.</summary>
    internal sealed class ToStringTranslator : IMethodCallTranslator
    {
        public bool TryTranslate(MethodCallExpression e, IExpressionEmitContext ctx, out string js)
        {
            js = null;
            if (e.Method.Name != nameof(object.ToString) || (e.Arguments != null && e.Arguments.Count != 0))
                return false;
            js = $"jsv_tostring({ctx.Emit(e.Object)})";
            return true;
        }
    }
}
