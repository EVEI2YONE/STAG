using System.Diagnostics;
using System.Text;

namespace STAG
{
    public enum NodeType
    {
        Root,
        Bar,
        Modifier,
        Preposition,
    }

    public enum ModifierType
    {
        R, //root
        S, //subject
        V, //verb
        C, //complement
        O, //object
        A  // ^
    }

    public class Node
    {
        public ModifierType Modifier { get; set; }
        public NodeType Type { get; set; }
        public String Value { get; set; }
        public List<Node> Children { get; set; } = new List<Node>();
        public override string ToString() => $"[{Modifier.ToString()}]{Value}:{Type.ToString()}";
    }
}