using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface IBackupService
    {
        Task GetBreezyToken();
        Task GetCountries();
        Task GetCompanies(string breezyId);
        Task GetPositions(string breezyId);
        Task GetCandidates(string breezyId);
    }
}