using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Web.NHaml.TemplateResolution;
using System.Web.NHaml.Parser;
using System.Web.NHaml.IO;
using System.IO;
using NHaml.Walkers;
using System.Diagnostics;

namespace NHaml
{
    public class HamlView : IActionResult
    {
        private Object viewModel;
        private static Delegate renderer;

        public HamlView(Object viewModel)
        {
            this.viewModel = viewModel;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            if (renderer == null)
            {
                HamlTreeParser parser = new HamlTreeParser(new HamlFileLexer());
                ViewSource source = new FileViewSource(new System.IO.FileInfo("test.haml"));
                HamlDocument document = parser.ParseViewSource(source);
                LinqDocumentWalker newWalker = new LinqDocumentWalker(viewModel.GetType());
                newWalker.Walk(document);
                renderer = newWalker.Compile();
            }
            TextWriter writer = new StreamWriter(context.HttpContext.Response.Body);
            //context.HttpContext.Response.Headers.Add("Content-Type", new StringValues("text/html"));
            Stopwatch timer = new Stopwatch();
            timer.Start();
            renderer.DynamicInvoke(new object[] { writer, viewModel });
            timer.Stop();
            Console.WriteLine("X-Runtime-us: {0}", timer.ElapsedTicks / 10);
            return writer.FlushAsync();
        }
    }
}