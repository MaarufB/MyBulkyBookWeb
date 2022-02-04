using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using BulkyBook.Utility;
//using System.Drawing;
//using System.Drawing.Drawing2D;
//using System.Drawing.Imaging;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
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
        public async Task<IActionResult> Create(CoverType obj)
        {
            if (!ModelState.IsValid) ModelState.AddModelError("name", "Cover Type name is not valid");

            if (ModelState.IsValid)
            {
                await _unitOfWork.CoverType.AddAsync(obj);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Cover Type created successfully!";

                return RedirectToAction("Index");
            }

            return View(obj);
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                Product = new(),
                CategoryList = _unitOfWork.Category.GetAllAsync().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAllAsync().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            if (id == null || id == 0)
            {
                // create product

                return View(productVM);
            }
            else
            {
                //update product
                productVM.Product = _unitOfWork.Product.GetFirstOrDefaultAsync(u => u.Id == id);
                return View(productVM);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProductVM obj, IFormFile? file)
        {
            
            if (ModelState.IsValid)
            {
                var tempPath = Path.GetTempPath();//_webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    var fileName = Guid.NewGuid().ToString();
                    var uploads = tempPath;  // Path.Combine(tempPath, @"images\products");
                    var extention = Path.GetExtension(file.FileName);
                    var saveFile = Path.Combine(uploads, fileName+ extention);

                    if (obj.Product.ImageUrl != null)
                    {
                        var oldImagePath = Path.Combine(tempPath, obj.Product.ImageUrl.Trim('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }


                    //using (var fileStreams = new FileStream(saveFile, FileMode.Create))
                    //{
                    //    await file.CopyToAsync(fileStreams); 
                    //}

                    //var bitmapImage = new Bitmap(saveFile);
                    //var resizeImage = await ResizeImage(bitmapImage, 300, 300);
                    //var imageArray = await ImageToByte(resizeImage);
                    
                    using (var laborImg = Image.Load(file.OpenReadStream()))
                    {
                        laborImg.Mutate(x => x.Resize(300, 300));
                        laborImg.Save(saveFile);
                        laborImg.Dispose();
                    }

                    byte[] imageArray = System.IO.File.ReadAllBytes(saveFile); //Path.Combine(uploads, fileName + extention)
                    string base64ImageRepresentation = Convert.ToBase64String(imageArray); //imageArray

                    obj.Product.ImageUrl = $"data:image/{extention};base64,{base64ImageRepresentation}"; //@"\images\products\" + fileName + extention;
                }

                if(obj.Product.Id == 0)
                {
                    await _unitOfWork.Product.AddAsync(obj.Product);
                }
                else
                {
                     _unitOfWork.Product.Update(obj.Product);
                }

                await _unitOfWork.SaveAsync();
                TempData["success"] = "Product created successfully!";
                
                return RedirectToAction("Index");
            }

            return View(obj);
        }

        //public IActionResult Delete(int? id)
        //{
        //    if (id == null || id == 0) return NotFound();

        //    var coverTypeFromDBFirst = _unitOfWork.CoverType.GetFirstOrDefault(c => c.Id == id);

        //    if (coverTypeFromDBFirst == null) return NotFound();

        //    return View(coverTypeFromDBFirst);
        //}

        

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _unitOfWork.Product.GetAllAsync(includeProperties:"Category,CoverType");
            return Json(new { data = productList });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int? id)
        {
            var obj = _unitOfWork.Product.GetFirstOrDefaultAsync(c => c.Id == id);

            if (obj == null) return Json(new {success = false, message="Error while deleting"});

            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageUrl.Trim('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            await _unitOfWork.Product.RemoveAsync(obj);
            await _unitOfWork.SaveAsync();

            return Json(new {success = true, message = "Product successfully deleted"});

        }

        #endregion

        

        //public async Task<byte[]> ImageToByte(Image? img)
        //{
        //    ImageConverter converter = new ImageConverter();
        //    byte[]? data = null;

        //    if (img is not null)
        //    {
        //        data = (byte[]?)converter.ConvertTo(img, typeof(byte[]));
        //    }

        //    return await Task.Run(() => data);
        //}

        //public async Task<Bitmap> ResizeImage(Image image, int width, int height)
        //{
        //    var destRect = new Rectangle(0, 0, width, height);
        //    var resultImage = new Bitmap(width, height);
        //    resultImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
        //    using (var graphics = Graphics.FromImage(resultImage))
        //    {
        //        graphics.CompositingMode = CompositingMode.SourceCopy;
        //        graphics.CompositingQuality = CompositingQuality.HighQuality;
        //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //        graphics.SmoothingMode = SmoothingMode.HighQuality;
        //        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        //        using (var wrapMode = new ImageAttributes())
        //        {
        //            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        //            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        //        }
        //    }
        //    return await Task.Run(() => resultImage);
        //}
    }
}
