using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Assingment_BeeLingua.DAL.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid.Helpers.Mail;

namespace Assingment_BeeLingua.API.Functions
{
    public static class EmailFunction
    {
        private const string attachFile = @"Nature.mp4";

        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        [FunctionName("SendEmailFunction")]
        public static async Task<IActionResult> SendEmailFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            [RequestBodyType(typeof(OutgoingEmail), "UpdateLesson request")] HttpRequest req,
            [SendGrid(ApiKey = "SendGridAPIKey")] IAsyncCollector<SendGridMessage> messageCollector,
            [SwaggerIgnore]  ILogger log)
        {
            //string reqBody = await new StreamReader(req.Body).ReadToEndAsync();
           
            
            var reqBody = await req.ReadFormAsync();
            string request = reqBody["Request"];
            var emailObject = JsonConvert.DeserializeObject<OutgoingEmail>(request);
            var files = req.Form.Files["files"];
            
            var ms = new MemoryStream();
            files.CopyTo(ms);
            var fileBytes = ms.ToArray();

            emailObject.Subject = emailObject.Subject;
           
            string[] recepient = {"derianpratama@gmail.com", "fajri@ecomindo.com", " adi.nugraha@ecomindo" };
            foreach (string to in recepient)
            {
                emailObject.To = to;
                var message = MailHelper.CreateSingleTemplateEmail(
                from: new EmailAddress(emailObject.From),
                to: new EmailAddress(to),
                templateId: "d-243f54ad82ef484d8ed89e4c0fb24f9a",
                dynamicTemplateData: emailObject);
                message.AddAttachment(files.FileName, Convert.ToBase64String(fileBytes));
                await messageCollector.AddAsync(message);
            }
            return new OkObjectResult("Email Sent");

        }
    }
}

