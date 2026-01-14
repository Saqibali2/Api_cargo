using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class DocumentsDto
    {
        public string CnicLink { get; set; }
        public string LicenseLink { get; set; }
        public string FrontLink { get; set; }
        public string BackLink { get; set; }
    }
}