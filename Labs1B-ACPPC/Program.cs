using System;
using System.Globalization;

class Program
{
    const double EPS = 1e-10;

    static int variablesCount;
    static int constraintsCount;

    static double[] objective;
    static double[,] constraints;
    static double[] rightSide;
    static string[] signs;
    static bool isMax;

    static double[,] table;
    static string[] rowLabels;
    static string[] colLabels;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        while (true)
        {
            Console.WriteLine("\nПРАКТИЧНА РОБОТА №1В");
            Console.WriteLine("Модифіковані жорданові виключення");
            Console.WriteLine("1 - Ввести задачу вручну");
            Console.WriteLine("2 - Завантажити приклад з методички");
            Console.WriteLine("3 - Завантажити мій варіант 12");
            Console.WriteLine("4 - Показати задачу");
            Console.WriteLine("5 - Розв'язати задачу");
            Console.WriteLine("0 - Вихід");
            Console.Write("Ваш вибір: ");

            int choice = int.Parse(Console.ReadLine());

            if (choice == 0)
                break;

            switch (choice)
            {
                case 1:
                    InputProblem();
                    break;

                case 2:
                    LoadExample();
                    break;

                case 3:
                    LoadVariant12();
                    break;

                case 4:
                    if (CheckProblem()) PrintProblem();
                    break;

                case 5:
                    if (CheckProblem()) Solve();
                    break;

                default:
                    Console.WriteLine("Невірний вибір.");
                    break;
            }
        }
    }


    static void InputProblem()
    {
        Console.Write("Кількість змінних: ");
        variablesCount = int.Parse(Console.ReadLine());

        Console.Write("Кількість обмежень: ");
        constraintsCount = int.Parse(Console.ReadLine());

        Console.Write("Тип задачі max/min: ");
        isMax = Console.ReadLine().Trim().ToLower() == "max";

        objective = new double[variablesCount];
        constraints = new double[constraintsCount, variablesCount];
        rightSide = new double[constraintsCount];
        signs = new string[constraintsCount];

        Console.WriteLine("\nКоефіцієнти функції Z:");
        for (int j = 0; j < variablesCount; j++)
        {
            Console.Write($"c[{j + 1}] = ");
            objective[j] = double.Parse(Console.ReadLine());
        }

        Console.WriteLine("\nОбмеження:");
        for (int i = 0; i < constraintsCount; i++)
        {
            Console.WriteLine($"Обмеження {i + 1}:");

            for (int j = 0; j < variablesCount; j++)
            {
                Console.Write($"a[{i + 1},{j + 1}] = ");
                constraints[i, j] = double.Parse(Console.ReadLine());
            }

            Console.Write("Знак <=, >= або = : ");
            signs[i] = Console.ReadLine().Trim();

            Console.Write($"b[{i + 1}] = ");
            rightSide[i] = double.Parse(Console.ReadLine());
        }

        Console.WriteLine("Задачу збережено.");
    }

    static void LoadExample()
    {
        variablesCount = 4;
        constraintsCount = 3;
        isMax = true;

        objective = new double[] { 1, 2, -1, -1 };

        constraints = new double[,]
        {
            { 1,  1, -1, -2 },
            { 1,  1,  1, -1 },
            { 2, -1,  3,  4 }
        };

        signs = new string[] { "<=", ">=", "<=" };
        rightSide = new double[] { 6, 5, 10 };

        Console.WriteLine("Завантажено приклад з методички.");
    }

    static void LoadVariant12()
    {
        variablesCount = 4;
        constraintsCount = 3;
        isMax = true;

        objective = new double[] { -2, -1, 1, 1 };

        constraints = new double[,]
        {
            { 2, -1, 3, 4 },
            { 1,  1, 1, -1 },
            { 1,  2, 2, 4 }
        };

        signs = new string[] { "<=", "<=", "<=" };
        rightSide = new double[] { 10, 5, 12 };

        Console.WriteLine("Завантажено варіант 12.");
    }

    static bool CheckProblem()
    {
        if (objective == null)
        {
            Console.WriteLine("Спочатку потрібно ввести або завантажити задачу.");
            return false;
        }

        return true;
    }


    static void Solve()
    {
        Console.WriteLine("\nЗгенерований протокол обчислення:\n");

        PrintProblem();

        if (!isMax)
        {
            Console.WriteLine("\nПерехід до задачі максимізації функції мети Z':");

            Console.Write("Z' = ");
            for (int j = 0; j < variablesCount; j++)
            {
                double value = -objective[j];
                PrintTerm(value, $"X[{j + 1}]", j == 0);
            }
            Console.WriteLine(" -> max");
        }

        Console.WriteLine("\nПерепишемо систему обмежень:");
        BuildSimplexTable();

        PrintRewrittenSystem();

        Console.WriteLine("\nВхідна симплекс-таблиця:");
        PrintTable();

        Console.WriteLine("Пошук опорного розв'язку:");
        FindSupportSolution();

        Console.WriteLine("\nЗнайдено опорний розв'язок:");
        PrintCurrentX();

        Console.WriteLine("\nПошук оптимального розв'язку:");
        FindOptimalSolution();

        Console.WriteLine("\nЗнайдено оптимальний розв'язок:");
        PrintCurrentX();

        double zValue = table[table.GetLength(0) - 1, table.GetLength(1) - 1];

        if (isMax)
            Console.WriteLine($"\nMax (Z) = {zValue:F2}");
        else
            Console.WriteLine($"\nMin (Z) = {-zValue:F2}");
    }

    static void BuildSimplexTable()
    {
        int rows = constraintsCount + 1;
        int cols = variablesCount + 1;

        table = new double[rows, cols];
        rowLabels = new string[rows];
        colLabels = new string[cols];

        for (int j = 0; j < variablesCount; j++)
            colLabels[j] = $"-x{j + 1}";

        colLabels[cols - 1] = "1";

        for (int i = 0; i < constraintsCount; i++)
        {
            rowLabels[i] = $"y{i + 1}";

            if (signs[i] == "<=")
            {
                for (int j = 0; j < variablesCount; j++)
                    table[i, j] = constraints[i, j];

                table[i, cols - 1] = rightSide[i];
            }
            else if (signs[i] == ">=")
            {
                for (int j = 0; j < variablesCount; j++)
                    table[i, j] = -constraints[i, j];

                table[i, cols - 1] = -rightSide[i];
            }
            else
            {
                Console.WriteLine("Увага: знак '=' у цій версії бажано замінити двома нерівностями.");
            }
        }

        rowLabels[rows - 1] = "Z";

        for (int j = 0; j < variablesCount; j++)
        {
            if (isMax)
                table[rows - 1, j] = -objective[j];
            else
                table[rows - 1, j] = objective[j];
        }

        table[rows - 1, cols - 1] = 0;
    }

    static void FindSupportSolution()
    {
        while (true)
        {
            int pivotRow = FindNegativeFreeTermRow();

            if (pivotRow == -1)
                break;

            int pivotCol = FindNegativeElementInRow(pivotRow);

            if (pivotCol == -1)
            {
                Console.WriteLine("Опорний розв'язок не існує.");
                return;
            }

            Console.WriteLine($"\nРозв'язувальний рядок: {rowLabels[pivotRow]}");
            Console.WriteLine($"Розв'язувальний стовпець: {colLabels[pivotCol]}");

            ModifiedJordanStep(pivotRow, pivotCol);
            PrintTable();
        }
    }

    static void FindOptimalSolution()
    {
        while (true)
        {
            int pivotCol = FindNegativeElementInZRow();

            if (pivotCol == -1)
                break;

            int pivotRow = FindResolvingRowForOptimal(pivotCol);

            if (pivotRow == -1)
            {
                Console.WriteLine("Цільова функція необмежена.");
                return;
            }

            Console.WriteLine($"\nРозв'язувальний рядок: {rowLabels[pivotRow]}");
            Console.WriteLine($"Розв'язувальний стовпець: {colLabels[pivotCol]}");

            ModifiedJordanStep(pivotRow, pivotCol);
            PrintTable();
        }
    }


    static void ModifiedJordanStep(int pivotRow, int pivotCol)
    {
        int rows = table.GetLength(0);
        int cols = table.GetLength(1);

        double[,] old = CopyMatrix(table);
        double pivot = old[pivotRow, pivotCol];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (i == pivotRow && j == pivotCol)
                {
                    table[i, j] = 1.0 / pivot;
                }
                else if (i == pivotRow)
                {
                    table[i, j] = old[i, j] / pivot;
                }
                else if (j == pivotCol)
                {
                    table[i, j] = -old[i, j] / pivot;
                }
                else
                {
                    table[i, j] =
                        (old[i, j] * pivot - old[i, pivotCol] * old[pivotRow, j]) / pivot;
                }
            }
        }

        string temp = rowLabels[pivotRow];

        if (colLabels[pivotCol].StartsWith("-x"))
            rowLabels[pivotRow] = colLabels[pivotCol].Substring(1);
        else
            rowLabels[pivotRow] = colLabels[pivotCol].Substring(1);

        colLabels[pivotCol] = "-" + temp;
    }


    static int FindNegativeFreeTermRow()
    {
        int lastCol = table.GetLength(1) - 1;

        for (int i = 0; i < constraintsCount; i++)
        {
            if (table[i, lastCol] < -EPS)
                return i;
        }

        return -1;
    }

    static int FindNegativeElementInRow(int row)
    {
        int lastCol = table.GetLength(1) - 1;

        for (int j = 0; j < lastCol; j++)
        {
            if (table[row, j] < -EPS)
                return j;
        }

        return -1;
    }

    static int FindNegativeElementInZRow()
    {
        int lastRow = table.GetLength(0) - 1;
        int lastCol = table.GetLength(1) - 1;

        for (int j = 0; j < lastCol; j++)
        {
            if (table[lastRow, j] < -EPS)
                return j;
        }

        return -1;
    }

    static int FindResolvingRowForOptimal(int pivotCol)
    {
        int lastCol = table.GetLength(1) - 1;

        int bestRow = -1;
        double bestRatio = double.MaxValue;

        for (int i = 0; i < constraintsCount; i++)
        {
            if (table[i, pivotCol] > EPS)
            {
                double ratio = table[i, lastCol] / table[i, pivotCol];

                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    bestRow = i;
                }
            }
        }

        return bestRow;
    }


    static void PrintProblem()
    {
        Console.WriteLine("Постановка задачі:\n");

        Console.Write("Z = ");

        for (int j = 0; j < variablesCount; j++)
            PrintTerm(objective[j], $"x{j + 1}", j == 0);

        Console.WriteLine(isMax ? " -> max" : " -> min");

        Console.WriteLine("\nпри обмеженнях:\n");

        for (int i = 0; i < constraintsCount; i++)
        {
            for (int j = 0; j < variablesCount; j++)
                PrintTerm(constraints[i, j], $"x{j + 1}", j == 0);

            Console.WriteLine($" {signs[i]} {rightSide[i]}");
        }

        Console.WriteLine("\nx[j]>=0, j=1,4");
    }

    static void PrintRewrittenSystem()
    {
        int lastCol = table.GetLength(1) - 1;

        for (int i = 0; i < constraintsCount; i++)
        {
            for (int j = 0; j < variablesCount; j++)
            {
                Console.Write($"{table[i, j]:F2} * X[{j + 1}]");

                if (j < variablesCount - 1)
                    Console.Write(" + ");
            }

            Console.WriteLine($" + {table[i, lastCol]:F2} >= 0");
        }
    }

    static void PrintTable()
    {
        int rows = table.GetLength(0);
        int cols = table.GetLength(1);

        Console.WriteLine();

        Console.Write("        ");

        for (int j = 0; j < cols; j++)
            Console.Write($"{colLabels[j],10}");

        Console.WriteLine();

        Console.WriteLine(new string('-', 10 + cols * 10));

        for (int i = 0; i < rows; i++)
        {
            Console.Write($"{rowLabels[i],6} = ");

            for (int j = 0; j < cols; j++)
                Console.Write($"{table[i, j],10:F2}");

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    static void PrintCurrentX()
    {
        double[] x = new double[variablesCount];
        int lastCol = table.GetLength(1) - 1;

        for (int i = 0; i < constraintsCount; i++)
        {
            if (rowLabels[i].StartsWith("x"))
            {
                int index = int.Parse(rowLabels[i].Substring(1)) - 1;

                if (index >= 0 && index < variablesCount)
                    x[index] = table[i, lastCol];
            }
        }

        Console.Write("X = (");

        for (int i = 0; i < variablesCount; i++)
        {
            Console.Write($"{x[i]:F2}");

            if (i < variablesCount - 1)
                Console.Write("; ");
        }

        Console.WriteLine(")");
    }

    static void PrintTerm(double value, string name, bool first)
    {
        if (Math.Abs(value) < EPS)
            return;

        if (first)
        {
            if (value < 0)
                Console.Write($"-{Math.Abs(value)}{name}");
            else
                Console.Write($"{value}{name}");
        }
        else
        {
            if (value < 0)
                Console.Write($" - {Math.Abs(value)}{name}");
            else
                Console.Write($" + {value}{name}");
        }
    }


    static double[,] CopyMatrix(double[,] source)
    {
        int rows = source.GetLength(0);
        int cols = source.GetLength(1);

        double[,] result = new double[rows, cols];

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result[i, j] = source[i, j];

        return result;
    }
}