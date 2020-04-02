using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb
{
    public class StorageUtil
    {
        const string CourseTableName = "courses";
        const string UserTableName = "users";

        private readonly CloudStorageAccount storageAccount;
        public StorageUtil(string connectionString)
        {
            storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        private CloudTableClient GetTableClient()
        {
            return storageAccount.CreateCloudTableClient();
        }

        public async Task<CloudTable> GetCourseTableAsync()
        {
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(CourseTableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<CloudTable> GetUserTableAsync()
        {
            var tableClient = GetTableClient();
            var table = tableClient.GetTableReference(UserTableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        public async Task<List<CourseEntity>> GetCoursesAsync(ulong guildId)
        {
            var table = await GetCourseTableAsync();

            var query = new TableQuery()
            {
                FilterString = TableQuery.GenerateFilterCondition(nameof(CourseEntity.PartitionKey), QueryComparisons.Equal, guildId.ToString()),
            };

            return await SegmentedQueryHelperAsync<CourseEntity>(table, query, entity =>
            {
                return new CourseEntity()
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    ChannelId = entity.Properties[nameof(CourseEntity.ChannelId)].StringValue,
                    RoleId = entity.Properties[nameof(CourseEntity.RoleId)].StringValue,
                };
            });
        }

        public async Task<CourseEntity> GetCourseAsync(ulong guildId, string courseId)
        {
            var table = await GetCourseTableAsync();

            var query = new TableQuery()
            {
                FilterString = TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition(nameof(CourseEntity.PartitionKey), QueryComparisons.Equal, guildId.ToString()),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(nameof(CourseEntity.RowKey), QueryComparisons.Equal, courseId)),
            };

            var results = await SegmentedQueryHelperAsync<CourseEntity>(table, query, entity =>
            {
                return new CourseEntity()
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    ChannelId = entity.Properties[nameof(CourseEntity.ChannelId)].StringValue,
                    RoleId = entity.Properties[nameof(CourseEntity.RoleId)].StringValue,
                };
            });
            return results.FirstOrDefault();
        }

        private async Task<List<T1>> SegmentedQueryHelperAsync<T1>(CloudTable table, TableQuery query, Func<DynamicTableEntity, T1> conversionFunc)
            where T1 : TableEntity
        {
            var allResults = new List<T1>();

            TableContinuationToken continuationToken = null;
            do
            {
                var partialResults = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = partialResults.ContinuationToken;

                foreach (var result in partialResults.Results)
                {
                    var converted = conversionFunc(result);
                    allResults.Add(converted);
                }
            }
            while (continuationToken != null);

            return allResults;
        }

    }
}
