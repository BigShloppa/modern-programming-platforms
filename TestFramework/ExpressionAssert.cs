using System;
using System.Linq.Expressions;
using System.Text;

namespace TestFramework
{
    public static partial class Assert
    {
        public static void That(Expression<Func<bool>> expression)
        {
            if (expression.Compile().Invoke()) return;

            var body = expression.Body;
            var sb = new StringBuilder();
            sb.Append("Expression failed: ");
            sb.Append(expression.ToString());
            sb.Append(". ");
            AnalyzeExpression(body, sb);
            throw new AssertException(sb.ToString());
        }

        private static void AnalyzeExpression(Expression expr, StringBuilder sb)
        {
            if (expr is BinaryExpression binary)
            {
                sb.Append(" (");
                AnalyzeExpression(binary.Left, sb);
                sb.Append($" {binary.NodeType} ");
                AnalyzeExpression(binary.Right, sb);
                sb.Append(")");
                return;
            }
            if (expr is MethodCallExpression call)
            {
                sb.Append(call.Method.Name);
                sb.Append("(");
                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    AnalyzeExpression(call.Arguments[i], sb);
                }
                sb.Append(")");
                return;
            }
            if (expr is MemberExpression member)
            {
                sb.Append(member.Member.Name);
                return;
            }
            if (expr is ConstantExpression constant)
            {
                sb.Append(constant.Value?.ToString() ?? "null");
                return;
            }
            sb.Append(expr.ToString());
        }
    }
}