using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
