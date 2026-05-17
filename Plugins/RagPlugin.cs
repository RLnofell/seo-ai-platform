using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using AI_SEO_Ssas_Platform.Services;

namespace AI_SEO_Ssas_Platform.Plugins;

public class RagPlugin
{
    private readonly ILogCollector _logCollector;
    private readonly ISemanticTextMemory _memory;

    public RagPlugin(ILogCollector logCollector, ISemanticTextMemory memory)
    {
        _logCollector = logCollector;
        _memory = memory;
    }

    [KernelFunction("SearchInternalKnowledge")]
    [Description("Tìm kiếm thông tin nội bộ (như bảng giá, quy trình kỹ thuật đặc thù của khách hàng) từ cơ sở dữ liệu Vector Database (RAG).")]
    public async Task<string> SearchInternalKnowledgeAsync(
        [Description("Từ khóa hoặc câu hỏi cần tra cứu (VD: 'Bảng giá Thắng Hiền', 'Quy trình Đạt Phát')")] string query)
    {
        await _logCollector.AddLogAsync($"\n[RAG Plugin] Đang lục lọi trí nhớ với từ khóa: '{query}'...");

        if (_memory == null) return "Lỗi: Không thể kết nối đến Vector Database.";

        var results = _memory.SearchAsync("seo_knowledge", query, limit: 5, minRelevanceScore: 0.1);
        
        await foreach (var result in results)
        {
            if (result.Metadata.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                await _logCollector.AddLogAsync($"[RAG Plugin] Đã tìm thấy (Hybrid Match): {result.Metadata.Text}");
                return result.Metadata.Text;
            }
        }

        var firstResult = await _memory.SearchAsync("seo_knowledge", query, limit: 1).FirstOrDefaultAsync();
        if (firstResult != null)
        {
            await _logCollector.AddLogAsync($"[RAG Plugin] Đã tìm thấy (Vector Match): {firstResult.Metadata.Text}");
            return firstResult.Metadata.Text;
        }

        await _logCollector.AddLogAsync("[RAG Plugin] Không tìm thấy thông tin phù hợp.");
        return "Không tìm thấy thông tin phù hợp trong dữ liệu nội bộ.";
    }
}
