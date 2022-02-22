// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Security.KeyVault.Secrets;
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System.Text;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;

namespace FrameStorageEventGrid
{
    public static class FrameStorageEventProcessor
    {
        /// <summary>
        /// Retrieve the specified secret value from the Azure Key Vault at the specified location.
        /// If the webapp is deployed in Azure, access to the secret should be managed using a Managed Identity.
        /// For more information see: https://docs.microsoft.com/en-us/azure/key-vault/general/tutorial-net-create-vault-azure-web-app
        /// and: https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-6.0
        /// </summary>
        /// <param name="KeyVaultUri"></param>
        /// <param name="SecretName"></param>
        /// <returns></returns>
        public static string GetSecretValueWithManagedIdentity(string KeyVaultUri, string SecretName)
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                 }
            };
            var client = new SecretClient(new Uri(KeyVaultUri), new DefaultAzureCredential(true), options);

            KeyVaultSecret secret = client.GetSecret(SecretName);

            return secret.Value;
        }

        public static async Task<ImageAnalysis> AnalyzeImage(string ImageUrl)
        {
            // Retrieve information for accessing computer vision endpoint
            string compVisSubscriptionKey = GetSecretValueWithManagedIdentity(
                System.Environment.GetEnvironmentVariable("KeyVaultUri"), 
                "ryan42-compvis-subscription-key");

            string compVisEndpoint = System.Environment.GetEnvironmentVariable("CompVisEndpoint");

            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(compVisSubscriptionKey))
              { Endpoint = compVisEndpoint };

            // Define the features to be extracted from the image (face only)
            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            { VisualFeatureTypes.Faces };

            // Analyze the image 
            ImageAnalysis results = await client.AnalyzeImageAsync(ImageUrl, visualFeatures: features);
            return results;
        }

        [FunctionName("ProcessFrameStorageEvent")]
        public static async Task Run([EventGridTrigger]Microsoft.Azure.EventGrid.Models.EventGridEvent eventGridEvent, ILogger log)
        {
            // The Microsoft.Azure.EventGrid.Models.EventGridEvent class is deprecated and being replaced by
            // this class: Azure.Messaging.EventGrid.EventGrid.
            //
            // The new class doesn't appear to have been completely integrated with EventGrid yet however;
            // attempting to use the new class as an argument to the Run procedure instead results in an exception
            // since there is no default constructor available for it.
            //
            // Until this integration has been completed, we will have to serialize instances of the new class
            // from instances of the deprecated class.
            var azEventGridEvent = new Azure.Messaging.EventGrid.EventGridEvent(
                eventGridEvent.Subject, 
                eventGridEvent.EventType, 
                eventGridEvent.DataVersion, 
                BinaryData.FromString(eventGridEvent.Data.ToString()));

            // Determine if the event was a standard system event
            if (azEventGridEvent.TryGetSystemEventData(out object systemEvent))
            {
                switch (systemEvent)
                {
                    case Azure.Messaging.EventGrid.SystemEvents.StorageBlobCreatedEventData blobCreatedEventData:
                        log.LogInformation("Blob Created:");
                        log.LogInformation(blobCreatedEventData.Api);
                        log.LogInformation(blobCreatedEventData.ContentType);
                        log.LogInformation(blobCreatedEventData.Sequencer);
                        log.LogInformation(blobCreatedEventData.Url);

                        Task<ImageAnalysis> imageAnalyzing = AnalyzeImage(blobCreatedEventData.Url);
                        ImageAnalysis results = await imageAnalyzing;

                        if (results.Faces is not null)
                        {
                            foreach (var face in results.Faces)
                            {
                                log.LogInformation($"A {face.Gender} of age {face.Age} at location {face.FaceRectangle.Left}, " +
                                  $"{face.FaceRectangle.Left}, {face.FaceRectangle.Top + face.FaceRectangle.Width}, " +
                                  $"{face.FaceRectangle.Top + face.FaceRectangle.Height}");
                            }
                        }
                        break;

                    case Azure.Messaging.EventGrid.SystemEvents.StorageBlobDeletedEventData blobDeletedEventData:
                        log.LogInformation("Blob Deleted:");
                        log.LogInformation(blobDeletedEventData.Api);
                        log.LogInformation(blobDeletedEventData.ContentType);
                        log.LogInformation(blobDeletedEventData.Sequencer);
                        log.LogInformation(blobDeletedEventData.Url);
                        break;

                    default:
                        log.LogInformation("Other Event:");
                        log.LogInformation(azEventGridEvent.EventType);
                        log.LogInformation(systemEvent.GetType().FullName);
                        break;
                }
            }
            else // The event was not a system event
            {
                // This is where custom user events would be processed
                switch (eventGridEvent.EventType)
                {
                    default:
                        log.LogInformation("Other Event:");
                        log.LogInformation(eventGridEvent.EventType);
                        log.LogInformation(eventGridEvent.Data.ToString());
                        break;
                }
            }
        }
    }
}
