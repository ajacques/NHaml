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
This is a partial-rewrite of the original engine to support ASP.NET Core and the .net Framework Core/Standard editions using Roslyn. .net Core doesn't support the same code generation system that the full .net Framework used, so I'm replacing everything with dynamic Linq Expressions and Roslyn compiled assemblies. The new system should be pretty fast since everything gets compiled down to static TextWriter.Write calls for most of the HAML code with calls to pre-compiled IL code generated from any inline C# code in the HAML template.

Example:
```
.Lambda #Lambda1<System.Action`2[System.IO.TextWriter,System.String]>(
    System.IO.TextWriter $var1,
    System.String $var2) {
    .Block() {
        .Call $var1.Write("<!DOCTYPE html><html lang="en"><head><title>Hello world</title><meta charset="utf-8"/><meta content="width=device-width, initial-scale=1.0, maximum-scale=1.0" name="viewport"/></head><body data="")
        ;
        .Call $var1.Write("");
        .Call $var1.Write(.Call (.Call (.Call $var2.GetType()).ToString()).ToUpperInvariant());
        .Call $var1.Write(""><div>");
        .Call $var1.Write(.Call __haml_UserCode_CompilationTarget.binder());
        .Call $var1.Write("<div></div></div><div></div></body></html>")
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

Issues
================
Our IssueTracker is located here: [http://code.google.com/p/nhaml/issues/list]

Getting Help
===========
The Google Group NHaml-Users at ( [http://groups.google.com/group/nhaml-users] ) is the best place to go.


Contributors
============
- Andrew Peters [http://andrewpeters.net]
- Steve Wagner (lanwin) [http://www.lanwin.de]
- Simon Cropp
- Zsolt Sz. Sztupák (sztupy) [http://www.sztupy.hu]
- Russell Allen (russpall) [http://www.russellallen.info]
