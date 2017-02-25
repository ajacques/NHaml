using System.Web.NHaml.IO;

namespace System.Web.NHaml.Parser.Rules
{
    public class HamlNodeEval : HamlNode
    {
        public HamlNodeEval(HamlLine line)
            : base(line) { }

        public HamlNodeEval(int lineNumber, string content)
            : base(lineNumber, content) { }

        protected override bool IsContentGeneratingTag
        {
            get { return true; }
        }
    }
}
