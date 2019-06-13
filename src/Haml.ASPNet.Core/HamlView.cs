using Haml.Compiling;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web.NHaml.IO;
using System.Web.NHaml.Parser;
using System.Web.NHaml.TemplateResolution;
using System.Reflection;

namespace NHaml
{
    public class HamlView : IActionResult
    {
        private object viewModel;
        private string _viewFile;
        private static Type renderer;
        private static MethodInfo renderMethod;

        public HamlView(string viewFile, object viewModel)
        {
            this._viewFile = viewFile;
            this.viewModel = viewModel;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            if (renderer == null)
            {
                LinqDocumentWalker newWalker = new LinqDocumentWalker(viewModel.GetType());
                newWalker.Render(new TemplateRenderContext("Views/Shared/Layout.haml", _viewFile));
                renderer = newWalker.Compile();
                renderMethod = renderer.GetMethod("render");
            }
            TextWriter writer = new StreamWriter(context.HttpContext.Response.Body);
            //context.HttpContext.Response.Headers.Add("Content-Type", new StringValues("text/html"));
            Stopwatch timer = new Stopwatch();
            timer.Start();
            object instance = Activator.CreateInstance(renderer, viewModel);
            renderMethod.Invoke(instance, new object[] { writer });
            timer.Stop();
            Debug.WriteLine("X-Runtime-us: {0}", timer.ElapsedTicks / 10);
            return writer.FlushAsync();
        }
    }
}
