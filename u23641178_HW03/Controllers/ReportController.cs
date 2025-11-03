using u23641178_HW03.Models;
using u23641178_HW03.ViewModels;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;



namespace u23641178_HW03.Controllers
{
  
        public class ReportController : Controller
        {
            private readonly BikeStoresEntities2 _dbContext;
            private readonly string _archiveDirectory = "~/Reports/Archive/";

            public ReportController()
            {
                _dbContext = new BikeStoresEntities2();
                EnsureArchiveDirectoryExists();
            }

            // Ensure archive folder exists
            private void EnsureArchiveDirectoryExists()
            {
                try
                {
                    var physicalDirectory = System.Web.Hosting.HostingEnvironment.MapPath(_archiveDirectory);
                    if (!Directory.Exists(physicalDirectory))
                    {
                        Directory.CreateDirectory(physicalDirectory);
                    }
                }
                catch (Exception) { /* Silently fail */ }
            }

            // Main analytics dashboard
            public async Task<ActionResult> Index(string reportType = "popular")
            {
                var analyticsModel = new ReportViewModel
                {
                    ReportType = reportType,
                    ArchivedDocuments = LoadArchivedReports()
                };

                await PopulateReportData(analyticsModel, reportType);

                return View(analyticsModel);
            }

            // Populate report data based on type
            private async Task PopulateReportData(ReportViewModel model, string reportCategory)
            {
                switch (reportCategory)
                {
                    case "popular":
                        model.PopularProducts = await FetchPopularProductsData();
                        break;
                    case "sales":
                        model.SalesFrequency = await FetchSalesFrequencyData();
                        break;
                    case "customer":
                        model.CustomerRanking = await FetchCustomerRankingData();
                        break;
                    case "staff":
                        model.StaffRanking = await FetchStaffPerformanceData();
                        break;
                    case "store":
                        model.StoreRanking = await FetchStorePerformanceData();
                        break;
                    case "duration":
                        model.DurationAnalysis = await FetchShippingDurationData();
                        break;
                }
            }

            // ===================== DATA RETRIEVAL METHODS =====================

            // Fetch top-selling products
            private async Task<List<PopularProductReport>> FetchPopularProductsData()
            {
                var topProducts = await (
                    from orderItem in _dbContext.order_items
                    join prod in _dbContext.products on orderItem.product_id equals prod.product_id
                    join brand in _dbContext.brands on prod.brand_id equals brand.brand_id
                    join cat in _dbContext.categories on prod.category_id equals cat.category_id
                    group orderItem by new { prod.product_name, brand.brand_name, cat.category_name } into grouped
                    orderby grouped.Count() descending
                    select new PopularProductReport
                    {
                        ProductName = grouped.Key.product_name,
                        BrandName = grouped.Key.brand_name,
                        CategoryName = grouped.Key.category_name,
                        OrderCount = grouped.Count(),
                        TotalRevenue = grouped.Sum(x => x.quantity * x.list_price * (1 - x.discount))
                    }
                ).Take(10).ToListAsync();

                return topProducts;
            }

            // Fetch daily sales statistics
            private async Task<List<SalesFrequencyReport>> FetchSalesFrequencyData()
            {
                var dailySales = await (
                    from ord in _dbContext.orders
                    join orderItem in _dbContext.order_items on ord.order_id equals orderItem.order_id
                    group orderItem by DbFunctions.TruncateTime(ord.order_date) into dailyGroup
                    orderby dailyGroup.Key descending
                    select new SalesFrequencyReport
                    {
                        OrderDate = dailyGroup.Key.Value,
                        TotalOrders = dailyGroup.Select(x => x.order_id).Distinct().Count(),
                        TotalQuantity = dailyGroup.Sum(x => x.quantity),
                        TotalRevenue = dailyGroup.Sum(x => x.quantity * x.list_price * (1 - x.discount))
                    }
                ).Take(30).ToListAsync();

                return dailySales;
            }

