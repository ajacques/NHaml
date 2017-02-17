namespace System.Web.NHaml.Parser.Exceptions
{
    public class HamlMalformedVariableException : Exception
    {
        public HamlMalformedVariableException(string variable, int lineNo)
            : base(string.Format("Malformed variable on line {0} : {1}", lineNo, variable))
        { }
    }
}
