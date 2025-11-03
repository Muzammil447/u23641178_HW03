using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace u23641178_HW03.ViewModels
{
    // Manager representation model
    public class ManagerViewModel
    {
        public int StaffId { get; set; }
        public string Name { get; set; }
    }

    // Client information model
    public class CustomerViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; }

        [Phone]
        [Display(Name = "Contact Number")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Display(Name = "Street Address")]
        public string Street { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "State/Province")]
        public string State { get; set; }

        [Display(Name = "Postal Code")]
        public string ZipCode { get; set; }
    }

    // Main dashboard model
    public class HomeViewModel
    {
        public List<StaffViewModel> Staff { get; set; }
        public List<CustomerViewModel> Customers { get; set; }
        public List<ProductViewModel> Products { get; set; }

        public HomeViewModel()
        {
            Staff = new List<StaffViewModel>();
            Customers = new List<CustomerViewModel>();
            Products = new List<ProductViewModel>();
        }
    }

    // Bicycle product model
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [Display(Name = "Product Name")]
        [StringLength(200)]
        public string ProductName { get; set; }

        [Display(Name = "Brand")]
        [Required(ErrorMessage = "Brand selection is required")]
        public int BrandId { get; set; }

        public string BrandName { get; set; }

        [Display(Name = "Category")]
        [Required(ErrorMessage = "Category selection is required")]
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        [Display(Name = "Model Year")]
        [Range(1900, 2100, ErrorMessage = "Invalid year")]
        public short ModelYear { get; set; }

        [Display(Name = "List Price")]
        [DataType(DataType.Currency)]
        [Range(0, 999999, ErrorMessage = "Price must be positive")]
        public decimal ListPrice { get; set; }

        public string ImagePath { get; set; }
    }

    // Analytics and reporting model
    public class ReportViewModel
    {
        public string ReportType { get; set; }
        public List<PopularProductReport> PopularProducts { get; set; }
        public List<SalesFrequencyReport> SalesFrequency { get; set; }
        public List<CustomerRankingReport> CustomerRanking { get; set; }
        public List<StaffRankingReport> StaffRanking { get; set; }
        public List<StoreRankingReport> StoreRanking { get; set; }
        public List<DurationAnalysisReport> DurationAnalysis { get; set; }
        public List<DocumentArchive> ArchivedDocuments { get; set; }
        public string ChartLabels { get; set; }
        public string ChartData { get; set; }

        public ReportViewModel()
        {
            PopularProducts = new List<PopularProductReport>();
            SalesFrequency = new List<SalesFrequencyReport>();
            CustomerRanking = new List<CustomerRankingReport>();
            StaffRanking = new List<StaffRankingReport>();
            StoreRanking = new List<StoreRankingReport>();
            DurationAnalysis = new List<DurationAnalysisReport>();
            ArchivedDocuments = new List<DocumentArchive>();
        }
    }

    // Best-selling products analysis
    public class PopularProductReport
    {
        public string ProductName { get; set; }
        public string BrandName { get; set; }
        public string CategoryName { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Daily sales tracking
    public class SalesFrequencyReport
    {
        public DateTime OrderDate { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Top customers analysis
    public class CustomerRankingReport
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalSpent { get; set; }
    }

    // Employee performance tracking
    public class StaffRankingReport
    {
        public string StaffName { get; set; }
        public string Email { get; set; }
        public int TotalSales { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Store performance analysis
    public class StoreRankingReport
    {
        public string StoreName { get; set; }
        public string Location { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Shipping timeline analysis
    public class DurationAnalysisReport
    {
        public string Category { get; set; }
        public int TotalOrders { get; set; }
        public int? AvgDaysToShip { get; set; }
    }

    // Saved reports storage model
    public class DocumentArchive
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Description { get; set; }
    }

    // Employee information model
    public class StaffViewModel
    {
        public int StaffId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Contact Number")]
        public string Phone { get; set; }

        [Display(Name = "Active Status")]
        public byte Active { get; set; }

        [Display(Name = "Store Location")]
        [Required(ErrorMessage = "Store selection is required")]
        public int StoreId { get; set; }

        public string StoreName { get; set; }

        [Display(Name = "Reports To")]
        public int? ManagerId { get; set; }

        public string ManagerName { get; set; }
    }
}
