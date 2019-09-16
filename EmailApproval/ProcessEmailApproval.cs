using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.IdentityModel;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading;

namespace EmailApproval
{
    public static class ProcessEmailApproval
    {


        private const string KTenantString = "monojitdattaoutlook.onmicrosoft.com";
        private const string KAuthorityString = "https://login.microsoftonline.com/{0}";        
        // private const string KRedirectUriString = "https://socialpost-apim.portal.azure-api.net/docs/services/mphapimoauth2/console/oauth2/authorizationcode/callback";        

        public static async Task<AuthenticationResult> AuthenticateCommentsAsync()
        {

            var clientIDString = Environment.GetEnvironmentVariable("Client_ID");
            var clientSecretString = Environment.GetEnvironmentVariable("Client_Secret");
            var resourceIDString = Environment.GetEnvironmentVariable("Resource_ID");

            var authorityString = string.Format(KAuthorityString, KTenantString);
            var authContext = new AuthenticationContext(authorityString);
            var creds = new ClientCredential(clientIDString, clientSecretString);
            var token = await authContext.AcquireTokenAsync(resourceIDString, creds);
            Console.WriteLine($"token:{token}");
            return token;

        }

        public static async Task AskForCommentsAsync()
        {

            var handler = new HttpClientHandler()
            {

                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
                {
                    return true;
                }

            };

            using (var cl = new HttpClient(handler))
            {

                cl.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                                              Environment.GetEnvironmentVariable("API_KEY"));
                cl.DefaultRequestHeaders.Add("Ocp-Apim-Trace", "true");
                
                try
                {


                    var httpResponse = await cl.GetAsync(Environment.GetEnvironmentVariable("API_URL"));
                    if ((httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                        || (httpResponse.StatusCode == HttpStatusCode.Forbidden))
                    {

                        var token = await AuthenticateCommentsAsync();
                        cl.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");
                        httpResponse = await cl.GetAsync(Environment.GetEnvironmentVariable("API_URL"));

                    }

                    var resp = await httpResponse?.Content?.ReadAsStringAsync();
                    Console.WriteLine($"resp:{resp}");

                }
                catch (Exception ex)
                {

                    Console.WriteLine($"ex:{ex.Message}");                    

                }

            }

        }


        [FunctionName("ProcessEmailApproval")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequestMessage requestMessage, ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            var option = await requestMessage.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<OptionModel>(option);
            Console.WriteLine(obj.Option);

            bool res = (obj.Option == "Approve");

            if (res == true)
                // await AuthenticateCommentsAsync();
                await AskForCommentsAsync();

            return (res == true) ? (ActionResult)(new OkObjectResult($"OK"))
                                 : (ActionResult)(new BadRequestObjectResult($"Error"));
        }
    }   
}
