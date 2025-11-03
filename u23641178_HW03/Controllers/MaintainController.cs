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
    public class MaintainController : Controller
    {
        private readonly BikeStoresEntities2 _dataContext;
        private const int ITEMS_PER_PAGE_STAFF = 5;
        private const int ITEMS_PER_PAGE_CUSTOMER = 5;
        private const int ITEMS_PER_PAGE_PRODUCT = 10;

        public MaintainController()
        {
            _dataContext = new BikeStoresEntities2();
        }

        // Main management view
        public async Task<ActionResult> Index(int? staffPage, int? customerPage, int? productPage)
        {
            await InitializeViewBagData();

            var pageConfiguration = DeterminePageNumbers(staffPage, customerPage, productPage);

            var managementData = new HomeViewModel
            {
                Staff = await RetrieveAllStaff(),
                Customers = await RetrieveAllCustomers(),
                Products = await RetrieveAllProducts()
            };

            ConfigurePagedLists(managementData, pageConfiguration);

            return View(managementData);
        }

        // Initialize reference data for dropdowns
        private async Task InitializeViewBagData()
        {
            ViewBag.Stores = await _dataContext.stores.ToListAsync();
            ViewBag.Managers = await BuildManagerDropdownList();
            ViewBag.Brands = await _dataContext.brands.ToListAsync();
            ViewBag.Categories = await _dataContext.categories.ToListAsync();
        }

        // Build manager selection options
        private async Task<List<ManagerViewModel>> BuildManagerDropdownList()
        {
            return await _dataContext.staffs
                .Select(s => new ManagerViewModel
                {
                    StaffId = s.staff_id,
                    Name = s.first_name + " " + s.last_name
                })
                .ToListAsync();
        }

        // Determine page numbers with defaults
        private (int staffPageNum, int customerPageNum, int productPageNum) DeterminePageNumbers(
            int? staffPage, int? customerPage, int? productPage)
        {
            return (
                staffPageNum: staffPage ?? 1,
                customerPageNum: customerPage ?? 1,
                productPageNum: productPage ?? 1
            );
        }

        // Configure paged lists for view
        private void ConfigurePagedLists(HomeViewModel data,
            (int staffPageNum, int customerPageNum, int productPageNum) pages)
        {
            ViewBag.StaffPage = data.Staff.ToPagedList(pages.staffPageNum, ITEMS_PER_PAGE_STAFF);
            ViewBag.CustomerPage = data.Customers.ToPagedList(pages.customerPageNum, ITEMS_PER_PAGE_CUSTOMER);
            ViewBag.ProductPage = data.Products.ToPagedList(pages.productPageNum, ITEMS_PER_PAGE_PRODUCT);
        }

        // ===================== STAFF OPERATIONS =====================

        // Retrieve all staff with related data
        private async Task<List<StaffViewModel>> RetrieveAllStaff()
        {
            var staffMembers = await _dataContext.staffs
                .Include(s => s.store)
                .ToListAsync();

            var staffViewModels = new List<StaffViewModel>();

            foreach (var member in staffMembers)
            {
                var supervisorName = await GetSupervisorName(member.manager_id);

                staffViewModels.Add(new StaffViewModel
                {
                    StaffId = member.staff_id,
                    FirstName = member.first_name,
                    LastName = member.last_name,
                    Email = member.email,
                    Phone = member.phone,
                    Active = member.active,
                    StoreId = member.store_id,
                    StoreName = member.store.store_name,
                    ManagerId = member.manager_id,
                    ManagerName = supervisorName
                });
            }

            return staffViewModels;
        }

        // Get supervisor name by ID
        private async Task<string> GetSupervisorName(int? managerId)
        {
            if (!managerId.HasValue)
                return "N/A";

            var supervisor = await _dataContext.staffs
                .Where(m => m.staff_id == managerId.Value)
                .FirstOrDefaultAsync();

            return supervisor != null
                ? $"{supervisor.first_name} {supervisor.last_name}"
                : "N/A";
        }

        // Update staff member
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateStaff(StaffViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed. Please check your input.";
                return RedirectToAction("Index");
            }

            try
            {
                var existingStaff = await _dataContext.staffs.FindAsync(viewModel.StaffId);

                if (existingStaff == null)
                {
                    TempData["Error"] = "Staff member not found in the system.";
                    return RedirectToAction("Index");
                }

                UpdateStaffProperties(existingStaff, viewModel);
                await _dataContext.SaveChangesAsync();

                TempData["Success"] = $"{viewModel.FirstName} {viewModel.LastName} updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Update failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Apply updates to staff entity
        private void UpdateStaffProperties(staff entity, StaffViewModel model)
        {
            entity.first_name = model.FirstName;
            entity.last_name = model.LastName;
            entity.email = model.Email;
            entity.phone = model.Phone;
            entity.active = model.Active;
            entity.store_id = model.StoreId;
            entity.manager_id = model.ManagerId;
        }

        // Delete staff member
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteStaff(int id)
        {
            try
            {
                var staffToRemove = await _dataContext.staffs.FindAsync(id);

                if (staffToRemove == null)
                {
                    TempData["Error"] = "Staff member not found.";
                    return RedirectToAction("Index");
                }

                var staffName = $"{staffToRemove.first_name} {staffToRemove.last_name}";
                _dataContext.staffs.Remove(staffToRemove);
                await _dataContext.SaveChangesAsync();

                TempData["Success"] = $"{staffName} has been removed from the system.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Deletion failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // ===================== CUSTOMER OPERATIONS =====================

        // Retrieve all customers
        private async Task<List<CustomerViewModel>> RetrieveAllCustomers()
        {
            return await _dataContext.customers
                .Select(c => new CustomerViewModel
                {
                    CustomerId = c.customer_id,
                    FirstName = c.first_name,
                    LastName = c.last_name,
                    Phone = c.phone,
                    Email = c.email,
                    Street = c.street,
                    City = c.city,
                    State = c.state,
                    ZipCode = c.zip_code
                })
                .ToListAsync();
        }

        // Update customer information
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateCustomer(CustomerViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed. Please verify all fields.";
                return RedirectToAction("Index");
            }

            try
            {
                var existingCustomer = await _dataContext.customers.FindAsync(viewModel.CustomerId);

                if (existingCustomer == null)
                {
                    TempData["Error"] = "Customer not found in database.";
                    return RedirectToAction("Index");
                }

                ApplyCustomerUpdates(existingCustomer, viewModel);
                await _dataContext.SaveChangesAsync();

                TempData["Success"] = $"Customer {viewModel.FirstName} {viewModel.LastName} updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Update operation failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Apply customer property updates
        private void ApplyCustomerUpdates(customer entity, CustomerViewModel model)
        {
            entity.first_name = model.FirstName;
            entity.last_name = model.LastName;
            entity.phone = model.Phone;
            entity.email = model.Email;
            entity.street = model.Street;
            entity.city = model.City;
            entity.state = model.State;
            entity.zip_code = model.ZipCode;
        }

        // Delete customer record
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customerToDelete = await _dataContext.customers.FindAsync(id);

                if (customerToDelete == null)
                {
                    TempData["Error"] = "Customer record not found.";
                    return RedirectToAction("Index");
                }

                var customerName = $"{customerToDelete.first_name} {customerToDelete.last_name}";
                _dataContext.customers.Remove(customerToDelete);
                await _dataContext.SaveChangesAsync();

                TempData["Success"] = $"{customerName} has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Deletion operation failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // ===================== PRODUCT OPERATIONS =====================

        // Retrieve all products with brand/category
        private async Task<List<ProductViewModel>> RetrieveAllProducts()
        {
            var productList = await _dataContext.products
                .Include(p => p.brand)
                .Include(p => p.category)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.product_id,
                    ProductName = p.product_name,
                    BrandId = p.brand_id,
                    BrandName = p.brand.brand_name,
                    CategoryId = p.category_id,
                    CategoryName = p.category.category_name,
                    ModelYear = p.model_year,
                    ListPrice = p.list_price
                })
                .ToListAsync();

            AssignBrandImages(productList);

            return productList;
        }

        // Assign image paths based on brand
        private void AssignBrandImages(List<ProductViewModel> products)
        {
            foreach (var product in products)
            {
                product.ImagePath = ResolveBrandImagePath(product.BrandName);
            }
        }

        // Map brand to corresponding image
        private string ResolveBrandImagePath(string brandName)
        {
            var brandImageDictionary = new Dictionary<string, string>
            {
                { "Electra", "~/Bikes_Images/Electra_Cruiser.jpeg" },
                { "Haro", "~/Bikes_Images/Haro_Bikes1.jpeg" },
                { "Heller", "~/Bikes_Images/Heller_bike1.jpeg" },
                { "Pure Cycles", "~/Bikes_Images/Pure_Cycles1.jpeg" },
                { "Ritchey", "~/Bikes_Images/Ritchey_bike2.jpeg" },
                { "Strider", "~/Bikes_Images/Strider_balance_bike1.jpeg" },
                { "Sun Bicycles", "~/Bikes_Images/Sun_bicycles_retro.jpeg" },
                { "Surly", "~/Bikes_Images/Surly_bike.jpeg" },
                { "Trek", "~/Bikes_Images/Trek_Marlin_bike.jpeg" }
            };

            return brandImageDictionary.ContainsKey(brandName)
                ? brandImageDictionary[brandName]
                : "~/Bikes_Images/Electra_Cruiser.jpeg";
        }

        // Update product details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateProduct(ProductViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation error. Please review the form.";
                return RedirectToAction("Index");
            }

            try
            {
                var existingProduct = await _dataContext.products.FindAsync(viewModel.ProductId);

                if (existingProduct == null)
                {
                    TempData["Error"] = "Product not found in inventory.";
                    return RedirectToAction("Index");
                }

                ModifyProductProperties(existingProduct, viewModel);
                await _dataContext.SaveChangesAsync();

                TempData["Success"] = $"Product '{viewModel.ProductName}' updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Update failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Modify product entity properties
        private void ModifyProductProperties(product entity, ProductViewModel model)
        {
            entity.product_name = model.ProductName;
            entity.brand_id = model.BrandId;
            entity.category_id = model.CategoryId;
            entity.model_year = model.ModelYear;
            entity.list_price = model.ListPrice;
        }

        // Remove product from inventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteProduct(int id)
        {
            try
            {
                var productToRemove = await _dataContext.products.FindAsync(id);

                if (productToRemove == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                var productName = productToRemove.product_name;
                _dataContext.products.Remove(productToRemove);
                await _dataContext.SaveChangesAsync();

                TempData["Success"] = $"Product '{productName}' has been removed from inventory.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Deletion error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // ===================== CLEANUP =====================
        protected override void Dispose(bool disposing)
        {
            if (disposing && _dataContext != null)
            {
                _dataContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
