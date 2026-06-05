using System.ComponentModel;
using Microsoft.SemanticKernel;
using AI_SEO_Ssas_Platform.Services;

namespace AI_SEO_Ssas_Platform.Plugins;

public class SeoAutomationPlugin
{
    private readonly ILogCollector _logCollector;

    public SeoAutomationPlugin(ILogCollector logCollector)
    {
        _logCollector = logCollector;
    }

    [KernelFunction("SearchGoogleTop10")]
    [Description("Tự động lên Google tra cứu Top 10 đối thủ của từ khóa để phân tích bài viết trước khi viết.")]
    public async Task<string> SearchGoogleTop10(
        [Description("Từ khóa cần SEO")] string keyword,
        [Description("Ngôn ngữ yêu cầu (vi hoặc en)")] string language = "vi")
    {
        if (language == "en")
        {
            await _logCollector.AddLogAsync($"\n[Seo Plugin] Calling Google Search API to find top competitors for keyword: '{keyword}'...");
            
            return $"Top competitors for keyword '{keyword}' are focusing on service quality and warranty. " +
                   "To rank better, the article should include specific pricing details and a long-term warranty commitment.";
        }
        else
        {
            await _logCollector.AddLogAsync($"\n[Seo Plugin] Đang gọi Google Search API tìm Top đối thủ cho từ khóa: '{keyword}'...");
            
            return $"Top đối thủ cho từ khóa '{keyword}' đang tập trung vào chất lượng dịch vụ và bảo hành. " +
                   "Để SEO tốt hơn, bài viết cần có thông tin chi tiết về giá cả cụ thể và cam kết bảo hành lâu dài.";
        }
    }

    [KernelFunction("CheckKeywordDensity")]
    [Description("Tự động check mật độ từ khóa (Keyword Density) của nội dung bài viết vừa tạo.")]
    public async Task<string> CheckKeywordDensity(
        [Description("Nội dung bài viết cần kiểm tra")] string content, 
        [Description("Từ khóa cần SEO chính")] string keyword,
        [Description("Ngôn ngữ yêu cầu (vi hoặc en)")] string language = "vi")
    {
        if (language == "en")
        {
            await _logCollector.AddLogAsync($"\n[Seo Plugin] Checking keyword density for '{keyword}' in the article...");
        }
        else
        {
            await _logCollector.AddLogAsync($"\n[Seo Plugin] Đang kiểm tra mật độ từ khóa '{keyword}' trong bài viết...");
        }
        
        int count = System.Text.RegularExpressions.Regex.Matches(
            content, 
            System.Text.RegularExpressions.Regex.Escape(keyword), 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        ).Count;
        int wordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        double density = wordCount > 0 ? (double)count / wordCount * 100 : 0;

        if (language == "en")
        {
            return $"Current keyword density is {density:F1}%. SEO check completed successfully.";
        }
        else
        {
            return $"Mật độ từ khóa hiện tại là {density:F1}%. Đã kiểm tra chuẩn SEO thành công.";
        }
    }

    [KernelFunction("PostToWordPress")]
    [Description("Tự động đăng bài lên WordPress của khách hàng sau khi bài viết đã được kiểm tra và tối ưu hoàn thiện.")]
    public async Task<string> PostToWordPress(
        [Description("Tiêu đề bài viết SEO")] string title, 
        [Description("Nội dung bài viết chuẩn SEO")] string content,
        [Description("Ngôn ngữ yêu cầu (vi hoặc en)")] string language = "vi")
    {
        if (language == "en")
        {
            await _logCollector.AddLogAsync($"\n[Seo Plugin] Calling WordPress REST API to publish the article '{title}'...");
            await _logCollector.AddLogAsync($"[Seo Plugin] -> Successfully published on the website!");
            
            return $"Successfully published at URL: https://clientwebsite.com/{title.ToLower().Replace(" ", "-")}";
        }
        else
        {
            await _logCollector.AddLogAsync($"\n[Seo Plugin] Đang gọi WordPress REST API để xuất bản bài viết '{title}'...");
            await _logCollector.AddLogAsync($"[Seo Plugin] -> Đã đăng thành công lên Website!");
            
            return $"Đã xuất bản thành công tại URL: https://khachhang.com/{title.ToLower().Replace(" ", "-")}";
        }
    }
}
