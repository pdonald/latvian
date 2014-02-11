# Tokenizer

## Quick Start

Create a new instance of `Latvian.Tokenization.LatvianTokenizer` and call the `Tokenize` method with the text to tokenize as a string parameter.

```csharp
using System;

using Latvian.Tokenization;
using Latvian.Tokenization.Tokens;

class Program
{
    static void Main()
    {
        string text = "Sveika, pasaule! Man iet labi. Šodienas datums ir 2014-01-01";

        LatvianTokenizer tokenizer = new LatvianTokenizer();

        foreach (Token token in tokenizer.Tokenize(text))
        {
            Console.WriteLine("Line {0}: Pos {1}: Type: {2} Token: {3}",
                token.Line, token.Position, token.GetType(), token.Text);

            if (token is DateToken)
            {
                DateToken dateToken = token as DateToken;
                Console.WriteLine(dateToken.DateTime.ToString("dd/MM/yyyy"));
            }
        }
    }
}
```

You can also tokenize sentences.

```csharp
using Sentence = IEnumerable<Token>;

IEnumerable<Sentence> sentences = tokenizer.TokenizeSentences(text);

foreach (Sentence sentence in sentences)
{
    foreach (Token token in sentence)
    {
        Console.WriteLine(token);
    }

    Console.WriteLine("-- End of sentence --");
}
```

If you already have tokenized text, you can break it into sentences:

```csharp
Token[] tokens = tokenizer.Tokenize(text).ToArray();
Sentence[] sentences = tokenizer.BreakSentences(tokens).ToArray();
```

## Usage

`Token.Text` contains the token value.

```csharp
Token token = new LatvianTokenizer().Tokenize("vārds").First();
Assert.AreEqual("vārds", token.Text);
Assert.AreEqual("vārds", token.ToString());
```

If you are only interested in token values:

```csharp
string[] tokens = new LatvianTokenizer().Tokenize("viens divi").Select(t => t.Text).ToArray();
Assert.AreEqual("viens", tokens[0]);
Assert.AreEqual("divi", tokens[1]);
```

Two tokens are identical if their values match.

```csharp
var tokens = new LatvianTokenizer().Tokenize("viens viens").Distinct();
Assert.AreEqual(1, tokens.Count());
```

`Token` has starting and ending positions as well as starting and ending line numbers and the positions on the lines. Positions start at 0.

```csharp
Token[] tokens = new LatvianTokenizer().Tokenize("Vārds.").ToArray();

Assert.AreEqual(0, tokens[0].Position);
Assert.AreEqual(5, tokens[0].PositionEnd);
Assert.AreEqual(0, tokens[0].Line);
Assert.AreEqual(0, tokens[0].LineEnd);
Assert.AreEqual(0, tokens[0].LinePosition);
Assert.AreEqual(5, tokens[0].LinePositionEnd);

Assert.AreEqual(".", tokens[1].Text);
Assert.AreEqual(5, tokens[1].Position);
Assert.AreEqual(6, tokens[1].PositionEnd);
Assert.AreEqual(0, tokens[1].Line);
Assert.AreEqual(0, tokens[1].LineEnd);
Assert.AreEqual(5, tokens[1].LinePosition);
Assert.AreEqual(6, tokens[1].LinePositionEnd);
```

Token type can be determined by looking at its class type. Casting to a more specific type will reveal some other useful properties. For example, `DateToken` has a `DateTime` property which returns the parsed date as `System.DateTime`. Token types are in the `Latvian.Tokenization.Tokens` namespace.

```csharp
Token token = new LatvianTokenizer().Tokenize("2014-01-01").First();
Assert.IsTrue(token is DateToken);
Assert.AreEqual(2014, ((DateToken)token).DateTime.Year);

Token token = new LatvianTokenizer().Tokenize("123").First();
Assert.IsTrue(token is OrdinalNumberToken);
Assert.AreEqual(123 * 2, ((OrdinalNumberToken)token).Value * 2);
```

By default, whitespace and control characters are not returned. You can use the `IncludeWhitespace` and `IncludeControlChars` boolean properties to change this behavior.

