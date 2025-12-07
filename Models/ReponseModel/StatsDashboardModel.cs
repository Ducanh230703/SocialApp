using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class StatsDashboardModel
    {
        /// <summary>
        /// Tổng số bài đăng trong khoảng ngày
        /// </summary>
        public int TotalPostsCount { get; set; }

        /// <summary>
        /// Số lượng user đăng ký theo ngày
        /// </summary>
        public List<DailyRegistrationStats> DailyRegistrations { get; set; } = new();

        /// <summary>
        /// Số lượng bài đăng theo ngày
        /// (Bạn chưa dùng nhưng model API có trả về)
        /// </summary>
        public List<DailyRegistrationStats> DailyPosts { get; set; } = new();
    }


}
