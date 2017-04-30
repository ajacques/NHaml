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
            Parser = new HamlTreeParser(new HamlFileLexer());

            TemplateDirectory = new DirectoryInfo(mainTemplate).Parent;

            LayoutRoot = Parser.ParseViewSource(new FileViewSource(new FileInfo(layoutPath)));
            MainTemplate = Parser.ParseViewSource(new FileViewSource(new FileInfo(mainTemplate)));
        }

        private HamlTreeParser Parser
        {
            get;
            set;
        }

        public HamlDocument GetTemplate(string relativePath)
        {
            return Parser.ParseViewSource(new FileViewSource(new FileInfo(Path.Combine(TemplateDirectory.FullName, relativePath))));
        }

        public DirectoryInfo TemplateDirectory
        {
            get;
            private set;
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
