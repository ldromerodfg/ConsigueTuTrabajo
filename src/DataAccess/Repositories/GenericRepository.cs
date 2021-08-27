using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Contexts;
using DataAccess.Interfaces;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly DefaultContext _dbContext;
        private readonly DbSet<T> _entity;

        public GenericRepository(DefaultContext context)
        {
            _dbContext = context;
            _entity = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _entity.ToListAsync();
        }

        public async Task<T> GetById(int id)
        {
            return await _entity.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
