namespace ShuttleMate.Core.Bases
{
    public class BasePaginatedList<T>
    {
        public IReadOnlyCollection<T> Items { get; private set; }

        public int TotalItems { get; private set; }

        public int CurrentPage { get; private set; }

        public int TotalPages { get; private set; }

        public int PageSize { get; private set; }

        public BasePaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
        {
            TotalItems = count;
            CurrentPage = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            Items = items;
        }

        public bool HasPreviousPage => CurrentPage > 0;

        public bool HasNextPage => CurrentPage + 1 < TotalPages;

    }
}