using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork? _unitOfWork;
        [BindProperty]
        public ShoppingCartVM? shoppingCartVM { get; set; }  
        public int OrderTotal { get; set; }
        private readonly IEmailSender _emailSender;
        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        //[AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var claim = await Task.Run(() => claimsIdentity.FindFirst(ClaimTypes.NameIdentifier));

            shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = await Task.Run(() => _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == claim.Value, includeProperties: "Product")),
                OrderHeader = new()
            };
            foreach(var cart in shoppingCartVM.ListCart)
            {
                cart.Price = await GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }


            return await Task.Run(() => View(shoppingCartVM));
        }

        public async Task<IActionResult> Summary()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var claim = await Task.Run(() => claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier));

            shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = await Task.Run(() => _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId== claim.Value, includeProperties: "Product")),
                OrderHeader= new()
            };
            
            shoppingCartVM.OrderHeader.ApplicationUser = await Task.Run(() => _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(
                u => u.Id==claim.Value));
             shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = await GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return await Task.Run(() => View(shoppingCartVM));
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var claim = await Task.Run(() => claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier));
            
            shoppingCartVM.ListCart = await Task.Run(() => _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == claim.Value, includeProperties: "Product"));

            shoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            shoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = await GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            var applicationUser = await Task.Run(() => _unitOfWork.ApplicationUser.GetFirstOrDefaultAsync(u => u.Id == claim.Value));
            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            await _unitOfWork.OrderHeader.AddAsync(shoppingCartVM.OrderHeader);
            await _unitOfWork.SaveAsync();

            foreach (var cart in shoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderId = shoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                await _unitOfWork.OrderDetail.AddAsync(orderDetail);
                await _unitOfWork.SaveAsync();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {

                //Stripe Setting 
                var domain = "https://localhost:5001/";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + $"customer/cart/index",
                };

                foreach (var item in shoppingCartVM.ListCart)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title,
                            },

                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);
                }


                var service = new SessionService();

                var session = await service.CreateAsync(options);

                shoppingCartVM.OrderHeader.SessionId = session.Id;
                shoppingCartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
                await Task.Run(() => _unitOfWork.OrderHeader.UpdateStripePaymentID(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId));
                await _unitOfWork.SaveAsync();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            else
            {
                return await Task.Run(() => RedirectToAction("OrderConfirmation", "Cart", new { Id = shoppingCartVM.OrderHeader.Id }));
            }

        }

        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var orderHeader = await Task.Run(() => _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == id, includeProperties: "ApplicationUser"));
          

            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = await service.GetAsync(orderHeader.SessionId);
                // Check the stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    await Task.Run(() => _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved));
                    await _unitOfWork.SaveAsync();
                }
            }

            await _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book", "<p>New Order Created</p>");      
           
            var shoppingCarts = await Task.Run(() => _unitOfWork.ShoppingCart.GetAllAsync(u=>u.ApplicationUserId == orderHeader.ApplicationUserId).ToList());
            HttpContext.Session.Clear();
            await _unitOfWork.ShoppingCart.RemoveRangeAsync(shoppingCarts);
            await _unitOfWork.SaveAsync();

            return await Task.Run(() => View(id));

        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await Task.Run(() => _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id == cartId));
            await _unitOfWork.ShoppingCart.IncrementCount(cart, 1);
            await _unitOfWork.SaveAsync();
            
            return await Task.Run(() => RedirectToAction(nameof(Index)));    
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await Task.Run(() => _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id==cartId));
            if(cart.Count <= 1)
            {
                await _unitOfWork.ShoppingCart.RemoveAsync(cart);
                var count =  await Task.Run(() => _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count - 1);
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
                await _unitOfWork.ShoppingCart.DecrementCount(cart, 1);
            }

            await _unitOfWork.SaveAsync();
             
            return await Task.Run(() => RedirectToAction(nameof(Index)));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await Task.Run(() => _unitOfWork.ShoppingCart.GetFirstOrDefaultAsync(u => u.Id == cartId));
            await _unitOfWork.ShoppingCart.RemoveAsync(cart);
            await _unitOfWork.SaveAsync();

            //var count2 = _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == cart.ApplicationUserId);


            var count = await Task.Run(() => _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count);

            HttpContext.Session.SetInt32(SD.SessionCart, count);
             
            return await Task.Run(() => RedirectToAction(nameof(Index)));
        }

        private async Task<double> GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100) 
        {
            if(quantity <= 50)
            {
                return await Task.Run(() => price);
            }
            else
            {
                if(quantity <= 100)
                {
                    return await Task.Run(() => price50);
                }
                return await Task.Run(() => price100);
            }


        }
    }
}
