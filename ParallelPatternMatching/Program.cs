using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace ParallelPatternMatching
{
    // ============================================================
    // МОДЕЛИ ЗА ДАННИ
    // ============================================================

    /// <summary>
    /// Резултат от търсене
    /// </summary>
    public class SearchResult
    {
        public string AlgorithmName { get; set; }
        public List<int> Positions { get; set; } = new List<int>();
        public double TimeMs { get; set; }
        public int ThreadCount { get; set; }
        public bool IsParallel { get; set; }
    }

    // ============================================================
    // KNUTH-MORRIS-PRATT АЛГОРИТЪМ
    // ============================================================

    public static class KMP
    {
        /// <summary>
        /// Изчислява failure function за шаблона
        /// </summary>
        public static int[] ComputeFailure(string pattern)
        {
            int m = pattern.Length;
            int[] failure = new int[m];
            int j = 0;

            for (int i = 1; i < m; i++)
            {
                while (j > 0 && pattern[i] != pattern[j])
                    j = failure[j - 1];

                if (pattern[i] == pattern[j])
                    j++;

                failure[i] = j;
            }

            return failure;
        }

        /// <summary>
        /// Последователно търсене
        /// </summary>
        public static SearchResult SearchSequential(string text, string pattern)
        {
            var result = new SearchResult
            {
                AlgorithmName = "KMP",
                IsParallel = false,
                ThreadCount = 1
            };

            var sw = Stopwatch.StartNew();

            int[] failure = ComputeFailure(pattern);
            int n = text.Length;
            int m = pattern.Length;
            int j = 0;

            for (int i = 0; i < n; i++)
            {
                while (j > 0 && text[i] != pattern[j])
                    j = failure[j - 1];

                if (text[i] == pattern[j])
                    j++;

                if (j == m)
                {
                    result.Positions.Add(i - m + 1);
                    j = failure[j - 1];
                }
            }

            sw.Stop();
            result.TimeMs = sw.Elapsed.TotalMilliseconds;

            return result;
        }

        /// <summary>
        /// Паралелно търсене
        /// </summary>
        public static SearchResult SearchParallel(string text, string pattern, int threadCount = 0)
        {
            if (threadCount <= 0)
                threadCount = Environment.ProcessorCount;

            var result = new SearchResult
            {
                AlgorithmName = "KMP",
                IsParallel = true,
                ThreadCount = threadCount
            };

            var sw = Stopwatch.StartNew();

            int[] failure = ComputeFailure(pattern);
            int n = text.Length;
            int m = pattern.Length;
            int chunkSize = (n + threadCount - 1) / threadCount;

            // Локални резултати за всяка нишка
            var localResults = new List<int>[threadCount];
            for (int i = 0; i < threadCount; i++)
                localResults[i] = new List<int>();

            Parallel.For(0, threadCount, threadId =>
            {
                int start = threadId * chunkSize;
                int end = Math.Min(start + chunkSize + m - 1, n);

                if (start >= n) return;

                int j = 0;

                for (int i = start; i < end; i++)
                {
                    while (j > 0 && text[i] != pattern[j])
                        j = failure[j - 1];

                    if (text[i] == pattern[j])
                        j++;

                    if (j == m)
                    {
                        int pos = i - m + 1;
                        // Записвай само позиции в основния chunk
                        if (pos >= start && pos < start + chunkSize)
                            localResults[threadId].Add(pos);

                        j = failure[j - 1];
                    }
                }
            });

            // Обединяване на резултатите
            foreach (var list in localResults)
                result.Positions.AddRange(list);

            result.Positions.Sort();

            sw.Stop();
            result.TimeMs = sw.Elapsed.TotalMilliseconds;

            return result;
        }
    }

    // ============================================================
    // BOYER-MOORE АЛГОРИТЪМ
    // ============================================================

    public static class BoyerMoore
    {
        /// <summary>
        /// Изчислява Bad Character таблица
        /// </summary>
        public static int[] ComputeBadChar(string pattern)
        {
            int[] badChar = new int[256];

            for (int i = 0; i < 256; i++)
                badChar[i] = -1;

            for (int i = 0; i < pattern.Length; i++)
                badChar[pattern[i]] = i;

            return badChar;
        }

        /// <summary>
        /// Последователно търсене
        /// </summary>
        public static SearchResult SearchSequential(string text, string pattern)
        {
            var result = new SearchResult
            {
                AlgorithmName = "Boyer-Moore",
                IsParallel = false,
                ThreadCount = 1
            };

            var sw = Stopwatch.StartNew();

            int[] badChar = ComputeBadChar(pattern);
            int n = text.Length;
            int m = pattern.Length;
            int i = 0;

            while (i <= n - m)
            {
                int j = m - 1;

                // Сравнение отдясно наляво
                while (j >= 0 && pattern[j] == text[i + j])
                    j--;

                if (j < 0)
                {
                    // Намерено съвпадение
                    result.Positions.Add(i);
                    i++;
                }
                else
                {
                    // Изместване според Bad Character правилото
                    int shift = j - badChar[text[i + j]];
                    i += Math.Max(1, shift);
                }
            }

            sw.Stop();
            result.TimeMs = sw.Elapsed.TotalMilliseconds;

            return result;
        }

        /// <summary>
        /// Паралелно търсене
        /// </summary>
        public static SearchResult SearchParallel(string text, string pattern, int threadCount = 0)
        {
            if (threadCount <= 0)
                threadCount = Environment.ProcessorCount;

            var result = new SearchResult
            {
                AlgorithmName = "Boyer-Moore",
                IsParallel = true,
                ThreadCount = threadCount
            };

            var sw = Stopwatch.StartNew();

            int[] badChar = ComputeBadChar(pattern);
            int n = text.Length;
            int m = pattern.Length;
            int chunkSize = (n + threadCount - 1) / threadCount;

            var localResults = new List<int>[threadCount];
            for (int i = 0; i < threadCount; i++)
                localResults[i] = new List<int>();

            Parallel.For(0, threadCount, threadId =>
            {
                int start = threadId * chunkSize;
                int end = Math.Min(start + chunkSize + m - 1, n);

                if (start >= n) return;

                int i = start;

                while (i <= end - m)
                {
                    int j = m - 1;

                    while (j >= 0 && pattern[j] == text[i + j])
                        j--;

                    if (j < 0)
                    {
                        if (i >= start && i < start + chunkSize)
                            localResults[threadId].Add(i);
                        i++;
                    }
                    else
                    {
                        int shift = j - badChar[text[i + j]];
                        i += Math.Max(1, shift);
                    }
                }
            });

            foreach (var list in localResults)
                result.Positions.AddRange(list);

            result.Positions.Sort();

            sw.Stop();
            result.TimeMs = sw.Elapsed.TotalMilliseconds;

            return result;
        }
    }

    // ============================================================
    // AHO-CORASICK АЛГОРИТЪМ
    // ============================================================

    public class AhoCorasick
    {
        /// <summary>
        /// Възел в Trie структурата
        /// </summary>
        private class Node
        {
            public Dictionary<char, Node> Children = new Dictionary<char, Node>();
            public Node Failure = null;
            public List<int> Output = new List<int>();
        }

        private Node _root = new Node();
        private List<string> _patterns = new List<string>();
        private bool _isBuilt = false;

        /// <summary>
        /// Добавя шаблон към автомата
        /// </summary>
        public void AddPattern(string pattern)
        {
            _patterns.Add(pattern);
            int patternIndex = _patterns.Count - 1;

            var node = _root;
            foreach (char c in pattern)
            {
                if (!node.Children.ContainsKey(c))
                    node.Children[c] = new Node();
                node = node.Children[c];
            }
            node.Output.Add(patternIndex);
        }

        /// <summary>
        /// Изгражда failure links чрез BFS
        /// </summary>
        public void Build()
        {
            var queue = new Queue<Node>();

            // Първо ниво - failure сочи към root
            foreach (var child in _root.Children.Values)
            {
                child.Failure = _root;
                queue.Enqueue(child);
            }

            // BFS за останалите нива
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var pair in current.Children)
                {
                    char c = pair.Key;
                    var child = pair.Value;
                    queue.Enqueue(child);

                    var failure = current.Failure;
                    while (failure != null && !failure.Children.ContainsKey(c))
                        failure = failure.Failure;

                    child.Failure = failure?.Children.GetValueOrDefault(c) ?? _root;

                    if (child.Failure == child)
                        child.Failure = _root;

                    // Копиране на output от failure
                    child.Output.AddRange(child.Failure.Output);
                }
            }

            _isBuilt = true;
        }

        /// <summary>
        /// Последователно търсене
        /// </summary>
        public SearchResult SearchSequential(string text)
        {
            if (!_isBuilt) Build();

            var result = new SearchResult
            {
                AlgorithmName = "Aho-Corasick",
                IsParallel = false,
                ThreadCount = 1
            };

            var sw = Stopwatch.StartNew();

            var node = _root;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                while (node != _root && !node.Children.ContainsKey(c))
                    node = node.Failure;

                if (node.Children.ContainsKey(c))
                    node = node.Children[c];

                foreach (int patternIdx in node.Output)
                {
                    int pos = i - _patterns[patternIdx].Length + 1;
                    result.Positions.Add(pos);
                }
            }

            result.Positions.Sort();

            sw.Stop();
            result.TimeMs = sw.Elapsed.TotalMilliseconds;

            return result;
        }

        /// <summary>
        /// Паралелно търсене
        /// </summary>
        public SearchResult SearchParallel(string text, int threadCount = 0)
        {
            if (!_isBuilt) Build();

            if (threadCount <= 0)
                threadCount = Environment.ProcessorCount;

            var result = new SearchResult
            {
                AlgorithmName = "Aho-Corasick",
                IsParallel = true,
                ThreadCount = threadCount
            };

            var sw = Stopwatch.StartNew();

            int n = text.Length;
            int maxLen = _patterns.Max(p => p.Length);
            int chunkSize = (n + threadCount - 1) / threadCount;

            var localResults = new List<int>[threadCount];
            for (int i = 0; i < threadCount; i++)
                localResults[i] = new List<int>();

            Parallel.For(0, threadCount, threadId =>
            {
                int start = threadId * chunkSize;
                int end = Math.Min(start + chunkSize + maxLen - 1, n);

                if (start >= n) return;

                var node = _root;

                for (int i = start; i < end; i++)
                {
                    char c = text[i];

                    while (node != _root && !node.Children.ContainsKey(c))
                        node = node.Failure;

                    if (node.Children.ContainsKey(c))
                        node = node.Children[c];

                    foreach (int patternIdx in node.Output)
                    {
                        int pos = i - _patterns[patternIdx].Length + 1;
                        if (pos >= start && pos < start + chunkSize)
                            localResults[threadId].Add(pos);
                    }
                }
            });

            foreach (var list in localResults)
                result.Positions.AddRange(list);

            result.Positions.Sort();

            sw.Stop();
            result.TimeMs = sw.Elapsed.TotalMilliseconds;

            return result;
        }

        public List<string> Patterns => _patterns;
    }

    // ============================================================
    // ГЕНЕРАТОР НА ТЕСТОВИ ДАННИ
    // ============================================================

    public static class TestDataGenerator
    {
        private static Random _random = new Random(42);

        /// <summary>
        /// Генерира случаен текст
        /// </summary>
        public static string GenerateText(int length, string alphabet = "ACGT")
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = alphabet[_random.Next(alphabet.Length)];
            return new string(chars);
        }

        /// <summary>
        /// Извлича случаен подниз от текст
        /// </summary>
        public static string ExtractPattern(string text, int length)
        {
            if (length > text.Length) length = text.Length;
            int start = _random.Next(text.Length - length);
            return text.Substring(start, length);
        }
    }

    // ============================================================
    // БЕНЧМАРК
    // ============================================================

    public static class Benchmark
    {
        /// <summary>
        /// Изпълнява бенчмарк за всички алгоритми
        /// </summary>
        public static void Run(string text, string pattern, int threadCount)
        {
            Console.WriteLine($"\nТекст: {text.Length:N0} символа");
            Console.WriteLine($"Шаблон: \"{Truncate(pattern, 30)}\" ({pattern.Length} символа)");
            Console.WriteLine($"Нишки: {threadCount}");
            Console.WriteLine(new string('─', 60));

            // KMP
            var kmpSeq = KMP.SearchSequential(text, pattern);
            var kmpPar = KMP.SearchParallel(text, pattern, threadCount);
            PrintResult("KMP", kmpSeq, kmpPar);

            // Boyer-Moore
            var bmSeq = BoyerMoore.SearchSequential(text, pattern);
            var bmPar = BoyerMoore.SearchParallel(text, pattern, threadCount);
            PrintResult("Boyer-Moore", bmSeq, bmPar);

            // Aho-Corasick
            var ac = new AhoCorasick();
            ac.AddPattern(pattern);
            ac.Build();
            var acSeq = ac.SearchSequential(text);
            var acPar = ac.SearchParallel(text, threadCount);
            PrintResult("Aho-Corasick", acSeq, acPar);
        }

        private static void PrintResult(string name, SearchResult seq, SearchResult par)
        {
            double speedup = seq.TimeMs / par.TimeMs;
            double efficiency = speedup / par.ThreadCount * 100;

            Console.WriteLine($"\n{name}:");
            Console.WriteLine($"  Sequential:  {seq.TimeMs,8:F3} ms");
            Console.WriteLine($"  Parallel:    {par.TimeMs,8:F3} ms");
            Console.WriteLine($"  Speedup:     {speedup,8:F2}x");
            Console.WriteLine($"  Efficiency:  {efficiency,8:F1}%");
            Console.WriteLine($"  Matches:     {seq.Positions.Count}");

            // Верификация
            bool valid = seq.Positions.SequenceEqual(par.Positions);
            Console.WriteLine($"  Verified:    {(valid ? "✓ OK" : "✗ ERROR")}");
        }

        private static string Truncate(string s, int maxLen)
        {
            return s.Length <= maxLen ? s : s.Substring(0, maxLen - 3) + "...";
        }
    }

    // ============================================================
    // ГЛАВНА ПРОГРАМА
    // ============================================================

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ПАРАЛЕЛИЗАЦИЯ НА АЛГОРИТМИ ЗА ТЪРСЕНЕ ПО ШАБЛОН       ║");
            Console.WriteLine("║  KMP | Boyer-Moore | Aho-Corasick                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝");

            while (true)
            {
                Console.WriteLine("\n--- МЕНЮ ---");
                Console.WriteLine("1. Бърз тест (малък текст)");
                Console.WriteLine("2. Среден тест (1 MB)");
                Console.WriteLine("3. Голям тест (10 MB)");
                Console.WriteLine("4. Тест за мащабируемост");
                Console.WriteLine("5. Собствен тест");
                Console.WriteLine("0. Изход");
                Console.Write("\nИзбор: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        QuickTest();
                        break;
                    case "2":
                        MediumTest();
                        break;
                    case "3":
                        LargeTest();
                        break;
                    case "4":
                        ScalabilityTest();
                        break;
                    case "5":
                        CustomTest();
                        break;
                    case "0":
                        Console.WriteLine("Довиждане!");
                        return;
                    default:
                        Console.WriteLine("Невалиден избор.");
                        break;
                }

                Console.WriteLine("\nНатиснете Enter за продължаване...");
                Console.ReadLine();
            }
        }

        static void QuickTest()
        {
            Console.WriteLine("\n=== БЪРЗ ТЕСТ ===");

            string text = "ABABDABACDABABCABABABABDABACDABABCABAB";
            string pattern = "ABABCABAB";

            Console.WriteLine($"Текст: {text}");
            Console.WriteLine($"Шаблон: {pattern}");
            Console.WriteLine();

            // KMP
            var kmp = KMP.SearchSequential(text, pattern);
            Console.WriteLine($"KMP намери на позиции: [{string.Join(", ", kmp.Positions)}]");

            // Boyer-Moore
            var bm = BoyerMoore.SearchSequential(text, pattern);
            Console.WriteLine($"Boyer-Moore намери на позиции: [{string.Join(", ", bm.Positions)}]");

            // Aho-Corasick
            var ac = new AhoCorasick();
            ac.AddPattern(pattern);
            ac.Build();
            var acResult = ac.SearchSequential(text);
            Console.WriteLine($"Aho-Corasick намери на позиции: [{string.Join(", ", acResult.Positions)}]");
        }

        static void MediumTest()
        {
            Console.WriteLine("\n=== СРЕДЕН ТЕСТ (1 MB) ===");

            string text = TestDataGenerator.GenerateText(1_000_000);
            string pattern = TestDataGenerator.ExtractPattern(text, 12);

            Benchmark.Run(text, pattern, Environment.ProcessorCount);
        }

        static void LargeTest()
        {
            Console.WriteLine("\n=== ГОЛЯМ ТЕСТ (10 MB) ===");

            string text = TestDataGenerator.GenerateText(10_000_000);
            string pattern = TestDataGenerator.ExtractPattern(text, 15);

            Benchmark.Run(text, pattern, Environment.ProcessorCount);
        }

        static void ScalabilityTest()
        {
            Console.WriteLine("\n=== ТЕСТ ЗА МАЩАБИРУЕМОСТ ===");

            string text = TestDataGenerator.GenerateText(5_000_000);
            string pattern = TestDataGenerator.ExtractPattern(text, 12);

            Console.WriteLine($"Текст: {text.Length:N0} символа");
            Console.WriteLine($"Шаблон: {pattern.Length} символа\n");

            int[] threadCounts = { 1, 2, 4, 8 };

            Console.WriteLine("Нишки |    KMP    | Boyer-Moore | Aho-Corasick");
            Console.WriteLine("------+-----------+-------------+-------------");

            // Baseline (sequential)
            var kmpBase = KMP.SearchSequential(text, pattern).TimeMs;
            var bmBase = BoyerMoore.SearchSequential(text, pattern).TimeMs;
            var ac = new AhoCorasick();
            ac.AddPattern(pattern);
            ac.Build();
            var acBase = ac.SearchSequential(text).TimeMs;

            foreach (int threads in threadCounts)
            {
                double kmpTime, bmTime, acTime;

                if (threads == 1)
                {
                    kmpTime = kmpBase;
                    bmTime = bmBase;
                    acTime = acBase;
                }
                else
                {
                    kmpTime = KMP.SearchParallel(text, pattern, threads).TimeMs;
                    bmTime = BoyerMoore.SearchParallel(text, pattern, threads).TimeMs;
                    acTime = ac.SearchParallel(text, threads).TimeMs;
                }

                double kmpSpeedup = kmpBase / kmpTime;
                double bmSpeedup = bmBase / bmTime;
                double acSpeedup = acBase / acTime;

                Console.WriteLine($"  {threads,2}   |   {kmpSpeedup,5:F2}x   |    {bmSpeedup,5:F2}x    |    {acSpeedup,5:F2}x");
            }
        }

        static void CustomTest()
        {
            Console.WriteLine("\n=== СОБСТВЕН ТЕСТ ===");

            Console.Write("Въведете текст (или 'gen:N' за генериране на N символа): ");
            string textInput = Console.ReadLine();

            string text;
            if (textInput.StartsWith("gen:"))
            {
                int size = int.Parse(textInput.Substring(4));
                text = TestDataGenerator.GenerateText(size);
                Console.WriteLine($"Генерирани {size:N0} символа.");
            }
            else
            {
                text = textInput;
            }

            Console.Write("Въведете шаблон: ");
            string pattern = Console.ReadLine();

            Console.Write("Брой нишки (Enter за auto): ");
            string threadInput = Console.ReadLine();
            int threads = string.IsNullOrEmpty(threadInput)
                ? Environment.ProcessorCount
                : int.Parse(threadInput);

            Benchmark.Run(text, pattern, threads);
        }
    }
}
