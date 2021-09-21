using System.Threading.Tasks;
using Domain.Entities;

namespace Service.Interfaces
{
    public interface ISettingService
    {
        Task<Setting> GetAsync();
        Task UpdateAsync(Setting entity);
    }
}