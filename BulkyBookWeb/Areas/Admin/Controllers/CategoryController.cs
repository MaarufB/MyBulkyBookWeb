using BulkyBook.DataAccess;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork  = unitOfWork;
        }

        public async Task<IActionResult>Index()
        {
            var objCategoryList = _unitOfWork.Category.GetAllAsync();//ToListAsync();

            return await Task.Run(() => View(objCategoryList));
        }


        //GET
        public async Task<IActionResult> Create()
        {

            return await Task.Run(() => View());
        }


        //POST
        [HttpPost]
        [ValidateAntiForgeryToken] // Watch the video for more details about AntiForgeToken on dotnetmaster.com
        public IActionResult Create(Category obj)
        {
            // This is a custom Error
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                //ModelState.AddModelError("CustomError", "The displayOrder cannot exactly match the Name."); // This will return to the asp-validation-summary
                ModelState.AddModelError("name", "The displayOrder cannot exactly match the Name."); // This will display on the span element in input name textbox
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.Category.AddAsync(obj);
                _unitOfWork.SaveAsync();
                TempData["success"] = "Category created successfully";

                return RedirectToAction("Index");

            }

            return View(obj);
        }

        // GET method to render the view for edit controller
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            //var categoryFromDB = _db.Categories.Find(id);
            var categoryFromDbFirst = _unitOfWork.Category.GetFirstOrDefaultAsync(c => c.Id == id);
            //var categoryFromDbSingleOrDefault = _db.Categories.SingleOrDefault(c => c.Id == id);

            // You can use single or singleordefault. SingleOrDefault will return empty if the id is not found. The single will throw an error if the id is not found
            // We also have First() and FirstOrDefault(). SingleOr
            // SingleOrDefault Will throw an error if it founds more than 1 elements. FirstOrDefault will return the first element if it founds more than 1 elements  and it will not throw an exception
            // We also have Find() Method and it will find the id as primary key of the table(Model)

            if (categoryFromDbFirst == null) return NotFound();
            
            return await Task.Run(() =>View(categoryFromDbFirst));
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken] // Watch the video for more details about AntiForgeToken on dotnetmaster.com
        public async Task<IActionResult> Edit(Category obj)
        {
            // This is a custom Error
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                //ModelState.AddModelError("CustomError", "The displayOrder cannot exactly match the Name."); // This will return to the asp-validation-summary
                ModelState.AddModelError("name", "The displayOrder cannot exactly match the Name."); // This will display on the span element in input name textbox
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(obj);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Category updated successfully";
                return RedirectToAction("Index");

            }

            return await Task.Run(() => View(obj));
        }





        // GET method to render the view for Delete controller
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var categoryFromDBFirst = _unitOfWork.Category.GetFirstOrDefaultAsync(u => u.Id == id); //_db.Categories.Find(id);

            if (categoryFromDBFirst == null) return NotFound();

            return await Task.Run(() => View(categoryFromDBFirst));
        }

        //POST
        [HttpPost, ActionName("Delete")] // The reason for that is we change the asp-action value to Delete instead of DeletePost
        [ValidateAntiForgeryToken] // Watch the video for more details about AntiForgeToken on dotnetmaster.com
        public async Task<IActionResult> DeletePost(int? id)
        {
            // This is a custom Error
            var obj = _unitOfWork.Category.GetFirstOrDefaultAsync(u => u.Id == id);
            

            if (obj == null) return NotFound();

            await _unitOfWork.Category.RemoveAsync(obj);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Category deleted successfully";

            return await Task.Run(() => RedirectToAction("Index"));
        }
    }
}