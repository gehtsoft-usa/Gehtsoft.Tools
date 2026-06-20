using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable S1199, S4035

namespace Gehtsoft.ExpressionToJs
{
    /// <summary>
    /// Converts a C# lambda expression into an equivalent JavaScript expression string.
    ///
    /// This is the entry point of the library. Construct it around a lambda - typically a boolean
    /// form-validation predicate - and read <see cref="JavaScriptExpression"/> to get JavaScript
    /// that computes the same result in the browser. The point is to keep one source of truth: the
    /// lambda runs server-side as a compiled delegate, and its emitted twin runs client-side, so a
    /// rule cannot drift between the two ends.
    ///
    /// Configure framing and translation [i]before[/i] reading the expression: set
    /// <see cref="DateMode"/> for date semantics, and register custom translations through
    /// <see cref="Methods"/>, <see cref="Constants"/>, and <see cref="Members"/>. For deeper control -
    /// notably how a parameter renders on the client - derive a subclass and override the
    /// [c]protected virtual[/c] emit methods. Whatever the emitted code calls (the [c]jsv_*[/c]
    /// helpers) must be present at evaluation time; see
    /// <see cref="ExpressionToJsStubAccessor.GetJsIncludesAsString"/>.
    /// </summary>
    public class ExpressionCompiler : IEquatable<ExpressionCompiler>, IEquatable<Expression>
    {
        private readonly LambdaExpression mExpression;
        private string mJavaScriptExpression = null;

        private readonly EmitContext mEmitContext;
        private readonly List<IMethodCallTranslator> mUserTranslators = new List<IMethodCallTranslator>();
        private readonly List<IConstantTranslator> mUserConstants = new List<IConstantTranslator>();
        private readonly List<IMemberTranslator> mUserMembers = new List<IMemberTranslator>();
        private readonly MethodRegistry mMethodRegistry;
        private readonly ConstantRegistry mConstantRegistry;
        private readonly MemberRegistry mMemberRegistry;
        private readonly List<ParameterBinding> mParameterBindings = new List<ParameterBinding>();
        private readonly ParameterRegistry mParameterRegistry;

        // Built-ins are disjoint by type+name (at most one matches any node), shared across instances
        // and never mutated. The member list ends with the terminal parameter-access fallback.
        private static readonly IReadOnlyList<IMethodCallTranslator> sBuiltinTranslators = BuildBuiltinTranslators();
        private static readonly IReadOnlyList<IConstantTranslator> sBuiltinConstants = new IConstantTranslator[] { new DateTimeConstantTranslator(), new BuiltinConstantTranslator() };
        private static readonly IReadOnlyList<IMemberTranslator> sBuiltinMembers = BuildBuiltinMemberTranslators();

        /// <summary>
        /// The CLR type the lambda produces.
        ///
        /// Inspect it to decide how to consume the emitted code - most commonly to confirm a
        /// validation lambda yields a [c]bool[/c] before wiring its JavaScript into a form's
        /// validity check.
        /// </summary>
        public Type ReturnType { get; }

        /// <summary>
        /// How DateTime values are framed in the emitted JavaScript - local time or UTC.
        ///
        /// Selects <see cref="DateTimeMode.Local"/> (default) or <see cref="DateTimeMode.Utc"/>. Set
        /// it before reading <see cref="JavaScriptExpression"/>. The host must bind JS-side dates in
        /// the same frame (see the library docs / PLAN.md).
        /// </summary>
        public DateTimeMode DateMode { get; set; } = DateTimeMode.Local;

        /// <summary>
        /// Registry for mapping method calls the built-ins don't cover to JavaScript.
        ///
        /// Register here when the lambda calls a method the built-ins don't handle (a domain helper,
        /// an overload with different arguments) so it maps to JavaScript instead of throwing at
        /// compile time. Register before reading <see cref="JavaScriptExpression"/>; your
        /// registrations are consulted ahead of the built-ins, which also lets you deliberately
        /// override one.
        /// </summary>
        public IJsMethodRegistry Methods => mMethodRegistry;

        /// <summary>
        /// Registry for mapping constant types the built-ins don't cover to JavaScript.
        ///
        /// Register here to teach the compiler how to emit a constant of a type it doesn't already
        /// handle (a <see cref="Guid"/>, a domain value struct) or to emit an enum by name rather
        /// than by its numeric value. Register before reading <see cref="JavaScriptExpression"/>.
        /// </summary>
        public IJsConstantRegistry Constants => mConstantRegistry;

        /// <summary>
        /// Registry for mapping property reads the built-ins don't cover to JavaScript.
        ///
        /// Register here when the lambda reads a property the built-ins don't handle and you want it
        /// to map to a specific JavaScript form. Register before reading
        /// <see cref="JavaScriptExpression"/>.
        /// </summary>
        public IJsMemberRegistry Members => mMemberRegistry;

        /// <summary>
        /// Registry for binding a parameter type to a host-side lookup instead of emitting it verbatim.
        ///
        /// Register a model type here (with <see cref="IJsParameterRegistry.MapReference"/>) so rules
        /// over that type emit against the form through a [c]reference('path')[/c] hook rather than a
        /// server-shaped object - the form-validation case, with no subclassing. Register before
        /// reading <see cref="JavaScriptExpression"/>.
        /// </summary>
        public IJsParameterRegistry Parameters => mParameterRegistry;

        /// <summary>
        /// The emitted JavaScript - the whole reason to use this type.
        ///
        /// Read it after the compiler is fully configured (date mode and any custom registrations
        /// applied); the result is computed on first access and cached, so later configuration
        /// changes are not reflected. Send the string to the client to evaluate, having first loaded
        /// the runtime stub (<see cref="ExpressionToJsStubAccessor.GetJsIncludesAsString"/>).
        /// </summary>
        public string JavaScriptExpression
        {
            get
            {
                return mJavaScriptExpression ?? (mJavaScriptExpression = WalkExpression(mExpression.Body));
            }
        }

