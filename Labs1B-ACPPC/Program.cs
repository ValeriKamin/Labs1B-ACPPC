using System;
using System.Collections.Generic;
using System.Globalization;

class Program
{
    const double EPS = 1e-9;

    static int n, m;
    static double[] c;
    static double[,] a;
    static double[] b;
    static string[] signs;
    static bool isMax;

    static double[,] table;
    static int[] basis;
    static double[] currentC;
    static bool[] artificial;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        while (true)
        {
            Console.WriteLine("\nПРАКТИЧНА РОБОТА №1В");
            Console.WriteLine("1 - Ввести задачу");
            Console.WriteLine("2 - Завантажити приклад");
            Console.WriteLine("3 - Показати задачу");
            Console.WriteLine("4 - Розв'язати задачу");
            Console.WriteLine("0 - Вихід");
            Console.Write("Ваш вибір: ");

            int choice = int.Parse(Console.ReadLine());

            if (choice == 0) break;

            switch (choice)
            {
                case 1:
                    InputProblem();
                    break;
                case 2:
                    LoadExample();
                    break;
                case 3:
                    if (Exists()) PrintProblem();
                    break;
                case 4:
                    if (Exists()) Solve();
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
        n = int.Parse(Console.ReadLine());

        Console.Write("Кількість обмежень: ");
        m = int.Parse(Console.ReadLine());

        Console.Write("Тип задачі max/min: ");
        isMax = Console.ReadLine().Trim().ToLower() == "max";

        c = new double[n];
        a = new double[m, n];
        b = new double[m];
        signs = new string[m];

        Console.WriteLine("\nКоефіцієнти функції Z:");
        for (int j = 0; j < n; j++)
        {
            Console.Write($"c[{j + 1}] = ");
            c[j] = double.Parse(Console.ReadLine());
        }

        Console.WriteLine("\nОбмеження:");
        for (int i = 0; i < m; i++)
        {
            Console.WriteLine($"Обмеження {i + 1}");

            for (int j = 0; j < n; j++)
            {
                Console.Write($"a[{i + 1},{j + 1}] = ");
                a[i, j] = double.Parse(Console.ReadLine());
            }

            Console.Write("Знак <=, >= або = : ");
            signs[i] = Console.ReadLine().Trim();

            Console.Write($"b[{i + 1}] = ");
            b[i] = double.Parse(Console.ReadLine());
        }

        Console.WriteLine("Задачу збережено.");
    }

    static void LoadExample()
    {
        n = 4;
        m = 3;
        isMax = true;


        c = new double[] { -2, -1, 1, 1 };

        a = new double[,]
        {
        { 2, -1, 3, 4 },
        { 1,  1, 1,-1 },
        { 1,  2, 2, 4 }
        };

        signs = new string[] { "<=", "<=", "<=" };

        b = new double[] { 10, 5, 12 };

        Console.WriteLine("Завантажено варіант 12.");
    }

    static bool Exists()
    {
        if (c == null)
        {
            Console.WriteLine("Спочатку введіть задачу.");
            return false;
        }

        return true;
    }

    static void PrintProblem()
    {
        Console.WriteLine("\nПостановка задачі:");
        Console.Write("Z = ");

        for (int j = 0; j < n; j++)
        {
            if (j > 0 && c[j] >= 0) Console.Write("+ ");
            Console.Write($"{c[j]}x{j + 1} ");
        }

        Console.WriteLine(isMax ? "-> max" : "-> min");

        Console.WriteLine("при обмеженнях:");
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                if (j > 0 && a[i, j] >= 0) Console.Write("+ ");
                Console.Write($"{a[i, j]}x{j + 1} ");
            }

            Console.WriteLine($"{signs[i]} {b[i]}");
        }

