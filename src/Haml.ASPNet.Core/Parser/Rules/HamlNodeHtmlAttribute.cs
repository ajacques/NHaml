using System.Web.NHaml.Crosscutting;
using System.Web.NHaml.Parser.Exceptions;

namespace System.Web.NHaml.Parser.Rules
{
    public class HamlNodeHtmlAttribute : HamlNode
    {
        private string _name = string.Empty;
        private char _quoteChar = '\'';

        public static HamlNodeHtmlAttribute FromNameValuePair(int lineNumber, string nameValuePair)
        {
            int index = 0;
            string name = ParseName(ref index, nameValuePair, lineNumber);

            return new HamlNodeHtmlAttribute(lineNumber, nameValuePair);
        }

        private HamlNodeHtmlAttribute(int sourceFileLineNo, string nameValuePair)
            : base(sourceFileLineNo, nameValuePair)
        {
            int index = 0;
            _name = ParseName(ref index, nameValuePair, sourceFileLineNo);
            AddChild(new HamlNodeTextContainer(sourceFileLineNo, ParseValue(index, nameValuePair)));
        }
        public HamlNodeHtmlAttribute(int lineNumber, string name, string value)
            : base(lineNumber, string.Format("{0}: {1}", name, value))
        {
            _name = name;

            AddChild(new HamlNodeTextContainer(SourceFileLineNum, GetValue(value)));
        }

        private static string ParseValue(int index, string content)
        {
            if (index >= content.Length) return null;

            return content.Substring(index + 1).Trim();
        }

        protected override bool IsContentGeneratingTag
        {
            get { return true; }
        }

        public string Name
        {
            get { return _name; }
        }

        public char QuoteChar
        {
            get { return _quoteChar; }
        }

        private static string ParseName(ref int index, string content, int lineNumber)
        {
            string result = HtmlStringHelper.ExtractTokenFromTagString(content, ref index, new[] { ':', '\0' });
            if (string.IsNullOrEmpty(result))
                throw new HamlMalformedTagException("Malformed HTML attribute \"" + content + "\"", lineNumber);

            return result.TrimEnd(':').TrimStart(' ');
        }

        private string GetValue(string value)
        {
            if (IsQuoted(value))
                return RemoveQuotes(value);
            else if (IsVariable(value))
                return value;
            else
                return "#{" + value + "}";
        }

        private bool IsVariable(string value)
        {
            return value.StartsWith("#{") && value.EndsWith("}");
        }

        private bool IsQuoted(string input)
        {
          return ((input[0] == '\'' && input[input.Length - 1] == '\'')
                || (input[0] == '"' && input[input.Length - 1] == '"'));
        }

        private string RemoveQuotes(string input)
        {
            if (input.Length < 2 || IsQuoted(input) == false)
                return input;

            _quoteChar = input[0];
            return input.Substring(1, input.Length - 2);
        }
    }
}
