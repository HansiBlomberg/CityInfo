using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityInfo.API.Controllers
{

    [Authorize]
    [Route("api/cities")]
    public class PointsOfInterestController : Controller
    {
        private ILogger<PointsOfInterestController> _logger;
        private IMailService _mailService;
        private ICityInfoRepository _cityInfoRepository;

        public PointsOfInterestController(ILogger<PointsOfInterestController> logger,
            IMailService mailService,
            ICityInfoRepository cityInfoRepository)
        {
            _logger = logger;
            _mailService = mailService;
            _cityInfoRepository = cityInfoRepository;
        }

        [Authorize(Roles = "Administrator, CityManager, Explorer, Traveler")]
        [HttpGet("{cityId}/pointsofinterest")]
        public IActionResult GetPointsOfInterest(int cityId)
        {

          

            try
            {
                // throw new Exception("Exception test!!");

                // var city = CitiesDataStore.Current.Cities.FirstOrDefault(c => c.Id == cityId);


                if (!_cityInfoRepository.CityExists(cityId))
                {
                    _logger.LogInformation($"City with id {cityId} wasn't found when accessing points of interest.");
                    return NotFound();
                }

                var pointsOfInterestForCity = _cityInfoRepository.GetPointsOfInterestForCity(cityId);
                var pointsOfInterestForCityResults =
                    Mapper.Map<IEnumerable<PointOfInterestDto>>(pointsOfInterestForCity);
                

                return Ok(pointsOfInterestForCityResults);

            }

            catch (Exception ex)
            {
                _logger.LogCritical($"Exception while getting points of interest for city with id {cityId}.", ex);
                return StatusCode(500, "A problem happened while handling your request.");
            }
        }

       

        [Authorize(Roles = "Administrator, CityManager, Explorer, Traveler")]
        [HttpGet("{cityId}/pointsofinterest/{id}", Name = "GetPointOfInterest")]
        public IActionResult GetPointOfInterest(int cityId, int id)
        {


            if (!_cityInfoRepository.CityExists(cityId))
            {
                return NotFound();
            }

            var pointOfInterest = _cityInfoRepository.GetPointOfInterestForCity(cityId, id);
            if(pointOfInterest == null)
            {
                return NotFound();
            }

            var pointOfInterestResult = Mapper.Map<PointOfInterestDto>(pointOfInterest);
        

            return Ok(pointOfInterestResult);



            //var city = CitiesDataStore.Current.Cities.FirstOrDefault(c => c.Id == cityId);
            //if (city == null)
            //{
            //    return NotFound();
            //}

            //var pointOfInterest = city.PointsOfInterest.FirstOrDefault(p => p.Id == id);
            //if(pointOfInterest == null)
            //{
            //    return NotFound();
            //}

            //return Ok(pointOfInterest);

        }

        [Authorize(Roles = "Administrator, Traveler")]
        [HttpPost("{cityId}/pointsofinterest")]
        public IActionResult CreatePointOfInterest(int cityId, 
            [FromBody] PointOfInterestForCreationDto pointOfInterest)
        {

            // A traveler can only access this method if the traveler has visited the city!
            if (blockTravelerWhoNeverVisitedCity(cityId))
            {
                return NotFound();
            }


            if (pointOfInterest == null)
            {
                return BadRequest();
            }

            if(pointOfInterest.Description == pointOfInterest.Name)
            {
                ModelState.AddModelError("Description", "The provided description should be different from the name.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

         
            if (!_cityInfoRepository.CityExists(cityId))
            {
                return NotFound();
            }



            var finalPointOfInterest = Mapper.Map<Entities.PointOfInterest>(pointOfInterest);

            _cityInfoRepository.AddPointOfInterestForCity(cityId, finalPointOfInterest);

            if(!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }

            var createdPointOfInterestToReturn = Mapper.Map<Models.PointOfInterestDto>(finalPointOfInterest);

            var createdAt = CreatedAtRoute(
              routeName: "GetPointOfInterest",
              routeValues: new
              { cityId = cityId, id = createdPointOfInterestToReturn.Id },
              value: createdPointOfInterestToReturn);

            return createdAt;
        }

        [Authorize(Roles = "Administrator, Traveler")]
        [HttpPut("{cityId}/pointsofinterest/{id}")]
        public IActionResult UpdatePointOfInterest(int cityId, int id, 
            [FromBody] PointOfInterestForUpdateDto pointOfInterest)
        {

            // A traveler can only access this method if the traveler has visited the city!
            if (blockTravelerWhoNeverVisitedCity(cityId))
            {
                return NotFound();
            }

            if (pointOfInterest == null)
            {
                return BadRequest();
            }

            if (pointOfInterest.Description == pointOfInterest.Name)
            {
                ModelState.AddModelError("Description", "The provided description should be different from the name.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            
            if (!_cityInfoRepository.CityExists(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = _cityInfoRepository.GetPointOfInterestForCity(cityId, id);
            if(pointOfInterestEntity == null)
            {
                return NotFound();
            }

            Mapper.Map(pointOfInterest, pointOfInterestEntity);
           
            if(!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }

            return NoContent();


        }

        [Authorize(Roles = "Administrator, Traveler")]
        [HttpPatch("{cityId}/pointsofinterest/{id}")]
        public IActionResult PartiallyUpdatePointOfInterest(int cityId, int id,
            [FromBody] JsonPatchDocument<PointOfInterestForUpdateDto> patchDoc)
        {

            // A traveler can only access this method if the traveler has visited the city!
            if (blockTravelerWhoNeverVisitedCity(cityId))
            {
                return NotFound();
            }

            if (patchDoc == null )
            {
                return BadRequest();
            }

            if (!_cityInfoRepository.CityExists(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = _cityInfoRepository.GetPointOfInterestForCity(cityId, id);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            var pointOfInterestToPatch = Mapper.Map<PointOfInterestForUpdateDto>(pointOfInterestEntity);

            patchDoc.ApplyTo(pointOfInterestToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (pointOfInterestToPatch.Description == pointOfInterestToPatch.Name)
            {
                ModelState.AddModelError("Description", "The provided description should be different from the name.");
            }

            TryValidateModel(pointOfInterestToPatch);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            Mapper.Map(pointOfInterestToPatch, pointOfInterestEntity);

            if (!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }


            return NoContent();

        }

        [Authorize(Roles = "Administrator, Traveler")]
        [HttpDelete("{cityId}/pointsofinterest/{id}")]
        public IActionResult DeletePointOfInterest(int cityId, int id)
        {

            // A traveler can only access this method if the traveler has visited the city!
            if (blockTravelerWhoNeverVisitedCity(cityId))
            {
                return NotFound();
            }

            if (!_cityInfoRepository.CityExists(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = _cityInfoRepository.GetPointOfInterestForCity(cityId, id);
            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            _cityInfoRepository.DeletePointOfInterest(pointOfInterestEntity);

            if (!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }

            _mailService.Send("Point of interest deleted.",
                $"Point of interest {pointOfInterestEntity.Name} with id {pointOfInterestEntity.Id} was deleted.");

            return NoContent();

        }


        private bool blockTravelerWhoNeverVisitedCity(int cityId)
        {

            if (User.IsInRole("Traveler"))
            {
                // Traveller can only create, update, remove points of interests if city id is
                // in the comma separated list of city ids:s in ClaimValue of ClaimType VisitedCities
                var visitedCitiesClaim = User.Claims.SingleOrDefault(claim => claim.Type == "VisitedCities");
                if (visitedCitiesClaim == null)
                {
                    return true;
                }
                var visitedCitiesCommaSeparated = visitedCitiesClaim.Value;

                var visitedCities = visitedCitiesCommaSeparated.Split(',');
                var visitedCitiesIds = new List<int>();
                var tempId = 0;
                foreach (var visitedCity in visitedCities)
                {
                    if (int.TryParse(visitedCity, out tempId))
                    {
                        visitedCitiesIds.Add(tempId);
                    }
                }
                if (visitedCitiesIds.Contains(cityId))
                {
                    return false;  // Dont block this user, user is traveler and have visited the city
                }
                return true; // block user, user is traveler but have not visited the city
            }
            return false; // Dont block this user, user is not traveler.
        }

    }
}
