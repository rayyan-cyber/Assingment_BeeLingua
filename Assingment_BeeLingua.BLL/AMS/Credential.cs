using Microsoft.Azure.Management.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assingment_BeeLingua.BLL.AMS
{
    public class Credential
    {
        public IAzureMediaServicesClient Client { get; set; }
        public string AadClientId { get; set; }
        public string AadSecret { get; set; }
        public string AadTenantId { get; set; }
        public string AccountName { get; set; }
        public Uri AadEndpoint { get; set; }
        public Uri ArmAadAudience { get; set; }
        public Uri ArmEndpoint { get; set; }
        public string Region { get; set; }
        public string ResourceGroup { get; set; }
        public string SubscriptionId { get; set; }
    }
}
