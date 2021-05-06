using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Gehtsoft.ExpressionToJs.Tests
{
    public class ValidationExpressionCompiler : ExpressionCompiler
    {
        private readonly ParameterExpression mEntityParameter = null;
        private readonly ParameterExpression mValueParameter = null;

        public ValidationExpressionCompiler(LambdaExpression lambdaExpression, int? entityParameterIndex = null, int? valueParameterIndex = null) : base(lambdaExpression)
        {
            if ((lambdaExpression.Parameters.Count < 1 && (entityParameterIndex != null || valueParameterIndex != null)) || lambdaExpression.Parameters.Count > 2)
                throw new ArgumentException("The expression must have only one or two parameters", nameof(lambdaExpression));

            if (entityParameterIndex != null)
                mEntityParameter = lambdaExpression.Parameters[(int) entityParameterIndex];
            if (valueParameterIndex != null)
                mValueParameter = lambdaExpression.Parameters[(int) valueParameterIndex];
        }

        protected override string AddParameter(ParameterExpression parameterExpression)
        {
            if (parameterExpression == mEntityParameter)
                return "reference()";
            else if (parameterExpression == mValueParameter)
                return "value";
            else
                throw new InvalidOperationException("Only 'value' and 'entity' parameters are supported");
        }

        protected override string AddParameterAccess(Expression expression) => AddParameterAccess(expression, true);

        protected virtual string AddParameterAccess(Expression expression, bool initial)
        {
            string result = null;
            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression memberExpression = (MemberExpression) expression;
                result = AddParameterAccess(memberExpression.Expression, false);
                if (result != "")
                    result += ".";
                result += memberExpression.Member.Name;
            }
            else if (expression.NodeType == ExpressionType.Parameter)
            {
                if (expression == mValueParameter)
                    throw new InvalidOperationException("'value' parameter must be a primary value");
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
