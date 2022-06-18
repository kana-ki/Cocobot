using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Cocobot.Persistance
{
    // This repository should remain singleton. While it will work fine none-singleton, LiteDb opens,
    // locks, unlocks and closes the target database file as the LiteDatabase object is instantiated
    // and disposed, all of which are very expensive. Page caching is also kept at LiteDatabase level,
    // so is lost on disposing it. Additionally, LiteDb is thread-safe. So, it's considerably more 
    // performant to keep a continual instance of LiteDatabase. 
    // For more information, see here: https://github.com/mbdavid/LiteDB/wiki/Concurrency
    public class LiteDbRepository : IObjectRepository, IDisposable
    {

        private readonly LiteDatabase _repository;

        public LiteDbRepository(string strConnectionString)
        {
            var connectionString = new ConnectionString(strConnectionString);
            if (!Path.IsPathRooted(connectionString.Filename))
            {
                var assembliesPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location!).LocalPath);
                connectionString.Filename = Path.Combine(assembliesPath, connectionString.Filename);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(connectionString.Filename));
            this._repository = new LiteDatabase(connectionString);
        }

        public void Upsert<T>(T entity) where T : class, IEntity =>
            this._repository.GetCollection<T>().Upsert(entity.Id.ToString(), entity);

        public void Delete<T>(T entity) where T : class, IEntity =>
            this._repository.GetCollection<T>().Delete(entity.Id.ToString());

        public void Delete<T>(ulong id) where T : class, IEntity =>
            this._repository.GetCollection<T>().Delete(id.ToString());

        public IQueryable<T> GetWhere<T>(Expression<Func<T, bool>> predicate) where T : class, IEntity =>
            this._repository.GetCollection<T>().Find(predicate).AsQueryable();

        public IQueryable<T> GetAll<T>() where T : class, IEntity =>
            this._repository.GetCollection<T>().FindAll().AsQueryable();

        public T GetById<T>(ulong id) where T : class, IEntity =>
            this._repository.GetCollection<T>().FindById(id.ToString());

        public IEnumerable<T> GetById<T>(IEnumerable<ulong> ids) where T : class, IEntity
        {
            var array = new BsonArray(ids.Select(id => new BsonValue(id.ToString())));
            return this._repository.GetCollection<T>().Find(Query.In("_id", array));
        }

        public bool Exists<T>(ulong id) where T : class, IEntity =>
            this._repository.GetCollection<T>().FindById(id.ToString()) != null;

        public void Dispose() =>
            this.Dispose(true);

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._repository.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        ~LiteDbRepository() =>
            this.Dispose(false);

    }
}
