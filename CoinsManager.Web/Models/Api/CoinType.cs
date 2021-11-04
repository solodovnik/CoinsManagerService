using System;
using System.Collections.Generic;

#nullable disable

namespace CoinsManager.Web.Models
{
    public partial class CoinType
    {
        public CoinType()
        {
            Coins = new HashSet<Coin>();
        }

        public int Id { get; set; }
        public string Type { get; set; }

        public virtual ICollection<Coin> Coins { get; set; }
    }
}
