# Automata

## Deterministic Finite Automaton (DFA)

`DFA` is a very simple class. It has just one property - the start state. `DFA.State` is a dictionary that maps characters to other states. State can be an accepting state if `DFA.State.IsFinal` is `true`.

```csharp
using DFA = DeterministicFiniteAutomaton;

DFA.State s1 = new DFA.State();
DFA.State s2 = new DFA.State() { IsFinal = "true" };

s1.AddTransition('a', s2);
s1.AddTransition('b', s2);

DFA dfa = new DFA() { Start = s1 };

Assert.IsTrue(dfa.IsMatch("a"));
Assert.IsTrue(dfa.IsMatch("b"));
Assert.IsFalse(dfa.IsMatch(""));
Assert.IsFalse(dfa.IsMatch("c"));
Assert.IsFalse(dfa.IsMatch("ab"));
Assert.IsFalse(dfa.IsMatch("ba"));
```

This implementation supports char ranges.

```csharp
CharRange digit = new CharRange('0', '9');
CharRange az = new CharRange('a', 'z');
CharRange colon = 'c'; // new CharRange('c', 'c');

Assert.IsTrue(az.Contains(c));
Assert.IsFalse(az.Overlaps(digit));

DFA.State s1 = new DFA.State();
DFA.State s2 = new DFA.State() { IsFinal = "true" };

s1.AddTransition(digit, s2);
s1.AddTransition(az, s2);

DFA dfa = new DFA() { Start = s1 };

for (char c = '0'; c <= '9'; c++) Assert.IsTrue(dfa.IsMatch(c.ToString()));
for (char c = 'a'; c <= 'z'; c++) Assert.IsTrue(dfa.IsMatch(c.ToString()));
```

DFA has only one transition state for each input. If you try to add an input that already has a transition state associated with it, an exception will be thrown.

```csharp
s1.AddTransition(digit, s2);
s1.AddTransition(az, s2);
s1.AddTransition(c, s2); // ArgumentOutOfRangeException because of az previously
```

If you know it's not an accident and you want to override it, you can use `DFA.State.SetTransition`.

```csharp
DFA.State s1 = new DFA.State();
DFA.State s2 = new DFA.State() { IsFinal = "true" };
DFA.State s3 = new DFA.State() { IsFinal = "true" };

s1.SetTransition(digit, s2);
s1.SetTransition(az, s2);
s1.SetTransition(c, s3);

// 0..9 => s2
// a..b => s2
// d..z => s2
// c    => s3

Assert.IsTrue(dfa.IsMatch("c"));
```

`DFA.State.Alphabet` contains all inputs that this state recognizes. 

```csharp
Assert.IsTrue(s1.Alphabet.Contains(az));
Assert.IsTrue(s1.Alphabet.Any(r => r.Contains('w')));
```

`DFA.State.Transitions` contains all distinct states that you can get to from this state.

```csharp
Assert.IsTrue(s1.Transitions.Contains(s2));
Assert.IsTrue(s1.Transitions.Contains(s3));
```

Given a `char`, you can determine the next state with `DFA.State[char]`. This will perform an optimized binary search.

```csharp
public static bool IsMatch(DFA.State start, string text)
{
    if (start == null)
        return false;

    State current = start;

    foreach (char c in input)
    {
        current = current[c];

        if (current == null)
            return false;
    }

    return current.IsFinal;
}
```

You can get all states in a `DFA` that are accessible from the start state with `DFA.States`.

You can save and restore the DFA from a stream with `DFA.Save(Stream)` and `DFA.Load(Stream)`.

```csharp
using (Stream stream = new GZipStream(new FileStream("dfa.bin.gz", FileMode.Create, FileAccess.Write), CompressionLevel.Optimal))
{
    dfa.Save(stream);
}

using (Stream stream = new GZipStream(new FileStream("dfa.bin.gz", FileMode.Open, FileAccess.Read), CompressionMode.Decompress))
{
    dfa.Load(stream);
}
```

This DFA is very specific for the tokenizer. Namely, each state has a `Values` property which is an array of integers that should always be in a sorted order. But it would be easy to create a generic version.

```csharp
class State<T> : DFA.State
{
    public State()
    {
        Values = new List<T>();
    }

    public string Name
    {
        get;
        set;
    }

    public new List<T> Values
    {
        get; 
        private set;
    }
}
```

This would let you asign custom data to states:

```csharp
State<string> s1 = new State<string> { Name = "A" };
State<string> s2 = new State<string> { Name = "B" };
State<string> s3 = new State<string> { Name = "C" };
State<string> s4 = new State<string> { Name = "D" };
State<string> s5 = new State<string> { Name = "E", IsFinal = true };
State<string> s6 = new State<string> { Name = "F", IsFinal = true };

s1.AddTransition('h', s2);
s2.AddTransition('a', s3);
s3.AddTransition('h', s4);
s4.AddTransition('a', s5);
s5.AddTransition('h', s4);
s5.Values.Add("laughter");

s2.AddTransition('m', s6);
s6.AddTransition('m', s6);
s6.Values.Add("deep in thought");

public static string Mood(State<string> start, string text)
{
    if (start == null)
        return null;

    State<string> current = start;

    foreach (char c in text)
    {
        current = current[c] as State<string>;

        if (current == null)
            return null;
    }

    return current.Values.FirstOrDefault();
}

Assert.AreEqual("laughter", Mood(s1, "haha"));
Assert.AreEqual("laughter", Mood(s1, "hahahaha"));
Assert.AreEqual("deep in thought", Mood(s1, "hmm"));
Assert.AreEqual("deep in thought", Mood(s1, "hmmmmmm"));
Assert.AreEqual(null, Mood(s1, "123"));
```

### Graphs

If you compile the project in the Debug configuration, you can easily visualize a DFA.

This will create the file contents for Graphiz.

```csharp
string s = dfa.ToGraph().ToString();
```

If you have Graphiz installed in the right directory, all you need to do is:

```csharp
dfa.ToGraph().SaveImage(@"C:\Users\Me\Desktop\dfa.png");
```

It will look like this:

![](http://i.imgur.com/yWGBJ8c.png)

## Non-deterministic Finite Automaton (NFA)

TODO