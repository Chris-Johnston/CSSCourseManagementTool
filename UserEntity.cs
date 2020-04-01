using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb
{
    public class UserEntity : TableEntity
    {

        // don't even need this at all, I think, we can get this from current user
        public string GuildId
        {
            get => this.PartitionKey;
            set => this.PartitionKey = value;
        }

        public string UserId
        {
            get => this.RowKey;
            set => this.RowKey = value;
        }

        [IgnoreProperty]
        public List<string> JoinCourseIds { get; set; }

        public string JoinCourseIdsJson
        {
            get => JsonConvert.SerializeObject(JoinCourseIds);
            set => JoinCourseIds = JsonConvert.DeserializeObject<List<string>>(value);
        }
    }
}
