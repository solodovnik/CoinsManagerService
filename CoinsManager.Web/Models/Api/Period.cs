using System;
using System.Collections.Generic;

#nullable disable

namespace CoinsManager.Web.Models
{
    public partial class Period
    {
        public Period()
        {
            Coins = new HashSet<Coin>();
        }

        public int Id { get; set; }
        public string Period1 { get; set; }
        public int Country { get; set; }

        public virtual Country CountryNavigation { get; set; }
        public virtual ICollection<Coin> Coins { get; set; }
    }
}
