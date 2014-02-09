using Latvian.Tokenization.Automata;

using DFA = Latvian.Tokenization.Automata.DeterministicFiniteAutomaton;
using NFA = Latvian.Tokenization.Automata.NondeterministicFiniteAutomaton;

using NUnit.Framework;

namespace Latvian.Tests.Tokenization.Automata
{
    [TestFixture]
    public class DfaTests
    {
        [Test]
        public void a()
        {
            DFA.State s1 = new DFA.State();
            DFA.State s2 = new DFA.State() { IsFinal = true };
            
            s1.AddTransition('a', s2);

            DFA dfa = new DFA(s1);

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsTrue(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsFalse(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("ba"));
        }

        [Test]
        public void ab()
        {
            DFA.State s1 = new DFA.State();
            DFA.State s2 = new DFA.State();
            DFA.State s3 = new DFA.State() { IsFinal = true };
            
            s1.AddTransition('a', s2);
            s2.AddTransition('b', s3);

            DFA dfa = new DFA(s1);

            Assert.IsFalse(dfa.IsMatch(""));
            Assert.IsFalse(dfa.IsMatch("a"));
            Assert.IsFalse(dfa.IsMatch("aa"));
            Assert.IsFalse(dfa.IsMatch("aaa"));
            Assert.IsTrue(dfa.IsMatch("ab"));
            Assert.IsFalse(dfa.IsMatch("b"));
            Assert.IsFalse(dfa.IsMatch("ba"));
        }

        /* [Test]
        public void RangeSplit()
        {
            CharRange az = new CharRange('a', 'z');
            CharRange cf = new CharRange('c', 'f');
            CharRange digits = new CharRange('0', '9');

            DFA.State s1 = new DFA.State();
            DFA.State s2 = new DFA.State { IsFinal = true };

            s1.AddTransition(az, s2);
            s1.AddTransition(cf, s2);
            s1.AddTransition(digits, s2);

            DFA dfa = new DFA(s1);
            Assert.IsTrue(!dfa.IsMatch("a"));
            Assert.IsTrue(!dfa.IsMatch("a"));
        } */
    }
}
