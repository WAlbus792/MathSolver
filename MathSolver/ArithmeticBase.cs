using System;
using System.Collections.Generic;
using System.Globalization;

namespace MathSolver
{
    public abstract class ArithmeticBase
    {
        #region Constructor

        protected ArithmeticBase(CultureInfo cultureInfo)
        {
            OperatorList = new List<string>(10) { "^", "%", ":", "/", "*", "-", "+", ">", "<", "=" };

            OperationAction = new Dictionary<string, Func<double, double, double>>(10)
            {
                ["^"] = Math.Pow,
                ["%"] = (a, b) => a % b,
                [":"] = (a, b) => a / b,
                ["/"] = (a, b) => a / b,
                ["*"] = (a, b) => a * b,
                ["-"] = (a, b) => a - b,
                ["+"] = (a, b) => a + b,

                [">"] = (a, b) => a > b ? 1 : 0,
                ["<"] = (a, b) => a < b ? 1 : 0,
                ["="] = (a, b) => Math.Abs(a - b) < 0.00000001 ? 1 : 0
            };

            LocalVariables = new Dictionary<string, double>(8)
            {
                ["pi"] = 3.14159265358979,
                ["tao"] = 6.28318530717959,

                ["e"] = 2.71828182845905,
                ["phi"] = 1.61803398874989,
                ["major"] = 0.61803398874989,
                ["minor"] = 0.38196601125011,

                ["pitograd"] = 57.2957795130823,
                ["piofgrad"] = 0.01745329251994
            };

            LocalFunctions = new Dictionary<string, Func<double[], double>>(26)
            {
                ["abs"] = inputs => Math.Abs(inputs[0]),

                ["cos"] = inputs => Math.Cos(inputs[0]),
                ["cosh"] = inputs => Math.Cosh(inputs[0]),
                ["acos"] = inputs => Math.Acos(inputs[0]),
                ["arccos"] = inputs => Math.Acos(inputs[0]),

                ["sin"] = inputs => Math.Sin(inputs[0]),
                ["sinh"] = inputs => Math.Sinh(inputs[0]),
                ["asin"] = inputs => Math.Asin(inputs[0]),
                ["arcsin"] = inputs => Math.Asin(inputs[0]),

                ["tan"] = inputs => Math.Tan(inputs[0]),
                ["tanh"] = inputs => Math.Tanh(inputs[0]),
                ["atan"] = inputs => Math.Atan(inputs[0]),
                ["arctan"] = inputs => Math.Atan(inputs[0]),

                ["sqrt"] = inputs => Math.Sqrt(inputs[0]),
                ["pow"] = inputs => Math.Pow(inputs[0], inputs[1]),
                ["root"] = inputs => Math.Pow(inputs[0], 1 / inputs[1]),
                ["rem"] = inputs => Math.IEEERemainder(inputs[0], inputs[1]),

                ["sign"] = inputs => Math.Sign(inputs[0]),
                ["exp"] = inputs => Math.Exp(inputs[0]),

                ["floor"] = inputs => Math.Floor(inputs[0]),
                ["ceil"] = inputs => Math.Ceiling(inputs[0]),
                ["ceiling"] = inputs => Math.Ceiling(inputs[0]),
                ["round"] = inputs => Math.Round(inputs[0]),
                ["truncate"] = inputs => inputs[0] < 0 ? -Math.Floor(-inputs[0]) : Math.Floor(inputs[0]),

                ["log"] = inputs =>
                {
                    switch (inputs.Length)
                    {
                        case 1:
                            return Math.Log10(inputs[0]);
                        case 2:
                            return Math.Log(inputs[0], inputs[1]);
                        default:
                            return 0;
                    }
                },

                ["ln"] = inputs => Math.Log(inputs[0])
            };

            CultureInfo = cultureInfo ?? CultureInfo.InvariantCulture;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Список операции двух чисел
        /// </summary>
        public static Dictionary<string, Func<double, double, double>> OperationAction { get; set; }

        /// <summary>
        ///     Список математических операторов
        /// </summary>
        public static List<string> OperatorList { get; set; }

        /// <summary>
        ///     Все математические фунцкии, такие как например sin(x), cos(x) и т.д.
        /// </summary>
        public static Dictionary<string, Func<double[], double>> LocalFunctions { get; set; }

        /// <summary>
        ///     Статические значения, такие как например ПИ, e и т.д.
        /// </summary>
        public static Dictionary<string, double> LocalVariables { get; set; }

        public CultureInfo CultureInfo { get; }

        #endregion

        #region Methods

        /// <summary>
        ///     Находит и возвращает значения находящейся впереди члена
        /// </summary>
        /// <param name="first">Первый член</param>
        /// <param name="second">Второй член</param>
        protected double GetAheadValuesOfX(ref string first, ref string second, out double firstAheadValue)
        {
            if (first.Contains("x"))
                if (first.Length == 2 && (first.StartsWith("-") || first.StartsWith("+")))
                    first = first.Insert(1, "1");
                else if (first.Length == 1)
                    first = first.Insert(0, "1");

            if (second.Contains("x"))
                if (second.Length == 2 && (second.StartsWith("-") || second.StartsWith("+")))
                    second = second.Insert(1, "1");
                else if (second.Length == 1)
                    second = second.Insert(0, "1");

            firstAheadValue = double.Parse(first.Contains("x") ? first.Substring(0, first.IndexOf('x')) : first);
            var secondAheadValue =
                double.Parse(second.Contains("x") ? second.Substring(0, second.IndexOf('x')) : second);

            if (first.Contains("x"))
                first = first.Remove(0, first.IndexOf("x", StringComparison.Ordinal));
            if (second.Contains("x"))
                second = second.Remove(0, second.IndexOf("x", StringComparison.Ordinal));

            return secondAheadValue;
        }

        protected abstract List<string> Lexer(string expr);

        #endregion
    }
}
