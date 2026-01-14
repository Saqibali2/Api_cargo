using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class VehicleDto
    {
        public string RegNo { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string WeightCapacity { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
    }
}