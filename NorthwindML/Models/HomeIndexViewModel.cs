using System.Collections.Generic;
using Packt.Shared;

namespace NorthwindML.Models
{
    public class HomeIndexViewModel
    {
        public IEnumerable<Category> Categories { get; set; }

        public bool GermanyDataSetExists { get; set; }

        public bool UKDataSetExists { get; set; }

        public bool USADataSetExists { get; set; }

        public long Milliseconds { get; set; }        
    }
}