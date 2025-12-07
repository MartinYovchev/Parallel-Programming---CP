# Parallel Pattern Matching

**[Български](README.bg.md)** | **English**

A high-performance C# implementation of parallel pattern matching algorithms with comprehensive benchmarking suite.

## Overview

This project implements three classic string pattern matching algorithms with both sequential and parallel versions, demonstrating the benefits of parallelization for computationally intensive text processing tasks.

## Implemented Algorithms

### 1. Knuth-Morris-Pratt (KMP)
- **Time Complexity**: O(n + m)
- **Preprocessing**: Computes failure function for pattern
- **Approach**: Linear-time matching using prefix information
- **Best for**: General-purpose pattern matching

### 2. Boyer-Moore
- **Time Complexity**: O(n/m) average, O(nm) worst case
- **Preprocessing**: Computes bad character table
- **Approach**: Right-to-left scanning with intelligent skipping
- **Best for**: Large alphabets and long patterns

### 3. Aho-Corasick
- **Time Complexity**: O(n + m + z) where z is matches
- **Preprocessing**: Builds trie with failure links
- **Approach**: Finite state automaton for multi-pattern matching
- **Best for**: Searching multiple patterns simultaneously

## Features

- **Parallel Implementations**: All algorithms include optimized parallel versions
- **Performance Benchmarking**: Detailed metrics including:
  - Execution time (sequential vs parallel)
  - Speedup ratio
  - Thread efficiency percentage
  - Result verification
- **Test Data Generator**: Configurable random text generation
- **Interactive Menu**: Multiple test scenarios
- **Thread Scalability**: Automatic or manual thread count configuration

## Requirements

- .NET 6.0 or later
- Multi-core processor (recommended for parallel testing)

## Installation

```bash
# Clone or download the project
cd ParallelPatternMatching

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release
```

## Usage

### Running the Application

```bash
dotnet run -c Release
```

### Menu Options

**1. Quick Test (Small Text)**
- Demonstrates exact pattern matching on a small example
- Shows positions where pattern is found
- Useful for understanding algorithm behavior

**2. Medium Test (1 MB)**
- Generates 1 million random characters
- Extracts a 12-character pattern
- Compares sequential vs parallel performance

**3. Large Test (10 MB)**
- Generates 10 million random characters
- Extracts a 15-character pattern
- Stress tests parallel implementations

**4. Scalability Test**
- Tests algorithms with 1, 2, 4, and 8 threads
- Displays speedup ratios for each configuration
- Helps identify optimal thread count

**5. Custom Test**
- Enter your own text and pattern
- Use `gen:N` to generate N random characters
- Specify custom thread count

## Example Output

```
=== СРЕДЕН ТЕСТ (1 MB) ===

Текст: 1,000,000 символа
Шаблон: "ACGTACGTACGT" (12 символа)
Нишки: 8
────────────────────────────────────────────────────────────

KMP:
  Sequential:     8.523 ms
  Parallel:       1.856 ms
  Speedup:        4.59x
  Efficiency:     57.4%
  Matches:        976
  Verified:       ✓ OK

Boyer-Moore:
  Sequential:     5.234 ms
  Parallel:       1.425 ms
  Speedup:        3.67x
  Efficiency:     45.9%
  Matches:        976
  Verified:       ✓ OK

Aho-Corasick:
  Sequential:     7.891 ms
  Parallel:       1.723 ms
  Speedup:        4.58x
  Efficiency:     57.2%
  Matches:        976
  Verified:       ✓ OK
```

## Architecture

### Project Structure

```
ParallelPatternMatching/
├── Program.cs              # Main implementation
├── README.md              # This file
└── ParallelPatternMatching.csproj
```

### Key Components

**Data Models**
- `SearchResult`: Encapsulates algorithm results and performance metrics

**Algorithm Classes**
- `KMP`: Knuth-Morris-Pratt implementation
- `BoyerMoore`: Boyer-Moore implementation
- `AhoCorasick`: Aho-Corasick implementation

**Utilities**
- `TestDataGenerator`: Random text and pattern generation
- `Benchmark`: Performance testing framework

### Parallelization Strategy

All algorithms use **chunk-based parallelization**:

1. **Text Division**: Input text divided into chunks (one per thread)
2. **Overlap Handling**: Chunks include overlap regions to catch boundary matches
3. **Local Results**: Each thread maintains its own result list
4. **Aggregation**: Results merged and sorted after parallel execution
5. **Verification**: Parallel results compared against sequential baseline

