using Automate.Helpers;
using Automate.Interfaces;
using Automate.Services;
using CL.Common.Models;
using CrystalQuartz.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Quartz;
using StayAccess.Arches.Interfaces;
using StayAccess.Arches.Repositories;
using StayAccess.BLL.Interfaces;
using StayAccess.BLL.Repositories;
using StayAccess.DAL;
using StayAccess.DAL.Interfaces;
using StayAccess.DAL.Repositories;
using StayAccess.DTO.APISettings;
using StayAccess.DTO.Doors.ArchesFrontDoor;
using StayAccess.DTO.Doors.LatchDoor;
using StayAccess.DTO.Doors.MCDoor;
using StayAccess.DTO.Email;
using StayAccess.Latch.Interfaces;
using StayAccess.Latch.Repositories;
using StayAccess.MC.Interfaces;
using StayAccess.MC.Repositories;
using StayAccess.Tools.Interfaces;
using StayAccess.Tools.Repositories;

namespace Automate
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();

            // register StayAccessDbContext
            services.AddDbContext<StayAccessDbContext>(dbOptions => dbOptions.UseSqlServer(Configuration["ConnectionStrings:DefaultConnection"], sqlDBOptions => sqlDBOptions.MigrationsAssembly("StayAccess.DAL")));

            // register repositories
            services.AddTransient(typeof(IGenericService<>), typeof(GenericService<>));

            services.AddTransient<IReservationCodeService, ReservationCodeService>();
            services.AddTransient<IBackupCodesService, BackupCodesService>();
            services.AddTransient<IUsedBackupCodesService, UsedBackupCodesService>();
            services.AddTransient<IHomeAssistantService, HomeAssistantService>();
            services.AddTransient(typeof(ILoggerService<>), typeof(LoggerService<>));
            services.AddTransient<IUnitActionLogService, UnitActionLogService>();
            services.AddTransient<IEventLoggerService, EventLoggerService>();
            services.AddTransient<IBuildingUnitService, BuildingUnitService>();
            services.AddTransient<ILockCodeTrackingService, LockCodeTrackingService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<ICodeTransactionService, CodeTransactionService>();
            services.AddTransient<ILogsCleanUpService, LogsCleanUpService>();
            services.AddTransient<ILogService, LogService>();
            services.AddTransient<IEmailLoggerService, EmailLoggerService>();
            services.AddTransient<IDateService, DateService>();

            services.AddTransient<IPersistentTokenService, PersistentTokenService>();
            services.AddTransient<ILatchService, StayAccess.Latch.Repositories.LatchService>();
            services.AddTransient<IMCService, MCService>();
            services.AddTransient<IReservationService, ReservationService>();
            services.AddTransient<ILatchReservationService, StayAccess.Latch.Repositories.LatchReservationService>();
            services.AddTransient<IBuildingService, BuildingService>();
            services.AddTransient<ILockKeyService, LockKeyService>();
            services.AddTransient<IBuildingLockSystemService, BuildingLockSystemService>();
            services.AddTransient<ILatchIntegrationService, LatchIntegrationService>();
            // services.AddTransient<ILatchIntegrationService, LatchIntegrationService>();
            services.AddTransient<FrontDoorService>();

            services.AddTransient<IAPIProviderService, APIProviderService>();


            // configuring for Latch Api
            services.Configure<LatchApi>(Configuration.GetSection("LatchApi"));
            services.Configure<MCApi>(Configuration.GetSection("MCApi"));

            // configuring Unit,Frontdoor Home Assistant API End Points
            services.Configure<HomeAssistantEndPoints>(Configuration.GetSection("HomeAssistantEndPoints"));

            // configuring Email credentials for email service
            services.Configure<EmailConfiguration>(Configuration.GetSection("EmailConfiguration"));

            // configuring ApiSettings to Mri credentials
            services.Configure<APIProviderSettings>(Configuration.GetSection("APIProviderSettings"));

            // configuring reservation start time credentials
            services.Configure<StayAccess.DTO.Reservations.Settings.ReservationStartTimeSetting>(Configuration.GetSection("ReservationStartTimeSetting"));

            // Configure jobs
            services.UseQuartz();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            // configure quartz timer
            var scheduler = app.ApplicationServices.GetService<IScheduler>();
            var quartz = new QuartzStartup(scheduler, Configuration);
            lifetime.ApplicationStarted.Register(quartz.Start);
            lifetime.ApplicationStopping.Register(quartz.Stop);

            app.UseCrystalQuartz(() => scheduler);
        }
    }
}
