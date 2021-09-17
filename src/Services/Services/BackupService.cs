using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Contexts;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.Interfaces;

namespace Service.Services
{
    public class BackupService : IBackupService
    {
        private readonly ILogger _logger;
        private readonly DefaultContext _dbContext;
        private readonly IConfiguration _configuration;
        private TimeSpan requestTimeout = TimeSpan.FromMinutes(2);
        private const int maxTries = 5;
        private const int awaitTime = 30000;

        public BackupService(ILogger<BackupService> logger, DefaultContext dbContext, 
            IConfiguration configuration)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task GetBreezyToken()
        { 
            string breezyToken = null;

            using (var client = new HttpClient())
            {
                client.Timeout = requestTimeout;

                try
                {
                    var content = new List<KeyValuePair<string, string>>();
                    content.Add(new KeyValuePair<string, string>("email", _configuration["BreezyHR:Email"]));
                    content.Add(new KeyValuePair<string, string>("password", _configuration["BreezyHR:Password"]));
                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.breezy.hr/v3/signin")
                    {
                        Content = new FormUrlEncodedContent(content)
                    };

                    //log += WriteOnLog("Requesting for Breezy token...");

                    _logger.LogInformation("Requesting for Breezy token...");

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        //log += WriteOnLog($"SUCCESS");
                        _logger.LogInformation("SUCCESS");

                        var responseBody = await response.Content.ReadAsStringAsync();

                        var userInfo = JsonConvert.DeserializeObject<dynamic>(responseBody);

                        // log += WriteOnLog($"Breezy Token: {breezyToken}");
                        //_logger.LogInformation($"Breezy Token: {breezyToken}");

                        breezyToken = (string)userInfo.access_token;
                    }
                    else
                    {
                        // log += WriteOnLog($"REQUEST UNSUCCESSFUL. HttpStatus: {response.StatusCode}");
                        _logger.LogInformation($"REQUEST UNSUCCESSFUL. HttpStatus: {response.StatusCode}");
                    }
                }
                catch (TaskCanceledException ex)
                {
                    if (!ex.CancellationToken.IsCancellationRequested)
                    {
                        // log += WriteOnLog("REQUEST TIMEOUT.");
                        _logger.LogInformation($"REQUEST TIMEOUT.");
                    }
                }
                catch (Exception ex)
                {
                    // log += WriteOnLog(ex.Message);
                    _logger.LogInformation(ex.Message);
                }
            }

