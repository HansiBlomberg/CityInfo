
using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CityInfo.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class CitiesController : Controller
    {
        private ICityInfoRepository _cityInfoRepository;
        private IMailService _mailService;

        public CitiesController(ICityInfoRepository cityInfoRepository,
            IMailService mailService)
        {
            _cityInfoRepository = cityInfoRepository;
            _mailService = mailService;
        }

        [Authorize(Roles = "Administrator, CityManager, Explorer, Traveler")]
        [HttpGet]
        

        public IActionResult GetCities()
        {
            // return Ok(CitiesDataStore.Current.Cities);
            var cityEntities = _cityInfoRepository.GetCities();

            var results = Mapper.Map<IEnumerable<CityWithoutPointsOfInterestDto>>(cityEntities);
            
            return Ok(results);


        }

        [Authorize(Roles = "Administrator, CityManager, Explorer, Traveler")]
        [HttpGet("{id}", Name = "GetCity")]
       
        public IActionResult GetCity(int id, bool includePointsOfInterest = false )
        {

            var city = _cityInfoRepository.GetCity(id, includePointsOfInterest);
            
            if(city == null)
            {
                return NotFound();
            }
            
            if(includePointsOfInterest)
            {
                var cityResult = Mapper.Map<CityDto>(city);

                return Ok(cityResult);
            }

            var cityWithoutPointsOfInterestResult = Mapper.Map<CityWithoutPointsOfInterestDto>(city);
            return Ok(cityWithoutPointsOfInterestResult);

            
        }

        [Authorize(Roles = "Administrator, CityManager")]
        [HttpPost]
        public IActionResult CreateCity([FromBody] CityWithoutPointsOfInterestForCreationDto city)
        {
            if (city == null)
            {
                return BadRequest();
            }

            if (city.Description == city.Name)
            {
                ModelState.AddModelError("Description", "The provided description should be different from the name.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


           
            var finalCity = Mapper.Map<Entities.City>(city);

            _cityInfoRepository.AddCity(finalCity);

            if (!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }

            var createdCityToReturn = Mapper.Map<Models.CityWithoutPointsOfInterestDto>(finalCity);

            var createdAt = CreatedAtRoute(
              routeName: "GetCity",
              routeValues: new
              { id = createdCityToReturn.Id },
              value: createdCityToReturn);

            return createdAt;
        }

        [Authorize(Roles = "Administrator, CityManager")]
        [HttpPut("{id}")]
        public IActionResult UpdateCity(int id,
           [FromBody] CityWithoutPointsOfInterestForUpdateDto city)
        {
            if (city == null)
            {
                return BadRequest();
            }

            if (city.Description == city.Name)
            {
                ModelState.AddModelError("Description", "The provided description should be different from the name.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var cityEntity = _cityInfoRepository.GetCity(id);
            if (cityEntity == null)
            {
                return NotFound();
            }

            Mapper.Map(city, cityEntity);

            if (!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }

            return NoContent();


        }
        [Authorize(Roles = "Administrator, CityManager")]
        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdatePointOfInterest(int id,
            [FromBody] JsonPatchDocument<CityWithoutPointsOfInterestForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            var cityEntity = _cityInfoRepository.GetCity(id);
            if (cityEntity == null)
            {
                return NotFound();
            }

            var cityToPatch = Mapper.Map<CityWithoutPointsOfInterestForUpdateDto>(cityEntity);

            patchDoc.ApplyTo(cityToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (cityToPatch.Description == cityToPatch.Name)
            {
                ModelState.AddModelError("Description", "The provided description should be different from the name.");
            }

            TryValidateModel(cityToPatch);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            Mapper.Map(cityToPatch, cityEntity);

            if (!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }


            return NoContent();

        }

        [Authorize(Roles = "Administrator, CityManager")]
        [HttpDelete("{id}")]
        public IActionResult DeleteCity(int id)
        {
         

            var cityEntity = _cityInfoRepository.GetCity(id);
            if (cityEntity == null)
            {
                return NotFound();
            }

            _cityInfoRepository.DeleteCity(cityEntity);

            if (!_cityInfoRepository.Save())
            {
                return StatusCode(500, "A problem happened while handling your request.");
            }

            _mailService.Send("Point of interest deleted.",
                $"Point of interest {cityEntity.Name} with id {cityEntity.Id} was deleted.");

            return NoContent();

        }

    }
}
