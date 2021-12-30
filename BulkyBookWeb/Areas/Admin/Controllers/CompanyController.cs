using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
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
                _unitOfWork.Company.Add(obj);
                _unitOfWork.Save();
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
                company = _unitOfWork.Company.GetFirstOrDefault(u => u.Id == id);
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
                    _unitOfWork.Company.Add(obj);
                    TempData["success"] = "Company created successfully!";
                }
                else
                {
                     _unitOfWork.Company.Update(obj);
                    TempData["success"] = "Company updated successfully!";
                }

                _unitOfWork.Save();


                return RedirectToAction("Index");
            }

            return View(obj);
        }
        

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _unitOfWork.Company.GetAll();
            return Json(new { data = companyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefault(c => c.Id == id);

            if (obj == null) return Json(new {success = false, message="Error while deleting"});

            _unitOfWork.Company.Remove(obj);
            _unitOfWork.Save();

            return Json(new {success = true, message = "Company successfully deleted"});

        }

        #endregion
    }
}
