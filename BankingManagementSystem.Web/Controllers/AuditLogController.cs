using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AuditLogController : Controller
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogController(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? actionFilter, int page = 1)
    {
        var pageSize = 20;
        var query = _auditLogRepository.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(a => a.Action.ToLower().Contains(term) ||
                                     (a.Description != null && a.Description.ToLower().Contains(term)) ||
                                     (a.EntityName != null && a.EntityName.ToLower().Contains(term)) ||
                                     (a.UserId != null && a.UserId.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action == actionFilter.Trim());
        }

        var totalItems = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.ActionFilter = actionFilter;
        ViewBag.PageNumber = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalItems = totalItems;

        return View(logs);
    }
}
