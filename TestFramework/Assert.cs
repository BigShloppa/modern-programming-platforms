using System;
using System.Linq.Expressions;
using System.Text;

namespace TestFramework
{
    public static class Assert
    {
        public static void AreEqual(object expected, object actual)
        {
            if (!Equals(expected, actual))
                throw new AssertException($"Expected: {expected}, Actual: {actual}");
        }

        public static void AreNotEqual(object expected, object actual)
        {
            if (Equals(expected, actual))
                throw new AssertException($"Expected not equal: {expected}, but got same");
        }

        public static void IsTrue(bool condition)
        {
            if (!condition)
                throw new AssertException("Expected true, got false");
        }

        public static void IsFalse(bool condition)
        {
            if (condition)
                throw new AssertException("Expected false, got true");
        }

        public static void IsNull(object obj)
        {
            if (obj != null)
                throw new AssertException($"Expected null, got {obj}");
        }

        public static void IsNotNull(object obj)
        {
            if (obj == null)
                throw new AssertException("Expected not null, got null");
        }

        public static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new AssertException($"Expected {typeof(TException).Name}, got {ex.GetType().Name}");
            }
            throw new AssertException($"Expected {typeof(TException).Name}, but no exception thrown");
        }

        public static void Greater<T>(T a, T b) where T : IComparable<T>
        {
            if (a.CompareTo(b) <= 0)
                throw new AssertException($"{a} is not greater than {b}");
        }

        public static void Less<T>(T a, T b) where T : IComparable<T>
        {
            if (a.CompareTo(b) >= 0)
                throw new AssertException($"{a} is not less than {b}");
        }

        public static void Contains(string substring, string fullString)
        {
            if (!fullString.Contains(substring))
                throw new AssertException($"'{fullString}' does not contain '{substring}'");
        }

        public static void DoesNotContain(string substring, string fullString)
        {
            if (fullString.Contains(substring))
                throw new AssertException($"'{fullString}' contains '{substring}'");
        }

        public static void That(Expression<Func<bool>> expression)
        {
            if (expression.Compile().Invoke()) return;

            var body = expression.Body;
            var sb = new StringBuilder();
            sb.Append("Assertion failed: ");
            FormatExpression(body, sb);
            sb.Append(" is false.");
            throw new AssertException(sb.ToString());
        }

        private static void FormatExpression(Expression expr, StringBuilder sb)
        {
            switch (expr)
            {
                case BinaryExpression binary:
                    sb.Append("(");
                    FormatExpression(binary.Left, sb);
                    sb.Append($" {GetOperatorSymbol(binary.NodeType)} ");
                    FormatExpression(binary.Right, sb);
                    sb.Append(")");
                    break;
                case MemberExpression member:
                    try
                    {
                        object value = Expression.Lambda(member).Compile().DynamicInvoke();
                        sb.Append(value?.ToString() ?? "null");
                    }
                    catch
                    {
                        sb.Append(member.Member.Name);
                    }
                    break;
                case ConstantExpression constant:
                    sb.Append(constant.Value?.ToString() ?? "null");
                    break;
                case MethodCallExpression call:
                    sb.Append(call.Method.Name);
                    sb.Append("(...)");
                    break;
                default:
                    sb.Append(expr.ToString());
                    break;
            }
        }

        private static string GetOperatorSymbol(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                _ => nodeType.ToString()
            };
        }

        private static void AnalyzeExpression(Expression expr, StringBuilder sb)
        {
            if (expr is BinaryExpression binary)
            {
                sb.Append("(");
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