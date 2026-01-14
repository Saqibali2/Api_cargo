using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class AuthController : ApiController
    {

        CargoConnectEntities2 db = new CargoConnectEntities2();

        [HttpGet]
        [Route("api/auth/status")]
        public IHttpActionResult GetAuthStatus()
        {
            return Ok("SUCCESS: Auth Connection successful.");
        }
        [HttpGet]
        [Route("api/users")]
        public IHttpActionResult GetUsers(Users user)
        {
            return Ok(db.Users.Select(nr => new
            {
                nr.user_id,
                nr.role_id,
                nr.joindate,
                nr.updated_at,

            }).ToList());
        }
        [HttpPost]
        [Route("api/auth/register")]
        public IHttpActionResult Register(RegisterDataClass request)
        {
            if (request == null ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password) ||
                string.IsNullOrEmpty(request.Role))
                return BadRequest("ERROR: Invalid registration data.");

            if (UserExists(request.Email))
                return BadRequest("ERROR: User already exists.");

            var user = new Users
            {
                email = request.Email,
                password = request.Password,
                role_id = GetRoleID(request.Role),
                joindate = DateTime.Now,
                suspended = false,
                is_active = true,
                last_login = DateTime.Now,
                updated_at = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            if (request.Role == "Driver")
            {
                if (request.Vehicle == null || request.Documents == null)
                    return BadRequest("ERROR: Vehicle Documents or Info not received.");

                var driver = new Driver
                {
                    user_id = user.user_id,
                    first_name = request.FirstName,
                    last_name = request.LastName,
                    CNIC = request.CNIC,
                    contact_no = request.ContactNo,
                    licence_no = request.LicenseNo,
                    city = request.City,
                    street_no = request.StreetNo,
                    profile_image_url = request.PhotoLink,
                    is_available = true
                };

                db.Driver.Add(driver);
                db.SaveChanges();

                var vehicle = new Vehicle
                {
                    vehicle_reg_no = request.Vehicle.RegNo,
                    driver_id = driver.driver_id,
                    model = request.Vehicle.Model,
                    type = request.Vehicle.Type,
                    weight_capacity = Double.Parse(request.Vehicle.WeightCapacity),
                    length = Double.Parse(request.Vehicle.Length),
                    width = Double.Parse(request.Vehicle.Width),
                    height = Double.Parse(request.Vehicle.Height)
                };

                db.Vehicle.Add(vehicle);

                var docs = new DriverDocuments
                {
                    driver_id = driver.driver_id,
                    uploaded_at = DateTime.Now,
                    cnic_link = request.Documents.CnicLink,
                    license_link = request.Documents.LicenseLink,
                    front_link = request.Documents.FrontLink,
                    back_link = request.Documents.BackLink,
                    photo_link = request.PhotoLink
                };

                db.DriverDocuments.Add(docs);
                db.SaveChanges();
            }
            else if (request.Role == "Customer")
            {
                var customer = new Customer
                {
                    user_id = user.user_id,
                    first_name = request.FirstName,
                    last_name = request.LastName,
                    CNIC = request.CNIC,
                    contact_no = request.ContactNo,
                    city = request.City,
                    street_no = request.StreetNo,
                    profile_image_url = request.PhotoLink
                };

                db.Customer.Add(customer);
                db.SaveChanges();
            }
            else
            {
                return BadRequest("ERROR: Invalid role.");
            }

            return Ok(new
            {
                message = "SUCCESS: Registration successful",
                role = request.Role,
                userId = user.user_id
            });
        }
        [HttpPost]
        [Route("api/auth/login")]
        public IHttpActionResult LoginUser([FromBody] Users user)
        {
            if (user == null || string.IsNullOrEmpty(user.email) || string.IsNullOrEmpty(user.password))
                return BadRequest("ERROR: Input data is null or invalid.");

            var existingUser = db.Users
                .FirstOrDefault(u => u.email == user.email && u.password == user.password);

            if (existingUser != null)
                return Ok(new
                {
                    message = "SUCCESS: Login successful.",
                    userId = existingUser.user_id,
                    roleID = existingUser.role_id,
                    roleName = db.Roles
                        .FirstOrDefault(r => r.role_id == existingUser.role_id)?.role_name
                });

            return BadRequest("ERROR: Email or password is incorrect.");
        }

        [HttpPost]
        [Route("api/users/suspend/{userId}")]
        public IHttpActionResult SuspendUser(int userId)
        {
            var user = db.Users.FirstOrDefault(u => u.user_id == userId);

            if (user == null)
                return BadRequest("ERROR: User not found.");

            if ((bool)user.suspended)
                return BadRequest("ERROR: User is already suspended.");

            user.suspended = true;
            user.is_active = false;
            user.updated_at = DateTime.Now;

            db.SaveChanges();

            return Ok(new
            {
                message = "SUCCESS: User suspended successfully.",
                userId = user.user_id
            });
        }
        [HttpPost]
        [Route("api/users/activate/{userId}")]
        public IHttpActionResult ActivateUser(int userId)
        {
            var user = db.Users.FirstOrDefault(u => u.user_id == userId);

            if (user == null)
                return BadRequest("ERROR: User not found.");

            if ((bool)!user.suspended && (bool)user.is_active)
                return BadRequest("ERROR: User is already active.");

            user.suspended = false;
            user.is_active = true;
            user.updated_at = DateTime.Now;

            db.SaveChanges();

            return Ok(new
            {
                message = "SUCCESS: User activated successfully.",
                userId = user.user_id
            });
        }


        public bool UserExists(String email)
        {
            return db.Users.Any(u => u.email == email);
        }
        public int GetRoleID(string role)
        {
            switch (role)
            {
                case "Driver": return 1;
                case "Customer": return 2;
                case "Admin": return 3;
                default: return -1;
            }
        }
    }
}
