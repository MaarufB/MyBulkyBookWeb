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

        public async Task<IActionResult> Index()
        {
            var objCoverTypeList = await Task.Run(() => _unitOfWork.CoverType.GetAllAsync());

            return await Task.Run(() => View(objCoverTypeList));
        }

        public async Task<IActionResult> Create()
        {
            return await Task.Run(() => View());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CoverType obj)
        {
            if (!ModelState.IsValid) ModelState.AddModelError("name", "Cover Type name is not valid");

            if (ModelState.IsValid)
            {
                await _unitOfWork.CoverType.AddAsync(obj);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Cover Type created successfully!";

                return await Task.Run(() => RedirectToAction("Index"));
            }

            return await Task.Run(() => View(obj));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var coverTypeFromDbFirst = await Task.Run(() => _unitOfWork.CoverType.GetFirstOrDefaultAsync(c => c.Id == id));

            if (coverTypeFromDbFirst == null) return NotFound();

            return await Task.Run(() => View(coverTypeFromDbFirst));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CoverType obj)
        {
            if (!ModelState.IsValid) ModelState.AddModelError("name", "Cover Type is not valid");
            if (ModelState.IsValid)
            {
                await Task.Run(() => _unitOfWork.CoverType.Update(obj));
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Cover Type update successfully!";

                return await Task.Run(() => RedirectToAction("Index"));
            }

            return await Task.Run(() => View(obj));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id == 0) return NotFound();

            var coverTypeFromDBFirst = await Task.Run(() => _unitOfWork.CoverType.GetFirstOrDefaultAsync(c => c.Id == id));

            if (coverTypeFromDBFirst == null) return NotFound();

            return await Task.Run(() => View(coverTypeFromDBFirst));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            var obj = await Task.Run(() => _unitOfWork.CoverType.GetFirstOrDefaultAsync(c => c.Id == id));

            if (obj == null) return NotFound();

            await _unitOfWork.CoverType.RemoveAsync(obj);
            await _unitOfWork.SaveAsync();
            TempData["success"] = "Cover Type deleted successfully!";

            return await Task.Run(() => RedirectToAction("Index"));

        }
    }
}
