using System;
using System.Collections.Generic;

#nullable disable

namespace CoinsManagerService.Models
{
    public partial class CoinEmbeddings
    {
 
        public int CoinId { get; set; }
        public string ObverseEmbedding { get; set; }
        public string ReverseEmbedding { get; set; }

        public virtual Coin Coin { get; set; }
    }
}