        /// <summary>
        /// Creates a compiler for the given lambda using local-time date framing.
        ///
        /// Use this overload unless the client binds dates in UTC; pass <see cref="DateTimeMode"/>
        /// explicitly (or set <see cref="DateMode"/>) when it does.
        /// </summary>
        /// <param name="lambdaExpression">
        /// The expression to translate - usually a validation predicate. Its parameters become the
        /// free variables of the emitted JavaScript, so they determine what the client code reads
        /// from; its body is what gets translated.
        /// </param>
        public ExpressionCompiler(LambdaExpression lambdaExpression)
            : this(lambdaExpression, DateTimeMode.Local)
        {
        }

        /// <summary>
        /// Creates a compiler for the given lambda with an explicit date framing.
        ///
        /// Use this when you already know whether the client binds dates in local time or UTC and
        /// want to fix it at construction.
        /// </summary>
        /// <param name="lambdaExpression">
        /// The expression to translate. Its parameters become the free variables of the emitted
        /// JavaScript; its body is what gets translated.
        /// </param>
        /// <param name="dateMode">
        /// Sets <see cref="DateMode"/>. Choose the frame the host will bind JS-side dates in:
        /// <see cref="DateTimeMode.Local"/> emits local-time constructs,
        /// <see cref="DateTimeMode.Utc"/> emits UTC ones. A mismatch silently shifts every
        /// calendar-component comparison by the client's offset.
        /// </param>
        public ExpressionCompiler(LambdaExpression lambdaExpression, DateTimeMode dateMode)
        {
            mExpression = lambdaExpression;
            ReturnType = lambdaExpression.ReturnType;
            DateMode = dateMode;
            mEmitContext = new EmitContext(this);
            mMethodRegistry = new MethodRegistry(mUserTranslators);
            mConstantRegistry = new ConstantRegistry(mUserConstants);
            mMemberRegistry = new MemberRegistry(mUserMembers);
            mParameterRegistry = new ParameterRegistry(mParameterBindings);
        }

        protected virtual string WalkExpression(Expression expression)
        {
            if (expression.CanReduce)
                expression = expression.Reduce();

            if (GetExpressionValue(expression, out object value))
                return EmitConstant(value);

            switch (expression.NodeType)
            {
                case ExpressionType.Lambda:
                    return WalkExpression(((LambdaExpression)expression).Body);

                case ExpressionType.Add:
                    return AddBinary("jsv_plus", (BinaryExpression)expression);

                case ExpressionType.Subtract:
                    return AddBinary("jsv_minus", (BinaryExpression)expression);

                case ExpressionType.Multiply:
                    return AddBinary("jsv_multiply", (BinaryExpression)expression);

                case ExpressionType.Divide:
                    return AddBinary("jsv_divide", (BinaryExpression)expression);

                case ExpressionType.Modulo:
                    return AddBinary("jsv_modulus", (BinaryExpression)expression);

                case ExpressionType.Power:
                    return AddBinary("jsv_power", (BinaryExpression)expression);

                case ExpressionType.Negate:
                    return AddUnary("jsv_unaryminus", (UnaryExpression)expression);

                case ExpressionType.And:
                    return AddBinary("jsv_bwand", (BinaryExpression)expression);

                case ExpressionType.AndAlso:
                    return AddBinary("jsv_and", (BinaryExpression)expression);

                case ExpressionType.Or:
                    return AddBinary("jsv_bwor", (BinaryExpression)expression);

                case ExpressionType.ExclusiveOr:
                    return AddBinary("jsv_xor", (BinaryExpression)expression);

                case ExpressionType.OrElse:
                    return AddBinary("jsv_or", (BinaryExpression)expression);

                case ExpressionType.Not:
                    return AddUnary("jsv_not", (UnaryExpression)expression);

                case ExpressionType.Equal:
                    return AddBinary("jsv_equal", (BinaryExpression)expression);

                case ExpressionType.LessThan:
                    return AddBinary("jsv_less", (BinaryExpression)expression);

                case ExpressionType.LessThanOrEqual:
                    return AddBinary("jsv_lessorequal", (BinaryExpression)expression);

                case ExpressionType.GreaterThan:
                    return AddBinary("jsv_greater", (BinaryExpression)expression);

                case ExpressionType.GreaterThanOrEqual:
                    return AddBinary("jsv_greaterorequal", (BinaryExpression)expression);

                case ExpressionType.NotEqual:
                    return AddBinary("jsv_notequal", (BinaryExpression)expression);

                case ExpressionType.Call:
                    return AddCall((MethodCallExpression)expression);

                case ExpressionType.Convert:
                    return AddConvert((UnaryExpression)expression);

                case ExpressionType.Constant:
                    return AddConstant((ConstantExpression)expression);

                case ExpressionType.MemberAccess:
                    return AddMemberAccess((MemberExpression)expression);

                case ExpressionType.Parameter:
                    return AddParameter((ParameterExpression)expression);

                case ExpressionType.Conditional:
                    return AddConditional((ConditionalExpression)expression);

                case ExpressionType.Coalesce:
                    return AddCoalesce((BinaryExpression)expression);

                case ExpressionType.ArrayIndex:
                    return AddArrayIndex((BinaryExpression)expression);

                case ExpressionType.ArrayLength:
                    return AddArrayLength((UnaryExpression)expression);

                default:
                    throw new ArgumentException($"Expression type {expression.NodeType} isn't supported", nameof(expression));
            }
        }

        protected virtual string AddArrayLength(UnaryExpression expression) => $"jsv_length({WalkExpression(expression.Operand)})";

        protected virtual string AddConditional(ConditionalExpression expression) => $"(({WalkExpression(expression.Test)}) ? ({WalkExpression(expression.IfTrue)}) : ({WalkExpression(expression.IfFalse)}))";

        protected virtual string AddCoalesce(BinaryExpression expression) => $"jsv_coalesce({WalkExpression(expression.Left)}, {WalkExpression(expression.Right)})";

        protected virtual string AddBinary(string function, BinaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Add && expression.Left.Type == typeof(DateTime) && expression.Right.Type == typeof(TimeSpan))
                return $"(new Date(({WalkExpression(expression.Left)}).getTime() + ({WalkExpression(expression.Right)})))";

            if (expression.NodeType == ExpressionType.Subtract && expression.Left.Type == typeof(DateTime) && expression.Right.Type == typeof(TimeSpan))
                return $"(new Date(({WalkExpression(expression.Left)}).getTime() - ({WalkExpression(expression.Right)})))";

