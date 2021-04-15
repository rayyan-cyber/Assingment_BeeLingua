using Assingment_BeeLingua.DAL.Models;
using Assingment_BeeLingua.DAL.Models.MediaService;
using Microsoft.Azure.Cosmos;
using Nexus.Base.CosmosDBRepository;
using System;

namespace Assingment_BeeLingua.DAL.Repository
{
    public class Repositories
    {
        private static readonly string _eventGridEndPoint = Environment.GetEnvironmentVariable("eventGridEndPoint");
        private static readonly string _eventGridKey = Environment.GetEnvironmentVariable("eventGridEndKey");

        public class LessonRepository : DocumentDBRepository<Lesson>
        {
            public LessonRepository(CosmosClient client) : base("Course", client, partitionProperties: "LessonCode",
                eventGridEndPoint: _eventGridEndPoint, eventGridKey: _eventGridKey)
            { }
        }

        public class NotificationLessonRepository : DocumentDBRepository<NotificationLesson>
        {
            public NotificationLessonRepository(CosmosClient client) : base("Course", client)
            { }
        }

        public class MediaServiceRepository : DocumentDBRepository<AssetAMS>
        {
            public MediaServiceRepository(CosmosClient client) : base("MigrationMedia", client, partitionProperties: "Subject",
                eventGridEndPoint: _eventGridEndPoint, eventGridKey: _eventGridKey)
            { }
        }
    }
}
