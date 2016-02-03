﻿using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Settings
{
    public partial class SortOptionModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.Configuration.Settings.Catalog.SortOptions.Name")]
        [AllowHtml]
        public string Name { get; set; }

        public int SortOptionTypeId { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Catalog.SortOptions.IsActive")]
        [AllowHtml]
        public bool IsActive { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Settings.Catalog.SortOptions.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}