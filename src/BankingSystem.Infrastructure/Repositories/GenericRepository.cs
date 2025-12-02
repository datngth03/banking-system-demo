namespace BankingSystem.Infrastructure.Repositories;

using BankingSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using BankingSystem.Infrastructure.Persistence;

public class GenericRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly BankingSystemDbContext _context;

    public GenericRepository(BankingSystemDbContext context)
    {
        _context = context;
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        _context.Set<T>().Update(entity);
        await Task.CompletedTask;
        return entity;
    }

    public async Task DeleteAsync(T entity)
    {
        _context.Set<T>().Remove(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
