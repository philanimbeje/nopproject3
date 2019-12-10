﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Nop.Web.Framework.Extensions;

namespace Nop.Web.Framework.TagHelpers.Admin
{
    /// <summary>
    /// "nop-card tag helper
    /// </summary>
    [HtmlTargetElement("nop-card", Attributes = NAME_ATTRIBUTE_NAME)]
    public class NopcardTagHelper : TagHelper
    {
        private const string NAME_ATTRIBUTE_NAME = "asp-name";
        private const string TITLE_ATTRIBUTE_NAME = "asp-title";
        private const string HIDE_BLOCK_ATTRIBUTE_NAME_ATTRIBUTE_NAME = "asp-hide-block-attribute-name";
        private const string IS_HIDE_ATTRIBUTE_NAME = "asp-hide";
        private const string IS_ADVANCED_ATTRIBUTE_NAME = "asp-advanced";
        private const string CARD_ICON_ATTRIBUTE_NAME = "asp-icon";

        private readonly IHtmlHelper _htmlHelper;

        /// <summary>
        /// Title of the panel
        /// </summary>
        [HtmlAttributeName(TITLE_ATTRIBUTE_NAME)]
        public string Title { get; set; }

        /// <summary>
        /// Name of the panel
        /// </summary>
        [HtmlAttributeName(NAME_ATTRIBUTE_NAME)]
        public string Name { get; set; }

        /// <summary>
        /// Name of the hide attribute of the panel
        /// </summary>
        [HtmlAttributeName(HIDE_BLOCK_ATTRIBUTE_NAME_ATTRIBUTE_NAME)]
        public string HideBlockAttributeName { get; set; }

        /// <summary>
        /// Indicates whether a block is hidden or not
        /// </summary>
        [HtmlAttributeName(IS_HIDE_ATTRIBUTE_NAME)]
        public bool IsHide { get; set; }

        /// <summary>
        /// Indicates whether a panel is advanced or not
        /// </summary>
        [HtmlAttributeName(IS_ADVANCED_ATTRIBUTE_NAME)]
        public bool IsAdvanced { get; set; }

        /// <summary>
        /// Panel icon
        /// </summary>
        [HtmlAttributeName(CARD_ICON_ATTRIBUTE_NAME)]
        public string CardIconIsAdvanced { get; set; }

        /// <summary>
        /// ViewContext
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="htmlHelper">HTML helper</param>
        public NopcardTagHelper(IHtmlHelper htmlHelper)
        {
            _htmlHelper = htmlHelper;
        }

        /// <summary>
        /// Process
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="output">Output</param>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            //contextualize IHtmlHelper
            var viewContextAware = _htmlHelper as IViewContextAware;
            viewContextAware?.Contextualize(ViewContext);

            //create card
            var card = new TagBuilder("div")
            {
                Attributes =
                {
                    new KeyValuePair<string, string>("id", Name),
                    new KeyValuePair<string, string>("data-card-name", Name),
                }
            };
            card.AddCssClass("card card-default");
            if (context.AllAttributes.ContainsName(IS_ADVANCED_ATTRIBUTE_NAME) && context.AllAttributes[IS_ADVANCED_ATTRIBUTE_NAME].Value.Equals(true))
            {
                card.AddCssClass("advanced-setting");
            }

            //create card heading and append title and icon to it
            var cardHeading = new TagBuilder("div");
            cardHeading.AddCssClass("card-header with-border clearfix");
            //cardHeading.Attributes.Add("data-hideAttribute", context.AllAttributes[HIDE_BLOCK_ATTRIBUTE_NAME_ATTRIBUTE_NAME].Value.ToString());

            if (context.AllAttributes[IS_HIDE_ATTRIBUTE_NAME].Value.Equals(false))
            {
                //cardHeading.AddCssClass("opened");
            }

            if (context.AllAttributes.ContainsName(CARD_ICON_ATTRIBUTE_NAME))
            {
                var cardIcon = new TagBuilder("i");
                //cardIcon.AddCssClass("card-icon");
                cardIcon.AddCssClass(context.AllAttributes[CARD_ICON_ATTRIBUTE_NAME].Value.ToString());
                var iconContainer = new TagBuilder("div");
                iconContainer.AddCssClass("card-title");
                iconContainer.InnerHtml.AppendHtml(cardIcon);
                iconContainer.InnerHtml.AppendHtml($"{context.AllAttributes[TITLE_ATTRIBUTE_NAME].Value}");
                cardHeading.InnerHtml.AppendHtml(iconContainer);
            }

            //cardHeading.InnerHtml.AppendHtml($"<span>{context.AllAttributes[TITLE_ATTRIBUTE_NAME].Value}</span>");
            var collapseIcon = new TagBuilder("i");
            collapseIcon.AddCssClass("fa");
            //collapseIcon.AddCssClass("toggle-icon");
            collapseIcon.AddCssClass(context.AllAttributes[IS_HIDE_ATTRIBUTE_NAME].Value.Equals(true) ? "fa-plus" : "fa-minus");

            var cardtoolContainer = new TagBuilder("div");
            cardtoolContainer.AddCssClass("card-tools pull-right");
            var cardbtnContainer = new TagBuilder("button");
            
            cardbtnContainer.AddCssClass("btn btn-tool");
            cardbtnContainer.MergeAttribute("type","button");
            cardbtnContainer.MergeAttribute("data-card-widget", "collapse");
            cardbtnContainer.InnerHtml.AppendHtml(collapseIcon);

            cardtoolContainer.InnerHtml.AppendHtml(cardbtnContainer);


            cardHeading.InnerHtml.AppendHtml(cardtoolContainer);

            //create inner card container to toggle on click and add data to it
            var cardContainer = new TagBuilder("div");
            cardContainer.AddCssClass("card-body");
            if (context.AllAttributes[IS_HIDE_ATTRIBUTE_NAME].Value.Equals(true))
            {
                cardContainer.AddCssClass("collapsed");
            }

            cardContainer.InnerHtml.AppendHtml(output.GetChildContentAsync().Result.GetContent());

            //add heading and container to card
            card.InnerHtml.AppendHtml(cardHeading);
            card.InnerHtml.AppendHtml(cardContainer);

            output.Content.AppendHtml(card.RenderHtmlContent());
        }
    }
}