            // Fetch top customers by spending
            private async Task<List<CustomerRankingReport>> FetchCustomerRankingData()
            {
                var topCustomers = await (
                    from ord in _dbContext.orders
                    join cust in _dbContext.customers on ord.customer_id equals cust.customer_id
                    join orderItem in _dbContext.order_items on ord.order_id equals orderItem.order_id
                    group orderItem by new { cust.customer_id, cust.first_name, cust.last_name, cust.email } into custGroup
                    orderby custGroup.Count() descending
                    select new CustomerRankingReport
                    {
                        CustomerName = custGroup.Key.first_name + " " + custGroup.Key.last_name,
                        Email = custGroup.Key.email,
                        TotalOrders = custGroup.Select(x => x.order_id).Distinct().Count(),
                        TotalQuantity = custGroup.Sum(x => x.quantity),
                        TotalSpent = custGroup.Sum(x => x.quantity * x.list_price * (1 - x.discount))
                    }
                ).Take(15).ToListAsync();

                return topCustomers;
            }

            // Fetch staff sales performance
            private async Task<List<StaffRankingReport>> FetchStaffPerformanceData()
            {
                var staffPerformance = await (
                    from ord in _dbContext.orders
                    join employee in _dbContext.staffs on ord.staff_id equals employee.staff_id
                    join orderItem in _dbContext.order_items on ord.order_id equals orderItem.order_id
                    group orderItem by new { employee.staff_id, employee.first_name, employee.last_name, employee.email } into empGroup
                    orderby empGroup.Count() descending
                    select new StaffRankingReport
                    {
                        StaffName = empGroup.Key.first_name + " " + empGroup.Key.last_name,
                        Email = empGroup.Key.email,
                        TotalSales = empGroup.Select(x => x.order_id).Distinct().Count(),
                        TotalQuantity = empGroup.Sum(x => x.quantity),
                        TotalRevenue = empGroup.Sum(x => x.quantity * x.list_price * (1 - x.discount))
                    }
                ).ToListAsync();

                return staffPerformance;
            }

            // Fetch store performance metrics
            private async Task<List<StoreRankingReport>> FetchStorePerformanceData()
            {
                var storeMetrics = await (
                    from ord in _dbContext.orders
                    join location in _dbContext.stores on ord.store_id equals location.store_id
                    join orderItem in _dbContext.order_items on ord.order_id equals orderItem.order_id
                    group orderItem by new { location.store_id, location.store_name, location.city, location.state } into storeGroup
                    orderby storeGroup.Sum(x => x.quantity * x.list_price * (1 - x.discount)) descending
                    select new StoreRankingReport
                    {
                        StoreName = storeGroup.Key.store_name,
                        Location = storeGroup.Key.city + ", " + storeGroup.Key.state,
                        TotalOrders = storeGroup.Select(x => x.order_id).Distinct().Count(),
                        TotalQuantity = storeGroup.Sum(x => x.quantity),
                        TotalRevenue = storeGroup.Sum(x => x.quantity * x.list_price * (1 - x.discount))
                    }
                ).ToListAsync();

                return storeMetrics;
            }

            // Fetch shipping duration analysis
            private async Task<List<DurationAnalysisReport>> FetchShippingDurationData()
            {
                var shippingAnalysis = await (
                    from ord in _dbContext.orders
                    where ord.shipped_date.HasValue
                    join orderItem in _dbContext.order_items on ord.order_id equals orderItem.order_id
                    join prod in _dbContext.products on orderItem.product_id equals prod.product_id
                    join cat in _dbContext.categories on prod.category_id equals cat.category_id
                    group ord by cat.category_name into catGroup
                    select new DurationAnalysisReport
                    {
                        Category = catGroup.Key,
                        TotalOrders = catGroup.Count(),
                        AvgDaysToShip = (int?)catGroup.Average(x => (double?)DbFunctions.DiffDays(x.order_date, x.shipped_date))
                    }
                ).ToListAsync();

                return shippingAnalysis;
            }

            // Load archived report files
            private List<DocumentArchive> LoadArchivedReports()
            {
                var directoryPath = Server.MapPath(_archiveDirectory);

                if (!Directory.Exists(directoryPath))
                    return new List<DocumentArchive>();

                var reportFiles = Directory.GetFiles(directoryPath)
                    .Where(f => !f.EndsWith("_desc.txt"))
                    .ToList();

                var archivedReports = new List<DocumentArchive>();

                foreach (var filePath in reportFiles)
                {
                    var fileDetails = new FileInfo(filePath);
                    var descriptionPath = Path.Combine(directoryPath,
                        Path.GetFileNameWithoutExtension(filePath) + "_desc.txt");

                    string fileDescription = string.Empty;

                    if (System.IO.File.Exists(descriptionPath))
                    {
                        fileDescription = System.IO.File.ReadAllText(descriptionPath);
                    }

                    archivedReports.Add(new DocumentArchive
                    {
                        FileName = fileDetails.Name,
                        FileType = fileDetails.Extension,
                        CreatedDate = fileDetails.CreationTime,
                        Description = fileDescription
                    });
                }

                return archivedReports.OrderByDescending(r => r.CreatedDate).ToList();
            }

