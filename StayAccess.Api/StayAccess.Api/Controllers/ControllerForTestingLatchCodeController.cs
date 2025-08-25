using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StayAccess.DAL.DomainEntities;
using StayAccess.DTO.Responses.Latch;
using StayAccess.Latch;
using StayAccess.Latch.Interfaces;
using StayAccess.Latch.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StayAccess.Api.Controllers
{
    [Microsoft.AspNetCore.Components.Route("api/LatchTests/[controller]")]
    [ApiController]
    public class ControllerForTestingLatchCodeController : BaseController
    {
        //private readonly ILatchService _latchRepo;
        //private readonly LatchService _latchServiceTesting;

        //public ControllerForTestingLatchCodeController(ILatchService latchRepo, LatchService latchServiceTesting)
        //{
        //    _latchRepo = latchRepo;
        //    _latchServiceTesting = latchServiceTesting;
        //}

       // [HttpGet]
       // [Microsoft.AspNetCore.Mvc.Route("Test1")]
       // [ProducesResponseType(StatusCodes.Status200OK)]
       // [ProducesResponseType(StatusCodes.Status400BadRequest)]
       // public async Task<LatchReturn<CreateReservationResponse>> Test1(Reservation reservation, ReservationCode reservationCode, bool isCronJob, DateTime currentEstTime)
       // {
       // //    try
       /////     {

       //       //  return await _latchServiceTesting.CreateCodeForLatchAsyncTest(reservation, reservationCode, isCronJob, currentEstTime);

       //         // return "Tested latch and worked. ";
       //         // return _latchRepo.CreateCodeForLatchAsync();
       // //    }
       // //    catch (Exception ex)
       //  //   {
       //   //      return ex.Message;
       //    // }
       // }


        //[HttpGet]
        //[Microsoft.AspNetCore.Mvc.Route("Test2")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<LatchReturn<CreateReservationResponse>> Test2Async(Reservation reservation, ReservationCode reservationCode, bool isCronJob, DateTime currentEstTime)
        //{
        //    // try
        //    // {
        //    return await _latchServiceTesting.CreateCodeForLatchAsyncTest(reservation, reservationCode, isCronJob, currentEstTime);
        //    // return _latchRepo.CreateCodeForLatchAsync();
        //    //  }
        //    //  catch (Exception ex)
        //    //  {

        //    // return ex.Message;
        //    //  }
        //}


    }
}



