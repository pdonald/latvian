using System;
using System.Linq;

using Latvian.Tokenization;

using NUnit.Framework;

namespace Latvian.Tests.Tokenization
{
    [TestFixture]
    public class LatvianTokenizerTests
    {
        ITokenizer tokenizer = new LatvianTokenizer { IncludeWhitespace = true };

        private void Test(string text, params string[] expected)
        {    
            string[] tokens = tokenizer.Tokenize(text).Select(t => t.Text).ToArray();
            CollectionAssert.AreEqual(expected, tokens);
        }

        [Test] public void cirvis() { Test("cirvis", new[] { "cirvis" }); }
        [Test] public void kaķis() { Test("kaķis", new[] { "kaķis" }); }
        [Test] public void cirvis_āmurs() { Test("cirvis āmurs", new[] { "cirvis", " ", "āmurs" }); }
        [Test] public void cirvis_āmurs_galds() { Test("cirvis āmurs galds", new[] { "cirvis", " ", "āmurs", " ", "galds" }); }
        [Test] public void cirvis_punkts_āmurs() { Test("cirvis. āmurs", new[] { "cirvis", ".", " ", "āmurs" }); }
        [Test] public void āmurs_atstarpe_jautzīme() { Test("āmurs ?", new[] { "āmurs", " ", "?" }); }
        [Test] public void iniciāļi() { Test("atnāca A. Bērziņš.", new[] { "atnāca", " ", "A.", " ", "Bērziņš", "." }); }
        [Test] public void datums() { Test("sapulce 2012. gada 3. aprīlī", new[] { "sapulce", " ", "2012.", " ", "gada", " ", "3.", " ", "aprīlī" }); }
        [Test] public void pieturzīmes() { Test("Kas tas ?!?!?", new[] { "Kas", " ", "tas", " ", "?!?!?" }); }
        [Test] public void pieturzīmes2() { Test("Kas tas⁈", new[] { "Kas", " ", "tas", "⁈" }); }
        [Test] public void pieturzīmes3() { Test("un tad-, kaut kas notika", new[] { "un", " ", "tad", "-", ",", " ", "kaut", " ", "kas", " ", "notika" }); }
        [Test] public void pieturzīmes4() { Test("tiešā runa.\" teksts", new[] { "tiešā", " ", "runa", ".", "\"", " ", "teksts" }); }
        [Test] public void tilde() { Test("~vārds", new[] { "~", "vārds" }); }
        [Test] public void ekomercija() { Test("e-komercija", new[] { "e-komercija" }); }
        [Test] public void apostrofs() { Test("Raug', Saule grimdama aicina mani", new[] { "Raug", "'", ",", " ", "Saule", " ", "grimdama", " ", "aicina", " ", "mani" }); }
        [Test] public void apostrofs_desa() { Test("'desa'", new[] { "'", "desa", "'" }); }
        [Test] public void epasts() { Test("uz e-pastu vards.uzvards@domains.lv", new[] { "uz", " ", "e-pastu", " ", "vards.uzvards@domains.lv" }); }
        [Test] public void teikums_ar_punktu() { Test("es eju.", "es", " ", "eju", "."); }
        [Test] public void saites () { Test("no http://www.faili.lv/fails.php?id=215 šejienes", "no", " ", "http://www.faili.lv/fails.php?id=215", " ", "šejienes"); }
        [Test] public void saites_ftp() { Test("Ftp adrese ftp://www.faili.lv/fails.php?id=215&actions=download", "Ftp", " ", "adrese", " ", "ftp://www.faili.lv/fails.php?id=215&actions=download"); }
        [Test] public void saites_www() { Test("mājaslapa www.skaistas-vietas.lv", "mājaslapa", " ", "www.skaistas-vietas.lv"); }
        [Test] public void daļskaitļi() { Test("Nobalsoja 1/2 no balstiesīgajiem", "Nobalsoja", " ", "1/2", " ", "no", " ", "balstiesīgajiem"); }
        [Test] public void tūkstoši() { Test("Šobrīd tiešsaitē ir 12'456 lietotāji", "Šobrīd", " ", "tiešsaitē", " ", "ir", " ", "12'456", " ", "lietotāji"); }
        [Test] public void ipadrese() { Test("Servera IP adrese ir 132.168.2.102", "Servera", " ", "IP", " ", "adrese", " ", "ir", " ", "132.168.2.102"); }
        [Test] public void nauda1() { Test("Ls 5.- gadā", "Ls", " ", "5.-", " ", "gadā"); }
        [Test] public void nauda2() { Test("pusgadā Ls 3,-", "pusgadā", " ", "Ls", " ", "3,-"); }
        [Test] public void nauda3() { Test("Cena Ls 0.40. Nākamais", "Cena", " ", "Ls", " ", "0.40", ".", " ", "Nākamais"); }
        [Test] public void nauda4() { Test("Ls 50.000,-", "Ls", " ", "50.000,-"); }
        [Test] public void noiepirkšanās() { Test("no iepirkšanās", "no", " ", "iepirkšanās"); }
        [Test] public void džilindžers() { Test("Dž. Dz. Džilindžers.", "Dž.", " ", "Dz.", " ",  "Džilindžers", "."); }
        [Test] public void atstarpes() { Test("a t s t a r p e s", "a t s t a r p e s"); }
        [Test] public void atstarpes2() { Test("te ir a t s t a r p e s", "te", " ", "ir", " ", "a t s t a r p e s"); }
        [Test] public void nonLV() { Test("старик с топором", "старик", " ", "с", " ", "топором"); }
        [Test] public void spaces() { Test(" es eju ", " ", "es", " ", "eju", " "); }
        [Test] public void vecadruka() { Test("şabeedrişka", "şabeedrişka"); }
        [Test] public void ampersand() { Test("tom&jerry", "tom", "&", "jerry"); }
        [Test] public void ampersand2() { Test("cirvis&", "cirvis", "&"); }
        [Test] public void A_upitis() { Test("A. Upītis", "A.", " ", "Upītis"); }
        [Test] public void A_upitis2() { Test("A.Upītis", "A.", "Upītis"); }
        [Test] public void klase() { Test("11.c", "11.", "c"); }
        [Test] public void klase2() { Test("11.a", "11.", "a"); }
        [Test] public void time1() { Test("00:00", "00:00"); }
        [Test] public void time2() { Test("23:59", "23:59"); }
        [Test] public void time3() { Test("23:59:59", "23:59:59");  }
        [Test] public void time4() { Test("24:00", "24", ":", "00"); }
        [Test] public void time5() { Test("13:60", "13", ":", "60"); }
        [Test] public void time6() { Test("25:00", "25", ":", "00"); }
        [Test] public void date1() { Test("2009-12-14", "2009-12-14"); }
        [Test] public void date2() { Test("2009.12.14", "2009.12.14"); }
        [Test] public void date3() { Test("9999.99.99", "9999.99.99"); }
        [Test] public void date4() { Test("0000-00-00", "0000-00-00"); }
    }
}
