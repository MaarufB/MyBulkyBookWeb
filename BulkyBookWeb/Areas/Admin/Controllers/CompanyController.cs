using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using BulkyBook.Utility;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CompanyController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            //IEnumerable<Product> objPoductList = _unitOfWork.Product.GetAll();
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Company obj)
        {
            if (!ModelState.IsValid) ModelState.AddModelError("name", "Company name is not valid");

            if (ModelState.IsValid)
            {
                _unitOfWork.Company.AddAsync(obj);
                _unitOfWork.SaveAsync();
                TempData["success"] = "Company created successfully!";

                return RedirectToAction("Index");
            }

            return View(obj);
        }

        public IActionResult Upsert(int? id)
        {
            Company company = new();

            if (id == null || id == 0)
            {
                // create product

                return View(company);
            }
            else
            {
                //update product
                company = _unitOfWork.Company.GetFirstOrDefaultAsync(u => u.Id == id);
                return View(company);
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj)
        {
            
            if (ModelState.IsValid)
            {
                

                if(obj.Id == 0)
                {
                    _unitOfWork.Company.AddAsync(obj);
                    TempData["success"] = "Company created successfully!";
                }
                else
                {
                     _unitOfWork.Company.Update(obj);
                    TempData["success"] = "Company updated successfully!";
                }

                _unitOfWork.SaveAsync();


                return RedirectToAction("Index");
            }

            return View(obj);
        }
        

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _unitOfWork.Company.GetAllAsync();
            return Json(new { data = companyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefaultAsync(c => c.Id == id);

            if (obj == null) return Json(new {success = false, message="Error while deleting"});

            _unitOfWork.Company.RemoveAsync(obj);
            _unitOfWork.SaveAsync();

            return Json(new {success = true, message = "Company successfully deleted"});

        }

        #endregion
    }
}
