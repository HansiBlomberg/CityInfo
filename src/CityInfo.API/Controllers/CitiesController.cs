
using AutoMapper;
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
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

        public CitiesController(ICityInfoRepository cityInfoRepository)
        {
            _cityInfoRepository = cityInfoRepository;
        }

        [Authorize(Roles = "Administrator, CityManager, Explorer")]
        [HttpGet]
        

        public IActionResult GetCities()
        {
            // return Ok(CitiesDataStore.Current.Cities);
            var cityEntities = _cityInfoRepository.GetCities();

            var results = Mapper.Map<IEnumerable<CityWithoutPointsOfInterestDto>>(cityEntities);
            
            return Ok(results);


        }

        [Authorize(Roles = "Administrator, CityManager, Explorer")]
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

    }
}
