﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBook.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType");
            return View(productList); 
        }

        // Wondering why this is not working try to solve this for the future
        //public IActionResult Details(int id)
        //{
        //    ShoppingCart cartObj = new()
        //    {
        //        Count = 1,
        //        Product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == id, includeProperties: "Category,CoverType"),
        //    };

        //    return View(cartObj);
        //}

        public IActionResult Details(int productId)
        {
            ShoppingCart cartObj = new()
            {
                Count = 1,
                ProductId = productId,
                Product = _unitOfWork.Product.GetFirstOrDefault(u => u.Id == productId, includeProperties: "Category,CoverType"),
            };

            return View(cartObj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCart.ApplicationUserId = claim.Value;

            var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(
                                  u => u.ApplicationUserId == claim.Value && 
                                  u.ProductId == shoppingCart.ProductId);

            if(cartFromDb == null)
            {
                await _unitOfWork.ShoppingCart.AddAsync(shoppingCart);
                await _unitOfWork.SaveAsync();
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);
            }
            else
            {
                _unitOfWork.ShoppingCart.IncrementCount(cartFromDb, shoppingCart.Count);
                await _unitOfWork.SaveAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}