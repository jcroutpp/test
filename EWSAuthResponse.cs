using Microsoft.Exchange.WebServices.Data;

namespace EwtLinkGenerator
{
    public class EWSAuthResponse
    {
        public string Token { get; }
        public string TenantId { get; }
        public ExchangeService ExchangeService { get; }

        public EWSAuthResponse(ExchangeService exchangeService, string token, string tenantId = null)
        {
            Token = token;
            TenantId = tenantId;
            ExchangeService = exchangeService;
        }
    }
}
