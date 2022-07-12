using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ApplicationInsights.Query;
using System.Threading.Tasks;

namespace WholesaleAlertsSLOs
{
    public class CreateAlertSLOCalc
    {   
        public async Task<int> QueryResults(string appID, string apiKey, string query, ILogger log)
        {
            var credentials = new ApiKeyClientCredentials(apiKey);
            var applicationInsightsClient = new ApplicationInsightsDataClient(credentials);
            var response = await applicationInsightsClient.Query.ExecuteWithHttpMessagesAsync(appID, query);
            var count = 0;

            if (response.Response.IsSuccessStatusCode)
            {
                var result = response.Body.Results;
                foreach (var item in result)
                {
                    count = count + 1;
                }
            }
            return count;
        }

        [FunctionName("CreateAlertSLOCalc")]
        public async Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var appID = //REDACTED SECRET;
            var apiKey = //REDACTED SECRET;
            var queryTotalResults = "requests | where cloud_RoleName contains 'dashboard' and name has 'SaveGasAlert' and timestamp > ago(5m)";
            var querySuccessResults = "requests | where cloud_RoleName contains 'dashboard' and name has 'SaveGasAlert' and resultCode ==302 and timestamp > ago(5m)";

            int successResult = await QueryResults(appID, apiKey, querySuccessResults, log);
            int totalResult = await QueryResults(appID, apiKey, queryTotalResults, log);

            double calculateSLO = Math.Round(((double)successResult / totalResult) * 100, 2);
            log.LogInformation("Current SLO: " + calculateSLO.ToString() + "%");    
        }
    }
}
