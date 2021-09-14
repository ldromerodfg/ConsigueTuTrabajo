using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
            string log = "";
            string breezyToken = null;
            TimeSpan requestTimeout = TimeSpan.FromMinutes(2);
            var maxTries = 3;

            using (var client = new HttpClient())
            {
                client.Timeout = requestTimeout;

                try
                {
                    var content = new List<KeyValuePair<string, string>>();
                    content.Add(new KeyValuePair<string, string>("email", Configuration["BreezyHR:Email"]));
                    content.Add(new KeyValuePair<string, string>("password", Configuration["BreezyHR:Password"]));
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.breezy.hr/v3/signin")
                    {
                        Content = new FormUrlEncodedContent(content)
                    };

                    Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: Requesting for Breezy token...");
                    log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: Requesting for Breezy token...";

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: SUCCESS");
                        log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: SUCCESS";

                        var responseBody = await response.Content.ReadAsStringAsync();

                        var userInfo = JsonConvert.DeserializeObject<dynamic>(responseBody);

                        breezyToken = userInfo.access_token;

                        Debug.WriteLine($"Breezy Token: {breezyToken}");
                    }
                    else
                    {
                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: REQUEST UNSUCCESSFUL. HttpStatus: {response.StatusCode}");
                        log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: REQUEST UNSUCCESSFUL. HttpStatus: {response.StatusCode}";
                    }
                }
                catch (TaskCanceledException ex)
                {
                    if (!ex.CancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: REQUEST TIMEOUT.");
                        log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: REQUEST TIMEOUT.";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}");
                    log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}";
                }
            }

            if (!context.Country.Any())
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = requestTimeout;

                    try
                    {
                        var countryResponse = await client.GetAsync("https://restcountries.eu/rest/v2/all");
                        countryResponse.EnsureSuccessStatusCode();
                        var countryResponseBody = await countryResponse.Content.ReadAsStringAsync();

                        var countries = JsonConvert.DeserializeObject<dynamic[]>(countryResponseBody);

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
                }
            }

            if (breezyToken != null)
            {
                //COMPANIES
                if (!context.Company.Any())
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = requestTimeout;
                        client.DefaultRequestHeaders.Add("Authorization", breezyToken);

                        try
                        {
                            var companyResponse = await client.GetAsync("https://api.breezy.hr/v3/companies");
                            companyResponse.EnsureSuccessStatusCode();
                            var companyResponseBody = await companyResponse.Content.ReadAsStringAsync();

                            var companies = JsonConvert.DeserializeObject<dynamic[]>(companyResponseBody);

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
                                            Updated = DateTime.Parse((string)company.updated_date).ToUniversalTime(),
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
                    }
                }

                //POSITIONS
                if (!context.Position.Any())
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = requestTimeout;
                        client.DefaultRequestHeaders.Add("Authorization", breezyToken);

                        try
                        {
                            var positionStates = new List<string> {
                                "published", "draft", "archived", "closed", "pending"
                            };

                            foreach (var positionState in positionStates)
                            {
                                foreach (var company in await context.Company.ToListAsync())
                                {
                                    var positionResponse = await client.GetAsync(
                                        $"https://api.breezy.hr/v3/company/{company.BreezyId}/positions?state={positionState}");

                                    if (positionResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        var positionResponseBody = await positionResponse.Content.ReadAsStringAsync();

                                        var positions = JsonConvert.DeserializeObject<dynamic[]>(positionResponseBody);

                                        if (positions != null && positions.Any())
                                        {
                                            foreach (var position in positions)
                                            {
                                                var positionId = (string)position._id;

                                                if (await context.Position.AnyAsync(x => x.BreezyId == positionId))
                                                {
                                                    continue;
                                                }

                                                var cityName = position.location != null && position.location.city != null
                                                    && !string.IsNullOrEmpty((string)position.location.city)
                                                    ? (string)position.location.city
                                                    : null;
                                                var stateCode = position.location != null && position.location.state != null
                                                    ? (string)position.location.state.id
                                                    : null;
                                                var countryCode = position.location != null
                                                    ? (string)position.location.country.id
                                                    : null;

                                                var city = position.location != null
                                                    ? await context.City.FirstOrDefaultAsync(x => x.Name == cityName)
                                                    : null;

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

                                                        city = new City
                                                        {
                                                            Name = position.location.city,
                                                            StateId = state.Id
                                                        };
                                                        await context.City.AddAsync(city);
                                                    }
                                                    await context.SaveChangesAsync();
                                                }

                                                var positionTypeId = position.type != null
                                                    ? (string)position.type.id
                                                    : null;

                                                var positionType = await context.PositionType.FirstOrDefaultAsync(x => x.Code == positionTypeId);

                                                if (positionTypeId != null && positionType == null)
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
                                                    PositionTypeId = positionType != null ? positionType.Id : null
                                                });
                                            }
                                        }
                                    };
                                }
                                await context.SaveChangesAsync();
                            }

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }

                //CANDIDATES
                if (!context.Candidate.Any())
                {
                    try
                    {
                        var iPosition = 0;

                        foreach (var position in await context.Position.Include(x => x.Company).IgnoreQueryFilters().AsNoTracking().ToListAsync())
                        {
                            iPosition++;

                            Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: LOOPING POSITION {iPosition} ({position.BreezyId})...");
                            log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: LOOPING POSITION {iPosition} ({position.BreezyId})...";

                            var page = 1;

                            var candidateResponse = new HttpResponseMessage();

                            var tries = 1;

                            do
                            {
                                try
                                {
                                    using (var client = new HttpClient())
                                    {
                                        client.Timeout = requestTimeout;

                                        client.DefaultRequestHeaders.Add("Authorization", breezyToken);

                                        var uri = $"https://api.breezy.hr/v3/company/{position.Company.BreezyId}/position/{position.BreezyId}/candidates?page_size=50&page={page}";

                                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: (POSITION {iPosition}) SENDING REQUEST TO {uri}...");
                                        log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: ({iPosition}) SENDING REQUEST TO {uri}...";

                                        candidateResponse = await client.GetAsync(uri);

                                        if (candidateResponse.IsSuccessStatusCode)
                                        {
                                            Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: SUCCESS");
                                            log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: SUCCESS";

                                            var candidateResponseBody = await candidateResponse.Content.ReadAsStringAsync();

                                            var candidates = JsonConvert.DeserializeObject<dynamic[]>(candidateResponseBody);

                                            if (candidates == null || !candidates.Any())
                                            {
                                                Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: MOVING TO NEXT POSITION...");
                                                log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: MOVING TO NEXT POSITION...";
                                                break;
                                            }

                                            int n = 1;

                                            foreach (var candidate in candidates)
                                            {
                                                var candidateId = (string)candidate._id;

                                                var candidateStageId = (string)candidate.stage.id;

                                                var candidateStage = await context.CandidateStage
                                                    .FirstOrDefaultAsync(x => x.BreezyId == candidateStageId);

                                                if (candidateStage == null)
                                                {
                                                    candidateStage = new CandidateStage
                                                    {
                                                        BreezyId = candidateStageId,
                                                        Name = (string)candidate.stage.name
                                                    };

                                                    await context.CandidateStage.AddAsync(candidateStage);

                                                    await context.SaveChangesAsync();
                                                }

                                                if (!await context.Candidate.AnyAsync(x => x.BreezyId == candidateId))
                                                {
                                                    await context.Candidate.AddAsync(new Candidate
                                                    {
                                                        BreezyId = candidateId,
                                                        MetaId = (string)candidate.meta_id,
                                                        Email = (string)candidate.email_address,
                                                        Headline = (string)candidate.headline,
                                                        Initial = (string)candidate.initial,
                                                        Name = (string)candidate.name,
                                                        Origin = (string)candidate.origin,
                                                        PhoneNumber = (string)candidate.phone_number,
                                                        Created = DateTime.Parse((string)candidate.creation_date).ToUniversalTime(),
                                                        Updated = (string)candidate.updated_date != null
                                                            ? DateTime.Parse((string)candidate.creation_date).ToUniversalTime()
                                                            : null,
                                                        PositionId = position.Id,
                                                        CandidateStageId = candidateStage.Id
                                                    });
                                                    n++;
                                                }
                                            }

                                            Debug.WriteLine("");

                                            page++;
                                        }
                                        else if (candidateResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                        {
                                            Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: REQUEST LIMIT REACHED. WAITING ONE MINUTE...");
                                            log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: REQUEST LIMIT REACHED. WAITING ONE MINUTE...";
                                            Thread.Sleep(90000);

                                            tries++;
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: UNSUCCESSFUL REQUEST. STATUS CODE {candidateResponse.StatusCode}");
                                            log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: UNSUCCESSFUL REQUEST. STATUS CODE {candidateResponse.StatusCode}";

                                            tries++;
                                        }
                                    }
                                    
                                    if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");
                                }
                                catch (TaskCanceledException ex)
                                {
                                    if (!ex.CancellationToken.IsCancellationRequested)
                                    {
                                        Debug.WriteLine($"REQUEST TIMEOUT. TRYING AGAIN ({tries} of {maxTries})...");
                                        log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: REQUEST TIMEOUT. TRYING AGAIN ({tries} of {maxTries})...";
                                    }
                                    else 
                                    {
                                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}");
                                        log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}";
                                    }

                                    if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");

                                    Thread.Sleep(10000);

                                    tries++;
                                }
                                catch(HttpRequestException ex)
                                {
                                    Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}");
                                    log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}";

                                    if (tries == 3) throw new Exception("REQUEST TRIES LIMIT REACHED");

                                    Thread.Sleep(10000);
                                    tries++;
                                }
                            }
                            while (candidateResponse.StatusCode == System.Net.HttpStatusCode.OK
                                || candidateResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests);
                        }

                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}");
                        log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}";
                    }
                }
            }
            else
            {
                Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: Breezy Token is null");
                log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: Breezy Token is null";
            }

            Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: EXECUTION FINISHED");
            log += $"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: EXECUTION FINISHED";

            try{
                await File.WriteAllTextAsync($"./bin/Debug/log_{DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss")}.txt", log);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}: {ex.Message}");
            }
        }
    }
}