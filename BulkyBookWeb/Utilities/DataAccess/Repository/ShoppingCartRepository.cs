using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepositoy
    {
        private readonly ApplicationDbContext _db;
        public ShoppingCartRepository(ApplicationDbContext db): base(db)
        {
            _db = db;
        }

        public async Task<int> DecrementCount(ShoppingCart shoppingCart, int count)
        {
            shoppingCart.Count -= count;
            return await Task.Run(() => shoppingCart.Count);
        }

        public async Task<int> IncrementCount(ShoppingCart shoppingCart, int count)
        {
            shoppingCart.Count += count;

            return await Task.Run(() => shoppingCart.Count);
        }
    }
}
