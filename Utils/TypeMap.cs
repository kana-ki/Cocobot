using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Cocobot.Utils
{
    internal class TypeMap<T> : IEnumerable<T> where T : class
    {

        private readonly Dictionary<string, Type> _typeMap = new ();
        private readonly IServiceProvider _serviceProvider;

        public TypeMap(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public TypeMap<T> Add<A>(string key) where A : T {
            this._typeMap.Add(key, typeof(A));
            return this;
        }

        public Type Get(string key)
        {
            return this._typeMap[key];
        }

        public bool ContainsKey(string key) =>
            this._typeMap.ContainsKey(key);

        public T Activate(string key) {
            var hasKey = this._typeMap.ContainsKey(key);
            if (!hasKey) {
                return default;
            }
            return ActivatorUtilities.CreateInstance(this._serviceProvider, this._typeMap[key]) as T;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var key in this._typeMap.Keys)
                yield return this.Activate(key);
        }

        IEnumerator IEnumerable.GetEnumerator() => 
            this.GetEnumerator();
    }
}
