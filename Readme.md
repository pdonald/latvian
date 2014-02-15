# Latvian

Latvian is a C# library for natural language processing (NLP) tasks in Latvian.

* [Tokenization](#tokenization)
* [Morphology](#morphology)
* [Part-of-speech tagging](#part-of-speech-tagging)

## Tokenization

Tokenization is the process of breaking a stream of text up into words, phrases, symbols, or other meaningful elements called tokens.

This library provides a fast, streaming, customizable, regular expression based tokenizer using a Deterministic Finite Automaton (DFA).

### Features

* **Fast** - around 2 million non-whitespace tokens per second or over 10 MiB/s on a modern CPU
* **Streaming** - processes one character at a time, no need to load the whole text in memory
* **Customizable** - easily extend or modify token types

### Usage

Create a new instance of `Latvian.Tokenization.LatvianTokenizer` and call the `Tokenize` method with the text as `string`, `TextReader` or `IEnumerable<char>`.

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

You can tokenize sentences as well. If you already have a sequence of tokens, you can break them up into sentences.

`Token` has starting and ending positions as well as starting and ending line numbers and the positions on the lines. Token types may have additional useful properties. Like the text as a `System.DateTime` for date tokens or the parsed value for numeric tokens.

### Customization

`LatvianTokenizer` is a collection of regular expression patterns. You can remove the ones you don't like and add new ones.

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

EmotionToken[] tokens = tokenizer.Tokenize("Šodien esmu :) bet vakar biju :(")
                                 .OfType<EmotionToken>().ToArray();

Assert.AreEqual(":)", tokens[0].Text);
Assert.AreEqual(":(", tokens[1].Text);

Assert.IsTrue(tokens[0].IsHappy);
Assert.IsFalse(tokens[1].IsHappy);
```

### Helper classes

This library uses its own very simplistic regular expression language to define token patterns. Regular expressions are then parsed, converted to a Non-deterministic Finite Automaton (NFA) and transformed into a Deterministic Finite Automaton (DFA). This DFA implementation supports char ranges to prevent the automaton from blowing up in size. You can also attach custom values to states and visualize the automaton with a single line of code. There are also useful classes for reading characters from a stream that support going back and then later releasing no longer used parts of the stream.

### Learn more

* [Tokenization README](Latvian/Tokenization)
* [Automata and regular expressions README](Latvian/Tokenization/Automata)
* [Unit tests](Latvian.Tests/Tokenization)
* [LU MII token types (GPLv3)](https://github.com/pdonald/latvian-lumii#tokenization)

## Morphology

This is still in the works. You can use the wrapper around LU MII morphology in the meantime. See the [Latvian.LuMii](#latvianlumii) module.

## Part-of-speech tagging

Part-of-speech (POS) tagging is the process of marking up a word in a text as corresponding to a particular part of speech, based on both its definition, as well as its context. 

This library provides an implementation of the averaged perceptron supervised classification machine learning algorithm. It's very very simple, insanely fast, has small memory footprint and very good accuracy.

A configured tagger that produces good results for the Latvian language is included.

### Features

* **Fast tagging** - tagging speed is around 422k tokens per second on a modern Intel Core i7 mobile CPU
* **Fast training** - 10-fold cross validation can be performed in less than 25 seconds, no kidding!
* **Parallel tagging**
* **Streaming** - *todo*
* **Small model** - 1.71 MiB model trained on 94k tokens

### Perceptron tagger

Load sentences as a list of `Latvian.Tagging.Token` which has `CorrectTag` and `PossibleTags` properties. Configure the perceptron tagger. You will need to experiment with feature templates and iteration count. Averaging the weights (which makes it the averaged perceptron) always produces better results. Sometimes tagging tokens a sentence in reverse order produces better results, like it does for the Latvian language.

```csharp
public class MyTagger : PerceptronTagger
{
    public MyTagger()
    {
        Average = true;
        Reverse = false;
        Iterations = 8;

        FeatureTemplates.Add(new Current(new TokenText()));
        FeatureTemplates.Add(new Next(new TokenText()));
        FeatureTemplates.Add(new Prev(new TokenText()));
        FeatureTemplates.Add(new Prev(new Pos()));
        FeatureTemplates.Add(new Prev(new Msd()));
                
        FeatureTemplates.Add(new Multiple(new Prev(1, new Msd()), new Prev(2, new Msd())));
        FeatureTemplates.Add(new Multiple(new Prev(1, new TokenText()), new Next(1, new TokenText())));
        FeatureTemplates.Add(new Multiple(new Prev(1, new TokenText()), new Prev(2, new TokenText())));
        FeatureTemplates.Add(new Multiple(new Next(1, new TokenText()), new Next(2, new TokenText())));

        for (int i = 1; i <= 4; i++)
            FeatureTemplates.Add(new Current(new TokenSuffix(i)));

        FeatureTemplates.Add(new PrevNoun());
    }
}
```

### Results

The perceptron tagger has been evaluated on two different corpora. Accuracy is the number of tokens with the correctly predicted full tag divided by the total number of tokens. Because the algorithm is so fast, 10-fold cross validation is the preferred way to compare results.

The accuracy depends on the features used, is influenced by the iteration count, averaging always improved the accuracy and tagging in reverse gave the accuracy a huge boost. Random seed also influences it but not by much.

The results could most likely be improved by experimenting with different sets of features.

#### LU MII

The [Institute of Mathematics and Computer Science](http://lumii.lv) at the [University of Latvia](http://lu.lv) (LU MII) has open sourced labeled data suitable for training the tagger.

Currently, the best results achieved with a corpus of around 6k sentences and 94k tokens are:

* **93.42%** - 10-fold cross validation (99% confidence interval: 93.13..93.72%)
* **93.53%** - split data

In comparison, their best model has a **93.8%** accuracy (split data). No 10-fold cross validation accuracy is available probably because it's too slow.

#### Tilde

SIA [Tilde](http://tilde.lv) is a company specializing in localization and language technologies.

The tagger has been evaluated on a manually annotated corpus of about 125k tokens from Tilde. The corpus is proprietary and is not available to the public.

Currently, the best results achieved are:

* **95.18%** - 10-fold cross validation (99% confidence interval: 94.91..95.45%)

In comparison, their tagger based on the maximum entropy model has a **91.51%** accuracy (split data). Note, however, that it was trained and tested on the different version of the same corpus (comparable perceptron result: 94.83%).

Tilde also has annotated corpora for Lithuanian and Estonian. Using the same features for these languages produces the following results:

* Lithuanian - **98.52%** (80k tokens)
* Estonian - **99.76%** (445k tokens)

### Learn more

* [Source (no README yet)](Latvian/Tagging)

## Modules

If you want to use part-of-speech tagging in your application, you will need a trained model. To train a model you need an annotated corpus. This library does not include pretrained models or corpora to train the models with.

While the library can be used as-is, you really need data for it to have practical applications (apart from the tokenizer). Unfortunately, the data is available under a much stricter license or not available at all.

The following projects are separate modules and are not dependencies. They use the same namespaces as this library (e.g. `Latvian.Tokenization` and `Latvian.Tagging`) but are located in different assemblies (e.g. `Latvian.LuMii.dll` and `Latvian.Tilde.dll`).

### [Latvian.LuMii](https://github.com/pdonald/latvian-lumii)

The [Institute of Mathematics and Computer Science](http://lumii.lv) at the [University of Latvia](http://lu.lv) (LU MII) has their own [morphological analyzer](https://github.com/PeterisP/morphology) and [part-of-speech tagger](https://github.com/PeterisP/LVTagger) written in Java using the Stanford NLP tools. 

`Latvian.LuMii` contains

* Abbreviation token type for the tokenizer based on their word list
* Collocation token type for the tokenizer based on their word list
* Wrapper around their tokenizer
* Wrapper around their morphological analyzer
* Pretrained model for part-of-speech tagging
* Data for training a part-of-speech model

Their project is licensed under GPLv3 and including their data in this project would require making this library also GPLv3. For this reason, it's available as a separate module and is licensed under GPLv3.

### Latvian.Tilde

This module uses proprietary data and tools from [Tilde](http://tilde.lv). It's not available to the public.

## Build

The library is written in C# 5.0 for .NET 4.5 using Microsoft Visual Studio 2013. It uses NuGet to manage dependencies. Unit tests are written using the NUnit framework.

Open the solution in Visual Studio and build it. It will restore packages using NuGet (currently just NUnit for unit tests) and should work out of the box.

There is no reason it should not build on mono. It's planned to be supported in the future.

## Acknowledgements

These articles, companies and projects helped the development of this library.

* [A good POS tagger in about 200 lines of Python](http://honnibal.wordpress.com/2013/09/11/a-good-part-of-speechpos-tagger-in-about-200-lines-of-python/)
* [NFA construction from regular expressions](http://matt.might.net/articles/implementation-of-nfas-and-regular-expressions-in-java/)
* [Tilde](http://tilde.lv) for providing data to evaluate the perceptron tagger
* [LU MII](http://lumii.lv) for open sourcing their data

## License

Apache 2.0