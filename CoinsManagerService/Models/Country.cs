using System;
using System.Collections.Generic;

#nullable disable

namespace CoinsManagerService.Models
{
    public partial class Country
    {
        public Country()
        {
            Periods = new HashSet<Period>();
        }

        public int Id { get; set; }      
        public string Name { get; set; }
        public int Continent { get; set; }

        public virtual Continent ContinentNavigation { get; set; }
        public virtual ICollection<Period> Periods { get; set; }
    }
}
