using Architecture_1.DataAccess.Repositories;
using Architecture_1.DataAccess.Repositories.interfaces;

namespace Architecture_1.DataAccess.UOW;
public interface IUnitOfWork
{
    int Complete();
}
