using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface ICandidateService
    {
        Task<IEnumerable<Candidate>> GetAllAsync(string email = null, string origin = null,
            string breezyId = null);
        Task<Candidate> GetAsync(int id);
        Task<Candidate> CreateAsync(Candidate entity);
        Task UpdateAsync(Candidate entity);
        Task DeleteAsync(int id);

        Task<Candidate> GetByEmailOrPhone(string email = null, string phoneNumber = null);
    }
}