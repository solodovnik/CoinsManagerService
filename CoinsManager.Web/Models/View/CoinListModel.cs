using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoinsManager.Web.Models.View
{
    public class CoinListModel
    {
        public IEnumerable<Coin> Coins { get; set; }
    }
}
