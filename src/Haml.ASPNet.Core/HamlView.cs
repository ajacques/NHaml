using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Web.NHaml;
using System.Web.NHaml.TemplateResolution;
using System.Web.NHaml.Walkers;
using System.Web.NHaml.Parser;
using System.Web.NHaml.IO;
using System.IO;
using System.Web.NHaml.TemplateBase;
using System.Collections.Generic;
using NHaml.Walkers;
using Microsoft.Extensions.Primitives;

namespace NHaml
{
    public class HamlView : IActionResult
    {
        private Object viewModel;

        public HamlView(Object viewModel)
        {
            this.viewModel = viewModel;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            ITemplateContentProvider provider = new FileTemplateContentProvider();
            HamlTreeParser parser = new HamlTreeParser(new HamlFileLexer());
            ViewSource source = new FileViewSource(new System.IO.FileInfo("test.haml"));
            HamlDocument document = parser.ParseViewSource(source);
            LinqDocumentWalker newWalker = new LinqDocumentWalker(viewModel.GetType());
            newWalker.Walk(document);
            Delegate result = newWalker.Compile();
            TextWriter writer = new StreamWriter(context.HttpContext.Response.Body);
            //context.HttpContext.Response.Headers.Add("Content-Type", new StringValues("text/html"));
            result.DynamicInvoke(new object[] { writer, viewModel });
            /*IDocumentWalker walker = new HamlDocumentWalker(new TestClassBuilder());
            string output = walker.Walk(document, "System.String", typeof(Object), new List<string>() { "System" });
            TemplateFactoryFactory factory = new TemplateFactoryFactory(provider, parser, walker, null, null, null);
            TemplateEngine engine = new TemplateEngine(cache, factory);
            engine.GetCompiledTemplate(source, typeof(TypedTemplate<String>)).CreateTemplate().Render(new StreamWriter(context.HttpContext.Response.Body));
            byte[] body = Encoding.UTF8.GetBytes("Hello world");
            context.HttpContext.Response.Body.Write(body, 0, body.Length);*/
            return writer.FlushAsync();
        }
    }
}