            if (expression.NodeType == ExpressionType.Subtract && expression.Left.Type == typeof(DateTime) && expression.Right.Type == typeof(DateTime))
                return $"(({WalkExpression(expression.Left)}).getTime() - ({WalkExpression(expression.Right)}).getTime())";

            //Logical && / || must short-circuit like C# does, so emit the native JS operators
            //rather than the eager jsv_and / jsv_or helper calls. Cover bool and nullable bool
            //operands (the latter appears as a lifted comparison, e.g. (bool?) == true).
            bool IsBoolean(Type t) => t == typeof(bool) || t == typeof(bool?);

            if (expression.NodeType == ExpressionType.OrElse && IsBoolean(expression.Left.Type) && IsBoolean(expression.Right.Type))
                return $"(({WalkExpression(expression.Left)}) || ({WalkExpression(expression.Right)}))";

            if (expression.NodeType == ExpressionType.AndAlso && IsBoolean(expression.Left.Type) && IsBoolean(expression.Right.Type))
                return $"(({WalkExpression(expression.Left)}) && ({WalkExpression(expression.Right)}))";

            // C# '^' is logical XOR on bool (a true/false result) and bitwise XOR on integers.
            if (expression.NodeType == ExpressionType.ExclusiveOr && IsBoolean(expression.Left.Type) && IsBoolean(expression.Right.Type))
                return $"jsv_boolxor({WalkExpression(expression.Left)}, {WalkExpression(expression.Right)})";

            return WrapInteger(expression, $"{function}({WalkExpression(expression.Left)}, {WalkExpression(expression.Right)})");
        }

        protected virtual string AddUnary(string function, UnaryExpression expression)
            => WrapInteger(expression, $"{function}({WalkExpression(expression.Operand)})");

        // Int32 arithmetic in C# truncates on divide and wraps on overflow; JS numbers do neither.
        // Coercing the result with `| 0` reproduces both: it truncates toward zero and wraps to 32 bits.
        // Applied only to int-typed results (not long/double/decimal, where `| 0` would be wrong).
        private static string WrapInteger(Expression expression, string js)
            => expression.Type == typeof(int) ? $"(({js}) | 0)" : js;

        /// <summary>
        /// Renders a single value as the JavaScript literal the compiler would emit for it.
        ///
        /// Use it when you are building or testing JavaScript fragments by hand and need one constant
        /// formatted the same way the compiler does; it covers only the built-in types and does not
        /// apply any custom <see cref="Constants"/> registrations, so prefer a full
        /// <see cref="ExpressionCompiler"/> for real translation.
        /// </summary>
        /// <param name="constantValue">
        /// The value to render. Its runtime type selects the literal form (string, number, boolean,
        /// date, enum, ...); [c]null[/c] renders as [c]null[/c]. A type the built-ins don't
        /// recognize throws.
        /// </param>
        /// <returns>The JavaScript literal for <paramref name="constantValue"/>.</returns>
        public static string AddConstant(object constantValue)
        {
            if (TryEmitBuiltinConstant(constantValue, out string js))
                return js;
            throw new ArgumentException($"Constant type {constantValue.GetType().Name} isn't supported");
        }

        // The built-in constant emission, shared by the static entry point and the built-in
        // constant translator (so custom and built-in constants run through one uniform pipeline).
        private static bool TryEmitBuiltinConstant(object constantValue, out string js)
        {
            if (constantValue == null)
            {
                js = "null";
                return true;
            }

            if (constantValue is string || constantValue is Regex)
            {
                js = EscapeJsString(constantValue.ToString());
                return true;
            }

            if (constantValue is char c)
            {
                // A char literal used as a string-method argument (Replace('a','b'), PadLeft(5,'0'),
                // Contains('a'), ...). Char comparisons reach JS as char codes via Convert->int instead.
                js = EscapeJsString(c.ToString());
                return true;
            }

            if (constantValue is bool bv)
            {
                js = bv ? "true" : "false";
                return true;
            }

            if (constantValue is DateTime dt)
            {
                // The static path is the back-compat shim and always emits Local. The instance
                // pipeline routes DateTime through DateTimeConstantTranslator (mode-aware) first.
                js = FormatDateLiteral(dt, DateTimeMode.Local);
                return true;
            }

            if (constantValue is Enum)
            {
                // Enums emit as their underlying numeric value - the chosen JS representation,
                // consistent with the Convert(enum -> int) path that comparisons already use.
                // (Per-type name emission is available via compiler.Constants.MapConstant<T>.)
                object underlying = Convert.ChangeType(constantValue, Enum.GetUnderlyingType(constantValue.GetType()), CultureInfo.InvariantCulture);
                js = Convert.ToString(underlying, CultureInfo.InvariantCulture);
                return true;
            }

            switch (constantValue)
            {
                case int iv: js = iv.ToString(CultureInfo.InvariantCulture); return true;
                case double dv: js = dv.ToString(CultureInfo.InvariantCulture); return true;
                case decimal dcv: js = dcv.ToString(CultureInfo.InvariantCulture); return true;
                case long lv: js = lv.ToString(CultureInfo.InvariantCulture); return true;
                case short sv: js = sv.ToString(CultureInfo.InvariantCulture); return true;
                case float fv: js = fv.ToString(CultureInfo.InvariantCulture); return true;
                case TimeSpan ts: js = ts.TotalMilliseconds.ToString(CultureInfo.InvariantCulture); return true;
            }

            js = null;
            return false;
        }

        // Emits a constant value: custom translators first, then the built-ins; both via IConstantTranslator.
        private string EmitConstant(object value)
        {
            foreach (IConstantTranslator translator in mUserConstants)
                if (translator.TryTranslate(value, mEmitContext, out string custom))
                    return custom;

            foreach (IConstantTranslator translator in sBuiltinConstants)
                if (translator.TryTranslate(value, mEmitContext, out string builtin))
                    return builtin;

            throw new ArgumentException($"Constant type {value?.GetType().Name} isn't supported");
        }

        private sealed class BuiltinConstantTranslator : IConstantTranslator
        {
            public bool TryTranslate(object value, IExpressionEmitContext context, out string js) => TryEmitBuiltinConstant(value, out js);
        }

