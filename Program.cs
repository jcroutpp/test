using EwtLinkGenerator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Task = System.Threading.Tasks.Task;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient("default").SetHandlerLifetime(TimeSpan.FromMinutes(5));

var app = builder.Build();
var env = app.Services.GetService<IWebHostEnvironment>();


// app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, "wwwroot"))
});

app.MapPost("/api2", () =>
{
    return "hello!";
});
app.MapPost("/api", async (HttpContext context, IHttpClientFactory clientFactory) =>
{
    string payload = null;
    using (StreamReader reader = new(context.Request.Body, Encoding.UTF8, true, 1024, true))
    {
        payload = await reader.ReadToEndAsync();
    }
    EwtRequest req = Newtonsoft.Json.JsonConvert.DeserializeObject<EwtRequest>(payload, new Newtonsoft.Json.JsonSerializerSettings
    {
        NullValueHandling = Newtonsoft.Json.NullValueHandling.Include
    });
    string link = await SendRandomEmail(req, clientFactory);
    if (Uri.TryCreate(link, UriKind.Absolute, out var _))
    {
        context.Response.Redirect(link);
    }
    else
    {
        context.Response.StatusCode = 500;
    }
});

app.Run();


static string GetEmailDomain(string headerFrom)
{
    var emailAddress = headerFrom.Replace("\"", string.Empty).Replace("\\", string.Empty);
    if (string.IsNullOrWhiteSpace(emailAddress)) return string.Empty;

    try
    {
        if (emailAddress.Contains(',')) // do we need to check all senders? the spam risk is high
            emailAddress = emailAddress.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0];

        if (emailAddress.Contains('<'))
        {
            var values = emailAddress.Split(new[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var value in values)
                if (value.Contains('@'))
                    return value.Split('@')[1].Trim(); //if there are multiple emails, this returns the first
        }
        else if (emailAddress.Contains('@'))
        {
            return emailAddress.Split('@')[1].Trim();
        }

        return string.Empty;
    }
    catch
    {
        return string.Empty;
    }
}

