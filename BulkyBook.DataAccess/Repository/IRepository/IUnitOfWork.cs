﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    // Try to Enherit IDisposable. This is from Mosh
    public interface IUnitOfWork
    {
        ICategoryRepository Category{ get; }
        ICoverTypeRepository CoverType{ get; }  
        IProductRepository Product { get; }
        void Save();
    }
}