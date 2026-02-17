using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
public class CheckpointsController : ApiController
        {
        CargoConnectEntities3 db = new CargoConnectEntities3();
            [HttpGet]
            [Route("api/checkpoints/status")]
            public IHttpActionResult GetCheckpointsStatus()
            {
                return Ok("SUCCESS: Checkpoints connection successful.");
            }
            [HttpGet]
            [Route("api/checkpoints")]
            public IHttpActionResult GetCheckpoints()
            {
                var checkpoints = db.Checkpoints.Select(nr => new
                {
                    nr.checkpoint_id,
                    nr.name,
                    nr.latitude,
                    nr.longitude,

                }).ToList();
                return Ok(checkpoints);
            }
            [HttpGet]
            [Route("api/checkpoints/{id}")]
            public IHttpActionResult GetCheckpoint(int id)
            {
                var result = db.Checkpoints.Select(nr => new
                {
                    nr.checkpoint_id,
                    nr.name,
                    nr.latitude,
                    nr.longitude,

                }).Where(nr => nr.checkpoint_id == id).FirstOrDefault();
                if (result == null)
                    return BadRequest("ERROR: No entries found.");
                else
                    return Ok(result);
            }
        }
    }
