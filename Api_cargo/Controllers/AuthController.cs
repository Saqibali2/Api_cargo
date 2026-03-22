using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Data.Entity;
using System.Transactions;
namespace Api_cargo.Controllers
{
        public class AuthController : ApiController
        {
        CargoConnectEntities3 db = new CargoConnectEntities3();

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
        public string SaveImage(string base64Image, string fileName)
        {
            if (string.IsNullOrEmpty(base64Image)) return null;

            // Agar image mein "data:image/jpeg;base64," header ho to usey hatayein
            if (base64Image.Contains(","))
            {
                base64Image = base64Image.Split(',')[1];
            }

            string folderPath = HttpContext.Current.Server.MapPath("~/UploadedImages/");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fullPath = Path.Combine(folderPath, fileName);
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            File.WriteAllBytes(fullPath, imageBytes);

            // Ye URL ab mobile app/web par access ho sakega
            return "/UploadedImages/" + fileName;
        }


[HttpPost]
    [Route("api/auth/register")]
    public IHttpActionResult Register(RegisterDataClass request)
    {
        using (var scope = new TransactionScope())
        {
            try
            {
                if (request == null ||
                    string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrEmpty(request.Password) ||
                    string.IsNullOrEmpty(request.Role))
                    return BadRequest("ERROR: Invalid registration data.");

                if (UserExists(request.Email))
                    return BadRequest("ERROR: User already exists.");

                // 🔥 VEHICLE DUPLICATE CHECK
                if (request.Role == "Driver" && request.Vehicle != null)
                {
                    if (db.Vehicle.Any(v => v.vehicle_reg_no == request.Vehicle.RegNo))
                        return BadRequest("ERROR: Vehicle already registered.");
                }

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

                    string profileFileName = $"profile_{user.user_id}_{DateTime.Now.Ticks}.jpg";
                    string profileUrl = SaveImage(request.PhotoLink, profileFileName);

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
                        profile_image_url = profileUrl,
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
                    db.SaveChanges();

                    var docs = new DriverDocuments
                    {
                        driver_id = driver.driver_id,
                        uploaded_at = DateTime.Now,
                        cnic_link = SaveImage(request.Documents.CnicLink, "cnic_" + driver.driver_id + ".jpg"),
                        license_link = SaveImage(request.Documents.LicenseLink, "license_" + driver.driver_id + ".jpg"),
                        front_link = SaveImage(request.Documents.FrontLink, "front_" + driver.driver_id + ".jpg"),
                        back_link = SaveImage(request.Documents.BackLink, "back_" + driver.driver_id + ".jpg"),
                        photo_link = profileUrl
                    };

                    db.DriverDocuments.Add(docs);
                    db.SaveChanges();
                }
                else if (request.Role == "Customer")
                {
                    string customerFileName = $"cust_{user.user_id}_{DateTime.Now.Ticks}.jpg";

                    var customer = new Customer
                    {
                        user_id = user.user_id,
                        first_name = request.FirstName,
                        last_name = request.LastName,
                        CNIC = request.CNIC,
                        contact_no = request.ContactNo,
                        city = request.City,
                        street_no = request.StreetNo,
                        profile_image_url = SaveImage(request.PhotoLink, customerFileName)
                    };

                    db.Customer.Add(customer);
                    db.SaveChanges();
                }
                else
                {
                    return BadRequest("ERROR: Invalid role.");
                }

                int id = user.role_id;


                    scope.Complete(); 

                return Ok(new
                {
                    message = "SUCCESS: Registration successful",
                    role = request.Role,
                    userId = user.user_id,
                    roleBasedId = id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex); 
            }
        }
    }

