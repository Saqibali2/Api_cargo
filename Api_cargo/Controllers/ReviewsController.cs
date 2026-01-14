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
        CargoConnectEntities2 db = new CargoConnectEntities2();

        [HttpGet]
        [Route("api/reviews/status")]
        public IHttpActionResult GetReviewsStatus()
        {
            return Ok("SUCCESS: Reviews connection successful.");
        }

        [HttpPost]
        [Route("api/reviews/add")]
        public IHttpActionResult AddReview(Reviews review)
        {
            if (review == null)
                return BadRequest("ERROR: Invalid review data.");

            db.Reviews.Add(review);
            db.SaveChanges();

            return Ok("SUCCESS: Review added successfully.");
        }

        [HttpGet]
        [Route("api/reviews/findbyid/{targetUserId}")]
        public IHttpActionResult GetReviewsByUser(int targetUserId)
        {
            var reviews = db.Reviews.Where(r => r.target_user_id == targetUserId).Select(s => new
            {
                s.trip_id,
                s.reviewer_user_id,
                s.target_user_id,
                s.rating,
                s.comments,
                s.created_at
            }
            );
            return Ok(reviews);
        }

        [HttpGet]
        [Route("api/reivews/findbytrip/{tripId}")]
        public IHttpActionResult GetReviewsByTrip(int tripId)
        {
            var reviews = db.Reviews.Where(t => t.trip_id == tripId).ToList();
            return Ok(reviews);
        }

        [HttpGet]
        [Route("api/reviews/findbyuser/{id}")]
        public IHttpActionResult GetReviewsByUserId(int id)
        {
            var reviews = db.Reviews.Where(r => r.reviewer_user_id == id).ToList();

            if (!reviews.Any())
                return Ok("No reviews given");

            return Ok(reviews);
        }
    }
}
