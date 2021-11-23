﻿using System.Net;
using System.Text;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Html;
using Nop.Services.Localization;

namespace Nop.Services.Customers
{
    /// <summary>
    /// Customer attributes formatter
    /// </summary>
    public partial class CustomerAttributeFormatter : ICustomerAttributeFormatter
    {
        #region Fields

        protected ICustomerAttributeParser CustomerAttributeParser { get; }
        protected ICustomerAttributeService CustomerAttributeService { get; }
        protected ILocalizationService LocalizationService { get; }
        protected INopHtmlHelper NopHtmlHelper { get; }
        protected IWorkContext WorkContext { get; }

        #endregion

        #region Ctor

        public CustomerAttributeFormatter(ICustomerAttributeParser customerAttributeParser,
            ICustomerAttributeService customerAttributeService,
            ILocalizationService localizationService,
            INopHtmlHelper nopHtmlHelper,
            IWorkContext workContext)
        {
            CustomerAttributeParser = customerAttributeParser;
            CustomerAttributeService = customerAttributeService;
            LocalizationService = localizationService;
            NopHtmlHelper = nopHtmlHelper;
            WorkContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Formats attributes
        /// </summary>
        /// <param name="attributesXml">Attributes in XML format</param>
        /// <param name="separator">Separator</param>
        /// <param name="htmlEncode">A value indicating whether to encode (HTML) values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the attributes
        /// </returns>
        public virtual async Task<string> FormatAttributesAsync(string attributesXml, string separator = "<br />", bool htmlEncode = true)
        {
            var result = new StringBuilder();
            var currentLanguage = await WorkContext.GetWorkingLanguageAsync();
            var attributes = await CustomerAttributeParser.ParseCustomerAttributesAsync(attributesXml);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                var valuesStr = CustomerAttributeParser.ParseValues(attributesXml, attribute.Id);
                for (var j = 0; j < valuesStr.Count; j++)
                {
                    var valueStr = valuesStr[j];
                    var formattedAttribute = string.Empty;
                    if (!attribute.ShouldHaveValues())
                    {
                        //no values
                        if (attribute.AttributeControlType == AttributeControlType.MultilineTextbox)
                        {
                            //multiline textbox
                            var attributeName = await LocalizationService.GetLocalizedAsync(attribute, a => a.Name, currentLanguage.Id);
                            //encode (if required)
                            if (htmlEncode)
                                attributeName = WebUtility.HtmlEncode(attributeName);
                            formattedAttribute = $"{attributeName}: {NopHtmlHelper.FormatText(valueStr, false, true, false, false, false, false)}";
                            //we never encode multiline textbox input
                        }
                        else if (attribute.AttributeControlType == AttributeControlType.FileUpload)
                        {
                            //file upload
                            //not supported for customer attributes
                        }
                        else
                        {
                            //other attributes (textbox, datepicker)
                            formattedAttribute = $"{await LocalizationService.GetLocalizedAsync(attribute, a => a.Name, currentLanguage.Id)}: {valueStr}";
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }
                    else
                    {
                        if (int.TryParse(valueStr, out var attributeValueId))
                        {
                            var attributeValue = await CustomerAttributeService.GetCustomerAttributeValueByIdAsync(attributeValueId);
                            if (attributeValue != null)
                            {
                                formattedAttribute = $"{await LocalizationService.GetLocalizedAsync(attribute, a => a.Name, currentLanguage.Id)}: {await LocalizationService.GetLocalizedAsync(attributeValue, a => a.Name, currentLanguage.Id)}";
                            }
                            //encode (if required)
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }

                    if (string.IsNullOrEmpty(formattedAttribute)) 
                        continue;

                    if (i != 0 || j != 0)
                        result.Append(separator);

                    result.Append(formattedAttribute);
                }
            }

            return result.ToString();
        }

        #endregion
    }
}