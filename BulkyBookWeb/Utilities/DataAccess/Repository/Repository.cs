﻿using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            //_db.ShoppingCarts.AsNoTracking().
            //_db.ShoppingCarts.Include(u => u.Product).Include(u=> u.Product);
            this.dbSet = _db.Set<T>();  
        }

        public async Task AddAsync(T entity)
        {
            await dbSet.AddAsync(entity);
        }
        // inludeProp - "Category, CoverType"
        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter=null, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if(filter != null)
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property);
                }
            }
            return query.ToList(); 
        }

        public T GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = true)
        {
            IQueryable<T> query;

            if (tracked)
            {
                query = dbSet;
            }
            else
            {
                query = dbSet.AsNoTracking();
            }


            query = query.Where(filter);

            if (includeProperties != null)
            {
                foreach (var property in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(property);
                }
            }
            return query.FirstOrDefault(); // To eleminate the warning the best approach to use is query.First()
        }

        public async Task RemoveAsync(T entity)
        {
            dbSet.Remove(entity);
        }

        public async Task RemoveRangeAsync(IEnumerable<T> entity)
        {
            dbSet.RemoveRange(entity);
        }
    }
}
