using System;
using System.Collections.Generic;
using System.Text;

namespace Assingment_BeeLingua.API.Functions.MediaService.DTO
{
    public class ConfigAssetDTO
    {
        public ConfigAssetDTO()
        {
            InputName = "input-{0}";
            OutputName = "output-{0}";
            JobName = "job-{0}";
            LocatorName = "locator-{0}";
        }
        public string AssetID { get; set; }
        public string InputName { get; set; }
        public string OutputName { get; set; }
        public string JobName { get; set; }
        public string LocatorName { get; set; }
    }
}
