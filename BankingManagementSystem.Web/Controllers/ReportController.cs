using System.Text;
using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingManagementSystem.Web.Controllers;

[Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
public class ReportController : Controller
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var summary = await _reportService.GetDailyBankingSummaryAsync(targetDate);
        return View(summary);
    }

    public async Task<IActionResult> Transactions(TransactionReportFilterDto filter)
    {
        var transactions = await _reportService.GetTransactionReportAsync(filter);
        ViewBag.Filter = filter;
        return View(transactions);
    }

    public async Task<IActionResult> ExportTransactionsCsv(TransactionReportFilterDto filter)
    {
        var transactions = await _reportService.GetTransactionReportAsync(filter);
        var csv = new StringBuilder();
        csv.AppendLine("Transaction Reference,Date,Account Number,Customer Name,Type,Amount,Balance After,Status,Description");

        foreach (var t in transactions)
        {
            csv.AppendLine($"\"{t.TransactionReference}\",\"{t.TransactionDate:yyyy-MM-dd HH:mm}\",\"{t.Account?.AccountNumber}\",\"{t.Account?.Customer?.FullName}\",\"{t.TransactionType}\",{t.Amount},{t.BalanceAfterTransaction},\"{t.Status}\",\"{t.Description?.Replace("\"", "\"\"")}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"Transactions_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> Customers(string? status, DateTime? fromDate, DateTime? toDate)
    {
        var customers = await _reportService.GetCustomerReportAsync(status, fromDate, toDate);
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        return View(customers);
    }

    public async Task<IActionResult> ExportCustomersCsv(string? status, DateTime? fromDate, DateTime? toDate)
    {
        var customers = await _reportService.GetCustomerReportAsync(status, fromDate, toDate);
        var csv = new StringBuilder();
        csv.AppendLine("Customer Code,Full Name,Email,Phone,NID,Status,Created At,Total Accounts");

        foreach (var c in customers)
        {
            csv.AppendLine($"\"{c.CustomerCode}\",\"{c.FullName}\",\"{c.Email}\",\"{c.Phone}\",\"{c.NID}\",\"{c.Status}\",\"{c.CreatedAt:yyyy-MM-dd}\",{c.Accounts?.Count ?? 0}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"Customers_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    public async Task<IActionResult> Accounts(int? accountTypeId, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var accounts = await _reportService.GetAccountReportAsync(accountTypeId, status, fromDate, toDate);
        ViewBag.AccountTypeId = accountTypeId;
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        return View(accounts);
    }

    public async Task<IActionResult> Loans(LoanReportFilterDto filter)
    {
        var loans = await _reportService.GetLoanReportAsync(filter);
        ViewBag.Filter = filter;
        return View(loans);
    }
}
