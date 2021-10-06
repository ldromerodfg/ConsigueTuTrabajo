using System.Threading.Tasks;
using DataAccess.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Service.Interfaces;
using Service.Services;

namespace Web
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
            services.AddControllersWithViews();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });

            services.AddDbContextPool<DefaultContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"),
                x => x.MigrationsAssembly("DataAccess")));

            services.AddTransient<ISettingService, SettingService>();
            services.AddTransient<IPositionService, PositionService>();
            services.AddTransient<ICandidateService, CandidateService>();
            services.AddTransient<IBackupService, BackupService>();
            services.AddTransient<ICityService, CityService>();
            services.AddTransient<ICompanyService, CompanyService>();
            services.AddTransient<ICountryService, CountryService>();
            services.AddTransient<IPositionTypeService, PositionTypeService>();
            services.AddTransient<IStateService, StateService>();
            services.AddTransient<ICandidateStageService, CandidateStageService>();

            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceScopeFactory serviceScopeFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = "api/v1";
            });

            Task.Run(async () =>
            {
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var settingService = scope.ServiceProvider.GetService<ISettingService>();
                    var backupService = scope.ServiceProvider.GetService<IBackupService>();

                    var setting = await settingService.GetAsync();

                    if (setting.BreezyToken == null)
                    {
                        await backupService.GetBreezyToken();
                        setting = await settingService.GetAsync();
                    }

                    await backupService.GetCountries();

                    if (setting.BreezyToken != null)
                    {
                        await backupService.GetCountries();
                        await backupService.GetCompanies(setting.BreezyToken);
                        await backupService.GetPositions(setting.BreezyToken);
                        await backupService.GetCandidates(setting.BreezyToken);
                    }
                }
            });
        }
    }
}