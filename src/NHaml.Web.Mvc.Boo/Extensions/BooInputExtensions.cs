using System;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Boo.Lang;
using NHaml.Web.Mvc.Boo.Helpers;
#if NET4
using ReturnString = System.Web.Mvc.MvcHtmlString;
#else
using ReturnString = String;
#endif

namespace NHaml.Web.Mvc.Boo.Extensions
{
    public static class BooInputExtensions
    {
        public static ReturnString CheckBox(this HtmlHelper htmlHelper, String name, Hash htmlAttributes)
        {
            return htmlHelper.CheckBox(name, HashHelper.ToStringKeyDictinary( htmlAttributes ));
        }

        public static ReturnString CheckBox(this HtmlHelper htmlHelper, String name, Boolean isChecked, Hash htmlAttributes)
        {
            return htmlHelper.CheckBox(name, isChecked, HashHelper.ToStringKeyDictinary( htmlAttributes ));
        }

        public static ReturnString Hidden(this HtmlHelper htmlHelper, String name, Object value, Hash htmlAttributes)
        {
            return htmlHelper.Hidden(name, value, HashHelper.ToStringKeyDictinary( htmlAttributes ));
        }

        public static ReturnString Password(this HtmlHelper htmlHelper, String name, Object value, Hash htmlAttributes)
        {
            return htmlHelper.Password(name, value, HashHelper.ToStringKeyDictinary( htmlAttributes ));
        }

        public static ReturnString RadioButton(this HtmlHelper htmlHelper, String name, Object value, Hash htmlAttributes)
        {
            return htmlHelper.RadioButton(name, value, HashHelper.ToStringKeyDictinary( htmlAttributes ));
        }

        public static ReturnString RadioButton(this HtmlHelper htmlHelper, String name, Object value, Boolean isChecked, Hash htmlAttributes)
        {
            return htmlHelper.RadioButton(name, value, isChecked, HashHelper.ToStringKeyDictinary( htmlAttributes ));
        }

        public static ReturnString TextBox(this HtmlHelper htmlHelper, String name, Object value, Hash htmlAttributes)
        {
            return htmlHelper.TextBox(name, value, HashHelper.ToStringKeyDictinary( htmlAttributes ));
        }
    }
}