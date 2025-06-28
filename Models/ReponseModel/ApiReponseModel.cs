    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Models.ReponseModel
    {
        public class ApiReponseModel<T>
        {
            public int Status { get; set; }
            public string Mess { get; set; }
            public T? Data { get; set; }
        }
    }