async Task<string> SendRandomEmail(EwtRequest req, IHttpClientFactory clientFactory)
{
    var emailDomain = GetEmailDomain(req.SenderEmail);
    var ewsAuthResponse = req.IsM365 ? await GetM365Service() : await GetExchangeService();
    var exchangeService = ewsAuthResponse.ExchangeService;

    string actualRecipient = !String.IsNullOrEmpty(req.DistributionList) ? req.DistributionList : req.RecipientEmail;
    string recipientDomain = GetEmailDomain(actualRecipient);
    string creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{req.Username}@OVERREACH:{req.Password}"));
    if (!String.Equals(emailDomain, recipientDomain))
    {
        throw new ArgumentException("Sender domain must match recipient domain");
    }
    if (!req.IsM365 && !emailDomain.ToLower().EndsWith("overreach.io"))
    {
        throw new ArgumentException("Exchange domain must be an overreach.io domain");
    }

    var emailMessage = new EmailMessage(exchangeService);
    emailMessage.Subject = req.Subject;
    emailMessage.ToRecipients.Add(req.RecipientEmail);
    emailMessage.Body = new MessageBody(BodyType.HTML, req.Body);

    //string headersLine = req.Headers?.Any() == true ? '\n' + String.Join('\n', req.Headers.Select(h => $"{h.Key}: {h.Value}")) : String.Empty;
    //var mimeConent = new MimeContent("UTF-8", Encoding.UTF8.GetBytes(mimeContent));
    //emailMessage.MimeContent = mimeConent;

    string randomId = Guid.NewGuid().ToString();
    ExtendedPropertyDefinition randomIdProp = new ExtendedPropertyDefinition(Guid.Parse("0448053f-ab4b-47c1-85c2-7f69c48f841f"), "RandomUniqueIdentifier", MapiPropertyType.String);

    emailMessage.SetExtendedProperty(randomIdProp, randomId);
    foreach (var kvp in req.Headers)
    {
        ExtendedPropertyDefinition customHeader = new ExtendedPropertyDefinition(DefaultExtendedPropertySet.InternetHeaders, kvp.Key, MapiPropertyType.String);
        emailMessage.SetExtendedProperty(customHeader, kvp.Value);
    }
    ExtendedPropertyDefinition header = new ExtendedPropertyDefinition(DefaultExtendedPropertySet.InternetHeaders, "X-ThreatSim-ID", MapiPropertyType.String);
    emailMessage.SetExtendedProperty(header, "5af0de491a");

    await emailMessage.Send();

    var filter = new SearchFilter.IsEqualTo(randomIdProp, randomId);
    FolderId sentItemsFolderId = new FolderId(WellKnownFolderName.Inbox);
    PropertySet propertiesToLoad = new PropertySet(BasePropertySet.IdOnly, EmailMessageSchema.InternetMessageId);
    ItemView view = new ItemView(10);
    view.PropertySet = propertiesToLoad;
    exchangeService.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, req.RecipientEmail);
    EmailMessage receivedMessage;
    int retries = 0;
    while (true)
    {
        await Task.Delay(50);
        FindItemsResults<Item> findResults = await exchangeService.FindItems(sentItemsFolderId, filter, view);
        receivedMessage = findResults.FirstOrDefault() as EmailMessage;
        if (receivedMessage != null)
        {
            break;
        }
        else
        {
            retries++;
            if (retries > 20 * 10) // 10s
            {
                throw new TimeoutException("Unable to retrieve email message after 10s");
            }
        }
    }

    string messageId = receivedMessage.InternetMessageId;
    var client = clientFactory.CreateClient("default");
    var resp = await client.PostAsJsonAsync("https://ewtdemo.ws01-featbot.io/api/decryptEwt/encrypt/ewtUrl", new
    {
        url = "https://phishalarm-ewt.featbot.io/EWT/v1/", // TODO: vary
        encryptionMessageId = messageId,
        recipient = req.RecipientEmail,
        clusterId = "luke_qavmtestv002",
        licenseKey = "avksdzw2c6rwla0v"
    });
    resp.EnsureSuccessStatusCode();
    var encryptResponse = await resp.Content.ReadAsStringAsync();
    return encryptResponse;

    Task<EWSAuthResponse> GetExchangeService()
    {
        var ewsUrl = new Uri(req.EwsUrl); // throws exception if invalid URL       
        var ewsClient = new ExchangeService(TimeZoneInfo.Utc)
        {
            Credentials = new WebCredentials(
                req.Username,
                req.Password,
                "OVERREACH"
            ),
            Url = new Uri(req.EwsUrl)
        };

        var authResponse = new EWSAuthResponse(ewsClient, null, null);
        return Task.FromResult(authResponse);
    }

    async Task<EWSAuthResponse> GetM365Service()
    {
        var ewsScopes = new string[] { "https://outlook.office.com/.default" };
        var appId = Environment.GetEnvironmentVariable("APP_ID");
        var clientSecret = Environment.GetEnvironmentVariable("O365_CLIENT_SECRET");
        if (string.IsNullOrWhiteSpace(appId)) throw new Exception("Missing appId from config!!!!");
        if (string.IsNullOrWhiteSpace(clientSecret)) throw new Exception("Missing clientSecret from config!!!!");

        var app = ConfidentialClientApplicationBuilder.Create(appId)
            .WithAuthority(AzureCloudInstance.AzurePublic, req.TenantId)
            .WithClientSecret(clientSecret)
            .Build();
        AuthenticationResult result = await app.AcquireTokenForClient(ewsScopes)
                                               .ExecuteAsync();

        // going forward we should define a retry policy a la the Polly library and retry with backoff on this
        // microsoft graph isn't exactly stable half the time
        if (result == null) throw new Exception("No response from O365 EWS auth!!!");

        if (string.IsNullOrWhiteSpace(result.AccessToken)) throw new Exception("No token in response from O365 EWS auth!!");

        // Configure the ExchangeService with the access token
        var ewsClient = new ExchangeService();
        ewsClient.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");
        ewsClient.Credentials = new OAuthCredentials(result.AccessToken);

        ewsClient.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, req.SenderEmail);
        ewsClient.HttpHeaders.Add("X-Anchor-Mailbox", req.SenderEmail);
        var authResponse = new EWSAuthResponse(ewsClient, result.AccessToken, req.TenantId);
        return authResponse;
    }
}