        /// <summary>
        /// Renders a fixed date as the JavaScript Date literal the compiler would emit for it.
        ///
        /// Use it when hand-authoring a JavaScript fragment that must agree with compiled
        /// expressions on date framing - call it with the same <see cref="DateMode"/> you give the
        /// compiler so both sides build dates the same way.
        /// </summary>
        /// <param name="dt">
        /// The date to render. A midnight value emits a date-only constructor; any non-zero time
        /// component emits the longer form down to seconds. The value's
        /// <see cref="DateTime.Kind"/> is ignored - <paramref name="mode"/> alone decides the frame.
        /// </param>
        /// <param name="mode">
        /// Which frame to build the date in: <see cref="DateTimeMode.Local"/> for a local-time
        /// constructor, <see cref="DateTimeMode.Utc"/> for a UTC one. This must match how the rest
        /// of the page interprets dates or the literal will be off by the client's offset.
        /// </param>
        /// <returns>The JavaScript [c]Date[/c] construction expression.</returns>
        public static string FormatDateLiteral(DateTime dt, DateTimeMode mode)
        {
            bool dateOnly = dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0;
            string args = dateOnly
                ? string.Format(CultureInfo.InvariantCulture, "{0:D},{1:D},{2:D}", dt.Year, dt.Month - 1, dt.Day)
                : string.Format(CultureInfo.InvariantCulture, "{0:D},{1:D},{2:D},{3:D},{4:D},{5:D}", dt.Year, dt.Month - 1, dt.Day, dt.Hour, dt.Minute, dt.Second);
            return mode == DateTimeMode.Utc ? $"new Date(Date.UTC({args}))" : $"new Date({args})";
        }

        /// <summary>
        /// Turns a CLR string into a safe, quoted JavaScript string literal.
        ///
        /// Use it from a custom translator (or when assembling JavaScript by hand) whenever you
        /// splice user- or data-supplied text into emitted code, so quotes, backslashes, and control
        /// characters can't break or inject into the output.
        /// </summary>
        /// <param name="value">
        /// The text to embed. Its characters are escaped as needed; the result already includes the
        /// surrounding quotes, so do not add your own.
        /// </param>
        /// <returns>A single-quoted JavaScript string literal equivalent to <paramref name="value"/>.</returns>
        public static string EscapeJsString(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length + 2);
            sb.Append('\'');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\'': sb.Append("\\'"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('\'');
            return sb.ToString();
        }

        /// <summary>
        /// Prepares a regular-expression pattern for use inside a JavaScript regex literal.
        ///
        /// Use it from a custom translator that emits a regex literal so an unescaped [c]/[/c] or a
        /// raw newline in the pattern doesn't terminate or corrupt the literal.
        /// </summary>
        /// <param name="pattern">
        /// The regex source. Only the characters that would break a slash-delimited literal are
        /// adjusted; existing backslash escapes are respected and left intact. The result is the
        /// inner pattern text only - wrap it in the slashes (and any flags) yourself.
        /// </param>
        /// <returns>The pattern, safe to place between [c]/[/c] delimiters.</returns>
        public static string EscapeJsRegex(string pattern)
        {
            StringBuilder sb = new StringBuilder(pattern.Length);
            bool escaped = false;
            foreach (char c in pattern)
            {
                if (c == '/' && !escaped)
                    sb.Append("\\/");
                else if (c == '\n' && !escaped)
                    sb.Append("\\n");
                else if (c == '\r' && !escaped)
                    sb.Append("\\r");
                else
                    sb.Append(c);
                escaped = (c == '\\') && !escaped;
            }
            return sb.ToString();
        }

        protected virtual string AddConstant(ConstantExpression expression) => EmitConstant(expression.Value);

        protected virtual string AddMemberAccess(MemberExpression expression)
        {
            foreach (IMemberTranslator translator in mUserMembers)
                if (translator.TryTranslate(expression, mEmitContext, out string custom))
                    return custom;

            foreach (IMemberTranslator translator in sBuiltinMembers)
                if (translator.TryTranslate(expression, mEmitContext, out string builtin))
                    return builtin;

            throw new ArgumentException($"Unexpected property of {expression.Member?.DeclaringType?.Name}.{expression.Member?.Name}", nameof(expression));
        }

        protected bool GetExpressionValue(Expression expression, out object returnValue)
        {
            returnValue = null;

            // A subtree that references a free parameter (one not bound by a lambda inside it), or an
            // ambient-now accessor (DateTime.Now/Today/UtcNow), cannot be precomputed to a constant -
            // the latter must stay dynamic so it evaluates at validation time on the client. Detect
            // that cheaply and skip the expensive compile+invoke (also avoids a failed JIT compile and
            // a swallowed exception at every parameter-referencing node, the common case).
            if (ContainsNonConstant(expression))
                return false;

            try
            {
                Delegate compiled = Expression.Lambda(expression).Compile();
                returnValue = compiled.DynamicInvoke();
                return true;
            }
            catch (Exception)
            {
                // A closed expression that still throws at evaluation (e.g. divide by zero) is
                // left for structural translation, exactly as before.
                returnValue = null;
                return false;
            }
        }

        private static bool ContainsNonConstant(Expression expression)
        {
            NonConstantFinder finder = new NonConstantFinder();
            finder.Visit(expression);
            return finder.Found;
        }

        // Detects whether a subtree cannot be precomputed at compile time: it references a free
        // parameter (one not bound by a lambda inside the subtree - lambda-bound params like the 'c'
        // in arr.Any(c => ...) do not count), or an ambient-now accessor (DateTime.Now/Today/UtcNow)
        // which must stay dynamic. Other closed sub-expressions still fold to constants as before.
        private sealed class NonConstantFinder : ExpressionVisitor
        {
            private readonly HashSet<ParameterExpression> mBound = new HashSet<ParameterExpression>();

            public bool Found { get; private set; }

