using System;
using System.Linq.Expressions;

namespace ObjectDatabase
{
    public static class ModelFactory<T> where T : IDataModel, new()
    {
        public static Func<T> Factory { get; } = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
    }
}