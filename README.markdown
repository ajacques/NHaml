NHaml
==============
NHaml (pronounced enamel) is a pure .NET implementation of the popular Rails
Haml view engine. From the Haml website:

"Haml is a markup language that‘s used to cleanly and simply describe the XHTML
of any web document, without the use of inline code. Haml functions as a
replacement for inline page templating systems such as PHP, ERB, and ASP.
However, Haml avoids the need for explicitly coding XHTML into the template,
because it is actually an abstract description of the XHTML, with some code
to generate dynamic content."

In other words, NHaml is an external DSL for XML. It’s primary qualities are it’s
simplicity, terseness, performance and that it outputs nicely formatted XML.
Additionally, the NHaml view engine provides support for Rails style layouts and
partials and ships with an ASP.NET MVC view engine.

What is this fork?
================
This is a partial-rewrite of the original engine to support ASP.NET Core and the .net Framework Core/Standard editions using Roslyn. .net Core doesn't support the same code generation system that the full .net Framework used, so I'm replacing everything with Roslyn compiled assemblies. The new system should be pretty fast since everything gets compiled down to static TextWriter.Write calls for most of the HAML code with calls to pre-compiled IL code generated from any inline C# code in the HAML template.

Example:
HAML:
```
!!!
%html{ lang: 'en' }
  %head
    %title Hello world
    %meta{ charset: 'utf-8' }
    %meta{ content: 'width=device-width, initial-scale=1.0, maximum-scale=1.0', name: 'viewport' }
  %body{ data: model.GetType.ToString.ToUpperInvariant }
    .page-wrap
      = DateTime.Now.ToString("yyyy-mm-dd")
      %h1= new Random().Next().ToString()
      %p= model.ToString()
      .content-pane.container
      - if true
        %div Is True
    .modal-backdrop.in
```

Gets compiled to:

```
sealed class __haml_UserCode_CompilationTarget
{
   public __haml_UserCode_CompilationTarget(System.String _modelType)
   {
      model = _modelType;
   }

   System.String model;
   private string _91ff3507()
   {
      return DateTime.Now.ToString(\"yyyy\");
   }

   private string _779c068e()
   {
      return DateTime.Now.ToString(\"yyyy-mm-dd\");
   }

   private string _79112ab0()
   {
      return new Random().Next().ToString();
   }

   private string _ea04f18()
   {
      return model.ToString();
   }

   private bool _e6180ba9()
   {
      return true;
   }

   private bool _4112ef9a()
   {
      return 1 > 0;
   }

   private bool _9aec0d90()
   {
      return false;
   }

   public void render(System.IO.TextWriter textWriter)
   {
      {
         textWriter.Write(\"<!DOCTYPE html><html lang=\\\"en\\\"><head><title>Hello world</title><meta charset=\\\"utf-8\\\"/><meta content=\\\"width=device-width, initial-scale=1.0, maximum-scale=1.0\\\" name=\\\"viewport\\\"/></head><body><div class=\\\"page-wrap \");
         textWriter.Write(_91ff3507());
         textWriter.Write(\"\\\">\");
         textWriter.Write(_779c068e());
         textWriter.Write(\"<h1>\");
         textWriter.Write(_79112ab0());
         textWriter.Write(\"</h1><p>\");
         textWriter.Write(_ea04f18());
         textWriter.Write(\"</p><div class=\\\"container content-pane\\\"/>\");
         if (_e6180ba9())
         {
            if (_4112ef9a())
            {
               textWriter.Write(\"<div>really true</div>\");
            }

            textWriter.Write(\"<div>Is True</div>\");
         }

         textWriter.Write(\"<div>wat</div>\");
         if (_9aec0d90())
         {

         textWriter.Write(\"<div>Is False</div>\");
         }

         textWriter.Write(\"</div><div class=\\\"in modal-backdrop\\\"/></body></html>\");
      }
   }
}
```

License
================
Copyright (c) 2010 Andrew Peters, Steve Wagner, Simon Cropp, Zsolt Sz. Sztupák
Copyright (c) 2012 Andrew Peters, Steve Wagner, Simon Cropp, Zsolt Sz. Sztupák, Russell Allen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

Contributors
============
- Andrew Peters [http://andrewpeters.net]
- Steve Wagner (lanwin) [http://www.lanwin.de]
- Simon Cropp
- Zsolt Sz. Sztupák (sztupy) [http://www.sztupy.hu]
- Russell Allen (russpall) [http://www.russellallen.info]
