using System;
using Newtonsoft.Json;

namespace EmailApproval
{
    public class OptionModel
    {

        [JsonProperty("option")]
        public string Option { get; set; }
        
    }
}
