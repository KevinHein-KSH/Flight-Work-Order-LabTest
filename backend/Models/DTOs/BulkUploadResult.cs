using System.Collections.Generic;

namespace LabTest.Models.DTOs
{
    public class BulkUploadResult
    {
        public int Count { get; set; }
        public List<string> Errors { get; set; } = new();

        public bool AnyInserted => Count > 0;
    }
}

