using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Gehtsoft.ExpressionToJs
{
    public class ExpressionCompiler : IEquatable<ExpressionCompiler>, IEquatable<Expression>
    {
        private readonly Type mReturnType;
        private LambdaExpression mExpression;
        private string mJavaScriptExpression = null;

        public Type ReturnType => mReturnType;

        public string JavaScriptExpression
        {
            get
            {
                if (mJavaScriptExpression == null)
                    mJavaScriptExpression = WalkExpression(mExpression.Body);
                return mJavaScriptExpression;
            }
        }

        public ExpressionCompiler(LambdaExpression lambdaExpression)
        {
            mExpression = lambdaExpression;
            mReturnType = lambdaExpression.ReturnType;
        }

        protected virtual string WalkExpression(Expression expression)
        {
            if (expression.CanReduce)
                expression = expression.Reduce();

            if (GetExpressionValue(expression, out object value))
                return AddConstant(value);

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

        protected virtual string AddBinary(string function, BinaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Add && expression.Left.Type == typeof(DateTime) && expression.Right.Type == typeof(TimeSpan))
                return $"(new Date(({WalkExpression(expression.Left)}).getTime() + ({WalkExpression(expression.Right)})))";

            if (expression.NodeType == ExpressionType.Subtract && expression.Left.Type == typeof(DateTime) && expression.Right.Type == typeof(TimeSpan))
                return $"(new Date(({WalkExpression(expression.Left)}).getTime() - ({WalkExpression(expression.Right)})))";

            if (expression.NodeType == ExpressionType.Subtract && expression.Left.Type == typeof(DateTime) && expression.Right.Type == typeof(DateTime))
                return $"(({WalkExpression(expression.Left)}).getTime() - ({WalkExpression(expression.Right)}).getTime())";

            if (expression.NodeType == ExpressionType.OrElse && expression.Left.Type == typeof(bool) && expression.Right.Type == typeof(bool))
                return $"(({WalkExpression(expression.Left)}) || ({WalkExpression(expression.Right)}))";

            if (expression.NodeType == ExpressionType.AndAlso && expression.Left.Type == typeof(bool) && expression.Right.Type == typeof(bool))
                return $"(({WalkExpression(expression.Left)}) && ({WalkExpression(expression.Right)}))";

            return $"{function}({WalkExpression(expression.Left)}, {WalkExpression(expression.Right)})";
        }

        protected virtual string AddUnary(string function, UnaryExpression expression) => $"{function}({WalkExpression(expression.Operand)})";

        public static string AddConstant(object constantValue)
        {
            if (constantValue == null)
                return "null";

            if (constantValue is string || constantValue is Regex)
                return $"'{constantValue.ToString()}'";

            if (constantValue is bool)
                return (bool)constantValue ? "true" : "false";

            if (constantValue is DateTime)
            {
                DateTime dt = (DateTime)constantValue;
                if (dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0)
                    return string.Format("new Date({0:D},{1:D},{2:D})", dt.Year, dt.Month - 1, dt.Day);
                else
                    return string.Format("new Date({0:D},{1:D},{2:D},{3:D},{4:D},{5:D})", dt.Year, dt.Month - 1, dt.Day, dt.Hour, dt.Minute, dt.Second);
            }

            if (constantValue is int)
                return ((int)constantValue).ToString(CultureInfo.InvariantCulture);

            if (constantValue is double)
                return ((double)constantValue).ToString(CultureInfo.InvariantCulture);

            if (constantValue is decimal)
                return ((decimal)constantValue).ToString(CultureInfo.InvariantCulture);

            if (constantValue is long)
                return ((long)constantValue).ToString(CultureInfo.InvariantCulture);

            if (constantValue is short)
                return ((short)constantValue).ToString(CultureInfo.InvariantCulture);

            if (constantValue is float)
                return ((float)constantValue).ToString(CultureInfo.InvariantCulture);

            if (constantValue is TimeSpan)
                return ((TimeSpan)constantValue).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

            throw new ArgumentException($"Constant type {constantValue.GetType().Name} isn't supported");
        }

        protected virtual string AddConstant(ConstantExpression expression) => AddConstant(expression.Value);

        protected virtual string AddMemberAccess(MemberExpression expression)
        {
            if (expression.Member.DeclaringType == typeof(DateTime))
            {
                if (expression.Member.Name == nameof(DateTime.Now))
                    return $"new Date()";
                if (expression.Member.Name == nameof(DateTime.Year))
                    return $"{WalkExpression(expression.Expression)}.getFullYear()";
                if (expression.Member.Name == nameof(DateTime.Month))
                    return $"({WalkExpression(expression.Expression)}.getMonth() + 1)";
                if (expression.Member.Name == nameof(DateTime.Day))
                    return $"{WalkExpression(expression.Expression)}.getDate()";
                if (expression.Member.Name == nameof(DateTime.Hour))
                    return $"{WalkExpression(expression.Expression)}.getHours()";
                if (expression.Member.Name == nameof(DateTime.Minute))
                    return $"{WalkExpression(expression.Expression)}.getMinutes()";
                if (expression.Member.Name == nameof(DateTime.Second))
                    return $"{WalkExpression(expression.Expression)}.getSeconds()";
                if (expression.Member.Name == nameof(DateTime.DayOfWeek))
                    return $"{WalkExpression(expression.Expression)}.getDay()";
            }
            if (expression.Member.DeclaringType == typeof(TimeSpan))
            {
                if (expression.Member.Name == nameof(TimeSpan.TotalMilliseconds))
                    return $"({WalkExpression(expression.Expression)})";
                if (expression.Member.Name == nameof(TimeSpan.TotalSeconds))
                    return $"({WalkExpression(expression.Expression)} / 1000.0)";
                if (expression.Member.Name == nameof(TimeSpan.TotalMinutes))
                    return $"({WalkExpression(expression.Expression)} / 60000.0)";
                if (expression.Member.Name == nameof(TimeSpan.TotalHours))
                    return $"({WalkExpression(expression.Expression)} / 3600000.0)";
                if (expression.Member.Name == nameof(TimeSpan.TotalDays))
                    return $"({WalkExpression(expression.Expression)} / 86400000.0)";
            }
            else if (expression.Member.DeclaringType != null && Nullable.GetUnderlyingType(expression.Member.DeclaringType) != null)
            {
                if (expression.Member.Name == nameof(Nullable<int>.HasValue))
                    return $"jsv_notequal({WalkExpression(expression.Expression)}, null)";
                if (expression.Member.Name == nameof(Nullable<int>.Value))
                    return $"({WalkExpression(expression.Expression)})";
            }
            else if (expression.Member.Name == nameof(Array.Length) || expression.Member.Name == nameof(List<object>.Count))
                return $"jsv_length({WalkExpression(expression.Expression)})";
            else if (IsExpressionRootsInParameter(expression))
                return AddParameterAccess(expression);

            throw new ArgumentException($"Unexpected property of {expression.Member?.DeclaringType?.Name}.{expression.Member?.Name}", nameof(expression));
        }

        protected bool GetExpressionValue(Expression expression, out object returnValue)
        {
            //check whether the member is not related to a parameter
            {
                returnValue = null;
                try
                {
                    LambdaExpression lambdaExpression = Expression.Lambda(expression);
                    Delegate delegateExpression = lambdaExpression.Compile();
                    returnValue = delegateExpression.DynamicInvoke();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
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

            throw new Exception($"Unexpected expression for the parameter access {expression.NodeType}");
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
            if (expression.Method.DeclaringType == typeof(Math))
            {
                if (expression.Method.Name == nameof(Math.Abs))
                    return $"Math.abs({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Sqrt))
                    return $"Math.sqrt({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Round) && expression.Arguments.Count == 1)
                    return $"Math.round({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Floor))
                    return $"Math.floor({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Ceiling))
                    return $"Math.ceil({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Truncate))
                    return $"jsv_trunc({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Sign))
                    return $"jsv_sign({WalkExpression(expression.Arguments[0])})";

                if (expression.Method.Name == nameof(Math.Max))
                    return $"Math.max({WalkExpression(expression.Arguments[0])}, {WalkExpression(expression.Arguments[1])})";
                if (expression.Method.Name == nameof(Math.Min))
                    return $"Math.min({WalkExpression(expression.Arguments[0])}, {WalkExpression(expression.Arguments[1])})";

                if (expression.Method.Name == nameof(Math.Log))
                    return $"Math.log({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Exp))
                    return $"Math.exp({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Pow))
                    return $"Math.pow({WalkExpression(expression.Arguments[0])}, {WalkExpression(expression.Arguments[1])})";

                if (expression.Method.Name == nameof(Math.Sin))
                    return $"Math.sin({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Cos))
                    return $"Math.cos({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Tan))
                    return $"Math.tan({WalkExpression(expression.Arguments[0])})";

                if (expression.Method.Name == nameof(Math.Sinh))
                    return $"Math.sinh({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Cosh))
                    return $"Math.cosh({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Tanh))
                    return $"Math.tanh({WalkExpression(expression.Arguments[0])})";

                if (expression.Method.Name == nameof(Math.Asin))
                    return $"Math.asin({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Acos))
                    return $"Math.acos({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Math.Atan))
                    return $"Math.atan({WalkExpression(expression.Arguments[0])})";
            }
            else if (expression.Method.DeclaringType == typeof(char))
            {
                if (expression.Method.Name == nameof(char.IsUpper))
                    return $"jsv_isUpperCase({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(char.IsLower))
                    return $"jsv_isLowerCase({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(char.IsDigit))
                    return $"jsv_isDigit({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(char.IsLetter))
                    return $"jsv_isLetter({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(char.IsLetterOrDigit))
                    return $"jsv_isLetterOrDigit({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(char.IsPunctuation))
                    return $"jsv_isPunctuation({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(char.IsControl))
                    return $"jsv_isControl({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(char.IsWhiteSpace))
                    return $"jsv_isWhiteSpace({WalkExpression(expression.Arguments[0])})";
            }
            else if (expression.Method.DeclaringType == typeof(Regex))
            {
                if (expression.Method.Name == nameof(Regex.IsMatch))
                {
                    if (expression.Object == null && expression.Arguments.Count == 2)
                    {
                        string pattern;
                        if (GetExpressionValue(expression.Arguments[1], out object r) && (r is string s))
                            pattern = $"/{s}/";
                        else
                            pattern = WalkExpression(expression.Arguments[1]);
                        return $"jsv_match({pattern}, {WalkExpression(expression.Arguments[0])})";
                    }
                    else if (expression.Object == null && expression.Arguments.Count == 3)
                    {
                        string pattern, suffix = "";
                        if (GetExpressionValue(expression.Arguments[1], out object r) && (r is string s))
                            pattern = s;
                        else
                            throw new InvalidOperationException("The string object is expected here");

                        if (!GetExpressionValue(expression.Arguments[2], out object r1) || !(r1 is RegexOptions ro))
                            throw new InvalidOperationException("The regex options are expected here");

                        {
                            if ((ro & RegexOptions.IgnoreCase) != 0)
                                suffix += "i";
                            if ((ro & RegexOptions.Multiline) != 0)
                                suffix += "m";
                        }
                        return $"jsv_match(/{pattern}/{suffix}, {WalkExpression(expression.Arguments[0])})";
                    }
                    else if (expression.Object != null && expression.Arguments.Count == 1)
                    {
                        if (!GetExpressionValue(expression.Object, out object r) || !(r is Regex re))
                            throw new InvalidOperationException("The regex object is expected here");

                        string pattern = re.ToString();
                        string suffix = "";
                        if ((re.Options & RegexOptions.IgnoreCase) != 0)
                            suffix += "i";
                        if ((re.Options & RegexOptions.Multiline) != 0)
                            suffix += "m";
                        return $"jsv_match(/{pattern}/{suffix}, {WalkExpression(expression.Arguments[0])})";
                    }
                }
            }
            else if (expression.Method.DeclaringType == typeof(DateTime))
            {
                if (expression.Method.Name == nameof(DateTime.AddDays))
                    return $"(new Date({WalkExpression(expression.Object)}.getTime() + ({WalkExpression(expression.Arguments[0])}) * 86400000))";
                if (expression.Method.Name == nameof(DateTime.AddHours))
                    return $"(new Date({WalkExpression(expression.Object)}.getTime() + ({WalkExpression(expression.Arguments[0])}) * 3600000))";
                if (expression.Method.Name == nameof(DateTime.AddMinutes))
                    return $"(new Date({WalkExpression(expression.Object)}.getTime() + ({WalkExpression(expression.Arguments[0])}) * 60000))";
                if (expression.Method.Name == nameof(DateTime.AddSeconds))
                    return $"(new Date({WalkExpression(expression.Object)}.getTime() + ({WalkExpression(expression.Arguments[0])}) * 1000))";
            }
            else if (expression.Method.DeclaringType == typeof(string))
            {
                if (expression.Method.Name == nameof(string.IsNullOrEmpty) && expression.Arguments.Count == 1)
                    return $"jsv_isempty({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(string.IsNullOrWhiteSpace) && expression.Arguments.Count == 1)
                    return $"jsv_isemptyorwhitespace({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(string.Trim) && expression.Object != null && expression.Arguments.Count == 0)
                    return $"jsv_trim({WalkExpression(expression.Object)})";
                if (expression.Method.Name == nameof(string.ToUpper) && expression.Object != null && expression.Arguments.Count == 0)
                    return $"jsv_upper({WalkExpression(expression.Object)})";
                if (expression.Method.Name == nameof(string.ToLower) && expression.Object != null && expression.Arguments.Count == 0)
                    return $"jsv_lower({WalkExpression(expression.Object)})";

                if (expression.Method.Name == nameof(string.Contains) && expression.Object != null && expression.Arguments.Count == 1)
                    return $"(({WalkExpression(expression.Object)}).indexOf({WalkExpression(expression.Arguments[0])}) >= 0)";

                if (expression.Method.Name == nameof(string.StartsWith) && expression.Object != null && expression.Arguments.Count == 1)
                    return $"(({WalkExpression(expression.Object)}).indexOf({WalkExpression(expression.Arguments[0])}) == 0)";
                if (expression.Method.Name == nameof(string.StartsWith) && expression.Object != null && expression.Arguments.Count == 2)
                {
                    if (!GetExpressionValue(expression.Arguments[1], out object arg1) || !(arg1 is StringComparison comparison))
                        throw new ArgumentException("Only StringComparison second argument is supported for Contains function");
                    if (comparison == StringComparison.CurrentCultureIgnoreCase || comparison == StringComparison.InvariantCultureIgnoreCase || comparison == StringComparison.OrdinalIgnoreCase)
                        return $"(jsv_upper({WalkExpression(expression.Object)}).indexOf(jsv_upper({WalkExpression(expression.Arguments[0])})) == 0)";
                    else
                        return $"(({WalkExpression(expression.Object)}).indexOf({WalkExpression(expression.Arguments[0])}) == 0)";
                }

                if (expression.Method.Name == nameof(string.IndexOf) && expression.Object != null)
                {
                    string startIndex = "0";
                    bool ignoreCase = false;
                    string obj = WalkExpression(expression.Object);
                    string argument = WalkExpression(expression.Arguments[0]);

                    if (expression.Arguments.Count > 1)
                    {
                        if (expression.Arguments[1].Type == typeof(StringComparison))
                        {
                            if (!GetExpressionValue(expression.Arguments[1], out object arg1) || !(arg1 is StringComparison comparison))
                                throw new ArgumentException("Only StringComparison argument is supported for IndexOf function");
                            ignoreCase = comparison == StringComparison.CurrentCultureIgnoreCase || comparison == StringComparison.InvariantCultureIgnoreCase || comparison == StringComparison.OrdinalIgnoreCase;
                        }
                        else
                        {
                            startIndex = WalkExpression(expression.Arguments[1]);
                        }
                    }

                    if (expression.Arguments.Count == 3)
                    {
                        if (!GetExpressionValue(expression.Arguments[2], out object arg1) || !(arg1 is StringComparison comparison))
                            throw new ArgumentException("Only StringComparison argument is supported for IndexOf function");
                        ignoreCase = comparison == StringComparison.CurrentCultureIgnoreCase || comparison == StringComparison.InvariantCultureIgnoreCase || comparison == StringComparison.OrdinalIgnoreCase;
                    }

                    if (ignoreCase)
                    {
                        obj = $"jsv_upper({obj})";
                        argument = $"jsv_upper({argument})";
                    }

                    return $"(({obj}).indexOf({argument}, {startIndex}))";
                }

                if (expression.Method.Name == nameof(string.Substring) && expression.Object != null && expression.Arguments.Count == 1)
                    return $"({WalkExpression(expression.Object)}).substr({WalkExpression(expression.Arguments[0])})";

                if (expression.Method.Name == nameof(string.Substring) && expression.Object != null && expression.Arguments.Count == 2)
                    return $"({WalkExpression(expression.Object)}).substr({WalkExpression(expression.Arguments[0])}, {WalkExpression(expression.Arguments[1])})";

                if (expression.Method.Name == "get_Chars" && expression.Arguments.Count == 1)
                    return $"jsv_index({WalkExpression(expression.Object)}, {WalkExpression(expression.Arguments[0])})";
            }
            else if (expression.Method.Name == nameof(string.ToString) && (expression.Arguments == null || expression.Arguments.Count == 0))
            {
                return $"jsv_tostring({WalkExpression(expression.Object)})";
            }
            else if (expression.Method.Name == "get_Item" && (expression.Arguments.Count == 1))
            {
                if (expression.Object?.NodeType == ExpressionType.Parameter)
                    return AddParameterAccess(expression);
                return $"jsv_index({WalkExpression(expression.Object)}, {WalkExpression(expression.Arguments[1])})";
            }
            else if (expression.Method.DeclaringType == typeof(Functions))
            {
                if (expression.Method.Name == nameof(Functions.DaysSince))
                    return $"jsv_dayssince({WalkExpression(expression.Arguments[0])}, {WalkExpression(expression.Arguments[1])})";
                if (expression.Method.Name == nameof(Functions.MonthsSince))
                    return $"jsv_monthssince({WalkExpression(expression.Arguments[0])}, {WalkExpression(expression.Arguments[1])})";
                if (expression.Method.Name == nameof(Functions.YearsSince))
                    return $"jsv_yearssince({WalkExpression(expression.Arguments[0])}, {WalkExpression(expression.Arguments[1])})";
                if (expression.Method.Name == nameof(Functions.IsCreditCardNumberCorrect))
                    return $"jsv_ccn_valid({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Functions.ToBool))
                    return $"jsv_string2bool({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Functions.ToInt))
                    return $"jsv_string2int({WalkExpression(expression.Arguments[0])})";
                if (expression.Method.Name == nameof(Functions.IsNull))
                    return $"(({WalkExpression(expression.Arguments[0])}) == null)"; ;
                if (expression.Method.Name == nameof(Functions.IsNullOrEmpty))
                    return $"jsv_isempty({WalkExpression(expression.Arguments[0])})"; ;
                if (expression.Method.Name == nameof(Functions.IsNotNull))
                    return $"(({WalkExpression(expression.Arguments[0])}) != null)"; ;
                if (expression.Method.Name == nameof(Functions.IsNotNullOrEmpty))
                    return $"jsv_not(jsv_isempty({WalkExpression(expression.Arguments[0])}))"; ;
                if (expression.Method.Name == nameof(Functions.Fractional))
                    return $"jsv_fractional({WalkExpression(expression.Arguments[0])})"; ;
            }

            if (expression.Arguments != null && expression.Arguments.Count >= 1 && (typeof(IEnumerable).IsAssignableFrom(expression.Arguments[0].Type)))
            {
                if (expression.Arguments.Count == 2 && expression.Arguments[1].NodeType == ExpressionType.Lambda)
                {
                    if (expression.Method.Name == nameof(Enumerable.Any))
                    {
                        return $"jsv_any({WalkExpression(expression.Arguments[0])}, {AddLambdaParameter((LambdaExpression)expression.Arguments[1])})";
                    }

                    if (expression.Method.Name == nameof(Enumerable.All))
                    {
                        return $"jsv_all({WalkExpression(expression.Arguments[0])}, {AddLambdaParameter((LambdaExpression)expression.Arguments[1])})";
                    }

                    if (expression.Method.Name == nameof(Enumerable.Count))
                    {
                        return $"jsv_count({WalkExpression(expression.Arguments[0])}, {AddLambdaParameter((LambdaExpression)expression.Arguments[1])})";
                    }

                    if (expression.Method.Name == nameof(Enumerable.First))
                    {
                        return $"jsv_first({WalkExpression(expression.Arguments[0])}, {AddLambdaParameter((LambdaExpression)expression.Arguments[1])})";
                    }

                    if (expression.Method.Name == nameof(Enumerable.Last))
                    {
                        return $"jsv_last({WalkExpression(expression.Arguments[0])}, {AddLambdaParameter((LambdaExpression)expression.Arguments[1])})";
                    }
                }
                else if (expression.Arguments.Count == 3 && expression.Arguments[1].NodeType == ExpressionType.Lambda)
                {
                    if (expression.Method.Name == nameof(Enumerable.FirstOrDefault))
                    {
                        return $"jsv_first({WalkExpression(expression.Arguments[0])}, {AddLambdaParameter((LambdaExpression)expression.Arguments[1])}, {WalkExpression(expression.Arguments[2])})";
                    }

                    if (expression.Method.Name == nameof(Enumerable.LastOrDefault))
                    {
                        return $"jsv_last({WalkExpression(expression.Arguments[0])}, {AddLambdaParameter((LambdaExpression)expression.Arguments[1])}, {WalkExpression(expression.Arguments[2])})";
                    }
                }
                else if (expression.Arguments.Count == 1)
                {
                    if (expression.Method.Name == nameof(Enumerable.Empty))
                    {
                        return $"jsv_isempty({WalkExpression(expression.Arguments[0])})";
                    }

                    if (expression.Method.Name == nameof(Enumerable.Count))
                    {
                        return $"jsv_count({WalkExpression(expression.Arguments[0])}, undefined)";
                    }
                }
            }

            throw new ArgumentException($"Unexpected call of {expression.Method?.DeclaringType?.Name}.{expression.Method?.Name}", nameof(expression));
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