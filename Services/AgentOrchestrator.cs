using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using AI_SEO_Ssas_Platform.Services;

namespace AI_SEO_Ssas_Platform.Services;

public record RunResponse(List<string> Logs, string FinalArticle, string DensityResult, string PostResult, string MetaAndSchema = "", string SeoAudit = "", string GoogleIndexingResult = "");

public interface IAgentOrchestrator
{
    Task<RunResponse> RunAgentAsync(string input, string connectionId, string language = "vi");
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

    public async Task<RunResponse> RunAgentAsync(string input, string connectionId, string language = "vi")
    {
        _logCollector.Initialize(connectionId);
        
        bool isEn = language.Equals("en", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(input)) 
            throw new ArgumentException(isEn ? "Input is invalid" : "Input không hợp lệ");

        if (isEn)
        {
            await _logCollector.AddLogAsync($"[AI Planner] Analyzing request: '{input}'...");
            await _logCollector.AddLogAsync("[AI Planner] Automatically planning and activating required tools...");
        }
        else
        {
            await _logCollector.AddLogAsync($"[AI Planner] Đang phân tích yêu cầu: '{input}'...");
            await _logCollector.AddLogAsync("[AI Planner] Tự động lập kế hoạch và kích hoạt các công cụ cần thiết...");
        }

#pragma warning disable SKEXP0070
        OpenAIPromptExecutionSettings settings = new() 
        { 
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
        };
#pragma warning restore SKEXP0070

        string prompt = isEn ? $"""
                         Your task is: "{input}".
                         You are an AI SEO Agent. You have been provided with tools.
                         Please reason automatically and use the tools to retrieve internal information (pricing, process) and write a short, search-optimized SEO post.
                         Do not output JSON code blocks.
                         """ : $"""
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
        string seoAuditText = "";
        string metaSchemaText = "";

        // Tự động nhận diện Entity (Thay thế hardcode Thắng Hiền/Đạt Phát)
        string keyword = await ExtractKeywordAsync(input, responseText);

        if (responseText.Contains("\"name\"") || responseText.Contains("Plugin") || responseText.Contains("plugin"))
        {
            if (isEn)
            {
                await _logCollector.AddLogAsync("\n[AI Planner] AI decided to execute the toolchain (Agentic Pipeline).");
                await _logCollector.AddLogAsync($"\n[Agentic Pipeline] Step 1: Retrieving internal knowledge for: '{keyword}'");
            }
            else
            {
                await _logCollector.AddLogAsync("\n[AI Planner] AI đã ra quyết định thực thi chuỗi công cụ (Agentic Pipeline).");
                await _logCollector.AddLogAsync($"\n[Agentic Pipeline] Bước 1: Tra cứu kiến thức nội bộ cho: '{keyword}'");
            }
            
            var ragPlugin = _kernel.Plugins["RagPlugin"];
            var ragResult = await _kernel.InvokeAsync(ragPlugin["SearchInternalKnowledge"], new() { ["query"] = keyword });
            
            if (isEn)
            {
                await _logCollector.AddLogAsync($"[Agentic Pipeline] Step 2: Analyzing Google competitors for: '{keyword}'");
            }
            else
            {
                await _logCollector.AddLogAsync($"[Agentic Pipeline] Bước 2: Phân tích đối thủ Google cho: '{keyword}'");
            }
            
            var seoPlugin = _kernel.Plugins["SeoAutomationPlugin"];
            var googleResult = await _kernel.InvokeAsync(seoPlugin["SearchGoogleTop10"], new() { ["keyword"] = keyword, ["language"] = language });
            
            if (isEn)
            {
                await _logCollector.AddLogAsync("\n[Agentic Pipeline] Step 3: Generating optimized SEO content...");
            }
            else
            {
                await _logCollector.AddLogAsync("\n[Agentic Pipeline] Bước 3: Sáng tạo nội dung tối ưu SEO...");
            }
            
            string finalPrompt = isEn ? $@"You are an AI SEO Specialist.
Task: Write a highly engaging, structured, search-optimized SEO post with clear bullet points for the request: '{input}'.

INPUT DATA:
1. Internal Data: {ragResult}
2. Market Data: {googleResult}

Requirements:
- Write the article directly in natural English.
- Seamlessly integrate pricing/warranty info from the internal data.
- The article should be about 200-300 words long."
            : $@"Bạn là một Chuyên gia SEO AI.
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
            
            if (isEn)
            {
                await _logCollector.AddLogAsync("\n[Agentic Pipeline] Step 4: Checking keyword density & running SEO audit...");
            }
            else
            {
                await _logCollector.AddLogAsync("\n[Agentic Pipeline] Bước 4: Kiểm tra mật độ từ khóa & phân tích điểm số SEO...");
            }
            
            var densityResult = await _kernel.InvokeAsync(seoPlugin["CheckKeywordDensity"], new() { ["content"] = finalArticle, ["keyword"] = keyword, ["language"] = language });
            densityResultText = densityResult.ToString() ?? "";

            // Calculate density double for scoring
            int count = System.Text.RegularExpressions.Regex.Matches(
                finalArticle, 
                System.Text.RegularExpressions.Regex.Escape(keyword), 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            ).Count;
            int wordCount = finalArticle.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            double density = wordCount > 0 ? (double)count / wordCount * 100 : 0;

            var seoAuditResult = await _kernel.InvokeAsync(seoPlugin["AnalyzeSeoScore"], new() 
            { 
                ["title"] = isEn ? $"Professional {keyword} Solutions" : $"Giải pháp {keyword} chuyên nghiệp", 
                ["content"] = finalArticle, 
                ["keyword"] = keyword, 
                ["density"] = density, 
                ["language"] = language 
            });
            seoAuditText = seoAuditResult.ToString() ?? "";

            var metaSchemaResult = await _kernel.InvokeAsync(seoPlugin["GenerateMetaTagsAndSchema"], new() 
            { 
                ["kernel"] = _kernel,
                ["title"] = isEn ? $"Professional {keyword} Solutions" : $"Giải pháp {keyword} chuyên nghiệp", 
                ["content"] = finalArticle, 
                ["keyword"] = keyword, 
                ["language"] = language 
            });
            metaSchemaText = metaSchemaResult.ToString() ?? "";

            if (isEn)
            {
                await _logCollector.AddLogAsync("\n[Agentic Pipeline] Step 5: Publishing to WordPress site...");
            }
            else
            {
                await _logCollector.AddLogAsync("\n[Agentic Pipeline] Bước 5: Xuất bản lên hệ thống WordPress...");
            }
            
            string title = isEn ? $"Professional {keyword} Solutions" : $"Giải pháp {keyword} chuyên nghiệp";
            var postResult = await _kernel.InvokeAsync(seoPlugin["PostToWordPress"], new() { ["title"] = title, ["content"] = finalArticle, ["language"] = language });
            postResultText = postResult.ToString() ?? "";

            string googleIndexingResultText = "";
            var urlMatch = System.Text.RegularExpressions.Regex.Match(postResultText, @"https?://[^\s]+");
            if (urlMatch.Success)
            {
                string url = urlMatch.Value;
                if (isEn)
                {
                    await _logCollector.AddLogAsync("\n[Agentic Pipeline] Step 6: Submitting to Google Indexing API...");
                }
                else
                {
                    await _logCollector.AddLogAsync("\n[Agentic Pipeline] Bước 6: Gửi yêu cầu lập chỉ mục tới Google Indexing API...");
                }
                var indexingResult = await _kernel.InvokeAsync(seoPlugin["SubmitToGoogleIndexing"], new() { ["url"] = url, ["language"] = language });
                googleIndexingResultText = indexingResult.ToString() ?? "";
            }
            
            if (isEn)
            {
                await _logCollector.AddLogAsync("\n[COMPLETED] Agent has successfully finished the SEO workflow.");
            }
            else
            {
                await _logCollector.AddLogAsync("\n[HOÀN TẤT] Agent đã hoàn thành toàn bộ quy trình SEO.");
            }

            return new RunResponse(_logCollector.GetLogs(), finalArticle, densityResultText, postResultText, metaSchemaText, seoAuditText, googleIndexingResultText);
        }
        else
        {
            if (isEn)
            {
                await _logCollector.AddLogAsync("\n[REPORT] AI Agent responded directly:");
            }
            else
            {
                await _logCollector.AddLogAsync("\n[BÁO CÁO] AI Agent phản hồi trực tiếp:");
            }
            await _logCollector.AddLogAsync(responseText);
            finalArticle = responseText;
        }

        return new RunResponse(_logCollector.GetLogs(), finalArticle, densityResultText, postResultText, "", "", "");
    }

    private async Task<string> ExtractKeywordAsync(string input, string aiResponse)
    {
        if (input.Contains("Thắng Hiền", StringComparison.OrdinalIgnoreCase)) return "Thắng Hiền";
        if (input.Contains("Đạt Phát", StringComparison.OrdinalIgnoreCase)) return "Đạt Phát";
        
        var extractPrompt = $"Extract the main service or brand keyword (1-3 words) from the following sentence: \"{input}\". Return ONLY the keyword, with no extra text, explanations, or quotes.";
        var result = await _kernel.InvokePromptAsync(extractPrompt);
        return result.ToString().Trim().TrimEnd('.').Replace("\"", "");
    }
}
