namespace STAG.Tests
{
    public class Tests
    {
        private ISTAGParser parser;

        [SetUp]
        public void Setup()
        {
            parser = new STAGParser();
        }

        [TestCase("The king is here",
            "king|is [s] The [v] here")]
        [TestCase("The king is generous", 
            "king|is\\generous [s] The")]
        [TestCase("The king is a friend",
            "king|is\\friend [s] The [c] a")]
        [TestCase("The stapler is on the desk", 
            "stapler|is [s] The [v] prep: { on;desk [o] the }")]
        [TestCase("My cat is a lover of tuna",
            "cat|is\\lover [s] My [c] a [c] prep: of;tuna")]
        [TestCase("The music is extremely loud",
            "music|is\\loud [s] The [c] extremely")]
        [TestCase("The graduation party is today",
            "party|is [s] The [s] graduation [v] today")]
        [TestCase("That child is being unreasonable",
            "child|is being\\unreasonable [s] That")]
        [TestCase("Her Favorite picture from the vacation in Florida is a snapshot of a playful dolphin",
            "picture|is\\snapshot [s] Her [s] Favorite [s] prep: { from;vacation [o] the [o] prep: in;Florida } [c] a [c] prep: { of;dolphin [o] a [o] playful }")]
        [TestCase("In her wallet are photos from the vacation in Florida",
            "photos|are\r\n[s] prep: { from;vacation [o] the [o] prep: in;Florida\r\n}\r\n[v] prep: { in;wallet [o] her\r\n}")]
        [TestCase("The king is happy",
            "king|is\\happy [s] The")]
        [TestCase("The king is in a good mood",
            "king|is\\^ [s] The [^] prep: { in;mood [o] a [o] good }")]
        [TestCase("The king is on the throne",
            "king|is [s] The [v] prep: { on;throne [o] the }")]
        [TestCase("The king on the throne is Henery VIII",
            "king|is\\Henery VIII [s] The [s] prep: { on;throne [o] the }")]
        [TestCase("The king is out of breath",
            "king|is\\^ [s] The [^] prep: out of;breath")]
        [TestCase("The king seems unhappy",
            "king|seems\\unhappy [s] The")]
        [TestCase("The king seems out of sorts",
            "king|seems\\^ [s] The [^] prep: out of;sorts")]
        [TestCase("The king became a tyrant",
            "king|became\\tyrant [s] The [c] a")]
        public void BasicParsing_Evaluation(string expectedOutput, string input)
        {
            var tree = parser.Parse(input);
            Assert.True(tree != null);

            var output = parser.Evaluate();
            Assert.That(output, Is.EqualTo(expectedOutput));
        }
    }
}