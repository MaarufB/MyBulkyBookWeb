using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork? _unitOfWork;
        [BindProperty]
        public OrderVM? orderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            return await Task.Run(() => View());
        }

        public async Task<IActionResult> Details(int orderId)
        {
            orderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAllAsync(u => u.OrderId == orderId, includeProperties: "Product"),
            };

            return await Task.Run(() => View(orderVM));
        }

        [ActionName("Details")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details_PAY_NOW()
        {
            orderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == orderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            orderVM.OrderDetail = _unitOfWork.OrderDetail.GetAllAsync(u => u.OrderId == orderVM.OrderHeader.Id, includeProperties: "Product");


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
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderid={orderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?{orderVM.OrderHeader.Id}",
            };

            foreach (var item in orderVM.OrderDetail)
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

            orderVM.OrderHeader.SessionId = session.Id;
            orderVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
            _unitOfWork.OrderHeader.UpdateStripePaymentID(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
           await  _unitOfWork.SaveAsync();
            Response.Headers.Add("Location", session.Url);
         
            return new StatusCodeResult(303);
        }

        public async Task<IActionResult> PaymentConfirmation(int orderHeaderid)
        {
            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == orderHeaderid);


            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = await service.GetAsync(orderHeader.SessionId);
                var orderHeaderIsNull = orderHeader.OrderStatus;
                // Check the stripe status
                // To elminate the warning for null
                var orderStatus =  orderHeader.OrderStatus != null ? orderHeader.OrderStatus : "Null";

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderid, orderStatus, SD.PaymentStatusApproved);
                    await _unitOfWork.SaveAsync();
                }
            }



            var shoppingCarts = _unitOfWork.ShoppingCart.GetAllAsync(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();


            await _unitOfWork.ShoppingCart.RemoveRangeAsync(shoppingCarts);
            await _unitOfWork.SaveAsync();

            return await Task.Run(() => View(orderHeaderid));

        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderDetail(int orderId)
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == orderVM.OrderHeader.Id, tracked:false);
            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = orderVM.OrderHeader.City;
            orderHeaderFromDb.State = orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;
            
            if(orderVM.OrderHeader.Carrier != null)
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }
            if(orderVM.OrderHeader.TrackingNumber != null)
            {
                orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "Order Details Upated Successfully!";


            return await Task.Run(() => RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id }));
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartProcessing(int orderId)
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            await _unitOfWork.SaveAsync();
            TempData["Success"] = "Order Status Upated Successfully!";

            return await Task.Run(() => RedirectToAction("Details", "Order", new { orderId = orderVM.OrderHeader.Id }));
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u=>u.Id==orderVM.OrderHeader.Id, tracked: false);
            orderHeader.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = orderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeader);  
            await _unitOfWork.SaveAsync();
            TempData["Success"] = "Order Shipped Successfully!";

            return await Task.Run(() => RedirectToAction("Details", "Order", new { orderId = orderVM.OrderHeader.Id }));
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefaultAsync(u => u.Id == orderVM.OrderHeader.Id, tracked: false);
            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(options);
                
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            await _unitOfWork.SaveAsync();
            TempData["Success"] = "Order Cancelled Successfully!";

            return await Task.Run(() => RedirectToAction("Details", "Order", new { orderId = orderVM.OrderHeader.Id }));
        }

        #region API CALLS
        [HttpGet]
        public async Task<IActionResult> GetAll(string? status)
        {
            IEnumerable<OrderHeader> orderHeaders;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeader.GetAllAsync(includeProperties: "ApplicationUser");
            }
            else 
            {
                var claimsIdentity = (ClaimsIdentity?)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                orderHeaders = _unitOfWork.OrderHeader.GetAllAsync(u => u.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser");
            }            
            
            switch (status)
            
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }

            return await Task.Run(() => Json(new {data = orderHeaders}));
        }
        #endregion
    }
}
