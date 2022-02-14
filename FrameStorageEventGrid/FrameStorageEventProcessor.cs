// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.Models;
using Azure.Security.KeyVault.Secrets;
using System;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace FrameStorageEventGrid
{
    /// <summary>
    /// 
    /// </summary>
    /// <see cref="https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventgrid/Azure.Messaging.EventGrid/samples/Sample3_ParseAndDeserializeEvents.md"/>
    public static class FrameStorageEventProcessor
    {
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

        [FunctionName("ProcessFrameStorageEvent")]
        public static void Run([EventGridTrigger]Azure.Messaging.EventGrid.EventGridEvent eventGridEvent, ILogger log)
        {
            // Determine if the event was a system event
            if (eventGridEvent.TryGetSystemEventData(out object systemEvent))
            {
                switch (systemEvent)
                {
                    case StorageBlobCreatedEventData blobCreatedEvent:
                        log.LogInformation("Blob Created:");
                        log.LogInformation(eventGridEvent.EventType);
                        log.LogInformation(eventGridEvent.Subject);
                        log.LogInformation(eventGridEvent.Topic);

                        log.LogInformation(blobCreatedEvent.Api);
                        log.LogInformation(blobCreatedEvent.ContentType);
                        log.LogInformation(blobCreatedEvent.Sequencer);
                        log.LogInformation(blobCreatedEvent.Url);

                        break;

                    case StorageBlobDeletedEventData blobDeletedEvent:
                        log.LogInformation("Blob Deleted:");
                        log.LogInformation(eventGridEvent.EventType);
                        log.LogInformation(eventGridEvent.Subject);
                        log.LogInformation(eventGridEvent.Topic);

                        log.LogInformation(blobDeletedEvent.Api);
                        log.LogInformation(blobDeletedEvent.ContentType);
                        log.LogInformation(blobDeletedEvent.Sequencer);
                        log.LogInformation(blobDeletedEvent.Url);
                        break;

                    default:
                        log.LogInformation("Other System Event:");
                        log.LogInformation(eventGridEvent.EventType);
                        log.LogInformation(eventGridEvent.Subject);
                        log.LogInformation(eventGridEvent.Topic);
                        log.LogInformation(eventGridEvent.Data.ToString());
                        break;
                }
            }
            else // The event was not a system event
            {
                // This is where custom user-created events would be handled
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
