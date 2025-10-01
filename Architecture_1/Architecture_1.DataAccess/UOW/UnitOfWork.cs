using Google;
using Architecture_1.DataAccess.Data;
using Architecture_1.DataAccess.Repositories.interfaces;

namespace Architecture_1.DataAccess.UOW;
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(
        AppDbContext dbContext
        )
    {
        _dbContext = dbContext;
    }

    public int Complete()
    {
        return _dbContext.SaveChanges();
    }
}
