using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Application.Settings;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FURPMS.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FURPMSDbContext>(options =>
            options
                // Chấp nhận cả chuỗi dạng URI mà Railway/Render/Heroku phơi ra
                // (postgresql://…) lẫn dạng khoá=giá trị của Npgsql — xem PostgresConnectionString.
                .UseNpgsql(PostgresConnectionString.Resolve(configuration))
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        services.Configure<JwtSettings>(opts => configuration.GetSection("JwtSettings").Bind(opts));
        services.Configure<EmailSettings>(opts => configuration.GetSection("EmailSettings").Bind(opts));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProposalRepository, ProposalRepository>();
        services.AddScoped<ICycleRepository, CycleRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IMasterDataRepository, MasterDataRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAcademicWorkService, AcademicWorkService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICycleService, CycleService>();
        services.AddScoped<IProposalService, ProposalService>();
        services.AddScoped<IProposalDocumentService, ProposalDocumentService>();
        services.AddScoped<IProposalExtractionService, ProposalExtractionService>();
        services.AddScoped<IResearchOrderService, ResearchOrderService>();
        services.AddScoped<IReviewRoundService, ReviewRoundService>();
        services.AddScoped<IReviewBoardService, ReviewBoardService>();
        services.AddScoped<ICouncilService, CouncilService>();
        services.AddScoped<IPersonnelRoleTypeService, PersonnelRoleTypeService>();
        services.AddScoped<IBudgetExpenseCategoryService, BudgetExpenseCategoryService>();
        services.AddScoped<ISystemFinancialConfigService, SystemFinancialConfigService>();
        services.AddScoped<ISystemSettingService, SystemSettingService>();
        services.AddScoped<IProposalBudgetService, ProposalBudgetService>();
        services.AddScoped<IBudgetPolicyService, BudgetPolicyService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();
        services.AddScoped<IChangeRequestService, ChangeRequestService>();
        services.AddScoped<IDocumentExportService, DocumentExportService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IDisbursementService, DisbursementService>();
        services.AddScoped<IDeliverableService, DeliverableService>();
        services.AddScoped<IAmendmentService, AmendmentService>();
        services.AddScoped<IReviewScoringService, ReviewScoringService>();
        services.AddScoped<IProgressReportService, ProgressReportService>();
        services.AddScoped<IFinalReportService, FinalReportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICouncilMeetingService, CouncilMeetingService>();
        services.AddScoped<IContractSettlementService, ContractSettlementService>();
        services.AddScoped<IAcceptanceEvaluationService, AcceptanceEvaluationService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddHttpClient<IGeminiService, GeminiService>();
        services.AddScoped<IAiSummaryService, AiSummaryService>();
        services.AddScoped<IRubricResolver, RubricResolver>();
        services.AddScoped<IAiAdvisorService, AiAdvisorService>();
        // Chỗ lưu file: có cấu hình Cloudinary thì dùng, không thì đĩa local.
        // ⚠️ PRODUCTION (Render) BẮT BUỘC có Cloudinary — filesystem của Render là TẠM,
        // mỗi lần redeploy/restart là mất sạch file người dùng đã nộp.
        if (CloudinaryFileStorage.IsConfigured(configuration))
            services.AddHttpClient<IFileStorage, CloudinaryFileStorage>();
        else
            services.AddSingleton<IFileStorage, LocalDiskFileStorage>();

        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<INotifier, Notifier>();
        services.AddScoped<IDeadlineReminderScanner, DeadlineReminderScanner>();
        // Hàng đợi phải là SINGLETON: controller/service ghi vào, worker nền đọc ra —
        // hai vòng đời khác nhau, đăng ký scoped là mỗi bên cầm một hàng đợi riêng.
        services.AddSingleton<IAiSummaryQueue, AiSummaryQueue>();
        services.AddHostedService<AiSummaryPregenerationService>();
        services.AddHostedService<DeadlineReminderService>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<DemoScenarioSeeder>();

        return services;
    }
}
