namespace System.Web.NHaml
{
    public class PartialNotFoundException : Exception
    {
        public PartialNotFoundException(string fileName)
            : base("Unable to find partial : " + fileName)
        { }
    }
}