        Console.WriteLine("xj >= 0");
    }

    static void Solve()
    {
        PrintProblem();

        Console.WriteLine("\nПерехід до задачі максимуму:");
        double[] target = new double[n];

        for (int j = 0; j < n; j++)
            target[j] = isMax ? c[j] : -c[j];

        BuildTable(target);

        Console.WriteLine("\nПочаткова симплекс-таблиця:");
        PrintTable();

        Console.WriteLine("\nЕТАП 1. Пошук опорного розв'язку");
        double[] phase1C = new double[table.GetLength(1) - 1];

        for (int j = 0; j < artificial.Length; j++)
            phase1C[j] = artificial[j] ? -1 : 0;

        currentC = phase1C;
        RecalculateZRow();

        SimplexProcess(true);

        double phaseValue = table[m, table.GetLength(1) - 1];

        if (Math.Abs(phaseValue) > EPS)
        {
            Console.WriteLine("Допустимого опорного розв'язку не існує.");
            return;
        }

        Console.WriteLine("\nОпорний розв'язок знайдено.");

        Console.WriteLine("\nЕТАП 2. Пошук оптимального розв'язку");
        double[] phase2C = new double[table.GetLength(1) - 1];

        for (int j = 0; j < n; j++)
            phase2C[j] = target[j];

        currentC = phase2C;
        RecalculateZRow();

        SimplexProcess(false);

        PrintAnswer(target);
    }

    static void BuildTable(double[] target)
    {
        int slackCount = 0;
        int artificialCount = 0;

        for (int i = 0; i < m; i++)
        {
            NormalizeConstraint(i);

            if (signs[i] == "<=") slackCount++;
            else if (signs[i] == ">=")
            {
                slackCount++;
                artificialCount++;
            }
            else if (signs[i] == "=") artificialCount++;
        }

        int totalVars = n + slackCount + artificialCount;
        table = new double[m + 1, totalVars + 1];
        basis = new int[m];
        artificial = new bool[totalVars];

        int slackIndex = n;
        int artificialIndex = n + slackCount;

        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
                table[i, j] = a[i, j];

            if (signs[i] == "<=")
            {
                table[i, slackIndex] = 1;
                basis[i] = slackIndex;
                slackIndex++;
            }
            else if (signs[i] == ">=")
            {
                table[i, slackIndex] = -1;
                slackIndex++;

                table[i, artificialIndex] = 1;
                artificial[artificialIndex] = true;
                basis[i] = artificialIndex;
                artificialIndex++;
            }
            else
            {
                table[i, artificialIndex] = 1;
                artificial[artificialIndex] = true;
                basis[i] = artificialIndex;
                artificialIndex++;
            }

            table[i, totalVars] = b[i];
        }
    }

    static void NormalizeConstraint(int i)
    {
        if (b[i] >= 0) return;

        for (int j = 0; j < n; j++)
            a[i, j] *= -1;

        b[i] *= -1;

        if (signs[i] == "<=") signs[i] = ">=";
        else if (signs[i] == ">=") signs[i] = "<=";
    }

    static void SimplexProcess(bool phase1)
    {
        int step = 1;

        while (true)
        {
            int pivotCol = FindPivotColumn(phase1);

            if (pivotCol == -1)
            {
                Console.WriteLine("Критерій оптимальності виконано.");
                break;
            }

            int pivotRow = FindPivotRow(pivotCol);

            if (pivotRow == -1)
            {
                Console.WriteLine("Цільова функція необмежена.");
                return;
            }

            Console.WriteLine($"\nКрок {step}");
            Console.WriteLine($"Розв'язувальний стовпець: {VarName(pivotCol)}");
            Console.WriteLine($"Розв'язувальний рядок: y{pivotRow + 1}");
            Console.WriteLine($"Розв'язувальний елемент: {table[pivotRow, pivotCol]:F4}");

            basis[pivotRow] = pivotCol;
            ModifiedJordanStep(pivotRow, pivotCol);
            RecalculateZRow();

            PrintTable();

            step++;
        }
    }

    static int FindPivotColumn(bool phase1)
    {
        int lastRow = m;
        int lastCol = table.GetLength(1) - 1;

        int index = -1;
        double min = 0;

        for (int j = 0; j < lastCol; j++)
        {
            if (!phase1 && artificial[j]) continue;

            if (table[lastRow, j] < min)
            {
                min = table[lastRow, j];
                index = j;
            }
        }

        return index;
    }

    static int FindPivotRow(int pivotCol)
    {
        int lastCol = table.GetLength(1) - 1;
        int index = -1;
        double minRatio = double.MaxValue;

        Console.WriteLine("\nВідношення b / a:");

        for (int i = 0; i < m; i++)
        {
            if (table[i, pivotCol] > EPS)
            {
                double ratio = table[i, lastCol] / table[i, pivotCol];
                Console.WriteLine($"y{i + 1}: {table[i, lastCol]:F4} / {table[i, pivotCol]:F4} = {ratio:F4}");

                if (ratio < minRatio)
                {
                    minRatio = ratio;
                    index = i;
                }
            }
            else
            {
                Console.WriteLine($"y{i + 1}: не розглядається");
            }
        }

        return index;
    }

    static void ModifiedJordanStep(int pivotRow, int pivotCol)
    {
        int rows = table.GetLength(0);
        int cols = table.GetLength(1);
        double pivot = table[pivotRow, pivotCol];

        for (int j = 0; j < cols; j++)
            table[pivotRow, j] /= pivot;

        for (int i = 0; i < rows; i++)
        {
            if (i == pivotRow) continue;

            double factor = table[i, pivotCol];

            for (int j = 0; j < cols; j++)
                table[i, j] -= factor * table[pivotRow, j];
        }
    }

    static void RecalculateZRow()
    {
        int lastRow = m;
        int lastCol = table.GetLength(1) - 1;

        for (int j = 0; j <= lastCol; j++)
            table[lastRow, j] = 0;

        for (int j = 0; j < lastCol; j++)
        {
            double zj = 0;

            for (int i = 0; i < m; i++)
                zj += currentC[basis[i]] * table[i, j];

            table[lastRow, j] = zj - currentC[j];
        }

        double z = 0;

        for (int i = 0; i < m; i++)
            z += currentC[basis[i]] * table[i, lastCol];

        table[lastRow, lastCol] = z;
    }

    static void PrintTable()
    {
        int cols = table.GetLength(1);

        Console.Write("\nБазис\t");
        for (int j = 0; j < cols - 1; j++)
            Console.Write($"{VarName(j)}\t");
        Console.WriteLine("b");

        for (int i = 0; i < m; i++)
        {
            Console.Write($"{VarName(basis[i])}\t");

            for (int j = 0; j < cols; j++)
                Console.Write($"{table[i, j]:F2}\t");

            Console.WriteLine();
        }

        Console.Write("Z\t");
        for (int j = 0; j < cols; j++)
            Console.Write($"{table[m, j]:F2}\t");

        Console.WriteLine("\n");
    }

    static void PrintAnswer(double[] target)
    {
        int lastCol = table.GetLength(1) - 1;
        double[] x = new double[n];

        for (int i = 0; i < m; i++)
        {
            if (basis[i] < n)
                x[basis[i]] = table[i, lastCol];
        }

        double z = 0;
        for (int j = 0; j < n; j++)
            z += c[j] * x[j];

        Console.WriteLine("\nОптимальний розв'язок:");
        Console.Write("X = (");

        for (int j = 0; j < n; j++)
        {
            Console.Write($"{x[j]:F4}");
            if (j < n - 1) Console.Write("; ");
        }

        Console.WriteLine(")");
        Console.WriteLine(isMax ? $"Max(Z) = {z:F4}" : $"Min(Z) = {z:F4}");
    }

    static string VarName(int index)
    {
        if (index < n) return $"x{index + 1}";
        if (artificial != null && artificial[index]) return $"a{index + 1}";
        return $"s{index - n + 1}";
    }
}