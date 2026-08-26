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
                  "totalBudget": "100.000.000 đ",
                  "budgetItems": [
                    { "category": "LABOR", "amount": "60,000,000 VND" },
                    { "category": "EQUIPMENT", "amount": 40000000 }
                  ],
                  "teamMembers": [
                    {
                      "fullName": "Trần Thị Bình",
                      "email": "binh@example.edu.vn",
                      "department": "SE",
                      "academicTitle": "ThS.",
                      "role": "Thành viên chính",
                      "workMonths": 6,
                      "isSecretary": true
                    }
                  ]
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
        Assert.Equal(2, result.BudgetItems.Count);
        Assert.Equal(60_000_000m, result.BudgetItems.Single(x => x.Category == "LABOR").Amount);
        var member = Assert.Single(result.TeamMembers);
        Assert.Equal("Trần Thị Bình", member.FullName);
        Assert.Equal("binh@example.edu.vn", member.Email);
        Assert.True(member.IsSecretary);
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

    [Fact]
    public async Task ExtractAsync_LoiProvider_KhongTraThongBaoKyThuatRaGiaoDien()
    {
        var service = new ProposalExtractionService(new FakeGeminiService
        {
            Error = new InvalidOperationException("Gemini API lỗi 404: models/gemini-old is unavailable")
        });
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Nội dung"));

        var result = await service.ExtractAsync(stream, "proposal.txt", "text/plain");

        Assert.Contains("Mô hình AI", result.Warning);
        Assert.DoesNotContain("gemini-old", result.Warning);
        Assert.DoesNotContain("404", result.Warning);
    }

    [Fact]
    public void CleanText_BoKyTuRac_GopKhoangTrang_VaGioiHanContext()
    {
        var clean = GeminiFileInput.CleanText("A\0   B\r\n\r\n C\t D");
        Assert.Equal("A B\nC D", clean);

        var oversized = GeminiFileInput.CleanText(new string('x', 130_000));
        Assert.Equal(120_000, oversized.Length);
    }

    private sealed class FakeGeminiService : IGeminiService
    {
        public bool IsConfigured { get; init; } = true;
        public string Response { get; init; } = "{}";
        public Exception? Error { get; init; }

        public Task<string> GenerateTextAsync(string prompt, CancellationToken ct = default) =>
            Error == null ? Task.FromResult(Response) : Task.FromException<string>(Error);

        public Task<string> GenerateFromInlineDataAsync(
            byte[] data,
            string mimeType,
            string prompt,
            CancellationToken ct = default) =>
            Error == null ? Task.FromResult(Response) : Task.FromException<string>(Error);

        // Ba thành viên dưới đây thêm 26/08 cùng lúc với rà trùng lặp. Test này không dùng tới,
        // chỉ cần thoả interface.
        public string EmbeddingModel => "text-embedding-004";

        public Task<GeminiUsage> GenerateWithUsageAsync(string prompt, CancellationToken ct = default) =>
            Error == null
                ? Task.FromResult(new GeminiUsage(Response, null, null, 0, "fake"))
                : Task.FromException<GeminiUsage>(Error);

        public Task<float[]> EmbedAsync(string text, CancellationToken ct = default) =>
            Error == null ? Task.FromResult(new float[768]) : Task.FromException<float[]>(Error);
    }
}
