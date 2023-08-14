//using Microsoft.Extensions.Logging;
//using System.Net.Http;
//using System.Text;
//using System.Xml.Linq;

//namespace EwtLinkGenerator
//{
//    public class NativeEwsClient
//    {
//        private readonly IHttpClientFactory clientFactory;
//        private readonly HttpClient m365Client;

//        public NativeEwsClient(IHttpClientFactory clientFactory)
//        {
//            this.clientFactory = clientFactory;

//        }

//        public string GetToken(EwtRequest req)
//        {
//            var payload = $"client_id={req.AppId}&client_secret={req.ClientSecret}&scope=https%3A%2F%2Foutlook.office.com%2F.default&grant_type=client_credentials";
        
//        }

//        private async Task<XDocument> SendEWSRequest(EwtRequest req, StringContent payload)
//        {
//            HttpClient client = clientFactory.CreateClient();
//            string url = req.IsM365 ? "https://outlook.office365.com/EWS/Exchange.asmx" : req.EwsUrl;
//            if (!req.IsM365)
//            {
//                string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{req.Username}@OVERREACH:{req.Password}"));
//                client.DefaultRequestHeaders.Add("Authorization", $"Basic {creds}");
//            }
//            else
//            {

//            }

//            HttpResponseMessage response = await client.PostAsync(url, payload);
//            string responseContent = await response.Content.ReadAsStringAsync();
//            XDocument xml = null;
//            try
//            {
//                xml = XDocument.Parse(responseContent);
//            }
//            catch (Exception ex)
//            {
//                throw new InvalidOperationException("Unable to parse EWS response XML", ex);
//            }

//            // var responseCode = GetXmlTagContents(xml, "ResponseCode", logUnableToFindMessage: false);
//            //var message = GetXmlTagContents(xml, "MessageText", logUnableToFindMessage: false) ?? GetXmlTagContents(xml, "Message", logUnableToFindMessage: false);

//            if (!response.IsSuccessStatusCode) // || (!String.IsNullOrEmpty(responseCode) && responseCode != "NoError"))
//            {
//                throw new InvalidOperationException($"EWS request did not succeed; status: {(int)response.StatusCode}");
//            }

//            return xml;
//        }

//        private string GetSoapEnvelope(string request)
//        {
//            var result =
//            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
//            "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
//            "               xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"" +
//            "               xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
//            "               xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\">" +
//            "  <soap:Header>" +
//            "  <t:RequestServerVersion Version=\"Exchange2013_SP1\"/>" +
//            "  </soap:Header>" +
//            $"  <soap:Body>{request}</soap:Body>" +
//            "</soap:Envelope>";
//            return result;
//        }

//        /// <summary>
//        /// This is required if the request is from an O365 token on behalf of an App like in EWT, 
//        /// as opposed to on behalf of the user that is passed into the API. Failure to include this header will result in this error:
//        /// "ExchangeImpersonation SOAP header must be present for this type of OAuth token."
//        /// </summary>
//        private string GetSoapEnvelopeWithImpersonation(string request, string impersonationEmail)
//        {
//            var result =
//                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
//                "<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
//                "               xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"" +
//                "               xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"" +
//                "               xmlns:m=\"http://schemas.microsoft.com/exchange/services/2006/messages\"" +
//                "               xmlns:t=\"http://schemas.microsoft.com/exchange/services/2006/types\">" +
//                "  <soap:Header>" +
//                "  <t:RequestServerVersion Version=\"Exchange2013_SP1\"/>" +
//                "  <t:ExchangeImpersonation>" +
//                "    <t:ConnectingSID>" +
//                $"      <t:SmtpAddress>{impersonationEmail}</t:SmtpAddress>" +
//                "    </t:ConnectingSID>" +
//                "  </t:ExchangeImpersonation>" +
//                "  </soap:Header>" +
//                $"  <soap:Body>{request}</soap:Body>" +
//                "</soap:Envelope>";
//            return result;
//        }

//        //private string GetXmlTagContents(XDocument xmlDoc, string tagName, bool logUnableToFindMessage = true)
//        //{
//        //    if (xmlDoc is null) return null;
//        //    try
//        //    {
//        //        var elements = xmlDoc.Descendants().Where(e => e.Name.LocalName == tagName).ToList();
//        //        if (!elements.Any())
//        //        {
//        //            if (logUnableToFindMessage) logger.LogWarning($"Unable to find any element \"{tagName}\" in xml document");
//        //            return null;
//        //        }
//        //        var element = elements.First();
//        //        if (String.IsNullOrWhiteSpace(element.Value))
//        //        {
//        //            logger.LogWarning($"Found empty element \"{tagName}\" in xml document");
//        //            return null;
//        //        }
//        //        return elements.First().Value?.Trim();
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        logger.LogError(ex, $"Unable to find element \"{tagName}\" in xml document");
//        //        return null;
//        //    }
//        //}
//    }
//}
