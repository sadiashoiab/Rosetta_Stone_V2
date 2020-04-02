using System.Collections.Generic;
using System.Threading.Tasks;
using ClearCareOnline.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rosetta.Models;
using Rosetta.Services;

namespace Rosetta.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        private readonly IRosettaStoneService _rosettaStoneService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IRosettaStoneService rosettaStoneService, ILogger<HomeController> logger)
        {
            _rosettaStoneService = rosettaStoneService;
            _logger = logger;
        }

        // GET status ..vv
        [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        public async Task<ActionResult<Status>> Status()
        {
            var result = await _rosettaStoneService.GetStatus();
            return Ok(result);
        }

        // GET franchise/220
        [ProducesResponseType(typeof(RosettaFranchise), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpGet("franchise/{franchiseNumber}")]
        public async Task<ActionResult<RosettaFranchise>> GetFranchise(string franchiseNumber)
        {
            var result = await _rosettaStoneService.GetFranchise(franchiseNumber);
            if (result != null)
            {
                return Ok(result);
            }

            _logger.LogError($"No ClearCare Agency found for Franchise Number: {franchiseNumber}");
            return NotFound();
        }

        // GET agencies
        [ProducesResponseType(typeof(IList<AgencyFranchiseMap>), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet("agencies")]
        public async Task<ActionResult<IList<AgencyFranchiseMap>>> GetAgencies()
        {
            var result = await _rosettaStoneService.GetAgencies();
            if (result != null)
            {
                return Ok(result);
            }

            _logger.LogError("No ClearCare Agencies found");
            return NotFound();
        }

        // GET franchises
        [ProducesResponseType(typeof(IList<RosettaFranchise>), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet("franchises")]
        public async Task<ActionResult<IList<RosettaFranchise>>> GetFranchises()
        {
            var result = await _rosettaStoneService.GetFranchises();
            if (result != null)
            {
                return Ok(result);
            }
            
            _logger.LogError("No ClearCare Franchises found");
            return NotFound();
        }

        //// note: this is here for debugging/testing purposes
        //// GET franchises
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[HttpGet("clear_cache")]
        //public ActionResult ClearCache()
        //{
        //    _rosettaStoneService.ClearCache();
        //    return Ok();
        //}
    }
}
