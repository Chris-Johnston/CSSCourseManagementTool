using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Models
{
    public class Role
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
    }

    public class RoleListModel
    {
        // these are the roles that the user can join
        public List<Role> JoinRoles { get; set; } = new List<Role>();

        // these are the roles that the user can leave
        public List<Role> LeaveRoles { get; set; } = new List<Role>();
    }
}
