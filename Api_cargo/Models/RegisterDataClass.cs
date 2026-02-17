using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class RegisterDataClass
    {
        public string Email { get; set; } // <- User Email
        public string Password { get; set; } // <- User Password
        public string Role { get; set; } // <- User Role - {Customer, Driver, Admin}

        // Common fields for all users
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CNIC { get; set; }
        public string ContactNo { get; set; }
        public string StreetNo { get; set; }
        public string City { get; set; }
        public string PhotoLink { get; set; }
        public string departure { get; set; }
        public string arrival { get; set; }
        // Driver DTOs
        public string LicenseNo { get; set; }
        public VehicleDto Vehicle { get; set; } // <- Vehicle Details for Driver
        public DocumentsDto Documents { get; set; } // <- Documents for Driver
        public List<Checkpoints> routeData { get; set; }
    }
}