using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.NHaml.IO;
using System.Web.NHaml.Parser;
using System.Web.NHaml.TemplateResolution;

namespace Haml.Compiling
{
    public class TemplateRenderContext
    {
        public TemplateRenderContext(string layoutPath, string mainTemplate)
        {
            HamlTreeParser parser = new HamlTreeParser(new HamlFileLexer());

            LayoutRoot = parser.ParseViewSource(new FileViewSource(new FileInfo(layoutPath)));
            MainTemplate = parser.ParseViewSource(new FileViewSource(new FileInfo(mainTemplate)));
        }

        public HamlDocument LayoutRoot
        {
            get;
            private set;
        }

        public HamlDocument MainTemplate
        {
            get;
            private set;
        }
    }
}
