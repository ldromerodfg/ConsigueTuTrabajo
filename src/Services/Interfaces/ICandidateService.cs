using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface ICandidateService
    {
        Task<IEnumerable<Candidate>> GetAllAsync(string email = null, string origin = null, string breezyId = null);
        Task<Candidate> GetAsync(int id);
    }
}