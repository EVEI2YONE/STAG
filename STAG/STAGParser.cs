using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace STAG
{
    public interface ISTAGParser
    {
        public Node? Parse(string input);
        public string Evaluate();
    }

    public class STAGParser : ISTAGParser
    {
        private List<string> Tokens;
        private string Subject;
        private string Verb;
        private string? Complement;
        private Node Root;

        private string NextToken()
        {
            var token =  Tokens.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(token))
                Tokens.Remove(token);
            else
                ExpectedToken("ANY", "NULL");
            return token; 
        }
        private string ConsumeTokensUntil(params string[] expectedTokens)
        {
            List<string> tokens = new List<string>();
            string token;
            while (expectedTokens.All(t => !PeekToken().Contains(t)))
            {
                tokens.Add(NextToken());
            }
            return string.Join(" ", tokens);
        }
        private string? PeekToken() => Tokens.FirstOrDefault();
        private void PushToken(string token) => Tokens.Insert(0, token);
        private void PushTokens(params string[] tokens)
        {
            foreach (var token in tokens.Reverse())
                Tokens.Insert(0, token);
        }
        private string? GetNthToken(string[] input, int position) => input.Skip(position).FirstOrDefault();
        private void ExpectedToken(string token, string actual) => throw new ArgumentException($"Expected token '{token}', but encountered '{actual}'");
        private void ExpectingToken(string expectedToken)
        {
            string token = NextToken();
            if (token != expectedToken)
                ExpectedToken(expectedToken, token);
        }
        private string? OptionalConsumeToken(string expectedToken)
        {
            var token = NextToken();
            string? result = null;
            if (token == expectedToken)
            {
                //verify ; then consume
            }
            else
            {
                result = token;
                token = NextToken();
            }
            return result;
        }
        private (string? Preposition, string Subject) GetPrepSub(string? input = null)
        {
            if (input != null)
            {
                var split = input.Split(';');
                //reverse insertion
                Tokens.Insert(0, split.Last());
                Tokens.Insert(0, ";");
                if(!string.IsNullOrWhiteSpace(split.FirstOrDefault()))
                    Tokens.Insert(0, split.First());
            }
            var preposition = OptionalConsumeToken(";");
            var prepSub = NextToken();
            return (preposition, prepSub);
        }
        private bool IsModifier(string input)
        {
            if(!string.IsNullOrEmpty(input) && input.Length == 3)
                return input.First() == '[' && input.Last() == ']';
            return false;
        }
        private ModifierType GetModifier(string token) => token.Skip(1).First().ToString().ToLower() switch
        {
            "s" => ModifierType.S,
            "v" => ModifierType.V,
            "c" => ModifierType.C,
            "o" => ModifierType.O,
            "^" => ModifierType.A,
            _ => throw new ArgumentException($"Expected token to be of [value]: S, V, C, O, ^. Instead, it was {token}")
        };
        private void CleanInput(string input)
        {
            input = input
                .Replace("|", " | ")
                .Replace("\\", " \\ ")
                .Replace(";", " ; ");

            Tokens = input.Split(null).Select(word => word.Trim())
            .Where(word => !string.IsNullOrEmpty(word))
            .ToList();
        }
        private void AssignRootModifier(Node childNode)
        {
            var child = Root.Children.First(n => n.Modifier == childNode.Modifier);
            child.Children.Add(childNode);
        }

        //Subject|Verb
        //Subject|Verb\Complement
        private void ParseSVC()
        {
            Subject = ConsumeTokensUntil("|");

            ExpectingToken("|");
            
            Verb = ConsumeTokensUntil("\\", "[");
            
            string token = PeekToken();
            if(token == "\\")
            {
                NextToken();
                Complement = ConsumeTokensUntil("[");
            }

            Root = new Node()
            {
                Modifier = ModifierType.R,
                Type = NodeType.Root,
                Value = null,
                Children = new List<Node>()
            };

            Root.Children.Add(new Node() { Modifier = ModifierType.S, Type = NodeType.Bar, Value = Subject });
            Root.Children.Add(new Node() { Modifier = ModifierType.V, Type = NodeType.Bar, Value = Verb });
            if(Complement != null)
                Root.Children.Add(new Node() { Modifier = ModifierType.C, Type = NodeType.Bar, Value = Complement });
        }

        //[X] token
        //[X] prep:
        private void ParseModifiers(Node parentNode)
        {
            if (parentNode == null)
                return;

            var token = NextToken();
            if (!IsModifier(token))
                ExpectedToken("[X]", token);

            var modifier = GetModifier(token);
            token = NextToken();
            Node node;
            if (token == "prep:")
            {
                node = ParsePreposition(modifier);
            }
            else
            {
                node = new Node();
                node.Type = NodeType.Modifier;
                node.Modifier = modifier;
                node.Value = token;
            }

            if(parentNode.Type == NodeType.Root)
                AssignRootModifier(node);
            else if(node != null)
                parentNode.Children.Add(node);
            
            token = PeekToken();
            if(IsModifier(token))
                ParseModifiers(parentNode);
        }

        // { prep;subject [X] ... }
        // { ;subject [X] ... }
        // prep;subject
        // ;subject
        private Node? ParsePreposition(ModifierType modifier)
        {
            var token = PeekToken();

            var node = new Node();
            node.Modifier = modifier;
            node.Type = NodeType.Preposition;
            
            string? preposition = null;
            string prepSub;
            if (token == "{")
            {
                ExpectingToken("{");

                (preposition, prepSub) = GetPrepSub();
                node.Value = $"{preposition};{prepSub}";

                ParseModifiers(node);

                ExpectingToken("}");
            }
            else
            {
                (preposition, prepSub) = GetPrepSub();
                node.Value = $"{preposition};{prepSub}";
            }

            return node;
        }

        public Node? Parse(string input)
        {
            if (input == null)
                return null;
            CleanInput(input);
            
            ParseSVC();
            ParseModifiers(Root);
            return Root;
        }

        private List<Node> GetNodesByModifier(Node node, ModifierType modifier) => node.Children.Where(n => n.Modifier == modifier).ToList();

        public string Evaluate()
        {
            if (Root == null)
                return string.Empty;

            var subjectNode = GetNodesByModifier(Root, ModifierType.S).First();
            var verbNode = GetNodesByModifier(Root, ModifierType.V).First();
            var complementNode = GetNodesByModifier(Root, ModifierType.C).FirstOrDefault();

            List<string> output = new List<string>();
            Evaluate_Recursive(Root, subjectNode, output);
            Evaluate_Recursive(Root, verbNode, output);
            if (complementNode != null)
                Evaluate_Recursive(Root, complementNode, output);
            return string.Join(" ", output);
        }

        private Node BuildUpToPreposition(Node parent, Node node, List<string> output)
        {
            var prepositionChild = node.Children.FirstOrDefault(n => n.Type == NodeType.Preposition);
            foreach (var child in node.Children)
            {
                if (child.Type == NodeType.Preposition)
                    break;
                output.Add(child.Value);
            }
            return prepositionChild;
        }

        private void Evaluate_Recursive(Node parent, Node node, List<string> output)
        {
            if (node == null)
                return;

            Node? prepositionChild = null;
            if(node.Type == NodeType.Bar)
            {
                if (node.Modifier == ModifierType.V)
                {
                    output.Add(node.Value);
                    prepositionChild = BuildUpToPreposition(parent, node, output);
                }
                else
                {
                    prepositionChild = BuildUpToPreposition(parent, node, output);
                    output.Add(node.Value);
                }

            }
            else if (node.Type == NodeType.Preposition)
            {
                var (preposition, prepSub) = GetPrepSub(node.Value);
                if(!string.IsNullOrWhiteSpace(preposition))
                    output.Add(preposition);

                prepositionChild = BuildUpToPreposition(parent, node, output);
                output.Add(prepSub);
            }
            else //Modifier
            {
                throw new NotImplementedException();
            }
            Evaluate_Recursive(node, prepositionChild, output);
        }
    }
}