            // ===================== REPORT SAVING =====================

            [HttpPost]
            public async Task<ActionResult> SaveReport(string reportType, string fileType, string fileName)
            {
                try
                {
                    var savePath = Server.MapPath(_archiveDirectory);
                    if (!Directory.Exists(savePath))
                    {
                        Directory.CreateDirectory(savePath);
                    }

                    var completeFileName = fileName + fileType;
                    var fullPath = Path.Combine(savePath, completeFileName);

                    await GenerateReportFile(reportType, fileType, fullPath);

                    return Json(new { success = true, message = "Report saved successfully!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            // Generate report file based on type and format
            private async Task GenerateReportFile(string reportType, string fileExtension, string outputPath)
            {
                switch (reportType)
                {
                    case "popular":
                        var popularData = await FetchPopularProductsData();
                        if (fileExtension == ".pdf")
                            BuildPopularProductsPDF(popularData, outputPath);
                        else if (fileExtension == ".xlsx")
                            BuildPopularProductsExcel(popularData, outputPath);
                        break;

                    case "sales":
                        var salesData = await FetchSalesFrequencyData();
                        if (fileExtension == ".pdf")
                            BuildSalesFrequencyPDF(salesData, outputPath);
                        else if (fileExtension == ".xlsx")
                            BuildSalesFrequencyExcel(salesData, outputPath);
                        break;

                    case "customer":
                        var customerData = await FetchCustomerRankingData();
                        if (fileExtension == ".pdf")
                            BuildCustomerRankingPDF(customerData, outputPath);
                        else if (fileExtension == ".xlsx")
                            BuildCustomerRankingExcel(customerData, outputPath);
                        break;

                    case "staff":
                        var staffData = await FetchStaffPerformanceData();
                        if (fileExtension == ".pdf")
                            BuildStaffPerformancePDF(staffData, outputPath);
                        else if (fileExtension == ".xlsx")
                            BuildStaffPerformanceExcel(staffData, outputPath);
                        break;

                    case "store":
                        var storeData = await FetchStorePerformanceData();
                        if (fileExtension == ".pdf")
                            BuildStorePerformancePDF(storeData, outputPath);
                        else if (fileExtension == ".xlsx")
                            BuildStorePerformanceExcel(storeData, outputPath);
                        break;

                    case "duration":
                        var durationData = await FetchShippingDurationData();
                        if (fileExtension == ".pdf")
                            BuildShippingDurationPDF(durationData, outputPath);
                        else if (fileExtension == ".xlsx")
                            BuildShippingDurationExcel(durationData, outputPath);
                        break;
                }
            }

            // ===================== PDF GENERATION METHODS =====================

            private void BuildPopularProductsPDF(List<PopularProductReport> data, string outputPath)
            {
                using (var document = new Document(PageSize.A4, 50, 50, 25, 25))
                {
                    PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
                    document.Open();

                    AddReportTitle(document, "Top-Selling Products Analysis");
                    AddGenerationTimestamp(document);

                    var dataTable = CreatePdfTable(new float[] { 3f, 2f, 2f, 1.5f, 2f });
                    AddTableHeaders(dataTable, new[] { "Product Name", "Brand", "Category", "Orders", "Revenue" });

                    foreach (var item in data)
                    {
                        AddTableRow(dataTable, new object[]
                        {
                        item.ProductName,
                        item.BrandName,
                        item.CategoryName,
                        item.OrderCount,
                        item.TotalRevenue.ToString("C")
                        });
                    }

                    document.Add(dataTable);
                    document.Close();
                }
            }

            private void BuildSalesFrequencyPDF(List<SalesFrequencyReport> data, string outputPath)
            {
                using (var document = new Document(PageSize.A4, 50, 50, 25, 25))
                {
                    PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
                    document.Open();

                    AddReportTitle(document, "Daily Sales Frequency Analysis");
                    AddGenerationTimestamp(document);

                    var dataTable = CreatePdfTable(new float[] { 2f, 2f, 2f, 2f });
                    AddTableHeaders(dataTable, new[] { "Date", "Total Orders", "Total Quantity", "Total Revenue" });

                    foreach (var item in data)
                    {
                        AddTableRow(dataTable, new object[]
                        {
                        item.OrderDate.ToString("yyyy-MM-dd"),
                        item.TotalOrders,
                        item.TotalQuantity,
                        item.TotalRevenue.ToString("C")
                        });
                    }

                    document.Add(dataTable);
                    document.Close();
                }
            }

            private void BuildCustomerRankingPDF(List<CustomerRankingReport> data, string outputPath)
            {
                using (var document = new Document(PageSize.A4, 50, 50, 25, 25))
                {
                    PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
                    document.Open();

                    AddReportTitle(document, "Top Customer Performance Rankings");
                    AddGenerationTimestamp(document);

                    var dataTable = CreatePdfTable(new float[] { 1f, 3f, 3f, 2f, 2f });
                    AddTableHeaders(dataTable, new[] { "Rank", "Customer Name", "Email", "Total Orders", "Total Spent" });

                    int position = 1;
                    foreach (var item in data)
                    {
                        AddTableRow(dataTable, new object[]
                        {
                        position,
                        item.CustomerName,
                        item.Email,
                        item.TotalOrders,
                        item.TotalSpent.ToString("C")
                        });
                        position++;
                    }

                    document.Add(dataTable);
                    document.Close();
                }
            }

            private void BuildStaffPerformancePDF(List<StaffRankingReport> data, string outputPath)
            {
                using (var document = new Document(PageSize.A4, 50, 50, 25, 25))
                {
                    PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
                    document.Open();

                    AddReportTitle(document, "Employee Performance Rankings");
                    AddGenerationTimestamp(document);

                    var dataTable = CreatePdfTable(new float[] { 1f, 3f, 3f, 2f, 2f });
                    AddTableHeaders(dataTable, new[] { "Rank", "Staff Name", "Email", "Total Sales", "Total Revenue" });

                    int position = 1;
                    foreach (var item in data)
                    {
                        AddTableRow(dataTable, new object[]
                        {
                        position,
                        item.StaffName,
                        item.Email,
                        item.TotalSales,
                        item.TotalRevenue.ToString("C")
                        });
                        position++;
                    }

                    document.Add(dataTable);
                    document.Close();
                }
            }

            private void BuildStorePerformancePDF(List<StoreRankingReport> data, string outputPath)
            {
                using (var document = new Document(PageSize.A4, 50, 50, 25, 25))
                {
                    PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
                    document.Open();

                    AddReportTitle(document, "Store Performance Analysis");
                    AddGenerationTimestamp(document);

                    var dataTable = CreatePdfTable(new float[] { 1f, 2f, 2f, 2f, 2f });
                    AddTableHeaders(dataTable, new[] { "Rank", "Store Name", "Location", "Total Orders", "Total Revenue" });

                    int position = 1;
                    foreach (var item in data)
                    {
                        AddTableRow(dataTable, new object[]
                        {
                        position,
                        item.StoreName,
                        item.Location,
                        item.TotalOrders,
                        item.TotalRevenue.ToString("C")
                        });
                        position++;
                    }

                    document.Add(dataTable);
                    document.Close();
                }
            }

            private void BuildShippingDurationPDF(List<DurationAnalysisReport> data, string outputPath)
            {
                using (var document = new Document(PageSize.A4, 50, 50, 25, 25))
                {
                    PdfWriter.GetInstance(document, new FileStream(outputPath, FileMode.Create));
                    document.Open();

                    AddReportTitle(document, "Shipping Duration Analysis by Category");
                    AddGenerationTimestamp(document);

                    var dataTable = CreatePdfTable(new float[] { 3f, 2f, 2f });
                    AddTableHeaders(dataTable, new[] { "Category", "Total Orders", "Avg Days to Ship" });

                    foreach (var item in data)
                    {
                        var avgDays = item.AvgDaysToShip.HasValue
                            ? item.AvgDaysToShip.Value.ToString("F1") + " days"
                            : "N/A";

                        AddTableRow(dataTable, new object[]
                        {
                        item.Category,
                        item.TotalOrders,
                        avgDays
                        });
                    }

                    document.Add(dataTable);
                    document.Close();
                }
            }

            // PDF Helper Methods
            private void AddReportTitle(Document doc, string titleText)
            {
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var title = new Paragraph(titleText, titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                doc.Add(title);
            }

            private void AddGenerationTimestamp(Document doc)
            {
                var timestampFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var timestamp = new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", timestampFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20
                };
                doc.Add(timestamp);
            }

            private PdfPTable CreatePdfTable(float[] columnWidths)
            {
                var table = new PdfPTable(columnWidths.Length)
                {
                    WidthPercentage = 100
                };
                table.SetWidths(columnWidths);
                return table;
            }

            private void AddTableHeaders(PdfPTable table, string[] headers)
            {
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                foreach (var header in headers)
                {
                    table.AddCell(new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });
                }
            }

            private void AddTableRow(PdfPTable table, object[] values)
            {
                var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                foreach (var value in values)
                {
                    table.AddCell(new Phrase(value.ToString(), dataFont));
                }
            }
            // ===================== EXCEL GENERATION METHODS =====================

            private void BuildPopularProductsExcel(List<PopularProductReport> data, string outputPath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Top Products");

                ConfigureExcelTitle(worksheet, "Top-Selling Products Analysis", 5);
                ConfigureExcelTimestamp(worksheet, 5);

                var headers = new[] { "Product Name", "Brand", "Category", "Order Count", "Total Revenue" };
                AddExcelHeaders(worksheet, headers, 4);

                int currentRow = 5;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.ProductName;
                    worksheet.Cell(currentRow, 2).Value = item.BrandName;
                    worksheet.Cell(currentRow, 3).Value = item.CategoryName;
                    worksheet.Cell(currentRow, 4).Value = item.OrderCount;
                    worksheet.Cell(currentRow, 5).Value = item.TotalRevenue;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(outputPath);
            }
        }

        private void BuildSalesFrequencyExcel(List<SalesFrequencyReport> data, string outputPath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sales Frequency");

                ConfigureExcelTitle(worksheet, "Daily Sales Frequency Analysis", 4);
                ConfigureExcelTimestamp(worksheet, 4);

                var headers = new[] { "Date", "Total Orders", "Total Quantity", "Total Revenue" };
                AddExcelHeaders(worksheet, headers, 4);

                int currentRow = 5;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.OrderDate;
                    worksheet.Cell(currentRow, 1).Style.DateFormat.Format = "yyyy-MM-dd";
                    worksheet.Cell(currentRow, 2).Value = item.TotalOrders;
                    worksheet.Cell(currentRow, 3).Value = item.TotalQuantity;
                    worksheet.Cell(currentRow, 4).Value = item.TotalRevenue;
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "$#,##0.00";
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(outputPath);
            }
        }

        private void BuildCustomerRankingExcel(List<CustomerRankingReport> data, string outputPath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Customer Rankings");

                ConfigureExcelTitle(worksheet, "Top Customer Performance Rankings", 5);
                ConfigureExcelTimestamp(worksheet, 5);

                var headers = new[] { "Rank", "Customer Name", "Email", "Total Orders", "Total Spent" };
                AddExcelHeaders(worksheet, headers, 4);

                int currentRow = 5;
                int rankPosition = 1;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = rankPosition;
                    worksheet.Cell(currentRow, 2).Value = item.CustomerName;
                    worksheet.Cell(currentRow, 3).Value = item.Email;
                    worksheet.Cell(currentRow, 4).Value = item.TotalOrders;
                    worksheet.Cell(currentRow, 5).Value = item.TotalSpent;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    currentRow++;
                    rankPosition++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(outputPath);
            }
        }

