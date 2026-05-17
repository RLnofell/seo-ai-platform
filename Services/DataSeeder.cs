using Microsoft.SemanticKernel.Memory;

namespace AI_SEO_Ssas_Platform.Services;

public static class DataSeeder
{
    public static async Task SeedAsync(ISemanticTextMemory memory)
    {
        Console.WriteLine("[*] Đang khởi tạo/kết nối Vector Database (SQLite)...");
        
        string kbPath = Path.Combine(Directory.GetCurrentDirectory(), "KnowledgeBase");
        if (!Directory.Exists(kbPath))
        {
            Directory.CreateDirectory(kbPath);
            Console.WriteLine("[!] Đã tạo thư mục KnowledgeBase. Hãy copy file .txt vào đây để AI học.");
            return;
        }

        var files = Directory.GetFiles(kbPath, "*.txt");
        if (files.Length == 0)
        {
            Console.WriteLine("[!] Thư mục KnowledgeBase trống. Bỏ qua nạp dữ liệu.");
            return;
        }

        Console.WriteLine($"[*] Tìm thấy {files.Length} tài liệu. Đang nạp vào Database...");
        
        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string content = await File.ReadAllTextAsync(file);
            
            await memory.SaveInformationAsync(
                collection: "seo_knowledge",
                id: fileName,
                text: content
            );
            Console.WriteLine($"  -> Đã nạp xong: {fileName}");
        }
        
        Console.WriteLine("[v] Hoàn tất nạp kiến thức!");
    }
}
