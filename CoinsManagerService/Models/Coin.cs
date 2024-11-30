using System;
using System.Collections.Generic;

#nullable disable

namespace CoinsManagerService.Models
{
    public partial class Coin
    {
        public int Id { get; set; }
        public string Nominal { get; set; }
        public string Currency { get; set; }
        public string Year { get; set; }
        public int Type { get; set; }
        public string CommemorativeName { get; set; }
        public int Period { get; set; }
        public string PictPreviewPath { get; set; }
        public string CatalogId { get; set; }

        public virtual Period PeriodNavigation { get; set; }
        public virtual CoinType TypeNavigation { get; set; }
    }
}