            try{
                var setting = await _dbContext.Setting.FirstAsync();
                setting.BreezyToken = breezyToken;

                _dbContext.Setting.Update(setting);
                await _dbContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        public async Task GetCountries()
        {
            if (!_dbContext.Country.Any())
            {
                _logger.LogInformation("FETCHING COUNTRIES...");

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
                                await _dbContext.Country.AddAsync(new Country
                                {
                                    Name = country.name,
                                    Code = country.alpha2Code
                                });
                            }

                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex.Message);
                    }
                }
            }
        }

        public async Task GetCompanies(string breezyToken)
        {
            if (!_dbContext.Company.Any())
            {
                _logger.LogInformation("FETCHING COMPANIES...");

                var _setting = await _dbContext.Setting.FirstAsync();
                
                using (var client = new HttpClient())
                {
                    client.Timeout = requestTimeout;
                    client.DefaultRequestHeaders.Add("Authorization", _setting.BreezyToken);

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

                                if (!_dbContext.Company.Any(x => x.BreezyId == companyId))
                                {
                                    await _dbContext.Company.AddAsync(new Company
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

                            await _dbContext.SaveChangesAsync();
                        }

                    }
                    catch(HttpRequestException ex)
                    {
                        
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex.Message);
                    }
                }
            }
        }

        public async Task GetPositions(string breezyToken)
        {
            _logger.LogInformation("FETCHING POSITIONS...");

            var _setting = await _dbContext.Setting.FirstAsync();

            using (var client = new HttpClient())
            {
                client.Timeout = requestTimeout;
                client.DefaultRequestHeaders.Add("Authorization", _setting.BreezyToken);

                var positionStates = new List<string> {
                    "published", "draft", "archived", "closed", "pending"
                };

                try
                {
                    foreach (var positionState in positionStates)
                    {
                        foreach (var company in await _dbContext.Company.ToListAsync())
                        {
                            var positionResponse = await client.GetAsync(
                                $"https://api.breezy.hr/v3/company/{company.BreezyId}/positions?state={positionState}");

                            if (positionResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var positionResponseBody = await positionResponse.Content.ReadAsStringAsync();

                                var positions = JsonConvert.DeserializeObject<dynamic[]>(positionResponseBody);

                                var dbPositions = await _dbContext.Position
                                    .Where(x => x.State == positionState)
                                    .Select(x => x.BreezyId)
                                    .AsNoTracking()
                                    .IgnoreQueryFilters()
                                    .ToListAsync();

                                if (positions != null && positions.Any()
                                    && positions.Any(x =>
                                        !dbPositions.Contains((string)x._id)))
                                {
                                    _logger.LogInformation($"NEW {positionState} POSITIONS FOUND");

                                    foreach (var position in positions.Where(x =>
                                        !dbPositions.Contains((string)x._id)))
                                    {
                                        var positionId = (string)position._id;

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
                                            ? await _dbContext.City.FirstOrDefaultAsync(x => x.Name == cityName)
                                            : null;

                                        if (city == null && cityName != null)
                                        {
                                            var state = await _dbContext.State.FirstOrDefaultAsync(x => x.Code == stateCode);

                                            if (state == null && stateCode != null)
                                            {
                                                var country = await _dbContext.Country.FirstOrDefaultAsync(x => x.Code == countryCode);

                                                state = new State
                                                {
                                                    Name = position.location.state.name,
                                                    Code = position.location.state.id,
                                                    CountryId = country.Id
                                                };

                                                await _dbContext.State.AddAsync(state);
                                                await _dbContext.SaveChangesAsync();

                                                city = new City
                                                {
                                                    Name = position.location.city,
                                                    StateId = state.Id
                                                };
                                                await _dbContext.City.AddAsync(city);
                                            }
                                            await _dbContext.SaveChangesAsync();
                                        }

                                        var positionTypeId = position.type != null
                                            ? (string)position.type.id
                                            : null;

                                        var positionType = await _dbContext.PositionType.FirstOrDefaultAsync(x => x.Code == positionTypeId);

                                        if (positionTypeId != null && positionType == null)
                                        {
                                            positionType = new PositionType
                                            {
                                                Name = position.type.name,
                                                Code = positionTypeId
                                            };

                                            await _dbContext.PositionType.AddAsync(positionType);
                                            await _dbContext.SaveChangesAsync();
                                        }

                                        _logger.LogInformation($"ADDING POSITION {position._id}...");

                                        await _dbContext.Position.AddAsync(new Position
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
                                else
                                {
                                    _logger.LogInformation($"NO NEW {positionState} POSITIONS");
                                }
                            };
                        }
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                }
            }
        }

        public async Task GetCandidates(string breezyToken)
        {
            if (!_dbContext.Candidate.Any())
            {
                _logger.LogInformation("FETCHING CANDIDATES...");

                var _setting = await _dbContext.Setting.FirstAsync();

                try
                {
                    var iPosition = 0;

                    var positions = await _dbContext.Position
                        .Include(x => x.Company)
                        .Select(x => new Position
                        {
                            BreezyId = x.BreezyId,
                            Company = new Company
                            {
                                BreezyId = x.Company.BreezyId
                            }
                        })
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var position in positions)
                    {
                        iPosition++;

                        // log += WriteOnLog($"LOOPING POSITION {iPosition} ({position.BreezyId})...");

                        _logger.LogInformation($"LOOPING POSITION {iPosition} ({position.BreezyId})...");

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

                                    client.DefaultRequestHeaders.Add("Authorization", _setting.BreezyToken);

                                    var uri = $"https://api.breezy.hr/v3/company/{position.Company.BreezyId}/position/{position.BreezyId}/candidates?page_size=50&page={page}";

                                    // log += WriteOnLog($"(POSITION {iPosition}) SENDING REQUEST TO {uri}...");
                                    _logger.LogInformation($"(POSITION {iPosition}) SENDING REQUEST TO {uri}...");

                                    candidateResponse = await client.GetAsync(uri);

                                    if (candidateResponse.IsSuccessStatusCode)
                                    {
                                        // log += WriteOnLog("SUCCESS");
                                        _logger.LogInformation($"SUCCESS");

                                        var candidateResponseBody = await candidateResponse.Content.ReadAsStringAsync();

                                        var candidates = JsonConvert.DeserializeObject<dynamic[]>(candidateResponseBody);

                                        if (candidates == null || !candidates.Any())
                                        {
                                            // log += WriteOnLog($"NO CANDIDATES FOR POSITION {position.BreezyId}. MOVING TO NEXT POSITION...");
                                            _logger.LogInformation($"NO CANDIDATES FOR POSITION {position.BreezyId}. MOVING TO NEXT POSITION...");
                                            break;
                                        }

                                        int n = 1;

                                        foreach (var candidate in candidates)
                                        {
                                            var candidateId = (string)candidate._id;

                                            var candidateStageId = (string)candidate.stage.id;

                                            var candidateStage = await _dbContext.CandidateStage
                                                .FirstOrDefaultAsync(x => x.BreezyId == candidateStageId);

                                            if (candidateStage == null)
                                            {
                                                candidateStage = new CandidateStage
                                                {
                                                    BreezyId = candidateStageId,
                                                    Name = (string)candidate.stage.name
                                                };

                                                await _dbContext.CandidateStage.AddAsync(candidateStage);

                                                await _dbContext.SaveChangesAsync();
                                            }

                                            if (!await _dbContext.Candidate.AnyAsync(x => x.BreezyId == candidateId))
                                            {
                                                await _dbContext.Candidate.AddAsync(new Candidate
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

                                            try
                                            {
                                                if (!Directory.Exists("./wwwroot/cv"))
                                                {
                                                    Directory.CreateDirectory("./wwwroot/cv");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                // log += WriteOnLog(ex.Message);
                                                _logger.LogInformation(ex.Message);
                                            }

                                            try
                                            {
                                                using (WebClient webClient = new WebClient())
                                                {
                                                    webClient.Headers.Add("Authorization", _setting.BreezyToken);

                                                    // log += WriteOnLog($"FETCHING CV FROM {(string)candidate.resume.url}...");
                                                    _logger.LogInformation($"FETCHING CV FROM {(string)candidate.resume.url}...");
                                                    byte[] arr = webClient.DownloadData((string)candidate.resume.url);

                                                    // log += WriteOnLog($"SUCCESS");
                                                    _logger.LogInformation($"SUCCESS");

                                                    var filePath = $"./wwwroot/cv/cv_{(string)candidate._id}.pdf";

                                                    if (!Directory.Exists(filePath))
                                                    {
                                                        // log += WriteOnLog($"WRITING CV FILE {filePath}");
                                                        _logger.LogInformation($"WRITING CV FILE {filePath}");
                                                        File.WriteAllBytes(filePath, arr);
                                                    }
                                                    else
                                                    {
                                                        // log += WriteOnLog($"CV FILE {filePath} ALREADY EXISTS");
                                                        _logger.LogInformation($"CV FILE {filePath} ALREADY EXISTS");
                                                    }
                                                }
                                            }
                                            catch (WebException ex)
                                            {
                                                // log += WriteOnLog(ex.Message);
                                                _logger.LogInformation(ex.Message);
                                            }
                                        }

                                        page++;
                                    }
                                    else if (candidateResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                    {
                                        // log += WriteOnLog($"REQUEST LIMIT REACHED. WAITING {awaitTime / 1000} MINUTES...");
                                        _logger.LogInformation($"REQUEST LIMIT REACHED. WAITING {awaitTime / 1000} MINUTES...");
                                        Thread.Sleep(awaitTime);

                                        tries++;
                                    }
                                    else
                                    {
                                        // log += WriteOnLog($"UNSUCCESSFUL REQUEST. STATUS CODE {candidateResponse.StatusCode}");
                                        _logger.LogInformation($"UNSUCCESSFUL REQUEST. STATUS CODE {candidateResponse.StatusCode}");
                                        Thread.Sleep(awaitTime);

                                        // log += WriteOnLog($"REQUEST LIMIT REACHED. {awaitTime / 1000} MINUTES...");
                                        _logger.LogInformation($"REQUEST LIMIT REACHED. {awaitTime / 1000} MINUTES...");

                                        tries++;
                                    }
                                }

                                if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");
                            }
                            catch (TaskCanceledException ex)
                            {
                                if (ex.CancellationToken.IsCancellationRequested)
                                {
                                    // log += WriteOnLog($"REQUEST TIMEOUT. ({tries} of {maxTries})...");
                                    _logger.LogInformation($"REQUEST TIMEOUT. ({tries} of {maxTries})...");
                                }
                                else
                                {
                                    // log += WriteOnLog(ex.Message);
                                    _logger.LogInformation(ex.Message);
                                }

                                if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");

                                Thread.Sleep(awaitTime);

                                // log += WriteOnLog($"REQUEST TIMEOUT. WAITING {awaitTime / 1000} SECONDS...");
                                _logger.LogInformation($"REQUEST TIMEOUT. WAITING {awaitTime / 1000} SECONDS...");

                                tries++;
                            }
                            catch (HttpRequestException ex)
                            {
                                // log += WriteOnLog(ex.Message);
                                _logger.LogInformation(ex.Message);

                                if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");

                                Thread.Sleep(awaitTime);

                                // log += WriteOnLog($"WAITING {awaitTime / 1000} SECONDS...");
                                _logger.LogInformation($"WAITING {awaitTime / 1000} SECONDS...");

                                tries++;
                            }
                        }
                        while (true);
                    }

                    await _dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // log += WriteOnLog(ex.Message);
                    _logger.LogInformation(ex.Message);
                }
            }
        }
    }

    public class PositionsPolling : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Timer _timer;
        private int pollingInterval = 1;

        public PositionsPolling(ILogger<PositionsPolling> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var settingService = scope.ServiceProvider.GetService<ISettingService>();
                var backupService = scope.ServiceProvider.GetService<IBackupService>();

                var breezyToken = (await settingService.Get()).BreezyToken;

                await backupService.GetCountries();
                await backupService.GetCompanies(breezyToken);
                await backupService.GetPositions(breezyToken);
                await backupService.GetCandidates(breezyToken);
            }

            _timer = new Timer(
                PollForPositions,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(pollingInterval)
            );
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void PollForPositions(object state)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var settingService = scope.ServiceProvider.GetService<ISettingService>();
                var backupService = scope.ServiceProvider.GetService<IBackupService>();
                
                var breezyToken = (await settingService.Get()).BreezyToken;
                
                await backupService.GetPositions(breezyToken);
            }

            _logger.LogInformation($"WAITING {pollingInterval} MINUTES");
        }
    }
}
