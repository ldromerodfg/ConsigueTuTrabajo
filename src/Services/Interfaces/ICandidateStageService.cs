using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface ICandidateStageService
    {
        Task<IEnumerable<CandidateStage>> GetAllAsync();
        Task<CandidateStage> GetAsync(int id);
        Task<CandidateStage> GetByBreezyIdAsync(string breezyId);
        Task<CandidateStage> CreateAsync(CandidateStage entity);
        Task UpdateAsync(CandidateStage entity);
        Task DeleteAsync(int id);
    }
}