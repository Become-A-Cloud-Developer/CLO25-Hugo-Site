+++
title = "4. Registration and Role Promotion"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Let visitors self-register as Candidates, and give admins a page that promotes Candidates to Admin. Self-demotion is disallowed."
weight = 4
draft = false
+++

# Registration and Role Promotion

## Goal

Complete the user management picture. Anyone can now **register** — landing in the Candidate role by default. Admins get a **User admin** page that lists all users and promotes Candidates to Admin (or demotes them back). The current admin can never modify their own roles, which prevents the last-admin-locked-out scenario.

This pulls together every primitive from the previous exercises:

- `UserManager.CreateAsync` for registration
- `SignInManager.SignInAsync` to log the new user in without a second round-trip
- `[Authorize(Roles = "Admin")]` to gate the administration page
- `AddToRoleAsync` / `RemoveFromRoleAsync` for promotion and demotion
- Explicit self-check to refuse modifications to the current principal

> **What you'll learn:**
>
> - How to build a registration flow without Identity's default Razor UI
> - How to enforce "admin privilege is conferred, not self-claimed"
> - Why self-role-modification is always explicitly disallowed
> - How to keep your test suite green while changing the user creation story

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed Exercises 5.1–5.3 — Identity, feature-flagged stores, admin seeding
> - ✓ An admin account (from `AdminSeed` config) that can log in

## Exercise Steps

### Overview

1. **Add** `UserManager` to `AccountController`
2. **Add** the `Register` GET/POST actions
3. **Create** the Register view
4. **Link** to Register from Login
5. **Build** `UserAdminController` for promotion/demotion
6. **Create** the UserAdmin view
7. **Add** a WhoAmI link to UserAdmin for admins
8. **Test Your Implementation**

### **Step 1:** Inject UserManager into AccountController

The Register action needs to create a user, which is `UserManager.CreateAsync` — a separate service from `SignInManager`. Take both through the constructor.

1. **Open** `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

2. **Replace** the constructor with:

   > `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

   ```csharp
   private readonly SignInManager<ApplicationUser> _signInManager;
   private readonly UserManager<ApplicationUser> _userManager;

   public AccountController(
       SignInManager<ApplicationUser> signInManager,
       UserManager<ApplicationUser> userManager)
   {
       _signInManager = signInManager;
       _userManager = userManager;
   }
   ```

### **Step 2:** Add the Register actions

The POST creates an `ApplicationUser`, calls `UserManager.CreateAsync(user, password)`, assigns the Candidate role, and signs the new user in. Any Identity errors (short password, duplicate username) are surfaced via `ModelState`.