            public override Expression Visit(Expression node)
            {
                return Found ? node : base.Visit(node); // short-circuit once something non-constant is seen
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                foreach (ParameterExpression p in node.Parameters)
                    mBound.Add(p);
                Expression result = base.VisitLambda(node);
                foreach (ParameterExpression p in node.Parameters)
                    mBound.Remove(p);
                return result;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (!mBound.Contains(node))
                    Found = true;
                return node;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (IsAmbientNow(node.Member))
                {
                    Found = true;
                    return node;
                }
                return base.VisitMember(node);
            }

            private static bool IsAmbientNow(System.Reflection.MemberInfo member)
                => member.DeclaringType == typeof(DateTime)
                   && (member.Name == nameof(DateTime.Now) || member.Name == nameof(DateTime.Today) || member.Name == nameof(DateTime.UtcNow));
        }

        protected bool IsExpressionRootsInParameter(Expression expression)
        {
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = (MemberExpression)expression;
                if (memberExpression.Expression.NodeType == ExpressionType.Parameter)
                    return true;
                else
                    return IsExpressionRootsInParameter(memberExpression.Expression);
            }
            else if (expression.NodeType == ExpressionType.ArrayIndex)
            {
                BinaryExpression binaryExpression = (BinaryExpression)expression;
                if (binaryExpression.Left.NodeType == ExpressionType.Parameter)
                    return true;
                else
                    return IsExpressionRootsInParameter(binaryExpression.Left);
            }
            else if (expression.NodeType == ExpressionType.Call)
            {
                MethodCallExpression callExpression = (MethodCallExpression)expression;
                if (callExpression.Method.Name == "get_Item" && callExpression.Arguments.Count == 1)
                    return true;
            }
            return false;
        }

        protected virtual string AddParameterAccess(Expression expression)
        {
            ParameterExpression root = GetRootParameter(expression);
            if (root != null && IsRootParameter(root) && TryGetParameterBinding(root.Type, out ParameterBinding binding))
                return binding.Access != null ? binding.Access(expression, root) : DefaultReferenceParameterAccess(expression);

            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = (MemberExpression)expression;
                return $"{AddParameterAccess(memberExpression.Expression)}.{memberExpression.Member.Name}";
            }
            else if (expression.NodeType == ExpressionType.ArrayIndex)
            {
                BinaryExpression binaryExpression = (BinaryExpression)expression;
                return $"jsv_index({AddParameterAccess(binaryExpression.Left)}, {WalkExpression(binaryExpression.Right)})";
            }
            else if (expression.NodeType == ExpressionType.Call)
            {
                MethodCallExpression callExpression = (MethodCallExpression)expression;
                if (callExpression.Method.Name == "get_Item" && callExpression.Arguments.Count == 1)
                    return $"jsv_index({AddParameterAccess(callExpression.Object)}, {WalkExpression(callExpression.Arguments[0])})";
            }
            else if (expression.NodeType == ExpressionType.Parameter)
                return ((ParameterExpression)expression).Name;

            throw new InvalidOperationException($"Unexpected expression for the parameter access {expression.NodeType}");
        }

        protected virtual string AddArrayIndex(BinaryExpression expression)
        {
            if (IsExpressionRootsInParameter(expression))
                return AddParameterAccess(expression);

            return $"jsv_index({WalkExpression(expression.Left)}, {WalkExpression(expression.Right)})";
        }

        protected virtual string AddLambdaParameter(LambdaExpression expression)
        {
            StringBuilder lambda = new StringBuilder();
            lambda.Append("function (");
            for (int i = 0; i < (expression.Parameters?.Count ?? 0); i++)
            {
                if (i > 0)
                    lambda.Append(", ");
                lambda.Append(expression.Parameters[i].Name);
            }

            lambda.Append(") { return (");
            lambda.Append(WalkExpression(expression.Body));
            lambda.Append(");}");
            return lambda.ToString();
        }

        protected virtual string AddCall(MethodCallExpression expression)
        {
            // User registrations first (deliberate-override escape hatch), then the disjoint built-ins.
            foreach (IMethodCallTranslator translator in mUserTranslators)
                if (translator.TryTranslate(expression, mEmitContext, out string custom))
                    return custom;

            foreach (IMethodCallTranslator translator in sBuiltinTranslators)
                if (translator.TryTranslate(expression, mEmitContext, out string builtin))
                    return builtin;

            throw new ArgumentException($"Unexpected call of {expression.Method?.DeclaringType?.Name}.{expression.Method?.Name}", nameof(expression));
        }

        // Adapts the (overridable) compiler helpers to the emit-context the translators consume.
        private sealed class EmitContext : IExpressionEmitContext
        {
            private readonly ExpressionCompiler mCompiler;

            public EmitContext(ExpressionCompiler compiler) => mCompiler = compiler;

            public string Emit(Expression expression) => mCompiler.WalkExpression(expression);
            public bool TryEvaluate(Expression expression, out object value) => mCompiler.GetExpressionValue(expression, out value);
            public string EmitPredicate(LambdaExpression lambda) => mCompiler.AddLambdaParameter(lambda);
            public string EmitParameterAccess(Expression expression) => mCompiler.AddParameterAccess(expression);
            public bool RootsInParameter(Expression expression) => mCompiler.IsExpressionRootsInParameter(expression);
            public string EscapeString(string value) => EscapeJsString(value);
            public string EscapeRegex(string value) => EscapeJsRegex(value);
            public DateTimeMode DateMode => mCompiler.DateMode;
        }

        // Additive, leaf-level registry. Registrations land in the user list (consulted before
        // built-ins); built-ins themselves are never exposed for mutation.
        private sealed class MethodRegistry : IJsMethodRegistry
        {
            private readonly List<IMethodCallTranslator> mTarget;

            public MethodRegistry(List<IMethodCallTranslator> target) => mTarget = target;

            public IJsMethodRegistry AddTranslator(IMethodCallTranslator translator)
            {
                mTarget.Add(translator ?? throw new ArgumentNullException(nameof(translator)));
                return this;
            }

            public IJsMethodRegistry MapMethod(Type declaringType, string name, string template, int? argCount = null)
            {
                mTarget.Add(new TableMethodTranslator().Map(declaringType, name, argCount, null, template));
                return this;
            }

            public IJsMethodRegistry MapMethod(System.Reflection.MethodInfo method, string template)
            {
                if (method == null) throw new ArgumentNullException(nameof(method));
                mTarget.Add(new TableMethodTranslator().Map(method.DeclaringType, method.Name, method.GetParameters().Length, !method.IsStatic, template));
                return this;
            }
        }

        private sealed class ConstantRegistry : IJsConstantRegistry
        {
            private readonly List<IConstantTranslator> mTarget;

            public ConstantRegistry(List<IConstantTranslator> target) => mTarget = target;

            public IJsConstantRegistry AddTranslator(IConstantTranslator translator)
            {
                mTarget.Add(translator ?? throw new ArgumentNullException(nameof(translator)));
                return this;
            }

            public IJsConstantRegistry MapConstant<T>(Func<T, string> emit)
            {
                if (emit == null) throw new ArgumentNullException(nameof(emit));
                mTarget.Add(new DelegateConstantTranslator(typeof(T), value => emit((T)value)));
                return this;
            }
        }

        private sealed class MemberRegistry : IJsMemberRegistry
        {
            private readonly List<IMemberTranslator> mTarget;

            public MemberRegistry(List<IMemberTranslator> target) => mTarget = target;

            public IJsMemberRegistry AddTranslator(IMemberTranslator translator)
            {
                mTarget.Add(translator ?? throw new ArgumentNullException(nameof(translator)));
                return this;
            }

            public IJsMemberRegistry MapMember(Type declaringType, string name, string template)
            {
                mTarget.Add(new TableMemberTranslator().Map(declaringType, name, template));
                return this;
            }
        }

        // One registered parameter binding: a type predicate plus how to render the bare parameter
        // and a chain rooted in it. Null funcs mean "use the built-in reference() rendering".
        private sealed class ParameterBinding
        {
            public readonly Func<Type, bool> Matches;
            public readonly Func<ParameterExpression, string> Parameter;
            public readonly Func<Expression, ParameterExpression, string> Access;

            public ParameterBinding(Func<Type, bool> matches, Func<ParameterExpression, string> parameter, Func<Expression, ParameterExpression, string> access)
            {
                Matches = matches;
                Parameter = parameter;
                Access = access;
            }
        }

        private sealed class ParameterRegistry : IJsParameterRegistry
        {
            private readonly List<ParameterBinding> mTarget;

            public ParameterRegistry(List<ParameterBinding> target) => mTarget = target;

            public IJsParameterRegistry MapReference(Func<Type, bool> matches)
            {
                mTarget.Add(new ParameterBinding(matches ?? throw new ArgumentNullException(nameof(matches)), null, null));
                return this;
            }

            public IJsParameterRegistry Map(Func<Type, bool> matches, Func<ParameterExpression, string> parameter, Func<Expression, ParameterExpression, string> parameterAccess)
            {
                if (matches == null) throw new ArgumentNullException(nameof(matches));
                if (parameter == null) throw new ArgumentNullException(nameof(parameter));
                if (parameterAccess == null) throw new ArgumentNullException(nameof(parameterAccess));
                mTarget.Add(new ParameterBinding(matches, parameter, parameterAccess));
                return this;
            }
        }

        // Built-in member translators. Disjoint by type+name, then the terminal parameter-access
        // fallback (a typed member like DateTime.Year deliberately wins over the generic fallback).
        private static IReadOnlyList<IMemberTranslator> BuildBuiltinMemberTranslators()
        {
            TableMemberTranslator table = new TableMemberTranslator();

            // DateTime.* member reads are mode-aware -> handled by DateTimeMemberTranslator below.
            table.Map(typeof(TimeSpan), nameof(TimeSpan.TotalMilliseconds), "($obj)")
                 .Map(typeof(TimeSpan), nameof(TimeSpan.TotalSeconds), "($obj / 1000.0)")
                 .Map(typeof(TimeSpan), nameof(TimeSpan.TotalMinutes), "($obj / 60000.0)")
                 .Map(typeof(TimeSpan), nameof(TimeSpan.TotalHours), "($obj / 3600000.0)")
                 .Map(typeof(TimeSpan), nameof(TimeSpan.TotalDays), "($obj / 86400000.0)");

            // Length/Count on any type -> jsv_length.
            table.Map(null, nameof(Array.Length), "jsv_length($obj)")
                 .Map(null, nameof(List<object>.Count), "jsv_length($obj)");

            return new IMemberTranslator[]
            {
                new DateTimeMemberTranslator(),
                table,
                new NullableMemberTranslator(),
                new ParameterAccessMemberTranslator(),
            };
        }

        // The fixed built-in method translators. Disjoint by type+name(+arity/shape) - e.g.
        // string.Contains (type string) and CollectionContainsTranslator (excludes string) never
        // both match - so iteration order is not load-bearing.
        private static IReadOnlyList<IMethodCallTranslator> BuildBuiltinTranslators()
        {
            TableMethodTranslator table = new TableMethodTranslator();

            table.Map(typeof(Math), nameof(Math.Abs), 1, false, "Math.abs($0)")
                 .Map(typeof(Math), nameof(Math.Sqrt), 1, false, "Math.sqrt($0)")
                 .Map(typeof(Math), nameof(Math.Round), 1, false, "jsv_round($0)")
                 .Map(typeof(Math), nameof(Math.Round), 2, false, "jsv_round($0, $1)", c => c.Arguments[1].Type == typeof(int))
                 .Map(typeof(Math), nameof(Math.Floor), 1, false, "Math.floor($0)")
                 .Map(typeof(Math), nameof(Math.Ceiling), 1, false, "Math.ceil($0)")
                 .Map(typeof(Math), nameof(Math.Truncate), 1, false, "jsv_trunc($0)")
                 .Map(typeof(Math), nameof(Math.Sign), 1, false, "jsv_sign($0)")
                 .Map(typeof(Math), nameof(Math.Max), 2, false, "Math.max($0, $1)")
                 .Map(typeof(Math), nameof(Math.Min), 2, false, "Math.min($0, $1)")
                 .Map(typeof(Math), nameof(Math.Log), 1, false, "Math.log($0)")
                 .Map(typeof(Math), nameof(Math.Exp), 1, false, "Math.exp($0)")
                 .Map(typeof(Math), nameof(Math.Pow), 2, false, "Math.pow($0, $1)")
                 .Map(typeof(Math), nameof(Math.Sin), 1, false, "Math.sin($0)")
                 .Map(typeof(Math), nameof(Math.Cos), 1, false, "Math.cos($0)")
                 .Map(typeof(Math), nameof(Math.Tan), 1, false, "Math.tan($0)")
                 .Map(typeof(Math), nameof(Math.Sinh), 1, false, "Math.sinh($0)")
                 .Map(typeof(Math), nameof(Math.Cosh), 1, false, "Math.cosh($0)")
                 .Map(typeof(Math), nameof(Math.Tanh), 1, false, "Math.tanh($0)")
                 .Map(typeof(Math), nameof(Math.Asin), 1, false, "Math.asin($0)")
                 .Map(typeof(Math), nameof(Math.Acos), 1, false, "Math.acos($0)")
                 .Map(typeof(Math), nameof(Math.Atan), 1, false, "Math.atan($0)");

            table.Map(typeof(char), nameof(char.IsUpper), 1, false, "jsv_isUpperCase($0)")
                 .Map(typeof(char), nameof(char.IsLower), 1, false, "jsv_isLowerCase($0)")
                 .Map(typeof(char), nameof(char.IsDigit), 1, false, "jsv_isDigit($0)")
                 .Map(typeof(char), nameof(char.IsLetter), 1, false, "jsv_isLetter($0)")
                 .Map(typeof(char), nameof(char.IsLetterOrDigit), 1, false, "jsv_isLetterOrDigit($0)")
                 .Map(typeof(char), nameof(char.IsPunctuation), 1, false, "jsv_isPunctuation($0)")
                 .Map(typeof(char), nameof(char.IsControl), 1, false, "jsv_isControl($0)")
                 .Map(typeof(char), nameof(char.IsWhiteSpace), 1, false, "jsv_isWhiteSpace($0)");

            table.Map(typeof(DateTime), nameof(DateTime.AddDays), 1, true, "(new Date($obj.getTime() + ($0) * 86400000))")
                 .Map(typeof(DateTime), nameof(DateTime.AddHours), 1, true, "(new Date($obj.getTime() + ($0) * 3600000))")
                 .Map(typeof(DateTime), nameof(DateTime.AddMinutes), 1, true, "(new Date($obj.getTime() + ($0) * 60000))")
                 .Map(typeof(DateTime), nameof(DateTime.AddSeconds), 1, true, "(new Date($obj.getTime() + ($0) * 1000))");

            table.Map(typeof(string), nameof(string.IsNullOrEmpty), 1, false, "jsv_isempty($0)")
                 .Map(typeof(string), nameof(string.IsNullOrWhiteSpace), 1, false, "jsv_isemptyorwhitespace($0)")
                 .Map(typeof(string), nameof(string.Trim), 0, true, "jsv_trim($obj)")
                 .Map(typeof(string), nameof(string.ToUpper), 0, true, "jsv_upper($obj)")
                 .Map(typeof(string), nameof(string.ToLower), 0, true, "jsv_lower($obj)")
                 .Map(typeof(string), nameof(string.Contains), 1, true, "(($obj).indexOf($0) >= 0)")
                 .Map(typeof(string), nameof(string.StartsWith), 1, true, "(($obj).indexOf($0) == 0)")
                 .Map(typeof(string), nameof(string.Substring), 1, true, "($obj).substr($0)")
                 .Map(typeof(string), nameof(string.Substring), 2, true, "($obj).substr($0, $1)")
                 .Map(typeof(string), nameof(string.EndsWith), 1, true, "jsv_endswith($obj, $0)")
                 .Map(typeof(string), nameof(string.Replace), 2, true, "jsv_replace($obj, $0, $1)")
                 .Map(typeof(string), nameof(string.PadLeft), 1, true, "jsv_padleft($obj, $0, ' ')")
                 .Map(typeof(string), nameof(string.PadLeft), 2, true, "jsv_padleft($obj, $0, $1)")
                 .Map(typeof(string), nameof(string.PadRight), 1, true, "jsv_padright($obj, $0, ' ')")
                 .Map(typeof(string), nameof(string.PadRight), 2, true, "jsv_padright($obj, $0, $1)")
                 .Map(typeof(string), nameof(string.TrimStart), 0, true, "jsv_trimstart($obj)")
                 .Map(typeof(string), nameof(string.TrimEnd), 0, true, "jsv_trimend($obj)")
                 .Map(typeof(string), nameof(string.ToUpperInvariant), 0, true, "jsv_upper($obj)")
                 .Map(typeof(string), nameof(string.ToLowerInvariant), 0, true, "jsv_lower($obj)")
                 .Map(typeof(string), nameof(string.LastIndexOf), 1, true, "(($obj).lastIndexOf($0))")
                 .Map(typeof(string), "get_Chars", 1, true, "jsv_index($obj, $0)");

            // DaysSince is epoch-based (mode-independent); MonthsSince/YearsSince are calendar-based
            // and mode-aware -> handled by DateDiffTranslator.
            table.Map(typeof(Functions), nameof(Functions.DaysSince), 2, false, "jsv_dayssince($0, $1)")
                 .Map(typeof(Functions), nameof(Functions.IsCreditCardNumberCorrect), 1, false, "jsv_ccn_valid($0)")
                 .Map(typeof(Functions), nameof(Functions.ToBool), 1, false, "jsv_string2bool($0)")
                 .Map(typeof(Functions), nameof(Functions.ToInt), 1, false, "jsv_string2int($0)")
                 .Map(typeof(Functions), nameof(Functions.IsNull), 1, false, "(($0) == null)")
                 .Map(typeof(Functions), nameof(Functions.IsNullOrEmpty), 1, false, "jsv_isempty($0)")
                 .Map(typeof(Functions), nameof(Functions.IsNotNull), 1, false, "(($0) != null)")
                 .Map(typeof(Functions), nameof(Functions.IsNotNullOrEmpty), 1, false, "jsv_not(jsv_isempty($0))")
                 .Map(typeof(Functions), nameof(Functions.Fractional), 1, false, "jsv_fractional($0)");

            return new IMethodCallTranslator[]
            {
                table,
                new RegexIsMatchTranslator(),
                new StringIndexOfTranslator(),
                new StringStartsWithComparisonTranslator(),
                new StringEndsWithComparisonTranslator(),
                new DateDiffTranslator(),
                new CollectionContainsTranslator(),
                new LinqTranslator(),
                new IndexerTranslator(),
                new ToStringTranslator(),
            };
        }

        protected virtual string AddConvert(UnaryExpression expression)
        {
            if (expression.Type == typeof(string))
                return $"jsv_tostring({WalkExpression(expression.Operand)})";
            else if (expression.Type == typeof(bool) && expression.Operand.Type == typeof(string))
                return $"jsv_string2bool({WalkExpression(expression.Operand)})";
            else if (expression.Type == typeof(int) && expression.Operand.Type == typeof(string))
                return $"jsv_string2int({WalkExpression(expression.Operand)})";
            else if (expression.Type == typeof(double) && expression.Operand.Type == typeof(string))
                return $"jsv_string2n({WalkExpression(expression.Operand)})";
            else if (expression.Type == typeof(float) && expression.Operand.Type == typeof(string))
                return $"jsv_string2n({WalkExpression(expression.Operand)})";
            else
                return $"({WalkExpression(expression.Operand)})";
        }

        protected virtual string AddParameter(ParameterExpression parameterExpression)
        {
            if (IsRootParameter(parameterExpression) && TryGetParameterBinding(parameterExpression.Type, out ParameterBinding binding))
                return binding.Parameter != null ? binding.Parameter(parameterExpression) : "reference()";
            return parameterExpression.Name;
        }

        /// <summary>
        /// Builds the dotted member path of a parameter access, for use inside a custom parameter
        /// binding.
        ///
        /// Turns a member chain rooted in a rule parameter into a path - [c]m.Address.PostalCode[/c]
        /// becomes [c]Address.PostalCode[/c], and the bare parameter becomes an empty string. Call it
        /// from the [c]parameterAccess[/c] function you pass to
        /// <see cref="IJsParameterRegistry.Map"/> so you don't have to walk the expression yourself.
        /// </summary>
        /// <param name="access">The member chain rooted in the parameter (as handed to your function).</param>
        public static string ParameterAccessPath(Expression access)
        {
            if (access == null)
                throw new ArgumentNullException(nameof(access));
            if (access.NodeType == ExpressionType.Parameter)
                return "";
            if (access.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression member = (MemberExpression)access;
                string head = ParameterAccessPath(member.Expression);
                return head.Length == 0 ? member.Member.Name : head + "." + member.Member.Name;
            }
            throw new InvalidOperationException($"Cannot build a parameter access path for {access.NodeType}.");
        }

        // A parameter binding applies only to the rule's own (top-level) parameters; parameters
        // introduced by a nested LINQ lambda are not bound and keep their own name.
        private bool IsRootParameter(ParameterExpression parameter)
            => mExpression.Parameters.Contains(parameter);

        private bool TryGetParameterBinding(Type type, out ParameterBinding binding)
        {
            for (int i = 0; i < mParameterBindings.Count; i++)
            {
                if (mParameterBindings[i].Matches(type))
                {
                    binding = mParameterBindings[i];
                    return true;
                }
            }
            binding = null;
            return false;
        }

        // Walks a member/index chain down to the parameter it roots in (null if it doesn't root in one).
        private static ParameterExpression GetRootParameter(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                    return (ParameterExpression)expression;
                case ExpressionType.MemberAccess:
                    MemberExpression member = (MemberExpression)expression;
                    return member.Expression == null ? null : GetRootParameter(member.Expression);
                case ExpressionType.ArrayIndex:
                    return GetRootParameter(((BinaryExpression)expression).Left);
                case ExpressionType.Call:
                    MethodCallExpression call = (MethodCallExpression)expression;
                    return call.Method.Name == "get_Item" && call.Object != null ? GetRootParameter(call.Object) : null;
                default:
                    return null;
            }
        }

        // Built-in reference() rendering used when a binding is registered via MapReference (null funcs).
        private string DefaultReferenceParameterAccess(Expression expression) => DefaultReferenceParameterAccess(expression, true);

        private string DefaultReferenceParameterAccess(Expression expression, bool initial)
        {
            string result = null;
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression member = (MemberExpression)expression;
                result = DefaultReferenceParameterAccess(member.Expression, false);
                if (result != "")
                    result += ".";
                result += member.Member.Name;
            }
            else if (expression.NodeType == ExpressionType.ArrayIndex)
            {
                BinaryExpression binary = (BinaryExpression)expression;
                return $"jsv_index({DefaultReferenceParameterAccess(binary.Left)}, {WalkExpression(binary.Right)})";
            }
            else if (expression.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = (MethodCallExpression)expression;
                if (call.Method.Name == "get_Item" && call.Arguments.Count == 1)
                    return $"jsv_index({DefaultReferenceParameterAccess(call.Object)}, {WalkExpression(call.Arguments[0])})";
            }
            else if (expression.NodeType == ExpressionType.Parameter)
            {
                return "";
            }

            if (initial)
                result = $"reference('{result}')";
            return result;
        }

        /// <summary>
        /// Tells whether two compilers were built from the same lambda.
        ///
        /// Useful as a cache key when you memoize emitted JavaScript and want to avoid recompiling
        /// the same rule. Equality is by the underlying expression, not by the emitted string or
        /// configuration.
        /// </summary>
        /// <param name="other">The compiler to compare against; [c]null[/c] is never equal.</param>
        /// <returns>[c]true[/c] when both wrap the same lambda expression.</returns>
        public bool Equals(ExpressionCompiler other)
        {
            if (other == null)
                return false;
            return object.Equals(mExpression, other.mExpression);
        }

        /// <summary>
        /// Tells whether this compiler was built from the given lambda.
        ///
        /// The same caching/identity use as the other overload, for when what you hold is the raw
        /// expression rather than a compiler.
        /// </summary>
        /// <param name="other">The expression to compare against this compiler's lambda; [c]null[/c] is never equal.</param>
        /// <returns>[c]true[/c] when this compiler wraps <paramref name="other"/>.</returns>
        public bool Equals(Expression other)
        {
            if (other == null)
                return false;
            return object.Equals(mExpression, other);
        }
    }
}