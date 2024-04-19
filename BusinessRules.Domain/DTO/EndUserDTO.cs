using BusinessRules.Domain.Fields;

namespace BusinessRules.Domain.DTO
{
    public class EndUserDTO
	{
        
        public BizField CurrentField { get; set; }
        public string BaseEndpointUrl { get; set; }
        public Guid CurrentApiKey { get; set; }

        /// <summary>
        /// DO NOT USE - required by model binder
        /// </summary>
        public EndUserDTO()
        {
                
        }

        public EndUserDTO(BizField currentField, Guid currentApiKey, string baseEndpointUrl)
        {
            CurrentField = currentField;
            CurrentApiKey = currentApiKey;
            BaseEndpointUrl = baseEndpointUrl;
        }
    }
}