    [HttpPost]
            [Route("api/auth/login")]
            public IHttpActionResult LoginUser(LoginRequest request)
            {
                int id = -1;
                if (request == null ||
                    string.IsNullOrEmpty(request.Email) ||
                    string.IsNullOrEmpty(request.Password))
                    return BadRequest("ERROR: Input data is null or invalid.");

                var email = request.Email.Trim().ToLower();
                var password = request.Password.Trim();

                var existingUser = db.Users.FirstOrDefault(u =>
                    u.email.ToLower() == email &&
                    u.password == password
                );

                if (existingUser == null)
                    return BadRequest("ERROR: Email or password is incorrect.");

                if (existingUser.role_id == 1)
                {
                    id = db.Admin.FirstOrDefault(r => r.user_id == existingUser.user_id)?.admin_id ?? -1;
                }
                else if (existingUser.role_id == 2)
                {
                    id = db.Customer.FirstOrDefault(r => r.user_id == existingUser.user_id)?.customer_id ?? -1;
                }
                else if (existingUser.role_id == 3)
                {
                    id = db.Driver.FirstOrDefault(r => r.user_id == existingUser.user_id)?.driver_id ?? -1;
                }

                return Ok(new
                {
                    message = "SUCCESS: Login successful.",
                    roleID = existingUser.role_id,
                    roleName = db.Roles
                            .FirstOrDefault(r => r.role_id == existingUser.role_id)?.role_name,
                    userID = existingUser.user_id,
                    roleBasedId = id
                });
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
            [Route("api/auth/upload")]
            public async Task<IHttpActionResult> UploadFile()
            {
                if (!Request.Content.IsMimeMultipartContent())
                {
                    return BadRequest("Unsupported media type.");
                }

                try
                {
                    var root = System.Web.HttpContext.Current.Server.MapPath("~/Uploads");
                    if (!System.IO.Directory.Exists(root)) System.IO.Directory.CreateDirectory(root);

                    var provider = new MultipartFormDataStreamProvider(root);
                    await Request.Content.ReadAsMultipartAsync(provider);

                    var file = provider.FileData.FirstOrDefault();
                    if (file == null) return BadRequest("No file uploaded.");

                    var originalFileName = file.Headers.ContentDisposition.FileName.Trim('\"');
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                    var fullPath = System.IO.Path.Combine(root, uniqueFileName);

                    System.IO.File.Move(file.LocalFileName, fullPath);

                    var request = System.Web.HttpContext.Current.Request;
                    var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}{request.ApplicationPath.TrimEnd('/')}/Uploads/{uniqueFileName}";

                    return Ok(new { link = baseUrl });
                }
                catch (Exception ex)
                {
                    return BadRequest("EXCEPTION: " + ex.Message);
                }
            }
        [HttpGet]
        [Route("api/users/getdata/{userId}")]
        public IHttpActionResult GetUserData(int userId)
        {
            var user = db.Users.Find(userId);
            if (user == null) return NotFound();

            object userData = null;

            if (user.role_id == 3)
            {
                userData = db.Driver.Where(d => d.user_id == userId)
                    .Select(d => new {
                        roleBasedId = 3,
                        name = d.first_name + " " + d.last_name,
                        contact = d.contact_no,
                        license_no = d.licence_no,
                        street_no = d.street_no,
                        city = d.city,
                        profileImageUrl = d.profile_image_url
                    }).FirstOrDefault();
            }
            else if (user.role_id == 2)
            {
                userData = db.Customer.Where(c => c.user_id == userId)
                    .Select(c => new {
                        roleBasedId = 2,
                        name = c.first_name + " " + c.last_name,
                        contact = c.contact_no,
                        street_no = c.street_no,
                        city = c.city,
                        profileImageUrl = c.profile_image_url
                    }).FirstOrDefault();
            }
            else if (user.role_id == 1)
            {
                userData = new
                {
                    roleBasedId = 1,
                    name = "Admin",
                    contact = "N/A",
                    street_no = "Office",
                    city = "HQ",
                    
                };
            }

            if (userData == null) return NotFound();

            return Ok(userData);
        }
        //[HttpPost]
        //[Route("api/users/getdata")]
        //public IHttpActionResult GetUserData([FromBody] UserIdRequest request)
        //{
        //    if (request == null) return BadRequest("Invalid request");

        //    var user = db.Users.Find(request.userId);
        //    if (user == null) return NotFound();

        //    object userData = null;

        //    if (user.role_id == 3)
        //    {
        //        userData = db.Driver.Where(d => d.user_id == request.userId)
        //            .Select(d => new {
        //                name = d.first_name + " " + d.last_name,
        //                contact = d.contact_no,
        //                license_no = d.licence_no,
        //                street_no = d.street_no,
        //                city = d.city,
        //                profileImageUrl = d.profile_image_url,

        //                //allRoutes = db.Routes.Where(r => r.driver_id == d.driver_id).Select(r => new {
        //                //    r.route_id,
        //                //    r.is_active,
        //                //    r.is_next_route,
        //                //    points = db.Checkpoints.Where(c => c.route_id == r.route_id)
        //                //               .OrderBy(c => c.sequence_no)
        //                //               .Select(c => new { c.name, c.latitude, c.longitude, c.sequence_no, c.reached })
        //                //               .ToList()
        //                //}).ToList(),

        //                //activeRoute = db.Routes.Where(r => r.driver_id == d.driver_id && r.is_active == true).Select(r => new {
        //                //    r.route_id,
        //                //    points = db.Checkpoints.Where(c => c.route_id == r.route_id)
        //                //               .OrderBy(c => c.sequence_no)
        //                //               .Select(c => new { c.name, c.latitude, c.longitude, c.sequence_no, c.reached })
        //                //               .ToList()
        //                //}).FirstOrDefault(),

        //                //nextRoute = db.Routes.Where(r => r.driver_id == d.driver_id && r.is_next_route == true).Select(r => new {
        //                //    r.route_id,
        //                //    points = db.Checkpoints.Where(c => c.route_id == r.route_id)
        //                //               .OrderBy(c => c.sequence_no)
        //                //               .Select(c => new { c.name, c.latitude, c.longitude, c.sequence_no, c.reached })
        //                //               .ToList()
        //                //}).FirstOrDefault()

        //            }).FirstOrDefault();
        //    }
        //    else if (user.role_id == 2)
        //    {
        //        userData = db.Customer.Where(c => c.user_id == request.userId)
        //            .Select(c => new {
        //                name = c.first_name + " " + c.last_name,
        //                contact = c.contact_no,
        //                license_no = "N/A",
        //                street_no = c.street_no,
        //                city = c.city,
        //                profileImageUrl = c.profile_image_url
        //            }).FirstOrDefault();
        //    }
        //    else if (user.role_id == 1) // Admin
        //    {
        //        userData = db.Admin.Where(a => a.user_id == request.userId)
        //            .Select(a => new {
        //                name = a.first_name + " " + a.last_name,
        //                contact = a.contact_no,
        //                license_no = "N/A",
        //                street_no = "Office",
        //                city = "Headquarters",
        //                profileImageUrl = "N/A"
        //            }).FirstOrDefault();
        //    }

        //    if (userData == null) return NotFound();

        //    return Ok(userData);
        //}
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
                    case "Driver": return 3;
                    case "Customer": return 2;
                    case "Admin": return 1;
                    default: return -1;
                }
            }
        }
    }
    public class UserIdRequest
    {
        public int userId { get; set; }
    }