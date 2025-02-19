using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;
using Minio;
using Newtonsoft.Json.Serialization;
using MongoDB.Driver;
using FestivalHoa.Properties.Installers;
using FestivalHoa.Properties.Interfaces.MonitorApi;
using FestivalHoa.Properties.Services.MonitorApi;

namespace FestivalHoa
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Đọc chuỗi kết nối từ cấu hình
            string mongoConnectionString = Configuration["DbSettings:ConnectionString"];
            string databaseName = Configuration["DbSettings:DatabaseName"];

            // Đăng ký MongoClient vào DI container
            services.AddSingleton<IMongoClient>(new MongoClient(mongoConnectionString));

            // Đăng ký IMongoDatabase để inject trực tiếp
            services.AddScoped<IMongoDatabase>(provider =>
            {
                var client = provider.GetRequiredService<IMongoClient>();
                return client.GetDatabase(databaseName);
            });

            // **Thêm HttpClientFactory**
            services.AddHttpClient();

            // Đăng ký MonitorService
            services.AddScoped<IMonitorService, CallHistoryService>();
            services.AddSingleton<IScheduledCallService, ScheduledCallService>(); // Service quản lý lịch gọi API

            // Đăng ký Background Service chạy tự động
            services.AddHostedService<ApiCallScheduler>();
            // Cấu hình Minio
            string endpoint = "minio.dongthap.gov.vn:9000";
            string accessKey = "CKghMslGxFQhnlTN";
            string secretKey = "UKnz2ype9MTCKNZqH2wbFcxS1Vph7ncx";

            services.AddMinio(accessKey, secretKey);
            services.AddMinio(configureClient => configureClient
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey));

            services.AddHttpContextAccessor();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddControllers(x => x.AllowEmptyInputInBodyModelBinding = true)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            services.InstallServicesInAssembly(Configuration);
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FestivalHoa.WebAPI", Version = "v1" });
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithOrigins(
                            "http://localhost:8080",
                            "http://localhost:8081",
                            "http://localhost:8013",
                            "http://localhost:8014",
                            "http://127.0.0.1:5500",
                            "https://festivalhoa.dongthap.gov.vn",
                            "https://hoasadec.com.vn",
                            "http://hoasadec.com.vn"
                        )
                        .AllowCredentials());
            });

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = long.MaxValue;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.Limits.MaxRequestBodySize = long.MaxValue;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Use(async (context, next) =>
            {
                var culture = CultureInfo.CurrentCulture.Clone() as CultureInfo;
                culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FestivalHoa.WebAPI v1"));
            }

            app.UseHttpsRedirection();
            app.UseCors("CorsPolicy");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
