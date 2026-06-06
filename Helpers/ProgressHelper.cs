//using System.Text.Json;

//namespace SalesmessageExporter.Helpers
//{
//    public static class ProgressHelper
//    {
//        private static readonly string FilePath = "progress.json";

//        // Returns the last successfully completed offset
//        public static int GetLastCompletedOffset()
//        {
//            if (!File.Exists(FilePath))
//                return 0;

//            string json = File.ReadAllText(FilePath);

//            try
//            {
//                var data =
//                    JsonSerializer.Deserialize<ProgressData>(json);

//                return data?.LastCompletedOffset ?? 0;
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        // Saves current completed offset to disk
//        public static void SaveProgress(int lastCompletedOffset)
//        {
//            var data = new ProgressData
//            {
//                LastCompletedOffset = lastCompletedOffset
//            };

//            string json = JsonSerializer.Serialize(
//                data,
//                new JsonSerializerOptions
//                {
//                    WriteIndented = true
//                });

//            File.WriteAllText(FilePath, json);
//        }

//        private class ProgressData
//        {
//            public int LastCompletedOffset { get; set; }
//        }
//    }
//}