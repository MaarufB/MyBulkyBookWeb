﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    // This is a generic repository
    public interface IRepository<T> where T : class
    {
        // T - Category
        T GetFirstOrDefault(Expression<Func<T, bool>> filter,string? includeProperties = null); // Change GetFirstOrDefault to Find()
        IEnumerable<T> GetAll(string? includeProperties = null);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entity);
    
    }
}