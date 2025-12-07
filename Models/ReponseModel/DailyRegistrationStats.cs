using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.ReponseModel
{
    public class DailyRegistrationStats
    {
        public string Date { get; set; } // Ngày đăng ký (YYYY-MM-DD)
        public int Count { get; set; }   // Số lượng đăng ký
    }
}