1. **Inside** `AccountController`, below `AccessDenied`, **add**:

   > `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

   ```csharp
   [HttpGet]
   public IActionResult Register() => View();

   [HttpPost]
   [ValidateAntiForgeryToken]
   public async Task<IActionResult> Register(string username, string email, string password)
   {
       if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
       {
           ModelState.AddModelError(string.Empty, "Username and password are required.");
           return View();
       }

       var user = new ApplicationUser
       {
           UserName = username,
           Email = email,
           EmailConfirmed = true,
       };

       var create = await _userManager.CreateAsync(user, password);
       if (!create.Succeeded)
       {
           foreach (var error in create.Errors)
               ModelState.AddModelError(string.Empty, error.Description);
           return View();
       }

       // New users always start as Candidate. Promotion to Admin is an
       // explicit administrator action (UserAdminController).
       await _userManager.AddToRoleAsync(user, "Candidate");

       await _signInManager.SignInAsync(user, isPersistent: false);
       return RedirectToAction("Index", "WhoAmI");
   }
   ```

### **Step 3:** Create the Register view

A trivial form with testid hooks for Playwright. The form posts to itself and displays model errors at the top.

1. **Create** `src/CloudSoft.Auth.Web/Views/Account/Register.cshtml`:

   > `src/CloudSoft.Auth.Web/Views/Account/Register.cshtml`

   ```html
   @{
       ViewData["Title"] = "Register";
   }

   <h1>Register</h1>

   @if (!ViewData.ModelState.IsValid)
   {
       <div class="alert alert-danger" data-testid="register-error">
           @Html.ValidationSummary()
       </div>
   }

   <form method="post" data-testid="register-form" style="max-width: 420px;">
       @Html.AntiForgeryToken()

       <div class="mb-3">
           <label for="username" class="form-label">Username</label>
           <input type="text" id="username" name="username" class="form-control"
                  data-testid="register-username" autocomplete="username" />
       </div>

       <div class="mb-3">
           <label for="email" class="form-label">Email</label>
           <input type="email" id="email" name="email" class="form-control"
                  data-testid="register-email" autocomplete="email" />
       </div>

       <div class="mb-3">
           <label for="password" class="form-label">Password</label>
           <input type="password" id="password" name="password" class="form-control"
                  data-testid="register-password" autocomplete="new-password" />
       </div>

       <button type="submit" class="btn btn-primary" data-testid="register-submit">Register</button>
   </form>
   ```

### **Step 4:** Link to Register from Login

Below the existing login form, add a single link so new users find the register page.

1. **In** `src/CloudSoft.Auth.Web/Views/Account/Login.cshtml`, **after** the login `<button>`, **add**:

   > `src/CloudSoft.Auth.Web/Views/Account/Login.cshtml`

   ```html
   <p class="mt-3">
       No account yet?
       <a asp-controller="Account" asp-action="Register" data-testid="login-register-link">Register here</a>.
   </p>
   ```

### **Step 5:** Build UserAdminController

The controller is gated by `[Authorize(Roles = "Admin")]`. The Promote and Demote actions both refuse to operate on the current user — this is the key safety invariant.

1. **Create** `src/CloudSoft.Auth.Web/Controllers/UserAdminController.cs`:

   > `src/CloudSoft.Auth.Web/Controllers/UserAdminController.cs`

   ```csharp
   using CloudSoft.Auth.Web.Data;
   using Microsoft.AspNetCore.Authorization;
   using Microsoft.AspNetCore.Identity;
   using Microsoft.AspNetCore.Mvc;
   using Microsoft.EntityFrameworkCore;

   namespace CloudSoft.Auth.Web.Controllers;

   [Authorize(Roles = "Admin")]
   public class UserAdminController : Controller
   {
       private readonly UserManager<ApplicationUser> _userManager;

       public UserAdminController(UserManager<ApplicationUser> userManager)
       {
           _userManager = userManager;
       }

       [HttpGet]
       public async Task<IActionResult> Index()
       {
           var users = await _userManager.Users.ToListAsync();
           var rows = new List<UserRow>();
           foreach (var user in users)
           {
               var roles = await _userManager.GetRolesAsync(user);
               rows.Add(new UserRow(user.Id, user.UserName ?? "(unknown)", user.Email, roles.ToArray()));
           }
           return View(rows);
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Promote(string userId)
       {
           var user = await _userManager.FindByIdAsync(userId);
           if (user is null) return NotFound();

           var currentUser = await _userManager.GetUserAsync(User);
           if (currentUser is not null && currentUser.Id == user.Id)
           {
               TempData["UserAdminMessage"] = "You cannot modify your own roles.";
               return RedirectToAction(nameof(Index));
           }

           if (!await _userManager.IsInRoleAsync(user, "Admin"))
           {
               await _userManager.AddToRoleAsync(user, "Admin");
               TempData["UserAdminMessage"] = $"{user.UserName} is now Admin.";
           }

           return RedirectToAction(nameof(Index));
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Demote(string userId)
       {
           var user = await _userManager.FindByIdAsync(userId);
           if (user is null) return NotFound();

           var currentUser = await _userManager.GetUserAsync(User);
           if (currentUser is not null && currentUser.Id == user.Id)
           {
               TempData["UserAdminMessage"] = "You cannot demote yourself.";
               return RedirectToAction(nameof(Index));
           }

           if (await _userManager.IsInRoleAsync(user, "Admin"))
           {
               await _userManager.RemoveFromRoleAsync(user, "Admin");
               TempData["UserAdminMessage"] = $"{user.UserName} is no longer Admin.";
           }

           return RedirectToAction(nameof(Index));
       }

       public record UserRow(string Id, string Username, string? Email, string[] Roles);
   }
   ```

> ⚠ **Common Mistakes**
>
> - Omitting the self-check. Without it, an admin could demote themselves; if they were the only admin, the app has no way back.
> - Using `string.Equals(username, currentUsername)` instead of comparing IDs. Usernames can be renamed; user IDs are immutable.

### **Step 6:** Build the UserAdmin view

The view is a table of users. For each row we show promote or demote buttons depending on the current roles, and the caller's own row gets a static "(cannot modify self)" label.

1. **Create** `src/CloudSoft.Auth.Web/Views/UserAdmin/Index.cshtml`:

   > `src/CloudSoft.Auth.Web/Views/UserAdmin/Index.cshtml`

   ```html
   @model IEnumerable<CloudSoft.Auth.Web.Controllers.UserAdminController.UserRow>
   @{
       ViewData["Title"] = "User administration";
       var currentUserName = User.Identity?.Name;
   }

   <h1 data-testid="useradmin-heading">User administration</h1>

   @if (TempData["UserAdminMessage"] is string msg)
   {
       <div class="alert alert-info" data-testid="useradmin-message">@msg</div>
   }

   <table class="table table-striped" data-testid="useradmin-table">
       <thead>
           <tr>
               <th>Username</th><th>Email</th><th>Roles</th><th>Actions</th>
           </tr>
       </thead>
       <tbody>
           @foreach (var row in Model)
           {
               var isCurrent = string.Equals(row.Username, currentUserName, StringComparison.Ordinal);
               <tr data-testid="useradmin-row-@row.Username">
                   <td>@row.Username @(isCurrent ? " (you)" : "")</td>
                   <td>@row.Email</td>
                   <td data-testid="useradmin-roles-@row.Username">
                       @(row.Roles.Length == 0 ? "(none)" : string.Join(", ", row.Roles))
                   </td>
                   <td>
                       @if (isCurrent)
                       {
                           <span class="text-muted">(cannot modify self)</span>
                       }
                       else if (!row.Roles.Contains("Admin"))
                       {
                           <form asp-action="Promote" method="post" style="display:inline">
                               @Html.AntiForgeryToken()
                               <input type="hidden" name="userId" value="@row.Id" />
                               <button type="submit" class="btn btn-sm btn-outline-primary"
                                       data-testid="promote-@row.Username">
                                   Promote to Admin
                               </button>
                           </form>
                       }
                       else
                       {
                           <form asp-action="Demote" method="post" style="display:inline">
                               @Html.AntiForgeryToken()
                               <input type="hidden" name="userId" value="@row.Id" />
                               <button type="submit" class="btn btn-sm btn-outline-warning"
                                       data-testid="demote-@row.Username">
                                   Demote
                               </button>
                           </form>
                       }
                   </td>
               </tr>
           }
       </tbody>
   </table>
   ```

### **Step 7:** Add the User admin link to Who Am I

1. **In** `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`, inside the `User.IsInRole("Admin")` block, **add** a second link beside the AdminOnly one:

   > `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

   ```html
   <a asp-controller="UserAdmin" asp-action="Index" class="btn btn-outline-primary me-2" data-testid="whoami-useradminlink">
       User admin
   </a>
   ```

### **Step 8:** Test Your Implementation

1. **Run** the app. Log in as `admin`.
2. On Who Am I, click **User admin**. You see a table of all users. Your row shows `(you)` and `(cannot modify self)`.
3. Log out. Click **Register here**. Create a user `alice`/`alice@example.com`/`alice`.
4. WhoAmI immediately shows you signed in as `alice` with role `Candidate`.
5. Log out. Log in as admin. On User admin, click **Promote to Admin** next to `alice`.
6. Alice's row now lists both `Candidate` and `Admin` (or just `Admin` depending on how you interpret "promote"). A green banner confirms the change.
7. Click **Demote** to revert. The row returns to `Candidate`.

> ✓ **Success indicators:**
>
> - A fresh registration lands on Who Am I as a signed-in Candidate
> - Admin can promote and demote other users
> - Admin cannot modify their own row
> - Candidates get HTTP 403 / access-denied when hitting `/UserAdmin`

## Common Issues

> **If you encounter problems:**
>
> **Register returns 400:** Missing antiforgery token — check that `@Html.AntiForgeryToken()` is inside the form.
>
> **"Password too short":** The `PasswordOptions` you relaxed in Exercise 5.1 apply here too. If you tightened them for production, the dev seed passwords will fail. Either use stronger test passwords or keep the relaxed options under Development.
>
> **Promote button produces "You cannot modify your own roles.":** You clicked it on your own row. That's working as designed; use a different user's row.

## Summary

The full user-management story now works end-to-end:

- ✓ Anyone can register; new users default to `Candidate`
- ✓ Admins have a dedicated page to promote or demote others
- ✓ Admins cannot modify their own roles
- ✓ Access to the admin page is gated by the same role-based attribute you've been using since Chapter 4

> **Key takeaway:** Administrative privilege is **conferred, not self-claimed**. Registration creates normal users; promotion is an action only an existing admin can perform. The `AdminSeed` mechanism from Exercise 5.3 supplies the bootstrap admin — the rest grow from it.

## Done!

You've completed Chapter 5 and the full eight-exercise arc. The app has:

- Cookie authentication built from primitives (Chapter 4)
- Roles, claims, and policy-based authorization (Chapter 4)
- External login via Google OIDC (Chapter 4)
- ASP.NET Core Identity with a feature-flagged user store (Chapter 5)
- Config-driven idempotent admin seeding (Chapter 5)
- Self-service registration and admin-only role promotion (Chapter 5)

This covers every authentication and authorization mechanism you're likely to use in typical ASP.NET Core applications.