#### Chunk Calculation

```csharp
int chunkSize = (n + threadCount - 1) / threadCount;
int start = threadId * chunkSize;
int end = Math.Min(start + chunkSize + patternLength - 1, n);
```

The overlap of `patternLength - 1` ensures no matches are missed at chunk boundaries.

## Performance Characteristics

### Expected Speedup

On a quad-core processor, typical speedup ranges:
- **KMP**: 2.5x - 3.5x
- **Boyer-Moore**: 2.0x - 3.0x
- **Aho-Corasick**: 2.5x - 3.5x

Efficiency decreases with more threads due to:
- Synchronization overhead
- Memory bandwidth limitations
- Cache effects

### When Parallelization Helps

Parallelization is most effective when:
- Text size > 100,000 characters
- Pattern is relatively short (< 100 characters)
- Multiple CPU cores available
- Memory bandwidth is sufficient

### When Sequential is Better

Use sequential versions when:
- Text size < 10,000 characters
- Single-core environment
- Memory is constrained
- Overhead of thread creation exceeds benefits

## Algorithm Comparison

| Algorithm     | Preprocessing | Best Case | Average Case | Worst Case | Space    |
|---------------|---------------|-----------|--------------|------------|----------|
| KMP           | O(m)          | O(n)      | O(n)         | O(n)       | O(m)     |
| Boyer-Moore   | O(m + σ)      | O(n/m)    | O(n)         | O(nm)      | O(m + σ) |
| Aho-Corasick  | O(Σm)         | O(n)      | O(n + z)     | O(n + z)   | O(Σm)    |

Where:
- n = text length
- m = pattern length
- σ = alphabet size
- Σm = sum of all pattern lengths
- z = number of matches

## Customization

### Changing the Alphabet

Modify the `TestDataGenerator.GenerateText` call:

```csharp
// DNA sequences (default)
string text = TestDataGenerator.GenerateText(1000000, "ACGT");

// Binary
string text = TestDataGenerator.GenerateText(1000000, "01");

// Lowercase letters
string text = TestDataGenerator.GenerateText(1000000, "abcdefghijklmnopqrstuvwxyz");
```

### Adding New Test Cases

Extend the menu in `Program.Main()`:

```csharp
case "6":
    MyCustomTest();
    break;
```

### Adjusting Thread Count

```csharp
// Use all available cores (default)
var result = KMP.SearchParallel(text, pattern);

// Use specific thread count
var result = KMP.SearchParallel(text, pattern, 4);
```

## Technical Notes

### Thread Safety

All parallel implementations are thread-safe:
- Each thread writes to its own local result list
- No shared mutable state during search phase
- Final aggregation happens after all threads complete

### Memory Considerations

For large texts (> 100 MB):
- Consider streaming approaches
- Monitor memory usage
- Adjust chunk sizes if needed

### Platform Differences

Performance may vary based on:
- CPU architecture (x86 vs ARM)
- Core count and hyperthreading
- Cache sizes
- Operating system scheduler

## Troubleshooting

### Build Warnings

Nullable reference warnings (CS8600, CS8618, etc.) are non-critical and can be safely ignored. The application functions correctly despite these warnings.

To suppress warnings, add to `.csproj`:

```xml
<PropertyGroup>
  <Nullable>disable</Nullable>
</PropertyGroup>
```

### Performance Issues

If parallel version is slower:
1. Increase text size (parallel overhead may dominate)
2. Check thread count (too many threads can hurt performance)
3. Monitor CPU usage (ensure cores are actually being used)
4. Verify Release build (`-c Release` flag)

## Contributing

Potential improvements:
- Add more algorithms (Rabin-Karp, Z-algorithm)
- Implement SIMD optimizations
- Add GPU acceleration
- Support for Unicode/UTF-8
- File-based input
- Export results to CSV/JSON

## License

This is a course project for educational purposes.

## References

- Knuth, D. E.; Morris, J. H.; Pratt, V. R. (1977). "Fast pattern matching in strings"
- Boyer, R. S.; Moore, J. S. (1977). "A fast string searching algorithm"
- Aho, A. V.; Corasick, M. J. (1975). "Efficient string matching: an aid to bibliographic search"

## Author

Course Project - Parallel Programming (2025)
