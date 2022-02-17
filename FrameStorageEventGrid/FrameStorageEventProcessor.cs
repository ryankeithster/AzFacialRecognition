// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace FrameStorageEventGrid
{
    public static class FrameStorageEventProcessor
    {
        [FunctionName("ProcessFrameStorageEvent")]
        public static void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation(eventGridEvent.EventType.ToString());
            log.LogInformation(eventGridEvent.Subject.ToString());
            log.LogInformation(eventGridEvent.Topic.ToString());

            switch(eventGridEvent.Data)
            {
                case StorageBlobCreatedEventData blobCreatedEventData:
                    log.LogInformation("Blob Created:");
                    log.LogInformation(blobCreatedEventData.Api);
                    log.LogInformation(blobCreatedEventData.ContentType);
                    log.LogInformation(blobCreatedEventData.Sequencer);
                    log.LogInformation(blobCreatedEventData.Url);
                    break;

                case StorageBlobDeletedEventData blobDeletedEventData:
                    log.LogInformation("Blob Deleted:");
                    log.LogInformation(blobDeletedEventData.Api);
                    log.LogInformation(blobDeletedEventData.ContentType);
                    log.LogInformation(blobDeletedEventData.Sequencer);
                    log.LogInformation(blobDeletedEventData.Url);
                    break;

                default:
                    log.LogInformation("Other Event:");
                    log.LogInformation(eventGridEvent.Data.ToString());
                    break;
            }
        }
    }
}
