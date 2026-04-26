using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class ReviewsController : ApiController
    {
        CargoConnectEntities4 db = new CargoConnectEntities4();

        [HttpGet]
        [Route("api/reviews/status")]
        public IHttpActionResult GetReviewsStatus()
        {
            return Ok("SUCCESS: Reviews connection successful.");
        }
        [HttpGet]
        [Route("api/reviews/average")]
        public IHttpActionResult GetAverageRating(int userId)
        {

            var reviews = db.Reviews
                .Where(r => r.target_user_id == userId);

            if (!reviews.Any())
            {
                return Ok(new
                {
                    user_id = userId,
                    average_rating = 0,
                    total_reviews = 0
                });
            }

            var result = new
            {
                user_id = userId,
                average_rating = Math.Round(reviews.Average(r => (double?)r.rating) ?? 0, 1),
                total_reviews = reviews.Count()
            };

            return Ok(result);
        }
        [HttpGet]
        [Route("api/reviews/user")]
        public IHttpActionResult GetUserReviews(int userId)
        {
            var query = db.Reviews
                .Where(r => r.reviewer_user_id == userId || r.target_user_id == userId);

            var reviews = (from r in query
                           join ru in db.Users on r.reviewer_user_id equals ru.user_id
                           join tu in db.Users on r.target_user_id equals tu.user_id
                           select new
                           {
                               id = r.review_id,
                               trip_id = r.trip_id,
                               rating = r.rating,
                               comment = r.comments,
                               created_at = r.created_at,

                               type = r.target_user_id == userId ? "received" : "given",

                               reviewer_name = ru.email,
                               reviewer_role = ru.role_id == 3 ? "driver" :
                                               ru.role_id == 2 ? "customer" : "admin",

                               target_name = tu.email,
                               target_role = tu.role_id == 3 ? "driver" :
                                             tu.role_id == 2 ? "customer" : "admin"
                           })
                           .OrderByDescending(r => r.created_at)
                           .ToList();

            var avg = Math.Round(query.Average(r => (double?)r.rating) ?? 0, 1);

            return Ok(new
            {
                average_rating = avg,
                total_reviews = query.Count(),
                reviews = reviews
            });
        }

    }

}