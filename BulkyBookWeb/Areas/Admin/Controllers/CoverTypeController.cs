using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CoverTypeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var objCoverTypeList = _unitOfWork.CoverType.GetAllAsync();

            return View(objCoverTypeList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType obj)
        {
            if (!ModelState.IsValid) ModelState.AddModelError("name", "Cover Type name is not valid");

            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.AddAsync(obj);
                _unitOfWork.SaveAsync();
                TempData["success"] = "Cover Type created successfully!";

                return RedirectToAction("Index");
            }

            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var coverTypeFromDbFirst = _unitOfWork.CoverType.GetFirstOrDefaultAsync(c => c.Id == id);

            if (coverTypeFromDbFirst == null) return NotFound();

            return View(coverTypeFromDbFirst);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType obj)
        {
            if (!ModelState.IsValid) ModelState.AddModelError("name", "Cover Type is not valid");
            if (ModelState.IsValid)
            {
                _unitOfWork.CoverType.Update(obj);
                _unitOfWork.SaveAsync();
                TempData["success"] = "Cover Type update successfully!";

                return RedirectToAction("Index");
            }

            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var coverTypeFromDBFirst = _unitOfWork.CoverType.GetFirstOrDefaultAsync(c => c.Id == id);

            if (coverTypeFromDBFirst == null) return NotFound();

            return View(coverTypeFromDBFirst);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var obj = _unitOfWork.CoverType.GetFirstOrDefaultAsync(c => c.Id == id);

            if (obj == null) return NotFound();

            _unitOfWork.CoverType.RemoveAsync(obj);
            _unitOfWork.SaveAsync();
            TempData["success"] = "Cover Type deleted successfully!";

            return RedirectToAction("Index");

        }
    }
}
