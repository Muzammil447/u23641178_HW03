using u23641178_HW03.Models;
using u23641178_HW03.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace u23641178_HW03.Controllers
{
    public class HomeController : Controller
    {
        private readonly BikeStoresEntities2 _context;
        private const int STAFF_PAGE_SIZE = 5;
        private const int CUSTOMER_PAGE_SIZE = 5;
        private const int PRODUCT_PAGE_SIZE = 10;

        public HomeController()
        {
            _context = new BikeStoresEntities2();
        }

        // Main dashboard endpoint
        public async Task<ActionResult> Index(int? brandFilter, int? categoryFilter,
            int? staffPage, int? customerPage, int? productPage)
        {
            await PrepareViewData();

            StoreFilterSelections(brandFilter, categoryFilter);
            var pageNumbers = CalculatePageNumbers(staffPage, customerPage, productPage);
            StoreCurrentPages(pageNumbers);

            var dashboardData = new HomeViewModel
            {
                Staff = await LoadEmployeeData(),
                Customers = await LoadClientData(),
                Products = await LoadBicycleInventory(brandFilter, categoryFilter)
            };

            PreparePagedCollections(dashboardData, pageNumbers);

            return View(dashboardData);
        }

        // Prepare dropdown and reference data
        private async Task PrepareViewData()
        {
            ViewBag.Brands = await _context.brands.ToListAsync();
            ViewBag.Categories = await _context.categories.ToListAsync();
            ViewBag.Stores = await _context.stores.ToListAsync();
            ViewBag.Managers = await BuildManagerList();
        }

        // Build manager selection list
        private async Task<List<ManagerViewModel>> BuildManagerList()
        {
            return await _context.staffs
                .Select(employee => new ManagerViewModel
                {
                    StaffId = employee.staff_id,
                    Name = employee.first_name + " " + employee.last_name
                })
                .ToListAsync();
        }

        // Store filter selections in ViewBag
        private void StoreFilterSelections(int? brandId, int? categoryId)
        {
            ViewBag.SelectedBrand = brandId;
            ViewBag.SelectedCategory = categoryId;
        }

        // Calculate page numbers with defaults
        private (int staff, int customer, int product) CalculatePageNumbers(
            int? staffPage, int? customerPage, int? productPage)
        {
            return (
                staff: staffPage ?? 1,
                customer: customerPage ?? 1,
                product: productPage ?? 1
            );
        }

        // Store current page numbers
        private void StoreCurrentPages((int staff, int customer, int product) pages)
        {
            ViewBag.CurrentStaffPage = pages.staff;
            ViewBag.CurrentCustomerPage = pages.customer;
            ViewBag.CurrentProductPage = pages.product;
        }

        // Prepare paged collections for view
        private void PreparePagedCollections(HomeViewModel model,
            (int staff, int customer, int product) pages)
        {
            ViewBag.StaffPage = model.Staff.ToPagedList(pages.staff, STAFF_PAGE_SIZE);
            ViewBag.CustomerPage = model.Customers.ToPagedList(pages.customer, CUSTOMER_PAGE_SIZE);
            ViewBag.ProductPage = model.Products.ToPagedList(pages.product, PRODUCT_PAGE_SIZE);
        }

        // Load employee data with relationships
        private async Task<List<StaffViewModel>> LoadEmployeeData()
        {
            var employeeQuery = _context.staffs
                .Include(e => e.store)
                .Include(e => e.staffs1);

            var employees = await employeeQuery.ToListAsync();

            return employees.Select(emp => new StaffViewModel
            {
                StaffId = emp.staff_id,
                FirstName = emp.first_name,
                LastName = emp.last_name,
                Email = emp.email,
                Phone = emp.phone,
                Active = emp.active,
                StoreId = emp.store_id,
                StoreName = emp.store.store_name,
                ManagerId = emp.manager_id,
                ManagerName = ResolveManagerName(emp.manager_id)
            }).ToList();
        }

        // Resolve manager name from ID
        private string ResolveManagerName(int? managerId)
        {
            if (!managerId.HasValue) return "N/A";

            var manager = _context.staffs
                .Where(m => m.staff_id == managerId.Value)
                .Select(m => m.first_name + " " + m.last_name)
                .FirstOrDefault();

            return manager ?? "N/A";
        }

        // Load customer data
        private async Task<List<CustomerViewModel>> LoadClientData()
        {
            return await _context.customers
                .Select(client => new CustomerViewModel
                {
                    CustomerId = client.customer_id,
                    FirstName = client.first_name,
                    LastName = client.last_name,
                    Phone = client.phone,
                    Email = client.email,
                    Street = client.street,
                    City = client.city,
                    State = client.state,
                    ZipCode = client.zip_code
                })
                .ToListAsync();
        }

        // Load bicycle inventory with filters
        private async Task<List<ProductViewModel>> LoadBicycleInventory(
            int? brandFilter, int? categoryFilter)
        {
            var productQuery = _context.products
                .Include(p => p.brand)
                .Include(p => p.category)
                .AsQueryable();

            productQuery = ApplyProductFilters(productQuery, brandFilter, categoryFilter);

            var bicycles = await productQuery
                .Select(bike => new ProductViewModel
                {
                    ProductId = bike.product_id,
                    ProductName = bike.product_name,
                    BrandId = bike.brand_id,
                    BrandName = bike.brand.brand_name,
                    CategoryId = bike.category_id,
                    CategoryName = bike.category.category_name,
                    ModelYear = bike.model_year,
                    ListPrice = bike.list_price
                })
                .ToListAsync();

            AssignProductImages(bicycles);

            return bicycles;
        }

        // Apply filters to product query
        private IQueryable<product> ApplyProductFilters(IQueryable<product> query,
            int? brandFilter, int? categoryFilter)
        {
            if (brandFilter.HasValue)
                query = query.Where(p => p.brand_id == brandFilter.Value);

            if (categoryFilter.HasValue)
                query = query.Where(p => p.category_id == categoryFilter.Value);

            return query;
        }

        // Assign image paths to products
        private void AssignProductImages(List<ProductViewModel> products)
        {
            foreach (var bike in products)
            {
                bike.ImagePath = MapBrandToImage(bike.BrandName);
            }
        }

        // Map brand names to image paths
        private string MapBrandToImage(string brandName)
        {
            var imageMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Electra"] = "~/Bikes_Images/Electra_Cruiser.jpeg",
                ["Haro"] = "~/Bikes_Images/Haro_Bikes1.jpeg",
                ["Heller"] = "~/Bikes_Images/Heller_bike1.jpeg",
                ["Pure Cycles"] = "~/Bikes_Images/Pure_Cycles1.jpeg",
                ["Ritchey"] = "~/Bikes_Images/Ritchey_bike2.jpeg",
                ["Strider"] = "~/Bikes_Images/Strider_balance_bike1.jpeg",
                ["Sun Bicycles"] = "~/Bikes_Images/Sun_bicycles_retro.jpeg",
                ["Surly"] = "~/Bikes_Images/Surly_bike.jpeg",
                ["Trek"] = "~/Bikes_Images/Trek_Marlin_bike.jpeg"
            };

            return imageMapping.TryGetValue(brandName, out string imagePath)
                ? imagePath
                : "~/Bikes_Images/Electra_Cruiser.jpeg";
        }

        // Create new employee record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateStaff(StaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the validation errors and try again.";
                return RedirectToAction("Index");
            }

            try
            {
                var newEmployee = MapToStaffEntity(model);
                _context.staffs.Add(newEmployee);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{model.FirstName} {model.LastName} has been added successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to create staff member: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Map ViewModel to Entity
        private staff MapToStaffEntity(StaffViewModel model)
        {
            return new staff
            {
                first_name = model.FirstName,
                last_name = model.LastName,
                email = model.Email,
                phone = model.Phone,
                active = model.Active,
                store_id = model.StoreId,
                manager_id = model.ManagerId
            };
        }

        // Create new customer record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateCustomer(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the validation errors and try again.";
                return RedirectToAction("Index");
            }

            try
            {
                var newClient = MapToCustomerEntity(model);
                _context.customers.Add(newClient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Customer {model.FirstName} {model.LastName} has been registered successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to register customer: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Map ViewModel to Entity
        private customer MapToCustomerEntity(CustomerViewModel model)
        {
            return new customer
            {
                first_name = model.FirstName,
                last_name = model.LastName,
                phone = model.Phone,
                email = model.Email,
                street = model.Street,
                city = model.City,
                state = model.State,
                zip_code = model.ZipCode
            };
        }

        // Clean up resources
        protected override void Dispose(bool disposing)
        {
            if (disposing && _context != null)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
