﻿namespace System.Web.NHaml.Walkers.Exceptions
{
    public class HamlUnknownNodeTypeException : Exception
    {
        public HamlUnknownNodeTypeException(Type nodeType, int lineNo)
            : this(nodeType, lineNo, null)
        { }

        private HamlUnknownNodeTypeException(Type nodeType, int lineNo, Exception ex)
            : base(string.Format("Unknown node type '{0}' on line {1}", nodeType.FullName, lineNo), ex)
        { }
    }
}
