using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StayAccess.API;
using StayAccess.Api.Interfaces;
using StayAccess.Api.Services;
using StayAccess.DAL;
using StayAccess.DAL.Interfaces;
using StayAccess.DAL.Repositories;
using System.Text;
using StayAccess.DTO.Email;
using StayAccess.Latch.Interfaces;
using StayAccess.Tools.Repositories;
using StayAccess.Tools.Interfaces;
using StayAccess.BLL.Repositories;
using StayAccess.BLL.Interfaces;
using StayAccess.DTO.Doors.LatchDoor;
using Newtonsoft.Json;
using StayAccess.DTO.APISettings;
using CL.Common.Models;
using StayAccess.Latch.Repositories;
using StayAccess.Arches.Interfaces;
using StayAccess.Arches.Repositories;
using StayAccess.DTO.Doors.MCDoor;
using StayAccess.MC.Repositories;
using StayAccess.MC.Interfaces;

namespace StayAccess.Api
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
            services.AddControllers().AddNewtonsoftJson(
              options => options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore);
            services.AddMemoryCache();

            // register StayAccessDbContext
            services.AddDbContext<StayAccessDbContext>(dbOptions => dbOptions.UseSqlServer(Configuration["ConnectionStrings:DefaultConnection"], sqlDBOptions => sqlDBOptions.MigrationsAssembly("StayAccess.DAL")));

            // register repositories
            services.AddTransient(typeof(IGenericService<>), typeof(GenericService<>));
            services.AddTransient<IBuildingUnitService, BuildingUnitService>();
            services.AddTransient<IReservationCodeService, ReservationCodeService>();
            services.AddTransient<IBackupCodesService, BackupCodesService>();
            services.AddTransient<IUsedBackupCodesService, UsedBackupCodesService>();
            services.AddTransient<Arches.Interfaces.IHomeAssistantService, Arches.Repositories.HomeAssistantService>();
            services.AddTransient(typeof(ILoggerService<>), typeof(LoggerService<>));
            services.AddTransient<IUnitActionLogService, UnitActionLogService>();

            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IEventLoggerService, EventLoggerService>();
            services.AddTransient<IPersistentTokenService, PersistentTokenService>();
            services.AddTransient<ICodeTransactionService, CodeTransactionService>();
            //services.AddTransient<MCService, MCService>();
            services.AddTransient<IMCService, MCService>();
           
            services.AddTransient<ILatchService, Latch.Repositories.LatchService>();    
            services.AddTransient<IReservationService, ReservationService>();
            services.AddTransient<ILogService, LogService>();
            services.AddTransient<IDateService, DateService>();

            services.AddTransient<Latch.Interfaces.ILatchReservationService, Latch.Repositories.LatchReservationService>();
            services.AddTransient<IBuildingService, BuildingService>();
            services.AddTransient<ILockKeyService, LockKeyService>();
            services.AddTransient<IBuildingLockSystemService, BuildingLockSystemService>();
            services.AddTransient<ILatchIntegrationService, LatchIntegrationService>();
            services.AddTransient<IEmailLoggerService, EmailLoggerService>();

            services.AddTransient<IAPIProviderService, APIProviderService>();


            services.AddTransient<IHttpClientService, HttpClientService>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // configuring for Latch API
            services.Configure<LatchApi>(Configuration.GetSection("LatchApi"));
            services.Configure<MCApi>(Configuration.GetSection("MCApi"));
            services.AddSingleton<MCApi>();


            // configuring Unit,Frontdoor Home Assistant API End Points
            services.Configure<DTO.Doors.ArchesFrontDoor.HomeAssistantEndPoints>(Configuration.GetSection("HomeAssistantEndPoints"));

            // configuring Email credentials for email service
            services.Configure<EmailConfiguration>(Configuration.GetSection("EmailConfiguration"));

            // configuring ApiSettings to Mri credentials
            services.Configure<APIProviderSettings>(Configuration.GetSection("APIProviderSettings"));

            // configuring reservation start time credentials
            services.Configure<StayAccess.DTO.Reservations.Settings.ReservationStartTimeSetting>(Configuration.GetSection("ReservationStartTimeSetting"));

            // cors policy
            services.AddCors(options => options.AddPolicy("Cors",
                builder =>
                {
                    builder.AllowAnyOrigin().
                            AllowAnyMethod().
                            AllowAnyHeader();
                }));

            ConfigureAuthentication(services);

            ConfigureSwagger(services);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };


            // configure azure application insights
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors("Cors");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();            
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "StayAccess.API v1");
                c.RoutePrefix = string.Empty;
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// Configures authentication
        /// </summary>
        /// <param name="services"></param>
        private void ConfigureAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                };
            });
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StayAccess.API", Version = "v1" });
                });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };
                options.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                options.OperationFilter<AuthenticationOperationFilter>();
            });
        }
    }
}