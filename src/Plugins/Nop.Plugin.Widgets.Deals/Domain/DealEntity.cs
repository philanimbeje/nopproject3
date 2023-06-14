﻿using Nop.Core;

namespace Nop.Plugin.Widgets.Deals.Domain;

public class DealEntity : BaseEntity
{
    public string Title { get; set; }
    public string LongDescription { get; set; }
    public string ShortDescription { get; set; }
}