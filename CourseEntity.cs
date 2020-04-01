using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb
{
    public class CourseEntity : TableEntity
    {
        public string GuildId
        {
            get => this.PartitionKey;
            set => this.PartitionKey = value;
        }

        public string CourseId
        {
            get => this.RowKey;
            set => this.RowKey = value;
        }

        // doesn't support ulong yay
        public string ChannelId { get; set; }

        public string RoleId { get; set; }
    }
}
