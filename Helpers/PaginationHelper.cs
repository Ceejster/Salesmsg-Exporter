namespace SalesmessageExporter.Helpers
{
    public static class PaginationHelper
    {
        public static bool ShouldTriggerCheckpoint(int currentPage, int checkpointSize)
        {
            return currentPage > 0 &&
                   currentPage % checkpointSize == 0;
        }

        public static bool HasMorePages(int returnedRecordCount, int pageSize)
        {
            return returnedRecordCount == pageSize;
        }
    }
}