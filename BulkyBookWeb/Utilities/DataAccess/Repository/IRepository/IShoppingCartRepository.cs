using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IShoppingCartRepositoy : IRepository<ShoppingCart>
    {
        Task<int> IncrementCount(ShoppingCart shoppingCart, int count);
        Task<int> DecrementCount(ShoppingCart shoppingCart, int count);
    }
}
