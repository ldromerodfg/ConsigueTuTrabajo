using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

            services.AddDbContext<DefaultContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), x => x.MigrationsAssembly("DataAccess")));

            services.AddTransient<IPositionService, PositionService>();

            services.AddSwaggerGen();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

#if DEBUG
                Task.Run(async () =>
                {
                    using (var serviceScope = app.ApplicationServices.CreateScope())
                    {
                        var dbContext = serviceScope.ServiceProvider.GetService<DefaultContext>();
                        await GetData(dbContext);
                    }
                });
#endif
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = "api/v1";
            });
        }

        public async Task GetData(DefaultContext context)
        {
            string breezyToken = null;

            using (var client = new HttpClient())
            {
                try
                {
                    var content = new List<KeyValuePair<string, string>>();
                    content.Add(new KeyValuePair<string, string>("email", Configuration["BreezyHR:Email"]));
                    content.Add(new KeyValuePair<string, string>("password", Configuration["BreezyHR:Password"]));
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.breezy.hr/v3/signin")
                    {
                        Content = new FormUrlEncodedContent(content)
                    };
                    var response = await client.SendAsync(request);

                    var responseBody = await response.Content.ReadAsStringAsync();

                    var userInfo = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    breezyToken = userInfo.access_token;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            if (!context.Country.Any())
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        context.Database.OpenConnection();
                        var response = await client.GetAsync("https://restcountries.eu/rest/v2/all");
                        response.EnsureSuccessStatusCode();
                        var responseBody = await response.Content.ReadAsStringAsync();

                        var countries = JsonConvert.DeserializeObject<dynamic[]>(responseBody);

                        if (countries != null && countries.Any())
                        {
                            foreach (var country in countries)
                            {
                                await context.Country.AddAsync(new Country
                                {
                                    Name = country.name,
                                    Code = country.alpha2Code
                                });
                            }

                            await context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }

            if (!context.Company.Any())
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("Authorization", breezyToken);
                        context.Database.OpenConnection();
                        var response = await client.GetAsync("https://api.breezy.hr/v3/companies");
                        response.EnsureSuccessStatusCode();
                        var responseBody = await response.Content.ReadAsStringAsync();

                        var companies = JsonConvert.DeserializeObject<dynamic[]>(responseBody);

                        if (companies != null && companies.Any())
                        {
                            foreach (var company in companies)
                            {
                                var companyId = (string)company._id;

                                if (!context.Company.Any(x => x.BreezyId == companyId))
                                {
                                    await context.Company.AddAsync(new Company
                                    {
                                        BreezyId = companyId,
                                        Name = company.name,
                                        FriendlyId = company.friendly_id,
                                        Created = DateTime.Parse((string)company.creation_date).ToUniversalTime(),
                                        Modified = DateTime.Parse((string)company.updated_date).ToUniversalTime(),
                                        MemberCount = (int)company.member_count,
                                        Initial = company.initial
                                    });
                                }
                            }

                            await context.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }

            if (!context.Position.Any())
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("Authorization", breezyToken);
                        context.Database.OpenConnection();

                        var positionStates = new List<string> {
                            "published", "draft", "archived", "closed", "pending"
                        };

                        foreach (var positionState in positionStates)
                        {
                            foreach (var company in await context.Company.ToListAsync())
                            {
                                var response = await client.GetAsync($"https://api.breezy.hr/v3/company/{company.BreezyId}/positions?state={positionState}");
                                response.EnsureSuccessStatusCode();
                                var responseBody = await response.Content.ReadAsStringAsync();

                                var positions = JsonConvert.DeserializeObject<dynamic[]>(responseBody);

                                if (positions != null && positions.Any())
                                {
                                    foreach (var position in positions)
                                    {
                                        var positionId = (string)position._id;

                                        if (await context.Position.AnyAsync(x => x.BreezyId == positionId))
                                        {
                                            continue;
                                        }

                                        if (position.location != null)
                                        {
                                            var cityName = position.location != null && position.location.city != null ? (string)position.location.city : null;
                                            var stateCode = position.location != null && position.location.state != null ? (string)position.location.state.id : null;
                                            var countryCode = position.location != null ? (string)position.location.country.id : null;

                                            var city = await context.City.FirstOrDefaultAsync(x => x.Name == cityName);

                                            if (city == null && cityName != null)
                                            {
                                                var state = await context.State.FirstOrDefaultAsync(x => x.Code == stateCode);

                                                if (state == null && stateCode != null)
                                                {
                                                    var country = await context.Country.FirstOrDefaultAsync(x => x.Code == countryCode);

                                                    state = new State
                                                    {
                                                        Name = position.location.state.name,
                                                        Code = position.location.state.id,
                                                        CountryId = country.Id
                                                    };

                                                    await context.State.AddAsync(state);
                                                    await context.SaveChangesAsync();
                                                }

                                                city = new City
                                                {
                                                    Name = position.location.city,
                                                    StateId = state.Id
                                                };

                                                await context.City.AddAsync(city);

                                                await context.SaveChangesAsync();
                                            }

                                            var positionTypeId = (string)position.type.id;

                                            var positionType = await context.PositionType.FirstOrDefaultAsync(x => x.Code == positionTypeId);

                                            if (positionType == null)
                                            {
                                                positionType = new PositionType
                                                {
                                                    Name = position.type.name,
                                                    Code = positionTypeId
                                                };

                                                await context.PositionType.AddAsync(positionType);
                                                await context.SaveChangesAsync();
                                            }

                                            await context.Position.AddAsync(new Position
                                            {
                                                BreezyId = position._id,
                                                State = position.state,
                                                Name = position.name,
                                                Education = position.education,
                                                Description = position.description,
                                                Created = DateTime.Now,
                                                CityId = city != null ? city.Id : null,
                                                CompanyId = company.Id,
                                                PositionTypeId = positionType.Id
                                            });
                                        }
                                    }

                                    await context.SaveChangesAsync();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        context.Database.CloseConnection();
                    }
                }
            }
        }
    }
}