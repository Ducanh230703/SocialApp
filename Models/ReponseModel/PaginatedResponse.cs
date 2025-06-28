    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Models.ReponseModel
    {
        public class PaginatedResponse<T>
        {
            public List<T> Data { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public bool HasNextPage => (PageNumber * PageSize) < TotalCount;
            public bool HasPreviousPage => PageNumber > 1;
        }
    }
