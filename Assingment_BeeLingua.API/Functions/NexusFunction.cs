using System.IO;
using System.Threading.Tasks;
using Assingment_BeeLingua.API.DTO;
using Assingment_BeeLingua.BLL.Services;
using Assingment_BeeLingua.DAL.Models;
using static Assingment_BeeLingua.DAL.Repository.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Net;
using Nexus.Base.CosmosDBRepository;
using AzureFunctions.Extensions.Swashbuckle.Attribute;

namespace Assingment_BeeLingua.API.Functions
{
    public class NexusFunction
    {
        private readonly IMapper _mapper;
        private readonly LessonService _lessonService;
        public NexusFunction(CosmosClient client)
        {
            if (_mapper == null)
            {
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<Lesson, LessonDTO>();
                });
                _mapper = config.CreateMapper();
            }

            _lessonService ??= new LessonService(new LessonRepository(client));
        }

        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PageResult<Lesson>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        [FunctionName("GetLessonById")]
        public async Task<IActionResult> GetLessonById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Lesson/GetById/{id}/{pk}")] HttpRequest req,
            string id,
            string pk,
            [SwaggerIgnore] ILogger log)
        {
            try
            {
                var data = await _lessonService.GetLessonById(id, new Dictionary<string, string> { { "LessonCode", pk } });
                if (data == null)
                {
                    return new NotFoundObjectResult("Data not found !!!!");
                }
                var dataDTO = _mapper.Map<LessonDTO>(data);
                return new OkObjectResult(dataDTO);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PageResult<Lesson>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        [FunctionName("GetLessonAll")]
        // TODO: routing tdk konsisten ada yang lengkap routingnya, tp ada jg yang null? : DONE
        public async Task<IActionResult> GetLessonAll(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Lesson/GetAll")] HttpRequest req,
            [SwaggerIgnore]  ILogger log)
        {
            try
            {
                var data = await _lessonService.GetLessonAll();
                var list = new List<LessonDTO>();
                if (data == null)
                {
                    return new NotFoundObjectResult("Data is Empty!!!");
                }
                var dataDTO = _mapper.Map<List<LessonDTO>>(data);
                return new OkObjectResult(dataDTO);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PageResult<Lesson>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        [FunctionName("CreateLesson")]
        public async Task<IActionResult> CreateLesson(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Lesson/Create")] HttpRequest req,
           [RequestBodyType(typeof(Lesson), "CreateLesson request")]
           [SwaggerIgnore] ILogger log)
        {
            try
            {
                string reqBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<Lesson>(reqBody);

                var dataToBeInserted = new Lesson
                {
                    LessonCode = input.LessonCode,
                    Description = input.Description
                };

                var data = await _lessonService.CreateLesson(dataToBeInserted);
                var dataDTO = _mapper.Map<LessonDTO>(data);

                return new OkObjectResult(dataDTO);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PageResult<Lesson>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        [FunctionName("UpdateLesson")]
        public async Task<IActionResult> UpdateLesson(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Lesson/Update/{id}")] HttpRequest req,
           [RequestBodyType(typeof(Lesson), "UpdateLesson request")]
           string id,
           [SwaggerIgnore] ILogger log)
        {
            try
            {
                string reqBody = await new StreamReader(req.Body).ReadToEndAsync();
                var input = JsonConvert.DeserializeObject<Lesson>(reqBody);

                var dataExisting = await _lessonService.GetLessonById(id, new Dictionary<string, string> { { "LessonCode", input.LessonCode } });
                if (dataExisting == null)
                {
                    return new NotFoundObjectResult("Data not found !!!!");
                }

                dataExisting.Description = input.Description;

                var data = await _lessonService.UpdateLesson(id, dataExisting);
                var dataDTO = _mapper.Map<LessonDTO>(data);

                return new OkObjectResult(dataDTO);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PageResult<Lesson>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        [FunctionName("DeleteLesson")]
        public async Task<IActionResult> DeleteLesson(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Lesson/Delete/{id}/{pk}")] HttpRequest req,
            string id,
            string pk,
            ILogger log)
        {
            try
            {
                var data = await _lessonService.DeleteLesson
                    (id, new Dictionary<string, string> { { "LessonCode", pk } });
                return new OkObjectResult(data);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }

        //
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PageResult<Lesson>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        [FunctionName("CreateLessonEdited")]
        public async Task<IActionResult> CreateLessonEdited(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Lesson/CreateEdited/{id}/{pk}")] HttpRequest req,
           string id,
           string pk,
           [RequestBodyType(typeof(Lesson), "CreateLesson request")]
           [SwaggerIgnore] ILogger log)
        {
            try
            {
                var data = await _lessonService.CreateLessonEdited(id, new Dictionary<string, string> { { "LessonCode", pk } });
                var dataDTO = _mapper.Map<LessonDTO>(data);

                return new OkObjectResult(dataDTO);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}

