using System.Text;
using FURPMS.Application.Interfaces;
using FURPMS.Infrastructure.Services;

namespace FURPMS.Tests.Proposals;

public class ProposalExtractionServiceTests
{
    [Fact]
    public async Task ExtractAsync_JsonHopLe_MapDuCacTruongNoiDung()
    {
        var gemini = new FakeGeminiService
        {
            Response = """
                ```json
                {
                  "titleVi": "Đề tài thử nghiệm",
                  "titleEn": "Test proposal",
                  "abstractVi": "Tóm tắt",
                  "researchObjectives": "Mục tiêu",
                  "methodology": "Phương pháp",
                  "expectedOutput": "Sản phẩm",
                  "urgency": "Cấp thiết",
                  "novelty": "Tính mới",
                  "applicationPotential": "Ứng dụng",
                  "transferPotential": "Chuyển giao",
                  "facilities": "Phòng lab",
                  "durationMonths": 18,
                  "totalBudget": 100000000
                }
                ```
                """
        };
        var service = new ProposalExtractionService(gemini);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Nội dung đề cương"));

        var result = await service.ExtractAsync(stream, "proposal.txt", "text/plain");

        Assert.Equal("Đề tài thử nghiệm", result.TitleVi);
        Assert.Equal("Test proposal", result.TitleEn);
        Assert.Equal("Cấp thiết", result.Urgency);
        Assert.Equal("Tính mới", result.Novelty);
        Assert.Equal("Ứng dụng", result.ApplicationPotential);
        Assert.Equal("Chuyển giao", result.TransferPotential);
        Assert.Equal("Phòng lab", result.Facilities);
        Assert.Equal(18, result.DurationMonths);
        Assert.Equal(100_000_000m, result.TotalBudget);
        Assert.Null(result.Warning);
    }

    [Fact]
    public async Task ExtractAsync_ChuaCauHinhAi_TraCanhBaoDeNhapTay()
    {
        var service = new ProposalExtractionService(new FakeGeminiService { IsConfigured = false });
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Nội dung"));

        var result = await service.ExtractAsync(stream, "proposal.txt", "text/plain");

        Assert.NotNull(result.Warning);
        Assert.Contains("chưa được cấu hình", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_AiTraSaiJson_TraCanhBaoDeNhapTay()
    {
        var service = new ProposalExtractionService(new FakeGeminiService { Response = "không phải JSON" });
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Nội dung"));

        var result = await service.ExtractAsync(stream, "proposal.txt", "text/plain");

        Assert.NotNull(result.Warning);
        Assert.Contains("không đúng định dạng", result.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractAsync_DinhDangKhongDocDuoc_NemLoiRoRang()
    {
        var service = new ProposalExtractionService(new FakeGeminiService());
        await using var stream = new MemoryStream([1, 2, 3]);

        var error = await Assert.ThrowsAsync<ArgumentException>(
            () => service.ExtractAsync(stream, "proposal.doc", "application/msword"));

        Assert.Contains("Chỉ hỗ trợ PDF, DOCX, TXT", error.Message);
    }

    private sealed class FakeGeminiService : IGeminiService
    {
        public bool IsConfigured { get; init; } = true;
        public string Response { get; init; } = "{}";

        public Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default) =>
            Task.FromResult(Response);

        public Task<string> GenerateFromInlineDataAsync(
            byte[] data,
            string mimeType,
            string prompt,
            CancellationToken ct = default) => Task.FromResult(Response);
    }
}
