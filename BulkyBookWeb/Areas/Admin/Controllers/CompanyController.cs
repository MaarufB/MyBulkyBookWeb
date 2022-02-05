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

        public async Task<IActionResult> Index()
        {
            //IEnumerable<Product> objPoductList = _unitOfWork.Product.GetAll();
            return await Task.Run(() => View());
        }

        public async Task<IActionResult> Create()
        {
            return await Task.Run(() => View());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company obj)
        {
            if (!ModelState.IsValid) ModelState.AddModelError("name", "Company name is not valid");

            if (ModelState.IsValid)
            {
                await _unitOfWork.Company.AddAsync(obj);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Company created successfully!";

                return await Task.Run(() => RedirectToAction("Index"));
            }

            return await Task.Run(() => View(obj));
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            Company company = new();

            if (id == null || id == 0)
            {
                // create product

                return await Task.Run(() => View(company));
            }
            else
            {
                //update product
                company = _unitOfWork.Company.GetFirstOrDefaultAsync(u => u.Id == id);
                return await Task.Run(() => View(company));
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Company obj)
        {
            
            if (ModelState.IsValid)
            {
                

                if(obj.Id == 0)
                {
                    await _unitOfWork.Company.AddAsync(obj);
                    TempData["success"] = "Company created successfully!";
                }
                else
                {
                     _unitOfWork.Company.Update(obj);
                    TempData["success"] = "Company updated successfully!";
                }

                await _unitOfWork.SaveAsync();


                return await Task.Run(() => RedirectToAction("Index"));
            }

            return await Task.Run(() => View(obj));
        }
        

        #region API CALLS
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var companyList = _unitOfWork.Company.GetAllAsync();
            return await Task.Run(() => Json(new { data = companyList }));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            var obj = _unitOfWork.Company.GetFirstOrDefaultAsync(c => c.Id == id);

            if (obj == null) return await Task.Run(() => Json(new {success = false, message="Error while deleting"}));

            await _unitOfWork.Company.RemoveAsync(obj);
            await _unitOfWork.SaveAsync();

            return await Task.Run(() => Json(new {success = true, message = "Company successfully deleted"}));

        }

        #endregion
    }
}