        private void BuildStaffPerformanceExcel(List<StaffRankingReport> data, string outputPath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Staff Performance");

                ConfigureExcelTitle(worksheet, "Employee Performance Rankings", 5);
                ConfigureExcelTimestamp(worksheet, 5);

                var headers = new[] { "Rank", "Staff Name", "Email", "Total Sales", "Total Revenue" };
                AddExcelHeaders(worksheet, headers, 4);

                int currentRow = 5;
                int rankPosition = 1;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = rankPosition;
                    worksheet.Cell(currentRow, 2).Value = item.StaffName;
                    worksheet.Cell(currentRow, 3).Value = item.Email;
                    worksheet.Cell(currentRow, 4).Value = item.TotalSales;
                    worksheet.Cell(currentRow, 5).Value = item.TotalRevenue;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    currentRow++;
                    rankPosition++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(outputPath);
            }
        }

        private void BuildStorePerformanceExcel(List<StoreRankingReport> data, string outputPath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Store Performance");

                ConfigureExcelTitle(worksheet, "Store Performance Analysis", 5);
                ConfigureExcelTimestamp(worksheet, 5);

                var headers = new[] { "Rank", "Store Name", "Location", "Total Orders", "Total Revenue" };
                AddExcelHeaders(worksheet, headers, 4);

                int currentRow = 5;
                int rankPosition = 1;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = rankPosition;
                    worksheet.Cell(currentRow, 2).Value = item.StoreName;
                    worksheet.Cell(currentRow, 3).Value = item.Location;
                    worksheet.Cell(currentRow, 4).Value = item.TotalOrders;
                    worksheet.Cell(currentRow, 5).Value = item.TotalRevenue;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    currentRow++;
                    rankPosition++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(outputPath);
            }
        }

        private void BuildShippingDurationExcel(List<DurationAnalysisReport> data, string outputPath)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Shipping Duration");

                ConfigureExcelTitle(worksheet, "Shipping Duration Analysis by Category", 3);
                ConfigureExcelTimestamp(worksheet, 3);

                var headers = new[] { "Category", "Total Orders", "Avg Days to Ship" };
                AddExcelHeaders(worksheet, headers, 4);

                int currentRow = 5;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.Category;
                    worksheet.Cell(currentRow, 2).Value = item.TotalOrders;
                    worksheet.Cell(currentRow, 3).Value = item.AvgDaysToShip.HasValue ? item.AvgDaysToShip.Value : 0;
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "0.0";
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(outputPath);
            }
        }

        // Excel Helper Methods
        private void ConfigureExcelTitle(IXLWorksheet sheet, string titleText, int columnSpan)
        {
            sheet.Cell(1, 1).Value = titleText;
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;
            sheet.Range(1, 1, 1, columnSpan).Merge();
        }

        private void ConfigureExcelTimestamp(IXLWorksheet sheet, int columnSpan)
        {
            sheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
            sheet.Range(2, 1, 2, columnSpan).Merge();
        }

        private void AddExcelHeaders(IXLWorksheet sheet, string[] headerTexts, int headerRow)
        {
            for (int i = 0; i < headerTexts.Length; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headerTexts[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }
        }

        // ===================== FILE OPERATIONS =====================

        [HttpGet]
        public ActionResult DownloadReport(string fileName)
        {
            try
            {
                var directoryPath = Server.MapPath(_archiveDirectory);
                var fullFilePath = Path.Combine(directoryPath, fileName);

                if (!System.IO.File.Exists(fullFilePath))
                {
                    return HttpNotFound("The requested file does not exist.");
                }

                var fileBytes = System.IO.File.ReadAllBytes(fullFilePath);
                var mimeType = DetermineMimeType(fileName);

                return File(fileBytes, mimeType, fileName);
            }
            catch (Exception ex)
            {
                return Content($"Download error: {ex.Message}");
            }
        }

        private string DetermineMimeType(string fileName)
        {
            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return "application/pdf";
            else if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            else
                return "application/octet-stream";
        }

        [HttpPost]
        public ActionResult DeleteReport(string fileName)
        {
            try
            {
                var directoryPath = Server.MapPath(_archiveDirectory);
                var reportFilePath = Path.Combine(directoryPath, fileName);
                var descriptionFilePath = Path.Combine(directoryPath,
                    Path.GetFileNameWithoutExtension(fileName) + "_desc.txt");

                if (System.IO.File.Exists(reportFilePath))
                {
                    System.IO.File.Delete(reportFilePath);
                }

                if (System.IO.File.Exists(descriptionFilePath))
                {
                    System.IO.File.Delete(descriptionFilePath);
                }

                return Json(new { success = true, message = "Report deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Deletion failed: {ex.Message}" });
            }
        }

        [HttpPost]
        public ActionResult UpdateDescription(string fileName, string description)
        {
            try
            {
                var directoryPath = Server.MapPath(_archiveDirectory);
                var descriptionFilePath = Path.Combine(directoryPath,
                    Path.GetFileNameWithoutExtension(fileName) + "_desc.txt");

                System.IO.File.WriteAllText(descriptionFilePath, description);

                return Json(new { success = true, message = "Description updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Update failed: {ex.Message}" });
            }
        }

        // ===================== CLEANUP =====================
        protected override void Dispose(bool disposing)
        {
            if (disposing && _dbContext != null)
            {
                _dbContext.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}