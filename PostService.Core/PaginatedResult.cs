using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostService.Core
{
    public class PaginatedResult<T>
    {
        /// <summary>
        /// The items for the current page.
        /// </summary>
        public List<T> Items { get; set; }

        /// <summary>
        /// The total number of items across all pages.
        /// </summary>
        public long TotalCount { get; set; }

        public PaginatedResult(List<T> items, long totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }
    }
}
