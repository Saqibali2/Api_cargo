using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class PackageDto
    {
        public int shipment_id { get; set; }
        public string name { get; set; }
        public double? weight { get; set; }
        public double? length { get; set; }
        public double? width { get; set; }
        public double? height { get; set; }
        public int? quantity { get; set; }
        public string color { get; set; }
        public string tagNo { get; set; }
    }
}