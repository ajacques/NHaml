using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Haml.Compiling
{
    interface ITemplateRenderer
    {
        void Write(string content);
        void Write(char content);
        void Write(string format, params object[] formats);
        void ConditionalBegin();
        void ConditionalElseBegin();
        void ConditionalElseEnd();
        void ConditionalEnd(string methodName);
    }
}
