using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MathSolver
{
    public class ValueParser : ArithmeticBase
    {
        public ValueParser(CultureInfo cultureInfo = null) : base(cultureInfo)
        {
        }

        /// <summary>
        ///     Парсит выражение и возвращает результат
        /// </summary>
        /// <param name="expr">Полное математическое выражение</param>
        public double Parse(string expr)
        {
            return SolveAllOperation(Lexer(expr));
        }

        /// <summary>
        ///     Делает разбор символов находящийся в выражении
        /// </summary>
        /// <param name="expr">Полное математическое выражение</param>
        protected override List<string> Lexer(string expr)
        {
            var token = "";
            var tokens = new List<string>();

            expr = expr.Replace(" ", "");
            expr = expr.Replace("+-", "-");
            expr = expr.Replace("-+", "-");
            expr = expr.Replace("--", "+");

            for (var i = 0; i < expr.Length; i++)
            {
                var ch = expr[i];

                if (char.IsWhiteSpace(ch))
                    continue;

                if (char.IsLetter(ch))
                {
                    if (i != 0 && (char.IsDigit(expr[i - 1]) || expr[i - 1] == ')'))
                        tokens.Add("*");

                    token += ch;

                    while (i + 1 < expr.Length && char.IsLetterOrDigit(expr[i + 1]))
                        token += expr[++i];

                    tokens.Add(token);
                    token = "";

                    continue;
                }

                if (char.IsDigit(ch))
                {
                    token += ch;

                    // для больших чисел > 9 сразу соединяет вместе 
                    while (i + 1 < expr.Length && (char.IsDigit(expr[i + 1]) || expr[i + 1] == '.'))
                        token += expr[++i];

                    tokens.Add(token);
                    token = "";

                    continue;
                }

                if (i + 1 < expr.Length && (ch == '-' || ch == '+') && char.IsDigit(expr[i + 1]) &&
                (i == 0 || OperatorList.IndexOf(expr[i - 1].ToString(
#if !NETSTANDARD1_4
                        this.CultureInfo
#endif
                        )) != -1 || i - 1 >= 0 && expr[i - 1] == '('))
                {
                    token += ch;

                    while (i + 1 < expr.Length && (char.IsDigit(expr[i + 1]) || expr[i + 1] == '.'))
                        token += expr[++i];

                    tokens.Add(token);
                    token = "";

                    continue;
                }

                if (ch == '(')
                    if (i != 0 && (char.IsDigit(expr[i - 1]) || char.IsDigit(expr[i - 1]) || expr[i - 1] == ')'))
                    {
                        tokens.Add("*");
                        tokens.Add("(");
                    }
                    else
                    {
                        tokens.Add("(");
                    }
                else
                    tokens.Add(ch.ToString());
            }

            return tokens;
        }

        /// <summary>
        ///     Решает все операции с числами
        /// </summary>
        /// <param name="tokens">Символы в разборе</param>
        private double SolveAllOperation(List<string> tokens)
        {
            // Замена переменных со статическими значениями 
            for (var index = 0; index < tokens.Count; index++)
                if (LocalVariables.Keys.Contains(tokens[index]))
                    tokens[index] = LocalVariables[tokens[index]].ToString(this.CultureInfo);

            // если есть выражения в круглых скобках
            while (tokens.IndexOf("(") != -1)
            {
                // достает символы в круглых скобках "(" и ")" начиная с конца
                var open = tokens.LastIndexOf("(");
                var close = tokens.IndexOf(")", open);

                // в случае, если значение close будет -1, то это будет означать, 
                // что после открывающей скобки нет закрывающей, и бросаться исключение
                if (open >= close)
                    throw new ArithmeticException(
                        string.Format("Не найдена закрывающая скобка после открывающей. Символ: {0}",
                            open.ToString(this.CultureInfo)));

                var roughExpr = new List<string>();

                // добавляем символы в скобках
                for (var index = open + 1; index < close; index++)
                    roughExpr.Add(tokens[index]);

                double tmpResult;

                var args = new List<double>();
                var functionName = tokens[open == 0 ? 0 : open - 1];

                if (LocalFunctions.Keys.Contains(functionName))
                {
                    if (roughExpr.Contains(","))
                    {
                        for (var i = 0; i < roughExpr.Count; i++)
                        {
                            var defaultExpr = new List<string>();
                            var firstCommaOrEndOfExpression = roughExpr.IndexOf(",", i) != -1 ? roughExpr.IndexOf(",", i)
                                                                                              : roughExpr.Count;

                            while (i < firstCommaOrEndOfExpression)
                                defaultExpr.Add(roughExpr[i++]);

                            args.Add(defaultExpr.Count == 0 ? 0 : SolveSimpleExpression(defaultExpr));
                        }

                        tmpResult = double.Parse(LocalFunctions[functionName](args.ToArray()).ToString(this.CultureInfo), this.CultureInfo);
                    }
                    else
                    {
                        tmpResult = double.Parse(LocalFunctions[functionName](new[]
                        {
                            SolveSimpleExpression(roughExpr)
                        }).ToString(this.CultureInfo), this.CultureInfo);
                    }
                }

                else
                    tmpResult = SolveSimpleExpression(roughExpr);

                // заменяем скобку на результат, который получился с помощью предыдущего метода
                tokens[open] = tmpResult.ToString(this.CultureInfo);
                // удаляем символы после нее
                tokens.RemoveRange(open + 1, close - open);

                if (LocalFunctions.Keys.Contains(functionName))
                    tokens.RemoveAt(open - 1);
            }

            // решаем оставшейся последнюю операцию
            return SolveSimpleExpression(tokens);
        }

        /// <summary>
        ///     Решает простую операцию двух чисел
        /// </summary>
        /// <param name="tokens">Символы в разборе</param>
        private double SolveSimpleExpression(List<string> tokens)
        {
            switch (tokens.Count)
            {
                case 1:
                    return double.Parse(tokens[0], this.CultureInfo);
                case 2:
                    var op = tokens[0];

                    if (op == "-" || op == "+")
                        return double.Parse((op == "+" ? ""
                                                       : (tokens[1].Substring(0, 1) == "-" ? "" : "-")) + tokens[1],
                                                       this.CultureInfo);

                    return OperationAction[op](0, double.Parse(tokens[1], this.CultureInfo));
                case 0:
                    return 0;
            }

            foreach (var op in OperatorList)
                while (tokens.IndexOf(op) != -1)
                {
                    var opPlace = tokens.IndexOf(op);

                    var numberA = double.Parse(tokens[opPlace - 1], this.CultureInfo);
                    var numberB = double.Parse(tokens[opPlace + 1], this.CultureInfo);

                    var result = OperationAction[op](numberA, numberB);

                    tokens[opPlace - 1] = result.ToString(this.CultureInfo);
                    tokens.RemoveRange(opPlace, 2);
                }

            return double.Parse(tokens[0], this.CultureInfo);
        }
    }
}
