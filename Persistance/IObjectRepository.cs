using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Cocobot.Persistance
{
    public interface IObjectRepository
    {
        void Upsert<T>(T entity) where T : class, IEntity;
        void Delete<T>(T entity) where T : class, IEntity;
        void Delete<T>(ulong id) where T : class, IEntity;
        bool Exists<T>(ulong id) where T : class, IEntity;
        IQueryable<T> GetWhere<T>(Expression<Func<T, bool>> predicate) where T : class, IEntity;
        IQueryable<T> GetAll<T>() where T : class, IEntity;
        T GetById<T>(ulong id) where T : class, IEntity;
        IEnumerable<T> GetById<T>(IEnumerable<ulong> ids) where T : class, IEntity;
    }
}
