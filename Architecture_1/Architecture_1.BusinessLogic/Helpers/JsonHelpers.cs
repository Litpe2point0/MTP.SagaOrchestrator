using System.Collections.Generic;
using System.Text.Json;


namespace Architecture_1.BusinessLogic.Helpers
{
    public class JsonService
    {
        public string ProcessJson(string rawJson)
        {
            // Parse JSON thành Dictionary để có thể thao tác dễ dàng
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(rawJson);

            // Thêm một key mới vào JSON
            data["processed_at"] = System.DateTime.UtcNow.ToString("o");

            // Trả JSON đã được xử lý
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
