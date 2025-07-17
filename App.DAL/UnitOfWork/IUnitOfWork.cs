using App.Commons.ResponseModel;
using App.DAL.Interfaces;

namespace App.DAL.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepoBase<T> GetRepo<T>() where T : class;
        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollBackAsync();
        Task<BaseResponseModel> SaveAsync();
    }
}
