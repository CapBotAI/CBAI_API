using App.Commons;
using App.Commons.ResponseModel;
using App.DAL.Context;
using App.DAL.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace App.DAL.UnitOfWork
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly MyDbContext _context;
		private readonly IServiceProvider _serviceProvider;
		private IDbContextTransaction _transaction;
		public UnitOfWork(MyDbContext masterContext, IServiceProvider serviceProvider)
		{
			_context = masterContext;
			_serviceProvider = serviceProvider;
		}
		public async Task BeginTransactionAsync()
		{
			_transaction = await _context.Database.BeginTransactionAsync();
		}

		public async Task CommitTransactionAsync()
		{
			try
			{
				await _transaction.CommitAsync();
			}
			catch
			{
				await _transaction.RollbackAsync();
			}
			finally
			{
				await _transaction.DisposeAsync();
				_transaction = null!;
			}
		}

		private bool disposed = false;
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					_context.Dispose();
				}
				this.disposed = true;
			}
		}

		public IRepoBase<T> GetRepo<T>() where T : class
		{
			return _serviceProvider.GetRequiredService<IRepoBase<T>>();
		}

		public async Task RollBackAsync()
		{
			await _transaction.RollbackAsync();
			await _transaction.DisposeAsync();
			_transaction = null!;
		}

		public async Task SaveChangesAsync()
		{
			await _context.SaveChangesAsync();
		}

		public async Task<BaseResponseModel> SaveAsync()
		{
			var update = await _context.SaveChangesAsync() > 0;
			if (!update) return new BaseResponseModel { IsSuccess = false, Message = ConstantModel.SaveDataFailed };
			return new BaseResponseModel { IsSuccess = true, Message = ConstantModel.SaveDataSuccess };
		}
	}
}
