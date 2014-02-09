using System;
using System.Linq;

using Latvian.Tokenization.Automata;

using DFA = Latvian.Tokenization.Automata.DeterministicFiniteAutomaton;
using NFA = Latvian.Tokenization.Automata.NondeterministicFiniteAutomaton;

using NUnit.Framework;

namespace Latvian.Tests.Tokenization.Automata
{
    [TestFixture]
    public class NfaTests
    {
        [Test]
        public void Empty()
        {
            NFA nfa = NFA.Empty();
            DFA dfa = nfa.ToDfa();

            Assert.IsTrue(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("aaaa"));
            Assert.IsFalse(dfa.IsMatch("bbb"));
        }

        [Test]
        public void Char()
        {
            NFA nfa = NFA.Char('a');
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsTrue(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("bbb"));
        }

        [Test]
        public void Concat()
        {
            NFA nfa = NFA.Concat(NFA.Char('a'), NFA.Char('b'));
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsTrue(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));
        }

        [Test]
        public void Sequence_Empty()
        {
            NFA nfa = NFA.Sequence();
            DFA dfa = nfa.ToDfa();

            Assert.IsTrue(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
        }

        [Test]
        public void Sequence_1_a()
        {
            NFA nfa = NFA.Sequence(NFA.Char('a'));
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsTrue(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
        }

        [Test]
        public void Sequence_2_ab()
        {
            NFA nfa = NFA.Sequence(NFA.Char('a'), NFA.Char('b'));
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsTrue(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));
        }

        [Test]
        public void Sequence_3_abc()
        {
            NFA nfa = NFA.Sequence(NFA.Char('a'), NFA.Char('b'), NFA.Char('c'));
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));
            Assert.IsTrue(dfa.IsMatch("abc"));
            Assert.IsFalse(dfa.IsMatch("abcc"));
            Assert.IsFalse(dfa.IsMatch("abcd"));
        }

        [Test]
        public void Or()
        {
            NFA nfa = NFA.Or(NFA.Char('a'), NFA.Char('b'));
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsTrue(dfa.IsMatch("a"));
            Assert.IsTrue(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));
        }

        [Test]
        public void Or_3_abc()
        {
            NFA nfa = NFA.Or(NFA.Char('a'), NFA.Char('b'), NFA.Char('c'));
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsTrue(dfa.IsMatch("a"));
            Assert.IsTrue(dfa.IsMatch("b"));
            Assert.IsTrue(dfa.IsMatch("c"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("cc"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));
            Assert.IsFalse(dfa.IsMatch("ccc"));
        }

        [Test]
        public void Star()
        {
            NFA nfa = NFA.Star(NFA.Sequence(NFA.Char('a'), NFA.Char('b')));
            DFA dfa = nfa.ToDfa();

            Assert.IsTrue(dfa.IsMatch(""));

            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("cc"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));

            Assert.IsTrue(dfa.IsMatch("ab"));
            Assert.IsTrue(dfa.IsMatch("abab"));
            Assert.IsTrue(dfa.IsMatch("ababab"));
        }

        [Test]
        public void Plus()
        {
            NFA nfa = NFA.Plus(NFA.Sequence(NFA.Char('a'), NFA.Char('b')));
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("cc"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));

            Assert.IsTrue(dfa.IsMatch("ab"));
            Assert.IsTrue(dfa.IsMatch("abab"));
            Assert.IsTrue(dfa.IsMatch("ababab"));
        }

        [Test]
        public void Repeat()
        {
            NFA nfa = NFA.Repeat(NFA.Sequence(NFA.Char('a'), NFA.Char('b')), 2);
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));

            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("bb"));
            Assert.IsFalse(dfa.IsMatch("ba"));
            Assert.IsFalse(dfa.IsMatch("cc"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("aba"));
            Assert.IsFalse(dfa.IsMatch("abb"));

            Assert.IsFalse(dfa.IsMatch("ab"));
            Assert.IsTrue(dfa.IsMatch("abab"));
            Assert.IsFalse(dfa.IsMatch("ababab"));
        }

        [Test]
        public void Misc()
        {
            NFA nfa = NFA.Sequence(NFA.Star(NFA.Or(NFA.Char('a'), NFA.Char('b'))), NFA.Char('a'), NFA.Char('b'), NFA.Char('b')); // (a|b)*abb
            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("ab"));

            Assert.IsTrue(dfa.IsMatch("abb"));
            Assert.IsTrue(dfa.IsMatch("aabb"));
            Assert.IsTrue(dfa.IsMatch("babb"));
            Assert.IsTrue(dfa.IsMatch("ababb"));
            Assert.IsTrue(dfa.IsMatch("aaabb"));
            Assert.IsTrue(dfa.IsMatch("bbabb"));
            Assert.IsTrue(dfa.IsMatch("abababb"));
        }

        [Test]
        public void Date()
        {
            CharRange digit = new CharRange('0', '9');
            CharRange dash = new CharRange('-');

            // NFA nfa = NFA.Sequence(NFA.Repeat(NFA.Char(digit), 4), NFA.Char(dash), NFA.Repeat(NFA.Char(digit), 2), NFA.Char(dash), NFA.Repeat(NFA.Char(digit), 2));
            
            NFA nfa = NFA.Sequence(
                NFA.Char(digit), NFA.Char(digit), NFA.Char(digit), NFA.Char(digit),
                NFA.Char(dash),
                NFA.Char(digit), NFA.Char(digit),
                NFA.Char(dash),
                NFA.Char(digit), NFA.Char(digit)
            );

            DFA dfa = nfa.ToDfa();

            Assert.IsTrue(dfa.IsMatch("2014-01-03"));
            Assert.IsTrue(dfa.IsMatch("0000-00-00"));
        }

        [Test]
        public void Time()
        {
            CharRange d09 = new CharRange('0', '9');
            CharRange d05 = new CharRange('0', '5');
            CharRange d03 = new CharRange('0', '3');
            CharRange colon = new CharRange(':');

            NFA a = NFA.Sequence(NFA.Or(NFA.Char('0'), NFA.Char('1')), NFA.Char(d09), NFA.Char(colon), NFA.Char(d05), NFA.Char(d09)); // [01][0-9]:[0-5][0-9]
            NFA b = NFA.Sequence(NFA.Char('2'), NFA.Char(d03), NFA.Char(colon), NFA.Char(d05), NFA.Char(d09)); // [2][0-3]:[0-5][0-9]
            NFA c = NFA.Sequence(NFA.Char('2'), NFA.Char('4'), NFA.Char(colon), NFA.Char('0'), NFA.Char('0')); // [2][4]:[0][0]

            NFA nfa = NFA.Or(a, b, c);
            DFA dfa = nfa.ToDfa();

            Assert.IsTrue(dfa.IsMatch("00:00"));
            Assert.IsTrue(dfa.IsMatch("01:50"));
            Assert.IsTrue(dfa.IsMatch("23:15"));
            Assert.IsTrue(dfa.IsMatch("23:59"));
            Assert.IsTrue(dfa.IsMatch("24:00"));

            Assert.IsFalse(dfa.IsMatch("14:78"));
            Assert.IsFalse(dfa.IsMatch("24:59"));
        }

        [Test]
        public void DateTime()
        {
            CharRange digit = new CharRange('0', '9');
            CharRange dash = new CharRange('-');
            CharRange colon = new CharRange(':');
            CharRange seperator = new CharRange(' ');

            // NFA date = NFA.Sequence(NFA.Repeat(NFA.Char(digit), 4), NFA.Char(dash), NFA.Repeat(NFA.Char(digit), 2), NFA.Char(dash), NFA.Repeat(NFA.Char(digit), 2));
            // NFA time = NFA.Sequence(NFA.Repeat(NFA.Char(digit), 2), NFA.Char(colon), NFA.Repeat(NFA.Char(digit), 2));

            NFA date = NFA.Sequence(
                NFA.Char(digit), NFA.Char(digit), NFA.Char(digit), NFA.Char(digit),
                NFA.Char(dash),
                NFA.Char(digit), NFA.Char(digit),
                NFA.Char(dash),
                NFA.Char(digit), NFA.Char(digit)
            );

            NFA time = NFA.Sequence(
                NFA.Char(digit), NFA.Char(digit), 
                NFA.Char(colon),
                NFA.Char(digit), NFA.Char(digit)
            );

            NFA datetime = NFA.Sequence(date, NFA.Char(seperator), time);

            NFA nfa = NFA.Or(date, time, datetime);
            DFA dfa = nfa.ToDfa();

            Assert.IsTrue(dfa.IsMatch("00:00"));
            Assert.IsTrue(dfa.IsMatch("23:15"));
            Assert.IsTrue(dfa.IsMatch("23:59"));

            Assert.IsTrue(dfa.IsMatch("2014-01-03"));
            Assert.IsTrue(dfa.IsMatch("0000-00-00"));

            Assert.IsTrue(dfa.IsMatch("2014-01-03 23:15"));
        }

        [Test]
        public void NfaToDfaExample() // http://web.cecs.pdx.edu/~harry/compilers/slides/LexicalPart3.pdf
        {
            NFA nfa = new NFA();

            NFA.State[] states = new NFA.State[11];
            for (int i = 0; i < states.Length; i++)
                states[i] = new NFA.State();

            states[0].AddEmptyTransition(states[1]);
            states[0].AddEmptyTransition(states[7]);
            states[1].AddEmptyTransition(states[2]);
            states[1].AddEmptyTransition(states[4]);
            states[2].AddTransition('a', states[3]);
            states[3].AddEmptyTransition(states[6]);
            states[4].AddTransition('b', states[5]);
            states[5].AddEmptyTransition(states[6]);
            states[6].AddEmptyTransition(states[1]);
            states[6].AddEmptyTransition(states[7]);
            states[7].AddTransition('a', states[8]);
            states[8].AddTransition('b', states[9]);
            states[9].AddTransition('b', states[10]);
            states[10].IsFinal = true;

            nfa.Start = states[0];
            nfa.Exit = states[10];

            DFA dfa = nfa.ToDfa();

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("ab"));

            Assert.IsTrue(dfa.IsMatch("abb"));
            Assert.IsTrue(dfa.IsMatch("aabb"));
            Assert.IsTrue(dfa.IsMatch("babb"));
            Assert.IsTrue(dfa.IsMatch("ababb"));
            Assert.IsTrue(dfa.IsMatch("aaabb"));
            Assert.IsTrue(dfa.IsMatch("bbabb"));
            Assert.IsTrue(dfa.IsMatch("abababb"));
        }
    }
}
