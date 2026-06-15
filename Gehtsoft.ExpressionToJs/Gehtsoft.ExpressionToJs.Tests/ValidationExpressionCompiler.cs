using System;
using System.Linq.Expressions;

namespace Gehtsoft.ExpressionToJs.Tests
{
    /// <summary>
    /// Sample <see cref="ExpressionCompiler"/> for the form-validation use case: it maps the
    /// lambda's parameters onto the <c>reference('path')</c> / <c>value</c> bindings the host page
    /// provides. This mirrors the real consumer
    /// (Gehtsoft.EF.Toolbox's <c>Gehtsoft.Validator.JSConvertor.ValidationExpressionCompiler</c>):
    /// <list type="bullet">
    /// <item>the <b>entity</b> parameter and any member chain rooted in it become
    /// <c>reference('Member.Sub')</c>;</item>
    /// <item>the <b>value</b> parameter becomes the ambient <c>value</c>;</item>
    /// <item>parameters introduced by a LINQ lambda (e.g. <c>r => r.Length &gt; 0</c>) are emitted
    /// by their own name, exactly as the base compiler does;</item>
    /// <item>array indexing inside a parameter chain becomes <c>jsv_index(...)</c>.</item>
    /// </list>
    /// </summary>
    public class ValidationExpressionCompiler : ExpressionCompiler
    {
        private readonly ParameterExpression mEntityParameter = null;
        private readonly ParameterExpression mValueParameter = null;

        public ValidationExpressionCompiler(LambdaExpression lambdaExpression, int? entityParameterIndex = null, int? valueParameterIndex = null) : base(lambdaExpression)
        {
            if ((lambdaExpression.Parameters.Count < 1 && (entityParameterIndex != null || valueParameterIndex != null)) || lambdaExpression.Parameters.Count > 2)
                throw new ArgumentException("The expression must have only one or two parameters", nameof(lambdaExpression));

            if (entityParameterIndex != null)
                mEntityParameter = lambdaExpression.Parameters[(int)entityParameterIndex];
            if (valueParameterIndex != null)
                mValueParameter = lambdaExpression.Parameters[(int)valueParameterIndex];
        }

        /// <summary>True while emitting the body of a LINQ lambda, where parameters are local
        /// (<c>r</c>, <c>c</c>, ...) rather than the entity/value bindings.</summary>
        protected bool InLambdaParameter { get; private set; } = false;

        protected override string AddLambdaParameter(LambdaExpression expression)
        {
            bool inLambdaParameter = InLambdaParameter;
            InLambdaParameter = true;
            string s = base.AddLambdaParameter(expression);
            InLambdaParameter = inLambdaParameter;
            return s;
        }

        protected override string AddParameter(ParameterExpression parameterExpression)
        {
            if (parameterExpression == mEntityParameter)
                return "reference()";
            else if (parameterExpression == mValueParameter)
                return "value";
            else if (InLambdaParameter)
                return base.AddParameter(parameterExpression);
            else
                throw new InvalidOperationException("Only 'value' and 'entity' parameters are supported");
        }

        protected override string AddParameterAccess(Expression expression) => AddParameterAccess(expression, true);

        protected virtual string AddParameterAccess(Expression expression, bool initial)
        {
            string result = null;
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = (MemberExpression)expression;
                result = AddParameterAccess(memberExpression.Expression, false);
                if (result != "")
                    result += ".";
                result += memberExpression.Member.Name;
            }
            else if (expression.NodeType == ExpressionType.ArrayIndex)
            {
                BinaryExpression binaryExpression = (BinaryExpression)expression;
                return $"jsv_index({AddParameterAccess(binaryExpression.Left)}, {WalkExpression(binaryExpression.Right)})";
            }
            else if (expression.NodeType == ExpressionType.Parameter)
            {
                if (expression == mValueParameter)
                    return "value";
                if (expression != mEntityParameter)
                    throw new InvalidOperationException("Only 'value' and 'entity' parameters are supported");
                return "";
            }

            if (initial)
                result = $"reference('{result}')";
            return result;
        }
    }
}
