using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSSCourseManagementWeb.Models
{
    public class RoleModel
    {
        public DiscordUserInfo CurrentUser { get; set; }

        public List<CourseEntity> JoinableCourses { get; set; } = new List<CourseEntity>();

        public List<CourseEntity> LeaveableCourses { get; set; } = new List<CourseEntity>();
    }
}