```csharp
Token[] tokens = new LatvianTokenizer().Tokenize("123 456").ToArray();
Assert.AreEqual("123", tokens[0].Text);
Assert.AreEqual("456", tokens[1].Text);

LatvianTokenizer tokenizer = new LatvianTokenizer() { IncludeWhitespace = true };
Token[] tokens = tokenizer.Tokenize("123 456").ToArray();
Assert.AreEqual("123", tokens[0].Text);
Assert.AreEqual(" ", tokens[1].Text);
Assert.AreEqual("456", tokens[2].Text);
```

There are extension methods for tokenizing a `Stream` and `TextReader` so that you don't have load the whole text in memory:

```csharp
using (FileStream stream = new FileStream("file.txt", FileMode.Open, FileAccess.Read))) {
    Token[] tokens = new LatvianTokenizer().Tokenize(stream).ToArray();
}

using (StreamReader reader = new StreamReader("file.txt")) {
    Token[] tokens = new LatvianTokenizer().Tokenize(reader).ToArray();
}
```

## Customization

`LatvianTokenizer` is a collection of regular expression patterns which are represented by token type classes. See the list of default token types below. You can remove the ones you don't like and add new ones.

```csharp
Token[] tokens = new LatvianTokenizer().Tokenize("A.Bērziņš").ToArray();
Assert.AreEqual("A.", tokens[0].Text);
Assert.AreEqual("Bērziņš", tokens[1].Text);

// vs

LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
tokenizer.Remove<InitialsToken>();
tokenizer.Compile(); // optional

Token[] tokens = tokenizer.Tokenize("A.Bērziņš").ToArray();
Assert.AreEqual("A", tokens[0].Text);
Assert.AreEqual(".", tokens[1].Text);
Assert.AreEqual("Bērziņš", tokens[2].Text);
```

If you use your own configuration often, you can create a custom class.

```csharp
public class MyTokenizer : LatvianTokenizer
{
    public MyTokenizer() : base(compile: false)
    {
        Remove<InitialsToken>();
        IncludeWhitespace = true;
    
        Compile();
    }
}

MyTokenizer tokenizer = new MyTokenizer();
```

Patterns use a custom regular expression language (see further below for a short tutorial). The simplest way is to create a new class. It must extend `Token` and implement `IHasPattern`.

```csharp
public class EmotionToken : Token, IHasPattern
{
    public string Pattern
    {
        get { return "[:].[)(DP]"; } // :) :( :D :P
    }

    public bool IsHappy
    {
        get { return Text == ":)" || Text == ":D"; }
    }
}

LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
tokenizer.Add<EmotionToken>();

EmotionToken[] tokens = tokenizer.Tokenize("Šodien esmu :) bet vakar biju :(").OfType<EmotionToken>().ToArray();
Assert.AreEqual(":)", tokens[0].Text);
Assert.AreEqual(":(", tokens[1].Text);
```

The tokenizer finds the longest match. If there are two patterns that overlap and match the same string, the longest match is returned.

```csharp
Token[] tokens = new LatvianTokenizer().Tokenize("2014-01-01 2014-01-01T12:00:00").ToArray();
Assert.AreEqual("2014-01-01", tokens[0].Text);
Assert.AreEqual("2014-01-01T12:00:00", tokens[1].Text);
```

If there are two patterns that match the same string of equal length, the first token type that was added to the tokenizer is used. 

```csharp
LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
tokenizer.Clear(); // removes existing TimeToken that matches 00:00:00
tokenizer.Add<TimeSpanToken>(); // matches 00:00:00
tokenizer.Add<ClockToken>(); // matches 00:00:00

Token token = tokenizer.Tokenize("00:00:00").First();
Assert.IsTrue(token is TimeSpanToken);
```

`LatvianTokenizer` implements `IList<Type>` so you can rearrange the order. There are strongly typed versions of `IList<Type>` methods available and one convenience method `Move<T>()`.

```csharp
// remove and insert at the first position
tokenizer.Remove<ClockToken>();
tokenizer.Insert<ClockToken>(0); 

// it's is the same as using the methods from IList<Type>
tokenizer.Remove(typeof(ClockToken));
tokenizer.Insert(0, typeof(ClockToken));

// here's a convenience method for rearranging the order
tokenizer.Move<ClockToken>(0);

// all of the above result in this
token = tokenizer.Tokenize("00:00:00").First();
Assert.IsTrue(token is ClockToken);
```

