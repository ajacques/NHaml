using System.Linq.Expressions;

namespace NHaml.Walkers.IntermediateNodes
{
    /// <summary>
    /// Represents either a fully compiled expression or a partial
    /// compilation 
    /// </summary>
    internal interface IIntermediateNode
    {
        Expression Build();
    }
}
