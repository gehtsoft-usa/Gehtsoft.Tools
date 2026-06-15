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

        // Built-ins are disjoint by type+name (at most one matches any node), shared across instances
        // and never mutated. The member list ends with the terminal parameter-access fallback.
        private static readonly IReadOnlyList<IMethodCallTranslator> sBuiltinTranslators = BuildBuiltinTranslators();
        private static readonly IReadOnlyList<IConstantTranslator> sBuiltinConstants = new IConstantTranslator[] { new DateTimeConstantTranslator(), new BuiltinConstantTranslator() };
        private static readonly IReadOnlyList<IMemberTranslator> sBuiltinMembers = BuildBuiltinMemberTranslators();

        public Type ReturnType { get; }

        /// <summary>
        /// How DateTime values are framed in the emitted JavaScript - <see cref="DateTimeMode.Local"/>
        /// (default) or <see cref="DateTimeMode.Utc"/>. Set before reading <see cref="JavaScriptExpression"/>.
        /// The host must bind JS-side dates in the same frame (see the library docs / PLAN.md).
        /// </summary>
        public DateTimeMode DateMode { get; set; } = DateTimeMode.Local;

        /// <summary>Extension point for translating custom method calls to JavaScript.</summary>
        public IJsMethodRegistry Methods => mMethodRegistry;

        /// <summary>Extension point for translating custom constant types to JavaScript.</summary>
        public IJsConstantRegistry Constants => mConstantRegistry;

        /// <summary>Extension point for translating custom member/property access to JavaScript.</summary>
        public IJsMemberRegistry Members => mMemberRegistry;

        public string JavaScriptExpression
        {
            get
            {
                return mJavaScriptExpression ?? (mJavaScriptExpression = WalkExpression(mExpression.Body));
            }
        }

        public ExpressionCompiler(LambdaExpression lambdaExpression)
            : this(lambdaExpression, DateTimeMode.Local)
        {
        }

        public ExpressionCompiler(LambdaExpression lambdaExpression, DateTimeMode dateMode)
        {
            mExpression = lambdaExpression;
            ReturnType = lambdaExpression.ReturnType;
            DateMode = dateMode;
            mEmitContext = new EmitContext(this);
            mMethodRegistry = new MethodRegistry(mUserTranslators);
            mConstantRegistry = new ConstantRegistry(mUserConstants);
            mMemberRegistry = new MemberRegistry(mUserMembers);
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

        // Back-compat entry point: emits the built-in constant types (no custom registrations).
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
        /// Render a DateTime literal in the requested frame: Local -> new Date(y, m, d, ...),
        /// Utc -> new Date(Date.UTC(y, m, d, ...)). DateTimeKind is ignored; the mode governs.
        /// </summary>
        public static string FormatDateLiteral(DateTime dt, DateTimeMode mode)
        {
            bool dateOnly = dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0;
            string args = dateOnly
                ? string.Format(CultureInfo.InvariantCulture, "{0:D},{1:D},{2:D}", dt.Year, dt.Month - 1, dt.Day)
                : string.Format(CultureInfo.InvariantCulture, "{0:D},{1:D},{2:D},{3:D},{4:D},{5:D}", dt.Year, dt.Month - 1, dt.Day, dt.Hour, dt.Minute, dt.Second);
            return mode == DateTimeMode.Utc ? $"new Date(Date.UTC({args}))" : $"new Date({args})";
        }

        /// <summary>Render a CLR string as a valid single-quoted JavaScript string literal.</summary>
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

        /// <summary>Escape a regex pattern so it is safe inside a JavaScript /.../ literal.</summary>
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

            // A subtree that references a free parameter (one not bound by a lambda inside it)
            // can never reduce to a constant. Detect that cheaply and skip the expensive
            // compile+invoke entirely - this avoids a failed JIT compile and a swallowed
            // exception at every parameter-referencing node, which is the common case.
            if (ReferencesFreeParameter(expression))
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

        private static bool ReferencesFreeParameter(Expression expression)
        {
            FreeParameterFinder finder = new FreeParameterFinder();
            finder.Visit(expression);
            return finder.Found;
        }

        // Detects a reference to a parameter that is not bound by a lambda within the visited
        // subtree (a free variable). Lambda-bound parameters (e.g. the 'c' in arr.Any(c => ...))
        // do not count, so closed sub-expressions still fold to constants as they did before.
        private sealed class FreeParameterFinder : ExpressionVisitor
        {
            private readonly HashSet<ParameterExpression> mBound = new HashSet<ParameterExpression>();

            public bool Found { get; private set; }

            public override Expression Visit(Expression node)
            {
                return Found ? node : base.Visit(node); // short-circuit once a free parameter is seen
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
            return parameterExpression.Name;
        }

        public bool Equals(ExpressionCompiler other)
        {
            if (other == null)
                return false;
            return object.Equals(mExpression, other.mExpression);
        }

        public bool Equals(Expression other)
        {
            if (other == null)
                return false;
            return object.Equals(mExpression, other);
        }
    }
}