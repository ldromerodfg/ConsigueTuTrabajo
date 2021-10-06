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
using Microsoft.Data.SqlClient;
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
        private readonly IPositionService _positionService;
        private readonly ICandidateService _candidateService;
        private readonly IStateService _stateService;
        private readonly ICityService _cityService;
        private readonly ISettingService _settingService;
        private readonly ICountryService _countryService;
        private readonly IPositionTypeService _positionTypeService;
        private readonly ICompanyService _companyService;
        private readonly ICandidateStageService _candidateStageService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private TimeSpan requestTimeout = TimeSpan.FromMinutes(2);
        private const int maxTries = 5;
        private const int awaitTime = 60000;

        public BackupService(ILogger<BackupService> logger, DefaultContext dbContext,
            IConfiguration configuration, IPositionService positionService, IStateService stateService,
            ICityService cityService, IPositionTypeService positionTypeService, ISettingService settingService,
            ICountryService countryService, ICompanyService companyService, ICandidateService candidateService,
            ICandidateStageService candidateStageService, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
            _positionService = positionService;
            _candidateService = candidateService;
            _stateService = stateService;
            _cityService = cityService;
            _positionTypeService = positionTypeService;
            _settingService = settingService;
            _companyService = companyService;
            _countryService = countryService;
            _candidateStageService = candidateStageService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task GetBreezyToken()
        {
            string breezyToken = null;

            int tries = 1;

            while (tries < maxTries)
            {
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

                        _logger.LogInformation("Requesting for Breezy token...");

                        var response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("SUCCESS");

                            var responseBody = await response.Content.ReadAsStringAsync();

                            var userInfo = JsonConvert.DeserializeObject<dynamic>(responseBody);

                            breezyToken = (string)userInfo.access_token;

                            try
                            {
                                var setting = await _settingService.GetAsync();
                                setting.BreezyToken = breezyToken;
                                await _settingService.UpdateAsync(setting);
                                break;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.Message);
                            }
                        }
                        else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogInformation($"TOO MANY REQUESTS. WAITING 1 MINUTE");
                            Thread.Sleep(60000);

                            if (tries == maxTries)
                            {
                                _logger.LogInformation($"REQUESTS LIMIT REACHED.");
                                break;
                            }
                            tries++;
                        }
                        else
                        {
                            if (tries == maxTries)
                            {
                                break;
                            }
                            tries++;
                            _logger.LogInformation($"REQUEST UNSUCCESSFUL. HttpStatus: {response.StatusCode}");
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        if (!ex.CancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation($"REQUEST TIMEOUT.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex.Message);
                    }
                }
            }
        }

        public async Task GetCountries()
        {
            var dbCountries = await _countryService.GetAllAsync();

            if (!dbCountries.Any())
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
                            foreach (var c in countries)
                            {
                                var country = new Country
                                {
                                    Name = c.name,
                                    Code = c.alpha2Code
                                };

                                await _countryService.CreateAsync(country);
                            }
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
            var dbCompanies = await _companyService.GetAllAsync();

            if (!dbCompanies.Any())
            {
                _logger.LogInformation("FETCHING COMPANIES...");

                var _setting = await _settingService.GetAsync();

                var tries = 1;

                while (tries < maxTries)
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = requestTimeout;
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("Authorization", _setting.BreezyToken);

                        try
                        {
                            var companyResponse = await client.GetAsync("https://api.breezy.hr/v3/companies");

                            if (companyResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var companyResponseBody = await companyResponse.Content.ReadAsStringAsync();

                                var companies = JsonConvert.DeserializeObject<dynamic[]>(companyResponseBody);

                                if (companies != null && companies.Any())
                                {
                                    foreach (var company in companies)
                                    {
                                        var companyId = (string)company._id;

                                        if (!dbCompanies.Any(x => x.BreezyId == companyId))
                                        {
                                            var c = new Company
                                            {
                                                BreezyId = companyId,
                                                Name = company.name,
                                                FriendlyId = company.friendly_id,
                                                Created = DateTime.Parse((string)company.creation_date).ToUniversalTime(),
                                                Updated = DateTime.Parse((string)company.updated_date).ToUniversalTime(),
                                                MemberCount = (int)company.member_count,
                                                Initial = company.initial
                                            };

                                            await _companyService.CreateAsync(c);
                                        }
                                    }
                                    break;
                                }
                            }
                            else if (companyResponse.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                await GetBreezyToken();

                                _setting = await _settingService.GetAsync();

                                if (tries == maxTries)
                                {
                                    _logger.LogInformation($"REQUEST LIMIT REACHED.");
                                    break;
                                }

                                tries++;
                            }
                            else
                            {
                                _logger.LogInformation($"UNSUCCESSFUL. HTTP CODE: {companyResponse.StatusCode}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation(ex.Message);
                        }
                    }
                }
            }
        }

        public async Task GetPositions(string breezyToken)
        {
            _logger.LogInformation("FETCHING POSITIONS...");

            var _setting = await _settingService.GetAsync();

            var tries = 1;

            var positionsToAdd = new List<Position>();
            var positionsToUpdate = new List<Position>();

            var positionStates = new List<string> {
                "published", "draft", "archived", "closed", "pending"
            };

            try
            {
                foreach (var positionState in positionStates)
                {
                    var companies = await _companyService.GetAllAsync();

                    foreach (var company in companies)
                    {
                        using (var scope = _serviceScopeFactory.CreateScope())
                        using (var client = new HttpClient())
                        {
                            var companyService = scope.ServiceProvider.GetService<ICompanyService>();
                            var positionService = scope.ServiceProvider.GetService<IPositionService>();
                            var positionTypeService = scope.ServiceProvider.GetService<IPositionTypeService>();

                            while (tries < maxTries)
                            {
                                client.Timeout = requestTimeout;
                                client.DefaultRequestHeaders.Clear();
                                client.DefaultRequestHeaders.Add("Authorization", _setting.BreezyToken);

                                var positionResponse = await client.GetAsync($"https://api.breezy.hr/v3/company/{company.BreezyId}/positions?state={positionState}");

                                if (positionResponse.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    var positionResponseBody = await positionResponse.Content.ReadAsStringAsync();

                                    var positions = JsonConvert.DeserializeObject<dynamic[]>(positionResponseBody);

                                    var dbPositions = await positionService.GetAllAsync(state: positionState);

                                    var newPositions = positions != null && positions.Any()
                                        ? positions.Where(x => !dbPositions.Select(x => x.BreezyId.Trim()).Contains(((string)x._id).Trim()))
                                        : new List<dynamic>();

                                    if (newPositions.Any())
                                    {
                                        _logger.LogInformation($"NEW {positionState} POSITIONS FOUND");
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"NO NEW {positionState} POSITIONS");
                                    }

                                    foreach (var position in newPositions)
                                    {
                                        positionsToAdd.Add(await BuildPosition(position, company.Id));
                                    }

                                    var updatedPositions = positions.Where(x =>
                                            !string.IsNullOrEmpty((string)x.updated_date)
                                                ? DateTime.Parse((string)x.updated_date) > DateTime.Now.AddHours(-1)
                                                : false);

                                    foreach (var position in updatedPositions)
                                    {
                                        positionsToAdd.Add(await BuildPosition(position, company.Id, update: true));
                                    }

                                    break;
                                }
                                else if (positionResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                {
                                    _logger.LogInformation($"REQUEST LIMIT REACHED. WAITING {awaitTime / 1000} SECONDS...");
                                    Thread.Sleep(awaitTime);

                                    tries++;
                                }
                                else if (positionResponse.StatusCode == HttpStatusCode.Unauthorized)
                                {
                                    _logger.LogInformation($"UNAUTHORIZED");
                                    await GetBreezyToken();
                                    _setting = await _settingService.GetAsync();

                                    if (tries == maxTries)
                                    {
                                        _logger.LogInformation($"REQUESTS LIMIT REACHED");
                                        throw new Exception("REQUESTS LIMIT REACHED");
                                    }

                                    tries++;
                                    _logger.LogInformation($"TRYING AGAIN ({tries} of {maxTries})");
                                }
                                else
                                {
                                    _logger.LogInformation($"UNSUCCESSFUL. HTTPCODE: {positionResponse.StatusCode}");

                                    if (tries == maxTries)
                                    {
                                        _logger.LogInformation($"REQUESTS LIMIT REACHED");
                                        throw new Exception("REQUESTS LIMIT REACHED");
                                    }

                                    tries++;

                                    _logger.LogInformation($"TRYING AGAIN ({tries} of {maxTries})");
                                }
                            }
                        }
                    }
                }

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var positionService = scope.ServiceProvider.GetService<IPositionService>();

                    if (positionsToAdd.Any())
                    {
                        await positionService.CreateRangeAsync(positionsToAdd);
                    }

                    if (positionsToUpdate.Any())
                    {
                        await positionService.UpdateRangeAsync(positionsToUpdate);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
            }
        }

        public async Task GetCandidates(string breezyToken)
        {
            var dbCandidates = (await _candidateService.GetAllAsync()).ToList();

            if (!dbCandidates.Any())
            {
                _logger.LogInformation("FETCHING CANDIDATES...");

                var _setting = await _settingService.GetAsync();

                try
                {
                    var iPosition = 0;

                    var positions = await _positionService.GetAllAsync();

                    foreach (var position in positions)
                    {
                        iPosition++;

                        _logger.LogInformation($"LOOPING POSITION {iPosition} ({position.BreezyId})...");

                        var page = 1;

                        var tries = 1;

                        while (tries < maxTries)
                        {
                            var candidateResponse = new HttpResponseMessage();

                            try
                            {
                                using (var client = new HttpClient())
                                {
                                    client.Timeout = requestTimeout;

                                    client.DefaultRequestHeaders.Clear();
                                    client.DefaultRequestHeaders.Add("Authorization", _setting.BreezyToken);

                                    var uri = $"https://api.breezy.hr/v3/company/{position.Company.BreezyId}/position/{position.BreezyId}/candidates?page_size=50&page={page}";

                                    _logger.LogInformation($"(POSITION {iPosition}) SENDING REQUEST TO {uri}...");

                                    candidateResponse = await client.GetAsync(uri);

                                    if (candidateResponse.IsSuccessStatusCode)
                                    {
                                        _logger.LogInformation($"SUCCESS");

                                        var candidateResponseBody = await candidateResponse.Content.ReadAsStringAsync();

                                        var candidates = JsonConvert.DeserializeObject<dynamic[]>(candidateResponseBody);

                                        if (candidates == null || !candidates.Any())
                                        {
                                            _logger.LogInformation($"NO CANDIDATES FOR POSITION {position.BreezyId}. MOVING TO NEXT POSITION...");
                                            break;
                                        }

                                        int n = 1;

                                        foreach (var candidate in candidates)
                                        {
                                            var candidateId = (string)candidate._id;

                                            var candidateStageId = (string)candidate.stage.id;

                                            using (var scope = _serviceScopeFactory.CreateScope())
                                            {
                                                var candidateStageService = scope.ServiceProvider.GetService<ICandidateStageService>();
                                                var candidateService = scope.ServiceProvider.GetService<ICandidateService>();

                                                var candidateStage = await candidateStageService.GetByBreezyIdAsync(candidateStageId);

                                                if (candidateStage == null)
                                                {
                                                    candidateStage = new CandidateStage
                                                    {
                                                        BreezyId = candidateStageId,
                                                        Name = (string)candidate.stage.name
                                                    };

                                                    await candidateStageService.CreateAsync(candidateStage);
                                                }

                                                if (candidateStage == null)
                                                {
                                                    var c = new Candidate
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
                                                    };

                                                    await candidateService.CreateAsync(c);

                                                    n++;
                                                }
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
                                                _logger.LogInformation(ex.Message);
                                            }

                                            try
                                            {
                                                if (candidate.resume != null)
                                                {
                                                    using (WebClient webClient = new WebClient())
                                                    {
                                                        webClient.Headers.Add("Authorization", _setting.BreezyToken);

                                                        _logger.LogInformation($"FETCHING CV FROM {(string)candidate.resume.url}...");
                                                        byte[] arr = webClient.DownloadData((string)candidate.resume.url);

                                                        _logger.LogInformation($"SUCCESS");

                                                        var filePath = $"./wwwroot/cv/cv_{(string)candidate._id}.pdf";

                                                        if (!Directory.Exists(filePath))
                                                        {
                                                            _logger.LogInformation($"WRITING CV FILE {filePath}");
                                                            File.WriteAllBytes(filePath, arr);
                                                        }
                                                        else
                                                        {
                                                            _logger.LogInformation($"CV FILE {filePath} ALREADY EXISTS");
                                                        }
                                                    }
                                                }
                                            }
                                            catch (WebException ex)
                                            {
                                                _logger.LogInformation(ex.Message);
                                            }
                                        }

                                        page++;
                                    }
                                    else if (candidateResponse.StatusCode == HttpStatusCode.Unauthorized)
                                    {
                                        _logger.LogInformation($"UNAUTHORIZED");
                                        await GetBreezyToken();
                                        _setting = await _settingService.GetAsync();

                                        if (tries == maxTries)
                                        {
                                            _logger.LogInformation($"REQUESTS LIMIT REACHED");
                                            throw new Exception("REQUESTS LIMIT REACHED");
                                        }

                                        tries++;
                                        _logger.LogInformation($"TRYING AGAIN ({tries} of {maxTries})");
                                    }
                                    else if (candidateResponse.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                                    {
                                        _logger.LogInformation($"REQUEST LIMIT REACHED. WAITING {awaitTime / 1000} SECONDS...");
                                        Thread.Sleep(awaitTime);

                                        tries++;
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"UNSUCCESSFUL REQUEST. STATUS CODE {candidateResponse.StatusCode}");
                                        Thread.Sleep(awaitTime);

                                        tries++;
                                    }
                                }

                                if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");
                            }
                            catch (TaskCanceledException ex)
                            {
                                if (ex.CancellationToken.IsCancellationRequested)
                                {
                                    _logger.LogInformation($"REQUEST TIMEOUT. ({tries} of {maxTries})...");
                                }
                                else
                                {
                                    _logger.LogInformation(ex.Message);
                                }

                                if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");

                                Thread.Sleep(awaitTime);

                                _logger.LogInformation($"REQUEST TIMEOUT. WAITING {awaitTime / 1000} SECONDS...");

                                tries++;
                            }
                            catch (HttpRequestException ex)
                            {
                                _logger.LogInformation(ex.Message);

                                if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");

                                Thread.Sleep(awaitTime);

                                _logger.LogInformation($"WAITING {awaitTime / 1000} SECONDS...");

                                tries++;
                            }
                            catch (SqlException ex)
                            {
                                _logger.LogInformation(ex.Message);

                                if (tries == maxTries) throw new Exception("REQUEST TRIES LIMIT REACHED");

                                Thread.Sleep(awaitTime);

                                _logger.LogInformation($"WAITING {awaitTime / 1000} SECONDS...");

                                tries++;
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex.Message);
                }
            }
        }

        private async Task<Position> BuildPosition(dynamic position, int idCompany, bool update = false)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var cityService = scope.ServiceProvider.GetService<ICityService>();
                var stateService = scope.ServiceProvider.GetService<IStateService>();
                var countryService = scope.ServiceProvider.GetService<ICountryService>();
                var positionTypeService = scope.ServiceProvider.GetService<IPositionTypeService>();
                var positionService = scope.ServiceProvider.GetService<IPositionService>();

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
                    ? await cityService.GetByNameAsync(cityName)
                    : null;

                if (city == null && cityName != null)
                {
                    var state = await stateService.GetByCodeAsync(stateCode);

                    if (state == null && stateCode != null)
                    {
                        var country = await countryService.GetByCodeAsync(countryCode);

                        state = new State
                        {
                            Name = position.location.state.name,
                            Code = position.location.state.id,
                            CountryId = country.Id
                        };

                        state = await stateService.CreateAsync(state);

                        city = new City
                        {
                            Name = position.location.city,
                            StateId = state.Id
                        };

                        city = await cityService.CreateAsync(city);
                    }
                }

                var positionTypeId = position.type != null
                    ? (string)position.type.id
                    : null;

                var positionType = await positionTypeService.GetByCodeAsync(positionTypeId);

                if (positionTypeId != null && positionType == null)
                {
                    positionType = new PositionType
                    {
                        Name = position.type.name,
                        Code = positionTypeId
                    };

                    positionType = await positionTypeService.CreateAsync(positionType);
                }

                _logger.LogInformation($"ADDING POSITION {position._id}...");

                if (update)
                {
                    Position p = await positionService.GetByBreezyIdAsync((string)position._id);

                    p.State = position.state;
                    p.Name = position.name;
                    p.Education = position.education;
                    p.Description = position.description;
                    p.Created = DateTime.Now;
                    p.CityId = city != null ? city.Id : null;
                    p.CompanyId = idCompany;
                    p.PositionTypeId = positionType != null ? positionType.Id : null;

                    return p;
                }
                else
                {
                    return new Position
                    {
                        BreezyId = position._id,
                        State = position.state,
                        Name = position.name,
                        Education = position.education,
                        Description = position.description,
                        Created = DateTime.Now,
                        CityId = city != null ? city.Id : null,
                        CompanyId = idCompany,
                        PositionTypeId = positionType != null ? positionType.Id : null
                    };
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Thread.Sleep(60000);

            _timer = new Timer(
                PollForPositions,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMinutes(pollingInterval)
            );
            return Task.CompletedTask;
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
                var candidateService = scope.ServiceProvider.GetService<ICandidateService>();

                var breezyToken = (await settingService.GetAsync()).BreezyToken;

                await backupService.GetPositions(breezyToken);
            }

            _logger.LogInformation($"WAITING {pollingInterval} MINUTES");
        }
    }
}