### Default token types

Here is the list of default token types along with the regular expression patterns used.

* WordToken: `[a-zA-ZĀ-ž]+`
* WhitespaceToken: `[ \t\r\n]+`
* ... _todo_ ...

_If this list is out of date, see LatvianTokenizer.cs_

### Regular expression language

* `[x]` matches a single character: `[a]` matches only `a`
* `[xX]` matches one of the characters: `[aA]` matches only `a` or `A` but not both
* `[x-x]` matches characters in the range (inclusive): `[0-9]` matches a single digit
* `*` matches zero or more occurences of the expression: `[0-9]*`
* `+` matches one or more occurrences of the expression: `[0-9]+`
* `.` concatinates expressions: `[0-9].[a-z]` matches a digit followed by a letter
* `|` matches one or the other expression: `[0-9]|[a-z]` matches a digit or a letter but not both
* Evaulated left to right, equal priority: `[0].[1]|[2]` is `[0].([1]|[2])` and matches `01` or `02`
* `()` changes order: `([0].[1])|[2]` matches `01` or `2`

All characters must be in square brackets, there are no implicit ranges. There is no implicit concatenation. There is no repetition apart from `*` and `+`.

Examples:

* Multi ranges: `[a-z0-9!@#]`
* No need to escape the dash: `[-02]` or `[02-]` but `[0\-2]` (all match `0` or `-` or `2`)
* Unicode chars: `[\u0000-\uffff]` matches any character
* Two or more: `[a].[a]+` or `[a].[a].[a]*`


## Performance

### Loading

All patterns are parsed, then converted into a non-deterministic finite automaton (NFA) that is then converted to a deterministic finite automaton (DFA). This is referred to as compiling. This takes a while but it may not be significant depending on usage.

By default, when a new instance of `LatvianTokenizer` is created, the default token types and patterns are added and compiled. If you plan on modifying token types, you can pass `false` in the constructor to delay compiling: `new LatvianTokenizer(compile: false)` or `new LatvianTokenizer(false)`. Adding new token types or removing existing token types will destroy the compiled automaton, so it will need to be compiled again. You can then compile the tokenizer by calling `LatvianTokenizer.Compile()`. If this is not done, it will be called automatically when you try to tokenize text for the first time. 

It's also possible to save the current configuration to a file and then load it later.

```csharp
LatvianTokenizer tokenizer = new LatvianTokenizer(compile: false);
tokenizer.Remove<InitialsToken>();
tokenizer.Save("tokenizer.bin"); // compiles automatically

LatvianTokenizer tokenizer2 = new LatvianTokenizer("tokenizer.bin");

LatvianTokenizer tokenizer3 = new LatvianTokenizer("tokenizer.bin", compile: false);
tokenizer3.Compile();
    
LatvianTokenizer tokenizer4 = new LatvianTokenizer(compile: false);
tokenizer4.Load("tokenizer.bin");

LatvianTokenizer tokenizer5 = new LatvianTokenizer(compile: false);
tokenizer5.Load("tokenizer.bin", compile: false);
tokenizer5.Compile();
```

Here are the benchmark results:

| Method                            | Time     |
| ----------------------------------|--------: |
| Compile                           |    25 ms |
| Load                              |    26 ms |
| Compile x100                      |  2543 ms |
| Load x100                         |  2689 ms |

_Release build, NUnit test runner, mobile i7, SSD_

At this time it looks like there is no difference between loading from a file and compiling everything every time. Loading is slower than compiling probably because it's neccessary to use reflection to load types from the file.

The tokenizer data file could be compressed but the space savings are insifignant for the default configuration (6 KiB vs 24 KiB) so it's not implemented.

### Tokenization

| Source               | Size       | Time     |  Rate      | Speed              |
| ---------------------|-----------:|--------: |-----------:|-------------------:|
| string               |     10 MiB |   740 ms | 13.5 MiB/s | 2 279 983 tokens/s |
| file (StreamReader)  |     10 MiB |  1022 ms |  9.8 MiB/s | 1 652 179 tokens/s |

_Release build, NUnit test runner, mobile i7, SSD_

