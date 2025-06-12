namespace CommunityService.Respones
{
    public class Pagination<T>
    {
        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public long Count { get; set; }

        public IReadOnlyList<T> Data { get; set; }

        public Pagination(int pageIndex, int pageSize, long count, IReadOnlyList<T> Data)
        {
            this.Data = Data;
            PageIndex = pageIndex;
            PageSize = pageSize;
            Count = count;
        }
    }
}
