using System;
using System.Collections.Generic;
using System.Text;

namespace Assingment_BeeLingua.BLL.AMS
{
    public class ConfigAsset
    {
        public ConfigAsset()
        {
            InputName = "input-{0}";
            InputDescription = "original \"{0}\"";
            OutputName = "output-{0}";
            OutputDescription = "encoded \"{0}\"";
            JobName = "job-{0}";
            LocatorName = "locator-{0}";

            UploadExpiryInHours = 12;
        }
        public string InputName { get; set; }
        public string InputDescription { get; set; }
        public string OutputName { get; set; }
        public string OutputDescription { get; set; }

        public string JobName { get; set; }
        public string LocatorName { get; set; }

        public int UploadExpiryInHours { get; set; }
    }
}
