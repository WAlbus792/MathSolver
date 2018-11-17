using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MathSolver
{
    public class VariableParser : ArithmeticBase
    {
        public VariableParser(CultureInfo cultureInfo = null) : base(cultureInfo)
        {
        }

        /// <summary>
        ///     Парсит выражение и возвращает результат
        ///     <para>
        ///         Для примера:
        ///         <c>f(x) = (x - 1)(x - 2)</c>
        ///     </para>
        /// </summary>
        /// <remarks>Выражение имеет переменное X</remarks>
        /// <param name="expr">Полное математическое выражение</param>
        public List<string> Parse(string expr)
        {
            return SimplifyExpression(Lexer(expr));
        }

        /// <summary>
        ///     Делает разбор символов находящийся в выражении
        /// </summary>
        /// <param name="expr">Полное математическое выражение</param>
        protected override List<string> Lexer(string expr)
        {
            var tokens = new List<string>(expr.Length);
            var token = string.Empty;

            for (var index = 0; index < expr.Length; index++)
            {
                var ch = expr[index];

                if (char.IsWhiteSpace(ch)) continue;

                if (char.IsDigit(ch))
                {
                    token += ch;

                    if (index != 0 && (expr[index - 1] == '-' || expr[index - 1] == '+'))
                        token = token.Insert(0, expr[index - 1].ToString());

                    var nextIndex = index;
                    // для больших чисел > 9 сразу соединяет вместе 
                    while (++nextIndex < expr.Length && (char.IsDigit(expr[nextIndex]) || expr[nextIndex] == '.'))
                        token += expr[index = nextIndex];

                    tokens.Add(token);

                    if (tokens.Count >= 2
                        && tokens[tokens.Count - 2] == "*"
                        && tokens[tokens.Count - 3] == ")")
                    {
                        tokens.Insert(tokens.Count - 1, "(");
                        tokens.Add(")");
                    }

                    token = "";

                    continue;
                }

                if (char.IsLetter(ch))
                {
                    if (index != 0 && (char.IsDigit(expr[index - 1]) || expr[index - 1] == ')'))
                        tokens.Add("*");

                    token += ch;

                    while (index + 1 < expr.Length && char.IsLetterOrDigit(expr[index + 1]))
                        token += expr[++index];

                    tokens.Add(token);
                    token = "";

                    continue;
                }

                if (ch == '(')
                {
                    // если перед знаком "(" стоит ")", то поставляется знак умножения
                    if (index != 0 && (char.IsDigit(expr[index - 1]) || expr[index - 1] == ')'))
                    {
                        tokens.Add("*");
                        tokens.Add("(");
                    }
                    else
                    {
                        tokens.Add("(");
                    }
                }

                // если это знак математического оператора
                else
                {
                    if (char.IsDigit(expr[index + 1]) && (ch == '-' || ch == '+')) continue;

                    tokens.Add(ch.ToString());
                }
            }

            return tokens;
        }

        /// <summary>
        ///     Возврашает упрощенное математическое выражение
        /// </summary>
        /// <param name="tokens">Символы в разборе</param>
        private List<string> SimplifyExpression(List<string> tokens)
        {
            // Замена переменных со статическими значениями 
            for (var index = 0; index < tokens.Count; index++)
                if (LocalVariables.Keys.Contains(tokens[index]))
                    tokens[index] = LocalVariables[tokens[index]].ToString(CultureInfo);

            var openBracketsCount = tokens.Count(s => s == "(");
            var closeBracketsCount = tokens.Count(s => s == ")");
            if (openBracketsCount != closeBracketsCount)
                throw new ArithmeticException("Введено неправильное выражение, просим исправить");

            var bracketTokens = new List<List<string>>();
            // достает символы в круглых скобках "(" и ")" начиная с конца
            for (var index = 0; index < closeBracketsCount; index++)
            {
                var open = tokens.LastIndexOf("(");
                var close = tokens.IndexOf(")", open);

                bracketTokens.Add(tokens.GetRange(open + 1, close - open - 1));

                tokens.RemoveRange(open, close - open + 1);
                if (tokens.Contains("*"))
                    tokens.RemoveRange(tokens.LastIndexOf("*"), 1);
            }

            bracketTokens.Reverse();
            // умножает все члены скобках со другимы
            var solvedBracketExpression = SolveBracketExpression(bracketTokens);

            // вставляет скобки для получившегося общего выражения в скобках сверху
            solvedBracketExpression.Insert(0, "(");
            solvedBracketExpression.Add(")");
            tokens.AddRange(solvedBracketExpression);

            // делает открытие скобок

            if (tokens[tokens.IndexOf("(") - 1] == "-")
            {
                var open = tokens.IndexOf("(");
                var close = tokens.IndexOf(")");
                var index = 0;

                var bracketExpression = tokens.GetRange(open + 1, close - open - 1);
                bracketExpression.ForEach(s =>
                {
                    if (!s.StartsWith("-") && !s.StartsWith("+"))
                        s = s.Insert(0, "-");
                    else if (s.StartsWith("-"))
                        s = s.Replace("-", "+");
                    else if (s.StartsWith("+"))
                        s = s.Replace("+", "-");

                    tokens[index++ + open + 1] = s;
                });

                tokens.RemoveAt(open - 1);
                tokens.RemoveAll(t => t == "(" || t == ")");
                tokens = AssociateMembers(tokens);
            }
            else if (!tokens[tokens.IndexOf("(") + 1].StartsWith("+"))
            {
                var open = tokens.IndexOf("(");
                var close = tokens.IndexOf(")");
                var index = 0;

                var bracketExpression = tokens.GetRange(open + 1, close - open - 1);
                bracketExpression.ForEach(s =>
                {
                    if (!s.StartsWith("-") && !s.StartsWith("+"))
                        s = s.Insert(0, "+");

                    tokens[index++ + open + 1] = s;
                });

                tokens.RemoveAt(open - 1);
                tokens.RemoveAll(t => t == "(" || t == ")");
                tokens = AssociateMembers(tokens);
            }

            return tokens;
        }

        /// <summary>
        ///     Решает операции со скобками (например делает умножение первого выражения в скобках со вторым)
        /// </summary>
        /// <param name="bracketTokens">Список списка символов в скобках</param>
        private List<string> SolveBracketExpression(List<List<string>> bracketTokens)
        {
            if (bracketTokens.Count == 1) return bracketTokens[0];

            var firstBracketExpression = bracketTokens[0];
            for (var mainIndex = 1; mainIndex < bracketTokens.Count; mainIndex++)
            {
                var token = string.Empty;
                var tokens = new List<string>();
                var secondBracketExpression = bracketTokens[mainIndex];
                for (var firstIndex = 0; firstIndex < firstBracketExpression.Count; firstIndex++)
                    for (var secondIndex = 0; secondIndex < secondBracketExpression.Count; secondIndex++)
                    {
                        var first = firstBracketExpression[firstIndex];
                        var second = secondBracketExpression[secondIndex];

                        if (first.Contains("x") || second.Contains("x"))
                        {
                            double firstAheadValue;
                            var secondAheadValue = GetAheadValuesOfX(ref first, ref second, out firstAheadValue);
                            var valueAtFirst = Math.Round(OperationAction["*"](firstAheadValue, secondAheadValue), 3);

                            int? degree = null;
                            if (first.Contains("x") && second.Contains("x"))
                            {
                                degree = 2;
                                if (first.Contains("^"))
                                {
                                    var degreeSymbolIndex = first.IndexOf("^", StringComparison.Ordinal);
                                    degree = int.Parse(first.Substring(degreeSymbolIndex + 1, 1)) + 1;
                                    first = first.Remove(degreeSymbolIndex + 1, 1);
                                }
                                else
                                {
                                    first = first + "^";
                                }
                            }
                            token = string.Format("{0}{1}{2}", valueAtFirst, first.Contains("x") ? first : second, degree);
                        }
                        else
                        {
                            token = OperationAction["*"](double.Parse(first), double.Parse(second))
                                .ToString(CultureInfo);
                        }

                        if (!token.Contains("-") && !token.Contains("+"))
                            token = token.Insert(0, "+");
                        tokens.Add(token);
                    }

                tokens = AssociateMembers(tokens);
                firstBracketExpression.Clear();
                firstBracketExpression = tokens;
            }

            var resultExpression = new List<string>(firstBracketExpression.Count);
            firstBracketExpression.ForEach(s =>
            {
                if (s == firstBracketExpression[0] && s.StartsWith("+"))
                    s = s.Remove(0, 1);

                if (s.StartsWith("-1x") || s.StartsWith("+1x"))
                    s = s.Remove(1, 1);
                if (s.StartsWith("1x"))
                    s = s.Remove(0, 1);

                resultExpression.Add(s);
            });

            return resultExpression;
        }

        /// <summary>
        ///     Приведение подобных членов
        /// </summary>
        /// <param name="tokens">Символы в разборе</param>
        public List<string> AssociateMembers(List<string> tokens)
        {
            var simplifiedTokens = new List<string>();
            List<string> sameMembers = null;
            if (tokens.Count(t => t.Contains("x^")) >= 1)
            {
                sameMembers = tokens.FindAll(t => t.Contains("x^"));
                if (sameMembers.Count == 1)
                {
                    simplifiedTokens.Add(sameMembers[0]);
                }
                else if (sameMembers.Count > 1)
                {
                    var degreeTokensGroups = tokens.Where(t => t.Contains("x^"))
                        .GroupBy(s => s.Substring(s.IndexOf("^", StringComparison.Ordinal), 2)).ToList();

                    for (var index = 0; index < degreeTokensGroups.Count; index++)
                    {
                        var degreeToken = degreeTokensGroups[index];
                        var aheadValues = degreeToken.Select(
                            s => double.Parse(s.Substring(0, s.IndexOf("x", StringComparison.Ordinal)))).ToList();

                        while (aheadValues.Count > 1)
                        {
                            aheadValues[0] = OperationAction["+"](aheadValues[0], aheadValues[1]);
                            aheadValues.RemoveAt(1);
                        }

                        var aheadValueString = aheadValues[0].ToString(CultureInfo);
                        if (!aheadValueString.Contains("-") && !aheadValueString.Contains("+"))
                            aheadValueString = aheadValueString.Insert(0, "+");

                        sameMembers.Insert(index, string.Format("{0}x{1}", aheadValueString, degreeToken.Key));
                        sameMembers.RemoveRange(index + 1, degreeToken.Count());
                        simplifiedTokens.Add(string.Format("{0}x{1}", aheadValueString, degreeToken.Key));
                    }

                    var indexOfx = tokens.FindIndex(t => t.Contains("x^"));
                    tokens.RemoveAll(t => t.Contains("x^"));
                    foreach (var member in sameMembers)
                    {
                        tokens.Insert(indexOfx, member);
                        indexOfx++;
                    }
                }
            }

            if (tokens.Count(t => t.Contains("x") && !t.Contains("^")) >= 1)
            {
                sameMembers = tokens.FindAll(t => t.Contains("x") && !t.Contains("^"));
                if (sameMembers.Count == 1)
                {
                    simplifiedTokens.Add(sameMembers[0]);
                }
                else
                {
                    var aheadValues = sameMembers
                        .Select(x => double.Parse(x.Substring(0, x.IndexOf("x", StringComparison.Ordinal)))).ToList();
                    while (aheadValues.Count > 1)
                    {
                        aheadValues[0] = OperationAction["+"](aheadValues[0], aheadValues[1]);
                        aheadValues.RemoveAt(1);
                    }

                    var aheadValueString = aheadValues[0].ToString(CultureInfo);
                    if (!aheadValueString.Contains("-") && !aheadValueString.Contains("+"))
                        aheadValueString = aheadValueString.Insert(0, "+");

                    simplifiedTokens.Add(string.Format("{0}x", aheadValueString));
                }
            }

            double value;
            if (tokens.Count(t => double.TryParse(t, out value)) >= 1)
            {
                sameMembers = tokens.FindAll(t => double.TryParse(t, out value));
                sameMembers.RemoveAll(s => s == "0" || s == "-0" || s == "+0");
                if (sameMembers.Count == 1)
                {
                    simplifiedTokens.Add(sameMembers[0]);
                }
                else if (sameMembers.Count > 1)
                {
                    var valuesDouble = sameMembers.ConvertAll(double.Parse);

                    while (valuesDouble.Count > 1)
                    {
                        valuesDouble[0] = OperationAction["+"](valuesDouble[0], valuesDouble[1]);
                        valuesDouble.RemoveAt(1);
                    }

                    var valueString = valuesDouble[0].ToString(CultureInfo);
                    if (!valueString.Contains("-") && !valueString.Contains("+"))
                        valueString = valueString.Insert(0, "+");

                    if (valueString != "0" && valueString != "-0" && valueString != "+0")
                        simplifiedTokens.Add(valueString);
                }
            }

            return simplifiedTokens;
        }
    }
}
