using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EcoService.Controllers
{
    public class TrombinoscopeController : Controller
    {

        private string _filePath = @"\\10.8.14.65\SmartLoanList\Content\Files\Trombinoscope.xlsx";
        //private string _filePath = @"D:\Projects\RHServicesV210325\SmartLoan2025\EcoService\EcoCerUploads\Trombinoscope.xlsx";


        // GET:  Trombinoscope
        public ActionResult Index()
        {
            var sheetNames = System.Web.HttpRuntime.Cache["TrombiSheetNames"] as List<String>;
            if (sheetNames == null)
            {
                sheetNames = GetSheetNames(_filePath);
                System.Web.HttpRuntime.Cache.Insert("TrombiSheetNames", sheetNames, null, DateTime.Now.AddHours(2), TimeSpan.Zero);
            }
            ViewBag.SheetNames = sheetNames;
            return View();
        }

        private List<String> GetSheetNames(string filepath)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(filepath)))
            {
                var sheetNames = new List<String>();

                foreach (var worksheet in package.Workbook.Worksheets)
                {
                    sheetNames.Add(worksheet.Name);
                }

                return sheetNames;
            }
        }

        private byte[] GetSheetImage(string filepath, string sheetname)
        {
            byte[] imageBytes = null;

            using (var package = new ExcelPackage(new FileInfo(filepath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetname];

                if (worksheet != null && worksheet.Drawings.Count > 0)
                {
                    var drawing = worksheet.Drawings[0] as ExcelPicture;

                    if (drawing != null && drawing.Image != null)
                    {
                        imageBytes = drawing.Image.ImageBytes;
                    }
                }
            }

            return imageBytes;
        }

        private List<byte[]> GetSheetImages(string filepath, string sheetname)
        {
            var imageList = new List<byte[]>();

            using (var package = new ExcelPackage(new FileInfo(filepath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetname];

                if (worksheet != null && worksheet.Drawings.Count > 0)
                {
                    foreach (var drawing in worksheet.Drawings)
                    {
                        if (drawing != null && drawing is ExcelPicture image)
                        {
                            imageList.Add(image.Image.ImageBytes);
                        }
                    }
                }
            }
            return imageList;
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult DisplaySheet(string selectedDepartment)
        {
            string cacheKey = "TrombiImages_" + selectedDepartment;
            var base64ImageStringList = System.Web.HttpRuntime.Cache[cacheKey] as List<string>;
            if (base64ImageStringList == null)
            {
                var sheetImagesList = GetSheetImages(_filePath, selectedDepartment);
                base64ImageStringList = sheetImagesList.Select(img => Convert.ToBase64String(img)).ToList();
                System.Web.HttpRuntime.Cache.Insert(cacheKey, base64ImageStringList, null, DateTime.Now.AddHours(2), TimeSpan.Zero);
            }
            
            ViewBag.SheetName = selectedDepartment;
            ViewBag.SheetImagesList = base64ImageStringList;

            return View("DisplaySheet");
        }

        [System.Web.Mvc.HttpPost]
        public ActionResult DisplaySheett(string selectedDepartment)
        {
            var sheetImage = GetSheetImage(_filePath, selectedDepartment);
            var base64ImageString = sheetImage != null ? Convert.ToBase64String(sheetImage) : null;
            ViewBag.SheetName = selectedDepartment;
            ViewBag.DepartmentImage = base64ImageString;

            return View("DisplaySheet");
        }
    }
}