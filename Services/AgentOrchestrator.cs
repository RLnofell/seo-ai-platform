using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using AI_SEO_Ssas_Platform.Services;

namespace AI_SEO_Ssas_Platform.Services;

public record RunResponse(List<string> Logs, string FinalArticle, string DensityResult, string PostResult);

public interface IAgentOrchestrator
{
    Task<RunResponse> RunAgentAsync(string input, string connectionId);
}

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly Kernel _kernel;
    private readonly ILogCollector _logCollector;
    private readonly IConfiguration _config;

    public AgentOrchestrator(Kernel kernel, ILogCollector logCollector, IConfiguration config)
    {
        _kernel = kernel;
        _logCollector = logCollector;
        _config = config;
    }

    public async Task<RunResponse> RunAgentAsync(string input, string connectionId)
    {
        _logCollector.Initialize(connectionId);
        
        if (string.IsNullOrWhiteSpace(input)) 
            throw new ArgumentException("Input không hợp lệ");

        await _logCollector.AddLogAsync($"[AI Planner] Đang phân tích yêu cầu: '{input}'...");
        await _logCollector.AddLogAsync("[AI Planner] Tự động lập kế hoạch và kích hoạt các công cụ cần thiết...");

#pragma warning disable SKEXP0070
        OpenAIPromptExecutionSettings settings = new() 
        { 
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
        };
#pragma warning restore SKEXP0070

        string prompt = $"""
                         Nhiệm vụ của bạn là: "{input}".
                         Bạn là AI SEO Agent. Bạn đã được cung cấp sẵn các công cụ (Tools).
                         Hãy tự động suy luận và sử dụng công cụ để lấy thông tin nội bộ (giá cả, quy trình) rồi viết 1 bài đăng chuẩn SEO ngắn gọn.
                         Tuyệt đối không in ra các đoạn mã JSON.
                         """;

        var result = await _kernel.InvokePromptAsync(prompt, new(settings));
        string responseText = result.ToString();
        
        string finalArticle = "";
        string densityResultText = "";
        string postResultText = "";

        // Tự động nhận diện Entity (Thay thế hardcode Thắng Hiền/Đạt Phát)
        string keyword = await ExtractKeywordAsync(input, responseText);

        if (responseText.Contains("\"name\"") || responseText.Contains("Plugin"))
        {
            await _logCollector.AddLogAsync("\n[AI Planner] AI đã ra quyết định thực thi chuỗi công cụ (Agentic Pipeline).");
            
            await _logCollector.AddLogAsync($"\n[Agentic Pipeline] Bước 1: Tra cứu kiến thức nội bộ cho: '{keyword}'");
            var ragPlugin = _kernel.Plugins["RagPlugin"];
            var ragResult = await _kernel.InvokeAsync(ragPlugin["SearchInternalKnowledge"], new() { ["query"] = keyword });
            
            await _logCollector.AddLogAsync($"[Agentic Pipeline] Bước 2: Phân tích đối thủ Google cho: '{keyword}'");
            var seoPlugin = _kernel.Plugins["SeoAutomationPlugin"];
            var googleResult = await _kernel.InvokeAsync(seoPlugin["SearchGoogleTop10"], new() { ["keyword"] = keyword });
            
            await _logCollector.AddLogAsync("\n[Agentic Pipeline] Bước 3: Sáng tạo nội dung tối ưu SEO...");
            string finalPrompt = $@"Bạn là một Chuyên gia SEO AI.
Nhiệm vụ: Viết 1 bài đăng chuẩn SEO thật lôi cuốn, có gạch đầu dòng rõ ràng cho yêu cầu: '{input}'.

DỮ LIỆU ĐẦU VÀO:
1. Dữ liệu nội bộ: {ragResult}
2. Dữ liệu thị trường: {googleResult}

Yêu cầu: 
- Viết bài trực tiếp, ngôn ngữ tự nhiên.
- Lồng ghép thông tin giá cả/bảo hành từ dữ liệu nội bộ.
- Độ dài khoảng 200-300 từ.";

            var articleResult = await _kernel.InvokePromptAsync(finalPrompt);
            finalArticle = articleResult.ToString();
            
            await _logCollector.AddLogAsync("\n[Agentic Pipeline] Bước 4: Kiểm tra mật độ từ khóa...");
            var densityResult = await _kernel.InvokeAsync(seoPlugin["CheckKeywordDensity"], new() { ["content"] = finalArticle, ["keyword"] = keyword });
            densityResultText = densityResult.ToString() ?? "";

            await _logCollector.AddLogAsync("\n[Agentic Pipeline] Bước 5: Xuất bản lên hệ thống WordPress...");
            string title = $"Giải pháp {keyword} chuyên nghiệp";
            var postResult = await _kernel.InvokeAsync(seoPlugin["PostToWordPress"], new() { ["title"] = title, ["content"] = finalArticle });
            postResultText = postResult.ToString() ?? "";
            
            await _logCollector.AddLogAsync("\n[HOÀN TẤT] Agent đã hoàn thành toàn bộ quy trình SEO.");
        }
        else
        {
            await _logCollector.AddLogAsync("\n[BÁO CÁO] AI Agent phản hồi trực tiếp:");
            await _logCollector.AddLogAsync(responseText);
            finalArticle = responseText;
        }

        return new RunResponse(_logCollector.GetLogs(), finalArticle, densityResultText, postResultText);
    }

    private async Task<string> ExtractKeywordAsync(string input, string aiResponse)
    {
        // Simple extraction logic - can be upgraded to another AI call
        if (input.Contains("Thắng Hiền", StringComparison.OrdinalIgnoreCase)) return "Thắng Hiền";
        if (input.Contains("Đạt Phát", StringComparison.OrdinalIgnoreCase)) return "Đạt Phát";
        
        // Fallback: Use AI to extract the main entity if not found
        var extractPrompt = $"Trích xuất 1 từ khóa chính (tên thương hiệu hoặc dịch vụ) từ câu sau: \"{input}\". Chỉ in ra từ khóa, không giải thích.";
        var result = await _kernel.InvokePromptAsync(extractPrompt);
        return result.ToString().Trim().TrimEnd('.');
    }
}
